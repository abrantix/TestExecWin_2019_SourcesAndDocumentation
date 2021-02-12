\ingroup GrpHiddenPages
\author Gerald Fahrnholz

\defgroup GrpInstallation Download and Installation
@{


<h2>Download</h2>
TestExecWindow is available for free download:
- [TestExecWin_2019.6.5.vsix for Visual Studio 2019](http://www.gerald-fahrnholz.eu/sw/downloads/TestExecWin_for_VisualStudio_2019/TestExecWin_2019.6.5.vsix):<br>
  VSIX installation file which automatically installs TestExecWin to Visual Studio 2019

- [TestExecWin_2017.6.4.vsix for Visual Studio 2017](http://www.gerald-fahrnholz.eu/sw/downloads/TestExecWin_for_VisualStudio_2017/TestExecWin_2017.6.4.vsix):<br>
  VSIX installation file which automatically installs TestExecWin to Visual Studio 2017

- [TestExecWin_1.6.3.vsix for Visual Studio 2015](http://www.gerald-fahrnholz.eu/sw/downloads/TestExecWin_for_VisualStudio_2015/TestExecWin_1.6.3.vsix):<br>
  VSIX installation file which automatically installs TestExecWin to Visual Studio 2015

- TestExecWin at Visual Studio Marketplace (
  [version for VS 2019](https://marketplace.visualstudio.com/items?itemName=GeraldFahrnholz.TestExecWindowforBOOSTTestVS2019),
  [version for VS 2017](https://marketplace.visualstudio.com/items?itemName=GeraldFahrnholz.TestExecWindowforBOOSTTestVS2017),
  [version for VS 2015](https://marketplace.visualstudio.com/vsgallery/6e3dda95-8fa2-4006-843e-39dc20a7d333)
  ):<br>
  Short description and possibility for download.

- [TestExecWin_2019_SourcesAndDocumentation.zip](http://www.gerald-fahrnholz.eu/sw/downloads/TestExecWin_for_VisualStudio_2019/TestExecWin_2019_SourcesAndDocumentation.zip):<br>
  Complete C# source code, mark down text files and documentation generated with DoxyGen.
  Instead of downloading the documentation you can directly use the [TestExecWin online documentation](http://www.gerald-fahrnholz.eu/sw/online_doc_testexecwin/generated/index.html):

- [Visual Studio 2017: TestExecWin_Sources.zip](http://www.gerald-fahrnholz.eu/sw/downloads/TestExecWin_for_VisualStudio_2017/TestExecWin_Sources.zip):<br>
  complete sources; language C#, Visual Studio 2017 project; includes markdown files of documentation
  and corresponding doxyfile to be used by DoxyGen 

- [Visual Studio 2015: TestExecWin_Sources.zip](http://www.gerald-fahrnholz.eu/sw/downloads/TestExecWin_for_VisualStudio_2015/TestExecWin_Sources.zip):<br>
  complete sources; language C#, Visual Studio 2015 project 

For more info about recent changes, known problems and planned enhancements see

- \ref change_history "Change History"
- \ref known_problems "Known problems - Current limitations"
- \ref future_versions "Features planned for future versions"


<h2>Installation</h2>
- Within your old version of "TestExecWindow" export your configured sets of test apps if desired.
- Within Visual studio deinstall your old version of "TestExecWindow" if already existing.
- Close Visual Studio.
- Double-Click downloaded installation file "TestExecWin.vsix". Confirm to install the extension.
- Restart Visual Studio.
- Within the newly installed version of "TestExecWindow" you now can import the saved sets of test apps.

Remark:<br>
During installation files will be copied to C:/Users /[UserName] /AppData /Local
/Microsoft /VisualStudio /14.0 /Extensions /[random name like 'sbmfwobd'].wfs. In case of problems
deinstallation can be done by simply removing this directory.
<p>

\anchor change_history
<h2>Change History TestExecWin for Visual Studio 2019</h2>

Actual version is 2019.6.5

- Version 2019.6.5 (03/2019)
   - [Download TestExecWin_2019.6.5.vsix](http://www.gerald-fahrnholz.eu/sw/downloads/TestExecWin_for_VisualStudio_2017/TestExecWin_2019.6.5.vsix)
   - adjusted for Visual Studio 2019, now using "AsyncPackage" which supports asynchronous loading

<h2>Change History TestExecWin for Visual Studio 2017</h2>

Actual version is 2017.6.4

- Version 2017.6.4 (10/2018)
   - [Download TestExecWin_2017.6.4.vsix](http://www.gerald-fahrnholz.eu/sw/downloads/TestExecWin_for_VisualStudio_2017/TestExecWin_2017.6.4.vsix)
   - extended functionality: now supporting macros BOOST_FIXTURE_TEST_CASE, TTB_BOOST_FIXTURE_TEST_CASE
   - change: new default for memory leak checking is "switched off" 
   - bugfix: improved the way how the correct project configuration (VCConfiguration) is found; this solves following problems:
     - "Import from solution" did not import all relevant projects from solution
     - when opening Visual Studio directly by clicking on a solution file the current startup project could not be found
   - bugfix: log files generated with option "shutdown" were not saved to target directory but to one directory level higher; when building log file path a missing path separator "\" was added

- Version 2017.6.3 (12/2017)
  - [Download TestExecWin_2017.6.3.vsix](http://www.gerald-fahrnholz.eu/sw/downloads/TestExecWin_for_VisualStudio_2017/TestExecWin_2017.6.3.vsix)
  - has the same functionality as TestExecWin 1.6.3 for Visual Studio 2015<br>

<h2>Change History TestExecWin for Visual Studio 2015</h2>

Actual version is 1.6.3. Older versions are no longer available online.

- Version 1.6.3 (12/2017)
  - [Download TestExecWin_1.6.3.vsix](http://www.gerald-fahrnholz.eu/sw/downloads/TestExecWin_for_VisualStudio_2015/TestExecWin_1.6.3.vsix):<br>
  - improvement for automatic shutdown
     - now using a forced shutdown (option "-f") to allow automatic shutdown also when the desktop is locked

- Version 1.6.2 (10/2017)
  - improvement for test execution: log files and automatic shutdown
    - you can decide to automatically shutdown your computer when all tests have terminated
    - before shutting down results from log pane are automatically written to logfile
    - logfiles are stored within target directory of current startup project or
      within temp directory
- Version 1.6.1 (05/2017)
  - improvement for test execution: max execution time
    - You can optionally define a max wait time interval. When time expires and the current call to your test has not yet terminated
    the test executable will be killed. The same timeout settings applies to all calls to your test app(s). When running a set of calls
    (e.g. within the tab with configured test apps or when choosing "Run each" for your test suites) after having killed the (possibly hanging)
    test app the next call will be started automatically.
  - improvement for list of test groups and funcs:
    - instead of selecting only a single list entry you now can select multiple entries.
      When clicking "Run selected" all selected test cases/suites will be executed within a single call to your test application.
  - improvement for configured test apps:
    - on button click you can scan your current solution and automatically add all found executables
      with their full execution path to the currently displayed test app set
    - on button click you can copy the test suites from current startup project to the currently displayed test app set.
      When executing the test app set each of the imported test suites will be executed within a separate call of your test executable
      (similar as you have pushed "Run each" within test group tab)
    - on button click you can copy the visible test cases from startup project to the currently displayed test app set.
      When executing the test app set each of the imported test cases will be executed within a separate call of your test executable
      (similar as you have pushed "Run each" within test func tab)
    - within your configured sets of test apps you can use "//" comments to (temporarily) skip an app from execution
      or to add arbitrary notes
    - when exporting a set of test apps the name of the set is used as default file name
    - you can now store 8 sets with test apps instead of 3
  - improvement for BOOST-Test:
    - added support for macro BOOST_AUTO_TEST_CASE_TEMPLATE
  - improvement for log list with test results:
    - copy to clip board (e.g. for sending within Email)
    - export as regular text file
  - improvement for memory check:
    - a string of "Object dump complete" within test output is also interpreted as memory leak.
      motivation: in case of really huge leak reports the limited buffer of output pane will
      no longer contain the initial message "Detected memory leaks"
  - bugfix source file parsser:
    now macro TTB_TEST_FUNC_DESC will be correctly parsed. You can also use "," and ")"
    within your test case description
    
- Version 1.6 (04/2017)
  - new feature: added second register card to support testing of a configurable set
    of test apps. You can define and save 3 different named sets of test apps directly
    within plugin settings. Additionally you can export and import your app sets
    (e.g. into next version of this plugin).
  - improvement: optionally checks for memory leaks (i.e. checks for text "Detected memory leaks"
    within test output as written to Visual Studio output pane).
    Only works for properly configured
    test applications which have activated a final memory check by C runtime.
    You can switch off this check, if you are tired of solving leak errors.
  - improvement: when started again, automatically restores last settings (e.g. last default args,
    last used set of test apps, last visibility of GUI controls)
  - improvement: added support for nested BOOST test suites
  - bugfix: test macros (e.g. BOOST_AUTO_TEST_CASE) which are commented out with "//" are ignored now

- Version 1.5.1 (12/2016)
  - bugfix: input text within default args field and selection of several lists 
    were reset when window was moved, docked or reopened
  - improvement: When being opened TestExecWin now automatically checks for
    the current startup project and displays all found test cases.

- Version 1.5 (12/2016)
  - Added "Src" buttons to directly jump to source code location
    of selected test function or test group
  - Added additional proposals for default args supporting BOOST and TTB test frames

- Version 1.4 (11/2016)
  - Running test application via Run button now causes dynamic color state for
    project name instead of changing background color of Run button
  - bugfix: changing startup project while test is running will now be ignored
    to avoid inconsistent data state and a possible crash of Visual Studio

\anchor known_problems
<h2>Known problems - Current limitations</h2>

- TTB tests: -selectTestFile

  **Problem**:<br>
  When using multiple selections for TTB test files only the last option will be
  considered by TTB testing framework

  **Workaround**:<br>
  Use only single selection of test files. You may manually add several test files separated by ","


\anchor future_versions
<h2>Features planned for future versions</h2>

- Currently there are no features planned. Let me know if you have special wishes which may be also useful for others.

<hr>
Next step: \ref GrpOpenTestExecWindow
@}

