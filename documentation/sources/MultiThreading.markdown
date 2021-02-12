\ingroup GrpHiddenPages
\author Gerald Fahrnholz

\defgroup GrpMultiThreading Do not forget to deal with multi threading
@{
\ingroup GrpDesign

Overview:
- \ref threading_problem          "The problem - unknown threads may call"
- \ref threading_solution         "A simple solution - synchronize via GUI thread"

\anchor threading_problem 
<h2>The problem - unknown threads may call</h2>
Within TestExecWindow many interactions start with pushing a button at the GUI.
Those actions are executed within the safe main GUI thread (dispatcher thread).

But we also react on asynhronous events which will arrive on some notification thread:

- When the test executable terminates we get informed by a system thread
  which is different from our GUI thread.
- Visual Studio sends notifications when something has changed (e.g. new startup project).
  Currently it seems that those notifications will already arrive on the safe GUI thread.
  But can we be sure for future versions?

Critical situations may occur when data are accessed both from GUI thread and some notification thread.
Unsynchronized changes may lead to corrupt data and program crash.
When concerning GUI elements (buttons, list boxes etc.) access is only allowed from GUI thread.
Visual Studio will rise an exception when access is tried from some other thread. 

\anchor threading_solution
<h2>A simple solution - synchronize via GUI thread</h2>

Assume we want to write a message to the log pane by calling the service function TestExecWindowControl.AddInfoToEventList.
We cannot be sure which thread will call this function.
But within GUI (TestExecWindowControl.xaml.cs) we can pass the call to the dispatcher thread:
\snippet TestExecWin/TestExecWindowControl.xaml.cs threading sync gui thread


An analogous problem occurs when we receive the message about the end of the test process
within main class TestExecWindow. In this case we also use
the GUI thread for synchronization:
\snippet TestExecWin/TestExecWindow.cs test terminated

The GUI again dispatches to the safe GUI thread:
\snippet TestExecWin/TestExecWindowControl.xaml.cs dispatch test terminate msg

@}
