using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

[SuppressMessage("ReSharper", "AllUnderscoreLocalParameterName")]
[GitHubActions(
    "Run Unit Tests on PR",
    GitHubActionsImage.UbuntuLatest,
    OnPullRequestBranches = ["main"],
    InvokedTargets = [nameof(RunUnitTests)])]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter]
    private readonly AbsolutePath TestResultDirectory = RootDirectory + "/.nuke/Artifacts/Test-Results/";
    
    [Solution]
    private readonly Solution Solution;
    
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetClean(_ => _
                .SetConfiguration(Configuration)
                .SetProject(Solution));
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(_ => _
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(_ => _
                .SetProjectFile(Solution)
                .SetNoRestore(true)
                .SetConfiguration(Configuration));
        });
    
    /// <summary>
    /// It will run all the unit tests
    /// Run directly : cmd> nuke RunUnitTests
    /// </summary>
    private Target RunUnitTests =>
        _ =>
            _.DependsOn(Compile)
                .Executes(() =>
                {
                    var testProjects = Solution.AllProjects.Where(x => x.Name.EndsWith(".Tests"));
                    DotNetTasks.DotNetTest(
                        a =>
                            a.SetConfiguration(Configuration)
                                .SetNoBuild(true)
                                .SetNoRestore(true)
                                .ResetVerbosity()
                                .SetResultsDirectory(TestResultDirectory)
                                .EnableCollectCoverage()
                                .SetCoverletOutputFormat(CoverletOutputFormat.opencover)
                                .EnableUseSourceLink()
                                .CombineWith(
                                    testProjects,
                                    (b, z) =>
                                        b.SetProjectFile(z)
                                            .AddLoggers($"trx;LogFileName={z.Name}.trx")
                                            .SetCoverletOutput(TestResultDirectory + $"{z.Name}.xml")
                                )
                    );
                });
}
