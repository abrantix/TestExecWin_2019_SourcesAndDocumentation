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
                                        OnTestModuleEntered?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = GetCurrentSuiteStack(), TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Entering });
                                        break;
                                    case TestExecInfoType.Leaving:
                                        CurrentTestSuite = string.Empty;
                                        CurrentTestCase = string.Empty;
                                        OnTestModuleLeft?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = GetCurrentSuiteStack(), TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Leaving });
                                        CurrentContextStringBuilder.Clear();
                                        CurrentTestModule = string.Empty;
                                        break;
                                    case TestExecInfoType.Skipping:
                                        CurrentTestCaseFailed = false;
                                        OnTestModuleSkipped?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = GetCurrentSuiteStack(), TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Skipping, TestInfo = "Module disabled" });
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
                                        OnTestSuiteEntered?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = GetCurrentSuiteStack(), TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Entering });
                                        break;
                                    case TestExecInfoType.Leaving:
                                        OnTestSuiteLeft?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = GetCurrentSuiteStack(), TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Leaving });
                                        if (testSuiteStack.Count > 0)
                                        {
                                            CurrentTestSuite = testSuiteStack.Pop();
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
                                        OnTestSuiteSkipped?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = Path.Combine(GetCurrentSuiteStack(), testLineInfo.TestName), TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Skipping, TestInfo = "Suite disabled" });
                                        break;
                                }
                                break;

                            case "case":
                                switch (testLineInfo.TestExecInfoType)
                                {
                                    case TestExecInfoType.Entering:
                                        CurrentContextStringBuilder.Clear();
                                        CurrentTestCase = testLineInfo.TestName;
                                        CurrentTestCaseFailed = false;
                                        OnTestCaseEntered?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = GetCurrentSuiteStack(), TestCase = CurrentTestCase, TestExecInfoType = TestExecInfoType.Entering });
                                        break;
                                    case TestExecInfoType.Leaving:
                                        OnTestCaseLeft?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = GetCurrentSuiteStack(), TestCase = CurrentTestCase, TestExecInfoType = CurrentTestCaseFailed ? TestExecInfoType.Error : TestExecInfoType.Leaving, TestInfo = CurrentContextStringBuilder.ToString() });
                                        CurrentTestCase = string.Empty;
                                        CurrentContextStringBuilder.Clear();
                                        CurrentTestCaseFailed = false;
                                        break;
                                    case TestExecInfoType.Skipping:
                                        OnTestCaseSkipped?.Invoke(this, new TestModuleEventArgs() { TestModule = CurrentTestModule, TestSuite = GetCurrentSuiteStack(), TestCase = testLineInfo.TestName, TestExecInfoType = TestExecInfoType.Skipping, TestInfo = "Testcase was skipped" });
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

        private TestExecInfo ParseStandardOutputLine(string line)
        {
            TestExecInfo testLineInfo = null;
            string testType = null;
            string testName = null;

            if (ParseTestLineInfo(line, "Entering test ", out testType, out testName))
            {
                testLineInfo = new TestExecInfo() { TestType = testType, TestName = testName, TestExecInfoType = TestExecInfoType.Entering };
            }
            else if (ParseTestLineInfo(line, "Leaving test ", out testType, out testName))
            {
                testLineInfo = new TestExecInfo() { TestType = testType, TestName = testName, TestExecInfoType = TestExecInfoType.Leaving };
            }
            else if (line.Contains(" is skipped "))
            {
                if (ParseTestLineInfo(line, ": Test ", out testType, out testName))
                {
                    testLineInfo = new TestExecInfo() { TestType = testType, TestName = testName, TestExecInfoType = TestExecInfoType.Skipping };
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

        private bool ParseTestLineInfo(string line, string pattern, out string testType, out string testName)
        {
            testType = null;
            testName = null;

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
