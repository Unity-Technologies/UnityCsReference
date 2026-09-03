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

    private readonly System.Diagnostics.Stopwatch _elapsed = new();

    public TimeSpan Elapsed => _elapsed.Elapsed;

    /// <summary>The last thing the host said it was doing; reported when a build fails to complete.</summary>
    public string LastProgressText { get; private set; } = "starting build";

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
                _elapsed.Restart();

                await using var asyncStream = _compilerClient.BuildStream(GetBuildParameters(configuration, useNugetRestore, generateBinLog), CancellationTokenSource.Token);

                await foreach (var response in asyncStream.ReadAllAsync())
                {
                    if (response.StreamEvent != null)
                    {
                        LastProgressText = $"{response.StreamEvent.Project} {response.StreamEvent.Text}";
                        Progress.SetDescription(ProgressId, LastProgressText);

                        ProgressEvents.Enqueue(new BuildProgressEvent
                        {
                            Text = LastProgressText,
                        });
                    }
                    else if (response.Result != null)
                    {
                        result = response.Result;
                    }
                }

                _elapsed.Stop();
                Console.WriteLine($"Done Building configuration '{configuration}' ({_elapsed.Elapsed.TotalSeconds}s)");

            }

            // The result is the last message on the stream, so reaching the end without one means the
            // host went away. Fail loudly rather than returning a null that NREs further downstream.
            if (result == null)
            {
                throw new InvalidOperationException(
                    $"The MSBuild build host closed the connection without reporting a build result "
                    + $"(last activity: '{LastProgressText}' after {_elapsed.Elapsed:hh\\:mm\\:ss}).");
            }

            return result;
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
