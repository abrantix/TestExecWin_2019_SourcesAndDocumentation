\ingroup GrpHiddenPages
\author Gerald Fahrnholz

\defgroup GrpDesign Internal Design
@{

- <strong>\ref GrpVisualStudioExtension</strong><br>
  How to use the Visual Studio Wizard to create an appropriate C# project
  for writing an extension plus several links to online resoures.
- <strong>\ref GrpPackage</strong><br>
  Class TestExecWindowPackage is responsible for registering TestExecWin
  as a valid package for Visual Studio. Furthermore the class establishes
  acess to DTE (development tools environment).
- <strong>\ref GrpVisualStudioConnector</strong><br>
  Class VisualStudioConnector encapsulates (nearly) all communication with
  Visual Studio. This includes requesting of project properties of current
  startup project and getting notified about changes within Visual Studio.
- <strong>\ref GrpParser</strong><br>
  Class SourceFileParser parses all .cpp files within the directory of the current
  startup project and all of its sub directories. Any found test cases of type
  BOOST or TTB will be extracted and prepared for presentation at GUI.
- <strong>\ref GrpExecutor</strong><br>
  Class Executor supports starting of a test application. Arbitrary start arguments can be
  passed to the test executable. The output of the test process is redirected
  to a special output pane within Visual Studio. When the test process
  terminates Executor will send a notification to TestExecWin to allow proper
  GUI updates.
- <strong>Class TestExecWindow</strong><br>
  Class Test ExecWindow is the main class which instantiates the helper classes
  VisualStudioConnector, SourceFileParser and Executor and establishes all required
  connections between them. Furthermore the utility function WriteLine which allows
  writing to log pane of TestExecWin is implemented here. To reduce coupling
  TestExecWindow implements the interfaces IMainEvents and IEventReceiver which
  may be called by other classes.
- <strong>Class TestExecWindowControl within TestExecWindowControl.xaml.cs</strong><br>
  is the typical GUI control class containing all button handlers and all code to
  manipulate GUI controls. Corresponding status data are stored to allow proper
  initialization and refresh of GUI display.
- <strong>TestExeccWindowControl.xaml</strong><br>
  is the typical layout file in XAML format. Here you can define all controls and
  their arrangement on screen. You can either directly edit the XAML file with the text
  editor or you can move controls from the ToolBox to the design editor and drag them
  around with the mouse.
- <strong>\ref GrpCommonFunctionality</strong><br>
  General support for online help, and additional display infos for a properly
  integrated Visual Studio extension. Typical files for an user interface application. 
- <strong>\ref GrpMultiThreading</strong><br>
  TestExecWindow has to deal with multiple threads. Synchronization is guaranteed
  by passing execution to GUI thread (dispatcher thread).
@}


\defgroup GrpSources Source files
@{
\ingroup GrpDesign
@}

\defgroup GrpSourceExecutor Executor.cs
@{
\ingroup GrpSources
\include "TestExecWin/Executor.cs"
@}

\defgroup GrpSourceSourceFileParser SourceFileParser.cs
@{
\ingroup GrpSources
\include "TestExecWin/SourceFileParser.cs"
@}

\defgroup GrpTestExecWindow TestExecWindow.cs
@{
\ingroup GrpSources
\include "TestExecWin/TestExecWindow.cs"
@}

\defgroup GrpTestExecWindowCommand TestExecWindowCommand.cs
@{
\ingroup GrpSources
\include "TestExecWin/TestExecWindowCommand.cs"
@}

\defgroup GrpTestExecWindowControlXaml TestExecWindowControl.xaml
@{
\ingroup GrpSources
\include "TestExecWin/TestExecWindowControl.xaml"
@}

\defgroup GrpTestExecWindowControlXamlCs TestExecWindowControl.xaml.cs
@{
\ingroup GrpSources
\include "TestExecWin/TestExecWindowControl.xaml.cs"
@}

\defgroup GrpTestExecWindowPackage TestExecWindowPackage.cs
@{
\ingroup GrpSources
\include "TestExecWin/TestExecWindowPackage.cs"
@}

\defgroup GrpSourceVisualStudioConnector VisualStudioConnector.cs
@{
\ingroup GrpSources
\include "TestExecWin/VisualStudioConnector.cs"
@}

\defgroup GrpHiddenPages Hidden page files of documentation
@{
\ingroup GrpSources
@}





