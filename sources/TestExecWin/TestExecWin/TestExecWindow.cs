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

namespace TestExecWin
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    public enum AppType { NO_TEST_FOUND, TTB, BOOST };

    public interface IMainEvents
    {
        void OnRefreshAll();
        void OnRefreshNow();
        void OnSetOutputLevel(int in_level);
        void OnTestTerminated(Result result, string in_args, bool in_memLeaksDetected, TimeSpan in_executionTime, string processOutput);
        void OnStartDebugging(string in_cmdLineParams);
        void OnOpenProtocolFile();
        void OnOpenSourceFile(string in_fullFileName, int in_lineNum);
        string OnGetExecutablesFromCurrentSolution();
    }

    public interface IExecute
    {
        void SetMemLeakCheck(bool in_state);
        void StartProcess(string exePath, string args, string workDir);
        void KillRunningProcess();
    }

    public interface IEventReceiver
    {
        void WriteLine(int in_outputLevel, String in_info);
        void MsgBox(String in_info);
    }

    public class TestGroupEntry
    {
        public bool IsBoostGroup { get; set; }
        public string FileFullPath { get; set; }
        public int LineNum { get; set; }
        public System.Collections.Generic.List<TestFuncEntry> testFuncs;
        public NodeList<TestGroupEntry> NodeList { get; set; }
        public string Name { get; set; }

        public TestGroupEntry(bool in_isBoostGroup, string name)
        {
            IsBoostGroup = in_isBoostGroup;
            testFuncs = new System.Collections.Generic.List<TestFuncEntry>();
            Name = name;
        }

        public string GetCmdString()
        {
            string cmdString = GetTestGroupHierarchyString();
            return "--run_test=" + cmdString;
        }

        public override string ToString()
        {
            return Name;
        }

        public string GetTestGroupHierarchyString()
        {
            return NodeList.GetPath();
            /*string allTestGroups = "";
            foreach (string grp in testGroups)
            {
                if (allTestGroups != "")
                {
                    allTestGroups += "/";
                }
                allTestGroups += grp;
            }
            return allTestGroups;*/
        }
    }

    public class TestFuncEntry
    {
        public bool IsBoostFunc { get; set; }
        public string FileFullPath { get; set; }
        public int LineNum { get; set; }
        public string TestFunction { get; set; }
        public bool IsTestDisabled { get; set; }
        public TestGroupEntry TestGroup { get; set; }

        public TestFuncEntry(bool in_isBoostFunc, TestGroupEntry testGroup, bool isTestDisabled)
        {
            IsBoostFunc = in_isBoostFunc;
            TestFunction = string.Empty;
            FileFullPath = "";
            LineNum = 0;
            IsTestDisabled = isTestDisabled;
            TestGroup = testGroup;
        }

        public string GetDisplayString()
        {
            return TestFunction;
        }

        public string GetCmdString()
        {
            if (IsBoostFunc)
            {
                string cmdString = TestGroup.GetTestGroupHierarchyString();
                cmdString += TestFunction;
                return "--run_test=" + cmdString;
            }
            else // TTB
            {
                return "-selectTestFunc " + TestFunction;
            }
        }
    }

    public class Project
    {
        public EnvDTE.Project DTEProject { get; set; }
        public string ProjectName { get; set; }
        public string SourceDirPath { get; set; }
        public string FullExePath { get; set; }
        public string TargetDirPath { get; set; }
        public bool IsStartupProject { get; set; }
        public AppType AppType { get; set; }
        public NodeList<TestGroupEntry> TestGroups { get; private set; }

        public Project()
        {
            SourceDirPath = "<not set>";
            TargetDirPath = "<not set>";
            FullExePath = "<not set>";
            IsStartupProject = false;
            AppType = AppType.NO_TEST_FOUND;
            var rootGroupEntry = new TestGroupEntry(false, string.Empty);
            TestGroups = new NodeList<TestGroupEntry>(rootGroupEntry);
            rootGroupEntry.NodeList = TestGroups;
        }
    }

    public class ProjectInfo
    {
        public string solutionFullPath { get; set; }
        public string config { get; set; }
        public System.Collections.Generic.List<Project> Projects { get; private set; }

        public Project SelectedProject { get; set; }

        public ProjectInfo()
        {
            solutionFullPath = "<not set>";
            config = "<not set>";
            Projects = new System.Collections.Generic.List<Project>();
            SelectedProject = new Project();
        }

        public void AddProject(Project project)
        {
            Projects.Add(project);
        }
        public string GetExePath()
        {
            return SelectedProject.FullExePath;
        }
    }
    class Services
    {
        public static string GetThreadIdPrompt()
        {
            int tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
            string msg = "TID " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString() + ": ";
            return msg;
        }

    };

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("7c0e601f-3680-4cfd-b2bb-b774e3119f4b")]
    public class TestExecWindow : ToolWindowPane, IEventReceiver, IMainEvents
    {
        // Helper objects with specific tasks
        private VisualStudioConnector m_vsConnector;
        private SourceFileParser m_parser;
        private Executor m_executor;

        private int m_outputLevel = 2;
        private bool m_writeThreadId = false;
        private ProjectInfo m_projectInfo = new ProjectInfo();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestExecWindow"/> class.
        /// </summary>
        public TestExecWindow() : base(null)
        {
            m_executor = new Executor(this, this);

            this.Caption = "TestExecWindow";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new TestExecWindowControl(this, this, m_executor);

            m_vsConnector = new VisualStudioConnector(this, this);
            m_parser = new SourceFileParser(this);
            RefreshAll();
        }

        //}

        protected override void Dispose(bool disposing)
        {
            m_vsConnector.DisconnectFromVisualStudio();
            base.Dispose(disposing);
        }

        private void RefreshAll()
        {
            WriteLine(3, "RefreshAll-Begin");

            try
            {
                /*// Read from startup project
                m_projectInfo = new ProjectInfo();
                if (!m_vsConnector.ReadSettingsOfStartupProject(m_projectInfo) || m_projectInfo.Projects.Count == 0)
                {
                    WriteLine(2,"WARNING: There is no startup project defined or the project settings are not accessible!");
                    return;
                }
                m_projectInfo.SelectedProject = m_projectInfo.Projects[0];
                m_parser.ScanProjectDir(m_projectInfo.SelectedProject);
                */

                m_projectInfo = m_vsConnector.ReadSettingsOfAllProjects();
                if (m_projectInfo.Projects.Count == 0)
                {
                    WriteLine(2, "WARNING: There is no project defined or the project settings are not accessible!");
                    return;
                }

                foreach (var project in m_projectInfo.Projects)
                {
                    m_parser.ScanProjectDir(project);
                }

                m_projectInfo.SelectedProject = m_projectInfo.Projects.FirstOrDefault(x => x.AppType == AppType.BOOST);

                if (m_projectInfo.SelectedProject == null)
                {
                    WriteLine(2, "WARNING: There is no project with BOOST or the project settings are not accessible!");
                    return;
                }


                Gui().SetTestInfo(m_projectInfo);
            }
            catch (Exception e)
            {
                string info = "EXCEPTION: " + e.ToString();
                WriteLine(1, info);
            }

            WriteLine(3, "RefreshAll-End");
        }

        public void WriteLine(int in_outputLevel, String in_info)
        {
            if (in_outputLevel <= m_outputLevel)
            {
                if (m_writeThreadId)
                {
                    Gui().AddInfoToEventList(Services.GetThreadIdPrompt() + in_info);
                }
                else
                {
                    Gui().AddInfoToEventList(in_info);
                }
            }
        }

        TestExecWindowControl Gui()
        {
            return (TestExecWindowControl)this.Content;
        }

        // Someone wants to refresh
        public void OnRefreshAll()
        {
            // Synchronize via GUI thread
            Gui().TriggerDelayedRefresh();
        }

        // After synchronizing GUI calls this method
        public void OnRefreshNow()
        {
            // If test is still running we do not want to remove current data
            if (Gui().TestIsRunning())
            {
                string info = "WARNING: Ignoring change of startup project while test is running";
                WriteLine(1, info);
                return;
            }

            // We assume to be within safe GUI thread
            RefreshAll();
        }

        public void OnSetOutputLevel(int in_level)
        {
            m_outputLevel = in_level;
            m_writeThreadId = m_outputLevel >= 2;
        }

        /// [test terminated]
        public void OnTestTerminated(Result result, string in_args, bool in_memLeaksDetected, TimeSpan in_executionTime, string processOutput)
        {
            // This function is called when the test app terminates.
            // The call may arrive on any system thread and needs to be synchronized.
            // Simply pass all data to GUI where the main GUI thread (dispatcher thread)
            // can be used for synchronization.
            Gui().TestTerminated(result, in_args, in_memLeaksDetected, in_executionTime, processOutput);
        }
        /// [test terminated]

        public void OnStartDebugging(string in_cmdLineParams)
        {
            m_vsConnector.StartDebugging(m_projectInfo, in_cmdLineParams);
        }

        public void OnOpenProtocolFile()
        {
            string protocolFilePath = m_projectInfo.GetExePath();
            protocolFilePath = protocolFilePath.Replace(".exe", ".out");
            if (!System.IO.File.Exists(protocolFilePath))
            {
                MsgBox("Test protocol not found: " + protocolFilePath);
            }
            else
            {
                m_vsConnector.OpenFile(protocolFilePath);
            }
        }

        public void OnOpenSourceFile(string in_fullFileName, int in_lineNum)
        {
            if (!System.IO.File.Exists(in_fullFileName))
            {
                MsgBox("Sourcefile not found: " + in_fullFileName);
            }
            else
            {
                m_vsConnector.OpenFile(in_fullFileName, in_lineNum);
            }
        }

        public string OnGetExecutablesFromCurrentSolution()
        {
            return m_vsConnector.GetExecutablesFromCurrentSolution();
        }

        public void MsgBox(string in_info)
        {
            System.Windows.MessageBox.Show(in_info, "Info");
        }
    }
}
