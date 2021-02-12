using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExecWin
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new SourceFileParser(null);
            var projectInfo = new ProjectInfo();
            var project = new Project();
            project.SourceDirPath = @"C:\work\AbrantixGit\MSW2\test";
            projectInfo.AddProject(project);
            projectInfo.SelectedProject = project;
            parser.ScanProjectDir(project);
        }
    }
}
