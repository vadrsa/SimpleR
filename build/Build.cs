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
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MinVer;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

[SuppressMessage("ReSharper", "AllUnderscoreLocalParameterName")]
[GitHubActions(
    "RunTestsOnPR",
    GitHubActionsImage.UbuntuLatest,
    OnPullRequestBranches = ["develop", "master"],
    InvokedTargets = [nameof(RunUnitTests)])]
[GitHubActions(
    "PublishProtocolPackage",
    GitHubActionsImage.UbuntuLatest,
    On = [GitHubActionsTrigger.WorkflowDispatch],
    FetchDepth = 0,
    InvokedTargets = [nameof(PublishProtocolPackage)],
    ImportSecrets = [nameof(NugetApiKey)])]
[GitHubActions(
    "PublishCorePackage",
    GitHubActionsImage.UbuntuLatest,
    On = [GitHubActionsTrigger.WorkflowDispatch],
    FetchDepth = 0,
    InvokedTargets = [nameof(PublishCorePackage)],
    ImportSecrets = [nameof(NugetApiKey)])]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main () => Execute<Build>(x => x.Compile);
    
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild
            ? Configuration.Debug
            : Configuration.Release;

    [Parameter]
    readonly AbsolutePath TestResultDirectory = RootDirectory / ".nuke/Artifacts/Test-Results/";
    
    [Parameter]
    readonly AbsolutePath ArtifactsDirectory = RootDirectory + ".nuke/Artifacts/";
    
    [Parameter]
    readonly AbsolutePath NugetDirectory = RootDirectory + ".nuke/Artifacts/nuget/";
    
    [MinVer]
    readonly MinVer MinVer;
    
    [Parameter]
    readonly string NugetApiUrl = "https://api.nuget.org/v3/index.json"; //default
    [Parameter]
    [Secret]
    readonly string NugetApiKey;
    
    [Solution]
    readonly Solution Solution;
    GitHubActions GitHubActions => GitHubActions.Instance;

    Target Print => _ => _
        .Executes(() =>
        {
            Log.Information("Configuration = {Configuration}", Configuration);
            Log.Information("GitHub Workflow = {Workflow}", GitHubActions?.Workflow);
            Log.Information("Branch = {Branch}", GitHubActions?.Ref);
            Log.Information("Commit = {Commit}", GitHubActions?.Sha);
            Log.Information("MinVer = {Value}", MinVer.Version);
            
        });
    
    Target Clean => _ => _
        .DependsOn(Print)
        .Executes(() =>
        {
            DotNetTasks.DotNetClean(_ => _
                .SetConfiguration(Configuration)
                .SetProject(Solution));
        });

    Target Restore => _ => _
        .DependsOn(Clean)
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
    Target RunUnitTests =>
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
    
    Target PackCore => _ => _
        .DependsOn(RunUnitTests)
        .Executes(() =>
        {
            DotNetTasks.DotNetPack(s =>
            {
                var proj = Solution.AllProjects.FirstOrDefault(x => x.Name == "SimpleR") ??
                           throw new NullReferenceException("Failed to find SimpleR project");
                return s
                    .SetProject(proj)
                    .SetConfiguration(Configuration.Publish)
                    .SetVersion(MinVer.Version)
                    .SetOutputDirectory(NugetDirectory);
            });
        });
    
    Target PackProtocol => _ => _
        .DependsOn(RunUnitTests)
        .Executes(() =>
        {
            DotNetTasks.DotNetPack(s =>
            {
                var proj = Solution.AllProjects.FirstOrDefault(x => x.Name == "SimpleR.Protocol") ??
                                     throw new NullReferenceException("Failed to find SimpleR.Protocol project");
                return s
                    .SetProject(proj)
                    .SetConfiguration(Configuration.Publish)
                    .SetVersion(MinVer.Version)
                    .SetOutputDirectory(NugetDirectory);
            });
        });
    
    Target PublishProtocolPackage => _ => _
        .DependsOn(PackProtocol)
        .Requires(() => NugetApiUrl)
        .Requires(() => NugetApiKey)
        .Executes(() =>
        {
            NugetDirectory.GlobFiles("*.nupkg")
                .Where(x => !x.Name.EndsWith("symbols.nupkg"))
                .ForEach(x =>
                {
                    DotNetTasks.DotNetNuGetPush(s => s
                        .SetTargetPath(x)
                        .SetSource(NugetApiUrl)
                        .SetApiKey(NugetApiKey)
                    );
                });
        });
    
    
    Target PublishCorePackage => _ => _
        .DependsOn(PackCore)
        .Requires(() => NugetApiUrl)
        .Requires(() => NugetApiKey)
        .Executes(() =>
        {
            NugetDirectory.GlobFiles("*.nupkg")
                .Where(x => !x.Name.EndsWith("symbols.nupkg"))
                .ForEach(x =>
                {
                    DotNetTasks.DotNetNuGetPush(s => s
                        .SetTargetPath(x)
                        .SetSource(NugetApiUrl)
                        .SetApiKey(NugetApiKey)
                    );
                });
        });
    
}
