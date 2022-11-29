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
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using System.Threading;
using Task = System.Threading.Tasks.Task;


namespace TestExecWin
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.3", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(TestExecWindow))]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(TestExecWindowPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    /// [package base class]
    public sealed class TestExecWindowPackage : ToolkitPackage
    /// [package base class]
    {
        /// [dte data]
        private EnvDTE80.DTE2 dte; // access to the DTE object
        private DteInitializer dteInitializer; // supports delayed DTE initialization
        /// [dte data]

        /// <summary>
        /// TestExecWindowPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "452b9afb-c2c1-462b-885d-5e6986f6aad5";

        /// <summary>
        /// Initializes a new instance of the <see cref="TestExecWindow"/> class.
        /// </summary>
        public TestExecWindowPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        /// [InitializeDTE]
        private async Task InitializeDTEAsync()
        {
            Debug.WriteLine("InitializeDTE-Begin");

            // Request access to DTE object
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            this.dte = await this.GetServiceAsync(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE))
                as EnvDTE80.DTE2;
            Microsoft.Assumes.Present(dte);

            // Pass DTE reference to static data members of other classes
            // which also need access to DTE
            Executor.dte = this.dte;
            VisualStudioConnector.dte = this.dte;

            Debug.WriteLine("InitializeDTE-End");
        }
        /// [InitializeDTE]

        #region Package Members

        /// [calling InitializeDTE]
        /// Initialization of the package; this method is called asynchronously by Visual Studio.
        /*protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<Microsoft.VisualStudio.Shell.ServiceProgressData> progress)
        {
            // Do something on background task
            // ...
            await Task.Delay(5000);

            // Now switch to UI thread

            // Switches to the UI thread in order to consume some services used in command initialization
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Synchronous initialization on UI thread
            TestExecWindowCommand.Initialize(this);
            base.Initialize();
            InitializeDTE();
        }*/

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();
            this.RegisterToolWindows(); // => calls MyToolWindow constructor
        
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            TestExecWindowCommand.Initialize(this);
            await InitializeDTEAsync();
            //await MentiToolWindowCommand.InitializeAsync(this);
        }
        /// [calling InitializeDTE]

        // old synchronous way of calling:
        //protected override void Initialize()
        //{
        //    TestExecWindowCommand.Initialize(this);
        //    base.Initialize();
        //    InitializeDTE();
        //}

        #endregion
    }

    internal class DteInitializer : IVsShellPropertyEvents
    {
        private IVsShell shellService;
        private uint cookie;
        private Action callback;

        internal DteInitializer(IVsShell shellService, Action callback)
        {
            int hr;

            this.shellService = shellService;
            this.callback = callback;

            // Set an event handler to detect when the IDE is fully initialized
            hr = this.shellService.AdviseShellPropertyChanges(this, out this.cookie);

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
        }

        int IVsShellPropertyEvents.OnShellPropertyChange(int propid, object var)
        {
            int hr;
            bool isZombie;

            if (propid == (int)__VSSPROPID.VSSPROPID_Zombie)
            {
                isZombie = (bool)var;

                if (!isZombie)
                {
                    // Release the event handler to detect when the IDE is fully initialized
                    hr = this.shellService.UnadviseShellPropertyChanges(this.cookie);

                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);

                    this.cookie = 0;

                    // Trigger initialization of DTE within TestExecWindowPackage.InitializeDTE
                    this.callback();
                }
            }
            return VSConstants.S_OK;
        }
    }
}
