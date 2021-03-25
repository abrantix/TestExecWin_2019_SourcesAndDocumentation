//------------------------------------------------------------------------------
//  Copyright(C) 2016  Gerald Fahrnholz
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program. If not, see <http://www.gnu.org/licenses/>.
//
//    Contact: http://www.gerald-fahrnholz.eu/impressum.php
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using EnvDTE80;
using System.IO;
using System.Threading;

namespace TestExecWin
{
    /// [derive from notification interfaces]
    /// Class derives from notification interfaces to get
    /// informed when user performs changes within Visual Studio.
    class VisualStudioConnector 
        : Microsoft.VisualStudio.Shell.Interop.IVsSelectionEvents
        , Microsoft.VisualStudio.Shell.Interop.IVsUpdateSolutionEvents
    /// [derive from notification interfaces]
    {
        public static EnvDTE80.DTE2 dte;

        private IVsMonitorSelection m_monitorSelection = null;
        private uint m_selectionEventsCookie = 0;

        private IVsSolutionBuildManager m_solutionBuildManager = null;
        private uint m_updateSolutionEventsCookie = 0;

        private IMainEvents m_mainEvents;
        private IEventReceiver m_eventReceiver;

        private ManualResetEvent m_buildCompletedEvent = new ManualResetEvent(false);

        public VisualStudioConnector(IMainEvents in_mainEvents, IEventReceiver in_eventReceiver)
        {
            m_mainEvents = in_mainEvents;
            m_eventReceiver = in_eventReceiver;
            ConnectWithVisualStudio();
        }

        /// [connect with VS]
        private void ConnectWithVisualStudio()
        {
            bool connectedToSelectionEvents = false;
            bool connectedToUpdateSolutionEvents = false;

            // Advise to selection events (e.g. startup project changed)
            m_monitorSelection = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.
                GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            if (m_monitorSelection != null)
            {
                m_monitorSelection.AdviseSelectionEvents(
                    this, out m_selectionEventsCookie);
                connectedToSelectionEvents = true;
            }
            else
            {
                WriteLine(1, "ConnectWithVisualStudio: GetService(SVsShellMonitorSelection) failed!");
            }

            // Advise to update solution events (e.g. switched debug/release configuration)
            m_solutionBuildManager = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.
                GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
            if (m_solutionBuildManager != null)
            {
                m_solutionBuildManager.AdviseUpdateSolutionEvents(
                    this, out m_updateSolutionEventsCookie);
                connectedToUpdateSolutionEvents = true;
            }
            else
            {
                WriteLine(1, "ConnectWithVisualStudio: GetService(SVsSolutionBuildManager) failed!");
            }

            if (connectedToSelectionEvents && connectedToUpdateSolutionEvents)
            {
                WriteLine(1, "ConnectWithVisualStudio succeeded");
            }
        }
        /// [connect with VS]

        /// [disconnect from VS]
        /// Release all connections to Visual Studio.
        /// This function is called from Dispose method of main app class TestExecWindow
        public void DisconnectFromVisualStudio()
        {
            if (m_monitorSelection != null && m_selectionEventsCookie != 0)
                m_monitorSelection.UnadviseSelectionEvents(m_selectionEventsCookie);
            if (m_solutionBuildManager != null && m_updateSolutionEventsCookie != 0)
                m_solutionBuildManager.UnadviseUpdateSolutionEvents(m_updateSolutionEventsCookie);
        }
        /// [disconnect from VS]


        public ProjectInfo ReadSettingsOfAllProjects()
        {
            ProjectInfo projectInfo = new ProjectInfo();
            WriteLine(3, "ReadSettingsOfAllProjects-Begin");
            try
            {
                if (dte == null)
                {
                    WriteLine(1, "dte is null (checking for projects is not possible)");
                    return projectInfo;
                }
                if (dte.Solution == null)
                {
                    WriteLine(1, "dte.Solution is null (checking for projects is not possible)");
                    return projectInfo;
                }
                if (dte.Solution.SolutionBuild == null)
                {
                    WriteLine(1, "dte.Solution.SolutionBuild is null (checking for projects is not possible)");
                    return projectInfo;
                }
                if (dte.Solution.SolutionBuild.ActiveConfiguration == null)
                {
                    WriteLine(1, "dte.Solution.SolutionBuild.ActiveConfiguration is null (checking for projects is not possible)");
                    return projectInfo;
                }

                projectInfo.solutionFullPath = dte.Solution.FileName;

                /// [get config name]
                //Get name of config (e.g. "Debug", "Release"
                string configName = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
                /// [get config name]

                string msg = "";

                /// [get name startup project]
                EnvDTE80.SolutionBuild2 sb = (EnvDTE80.SolutionBuild2)dte.Solution.SolutionBuild;
                if (sb == null)
                {
                    WriteLine(1, "SolutionBuild is null (checking for projects is not possible)");
                    return projectInfo;
                }

                // for further processing extract the name without path and extension
                // e.g. nameStartupProject = "TestRunner"
                // (extraction code not shown here)
                /// [get name startup project]

                // To simple solution which does not work for startup projects within a filter
                //EnvDTE.Project startupProj = dte.Solution.Item(msg);

                /// [get startup project]
                // Perform recursive search for the startup project through all solution filters:
                EnvDTE.Project[] projects = GetAllProjects(false);
                /// [get startup project]

                //iterate through main projects
                foreach (var dteProject in projects)
                {
                    var project = new Project();
                    project.DTEProject = dteProject;
                    project.ProjectName = dteProject.Name;

                    var projectFiles = new List<FileInfo>();

                    foreach (EnvDTE.ProjectItem fileItem in GetAllProjectItemsFromKind(project.DTEProject, EnvDTE.Constants.vsProjectItemKindPhysicalFile))
                    {
                        for (short i = 0; i < fileItem.FileCount; i++)
                        {
                            var fileInfo = new FileInfo(fileItem.FileNames[i]);
                            projectFiles.Add(fileInfo);
                        }
                    }
                    project.ProjectFiles = projectFiles.Distinct().ToArray();

                    try
                    {
                        project.SourceDirPath = System.IO.Path.GetDirectoryName(dteProject.FullName);
                    } catch(Exception ex)
                    {
                        WriteLine(3, "ReadSettingsOfAllProjects EXCEPTION: " + ex.ToString());
                        continue;
                    }
                    
                    EnvDTE.ConfigurationManager cm = dteProject.ConfigurationManager;
                    if (cm == null)
                    {
                        WriteLine(1, "No ConfigurationManager found");
                        WriteLine(3, "ReadSettingsOfAllProjects: no ConfigurationManager found");
                        continue;
                    }
                    if (cm.ActiveConfiguration == null)
                    {
                        WriteLine(1, "No ActiveConfiguration found");
                        WriteLine(3, "ReadSettingsOfAllProjects: no ActiveConfiguration found");
                        continue;
                    }
                    msg = "Platform=" + cm.ActiveConfiguration.PlatformName;
                    WriteLine(2, msg);

                    EnvDTE.Properties props = cm.ActiveConfiguration.Properties;
                    if (props != null)
                    {
                        WriteLine(2, "Now iterating over ActiveConfiguration.Properties...");

                        // Scan properties of ActiveConfiguration to be used for future extended requests
                        msg = "ReadSettingsOfStartupProject: ActiveConfiguration.Properties";
                        foreach (EnvDTE.Property p in props)
                        {
                            msg += "  " + p.Name;
                        }
                        WriteLine(2, msg);
                    }
                    /// [get exe path]
                    // Get full path of executable depending on found
                    // startup project and configuration (Debug/Release)
                    Microsoft.VisualStudio.VCProjectEngine.VCProject vcProj =
                        (Microsoft.VisualStudio.VCProjectEngine.VCProject)dteProject.Object;
                    Microsoft.VisualStudio.VCProjectEngine.IVCCollection configs =
                        (Microsoft.VisualStudio.VCProjectEngine.IVCCollection)vcProj.Configurations;
                    Microsoft.VisualStudio.VCProjectEngine.VCConfiguration config =
                        FindConfig(configs, configName);
                    if (config == null)
                    {
                        WriteLine(1, "Config " + configName + " not found");
                        continue;
                    }
                    msg = "PrimaryOutput (FullExePath)=" + config.PrimaryOutput;
                    WriteLine(2, msg);
                    /// [get exe path]
                    project.FullExePath = config.PrimaryOutput;
                    string delimiter = "/\\";
                    int posPathEnd = project.FullExePath.LastIndexOfAny(delimiter.ToCharArray());
                    if (posPathEnd > 0)
                    {
                        project.TargetDirPath = project.FullExePath.Substring(0, posPathEnd);
                    }
                    msg = "ReadSettingsOfAllProjects: OutputPath=" + project.TargetDirPath;
                    WriteLine(2, msg);

                    // Scan properties to be used for future extended requests
                    msg = "ReadSettingsOfAllProjects: startupProj.Properties";
                    foreach (EnvDTE.Property p in dteProject.Properties)
                    {
                        msg += "  " + p.Name;
                    }
                    WriteLine(3, msg);
                    projectInfo.AddProject(project);
                }
                projectInfo.config = configName;

                WriteLine(3, "ReadSettingsOfAllProjects-End");
            }
            catch (Exception ex)
            {
                WriteLine(1, "ReadSettingsOfAllProjects-End: EXCEPTION: " + ex.ToString());
            }
            return projectInfo;
        }

        public async Task BuildAsync(ProjectInfo projectInfo)
        {
            try
            {
                if (projectInfo.SelectedProject != null)
                {
                    string configName = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
                    Microsoft.VisualStudio.VCProjectEngine.VCProject vcProj =
                        (Microsoft.VisualStudio.VCProjectEngine.VCProject)projectInfo.SelectedProject.DTEProject.Object;
                    Microsoft.VisualStudio.VCProjectEngine.IVCCollection configs =
                        (Microsoft.VisualStudio.VCProjectEngine.IVCCollection)vcProj.Configurations;
                    Microsoft.VisualStudio.VCProjectEngine.VCConfiguration config =
                        FindConfig(configs, configName);
                    var solutionName = Path.GetFileNameWithoutExtension(dte.Solution.FileName);

                    dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Activate();
                    dte.ToolWindows.SolutionExplorer.GetItem($"{solutionName}\\{projectInfo.SelectedProject.DTEProject.Name}").Select(EnvDTE.vsUISelectionType.vsUISelectionTypeSelect);
                    m_buildCompletedEvent.Reset();
                    dte.Events.BuildEvents.OnBuildDone += BuildEvents_OnBuildDone;
                    dte.ExecuteCommand("ClassViewContextMenus.ClassViewProject.Build");
                    await Task.Run(() => m_buildCompletedEvent.WaitOne());
                }
            }
            catch (Exception ex)
            {
                WriteLine(1, "Could not start debugging\nEXCEPTION: " + ex.ToString());
            }
        }

        private void BuildEvents_OnBuildDone(EnvDTE.vsBuildScope Scope, EnvDTE.vsBuildAction Action)
        {
            m_buildCompletedEvent.Set();
        }

        /// [start debugging]
        public void StartDebugging(ProjectInfo projectInfo, string in_cmdLineParams)
        {
            try
            {
                if (projectInfo.SelectedProject != null)
                {
                    //m_startupProj.ConfigurationManager.ActiveConfiguration.Properties.
                    //    Item("CommandArguments").Value = in_cmdLineParams;
                    string configName = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
                    Microsoft.VisualStudio.VCProjectEngine.VCProject vcProj =
                        (Microsoft.VisualStudio.VCProjectEngine.VCProject)projectInfo.SelectedProject.DTEProject.Object;
                    Microsoft.VisualStudio.VCProjectEngine.IVCCollection configs =
                        (Microsoft.VisualStudio.VCProjectEngine.IVCCollection)vcProj.Configurations;
                    Microsoft.VisualStudio.VCProjectEngine.VCConfiguration config =
                        FindConfig(configs, configName);
                    Microsoft.VisualStudio.VCProjectEngine.VCDebugSettings dbgSettings =
                        (Microsoft.VisualStudio.VCProjectEngine.VCDebugSettings)config.DebugSettings;
                    dbgSettings.CommandArguments = in_cmdLineParams;
                    dbgSettings.Command = projectInfo.GetExePath();
                    var solutionName = Path.GetFileNameWithoutExtension(dte.Solution.FileName);

                    dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Activate();
                    dte.ToolWindows.SolutionExplorer.GetItem($"{solutionName}\\{projectInfo.SelectedProject.DTEProject.Name}").Select(EnvDTE.vsUISelectionType.vsUISelectionTypeSelect);

                    WriteLine(2, "StartDebugging: now starting debugger with dbgSettings.CommandArguments=" + dbgSettings.CommandArguments);
                    dte.ExecuteCommand("ClassViewContextMenus.ClassViewProject.Debug.Startnewinstance");
                    //dte.Debugger.Go(false /* do not wait for end of debugging*/);
                }
            }
            catch (Exception ex)
            {
                WriteLine(1, "Could not start debugging\nEXCEPTION: " + ex.ToString());
            }
        }
        /// [start debugging]

        /// [open text file]
        public void OpenFile(string in_fullFilePath, int in_lineNum = 0)
        {
            WriteLine(3, "OpenFile " + in_fullFilePath + " line " + in_lineNum);
            try
            {
                dte.ItemOperations.OpenFile(in_fullFilePath, EnvDTE.Constants.vsViewKindTextView);
                if (in_lineNum>0)
                {
                    dte.ExecuteCommand("Edit.Goto",in_lineNum.ToString());
                }
            }
            catch (Exception ex)
            {
                WriteLine(1, "Could not open file " + in_fullFilePath + "\nEXCEPTION: " + ex.ToString());
            }
        }
        /// [open text file]

        public string GetExecutablesFromCurrentSolution()
        {
            WriteLine(3, "GetExecutablesFromCurrentSolution-Begin");
            try
            {
                if (dte == null)
                {
                    WriteLine(1, "dte is null (checking for projects is not possible)");
                    return "";
                }
                if (dte.Solution == null)
                {
                    WriteLine(1, "dte.Solution is null (checking for projects is not possible)");
                    return "";
                }
                if (dte.Solution.SolutionBuild == null)
                {
                    WriteLine(1, "dte.Solution.SolutionBuild is null (checking for projects is not possible)");
                    return "";
                }
                if (dte.Solution.SolutionBuild.ActiveConfiguration == null)
                {
                    WriteLine(1, "dte.Solution.SolutionBuild.ActiveConfiguration is null (checking for projects is not possible)");
                    return "";
                }

                string configName = dte.Solution.SolutionBuild.ActiveConfiguration.Name;

                var projects = dte.Solution.Projects;
                if (projects==null)
                {
                    WriteLine(2, "GetExecutablesFromCurrentSolution-End: projects not available");
                    return "";
                }

                List<string> executables = new List<string>();
                foreach (EnvDTE.Project project in projects)
                {
                    if (project != null)
                    {
                        AddProjectRecursive(project, configName, executables);
                    }
                }
                WriteLine(2, "GetExecutablesFromCurrentSolution-End numExecutablesFound=" + executables.Count);
                executables.Sort();
                string executablesAsString = "";
                foreach (string executable in executables)
                {
                    executablesAsString += executable + "\n";
                }
                return executablesAsString;
            }
            catch (Exception ex)
            {
                WriteLine(1, "GetExecutablesFromCurrentSolution-End: EXCEPTION: " + ex.ToString());
                return "";
            }
        }

        private void AddProjectRecursive(EnvDTE.Project project, string configName, List<string> foundExecutables)
        {
            if (project == null)
            {
                return;
            }
            WriteLine(3, "AddProjectRecursive: found project.Name= " + project.Name + " project.Kind=" + project.Kind);

            if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                WriteLine(3, "AddProjectRecursive: found solution item " + project.Name);

                foreach (EnvDTE.ProjectItem item in project.ProjectItems)
                {
                    EnvDTE.Project realProject = item.Object as EnvDTE.Project;

                    if (realProject != null)
                    {
                        AddProjectRecursive(realProject, configName, foundExecutables);
                    }
                }
            }
            else // may be real project
            {
                Microsoft.VisualStudio.VCProjectEngine.VCProject vcProj = (Microsoft.VisualStudio.VCProjectEngine.VCProject)project.Object;
                if (vcProj == null)
                {
                    WriteLine(3, "AddProjectRecursive/may be real project: vcProj == null");
                    return;
                }

                Microsoft.VisualStudio.VCProjectEngine.IVCCollection configs = (Microsoft.VisualStudio.VCProjectEngine.IVCCollection)vcProj.Configurations;
                Microsoft.VisualStudio.VCProjectEngine.VCConfiguration config = FindConfig(configs, configName);
                if (config == null)
                {
                    WriteLine(3, "AddProjectRecursive/may be real project: config == null");
                    return;
                }

                string outputPath = config.PrimaryOutput;
                outputPath.Trim();
                string tmpOutputPath = outputPath.ToLower();
                if ((tmpOutputPath.Length > 0) && (tmpOutputPath.IndexOf(".exe") == tmpOutputPath.Length - 4))
                {
                    WriteLine(3, "AddProjectRecursive: Found project: " + project.Name + " exe: " + outputPath);
                    foundExecutables.Add(outputPath);
                }
                else
                {
                    WriteLine(3, "AddProjectRecursive: Ignored project: " + project.Name + " is no exe: " + outputPath);
                }
            }
        }

        /// [listen to changed startup project]
        // -----  Interface IVsSelectionEvents  -----

        // Check for notification about changed startup project
        int IVsSelectionEvents.OnElementValueChanged(
        uint elementid, object varValueOld, object varValueNew)
        {
            if (elementid == (uint)VSConstants.VSSELELEMID.SEID_StartupProject)
            {
                // When startup project is set in solution explorer a complete refresh is triggered
                if (varValueNew != null)
                {
                    WriteLine(2, "Detected new StartupProject");
                    m_mainEvents.OnRefreshAll();
                }
            }
            return VSConstants.S_OK;
        }

        // All other events from IVsSelectionEvents are ignored.
        // Eventhandler functions simply return OK status.
        int IVsSelectionEvents.OnCmdUIContextChanged(
            uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }
        /// ...
        /// [listen to changed startup project]

        int IVsSelectionEvents.OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }

        /// [listen to switched debug/release configuration]
        // -----  Interface IVsUpdateSolutionEvents  -----

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            WriteLine(2, "Detected OnActiveProjectCfgChange");
            m_mainEvents.OnRefreshAll();

            return VSConstants.S_OK;
        }
        /// [listen to switched debug/release configuration]

        // All other events from IVsSelectionEvents are ignored.
        // Eventhandler functions simply return OK status.
        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        // -----  Private methods  -----

        /// Find a project with a given name within the hierarchical tree of project/solution items
        private EnvDTE.Project[] GetAllProjects(bool recursive)
        {
            var projects = new List<EnvDTE.Project>();
            /// [iterate dte projects]
            foreach (EnvDTE.Project project in dte.Solution.Projects)
            /// [iterate dte projects]
            {
                if (project != null)
                {
                    WriteLine(3, "GetAllProjects: " + project.Name);
                    if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                    {
                        if (recursive)
                        {
                            projects.AddRange(GetProjectsRecursive(project));
                        }
                    }
                    else
                    {
                        projects.Add(project);
                    }
                }
            }
            return projects.ToArray();
        }


        private EnvDTE.ProjectItem[] GetAllProjectItemsFromKind(EnvDTE.Project project, string guid)
        {
            List<EnvDTE.ProjectItem> projectItems = new List<EnvDTE.ProjectItem>();
            foreach (EnvDTE.ProjectItem childProjectItem in project.ProjectItems)
            {
                if (childProjectItem.Kind == guid)
                {
                    projectItems.Add(childProjectItem);
                    //recursive call
                }
                projectItems.AddRange(GetAllProjectItemsFromKind(childProjectItem, guid));
            }

            return projectItems.ToArray();
        }

        private EnvDTE.ProjectItem[] GetAllProjectItemsFromKind(EnvDTE.ProjectItem projectItem, string guid)
        {
            List<EnvDTE.ProjectItem> projectItems = new List<EnvDTE.ProjectItem>();

            if (projectItem.Kind == guid)
            {
                projectItems.Add(projectItem);
            }

            foreach (EnvDTE.ProjectItem childProjectitem in projectItem.ProjectItems)
            {
                if (childProjectitem.Kind == guid)
                {
                    projectItems.Add(childProjectitem);
                }
                else
                {
                    //recursive call
                    projectItems.AddRange(GetAllProjectItemsFromKind(childProjectitem, guid));
                }
            }

            return projectItems.ToArray();
        }

        /// Find a project with a given name within the project items of a given project
        private EnvDTE.Project[] GetProjectsRecursive(EnvDTE.Project project)
        {
            var projects = new List<EnvDTE.Project>();

            if (project == null)
            {
                return null;
            }

            WriteLine(3, "FindProjectRecursive: " + project.Name);

            /// [recursive search]
            foreach (EnvDTE.ProjectItem item in project.ProjectItems)
            {
                EnvDTE.Project realProject = item.Object as EnvDTE.Project;

                if (realProject != null)
                {
                    if (realProject.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                    {
                        projects.AddRange(GetProjectsRecursive(
                            realProject));
                    }
                    else
                    {
                        projects.Add(realProject);
                    }
                }
            }
            /// [recursive search]

            return projects.ToArray();
        }

        /// Find a project with a given name within the project items of a given project
        private EnvDTE.Project FindProjectRecursive(EnvDTE.Project project, string nameProject)
        {
            if (project == null)
            {
                return null;
            }

            WriteLine(3, "FindProjectRecursive: " + project.Name);

            /// [recursive search]
            foreach (EnvDTE.ProjectItem item in project.ProjectItems)
            {
                EnvDTE.Project realProject = item.Object as EnvDTE.Project;

                if (realProject != null)
                {
                    if (realProject.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                    {
                        EnvDTE.Project projectNextLevel = FindProjectRecursive(
                            realProject, nameProject);
                        if (projectNextLevel != null)
                        {
                            return projectNextLevel;
                        }
                    }
                    else if (realProject.Name == nameProject)
                    {
                        return realProject;
                    }
                }
            }
            /// [recursive search]

            return null;
        }

        private void WriteLine(int in_outputLevel, String in_info)
        {
            m_eventReceiver.WriteLine(in_outputLevel, in_info);
        }

        /// [find config]
        private Microsoft.VisualStudio.VCProjectEngine.VCConfiguration FindConfig(Microsoft.VisualStudio.VCProjectEngine.IVCCollection in_configurations, String in_configName)
        {
            WriteLine(3, "FindConfig: searching for config " + in_configName);
            if ((in_configurations == null) || (in_configurations.Count <= 0))
            {
                WriteLine(3, "FindConfig: in_configurations is null or empty");
                return null;
            }

            WriteLine(3, "FindConfig: configurations.Count=" + in_configurations.Count);

            foreach (Microsoft.VisualStudio.VCProjectEngine.VCConfiguration configItem in in_configurations)
            {
                WriteLine(3, "FindConfig: configItem.Name=>" + configItem.Name + "< .ConfigurationName=>" + configItem.ConfigurationName + "<");
                if (configItem.ConfigurationName == in_configName)
                {
                    return configItem;
                }
            }
            return null;
        }
        /// [find config]
    }
}
