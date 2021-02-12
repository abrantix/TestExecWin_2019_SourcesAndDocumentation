\ingroup GrpHiddenPages
\author Gerald Fahrnholz

\defgroup GrpVisualStudioExtension Creating a Visual Studio Extension
@{
\ingroup GrpDesign

- \ref extension_create_project                "Creating the Visual Studio project"
- \ref extension_update                        "Updating a Visual Studio extension at VS Marketplace"
- \ref extension_links                         "General links to Microsoft"

\anchor extension_create_project
<h2>Creating the Visual Studio extension project</h2>

In the following we assume the creation of a separate new tool window for Visual Studio:

- create a new VSIX project within Visual Studio (New Project / Visual C# / Extensibility)
- add a tool window item template (Add / New Item / Visual C# / Extensibility / Custom tool window)
- the generated files can immediately be built
- start of debugging causes the start of an experimental instance of Visual Studio where your extension
  is already installed. In the experimental instance of Visual Studio you can test out the functionality
  without affecting your Visual Studio installation.
- your new tool window can be opened via View / Other Windows / YourWindowName
- the generated VSIX file automatically installs your extension on your computer
- For more detailed info and a step to step guide see 
  [Creating an Extension with a Tool Window] (https://msdn.microsoft.com/en-us/library/dn951126.aspx)

\anchor extension_update
<h2>Updating a Visual Studio extension at VS Marketplace</h2>
- Sign in to Visual Studio Marketplace and navigate to "Manage" page. Besides the version entry of your extension click
  the innocent looking 3 points "...". Within the upcoming context menu select "Edit"
- On the edit page simply use the first point to upload a version. The version number itself will update automatically
  within the web page.
- See also: [How to publish an update to visual studio extension] (https://stackoverflow.com/questions/51298694/how-to-publish-update-to-visual-studio-extension)
- **Changes required soon: Derive from AsyncPackage**<br>
  The package (TestExecWin.TestExecWindowPackage) class in any VSIX supporting Visual Studio 2015 or later should derive from AsyncPackage instead of Package. Read more about using AsyncPackage [here](https://aka.ms/asyncpackage).

\anchor extension_links
<h2>General links to Microsoft</h2>

- [Creating an Extension with a Tool Window] (https://msdn.microsoft.com/en-us/library/dn951126.aspx)
- [Detailed documentation and API reference material for building extensions] (http://aka.ms/d0ru3v)
- [Extension samples on GitHub] (http://aka.ms/pauhge)
- [Watch videos from the product team on Visual Studio extensibility] (http://aka.ms/fwg1ft)
- [Install an optional helper tool that adds extra IDE support for extension authors] (http://aka.ms/ui0qn6)
- [Install an optional helper tool that adds extra IDE support for extension authors] (http://aka.ms/ui0qn6)
- [How to: Migrate Extensibility Projects to Visual Studio 2017] (https://docs.microsoft.com/en-us/visualstudio/extensibility/how-to-migrate-extensibility-projects-to-visual-studio-2017)
- [Walkthrough: Publishing a Visual Studio Extension](https://docs.microsoft.com/de-de/visualstudio/extensibility/walkthrough-publishing-a-visual-studio-extension)
- [How to upgrade extensions to support Visual Studio 2019](https://blogs.msdn.microsoft.com/visualstudio/2018/09/26/how-to-upgrade-extensions-to-support-visual-studio-2019/)
- [Migrate to AsyncPackage](https://github.com/Microsoft/VSSDK-Extensibility-Samples/tree/master/AsyncPackageMigration)
- [Improving the responsiveness](https://blogs.msdn.microsoft.com/visualstudio/2018/05/16/improving-the-responsiveness-of-critical-scenarios-by-updating-auto-load-behavior-for-extensions/)
@}
