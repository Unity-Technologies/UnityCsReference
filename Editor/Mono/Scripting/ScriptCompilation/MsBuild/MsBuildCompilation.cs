// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.BuildService;
using UnityEditor.Compilation;
using UnityEditor.MSBuild;
using Unity.AsmDefToCSProj;
using UnityEditor.Build;
using Unity.Scripting;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

// Must be kept in sync (same order/values) with the native CompileTarget enum in
// Editor/Src/ScriptCompilation/ScriptCompilationPipeline.h — it is marshalled by (int).
enum CompileTarget
{
    // Maps to the Debug configuration used by the Editor
    EditorDebug,
    // Maps to the Release configuration used by the Editor
    EditorRelease,
    // Player configurations mirror PlayerSettings.ManagedCodeVariant (Debug/Checked/Instrumented/Release).
    PlayerDebug,
    PlayerChecked,
    PlayerInstrumented,
    PlayerRelease,
    PlayerWithTests
}

[VisibleToOtherModules("UnityEditor.ProjectAuditorModule")]
partial class MsBuildCompilation
{
    internal class CompilationMessage
    {
        public string AssemblyName;
        public LogMessage Msg;
    }

    [VisibleToOtherModules("UnityEditor.ProjectAuditorModule")]
    internal class CompilationMessages
    {
        public bool Success;
        public string[] Assemblies = Array.Empty<string>();
        public CompilationMessage[] Messages = Array.Empty<CompilationMessage>();
    }

    [NoAutoStaticsCleanup] // singleton managing the external MSBuild host process; persists across reload, the compiler client reconnects
    private static readonly MSBuildHostProgram _hostProgram = new MSBuildHostProgram();
    private Task<BuildResultMessage> _currentBuildTask;

    private MSBuildCompilationBuildState _currentBuildState;
    private bool _shouldRestore = false;
    private bool _requestedBuild = false;

    private bool _editorAssembliesMightBeDirty = false;

    // Computed by the converter during SetAllScripts; written into the shared analyzers props at build start.
    private string[] _globalAnalyzers = Array.Empty<string>();

    private readonly IncrementalAsmDefConverter _converter = new IncrementalAsmDefConverter();

    private static readonly TimeSpan _connectionTimeout = TimeSpan.FromMinutes(10);
    static ICompilerClient CompilerClient => _compilerClient.Value;
    [AutoStaticsCleanupOnCodeReload] // reset the lazy client on reload so [OnCodeLoaded] PrimeCompilerClient rebuilds the connection
    private static Lazy<ICompilerClient> _compilerClient = new (() =>
    {
        var socketOrNamedPipe = _hostProgram.EnsureRunningAndGetSocketOrNamedPipe();
        return ClientFactory.CreateChannel(socketOrNamedPipe, _connectionTimeout);
    });

    public void Initialize(bool createInitCsprojs, MSBuildCompilationOptions compilationOptions)
    {
        UnityEditorMSBuildPropsTargetsGeneration.UpdateInstallPathFile();

        if (createInitCsprojs)
            ProjectGenerator.Instance.GenerateUnityProjectIfMissing(Path.Combine("Assets", "Runtime", "Runtime.csproj"));

        // Only update essential props on initialization; deferrable props will be updated during restore
        UnityEditorMSBuildPropsTargetsGeneration.UpdateEssentialPropsOnly(EditorUserBuildSettings.activeBuildTarget);
    }

    [OnCodeLoaded]
    private static void PrimeCompilerClient()
    {
        if (!MsBuildCompilationInterface.IsEnabled())
            return;

        // No need to wait for this, just kick off the connection so that it's ready when we need it.
        _ = CompilerClient.PrimeConnectionAsync();
    }

    public void RequestMsBuildScriptCompilation(bool restore, string reason = null)
    {
        var restoreString = restore ? "/Restore" : "";
        Console.WriteLine($"Requesting Compilation: {reason}\nRestoring: {restoreString}");

        _currentBuildState?.CancellationTokenSource.Cancel();
        _shouldRestore |= restore;
        _requestedBuild = true;
    }

    public bool HaveScriptsForEditorBeenCompiledSinceLastDomainReload()
    {
        // This is used for TypeCache to refresh.
        // We should find another way. Ie. fast IO check if anything has changed
        return _editorAssembliesMightBeDirty;
    }

    public bool IsCompiling()
    {
        return _requestedBuild || _currentBuildState is not null;
    }

    public EditorCompilation.CompileStatus Compile(BuildTarget buildTarget, CompileTarget target, MSBuildCompilationOptions compilationOptions)
    {
        return TickCompilationPipeline(buildTarget, true, target, compilationOptions);
    }



    public bool TryGetLastBuildResult(CompileTarget target, out CompilationDoneResult? result)
    {
        var buildState = new MSBuildCompilationBuildState(CompilerClient);
        var disableNugetRestore = Application.HasARGV("disable-nuget-restore");

        //throw new NotImplementedException();
        result = new CompilationDoneResult();

        NullableBuildResultMessage buildResult;
        try
        {
            buildResult = buildState
                .GetLastBuildResultAsync(GetMsBuildConfiguration(target, EditorUserBuildSettings.activeBuildTarget),
                    !disableNugetRestore).Result;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"Could not query the MSBuild build host for the last build result: {e}");
            result = null;
            return false;
        }

        if (!buildResult.HasValue)
        {
            result = null;
            return false;
        }

        result = GetCompilationDoneResult(target, buildResult.BuildResult);
        return true;
    }

    public EditorCompilation.CompileStatus TickCompilationPipeline(BuildTarget buildTarget, bool allowBlocking, CompileTarget target, MSBuildCompilationOptions compilationOptions)
    {
        if (!_requestedBuild && _currentBuildTask == null)
            return EditorCompilation.CompileStatus.Idle;

        // An out-of-band analysis build (RequestAnalysisCompilationAsync) holds _currentBuildState without a
        // _currentBuildTask. Don't start or post-process a normal compile until it finishes — this serializes
        // the two against the host's shared per-build state. The pending _requestedBuild stays set and starts
        // once the analysis build clears _currentBuildState (or is cancelled by RequestMsBuildScriptCompilation).
        if (_currentBuildTask == null && _currentBuildState != null)
            return EditorCompilation.CompileStatus.Compiling;

        // If build is running return compiling
        if (_currentBuildTask != null && !_currentBuildTask.IsCompleted)
            return EditorCompilation.CompileStatus.Compiling;

        // Start a Build if requested
        if (_requestedBuild && _currentBuildTask == null)
        {
            _currentBuildState = new MSBuildCompilationBuildState(CompilerClient);
            _requestedBuild = false;

            Console.WriteLine($"Beginning build. Restoring: {_shouldRestore}");

            // Update deferrable props in parallel before build (defines, references, plugins, search paths, analyzers).
            // If a Build runs before any SetAllScripts populated _globalAnalyzers, fall back to the built-in
            // set so the shared analyzers props isn't written with the built-ins missing.
            var globalAnalyzers = _globalAnalyzers.Length > 0
                ? _globalAnalyzers
                : UnityEditorMSBuildPropsTargetsGeneration.GetBuiltinRoslynAnalyzerPaths();
            UnityEditorMSBuildPropsTargetsGeneration.UpdateDeferrablePropsInParallel(buildTarget, compilationOptions, globalAnalyzers);

            var generateBinLog = (bool)UnityEngine.Debug.GetDiagnosticSwitch("ScriptCompilationMsBuildBinlog").value || Application.HasARGV("generate-binlog");
            var disableNugetRestore = Application.HasARGV("disable-nuget-restore");
            var configuration = GetMsBuildConfiguration(target, buildTarget);

            _currentBuildTask = _currentBuildState.BuildAsync(_shouldRestore, generateBinLog, configuration, !disableNugetRestore);

            _shouldRestore = false;

            if (!allowBlocking)
                return EditorCompilation.CompileStatus.Compiling;

            // Pump the build task until it completes
            // This is because we can't write to the Progress bar from a background thread
            while (!_currentBuildTask.IsCompleted || !_currentBuildState.ProgressEvents.IsEmpty)
            {
                if (!_currentBuildState.ProgressEvents.TryDequeue(out var buildEvent))
                {
                    //This delay is required to fix the endless loop on macOS when debugger is attached
                    //JIRA: https://jira.unity3d.com/browse/MSBU-530
                    Task.Delay(1);
                    continue;
                }
                // Cancelling aborts the call, which lands the task as Canceled and is the only way
                // out of a build the host has wedged, since nothing times it out by wall clock.
                if (EditorUtility.DisplayCancelableProgressBar("Compiling Scripts", buildEvent.Text ?? "Building...", buildEvent.Progress))
                    _currentBuildState.CancellationTokenSource.Cancel();
            }

            EditorUtility.ClearProgressBar();

            return CompleteBuild(target, configuration, generateBinLog);
        }

        return CompleteBuild(target, GetMsBuildConfiguration(target, buildTarget),
            (bool)UnityEngine.Debug.GetDiagnosticSwitch("ScriptCompilationMsBuildBinlog").value || Application.HasARGV("generate-binlog"));
    }

    private EditorCompilation.CompileStatus CompleteBuild(CompileTarget target, string configuration, bool generateBinLog)
    {
        var buildState = _currentBuildState;
        var buildTask = _currentBuildTask;
        _currentBuildState = null;
        _currentBuildTask = null;

        // Superseded by a newer request, or cancelled by the user; the replacement is already queued.
        if (buildTask.IsCanceled)
        {
            _editorAssembliesMightBeDirty = false;
            return EditorCompilation.CompileStatus.CompilationFailed;
        }

        if (buildTask.IsFaulted)
        {
            _editorAssembliesMightBeDirty = false;
            UnityMSBuildLogger.LogBuildFailedWithoutResult(buildTask.Exception, buildState?.Elapsed ?? TimeSpan.Zero,
                buildState?.LastProgressText ?? "unknown", configuration, generateBinLog);
            return EditorCompilation.CompileStatus.CompilationFailed;
        }

        var buildResult = buildTask.Result;
        _editorAssembliesMightBeDirty = true;

        UnityMSBuildLogger.LogCompilerMessages(buildResult, IsEditorTarget(target));
        if (generateBinLog)
        {
            UnityMSBuildLogger.LogProjectBuildManagerMessages(buildResult);
        }

        ReportBuildFinished(target, buildResult);
        return GetCompileStatus(buildResult);
    }

    private CompilationDoneResult GetCompilationDoneResult(CompileTarget target, BuildResultMessage buildResult)
    {
        // Report defines, this is temporary so until Burst is integrated into MSBuild
        Dictionary<string, IReadOnlyList<string>> assemblyDefines = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var projectResult in buildResult.GetProjectResults())
        {
            // Since we only use this for burst, it was giving issues that we was giving the information about ILPP as well.
            // So we only add the defines for the projects that are not ILPP
            if (!projectResult.Value.IsILPostProcessor)
                assemblyDefines.TryAdd(projectResult.Value.AssemblyName, projectResult.Value.GetDefineConstants());
        }

        return new CompilationDoneResult(
                IsEditorTarget(target),
                buildResult.Success,
                buildResult.GetUpdatedOutputFiles(),
                buildResult.GetDeletedOutputFiles(),
                assemblyDefines);
    }

    private void ReportBuildFinished(CompileTarget target, BuildResultMessage buildResult)
    {
        CompilationPipeline.ReportBuildFinished(target, GetCompilationDoneResult(target, buildResult));
    }

    private static EditorCompilation.CompileStatus GetCompileStatus(BuildResultMessage buildResult)
    {
        return buildResult.Success ? EditorCompilation.CompileStatus.CompilationComplete : EditorCompilation.CompileStatus.CompilationFailed;
    }

    private static bool IsEditorTarget(CompileTarget target)
    {
        return target == CompileTarget.EditorDebug || target == CompileTarget.EditorRelease;
    }

    private static string GetMsBuildConfiguration(CompileTarget target, BuildTarget buildTarget)
    {
        switch (target)
        {
            case CompileTarget.EditorDebug:
                return "Debug";
            case CompileTarget.EditorRelease:
                return "Release";
            case CompileTarget.PlayerDebug:
                return $"{buildTarget}+Debug";
            case CompileTarget.PlayerChecked:
                return $"{buildTarget}+Checked";
            case CompileTarget.PlayerInstrumented:
                return $"{buildTarget}+Instrumented";
            case CompileTarget.PlayerRelease:
                return $"{buildTarget}+Release";
            case CompileTarget.PlayerWithTests:
                return $"{buildTarget}+Tests";
            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, null);
        }
    }

    private string[] m_AllAssemblyReferenceJsons;
    private string[] m_AllAssemblyReferenceJsonContents;
    private string[] m_AllAssemblyJsonPaths;
    private string[] m_AllAssemblyJsonContents;
    private string[] m_AllAssemblyJsonGuids;

    public void SetAllCustomScriptAssemblyReferenceJsonsContents(string[] allAssemblyReferenceJsons, string[] allAssemblyReferenceJsonContents)
    {
        m_AllAssemblyReferenceJsons = allAssemblyReferenceJsons;
        m_AllAssemblyReferenceJsonContents = allAssemblyReferenceJsonContents;
    }

    public void SetAllCustomScriptAssemblyJsonContents(string[] allAssemblyJsonPaths, string[] allAssemblyJsonContents, string[] guids)
    {
        m_AllAssemblyJsonPaths = allAssemblyJsonPaths;
        m_AllAssemblyJsonContents = allAssemblyJsonContents;
        m_AllAssemblyJsonGuids = guids;
    }

    public void ClearCustomScriptAssemblies()
    {
        // No-op ?
    }

    public void SetAllScripts(string[] allScripts)
    {
        var allAssemblyReferenceJsons = new string[m_AllAssemblyReferenceJsons.Length];
        for (int i = 0; i < m_AllAssemblyReferenceJsons.Length; i++)
        {
            allAssemblyReferenceJsons[i] = ConvertPath(m_AllAssemblyReferenceJsons[i]);
        }

        var allAssemblyJsonPaths = new string[m_AllAssemblyJsonPaths.Length];
        for (int i = 0; i < m_AllAssemblyJsonPaths.Length; i++)
        {
            allAssemblyJsonPaths[i] = ConvertPath(m_AllAssemblyJsonPaths[i]);
        }

        GetUserPrecompiledAssembliesForConverter(out var precompiledPaths, out var precompiledExplicitlyReferenced);

        var context = new ConversionContext()
        {
            RootFolder = ".",
            allAssemblyReferenceJsons = allAssemblyReferenceJsons,
            allAssemblyReferenceJsonContents = m_AllAssemblyReferenceJsonContents,
            allAssemblyJsonPaths = allAssemblyJsonPaths,
            allAssemblyJsonContents = m_AllAssemblyJsonContents,
            allAssemblyJsonGuids = m_AllAssemblyJsonGuids,
            allScriptPaths = allScripts,
            precompiledAssemblyPaths = precompiledPaths,
            precompiledAssemblyExplicitlyReferenced = precompiledExplicitlyReferenced,
            errors = new List<string>(),
            warnings = new List<string>(),
            // Built-ins live outside the project, so the converter can't discover them; supply them here.
            builtinAnalyzerPaths = UnityEditorMSBuildPropsTargetsGeneration.GetBuiltinRoslynAnalyzerPaths(),
            // The editor's analyzer registry: lets the converter classify analyzer DLLs without reading
            // .meta files, and invalidates its incremental cache when analyzers/labels change.
            roslynAnalyzerPaths = new PrecompiledAssemblyProvider().GetRoslynAnalyzerPaths(),
        };

        var conversionResult = _converter.Convert(context);
        _globalAnalyzers = conversionResult.GlobalAnalyzers;

        foreach (var error in context.errors)
        {
            UnityEngine.Debug.LogError(error);
        }

        foreach (var warning in context.warnings)
        {
            UnityEngine.Debug.LogWarning(warning);
        }
    }

    internal async Task<CompilationMessages> RequestAnalysisCompilationAsync(string configuration, CancellationToken cancellationToken)
    {
        // Reject if a build is already active or pending; conversely, once this build owns
        // _currentBuildState the tick loop won't start a normal compile until it finishes (see
        // TickCompilationPipeline). Reusing the same fields the normal path uses means IsCompiling() reports
        // true and a later RequestMsBuildScriptCompilation cancels this build cleanly.
        if (_requestedBuild || _currentBuildTask != null || _currentBuildState != null)
        {
            return new CompilationMessages
            {
                Success = false,
                Messages =
                [
                    new CompilationMessage
                    {
                        Msg = new LogMessage()
                        {
                            MessageType = LogMessageType.Error,
                            Message = "An MSBuild compilation is already in progress; retry once it completes."
                        }
                    },
                ],
            };
        }

        _currentBuildState = new MSBuildCompilationBuildState(CompilerClient);
        using var _ = cancellationToken.Register(() => _currentBuildState.CancellationTokenSource.Cancel());
        try
        {
            return await RunScriptCompilationAsync(configuration);
        }
        catch (OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        finally
        {
            _currentBuildState = null;
        }
    }

    private async Task<CompilationMessages> RunScriptCompilationAsync(string configuration)
    {
        // Mirror the regular compilation path (TickCompilationPipeline): refresh the deferrable
        // props (defines, references, plugins, search paths, analyzers) before building.
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        var globalAnalyzers = _globalAnalyzers.Length > 0
            ? _globalAnalyzers
            : UnityEditorMSBuildPropsTargetsGeneration.GetBuiltinRoslynAnalyzerPaths();
        UnityEditorMSBuildPropsTargetsGeneration.UpdateDeferrablePropsInParallel(buildTarget, GetAnalysisCompilationOptions(configuration, buildTarget), globalAnalyzers);

        var generateBinLog = (bool)UnityEngine.Debug.GetDiagnosticSwitch("ScriptCompilationMsBuildBinlog").value
            || Application.HasARGV("generate-binlog");

        var buildTask = _currentBuildState.BuildAsync(
            restore: false,
            generateBinLog: generateBinLog,
            configuration: configuration,
            useNugetRestore: false);

        var buildResult = await buildTask;
        return BuildScriptCompilationMessages(buildResult);
    }

    // Map EditorCompilationInterface.GetManagedCodeVariantOptions to the MSBuild flags
    private static MSBuildCompilationOptions GetAnalysisCompilationOptions(string configuration, BuildTarget buildTarget)
    {
        EditorScriptCompilationOptions variantOptions;
        if (configuration.EndsWith("+Analysis", StringComparison.Ordinal))
        {
            // Player-target analysis: match the active target's player build (variant + IL2CPP backend, which
            // managed option sets never carry so it's added explicitly).
            var namedBuildTarget = NamedBuildTarget.FromActiveSettings(buildTarget);
            variantOptions = EditorCompilationInterface.GetManagedCodeVariantOptions(namedBuildTarget);
            if (PlayerSettings.GetScriptingBackend(namedBuildTarget) == ScriptingImplementation.IL2CPP)
                variantOptions |= EditorScriptCompilationOptions.BuildingForIl2Cpp;
        }
        else
        {
            // Editor-target 'Analysis': match the editor build, whose managed code variant is Debug when
            // script debug info is on (codeOptimization Debug) and Checked otherwise — see
            // ScriptCompilationPipeline.cpp GetManagedCodeVariantForCompile. That yields asserts +
            // instrumentation always, plus no-optimization (DEBUG) in Debug.
            var editorVariant = CompilationPipeline.codeOptimization == CodeOptimization.Debug
                ? ManagedCodeVariant.Debug
                : ManagedCodeVariant.Checked;
            variantOptions = EditorCompilationInterface.GetManagedCodeVariantOptions(editorVariant);
        }

        var options = MSBuildCompilationOptions.BuildingEmpty;
        if (variantOptions.HasFlag(EditorScriptCompilationOptions.BuildingForIl2Cpp))
            options |= MSBuildCompilationOptions.BuildingForIl2Cpp;
        if (variantOptions.HasFlag(EditorScriptCompilationOptions.BuildingWithAsserts))
            options |= MSBuildCompilationOptions.BuildingWithAsserts;
        if (variantOptions.HasFlag(EditorScriptCompilationOptions.BuildingWithInstrumentation))
            options |= MSBuildCompilationOptions.BuildingWithInstrumentation;
        if (variantOptions.HasFlag(EditorScriptCompilationOptions.BuildingWithoutOptimization))
            options |= MSBuildCompilationOptions.BuildingWithDebug;

        return options;
    }

    private static CompilationMessages BuildScriptCompilationMessages(BuildResultMessage buildResult)
    {
        var assemblies = new List<string>();
        var messages = new List<CompilationMessage>();

        foreach (var kvp in buildResult.GetProjectResults())
        {
            var projectResult = kvp.Value;
            if (!string.IsNullOrEmpty(projectResult.AssemblyName))
                assemblies.Add(projectResult.AssemblyName);

            foreach (var msg in projectResult.GetLogMessages())
            {
                if (msg.MessageType == LogMessageType.Message)
                    continue;
                messages.Add(new CompilationMessage
                {
                    AssemblyName = projectResult.AssemblyName,
                    Msg = msg
                });
            }
        }

        foreach (var msg in buildResult.GetLogMessages())
        {
            if (msg.MessageType == LogMessageType.Message)
                continue;
            messages.Add(new CompilationMessage
            {
                AssemblyName = null,
                Msg = msg
            });
        }

        return new CompilationMessages
        {
            Success = buildResult.Success,
            Assemblies = assemblies.ToArray(),
            Messages = messages.ToArray(),
        };
    }

    private string ConvertPath(string path)
    {
        // Work with full paths to not rely on auto resolved relative paths inside to converter library.
        return AssetPath.GetFullPath(path);
    }

    // Hands the converter the auto-referenced plugin set the editor already knows about, so it doesn't
    // have to walk the filesystem or parse .meta files itself. The native layer in PrecompiledAssemblies.cpp
    // already filters hidden folders, parses meta, and tags AssemblyFlags.ExplicitlyReferenced.
    //
    // Filter to UserAssembly only: Unity modules (UnityEngine.dll, etc.) are wired by the SDK via
    // EnableUnityEngineReferences; emitting them as bare references would duplicate.
    private static void GetUserPrecompiledAssembliesForConverter(out string[] paths, out bool[] explicitlyReferenced)
    {
        var provider = new PrecompiledAssemblyProvider();
        var all = provider.GetAllPrecompiledAssemblies();
        var pathList = new List<string>(all.Length);
        var explicitList = new List<bool>(all.Length);
        foreach (var pa in all)
        {
            if ((pa.Flags & AssemblyFlags.UserAssembly) != AssemblyFlags.UserAssembly)
                continue;
            pathList.Add(pa.Path);
            explicitList.Add((pa.Flags & AssemblyFlags.ExplicitlyReferenced) == AssemblyFlags.ExplicitlyReferenced);
        }
        paths = pathList.ToArray();
        explicitlyReferenced = explicitList.ToArray();
    }

    private AssetPathMetaData[] m_AssetPathsMetaData;
    private Dictionary<string, VersionMetaData> m_VersionMetaDatas;

    public void SetAssetPathsMetaData(AssetPathMetaData[] assetPathMetaDatas)
    {
        m_AssetPathsMetaData = assetPathMetaDatas;

        if (assetPathMetaDatas == null)
        {
            m_VersionMetaDatas = null;
            return;
        }

        var versionMetaDataComparer = new VersionMetaDataComparer();
        var seen = new HashSet<VersionMetaData>(versionMetaDataComparer);
        var versionMetaDatas = new Dictionary<string, VersionMetaData>(assetPathMetaDatas.Length);
        foreach (var assetPathMetaData in assetPathMetaDatas)
        {
            var versionMetaData = assetPathMetaData.VersionMetaData;
            if (versionMetaData == null)
                continue;
            if (!seen.Add(versionMetaData))
                continue;
            versionMetaDatas.Add(versionMetaData.Name, versionMetaData);
        }
        m_VersionMetaDatas = versionMetaDatas;
    }

    public void SetAdditionalVersionMetaDatas(VersionMetaData[] versionMetaDatas)
    {
        UnityEngine.Assertions.Assert.IsTrue(m_VersionMetaDatas != null, "MsBuildCompilation.SetAssetPathsMetaData() must be called before MsBuildCompilation.SetAdditionalVersionMetaDatas()");
        foreach (var versionMetaData in versionMetaDatas)
        {
            m_VersionMetaDatas[versionMetaData.Name] = versionMetaData;
        }
    }

    public AssetPathMetaData[] GetAssetPathsMetaData() => m_AssetPathsMetaData;
    public Dictionary<string, VersionMetaData> GetVersionMetaDatas() => m_VersionMetaDatas;

    // Mirror of EditorCompilation.TryFindCustomScriptAssemblyFromAssemblyReference for the MSBuild
    // pipeline. The MSBuild side doesn't parse asmdef JSON into CustomScriptAssembly objects; the
    // raw JSON is stashed for the SDK converter, so we resolve over those arrays directly.
    public string TryGetAssemblyDefinitionFilePathFromReference(string reference)
    {
        if (string.IsNullOrEmpty(reference)
            || m_AllAssemblyJsonPaths == null
            || m_AllAssemblyJsonContents == null)
            return null;

        if (GUIDReference.IsGUIDReference(reference))
        {
            if (m_AllAssemblyJsonGuids == null)
                return null;

            var guid = GUIDReference.GUIDReferenceToGUID(reference);
            var count = Math.Min(m_AllAssemblyJsonGuids.Length, m_AllAssemblyJsonPaths.Length);
            for (int i = 0; i < count; i++)
            {
                if (string.Equals(m_AllAssemblyJsonGuids[i], guid, StringComparison.OrdinalIgnoreCase))
                    return m_AllAssemblyJsonPaths[i];
            }
            return null;
        }

        var len = Math.Min(m_AllAssemblyJsonContents.Length, m_AllAssemblyJsonPaths.Length);
        for (int i = 0; i < len; i++)
        {
            CustomScriptAssemblyData data;
            try
            {
                data = CustomScriptAssemblyData.FromJsonNoFieldValidation(m_AllAssemblyJsonContents[i]);
            }
            catch
            {
                continue;
            }
            if (data != null && string.Equals(data.name, reference, StringComparison.Ordinal))
                return m_AllAssemblyJsonPaths[i];
        }
        return null;
    }
}
