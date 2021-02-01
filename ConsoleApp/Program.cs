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
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances.Length == 1
                // If there is only one instance of MSBuild on this machine, set that as the one to use.
                ? visualStudioInstances[0]
                // Handle selecting the version of MSBuild you want to use.
                : SelectVisualStudioInstance(visualStudioInstances);

            Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

            // NOTE: Be sure to register an instance with the MSBuildLocator 
            //       before calling MSBuildWorkspace.Create()
            //       otherwise, MSBuildWorkspace won't MEF compose.
            MSBuildLocator.RegisterInstance(instance);
        }

        private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Console.WriteLine("Multiple installs of MSBuild detected please select one:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Console.WriteLine($"Instance {i + 1}");
                Console.WriteLine($"    Name: {visualStudioInstances[i].Name}");
                Console.WriteLine($"    Version: {visualStudioInstances[i].Version}");
                Console.WriteLine($"    MSBuild Path: {visualStudioInstances[i].MSBuildPath}");
            }

            while (true)
            {
                var userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out int instanceNumber) &&
                    instanceNumber > 0 &&
                    instanceNumber <= visualStudioInstances.Length)
                {
                    return visualStudioInstances[instanceNumber - 1];
                }
                Console.WriteLine("Input not accepted, try again.");
            }
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
            var pathToProject = @"C:\Users\marol\source\repos\CodeAnalysisApp2\WebApplication2\WebApplication2.csproj";
            MSBuildWorkspaceSetup();
            using (var workspace = MSBuildWorkspace.Create())
            {
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);

                // project is .net core web api (netcoreapp3.1)
                var project = await workspace.OpenProjectAsync(pathToProject, new ConsoleProgressReporter());
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
