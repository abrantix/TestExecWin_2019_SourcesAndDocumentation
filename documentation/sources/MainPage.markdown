\ingroup GrpHiddenPages
\author Gerald Fahrnholz

Insert picture at this hidden location to force Doxygen to copy the image file to the generated documentation.
Image will be used within html img tag on main page:
\image html Pictures/TestExecWinPresentation.png

\mainpage TestExecWin - an extension for Visual Studio 2015/2017/2019
\anchor anchor_mainpage

[\ref GrpInstallation]
[\ref GrpUserGuide]
[\ref GrpDesign<br>]


## Abstract
Every time you change the startup project within your Visual Studio
solution TestExecWin will automatically check if there are any test
cases to run. All found test cases are presented within the TestExecWindow
and can there be selected for execution. 

## Features
- support for BOOST and (proprietary) TTB testing frameworks
- gives overview of available test suites / test cases
- executes test cases by automatically providing suitable start arguments
- test output is written to a special tab within Visual Studio output pane
- allows debugging for selected test case
- jumps to protocol file (.out file)
- jumps to source code of selected test case
- allows iteration over all test cases with automatic check
  if each test case can be run alone 
- supports batch functionality to execute an arbitrary sequence
  of test executables
- optional check for memory leaks
- supports automatic shutdown of computer when last test has terminated
- possibilty to dock/undock as any other VS window
- layout may be adjusted to the needs of your project

## Online documentation

<table style="border : none;">
  <tr>
    <td>
\htmlonly
<a href="http://www.gerald-fahrnholz.eu/sw/doc/TestExecWin_WebPresentation.html"><img width="60" height="40" src="TestExecWinPresentation.png" alt="PPT presentation"></a>
\endhtmlonly
</td>
    <td>[Start powerpoint web presentation](http://www.gerald-fahrnholz.eu/sw/doc/TestExecWin_WebPresentation.html)</td></tr>
</table>

[TestExecWin online documentation](http://www.gerald-fahrnholz.eu/sw/online_doc_testexecwin/generated/index.html):<br>
  Here you can first read detailed documentation without need for any download or installation 

[TestExecWin Windows help file](http://www.gerald-fahrnholz.eu/sw/online_doc_testexecwin/generated/TestExecWin.chm):<br>
  Ready to use documentation with full search functionality. Before using you should download the file and copy it
  to a local hard drive to avoid problems with security settings. If main content is still invisible open properties window
  within file explorer and allow acccess to downloaded file.

TestExecWin at Visual Studio Marketplace (
[version for VS 2019](https://marketplace.visualstudio.com/items?itemName=GeraldFahrnholz.TestExecWindowforBOOSTTestVS2019),
[version for VS 2017](https://marketplace.visualstudio.com/items?itemName=GeraldFahrnholz.TestExecWindowforBOOSTTestVS2017),
[version for VS 2015](https://marketplace.visualstudio.com/vsgallery/6e3dda95-8fa2-4006-843e-39dc20a7d333
):<br>
Short description and possibility for download.

## Contents
\ref GrpInstallation

\ref GrpUserGuide
  - \ref GrpOpenTestExecWindow
  - \ref GrpUserInterface
  - \ref GrpUserInterfaceBatch
  - \ref GrpExtendedSettings
  - \ref GrpTypicalTestActivities
  - \ref GrpParsingSoureFiles

\ref GrpDesign
  - \ref GrpVisualStudioExtension
  - \ref GrpPackage
  - \ref GrpVisualStudioConnector
  - \ref GrpParser
  - \ref GrpExecutor
  - \ref GrpCommonFunctionality
  - \ref GrpMultiThreading
  - \ref GrpSources
