\ingroup GrpHiddenPages
\author Gerald Fahrnholz

\defgroup GrpVisualStudioConnector Class VisualStudioConnector - Visual Studio: data access via DTE, notifications, automation commands
@{
\ingroup GrpDesign

Overview:
- \ref connector_connect_studio      "Register for notifications at Visual Studio"
- \ref connector_notifications       "Receiving notifications from Visual Studio"
- \ref connector_project_info        "Requesting project information via DTE"
- \ref connector_studio_actions      "Automate Visual Studio"
  - \ref connector_studio_action_open_text_file      "Open text file"
  - \ref connector_studio_action_start_debugging     "Start debugging"

\anchor connector_connect_studio 
<h2>Register for notifications at Visual Studio</h2>

Derive from notification interfaces:
\snippet TestExecWin/VisualStudioConnector.cs derive from notification interfaces

Advise for notifications:
\snippet TestExecWin/VisualStudioConnector.cs connect with VS

Unadvise from notifications:
\snippet TestExecWin/VisualStudioConnector.cs disconnect from VS

\anchor connector_notifications 
<h2>Receiving notifications from Visual Studio</h2>

Notification about a change of startup project:
\snippet TestExecWin/VisualStudioConnector.cs listen to changed startup project

Notification about a change of configuration:
\snippet TestExecWin/VisualStudioConnector.cs listen to switched debug/release configuration


\anchor connector_project_info 
<h2>Requesting project information via DTE</h2>

Get current configuration (e.g Debug or Release)
\snippet TestExecWin/VisualStudioConnector.cs get config name

Get name of startup project
\snippet TestExecWin/VisualStudioConnector.cs get name startup project

Get startup project interface
\snippet TestExecWin/VisualStudioConnector.cs get startup project

Implementation detail: DTE allows iteration over projects
\snippet TestExecWin/VisualStudioConnector.cs iterate dte projects

Implementation detail: Hierarchical recursive search through
tree of project items within method FindProjectRecursive:
\snippet TestExecWin/VisualStudioConnector.cs recursive search

Get name and path of generated executable (depends on previously found startupProj and configName)
\snippet TestExecWin/VisualStudioConnector.cs get exe path

Helper function to find a given configuration
\snippet TestExecWin/VisualStudioConnector.cs find config

\anchor connector_studio_actions 
<h2>Automate Visual Studio</h2>

\anchor connector_studio_action_open_text_file
<h3>Open a text file within Visual Studio</h3>
\snippet TestExecWin/VisualStudioConnector.cs open text file

\anchor connector_studio_action_start_debugging
<h3>Start debugging with special command line options</h3>
\snippet TestExecWin/VisualStudioConnector.cs start debugging

@}
