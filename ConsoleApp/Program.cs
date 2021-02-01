using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CodeAnalysisApp2
{
    class Program
    {
        private static void MSBuildWorkspaceSetup()
        {
            // Attempt to set the version of MSBuild.
            var instance = MSBuildLocator.QueryVisualStudioInstances().First();

            Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

            // NOTE: Be sure to register an instance with the MSBuildLocator 
            //       before calling MSBuildWorkspace.Create()
            //       otherwise, MSBuildWorkspace won't MEF compose.
            MSBuildLocator.RegisterInstance(instance);
        }

        private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress loadProgress)
            {
                var projectDisplay = Path.GetFileName(loadProgress.FilePath);
                if (loadProgress.TargetFramework != null)
                {
                    projectDisplay += $" ({loadProgress.TargetFramework})";
                }

                Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }

        static async Task Main(string[] args)
        {
            var curDirInfo = new DirectoryInfo(Environment.CurrentDirectory);
            var pathToProject = curDirInfo.EnumerateFileSystemInfos("WebApp.csproj", new EnumerationOptions() {RecurseSubdirectories = true}).First();
            MSBuildWorkspaceSetup();
            using (var workspace = MSBuildWorkspace.Create())
            {
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);

                // project is .net core web api (netcoreapp3.1)
                var project = await workspace.OpenProjectAsync(pathToProject.FullName, new ConsoleProgressReporter());
                var compilation = await project.GetCompilationAsync();
                var allCompilationErrors = compilation.GetDiagnostics();

                using (var stream = new MemoryStream())
                {
                    var result = compilation.Emit(stream);
                }
            }
        }
    }
}
