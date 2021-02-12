\ingroup GrpHiddenPages
\author Gerald Fahrnholz

\defgroup GrpUserGuide User guide
@{
@}


\defgroup GrpOpenTestExecWindow Open TestExecWindow
@{
\ingroup GrpUserGuide
Open the installed window via Menu View / Other windows / TestExecWindow:

\image html Pictures/OpenTestExecWindow.png

You can use the window as a free floating window or choose any pane within Visual Studio you find suited for attaching it.

<hr>
- Read next chapter: \ref GrpUserInterface
@}


\defgroup GrpUserInterface User interface - description of control elements
@{
\ingroup GrpUserGuide

Depending on your test application TestExecWin will show up similar to the following screenshot:

\image html Pictures/Preview.png

<h2>Upper Row: Current startup project</h2>
**TestXmlCheckWithPugiXml - Debug**<br>
Name of current startup project and currently selected configuration

**Checkbox "Debug"**<br>
Choose whether to start test within debugger or to execute it regularly

**Run**<br>
Run all tests; test application is started and only default args (see \ref GrpExtendedSettings) are used

**Out**<br>
Open test protocol file (assumption: YourTestApp.exe generates protocol file YourTestApp.out within target directory)

**Refresh**<br>
Manually refresh of all data (e.g. when you have just added new test cases to your source code)

**Button "..."**<br>
Show/hide additional settings (see \ref GrpExtendedSettings)

<h2>Middle part: Test suites and test cases</h2>
The left / right list contains all BOOST test suites / test cases defined in the current
startup project.

**Check box "within suite"**<br>
You can decide whether to show all BOOST test cases or only those contained within
   the selected BOOST test suite

**Colored status fields "FAILED/OK"**<br>
Execution status of the last test execution

**Text field "--runTest="**<br>
Command line parameter which is automatically set when selecting one or more items within the list above.
   You can manually edit the parameter (e.g. for adding additional options)

**Run selected**<br>
starts the test executable by using the commmand line above. You can select one or more test suites / test cases
to be executed within a single call to your test executable

**Run each**<br>
sequentially executes all test suites / test cases within the list above. Each list entry is executed as a separate
call to your test executable. The result of each test run is displayed within log pane.

**Src**<br>
opens the corresponding source file and jumps to the selected BOOST test case.
For Boost test suites within the left list the first BOOST test case within the suite will be presented.

<h2>Lower part: Log pane</h2>
Log pane containing important status messages. The test execution itself is displayed within the regular output pane of Visual Studio.

**Checkbox "Shutdown"**<br>
When activated the termination of the selected tests will cause a saving of the log pane to a protocol file and a subsequent
shutdown of your computer. The log file will be stored within the target directory of your startup application or (if there is no
startup project selected) within the first temporary directory as found within environment variables TMP, TEMP or USERPROFILE.

**Clear log**<br>
Clear entries within log pane

**Copy**<br>
Copy list contents to paste buffer for later insertion as regular multiline text.

**Export**<br>
Export the list contents to a text file.

<HR>
- Read next chapter: \ref GrpUserInterfaceBatch
- Read previous chapter: \ref GrpOpenTestExecWindow
@}

\defgroup GrpUserInterfaceBatch Batch file support
@{
\ingroup GrpUserGuide

To control multiple executions of arbitrary test applications (e.g. the same test app with different start arguments or all/some test apps from your test solution) use batch file feature on tab "Test Apps":

\image html Pictures/TestApps.png

**Edit box**<br>
Basically the edit box in the middle of the screen has a simple batch syntax which can be used for manual editing:

- empty lines and lines starting with "//" are ignored
- all other lines are expected to contain "[fullPathToTestExecutable] [any number of start args]"

**Combobox "Test Success"**<br>
selects one of 8 predefined batch sets which are stored internally within tool settings. The name of each batch set can be changed by editing the text within the combo box (pay attention to auto switch feature of combo box when starting to edit)

**Run all**<br>
Starts the execution of all entries within the batch above.

**Save**<br>
Saves the last changes to the batch above within internal storage.

**Reload**<br>
Restores the batch to the state last saved to internal storage. May be useful when you want to cancel manual changes.

**Clear**<br>
Empties the text field.

**Export/Import**<br>
supports
- saving to an external text file
- reading contents from an external text file, the current contents are not replaced, the external contents are inserted at end of current batch
- automatically importing all test executables from within your current test solution
- automatically importing all test suites or test cases from the current startup project (e.g. to verify that all tests will run independently from each other or to
  identify which test cases have problems like memory leaks)

<hr>

- Read next chapter: \ref GrpExtendedSettings
- Read previous chapter: \ref GrpUserInterface
@}

\defgroup GrpExtendedSettings Extended settings - defaults args and configuration of layout
@{
\ingroup GrpUserGuide

You can adjust TestExecWindow to your needs. More options for individual configuration are available via the button "..." in the upper right corner. Selecting it leads to
a display of the following options:

\image html Pictures/MoreOptions.png

**Default args**<br>
define default args, which are used in addition to the args used for 
selecting a test suite or test case. This may be useful to activate some test behaviour
common for all test suites or test cases.

**List "only test groups"**<br>
decide whether to see both test suites and contained test cases
  or restrict visibility to one of them

**List "sort as read"**<br>
choose an appropriate sort order

**List "hide log"**<br>
hide the log pane if not needed

**List "wait for test app max 10 min"**<br>
wait indefinitely for termination of a test or set one of several predefined time intervals

**List "leak check"**<br>
activate or deactivate checking for memory leaks. The leak check assumes that your test app will use elementary
memory leak detection facilities of C runtime ("_CrtSetDbgFlag(.._CRTDBG_LEAK_CHECK_DF)"). In case of leaks corresponding messages are written to stdout/stderr ("Detected memory leaks...").
TestExecWin will simply check test output from Visual Studio pane to decide whether leaks are existing or not.
In combination with a separate run of each testcase you may identify the evil test cases.

**List "regular output"**<br>
rise output level within log pane (may be useful when looking for problems)


When the changed options from above are hidden again the configured window will look like:<p>
\image html Pictures/OnlyBoostTestSuites.png

<hr>
- Read next chapter: \ref GrpTypicalTestActivities
- Read previous chapter: \ref GrpUserInterface
@}

\defgroup GrpTypicalTestActivities Typical test activities
@{
\ingroup GrpUserGuide
Overview:
- \ref typical_test_select      "List available test cases and select for execution"
- \ref typical_test_search      "Searching for errors, extending test cases"
- \ref batch_testing            "Batch testing and automatic shutdown"

\anchor typical_test_select 
<h2>List available test cases and select for execution</h2>
Typically you will use TestExecWindow to get an overview of available test cases contained within a test application.
By selecting a single test suite or a single test case and then pushing "Run selected" TestExecWindow will
automatically set appropriate command line parameters and start the corresponding test case.
<p>
It depends on the structure of your specific test application if test suites or test cases can be executed independently
from each other. If this is not the case you should think about improving your test application. A good advise could be to
ensure that at least each test suite could be executed alone. 
<p>
TestExecWindow supports you to check which test suites or test cases can already run independently. By pushing "Run each" TestExecWindow will
start your test executable separately for each item in the list of test suites or test cases. The result of each call to your test application
is displayed within the log pane together with the used command line params.
<p>
Remark:<br>
If you simply want to execute all test cases as quick as possible it is recommended to use button "Run" at the top of the window.
In this case your test application will be started only once to execute all tests.


\anchor typical_test_search 
<h2>Searching for errors, extending test cases</h2>
If you want to extend your test cases or you want to search the reason for some test failure the following features may be useful:

- <strong>Protocol file</strong><br>
  Pushing "Out" opens the protocol file of your test application within Visual Studio. Usually you could find
  more detailed information in this file which may allow a better understanding of the error situation.
  It depends on your specific test application whether there exists a protocol file. TestExecWindow
  assumes a protocol file with the name "YourTestApp.out" besides the test executable "YourTestApp.exe".
- <strong>Debugging</strong><br>
  When searching for an error it may be useful to start test execution within debugger. You can simply switch debugging
  on and off by selecting / deselecting checkbox "Debug".<br>
  Remark: The command line params used to execute your test within debugger are stored within the project settings and
  will not be restored to the original values. You may have to reset them manually when running the debugger outside of
  TestExecWindow. 
- <strong>Src - jump to source code</strong><br>
  Pushing "Src" immediately opens the selected test function or test group within your source code.
  This may be useful for inspecting the test instructions when looking for a test error or for adding a
  new test case before or after the selected one.
- <strong>Refresh</strong><br>
  If you have just added a new test case press "Refresh" to trigger a refresh of the list of available test cases.
  This may also be necessary when you have opened TestExecWindow after you have already loaded your test project.
  TestExecWindow currentlly listens only to notifications about a changed startup project or a change of Debug/Release.
  If TestExecWindow is not opened it cannot listen to such events.

\anchor batch_testing 
<h2>Batch testing and automatic shutdown</h2>
After some time or when you are working within a team there may be a greater number of test applications and test cases.
It is a good idea to execute all unit tests **within the nightly or weekly build** you may have introduced. You should care
for a sufficient level of test output or a detailed test protocol to have enough information for later analysis of possible errors.

TestExecWin also offers the possibility to execute an arbitrary set of test applications in batch mode (for details see \ref GrpUserInterfaceBatch).
This feature may be useful in the following situations:

- **execute a specific subset of tests with one button click**<br>
  As a starting point for your batch you may use "Import from solution" to get a complete list of
  test executables from your test solution.
- **verify that all your test suites can be executed alone**<br>
  Use "Import test suites from startup project" to get a list of separate calls to your startup project
  with appropriate start args to select each test suite.
- **verify that all your test applications are free of memory leaks**<br>
  Ensure that within detailed settings "leak check" is activated. The status of each test execution is written to log pane and has one of the following values:
    - **OK:**<br>
      correct test result, no memory leak detected<br>
      Warning: if your test application is not configured to write memory leaks to stdout or if you have activated "no leak check" within detailed settings then you
      may get this status despite of having memory leaks!
    - **FAILED:**<br>
      there are functional errors (e.g. a BOOST-Check has failed), in this case the check for memory leaks was skipped
    - **FAILED (MEM_LEAKS):**<br>
      correct test result but memory leaks were detected

  **Precondition**:<br>
  Your test application is configured to write memory leak messages to stdout when program terminates
  (C runtime supports leak check, see for keywords "_CrtSetDbgFlag", "_CRTDBG_LEAK_CHECK_DF").

- **automatic shutdown and log file**<br>
  Before leaving your office you can start a test sequence. If you activate checkbox "Shutdown" your computer will be switched off
  when all tests have terminated. The next day you can find the test results from log pane within a protocol file
  with the name "TestExecWin.[InfoAboutExecutedTests].[TimeStamp].Log.txt". The protocol file is located within the target directory
  of your startup project. If there was no startup project selected at time of execution (via batch feature) you should check your
  temporary directory as defined via environment variables TMP, TEMP or USERPROFILE.
<hr>
- Read next chapter: \ref GrpParsingSoureFiles
- Read previous chapter: \ref GrpExtendedSettings
@}

\defgroup GrpParsingSoureFiles Parsing of source files - current limitations
@{
\ingroup GrpUserGuide

TestExecWindow parses the source files of your test application to look for test suites and test cases to execute.
During parsing the following rules and assumptions apply:

- within your Visual Studio solution only one startup project is set
- TestExecWindow knows the path to your startup project file. It assumes that all .cpp files located
  in the project directory or within a subdirectory of it may contain test cases.
- <strong>BOOST tests</strong> are expected to use the following macros to define the test case structure:<br>
  BOOST_AUTO_TEST_CASE<br>
  BOOST_AUTO_TEST_CASE_TEMPLATE<br>
  BOOST_AUTO_TEST_SUITE<br>
  BOOST_FIXTURE_TEST_SUITE<br>
  BOOST_FIXTURE_TEST_CASE<br>
  BOOST_AUTO_TEST_SUITE_END<br>
  TTB_BOOST_TEST_CASE (a combination of BOOST.Test with proprietary TTB framework)<br>
  TTB_BOOST_FIXTURE_TEST_CASE (a combination of BOOST.Test with proprietary TTB framework)<br>
- <strong>TTB tests</strong> are expected to use the following macros to define the test case structure:<br>   
  TTB_TEST_FUNC<br>
  TTB_TEST_FUNC_DESC


<hr>
- Read previous chapter: \ref GrpTypicalTestActivities
@}
