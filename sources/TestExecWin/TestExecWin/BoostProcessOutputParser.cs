using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExecWin
{
    /// <summary>
    /// Parses Boost process HRF-formatted output messages. For useful parsing results, please set --log_level=message
    /// Hint: In theory, we could use XML format instead of HRF but boost seems to don't send XML data while test is running
    /// </summary>
    public class BoostProcessOutputParser
    {
        public class TestModuleEventArgs : EventArgs
        {
            public TestExecInfoType TestExecInfoType { get; set; }
            public string TestModule { get; set; }
            public string TestSuite { get; set; }
            public string TestCase { get; set; }
            public string TestInfo { get; set; }
            public int DataDrivenTestCaseIndex { get; set; }
            public TestModuleEventArgs()
                : base()
            {
                DataDrivenTestCaseIndex = -1;
            }
        }

        public event EventHandler<TestModuleEventArgs> OnTestModuleEntered;
        public event EventHandler<TestModuleEventArgs> OnTestModuleLeft;
        public event EventHandler<TestModuleEventArgs> OnTestModuleSkipped;
        public event EventHandler<TestModuleEventArgs> OnTestSuiteEntered;
        public event EventHandler<TestModuleEventArgs> OnTestSuiteLeft;
        public event EventHandler<TestModuleEventArgs> OnTestSuiteSkipped;
        public event EventHandler<TestModuleEventArgs> OnTestCaseEntered;
        public event EventHandler<TestModuleEventArgs> OnTestCaseLeft;
        public event EventHandler<TestModuleEventArgs> OnTestCaseSkipped;

        public bool EnableParsing { get; set; }

        public enum TestExecInfoType
        { 
            Entering,
            Leaving,
            Skipping,
            Error,
        }
        private class TestExecInfo
        {
            public string TestType { get; set; }
            public string TestName { get; set; }
            public TestExecInfoType TestExecInfoType { get; set; }
            public int DataDrivenTestCaseIndex { get; set; }

            public TestExecInfo()
            {
                DataDrivenTestCaseIndex = -1;
            }
        }

        public StringBuilder StandardOutputStringBuilder { get; private set; }
        public StringBuilder ErrorOutputStringBuilder { get; private set; }
        private StringBuilder CurrentContextStringBuilder { get; set; }

        private bool CurrentSuiteDisabled { get; set; }

        public string CurrentTestModule { get; private set; }
        public string CurrentTestSuite { get; private set; }
        public string CurrentTestCase { get; private set; }

        public bool CurrentTestCaseFailed { get; private set; }

        private readonly Stack<string> testSuiteStack = new Stack<string>();

        public BoostProcessOutputParser()
        {
            StandardOutputStringBuilder = new StringBuilder(2000000);
            ErrorOutputStringBuilder = new StringBuilder(2000000);
            CurrentContextStringBuilder = new StringBuilder(100000);
            Clear();
        }

        private string GetCurrentSuiteStack()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var suite in testSuiteStack.Reverse())
            {
                if (!string.IsNullOrEmpty(suite))
                {
                    sb.Append('/');
                    sb.Append(suite);
                }
            }

            return sb.ToString();
        }

        public void StandardOutputReceiver(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                foreach (var line in outLine.Data.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    StandardOutputStringBuilder.AppendLine(outLine.Data);
                    CurrentContextStringBuilder.AppendLine(outLine.Data);

                    if (EnableParsing)
                    {
                        var testLineInfo = ParseStandardOutputLine(line);
                        if (testLineInfo != null)
                        {
                            switch (testLineInfo.TestType)
                            {
                                case "module":
                                    switch (testLineInfo.TestExecInfoType)
                                    {
                                        case TestExecInfoType.Entering:
                                            CurrentTestModule = testLineInfo.TestName;
                                            CurrentTestCaseFailed = false;
                                            CurrentContextStringBuilder.Clear();
                                            OnTestModuleEntered?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = CurrentTestSuite, TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Entering, DataDrivenTestCaseIndex = testLineInfo.DataDrivenTestCaseIndex });
                                            break;
                                        case TestExecInfoType.Leaving:
                                            CurrentTestSuite = string.Empty;
                                            CurrentTestCase = string.Empty;
                                            OnTestModuleLeft?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = CurrentTestSuite, TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Leaving, DataDrivenTestCaseIndex = testLineInfo.DataDrivenTestCaseIndex });
                                            CurrentContextStringBuilder.Clear();
                                            CurrentTestModule = string.Empty;
                                            break;
                                        case TestExecInfoType.Skipping:
                                            CurrentTestCaseFailed = false;
                                            OnTestModuleSkipped?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = CurrentTestSuite, TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Skipping, TestInfo = "Module disabled", DataDrivenTestCaseIndex = testLineInfo.DataDrivenTestCaseIndex });
                                            break;
                                    }
                                    break;

                                case "suite":
                                    switch (testLineInfo.TestExecInfoType)
                                    {
                                        case TestExecInfoType.Entering:
                                            testSuiteStack.Push(testLineInfo.TestName);
                                            CurrentTestSuite = testLineInfo.TestName;
                                            CurrentContextStringBuilder.Clear();
                                            OnTestSuiteEntered?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = CurrentTestSuite, TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Entering, DataDrivenTestCaseIndex = testLineInfo.DataDrivenTestCaseIndex });
                                            break;
                                        case TestExecInfoType.Leaving:
                                            OnTestSuiteLeft?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = CurrentTestSuite, TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Leaving, DataDrivenTestCaseIndex = testLineInfo.DataDrivenTestCaseIndex });
                                            if (testSuiteStack.Count > 0)
                                            {
                                                testSuiteStack.Pop();
                                                if (testSuiteStack.Count > 0)
                                                {
                                                    CurrentTestSuite = testSuiteStack.Peek();
                                                }
                                                else
                                                {
                                                    //uh, can't peek over root
                                                    CurrentTestSuite = string.Empty;
                                                }
                                            }
                                            else
                                            {
                                                //uh, can't pop over root
                                                CurrentTestSuite = string.Empty;
                                            }
                                            CurrentTestCase = string.Empty;
                                            CurrentTestCaseFailed = false;
                                            CurrentContextStringBuilder.Clear();
                                            break;
                                        case TestExecInfoType.Skipping:
                                            OnTestSuiteSkipped?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = testLineInfo.TestName, TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Skipping, TestInfo = "Suite disabled", DataDrivenTestCaseIndex = testLineInfo.DataDrivenTestCaseIndex });
                                            break;
                                    }
                                    break;

                                case "case":
                                    switch (testLineInfo.TestExecInfoType)
                                    {
                                        case TestExecInfoType.Entering:
                                            //Hack TestCase and TestSuite for DataDriven testcases
                                            if (testLineInfo.DataDrivenTestCaseIndex >= 0)
                                            {
                                                CurrentTestCase = testSuiteStack.Peek();
                                                var testSuites = testSuiteStack.ToList();
                                                CurrentTestSuite = testSuites[testSuites.Count - 2];
                                            }
                                            else
                                            {
                                                CurrentContextStringBuilder.Clear();
                                                CurrentTestCaseFailed = false;
                                                CurrentTestCase = testLineInfo.TestName;
                                            }
                                            OnTestCaseEntered?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = CurrentTestSuite, TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Entering });
                                            break;
                                        case TestExecInfoType.Leaving:
                                            //Hack TestCase and TestSuite for DataDriven testcases
                                            if (testLineInfo.DataDrivenTestCaseIndex >= 0)
                                            {
                                                CurrentTestCase = testSuiteStack.Peek();
                                                var testSuites = testSuiteStack.ToList();
                                                CurrentTestSuite = testSuites[testSuites.Count - 2];
                                            }
                                            else
                                            {
                                                CurrentTestCase = testLineInfo.TestName;
                                            }
                                            OnTestCaseLeft?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = CurrentTestSuite, TestCase = CurrentTestCase, TestExecInfoType = CurrentTestCaseFailed ? TestExecInfoType.Error : TestExecInfoType.Leaving, TestInfo = CurrentContextStringBuilder.ToString(), DataDrivenTestCaseIndex = testLineInfo.DataDrivenTestCaseIndex });
                                            if (testLineInfo.DataDrivenTestCaseIndex < 0)
                                            {
                                                CurrentTestCase = string.Empty;
                                                CurrentContextStringBuilder.Clear();
                                                CurrentTestCaseFailed = false;
                                            }
                                            break;
                                        case TestExecInfoType.Skipping:
                                            OnTestCaseSkipped?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = CurrentTestSuite, TestCase = testLineInfo.TestName, TestExecInfoType = TestExecInfoType.Skipping, TestInfo = "Testcase was skipped", DataDrivenTestCaseIndex = testLineInfo.DataDrivenTestCaseIndex });
                                            break;
                                        case TestExecInfoType.Error:
                                            CurrentTestCaseFailed = true;
                                            break;
                                    }
                                    break;

                                default:
                                    //unknown testType
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private TestExecInfo ParseStandardOutputLine(string line)
        {
            TestExecInfo testLineInfo = null;
            string testType = null;
            string testName = null;
            int dataDrivenTestCaseIndex = 0;

            if (ParseTestLineInfo(line, "Entering test ", out testType, out testName, out dataDrivenTestCaseIndex))
            {
                testLineInfo = new TestExecInfo() { TestType = testType, TestName = testName, TestExecInfoType = TestExecInfoType.Entering, DataDrivenTestCaseIndex = dataDrivenTestCaseIndex };
            }
            else if (ParseTestLineInfo(line, "Leaving test ", out testType, out testName, out dataDrivenTestCaseIndex))
            {
                testLineInfo = new TestExecInfo() { TestType = testType, TestName = testName, TestExecInfoType = TestExecInfoType.Leaving, DataDrivenTestCaseIndex = dataDrivenTestCaseIndex };
            }
            else if (line.Contains(" is skipped "))
            {
                if (ParseTestLineInfo(line, ": Test ", out testType, out testName, out dataDrivenTestCaseIndex))
                {
                    testLineInfo = new TestExecInfo() { TestType = testType, TestName = testName, TestExecInfoType = TestExecInfoType.Skipping, DataDrivenTestCaseIndex = dataDrivenTestCaseIndex };
                }
                else
                {
                    //uh, not parseable so far
                }
            }
            else if (line.Contains("error: in"))
            {
                //We guess that an error only occurs on a case
                testLineInfo = new TestExecInfo() { TestType="case", TestExecInfoType = TestExecInfoType.Error };
            }

            return testLineInfo;
        }

        private bool ParseTestLineInfo(string line, string pattern, out string testType, out string testName, out int dataDrivenTestCaseIndex)
        {
            testType = null;
            testName = null;
            dataDrivenTestCaseIndex = -1;

            try
            {
                int enteringTestIndex = line.IndexOf(pattern);
                if (enteringTestIndex >= 0)
                {
                    var testInfo = line.Substring(enteringTestIndex + pattern.Length);
                    testType = testInfo.Substring(0, testInfo.IndexOf(' '));
                    //Starting quote
                    testName = testInfo.Substring(testInfo.IndexOf('"') + 1);
                    //Ending quote
                    testName = testName.Substring(0, testName.IndexOf('"'));
                    //Drop suite (group) info if present
                    testName = testName.Split('/').Last();

                    //Check if case is most likely a BOOST_DATA_CASE
                    if (testType == "case" && testName.StartsWith("_"))
                    {
                        if (int.TryParse(testName.Substring(1), out int dataIndex))
                        {
                            dataDrivenTestCaseIndex = dataIndex;
                        }
                    }

                    return true;
                }
                else
                {
                    //uh, not parseable so far
                }
            }
            catch (Exception)
            { 
                //Ignore parsing errors
            }
            return false;
        }

        public void StandardErrorReceiver(object sendingProcess, System.Diagnostics.DataReceivedEventArgs errLine)
        {
            if (!string.IsNullOrEmpty(errLine.Data))
            {
                ErrorOutputStringBuilder.AppendLine(errLine.Data);
                CurrentContextStringBuilder.AppendLine($"Error: {errLine.Data}");
            }
        }

        public void Clear()
        {
            StandardOutputStringBuilder.Clear();
            ErrorOutputStringBuilder.Clear();
            CurrentContextStringBuilder.Clear();
            CurrentTestModule = string.Empty;
            CurrentTestSuite = string.Empty;
            CurrentTestCase = string.Empty;
            testSuiteStack.Clear();
            testSuiteStack.Push(string.Empty);
        }
    }
}
