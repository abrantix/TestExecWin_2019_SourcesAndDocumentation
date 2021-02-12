\ingroup GrpHiddenPages
\author Gerald Fahrnholz

\defgroup GrpPackage Class TestExecWindowPackage - Visual Studio integration, enabling DTE access
@{
\ingroup GrpDesign

Overview:
- \ref package_base_class          "Visual Studio integration by deriving from base class 'AsyncPackage'"
- \ref package_access_dte          "Getting acess to DTE"

\anchor package_base_class 
<h2>Visual Studio integration by deriving from base class 'AsyncPackage'</h2>
Base class "AsyncPackage" implements the required interface IVsPackage which cares for proper
integration with Visual Studio:
\snippet TestExecWin/TestExecWindowPackage.cs package base class

\anchor package_access_dte 
<h2>Getting acess to DTE</h2>
Microsoft Visual Studio allows programmatic access via a special environment,
the "DTE" (= development tools environment). The "DTE object" is the root of the automation model
for Visual Studio.

To get access to DTE object you can establish a connection within the
package class of your C# assembly. The reference to the DTE object can be
distributed within your application classes as needed.

First you have to define appropriate data:
\snippet TestExecWin/TestExecWindowPackage.cs dte data

Request for the DTE object is done within the custom method InitializeDTE:
\snippet TestExecWin/TestExecWindowPackage.cs InitializeDTE

The call of InitializeDTE is done within overridden base method InitializeAsync:
\snippet TestExecWin/TestExecWindowPackage.cs calling InitializeDTE

@}
