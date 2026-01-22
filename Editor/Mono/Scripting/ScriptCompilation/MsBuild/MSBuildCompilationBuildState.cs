// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.BuildService;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

class BuildProgressEvent
{
    public string? Text { get; set; }
    public float Progress { get; set; }
}

class MSBuildCompilationBuildState
{
    private readonly ICompilerClient _compilerClient;

    public MSBuildCompilationBuildState(ICompilerClient compilerClient)
    {
        _compilerClient = compilerClient;
    }

    public CancellationTokenSource CancellationTokenSource { get; } = new();
    public Task<BuildResultMessage>? ActiveBuildTask { get; set; }

    public ConcurrentQueue<BuildProgressEvent> ProgressEvents { get; } = new();

    private int ProgressId { get; set; }

    public Task<BuildResultMessage> BuildAsync(bool restore, bool generateBinLog, string configuration, bool useNugetRestore)
    {
        if (_compilerClient == null)
            throw new InvalidOperationException("Compiler client is not initialized");

        ProgressId = Progress.Start("Compiling Scripts", "Starting Build", Progress.Options.None);
        System.Console.WriteLine($@"Building with configuration: {configuration}
                                        Generating Binlogs: {generateBinLog}");

        ActiveBuildTask = Task.Run(async () =>
        {
            BuildResultMessage? result = null;
            using (new ProgressScope(ProgressId)){
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                await using var asyncStream = _compilerClient.BuildStream(GetBuildParameters(configuration, useNugetRestore, generateBinLog), CancellationTokenSource.Token);

                await foreach (var response in asyncStream.ReadAllAsync())
                {
                    if (response.StreamEvent != null)
                    {
                        Progress.SetDescription(ProgressId, $"{response.StreamEvent.Project} {response.StreamEvent.Text}");

                        ProgressEvents.Enqueue(new BuildProgressEvent
                        {
                            Text = $"{response.StreamEvent.Project} {response.StreamEvent.Text}",
                        });
                    }
                    else if (response.Result != null)
                    {
                        result = response.Result;
                    }
                }

                sw.Stop();
                Console.WriteLine($"Done Building configuration '{configuration}' ({sw.Elapsed.TotalSeconds}s)");

            }

            return result!;
        }, CancellationTokenSource.Token);

        return ActiveBuildTask;
    }

    public Task<NullableBuildResultMessage> GetLastBuildResultAsync(string configuration, bool useNugetRestore)
    {
        return Task.Run(async () => await _compilerClient.GetLastBuildResultAsync(GetBuildParameters(configuration, useNugetRestore)), CancellationTokenSource.Token);
    }

    private BuildParameters GetBuildParameters(string configuration, bool useNugetRestore, bool generateBinLog = false)
    {
        var dotnetSdk = Path.Combine(EditorApplication.applicationContentsPath, @"DotNetSdk\");
        var rootProject = Path.GetFullPath("Main.EntryPoint.csproj");

        return new BuildParameters
        {
            DotnetPath = dotnetSdk,
            Configuration = configuration,
            RootProject = rootProject,
            GenerateBinLog = generateBinLog,
            UseNugetRestore = useNugetRestore
        };
    }
}
