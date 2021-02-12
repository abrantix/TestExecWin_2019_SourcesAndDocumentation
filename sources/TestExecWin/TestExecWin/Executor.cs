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

namespace TestExecWin
{
    class Executor : IExecute
    {
        public static EnvDTE80.DTE2 dte;

        private IMainEvents m_mainEvents;
        private IEventReceiver m_evenReceiver;

        /// [exec data]
        // Control data used for executing an external process
        private System.Diagnostics.Process m_process = null;
        private StringBuilder m_processOutputStringBuilder;

        // Output window pane within Visual Studio to be
        // used as stdout for test executable
        private EnvDTE.OutputWindowPane m_outputPane = null;

        // Flag indicating whether stdout and stderr
        // of the started executable shall be redirected and
        // a separate execution window shall be suppressed.
        bool m_catchStdOutAndStdErr = true;

        // Flag indicating whether Executor shall be notified
        // when the process terminates
        bool m_getNotificationWhenProcessTerminates = true;

        // Flag indicating whether after process termination
        // the process output is checked for string "Detected memory leaks"
        private bool m_checkForMemoryLeaks = false;
        /// [exec data]

        public Executor(IMainEvents in_mainEvents, IEventReceiver in_eventReceiver)
        {
            m_mainEvents = in_mainEvents;
            m_evenReceiver = in_eventReceiver;
        }

        public void SetMemLeakCheck(bool in_state)
        {
            m_checkForMemoryLeaks = in_state;
        }

        /// [exec start process]
        public void StartProcess(string exePath, string args, string workDir)
        {
            try
            {
                // Ensure executable exists
                if (!System.IO.File.Exists(exePath))
                {
                    WriteLine(1, "Executable not found: " + exePath);
                    m_mainEvents.OnTestTerminated(Result.Failed, exePath, false, new TimeSpan(0), "Executable not found: " + exePath);
                    return;
                }

                // Ensure output pane exists
                if (m_outputPane == null)
                {
                    EnvDTE.OutputWindow ow = dte.ToolWindows.OutputWindow;
                    m_outputPane = ow.OutputWindowPanes.Add("Run UnitTest");
                }
                m_outputPane.Activate();
                m_outputPane.Clear();

                // -----  Prepare process data  -----
                m_process = new System.Diagnostics.Process();
                m_process.StartInfo.FileName = exePath;
                m_process.StartInfo.WorkingDirectory = workDir;
                m_process.StartInfo.Arguments = args.Trim();
                m_processOutputStringBuilder = new StringBuilder();

                if (m_getNotificationWhenProcessTerminates)
                {
                    // Remark: Method Executor.Process_Exited will be called
                    // from some system thread when the process has terminated.
                    // Synchronization will be needed!
                    m_process.Exited += new System.EventHandler(Process_Exited);
                    m_process.EnableRaisingEvents = true;
                }

                if (m_catchStdOutAndStdErr)
                {
                    m_process.StartInfo.UseShellExecute = false;
                    m_process.StartInfo.RedirectStandardOutput = true;
                    m_process.StartInfo.RedirectStandardError = true;
                    m_process.StartInfo.CreateNoWindow = true;
                    m_process.OutputDataReceived +=  new System.Diagnostics.
                        DataReceivedEventHandler(StandardOutputReceiver);
                    m_process.ErrorDataReceived += new System.Diagnostics.
                        DataReceivedEventHandler(StandardErrorReceiver);
                }

                // -----  Start the new process and start redirection of output  -----
                m_process.Start();
                if (m_catchStdOutAndStdErr)
                {
                    m_process.BeginOutputReadLine();
                    m_process.BeginErrorReadLine();
                }

                WriteLine(2, "Started " + m_process.StartInfo.FileName);
            }
            catch (Exception e)
            {
                string info = "EXCEPTION: Could not start executable " + exePath + " " + e.ToString();
                WriteLine(1, info);
                m_mainEvents.OnTestTerminated(Result.Failed, exePath, false, new TimeSpan(0), info);
            }
        }
        /// [exec start process]

        // Handle Exited event and display process information.
        public void Process_Exited(object sender, System.EventArgs e)
        {
            try
            {
                string info = string.Format("Exit time:    {0}" +
                    " Exit code:    {1}", m_process.ExitTime, m_process.ExitCode);
                TimeSpan executionTime = m_process.ExitTime - m_process.StartTime;
                WriteLine(2, info);
                bool memLeaksDetected = false;
                if (m_checkForMemoryLeaks)
                {
                    WriteLine(3, "Process_Exited: Checking mem leaks...");

                    //System.Threading.Thread.Sleep(500); // wait needed for completion of output?
                    WriteLine(3, "Process_Exited: .. after sleep");
                    var sel = m_processOutputStringBuilder.ToString();
                    WriteLine(3, "Process_Exited: selection acquired");
                    //sel.SelectAll();
                    WriteLine(3, "Process_Exited: all selected");
                    //sel.StartOfDocument(false);
                    //sel.EndOfDocument(true);
                    //System.IO.File.WriteAllText("C:\\MyTemp\\OutputPane.txt", sel.Text);

                    // In case of many memory leaks the output containing "Detected memory leaks" may
                    // no longer be within limited buffer of output pane. Therefore check also for
                    // final message "Object dump complete".
                    if ((sel.Contains("Detected memory leaks")) || (sel.Contains("Object dump complete")))
                    {
                        memLeaksDetected = true;
                        WriteLine(3, "Process_Exited: ERROR: Memory leaks detected!");
                    }
                }
                m_mainEvents.OnTestTerminated(m_process.ExitCode == 0 ? Result.Success : Result.Failed, m_process.StartInfo.Arguments, memLeaksDetected, executionTime, m_processOutputStringBuilder.ToString());
            }
            catch (Exception ex)
            {
                string info = "EXCEPTION within Process_Exited: "+ ex.ToString();
                WriteLine(1, info);
                m_mainEvents.OnTestTerminated(Result.Failed, m_process.StartInfo.Arguments, false, new TimeSpan(0), info);
            }
        }

        public void KillRunningProcess()
        {
            try
            {
                if (m_process == null)
                    return;

                WriteLine(1, "Killing " + m_process.ProcessName);
                /// [exec kill process]
                m_process.Kill();
                /// [exec kill process]
            }
            catch (Exception e)
            {
                string info = "EXCEPTION: " + e.ToString();
                WriteLine(2, info);
            }
        }

        /// [exec redirect stdout]
        private void StandardOutputReceiver(object sendingProcess,
            System.Diagnostics.DataReceivedEventArgs outLine)
        {
            // Receives the child process' standard output
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                m_processOutputStringBuilder.AppendLine(outLine.Data);
                /*if (m_outputPane != null)
                    m_outputPane.OutputString(outLine.Data + System.Environment.NewLine);*/
            }
        }
        /// [exec redirect stdout]

        private void StandardErrorReceiver(object sendingProcess,
            System.Diagnostics.DataReceivedEventArgs errLine)
        {
            // Receives the child process' standard error
            if (!string.IsNullOrEmpty(errLine.Data))
            {
                m_processOutputStringBuilder.AppendFormat("Error> {0}\n", errLine.Data);
                /*if (m_outputPane != null)
                    m_outputPane.OutputString("Error> " + errLine.Data + System.Environment.NewLine);*/
            }
        }

        private void WriteLine(int in_outputLevel, String in_info)
        {
            m_evenReceiver.WriteLine(in_outputLevel, in_info);
        }
    }
}
