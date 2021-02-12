\ingroup GrpHiddenPages
\author Gerald Fahrnholz

\defgroup GrpExecutor Class Executor - process execution and redirection of output
@{
\ingroup GrpDesign

Overview:
- \ref executor_data                 "Relevant class attributes"
- \ref executor_start_process        "Starting a process"
- \ref executor_redirect             "Redirection of process output"
- \ref executor_kill_process         "Kill a running process"

\anchor executor_data 
<h2>Relevant class attributes</h2>
To manage the execution of an external procss the following class data are used:
\snippet TestExecWin/Executor.cs exec data

\anchor executor_start_process 
<h2>Starting a process</h2>
\snippet TestExecWin/Executor.cs exec start process

\anchor executor_redirect 
<h2>Redirection of process output</h2>
The methods StandardOutputReceiver and StandardErrorReceiver will receive any output
from the started process.
On each call they will pass the given data to the output pane of Visual Studio:
\snippet TestExecWin/Executor.cs exec redirect stdout

\anchor executor_kill_process 
<h2>Kill a running process</h2>
A running process can be stopped at any time by simply calling:
\snippet TestExecWin/Executor.cs exec kill process
Even with killing the process the callback function Executor.Process_Exited will be called.

@}
