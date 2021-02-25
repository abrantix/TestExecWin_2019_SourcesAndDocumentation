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
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Windows.Input;
    using System.ComponentModel;
    using System.Windows.Data;
    using System.Threading.Tasks;

    class MyListItem
    {
        public string displayString;
        public int idx;

        public MyListItem(int in_idx, string in_displayString)
        {
            idx = in_idx;
            displayString = in_displayString;
        }

        public override string ToString()
        {
            return displayString;
        }
    };

    /// <summary>
    /// Interaction logic for TestExecWindowControl.
    /// </summary>
    public partial class TestExecWindowControl : UserControl
    {
        private ProjectInfo m_projectInfo = new ProjectInfo();
        private IMainEvents m_mainEvents;
        private IEventReceiver m_evenReceiver;
        private IExecute m_executor;

        enum RunMode { TEST_FUNCS, TEST_ALL };
        private RunMode m_curRunMode = RunMode.TEST_ALL;
        private int[] m_numExecutedTests = new int[] { 0, 0, 0, 0 };
        private int[] m_numFailedTests = new int[] { 0, 0, 0, 0 };
        private int[] m_numDisabledTests = new int[] { 0, 0, 0, 0 };
        private int[] m_totalNumTestsToExecute = new int[] { 0, 0, 0, 0 };
        private bool m_stoppedByUser = false;
        private System.Windows.Forms.Timer m_executionTimer = new System.Windows.Forms.Timer();
        enum WaitMode { WAIT_ENDLESS, WAIT_30_SEC, WAIT_1_MIN, WAIT_2_MIN, WAIT_5_MIN, WAIT_10_MIN, WAIT_20_MIN, WAIT_30_MIN, WAIT_40_MIN, WAIT_60_MIN };
        private WaitMode m_waitMode = (WaitMode)Properties.Settings.Default.IdxMaxWaitTime;

        private TestFunctionTreeViewItem[] TestFunctionsToExecute;
        TestTreeViewItem rootTestTreeViewItem = new TestTreeViewItem(null, new TestGroupEntry(true, string.Empty));

        private static RoutedUICommand m_showSourceCommand = new RoutedUICommand("Show Shource", "ShowSource", typeof(TestExecWindowControl));
        private static RoutedUICommand m_debugCommand = new RoutedUICommand("Debug", "Debug", typeof(TestExecWindowControl));
        public static RoutedUICommand ShowSourceCommand
        {
            get { return m_showSourceCommand; }
        }
        public static RoutedUICommand DebugCommand
        {
            get { return m_debugCommand; }
        }

        private bool IsLastSelectedItemATestFunctionTreeViewItem()
        {
            var fctCtx = testTreeView.LastSelectedItem as TestFunctionTreeViewItem;
            return fctCtx != null;
        }

        private void ShowSource_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsLastSelectedItemATestFunctionTreeViewItem();
        }

        private void Debug_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsLastSelectedItemATestFunctionTreeViewItem();
        }

        private void ShowSource_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var fctCtx = testTreeView.LastSelectedItem as TestFunctionTreeViewItem;
            if (fctCtx != null)
            {
                ShowSourceCodeFile(fctCtx.TestFuncEntry.FileFullPath, fctCtx.TestFuncEntry.LineNum);
            }
            else
            {
                MessageBox.Show("Groups are not supported, sorry.");
            }
        }

        private void Debug_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var fctCtx = testTreeView.LastSelectedItem as TestFunctionTreeViewItem;
            if (fctCtx != null)
            {
                m_mainEvents.OnStartDebugging(fctCtx.TestFuncEntry.GetCmdString() + " " + cbxDefaultArgs.Text);
            }
            else
            {
                MessageBox.Show("Groups are not supported, sorry.");
            }
        }

        private void InitForTestExecution(RunMode in_runMode, int in_numTestsToExecute)
        {
            m_stoppedByUser = false;
            m_idxRunSelectedTestFuncs = -1;
            m_curRunMode = in_runMode;
            int idx = (int)m_curRunMode;
            m_numExecutedTests[idx] = 0;
            m_numFailedTests[idx] = 0;
            m_numDisabledTests[idx] = 0;
            m_totalNumTestsToExecute[idx] = in_numTestsToExecute;
            m_state[idx] = State.RUNNING;
        }

        enum ColVisibility { BOTH, LEFT, RIGHT };
        ColVisibility m_colVisibility = (ColVisibility)Properties.Settings.Default.IdxGroupsAndFuncs;

        enum SortOrder { AS_READ_FROM_FILE, REVERSE_READ, ALPHA, REVERSE_ALPHA };
        bool m_optionsVisible = Properties.Settings.Default.OptionsAreVisible;
        bool m_logVisible = Properties.Settings.Default.LogIsVisible;
        SortOrder m_sortOrder = (SortOrder)Properties.Settings.Default.IdxSortOrder;

        enum State { IDLE, RUNNING, SUCCEEDED, FAILED };
        private State[] m_state = new State[] { State.IDLE, State.IDLE, State.IDLE, State.IDLE };

        private int m_idxRunSelectedTestFuncs = -1;

        enum ExpImpMode { EXPORT_TO_FILE = 1, IMPPORT_FROM_FILE, IMPORT_FROM_SOLUTION, IMPORT_TEST_GROUPS_FROM_STARTUP_PROJECT, IMPORT_VISIBLE_TEST_FUNCS_FROM_STARTUP_PROJECT };
        private List<string> m_currentTestApps = new List<string>();
        private List<string> m_dataDescTestApps;
        private int m_idxCurrentTestApps = Properties.Settings.Default.IdxCurrentTestApps;

        private System.Windows.Media.Brush m_originalBrushWindow;
        private System.Windows.Threading.DispatcherTimer m_refreshTimer = new System.Windows.Threading.DispatcherTimer();
        private bool m_refreshTimerIsActive = false;

        public TestExecWindowControl()
            : base()
        {
            this.InitializeComponent();

            m_executionTimer.Enabled = false;
            m_executionTimer.Tick += new EventHandler(OnExecutionTimeout);
            for (int i = 0; i < 4; ++i)
            {
                m_state[i] = State.IDLE;
            }
            m_originalBrushWindow = mainGrid.Background;
            {
                List<string> data = new List<string>();
                data.Add("sort as read");
                data.Add("reverse read order");
                data.Add("sort A->Z");
                data.Add("sort Z->A");

                cbxSortOrder.ItemsSource = data;
                cbxSortOrder.SelectedIndex = (int)m_sortOrder;
            }
            {
                List<string> data = new List<string>();
                data.Add("show log");
                data.Add("hide log");

                cbxLogVisibility.ItemsSource = data;
                cbxLogVisibility.SelectedIndex = m_logVisible ? 0 : 1;
            }
            {
                List<string> data = new List<string>();
                data.Add("regular output");
                data.Add("verbous output");
                data.Add("detailed output");

                int outputLevel = Properties.Settings.Default.IdxOutputLevel + 1;
                cbxOutputLevel.ItemsSource = data;
                cbxOutputLevel.SelectedIndex = outputLevel - 1;
            }
            {
                List<string> data = new List<string>();
                data.Add("leak check");
                data.Add("no leak check");

                cbxMemLeakCheck.ItemsSource = data;
                bool leakCheck = Properties.Settings.Default.CheckForMemoryLeaks;
                cbxMemLeakCheck.SelectedIndex = leakCheck ? 0 : 1;
            }
            {
                List<string> data = new List<string>();
                data.Add("wait for test app without time limit");
                data.Add("wait for test app max 30 sec");
                data.Add("wait for test app max 1 min");
                data.Add("wait for test app max 2 min");
                data.Add("wait for test app max 5 min");
                data.Add("wait for test app max 10 min");
                data.Add("wait for test app max 20 min");
                data.Add("wait for test app max 30 min");
                data.Add("wait for test app max 40 min");
                data.Add("wait for test app max 60 min");

                cbxMaxExecutionTime.ItemsSource = data;
                cbxMaxExecutionTime.SelectedIndex = (int)m_waitMode;
            }

            {
                List<string> data = new List<string>();
                data.Add("");
                data.Add("BOOST options:");
                data.Add("--detect_memory_leak=0");
                data.Add("--log_level=test_suite");
                data.Add("");
                data.Add("TTB Runner options:");
                data.Add("-- -checkForMemLeaks OFF");
                data.Add("-- -help");
                data.Add("");
                data.Add("custom options:");
                data.Add("-- -prefixEvents TTB_EXP");

                cbxDefaultArgs.ItemsSource = data;
                cbxDefaultArgs.SelectedIndex = 0;
                cbxDefaultArgs.IsEditable = true;
                cbxDefaultArgs.Text = Properties.Settings.Default.GeneralCmdLineArgs;
            }
            {
                m_dataDescTestApps = new List<string>();
                m_dataDescTestApps.Add(Properties.Settings.Default.DescTestApps1);
                m_dataDescTestApps.Add(Properties.Settings.Default.DescTestApps2);
                m_dataDescTestApps.Add(Properties.Settings.Default.DescTestApps3);
                m_dataDescTestApps.Add(Properties.Settings.Default.DescTestApps4);
                m_dataDescTestApps.Add(Properties.Settings.Default.DescTestApps5);
                m_dataDescTestApps.Add(Properties.Settings.Default.DescTestApps6);
                m_dataDescTestApps.Add(Properties.Settings.Default.DescTestApps7);
                m_dataDescTestApps.Add(Properties.Settings.Default.DescTestApps8);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestExecWindowControl"/> class.
        /// </summary>
        public TestExecWindowControl(IMainEvents in_mainEvents, IEventReceiver in_eventReceiver, IExecute in_executor)
            : this()
        {
            m_mainEvents = in_mainEvents;
            m_evenReceiver = in_eventReceiver;
            m_executor = in_executor;

            m_mainEvents.OnSetOutputLevel(Properties.Settings.Default.IdxOutputLevel + 1);
            m_executor.SetMemLeakCheck(Properties.Settings.Default.CheckForMemoryLeaks);
        }

        private void btnMoreOptions_Click(object sender, RoutedEventArgs e)
        {
            m_optionsVisible = !m_optionsVisible;
            Properties.Settings.Default.OptionsAreVisible = m_optionsVisible;
            Properties.Settings.Default.Save();
            RefreshDisplay();
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]

        private async Task RunSelectedTestFunctions()
        {
            for (int i = 0; i < 4; ++i)
            {
                m_state[i] = State.IDLE;
                m_numFailedTests[i] = 0;
                m_numDisabledTests[i] = 0;
                m_numExecutedTests[i] = 0;
                m_totalNumTestsToExecute[i] = 0;
            }

            await m_mainEvents.OnStartRunTestsAsync();

            var fctList = new List<TestFunctionTreeViewItem>();
            foreach (var selectedItem in testTreeView.SelectedItems)
            {
                if (selectedItem is TestFunctionTreeViewItem tfi)
                {
                    fctList.Add(tfi);
                }
                else if (selectedItem is TestTreeViewItem tfg)
                {
                    fctList.AddRange(tfg.GetOverallTestFunctions());
                }
            }
            TestFunctionsToExecute = fctList.Distinct().ToArray();

            //clear test results
            TestFunctionsToExecute.ToList().ForEach(x =>
            {
                x.TestResult.Result = Result.Tentative;
                x.TestResult.ProcessOutput = string.Empty;
                x.TreeViewParent.TestResult.Result = Result.Tentative;
                x.TreeViewParent.TestResult.ProcessOutput = string.Empty;
            });

            if (TestFunctionsToExecute.Length > 0)
            {
                InitForTestExecution(RunMode.TEST_FUNCS, TestFunctionsToExecute.Length);
                m_idxRunSelectedTestFuncs = 0;
                StartTestFunc(0);
            }
        }

        private async void btnRunSelectedTestFunc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (m_state[(int)RunMode.TEST_FUNCS] == State.RUNNING)
                {
                    StopExecution();
                }
                else
                {
                   await RunSelectedTestFunctions();
                }
            }
            catch (Exception ex)
            {
                WriteLine(1, "EXCEPTION: " + ex.ToString());
            }
        }

        private async void btnRunAllTestFuncs_Click(object sender, RoutedEventArgs e)
        {
            await RunAllTests(false);
        }

        private async void btnDebugAllTestFuncs_Click(object sender, RoutedEventArgs e)
        {
            await RunAllTests(true);
        }

        private async Task RunAllTests(bool debug)
        {
            try
            {
                if (m_state[(int)RunMode.TEST_FUNCS] == State.RUNNING)
                {
                    StopExecution();
                }
                else
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        m_state[i] = State.IDLE;
                        m_numFailedTests[i] = 0;
                        m_numDisabledTests[i] = 0;
                        m_numExecutedTests[i] = 0;
                        m_totalNumTestsToExecute[i] = 0;
                    }

                    if (rootTestTreeViewItem.TreeViewItems.Count > 0)
                    {
                        //clear test results
                        rootTestTreeViewItem.TestResult.Result = Result.Tentative;
                        rootTestTreeViewItem.TestResult.ProcessOutput = string.Empty;
                        rootTestTreeViewItem.TreeViewItems.First().GetOverallChildItems().ForEach(x =>
                        {
                            x.TestResult.Result = Result.Tentative;
                            x.TestResult.ProcessOutput = string.Empty;
                        });

                        InitForTestExecution(RunMode.TEST_ALL, rootTestTreeViewItem.OverallTestFunctionCount);
                        await StartAllTests(debug);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLine(1, "EXCEPTION: " + ex.ToString());
            }
        }

        private void btnShowHideLog_Click(object sender, RoutedEventArgs e)
        {
            m_logVisible = !m_logVisible;
            RefreshDisplay();
        }

        private void btnClearEvents_Click(object sender, RoutedEventArgs e)
        {
            lstEvents.Items.Clear();
        }

        private void btnOpenProtocolFile_Click(object sender, RoutedEventArgs e)
        {
            m_mainEvents.OnOpenProtocolFile();
        }

        private void btnRefreshAll_Click(object sender, RoutedEventArgs e)
        {
            m_mainEvents.OnRefreshNow();
        }

        private void btnCancelExecution_Click(object sender, RoutedEventArgs e)
        {
            StopExecution();
        }

        private void cbxSortOrder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            m_sortOrder = (SortOrder)comboBox.SelectedIndex;
            Properties.Settings.Default.IdxSortOrder = comboBox.SelectedIndex;
            Properties.Settings.Default.Save();
            RefreshTestGroupList();
        }

        private void cbxLogVisibility_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            m_logVisible = (comboBox.SelectedIndex == 0);
            Properties.Settings.Default.LogIsVisible = m_logVisible;
            Properties.Settings.Default.Save();
            RefreshDisplay();
        }

        private void cbxOutputLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            Properties.Settings.Default.IdxOutputLevel = comboBox.SelectedIndex;
            Properties.Settings.Default.Save();
            m_mainEvents?.OnSetOutputLevel(comboBox.SelectedIndex + 1);
        }

        private void cbxMemLeakCheck_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            bool leakCheck = (comboBox.SelectedIndex == 0);
            Properties.Settings.Default.CheckForMemoryLeaks = leakCheck;
            Properties.Settings.Default.Save();
            m_executor?.SetMemLeakCheck(leakCheck);
        }

        private void cbxMaxExecutionTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            m_waitMode = (WaitMode)comboBox.SelectedIndex;
            Properties.Settings.Default.IdxMaxWaitTime = comboBox.SelectedIndex;
            Properties.Settings.Default.Save();
        }


        private void chkTestFuncsForSelTestGroup_CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO?? RefreshTestFuncList();
            }
            catch (Exception ex)
            {
                WriteLine(1, "EXCEPTION: " + ex.ToString());
            }

        }

        private string GetInfoAboutCurrentProgress(RunMode in_runMode)
        {
            int idx = (int)in_runMode;
            if (m_state[idx] == State.RUNNING)
            {
                return $" ({m_numExecutedTests[idx]} of {m_totalNumTestsToExecute[idx]}) ({m_numDisabledTests[idx]} disabled)";
            }
            return "";
        }

        private string GetInfoAboutSucceededTest(RunMode in_runMode)
        {
            int idx = (int)in_runMode;
            if (m_totalNumTestsToExecute[idx] > 1)
            {
                return $" ({m_numExecutedTests[idx]} of {m_numExecutedTests[idx]}) ({m_numDisabledTests[idx]} disabled)";
            }
            return "";
        }

        private string GetInfoAboutFailedTest(RunMode in_runMode)
        {
            int idx = (int)in_runMode;
            if (m_totalNumTestsToExecute[idx] > 1)
            {
                return $" ({m_numFailedTests[idx]} of {m_numExecutedTests[idx]}) ({m_numDisabledTests[idx]} disabled)";
            }
            return "";
        }


        private string GetInfoAboutCurrentTest()
        {
            return string.Empty;
        }

        /// [dispatch test terminate msg]
        public void TestTerminated(Result result, string in_args, bool in_memLeaksDetected, TimeSpan in_executionTime, string processOutput)
        {
            // For safe updating of status data process within GUI thread
            this.Dispatcher.Invoke(new Action(() =>
                TestTerminatedWithinGuiThread(result, "", in_args, in_memLeaksDetected, in_executionTime, processOutput)));
        }

        private void TestTerminatedWithinGuiThread(Result result, string infoAboutTest, string in_args, bool in_memLeaksDetected, TimeSpan in_executionTime, string processOutput)
        {
            m_executionTimer.Stop();

            // Display execution status
            string info = in_args;
            if (info == "")
            {
                info = "<no args set>";
            }

            string durationStr;
            if (in_executionTime.TotalSeconds < 1.0)
            {
                durationStr = "<1s ";
            }
            else
            {
                double totalNumMinutes = in_executionTime.TotalSeconds / 60.0;
                durationStr = totalNumMinutes.ToString("F1") + "m ";
            }

            WriteLine(1, (result == Result.Failed ? "FAILED: " : (in_memLeaksDetected ? "FAILED (MEM_LEAKS): " : "OK: "))
                + durationStr + GetInfoAboutCurrentTest() + " " + info);
            bool failure = result == Result.Failed || in_memLeaksDetected;

            int idx = (int)m_curRunMode;
            ++m_numExecutedTests[idx];
            if (failure)
            {
                ++m_numFailedTests[idx];
            }

            if (result == Result.Disabled)
            {
                ++m_numDisabledTests[idx];
            }

            var testResult = new TestResult() { Result = result, MemLeaksDetected = in_memLeaksDetected, ExecutionTime = in_executionTime };
            if (m_idxRunSelectedTestFuncs >= 0)
            {
                TestFunctionsToExecute[m_idxRunSelectedTestFuncs].TestResult = testResult;
                TestFunctionsToExecute[m_idxRunSelectedTestFuncs].TestResult.ProcessOutput = processOutput;
                TestFunctionsToExecute[m_idxRunSelectedTestFuncs].GetAllAncestors().ForEach(p => p.ReflectTestResultsFromChilds());
            }
            else
            {
                //Run all
                rootTestTreeViewItem.TreeViewItems.First().TestResult = testResult;
                rootTestTreeViewItem.TreeViewItems.First().TestResult.ProcessOutput = processOutput;
                rootTestTreeViewItem.TreeViewItems.First().ReflectTestResultsFromChilds();
            }

            bool testSucceededUpToNow = (m_numFailedTests[idx] == 0);
            // Safe update of state variables and refresh
            // of GUI controls within GUI thread
            SetState(testSucceededUpToNow);
            /// ...
            /// [dispatch test terminate msg]

            if (!m_stoppedByUser)
            {
                if (m_idxRunSelectedTestFuncs >= 0)
                {
                    ++m_idxRunSelectedTestFuncs;
                    if (m_idxRunSelectedTestFuncs >= TestFunctionsToExecute.Length)
                    {
                        m_idxRunSelectedTestFuncs = -1;
                    }
                    else // start next test
                    {
                        StartTestFunc(m_idxRunSelectedTestFuncs);
                    }
                }
            }
        }

        private async Task StartAllTests(bool debug)
        {
            string args = cbxDefaultArgs.Text;
            if (!args.Contains("--log_level"))
            {
                args += " --log_level=test_suite";
            }
            if (debug)
            {
                m_mainEvents.OnStartDebugging(m_projectInfo.GetExePath() + " " + args);
            }
            else
            {
                m_mainEvents.OnRefreshNow();
                m_state[(int)m_curRunMode] = State.RUNNING;
                RefreshState();
                await m_mainEvents.OnStartRunTestsAsync();
                StartProcess(m_projectInfo.GetExePath(), args, m_projectInfo.SelectedProject.TargetDirPath, true);
            }
        }

        private void StartTestFunc(int in_idx)
        {
            m_state[(int)m_curRunMode] = State.RUNNING;
            RefreshState();
            if (TestFunctionsToExecute[in_idx].TestFuncEntry.IsTestDisabled)
            {
                m_mainEvents.OnTestTerminated(Result.Disabled, string.Empty, false, new TimeSpan(0), "Test is disabled");
            }
            else
            {
                StartProcess(m_projectInfo.GetExePath(), TestFunctionsToExecute[in_idx].TestFuncEntry.GetCmdString() + " " + cbxDefaultArgs.Text, m_projectInfo.SelectedProject.TargetDirPath, false);
            }
        }

        private void SetState(bool in_success)
        {
            if (m_state[(int)m_curRunMode] == State.RUNNING)
            {
                m_state[(int)m_curRunMode] = (in_success ? State.SUCCEEDED : State.FAILED);
            }
            RefreshState();
        }

        private void RefreshState(RunMode in_runMode, TextBlock tb, string info, bool setText = true)
        {
            switch (m_state[(int)in_runMode])
            {
                case State.IDLE:
                    tb.Foreground = System.Windows.Media.Brushes.Black;
                    if (setText)
                    {
                        tb.Text = info;
                    }
                    break;
                case State.RUNNING:
                    tb.Background = System.Windows.Media.Brushes.Orange;
                    tb.Foreground = System.Windows.Media.Brushes.Black;
                    if (setText)
                    {
                        tb.Text = "Running" + GetInfoAboutCurrentProgress(in_runMode);
                    }
                    break;
                case State.SUCCEEDED:
                    tb.Background = System.Windows.Media.Brushes.LightGreen;
                    tb.Foreground = System.Windows.Media.Brushes.Black;
                    if (setText)
                    {
                        tb.Text = "OK" + GetInfoAboutSucceededTest(in_runMode); ;
                    }
                    break;
                case State.FAILED:
                    tb.Background = System.Windows.Media.Brushes.Red;
                    tb.Foreground = System.Windows.Media.Brushes.White;
                    if (setText)
                    {
                        tb.Text = "FAILED" + GetInfoAboutFailedTest(in_runMode);
                    }
                    break;
            }
        }

        private void RefreshState()
        {
            if (m_state[(int)RunMode.TEST_FUNCS] != State.IDLE)
            {
                RefreshState(RunMode.TEST_FUNCS, txtInfo, GetInfoStartArgTestFunc());
            }
            RefreshState(RunMode.TEST_ALL, txtInfo, string.Empty, m_state[(int)RunMode.TEST_ALL] != State.IDLE);

            RefreshButtons();
        }

        private void RefreshButtons()
        {
            bool running = (m_state[(int)RunMode.TEST_ALL] == State.RUNNING)
                || (m_state[(int)RunMode.TEST_FUNCS] == State.RUNNING);

            btnRunSelectedTestFunc.IsEnabled = !running || ((m_state[(int)RunMode.TEST_FUNCS] == State.RUNNING) && (m_idxRunSelectedTestFuncs == -1));
            btnRunAllTestFuncs.IsEnabled = !running || ((m_state[(int)RunMode.TEST_FUNCS] == State.RUNNING) && (m_idxRunSelectedTestFuncs >= 0) || (m_state[(int)RunMode.TEST_ALL] == State.RUNNING));
            btnOpenProtocolFile.IsEnabled = !running;
            btnRefreshAll.IsEnabled = !running;

            btnRunSelectedTestFunc.Content = ((btnRunSelectedTestFunc.IsEnabled && running) ? "Stop test" : "Run selected");
            btnRunAllTestFuncs.Content = ((btnRunAllTestFuncs.IsEnabled && running) ? "Stop test" : "Run all");

            btnMoreOptions.Content = (m_optionsVisible ? "Hide" : "...");
        }

        private void SetRowVisibility()
        {
            if (m_optionsVisible)
            {
                mainRowOptions.Height = new GridLength(56);
                mainRowDefaultArgs.Height = new GridLength(30);
            }
            else
            {
                mainRowOptions.Height = new GridLength(0);
                mainRowDefaultArgs.Height = new GridLength(0);
            }

            if (m_logVisible)
            {
                mainRowLog.Height = new GridLength(5, GridUnitType.Star);
                mainRowLogButton.Height = new GridLength(30);
            }
            else
            {
                mainRowLog.Height = new GridLength(0);
                mainRowLogButton.Height = new GridLength(0);
            }
        }

        private void SetColVisibility()
        {
            switch (m_colVisibility)
            {
                case ColVisibility.BOTH:
                    mainColGroups.Width = new GridLength(1, GridUnitType.Star);
                    mainColFuncs.Width = new GridLength(1, GridUnitType.Star);
                    break;

                case ColVisibility.LEFT:
                    mainColGroups.Width = new GridLength(1, GridUnitType.Star);
                    mainColFuncs.Width = new GridLength(0);
                    break;

                case ColVisibility.RIGHT:
                    mainColGroups.Width = new GridLength(0);
                    mainColFuncs.Width = new GridLength(1, GridUnitType.Star);
                    break;
            }
        }

        public bool TestIsRunning()
        {
            for (int i = 0; i < 4; ++i)
            {
                if (m_state[i] == State.RUNNING)
                    return true;
            }
            return false;
        }

        public void TriggerDelayedRefresh()
        {
            // Redirect to main GUI thread
            WriteLine(2, "TriggerDelayedRefresh: passing to gui thread...");
            lstEvents.Dispatcher.Invoke(new Action(() => TriggerDelayedRefreshFromGuiThread()));
        }

        public void TriggerDelayedRefreshFromGuiThread()
        {
            if (m_refreshTimerIsActive)
            {
                WriteLine(2, "TriggerDelayedRefreshFromGuiThread: timer already active,call is ignored");
                return;
            }

            m_refreshTimerIsActive = true;
            m_refreshTimer.Tick += new EventHandler(RefreshTimer_Elapsed);
            m_refreshTimer.Interval = new TimeSpan(0, 0, 1);
            m_refreshTimer.Start();
            WriteLine(2, "TriggerDelayedRefreshFromGuiThread: new timer started");
        }

        private void RefreshTimer_Elapsed(object sender, EventArgs e)
        {
            if (!m_refreshTimerIsActive)
            {
                WriteLine(2, "RefreshTimer_Elapsed: timer already stopped, ignore timer event!");
                return;
            }
            WriteLine(2, "RefreshTimer_Elapsed: timer will be stopped, refresh is executed");
            m_refreshTimerIsActive = false;
            m_refreshTimer.Stop();
            m_mainEvents.OnRefreshNow();
        }

        /// [threading sync gui thread]
        public void AddInfoToEventList(string info)
        {
            // Redirect to main GUI thread
            lstEvents.Dispatcher.Invoke(new Action(() => AddInfoToEventListFromGuiThread(info)));
        }

        private void AddInfoToEventListFromGuiThread(string info)
        {
            // here we are sure to be within GUI thread and we can
            // safely access the list box
            lstEvents.Items.Add(info);
            /// ...
            /// [threading sync gui thread]

            // Ensure last added item is visible
            System.Windows.Automation.Peers.ListBoxAutomationPeer svAutomation = (System.Windows.Automation.Peers.ListBoxAutomationPeer)
                System.Windows.Automation.Peers.ScrollViewerAutomationPeer.CreatePeerForElement(lstEvents);

            System.Windows.Automation.Provider.IScrollProvider scrollInterface = (System.Windows.Automation.Provider.IScrollProvider)svAutomation.GetPattern(System.Windows.Automation.Peers.PatternInterface.Scroll);
            System.Windows.Automation.ScrollAmount scrollVertical = System.Windows.Automation.ScrollAmount.LargeIncrement;
            System.Windows.Automation.ScrollAmount scrollHorizontal = System.Windows.Automation.ScrollAmount.NoAmount;
            //If the vertical scroller is not available, the operation cannot be performed, which will raise an exception. 
            if (scrollInterface == null)
            {
                return;
            }
            if (scrollInterface.VerticallyScrollable)
                scrollInterface.Scroll(scrollHorizontal, scrollVertical);

            //lstEvents..TopIndex = lstEvents.Items.Count - 1;
            //lstEvents.SelectedIndex = -1;
        }

        internal void SetTestSuiteResult(string suitePath, Result result, string info, bool propagateResult)
        {
            if (!CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(new Action(() => { SetTestSuiteResult(suitePath, result, info, propagateResult); }));
            }
            else
            {
                var item = rootTestTreeViewItem.TreeViewItems.First().GetOverallTestGroups().FirstOrDefault(x => x.TestGroupEntry.Name == suitePath.Trim('/'));
                if (item != null)
                {
                    item.TestResult.Result = result;
                    item.TestResult.ProcessOutput = info;
                    if (propagateResult)
                    {
                        item.PropagateTestResultToAllChilds();
                    }
                    if (result == Result.Disabled)
                    {
                        m_numDisabledTests[(int)m_curRunMode] += item.OverallTestFunctionCount;
                    }
                }
                else
                {
                    //Unfortunately, BOOST interprets BOOST_DATA_TEST_CASE as Suite so we have to seek for that, too
                    var dataDestCaseItem = rootTestTreeViewItem.TreeViewItems.First().GetOverallTestFunctions().FirstOrDefault(x => x.TestFuncEntry.IsDataTestCase && x.TestFuncEntry.TestFunction == suitePath);
                    if (dataDestCaseItem != null)
                    {
                        dataDestCaseItem.TestResult.Result = result;
                        dataDestCaseItem.TestResult.ProcessOutput = info;
                        if (propagateResult)
                        {
                            item.PropagateTestResultToAllChilds();
                        }
                    }
                    else
                    {
                        WriteLine(1, $"ERROR: No test group found with suite '{suitePath}'");
                    }
                }

                RefreshState();
            }
        }

        internal void SetTestCaseResult(string suitePath, string name, Result result, string info)
        {
            if (!CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(new Action(() => { SetTestCaseResult(suitePath, name, result, info); }));
            }
            else
            {
                var item = rootTestTreeViewItem.TreeViewItems.First().GetOverallTestFunctions().FirstOrDefault(x => x.TestFuncEntry.TestGroup.Name == suitePath.Trim('/') && x.TestFuncEntry.TestFunction == name);
                if (item != null)
                {
                    item.TestResult.Result = result;
                    item.TestResult.ProcessOutput = info ?? string.Empty;
                }
                else
                {
                    WriteLine(1, $"ERROR: No test case found with suite '{suitePath}' and name '{name}'");
                }
                m_numExecutedTests[(int)m_curRunMode]++;

                switch (result)
                {
                    case Result.Disabled:
                        m_numDisabledTests[(int)m_curRunMode]++;
                        break;
                    case Result.Failed:
                        m_numFailedTests[(int)m_curRunMode]++;
                        break;
                    default:
                        break;
                }
                RefreshState();
            }
        }

        internal void SetTestInfo(ProjectInfo in_projectInfo)
        {
            m_projectInfo = in_projectInfo;
            for (int i = 0; i < 4; ++i)
            {
                m_state[i] = State.IDLE;
                m_numFailedTests[i] = 0;
                m_numDisabledTests[i] = 0;
                m_numExecutedTests[i] = 0;
                m_totalNumTestsToExecute[i] = 0;
            }
            if (m_projectInfo != null)
            {
                txtInfo.Text = m_projectInfo.SelectedProject + " - " + m_projectInfo.config;
                RefreshTestGroupList();
            }
            RefreshDisplay();
        }

        private string GetInfoStartArgTestFunc()
        {
            switch (m_projectInfo.SelectedProject.AppType)
            {
                case AppType.BOOST:
                    return "Select single test case / edit cmd";
                case AppType.TTB:
                    return "Select single test func / edit cmd";
                default:
                    return "--";
            }
        }

        private void RefreshDisplay()
        {
            RefreshDisplayText();
            RefreshState();
            SetColVisibility();
            SetRowVisibility();
        }

        private void RefreshDisplayText()
        {
            switch (m_projectInfo.SelectedProject.AppType)
            {
                case AppType.BOOST:
                    txtInfoTestGroups.Text = "BOOST test suites";
                    txtInfoTestFuncs.Text = "BOOST test cases";
                    chkTestFuncsForSelTestGroup.Content = "within suite";
                    break;
                case AppType.TTB:
                    txtInfoTestGroups.Text = "TTB test files";
                    txtInfoTestFuncs.Text = "TTB test functions";
                    chkTestFuncsForSelTestGroup.Content = "within file";
                    break;
                default:
                    txtInfoTestGroups.Text = "--";
                    txtInfoTestFuncs.Text = "--";
                    chkTestFuncsForSelTestGroup.Content = "--";
                    break;
            }
        }

        private void AddTestGroupEntryToTreeView(NodeList<TestGroupEntry> testGroupEntryNode, TestTreeViewItem parentTreeView)
        {
            TestTreeViewItem treeViewItem = new TestTreeViewItem(parentTreeView, testGroupEntryNode.Value) { DisplayName = testGroupEntryNode.Value.Name == string.Empty ? "<default>" : testGroupEntryNode.Value.Name };

            //add test functions
            foreach (var testFunc in testGroupEntryNode.Value.testFuncs)
            {
                treeViewItem.TreeViewItems.Add(new TestFunctionTreeViewItem(treeViewItem, testFunc));
            }

            //child group entries
            foreach (var childs in testGroupEntryNode.Childs)
            {
                //TODO: sort
                //Recursive call
                AddTestGroupEntryToTreeView(childs, treeViewItem);
            }

            //add to treeView
            parentTreeView.TreeViewItems.Add(treeViewItem);
        }

        private void RefreshTestGroupList()
        {
            if (m_projectInfo.SelectedProject != null)
            {
                TestExecWin.SortOrder sortOrder = TestExecWin.SortOrder.None;
                switch (m_sortOrder)
                {
                    case SortOrder.AS_READ_FROM_FILE:
                        sortOrder = TestExecWin.SortOrder.None;
                        break;
                    case SortOrder.ALPHA:
                        sortOrder = TestExecWin.SortOrder.Ascending;
                        break;
                    case SortOrder.REVERSE_ALPHA:
                        sortOrder = TestExecWin.SortOrder.Descending;
                        break;
                    case SortOrder.REVERSE_READ:
                        sortOrder = TestExecWin.SortOrder.Reverse;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                testTreeView.Items.Clear();
                rootTestTreeViewItem = new TestTreeViewItem(null, new TestGroupEntry(true, string.Empty)) { DisplayName = "dummyRoot" };
                AddTestGroupEntryToTreeView(m_projectInfo.SelectedProject.TestGroups, rootTestTreeViewItem);
                rootTestTreeViewItem.TreeViewItems.First().OverallSortAllChilds(sortOrder);
                testTreeView.Items.Add(rootTestTreeViewItem.TreeViewItems.First());
            }
        }

        private void treeViewItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var dataContext = ((MultiSelectTreeViewItem)sender).DataContext;

            if (dataContext is TestFunctionTreeViewItem fctCtx)
            {
                if (fctCtx.TestResult != null)
                {
                    txtResultInfo.Text = fctCtx.TestResult.ProcessOutput;
                }
                else
                {
                    txtResultInfo.Text = string.Empty;
                }
            }
            else if (dataContext is TestTreeViewItem ctx)
            {
                if (ctx.TestResult != null)
                {
                    txtResultInfo.Text = ctx.TestResult.ProcessOutput;
                }
                else
                {
                    txtResultInfo.Text = string.Empty;
                }
            }
            else
            {
                txtResultInfo.Text = string.Empty;
            }

            e.Handled = true;
        }

        private void treeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (testTreeView.LastSelectedItem is TestFunctionTreeViewItem fctCtx)
            {
                ShowSourceCodeFile(fctCtx.TestFuncEntry.FileFullPath, fctCtx.TestFuncEntry.LineNum);
                e.Handled = true;
            }
        }

        private async void RunContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await RunSelectedTestFunctions();
        }

        private void ShowSourceCodeFile(string file, int line)
        {
            try
            {
                WriteLine(3, "GoTo src: selIndex=" + line);
                m_mainEvents.OnOpenSourceFile(file, line);
            }
            catch (Exception ex)
            {
                WriteLine(1, "EXCEPTION: " + ex.ToString());
            }
        }

        private void StopExecution()
        {
            m_stoppedByUser = true;
            m_executor.KillRunningProcess();
        }

        private void WriteLine(int in_outputLevel, String in_info)
        {
            if (m_evenReceiver != null)
            {
                m_evenReceiver.WriteLine(in_outputLevel, in_info);
            }
        }

        private void cbxDefaultArgs_TextChanged(object sender, EventArgs e)
        {
            WriteLine(3, "cbxDefaultArgs_TextChanged: Storing new args=" + cbxDefaultArgs.Text);
            Properties.Settings.Default.GeneralCmdLineArgs = cbxDefaultArgs.Text;
            Properties.Settings.Default.Save();
        }

        private string GetExportInfo(bool in_withTimeStamp)
        {
            string info = "Exported from TestExecWin - " + txtInfo.Text;
            if (in_withTimeStamp)
            {
                var now = DateTime.Now;
                info += " - " + now.ToShortDateString() + " " + now.ToLongTimeString();
                info = "// " + info + "\n// Source path: " + m_projectInfo.SelectedProject.SourceDirPath + "\n";
            }
            return info;
        }

        private string GetListBoxItemsAsText(System.Windows.Controls.ListBox in_listBox)
        {
            string contents = "";
            foreach (var i in in_listBox.Items)
            {
                contents += i.ToString() + "\n";
            }
            return contents;
        }

        private void btnExportLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.FileName = GetExportInfo(false);
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    System.IO.File.WriteAllText(saveFileDialog.FileName,
                        GetExportInfo(true) + "\n" + GetListBoxItemsAsText(lstEvents));
                }
            }
            catch (Exception ex)
            {
                WriteLine(1, "EXCEPTION: " + ex.ToString());
            }
        }

        private void btnCopyLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.Clipboard.SetText(GetExportInfo(true) + "\n" + GetListBoxItemsAsText(lstEvents));
                WriteLine(1, "Copied to clipboard, ready for pasting!");
            }
            catch (Exception ex)
            {
                WriteLine(1, "EXCEPTION: " + ex.ToString());
            }
        }

        private void StartProcess(string exePath, string args, string workDir, bool enableBoostParsing)
        {
            int waitTimeMs = GetMaxWaitTimeInMs();
            if (waitTimeMs>0)
            {
                m_executionTimer.Interval = waitTimeMs;
                m_executionTimer.Start();
            }
            m_executor.StartProcess(exePath, args, workDir, enableBoostParsing);
        }
        private int GetMaxWaitTimeInMs()
        {
            switch (m_waitMode)
            {
                case WaitMode.WAIT_ENDLESS:
                    return 0;
                case WaitMode.WAIT_30_SEC:
                    return 30 * 1000;
                case WaitMode.WAIT_1_MIN:
                    return 60 * 1000;
                case WaitMode.WAIT_2_MIN:
                    return 120 * 1000;
                case WaitMode.WAIT_5_MIN:
                    return 300 * 1000;
                case WaitMode.WAIT_10_MIN:
                    return 600 * 1000;
                case WaitMode.WAIT_20_MIN:
                    return 1200 * 1000;
                case WaitMode.WAIT_30_MIN:
                    return 1800 * 1000;
                case WaitMode.WAIT_40_MIN:
                    return 2400 * 1000;
                case WaitMode.WAIT_60_MIN:
                    return 3600 * 1000;
                default:
                    return 3600 * 1000;
            }
        }
        private void OnExecutionTimeout(Object myObject, EventArgs myEventArgs)
        {
            m_executionTimer.Stop();
            WriteLine(1, "TIMEOUT of " + GetMaxWaitTimeInMs()/1000 + "sec has expired!");
            m_executor.KillRunningProcess();
        }

        private void SaveLog(RunMode in_runMode)
        {
            try
            {
                string fileName = "TextExecWin.";
                if (in_runMode == RunMode.TEST_FUNCS)
                {
                    fileName += "RunAllTestFuncs." + txtInfo.Text;
                }
                else if (in_runMode == RunMode.TEST_ALL)
                {
                    fileName += "SingleRun." + txtInfo.Text;
                }
                var now = DateTime.Now;
                string timeStamp = now.ToString("yyyy.MM.dd_HH.mm.ss");
                fileName += "." + timeStamp + ".Log.txt";
                WriteLine(2, "SaveLog-fileName=" + fileName);

                string fileDir = m_projectInfo.SelectedProject.TargetDirPath;
                if (fileDir == "<not set>")
                {
                    // Get the first path found within environment variables TMP, TEMP or USERPROFILE
                    fileDir = System.IO.Path.GetTempPath();
                }
                string fullFilePath = fileDir + "\\" + fileName;
                WriteLine(2, "SaveLog-fullFilePath=" + fullFilePath);

                string fileHeader = "// Exported from TestExecWin - " + now.ToShortDateString() + " " + now.ToLongTimeString();
                fileHeader += "\n// Test project : " + txtInfo.Text;
                fileHeader += "\n// Source path  : " + m_projectInfo.SelectedProject.SourceDirPath;
                fileHeader += "\n// RunMode      : " + in_runMode + "\n\n";
                WriteLine(3, "SaveLog-fileHeader=\n" + fileHeader);

                // Write list contents to file
                System.IO.File.WriteAllText(fullFilePath, fileHeader + GetListBoxItemsAsText(lstEvents));
            }
            catch (Exception ex)
            {
                WriteLine(1, "SaveLog-EXCEPTION: " + ex.ToString());
            }
        }
    }
}