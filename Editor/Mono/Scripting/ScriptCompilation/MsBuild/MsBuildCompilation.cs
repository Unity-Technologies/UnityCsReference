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
    [VisibleToOtherModules("UnityEditor.ProjectAuditorModule")]
    internal class CompilationMessage
    {
        public string AssemblyName;
        public LogMessage Msg;
    }

    [VisibleToOtherModules("UnityEditor.ProjectAuditorModule")]
    internal class CompilationAssembly
    {
        public string Name;
        public string OutputPath;
        public List<string> ReferencedAssemblyNames;
    }

    [VisibleToOtherModules("UnityEditor.ProjectAuditorModule")]
    internal class CompilationMessages
    {
        public bool Success;
        public CompilationAssembly[] Assemblies = Array.Empty<CompilationAssembly>();
        public CompilationMessage[] Messages = Array.Empty<CompilationMessage>();
    }

    [NoAutoStaticsCleanup] // singleton managing the external MSBuild host process; persists across reload, the compiler client reconnects
    private static readonly MSBuildHostProgram _hostProgram = new MSBuildHostProgram();
    private Task<BuildResultMessage> _currentBuildTask;

    private MSBuildCompilationBuildState _currentBuildState;
    private MSBuildCompilationBuildState _analysisBuildState;
    private bool _shouldRestore = false;
    private bool _requestedBuild = false;
    private string[] _playerExtraScriptingDefines = Array.Empty<string>();
    private bool _clearConsolePending;

    private bool _editorAssembliesMightBeDirty = false;

    // Snapshotted on every editor TickCompilationPipeline call so RequestMsBuildScriptCompilation
    // can dispatch the build immediately on the request thread (overlapping with asset-refresh
    // postprocess work) without waiting for the next editor tick. Invalid until the first tick.
    private BuildTarget _lastTickBuildTarget;
    private CompileTarget _lastTickCompileTarget;
    private MSBuildCompilationOptions _lastTickCompilationOptions;
    private bool _lastTickParamsValid;

    // What the in-flight build was dispatched with. An early dispatch uses the previous tick's parameters, which are stale
    // after a platform, scripting backend or code optimization change, so a caller with different ones must not consume
    // this task's result as its own.
    private BuildTarget _inflightBuildTarget;
    private CompileTarget _inflightCompileTarget;
    private MSBuildCompilationOptions _inflightCompilationOptions;

    // Computed by the converter during SetAllScripts; written into the shared analyzers props at build start.
    private string[] _globalAnalyzers = Array.Empty<string>();

    // From the same conversion; null until it ran this session.
    private string[] _defineConstraintSymbols;

    private readonly IncrementalAsmDefConverter _converter = new IncrementalAsmDefConverter(Path.Combine("Library", "MSBuild", "AsmDefConverterState.json"));

    private static readonly TimeSpan _connectionTimeout = TimeSpan.FromMinutes(10);
    // The host gives up on its pending work after 8 s.
    private static readonly TimeSpan _hostCleanUpTimeout = TimeSpan.FromSeconds(10);
    static ICompilerClient CompilerClient => _compilerClient.Value;
    [AutoStaticsCleanupOnCodeReload] // reset the lazy client on reload so [OnCodeLoaded] PrimeCompilerClient rebuilds the connection
    private static Lazy<ICompilerClient> _compilerClient = new (() =>
    {
        var socketOrNamedPipe = _hostProgram.EnsureRunningAndGetSocketOrNamedPipe();
        return ClientFactory.CreateChannel(socketOrNamedPipe, _connectionTimeout);
    });

    public void Initialize(MSBuildCompilationOptions compilationOptions)
    {
        UnityEditorMSBuildPropsTargetsGeneration.UpdateInstallPathFile();

        // Only update essential props on initialization; deferrable props will be updated during restore
        UnityEditorMSBuildPropsTargetsGeneration.UpdateEssentialPropsOnly(EditorUserBuildSettings.activeBuildTarget);
    }

    [OnCodeLoaded]
    private static void PrimeCompilerClient()
    {
        // Import workers never compile, and a host started there takes over the Editor's socket.
        if (!MsBuildCompilationInterface.IsEnabled() || AssetDatabase.IsAssetImportWorkerProcess())
            return;

        // No need to wait for this, just kick off the connection so that it's ready when we need it.
        _ = CompilerClient.PrimeConnectionAsync();

        EditorApplication.quitting += FinishHostWorkOnQuit;
    }

    // The host writes the build cache after reporting a build, and dies with the Editor. Letting it finish keeps the
    // next session from rebuilding everything the last one built.
    private static void FinishHostWorkOnQuit()
    {
        if (!_compilerClient.IsValueCreated)
            return;

        try
        {
            // Off the main thread: the client's continuations would otherwise wait for the blocked main thread.
            Task.Run(() => CompilerClient.CleanUpAndExitAsync()).Wait(_hostCleanUpTimeout);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[MSBU] The MSBuild host did not finish its pending work before quitting: {e.Message}");
        }
    }

    public void RequestMsBuildScriptCompilation(bool restore, string reason = null)
    {
        // Matches EditorCompilation.RequestScriptCompilation; the always-on startup build passes no reason
        if (!string.IsNullOrEmpty(reason))
            Console.WriteLine($"[ScriptCompilation] Requested script compilation because: {reason}{(restore ? " (with restore)" : "")}");

        // A package install or a plugin import changes the precompiled assemblies the props list.
        UnityEditorMSBuildPropsTargetsGeneration.InvalidateCache();

        _currentBuildState?.CancellationTokenSource.Cancel();
        _analysisBuildState?.CancellationTokenSource.Cancel();
        _shouldRestore |= restore;
        _requestedBuild = true;

        // Overlap the build dispatch with any remaining asset-refresh postprocess work: this request
        // typically fires from inside AssetDatabase.PostProcessAllAssets' native caller, and the editor
        // main-loop tick that would otherwise pick it up can be ~0.5-1.0 s away. Falls back to tick
        // dispatch when we lack cached tick parameters (first request after reload) or an in-flight
        // build still owns the state (its cancellation must land before the next dispatch).
        // A player build follows its request with a blocking player compile, which an editor build would only delay.
        if (!_lastTickParamsValid || BuildPipeline.isBuildingPlayer)
            return;
        if (_currentBuildTask != null || _currentBuildState != null || _analysisBuildState != null)
            return;

        try
        {
            DispatchBuild(_lastTickBuildTarget, _lastTickCompileTarget, _lastTickCompilationOptions);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[MSBU] Early build dispatch from RequestMsBuildScriptCompilation failed; falling back to tick dispatch: {ex}");
            _currentBuildTask = null;
            _currentBuildState = null;
            _requestedBuild = true;
        }
    }

    public bool HaveScriptsForEditorBeenCompiledSinceLastDomainReload()
    {
        // This is used for TypeCache to refresh.
        // We should find another way. Ie. fast IO check if anything has changed
        return _editorAssembliesMightBeDirty;
    }

    internal void OnCodeLoaded()
    {
        _editorAssembliesMightBeDirty = false;
    }

    public bool IsCompiling()
    {
        return _requestedBuild || _currentBuildState is not null || _analysisBuildState is not null;
    }

    // Like EditorCompilation.IsScriptCompilationRequested: editor assemblies that are not built and loaded yet.
    // An analysis build does not change them.
    public bool IsScriptCompilationPending()
    {
        return _requestedBuild || _currentBuildState is not null;
    }

    // Blocks until the requested target is built. The native caller treats every status but CompilationFailed as success.
    public EditorCompilation.CompileStatus Compile(BuildTarget buildTarget, CompileTarget target, MSBuildCompilationOptions compilationOptions, string[] extraScriptingDefines = null)
    {
        if (_currentBuildTask != null && !IsInflightBuildFor(buildTarget, target, compilationOptions))
        {
            PumpCurrentBuild();
            CompleteInflightBuild();
        }

        // BuildPlayerOptions.extraScriptingDefines: player builds only, like EditorCompilation (UUM-161128)
        _playerExtraScriptingDefines = IsEditorTarget(target) ? Array.Empty<string>() : extraScriptingDefines ?? Array.Empty<string>();

        if (_currentBuildTask == null)
            _requestedBuild = true;
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
        // Only snapshot params from the editor-tick path. Compile() reaches here with player
        // configurations for sync player builds (CompileScriptsForPlayerSync), and we never want
        // RequestMsBuildScriptCompilation (which always compiles for the editor) to early-dispatch
        // using those.
        if (target == CompileTarget.EditorDebug || target == CompileTarget.EditorRelease)
        {
            _lastTickBuildTarget = buildTarget;
            _lastTickCompileTarget = target;
            _lastTickCompilationOptions = compilationOptions;
            _lastTickParamsValid = true;
        }

        if (!_requestedBuild && _currentBuildTask == null)
            return EditorCompilation.CompileStatus.Idle;

        // An analysis build shares the host's per-build state, so a normal compile starts or completes once it finishes
        if (_analysisBuildState != null)
            return EditorCompilation.CompileStatus.Compiling;

        // Drain a build dispatched with stale parameters like Compile() does, then build with this tick's.
        if (_currentBuildTask != null && !IsInflightBuildFor(buildTarget, target, compilationOptions))
        {
            _currentBuildState.CancellationTokenSource.Cancel();
            if (!_currentBuildTask.IsCompleted && !allowBlocking)
                return EditorCompilation.CompileStatus.Compiling;

            if (allowBlocking)
                PumpCurrentBuild();
            CompleteInflightBuild();
            _requestedBuild = true;
        }

        if (_requestedBuild && _currentBuildTask == null)
            DispatchBuild(buildTarget, target, compilationOptions);

        // From here _currentBuildTask != null — either just dispatched above, or dispatched earlier
        // from RequestMsBuildScriptCompilation. Non-blocking callers poll; blocking callers pump.
        while (true)
        {
            if (_clearConsolePending)
            {
                _clearConsolePending = false;
                ConsoleWindow.ClearConsoleOnRecompile();
            }

            if (!_currentBuildTask.IsCompleted && !allowBlocking)
                return EditorCompilation.CompileStatus.Compiling;

            if (allowBlocking)
                PumpCurrentBuild();

            // An editor build superseded by a newer request is not a failure: reporting it as one marks
            // script compilation as failed until the replacement finishes (UUM-161011)
            if (_currentBuildTask.IsCanceled && _requestedBuild && IsEditorTarget(target))
            {
                Console.WriteLine("Build superseded by a newer request; starting the replacement build.");
                _currentBuildTask = null;
                _currentBuildState = null;
                DispatchBuild(buildTarget, target, compilationOptions);
                continue;
            }

            var status = CompleteBuild(target, GetMsBuildConfiguration(target, buildTarget), IsBinLogRequested());

            // The script updater changed sources and requested a rebuild; a blocking caller waits for it too.
            if (status == EditorCompilation.CompileStatus.Compiling)
                return TickCompilationPipeline(buildTarget, allowBlocking, target, compilationOptions);

            return status;
        }
    }

    private bool IsInflightBuildFor(BuildTarget buildTarget, CompileTarget target, MSBuildCompilationOptions compilationOptions) =>
        _inflightBuildTarget == buildTarget && _inflightCompileTarget == target && _inflightCompilationOptions == compilationOptions;

    private void CompleteInflightBuild() =>
        CompleteBuild(_inflightCompileTarget, GetMsBuildConfiguration(_inflightCompileTarget, _inflightBuildTarget), IsBinLogRequested());

    private static bool IsBinLogRequested() =>
        (bool)UnityEngine.Debug.GetDiagnosticSwitch("ScriptCompilationMsBuildBinlog").value || Application.HasARGV("generate-binlog");

    private void PumpCurrentBuild()
    {
        // Pump the build task until it completes
        // This is because we can't write to the Progress bar from a background thread
        while (!_currentBuildTask.IsCompleted || !_currentBuildState.ProgressEvents.IsEmpty)
        {
            if (!_currentBuildState.ProgressEvents.TryDequeue(out var buildEvent))
            {
                // Must block: an unawaited Task.Delay spins this loop, and the timer and thread-pool
                // work it queues starves the continuations that drain the build stream.
                // JIRA: https://jira.unity3d.com/browse/MSBU-530
                Thread.Sleep(1);
                continue;
            }
            // Cancelling aborts the call, which lands the task as Canceled and is the only way
            // out of a build the host has wedged, since nothing times it out by wall clock.
            if (EditorUtility.DisplayCancelableProgressBar("Compiling Scripts", buildEvent.Text ?? "Building...", buildEvent.Progress))
                _currentBuildState.CancellationTokenSource.Cancel();
        }

        EditorUtility.ClearProgressBar();
    }

    // May run on the main thread from either TickCompilationPipeline (editor tick) or
    // RequestMsBuildScriptCompilation (asset-refresh thread, same thread as the request).
    // Must only be called when _currentBuildTask == null and _currentBuildState == null.
    private void DispatchBuild(BuildTarget buildTarget, CompileTarget target, MSBuildCompilationOptions compilationOptions)
    {
        _currentBuildState = new MSBuildCompilationBuildState(CompilerClient);
        _inflightBuildTarget = buildTarget;
        _inflightCompileTarget = target;
        _inflightCompilationOptions = compilationOptions;
        _requestedBuild = false;

        Console.WriteLine($"Beginning build. Restoring: {_shouldRestore}");

        _clearConsolePending = true;

        // Update deferrable props in parallel before build (defines, references, plugins, search paths, analyzers).
        // If a Build runs before any SetAllScripts populated _globalAnalyzers, fall back to the built-in
        // set so the shared analyzers props isn't written with the built-ins missing.
        RegenerateMissingProjects();
        var globalAnalyzers = _globalAnalyzers.Length > 0
            ? _globalAnalyzers
            : UnityEditorMSBuildPropsTargetsGeneration.GetBuiltinRoslynAnalyzerPaths();
        UnityEditorMSBuildPropsTargetsGeneration.EnsureEntryPointProject();
        UnityEditorMSBuildPropsTargetsGeneration.UpdateDeferrablePropsInParallel(buildTarget, compilationOptions, globalAnalyzers, _defineConstraintSymbols, IsEditorTarget(target),
            IsEditorTarget(target) ? null : _playerExtraScriptingDefines);

        var generateBinLog = (bool)UnityEngine.Debug.GetDiagnosticSwitch("ScriptCompilationMsBuildBinlog").value || Application.HasARGV("generate-binlog");
        var disableNugetRestore = Application.HasARGV("disable-nuget-restore");
        var configuration = GetMsBuildConfiguration(target, buildTarget);

        BeginScriptUpdatingForBuild();
        _currentBuildTask = _currentBuildState.BuildAsync(_shouldRestore, generateBinLog, configuration, !disableNugetRestore);

        _shouldRestore = false;
    }

    private EditorCompilation.CompileStatus CompleteBuild(CompileTarget target, string configuration, bool generateBinLog)
    {
        var buildState = _currentBuildState;
        var buildTask = _currentBuildTask;
        _currentBuildState = null;
        _currentBuildTask = null;

        // Player builds (e.g. BuildAssetBundles) must not touch the editor flag, or entering Play Mode reloads the domain
        var isEditorTarget = IsEditorTarget(target);

        // Superseded by a newer request, or cancelled by the user; the replacement is already queued.
        if (buildTask.IsCanceled)
        {
            if (isEditorTarget)
                _editorAssembliesMightBeDirty = false;
            return EditorCompilation.CompileStatus.CompilationFailed;
        }

        if (buildTask.IsFaulted)
        {
            if (isEditorTarget)
                _editorAssembliesMightBeDirty = false;
            UnityMSBuildLogger.LogBuildFailedWithoutResult(buildTask.Exception, buildState?.Elapsed ?? TimeSpan.Zero,
                buildState?.LastProgressText ?? "unknown", configuration, generateBinLog);
            return EditorCompilation.CompileStatus.CompilationFailed;
        }

        var buildResult = buildTask.Result;
        TranslateCircularDependencyErrors(buildResult);
        if (isEditorTarget)
            _editorAssembliesMightBeDirty = true;

        // As with BeeDriver, upgradable warnings are updated too, so a successful build can have updates.
        // Diagnostics the script updater fixed are not reported; the rebuild reports its own.
        if (IsEditorTarget(target) && TryApplyScriptUpdates(buildResult))
            return EditorCompilation.CompileStatus.Compiling;

        UnityMSBuildLogger.LogCompilerMessages(buildResult, IsEditorTarget(target), m_ScriptUpdaterMessages);
        if (generateBinLog)
        {
            UnityMSBuildLogger.LogProjectBuildManagerMessages(buildResult);
        }

        FirePerAssemblyCompilationEvents(target, buildResult);
        ReportBuildFinished(target, buildResult);
        return GetCompileStatus(buildResult);
    }

    // Fires the per-assembly CompilationPipeline events (compilationStarted, assemblyCompilationStarted,
    // assemblyCompilationFinished, compilationFinished) for subscribers that would otherwise be Bee-only.
    // Includes BurstLoader, which under MSBU previously consumed the whole-build buildFinished event and
    // had to re-notify Burst about every assembly in the solution; firing per-assembly events for just the
    // DLLs MSBuild actually (re)wrote drops that to Bee's steady-state cost.
    //
    // Order: compilationStarted, then one round of assemblyCompilationStarted, then one round of
    // assemblyCompilationFinished (raising the internal EditorCompilation event, which bubbles up to the
    // public assemblyCompilationFinished), then compilationFinished. Rounds are not interleaved per-assembly
    // — subscribers must treat the timing of these events as advisory. Bee streams them per-assembly during
    // the build; MSBU batches them at build completion.
    private void FirePerAssemblyCompilationEvents(CompileTarget target, BuildResultMessage buildResult)
    {
        if (!IsEditorTarget(target))
            return;

        var projectResults = buildResult.GetProjectResults();
        var perAssembly = new List<(ScriptAssembly asm, UnityEditor.Compilation.CompilerMessage[] messages)>();

        foreach (var path in buildResult.GetUpdatedOutputFiles())
        {
            if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                continue;

            var assemblyName = Path.GetFileNameWithoutExtension(path);

            ProjectResultMessage projectResult = null;
            string projectFile = null;
            foreach (var kv in projectResults)
            {
                if (kv.Value.AssemblyName == assemblyName)
                {
                    projectResult = kv.Value;
                    projectFile = kv.Key;
                    break;
                }
            }
            if (projectResult == null)
                continue;
            if (projectResult.IsILPostProcessor)
                continue;

            var defineConstants = projectResult.GetDefineConstants();
            var defines = new string[defineConstants.Count];
            for (int i = 0; i < defines.Length; i++)
                defines[i] = defineConstants[i];

            var scriptAssembly = new ScriptAssembly
            {
                Filename = Path.GetFileName(path),
                OutputDirectory = Path.GetDirectoryName(path),
                Defines = defines,
                HasCompileErrors = projectResult.Result == ProjectBuildResultType.Failed,
            };
            perAssembly.Add((scriptAssembly, ConvertLogMessagesToCompilerMessages(projectResult.GetLogMessages(), projectFile)));
        }

        if (perAssembly.Count == 0)
            return;

        var editorCompilation = EditorCompilationInterface.Instance;

        try { editorCompilation.RaiseCompilationStarted(null); }
        catch (Exception e) { UnityEngine.Debug.LogException(e); }

        foreach (var (asm, _) in perAssembly)
            CompilationPipeline.RaiseAssemblyCompilationStarted(asm.FullPath);

        foreach (var (asm, messages) in perAssembly)
        {
            try { editorCompilation.RaiseAssemblyCompilationFinished(asm, messages); }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }

        try { editorCompilation.RaiseCompilationFinished(null); }
        catch (Exception e) { UnityEngine.Debug.LogException(e); }
    }

    private static UnityEditor.Compilation.CompilerMessage[] ConvertLogMessagesToCompilerMessages(IReadOnlyList<LogMessage> logMessages, string projectFile)
    {
        if (logMessages == null || logMessages.Count == 0)
            return Array.Empty<UnityEditor.Compilation.CompilerMessage>();

        var result = new UnityEditor.Compilation.CompilerMessage[logMessages.Count];
        for (int i = 0; i < logMessages.Count; i++)
        {
            var m = logMessages[i];
            result[i] = new UnityEditor.Compilation.CompilerMessage
            {
                message = m.Message,
                file = UnityMSBuildLogger.GetReportedFilePath(m.File, projectFile),
                line = m.LineNumber,
                column = m.ColumnNumber,
                type = m.MessageType switch
                {
                    LogMessageType.Error => UnityEditor.Compilation.CompilerMessageType.Error,
                    LogMessageType.Warning => UnityEditor.Compilation.CompilerMessageType.Warning,
                    _ => UnityEditor.Compilation.CompilerMessageType.Info,
                },
            };
        }
        return result;
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
    private bool m_ProjectFolderHasAnyScripts;
    private string[] m_LastAllScripts;

    // Parsed once per inventory; asmdef-by-name lookups run per postprocessor callback after every reload.
    private CustomScriptAssemblyData[] m_AsmdefData;
    private Dictionary<string, int> m_AsmdefIndexByName;
    private Dictionary<string, int> m_AsmdefIndexByDllName;

    // Inputs for the legacy AssemblyGraphBuilder, which maps a script path to its asmdef; rebuilt when the inventory changes.
    private CustomScriptAssembly[] m_CustomScriptAssemblies;
    private CustomScriptAssemblyReference[] m_CustomScriptAssemblyReferences;

    public void SetAllCustomScriptAssemblyReferenceJsonsContents(string[] allAssemblyReferenceJsons, string[] allAssemblyReferenceJsonContents)
    {
        m_AllAssemblyReferenceJsons = allAssemblyReferenceJsons;
        m_AllAssemblyReferenceJsonContents = allAssemblyReferenceJsonContents;
        m_CustomScriptAssemblyReferences = null;
    }

    public void SetAllCustomScriptAssemblyJsonContents(string[] allAssemblyJsonPaths, string[] allAssemblyJsonContents, string[] guids)
    {
        m_AllAssemblyJsonPaths = allAssemblyJsonPaths;
        m_AllAssemblyJsonContents = allAssemblyJsonContents;
        m_AllAssemblyJsonGuids = guids;
        m_AsmdefData = null;
        m_AsmdefIndexByName = null;
        m_AsmdefIndexByDllName = null;
        m_CustomScriptAssemblies = null;
        m_CustomScriptAssemblyReferences = null;

        ReportAsmdefSetupErrors();
    }

    // The legacy pipeline reports these from LoadingAssemblyDefinition, which MSBU does not run.
    void ReportAsmdefSetupErrors()
    {
        var tracker = EditorCompilationInterface.Instance.CompilationSetupErrorsTracker;
        tracker.ClearCompilationSetupErrors(CompilationSetupErrors.LoadError);

        if (m_AllAssemblyJsonPaths == null || m_AllAssemblyJsonContents == null)
            return;

        EnsureAsmdefIndex();

        var nameByGuid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (m_AllAssemblyJsonGuids != null)
        {
            for (int i = 0; i < m_AsmdefData.Length && i < m_AllAssemblyJsonGuids.Length; i++)
            {
                if (m_AsmdefData[i]?.name != null)
                    nameByGuid[m_AllAssemblyJsonGuids[i]] = m_AsmdefData[i].name;
            }
        }

        for (int i = 0; i < m_AsmdefData.Length; i++)
        {
            var references = m_AsmdefData[i]?.references;
            if (references == null || references.Length < 2)
                continue;

            var names = new string[references.Length];
            for (int j = 0; j < references.Length; j++)
            {
                names[j] = GUIDReference.IsGUIDReference(references[j])
                    ? nameByGuid.GetValueOrDefault(GUIDReference.GUIDReferenceToGUID(references[j]), string.Empty)
                    : references[j];
            }

            var exception = LoadingAssemblyDefinition.CreateDuplicateReferencesException(names, m_AllAssemblyJsonPaths[i]);
            if (exception == null)
                continue;

            tracker.SetCompilationSetupErrors(CompilationSetupErrors.LoadError);
            tracker.ProcessException(exception);
        }
    }

    // Name and asmdef path of every assembly definition, for AssemblyValidation under MSBU.
    public IEnumerable<(string name, string path)> GetAssemblyDefinitionNamesAndPaths()
    {
        if (m_AllAssemblyJsonPaths == null || m_AllAssemblyJsonContents == null)
            yield break;

        EnsureAsmdefIndex();
        for (int i = 0; i < m_AsmdefData.Length; i++)
        {
            var name = m_AsmdefData[i]?.name;
            if (!string.IsNullOrEmpty(name))
                yield return (name, m_AllAssemblyJsonPaths[i]);
        }
    }

    private void EnsureAsmdefIndex()
    {
        if (m_AsmdefIndexByName != null)
            return;

        var len = Math.Min(m_AllAssemblyJsonContents.Length, m_AllAssemblyJsonPaths.Length);
        var data = new CustomScriptAssemblyData[len];
        var byName = new Dictionary<string, int>(len, StringComparer.Ordinal);
        var byDllName = new Dictionary<string, int>(len, StringComparer.Ordinal);
        for (int i = 0; i < len; i++)
        {
            try
            {
                data[i] = CustomScriptAssemblyData.FromJsonNoFieldValidation(m_AllAssemblyJsonContents[i]);
            }
            catch
            {
                continue;
            }
            var name = data[i]?.name;
            if (name == null)
                continue;

            byName.TryAdd(name, i);
            if (name.Length > 0)
                byDllName.TryAdd(name.EndsWith(".dll", StringComparison.Ordinal) ? name : name + ".dll", i);
        }

        m_AsmdefData = data;
        m_AsmdefIndexByDllName = byDllName;
        m_AsmdefIndexByName = byName;
    }

    public void ClearCustomScriptAssemblies()
    {
        // No-op ?
    }

    // The Package Manager can extract a package into the same folder again, which drops its generated projects
    // without the script change that would call SetAllScripts.
    private void RegenerateMissingProjects()
    {
        if (m_LastAllScripts != null && !_converter.AreGeneratedFilesUnchanged())
            SetAllScripts(m_LastAllScripts);
    }

    public bool DoesProjectFolderHaveAnyScripts()
    {
        return m_ProjectFolderHasAnyScripts;
    }

    public void SetAllScripts(string[] allScripts)
    {
        m_LastAllScripts = allScripts;
        m_ProjectFolderHasAnyScripts = allScripts != null && allScripts.Length > 0;

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

        UnityEditorMSBuildPropsTargetsGeneration.InvalidateCache();

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
            designTimeExtensions = GetDesignTimeExtensions(),
            playModeTestRunnerEnabled = PlayerSettings.playModeTestRunnerEnabled,
        };

        var conversionResult = _converter.Convert(context);
        _globalAnalyzers = conversionResult.GlobalAnalyzers;
        _defineConstraintSymbols = conversionResult.DefineConstraintSymbols;

        foreach (var error in context.errors)
        {
            UnityEngine.Debug.LogError(error);
        }

        foreach (var warning in context.warnings)
        {
            UnityEngine.Debug.LogWarning(warning);
        }
    }

    // Matches what the IDE packages listed in the csproj before MSBU generated it.
    internal static string[] GetDesignTimeExtensions()
    {
        var extensions = new List<string>(EditorSettings.projectGenerationBuiltinExtensions);
        extensions.Add("asmdef");
        extensions.AddRange(EditorSettings.projectGenerationUserExtensions);
        return extensions.ToArray();
    }

    internal async Task<CompilationMessages> RequestAnalysisCompilationAsync(string configuration, int parentProgressId, CancellationToken cancellationToken)
    {
        // A blocking caller keeps the editor from ticking, so only wait for the host to finish the in-flight build
        while (_currentBuildTask != null && !_currentBuildTask.IsCompleted)
            await WaitForBuildAsync(_currentBuildTask, cancellationToken);

        // This build doesn't restore, and a requested compile waits for it (see TickCompilationPipeline)
        if (_shouldRestore || _analysisBuildState != null)
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

        var buildState = new MSBuildCompilationBuildState(CompilerClient, parentProgressId);
        _analysisBuildState = buildState;
        using var _ = cancellationToken.Register(() => buildState.CancellationTokenSource.Cancel());
        try
        {
            return await RunScriptCompilationAsync(buildState, configuration);
        }
        catch (OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        finally
        {
            _analysisBuildState = null;
        }
    }

    private static async Task WaitForBuildAsync(Task buildTask, CancellationToken cancellationToken)
    {
        var cancelled = new TaskCompletionSource<bool>();
        using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
        {
            await Task.WhenAny(buildTask, cancelled.Task);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task<CompilationMessages> RunScriptCompilationAsync(MSBuildCompilationBuildState buildState, string configuration)
    {
        // Derive BuildTarget from configuration string for Player analysis (Editor analysis uses the active target)
        BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
        if (configuration.EndsWith("+Analysis", StringComparison.Ordinal))
        {
            if (!Enum.TryParse(configuration.Substring(0, configuration.Length - "+Analysis".Length), out buildTarget))
                throw new ArgumentException($"{configuration} does not map to a known BuildTarget", "configuration");
        }

        // Mirror the regular compilation path (TickCompilationPipeline): refresh the deferrable
        // props (defines, references, plugins, search paths, analyzers) before building.
        RegenerateMissingProjects();
        var globalAnalyzers = _globalAnalyzers.Length > 0
            ? _globalAnalyzers
            : UnityEditorMSBuildPropsTargetsGeneration.GetBuiltinRoslynAnalyzerPaths();
        UnityEditorMSBuildPropsTargetsGeneration.EnsureEntryPointProject();
        UnityEditorMSBuildPropsTargetsGeneration.UpdateDeferrablePropsInParallel(buildTarget, GetAnalysisCompilationOptions(configuration, buildTarget), globalAnalyzers, _defineConstraintSymbols,
            !configuration.EndsWith("+Analysis", StringComparison.Ordinal));

        var generateBinLog = (bool)UnityEngine.Debug.GetDiagnosticSwitch("ScriptCompilationMsBuildBinlog").value
            || Application.HasARGV("generate-binlog");

        var buildTask = buildState.BuildAsync(
            restore: false,
            generateBinLog: generateBinLog,
            configuration: configuration,
            useNugetRestore: false);

        var buildResult = await buildTask;
        UnityMSBuildLogger.LogBuildSummary(buildResult);
        return BuildScriptCompilationMessages(buildResult, configuration);
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

    private static CompilationMessages BuildScriptCompilationMessages(BuildResultMessage buildResult, string configuration)
    {
        var assemblies = new List<CompilationAssembly>();
        var messages = new List<CompilationMessage>();

        foreach (var kvp in buildResult.GetProjectResults())
        {
            var projectResult = kvp.Value;

            // ILPostProcessors are build-time tools, not script assemblies, so they don't belong in the assembly list.
            if (!string.IsNullOrEmpty(projectResult.AssemblyName) && !projectResult.IsILPostProcessor)
            {
                assemblies.Add(new CompilationAssembly
                {
                    Name = projectResult.AssemblyName,
                    OutputPath = Path.GetFullPath(Path.Combine("Library", "MSBuild", "artifacts", "Publish", configuration, projectResult.AssemblyName + ".dll")),
                    ReferencedAssemblyNames = new List<string>(projectResult.GetReferencedAssemblyNames())
                });
            }

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

        // Legacy TryFindCustomScriptAssemblyFromAssemblyReference accepts both "MyAssembly" and
        // "MyAssembly.dll"; asmdef names never carry the extension, so normalize the input.
        var normalized = reference.EndsWith(".dll", StringComparison.Ordinal)
            ? reference.Substring(0, reference.Length - 4)
            : reference;

        EnsureAsmdefIndex();
        return m_AsmdefIndexByName.TryGetValue(normalized, out var index) ? m_AllAssemblyJsonPaths[index] : null;
    }

    // MSBU replacement for EditorCompilation.FindCustomScriptAssemblyFromScriptPath, which sees no asmdefs under MSBU (UUM-161062).
    // Returns null for a script in a predefined assembly such as Assembly-CSharp.
    public CustomScriptAssembly FindCustomScriptAssemblyFromScriptPath(string scriptPath)
    {
        if (string.IsNullOrEmpty(scriptPath) || !EnsureCustomScriptAssemblies())
            return null;

        var graphBuilder = AssemblyGraphBuilderFactory.GetOrCreate(
            Path.Combine(GetProjectDirectory(), "Assets"), m_CustomScriptAssemblies, m_CustomScriptAssemblyReferences);
        foreach (var match in graphBuilder.Match(new[] { ToProjectFullPath(scriptPath) }))
            return match.Key.IsPredefined ? null : match.Key;
        return null;
    }

    private static string GetProjectDirectory() => Directory.GetParent(Application.dataPath).FullName;

    // Same as LoadingAssemblyDefinition and EditorCompilation: the graph builder matches full paths, the inventory is project-relative.
    private static string ToProjectFullPath(string path) =>
        AssetPath.IsPathRooted(path) ? AssetPath.GetFullPath(path) : AssetPath.Combine(GetProjectDirectory(), path);

    // Mirrors LoadingAssemblyDefinition without its validation, which the converter reports under MSBU.
    private bool EnsureCustomScriptAssemblies()
    {
        if (m_CustomScriptAssemblies != null && m_CustomScriptAssemblyReferences != null)
            return true;
        if (m_AllAssemblyJsonPaths == null || m_AllAssemblyJsonContents == null || m_AllAssemblyJsonGuids == null)
            return false;

        EnsureAsmdefIndex();
        var assemblies = new List<CustomScriptAssembly>();
        var assemblyNames = new HashSet<string>(StringComparer.Ordinal);
        var assembliesByGuid = new Dictionary<string, CustomScriptAssembly>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < m_AsmdefData.Length; i++)
        {
            // The graph builder indexes assemblies by name and GUID, so a duplicate would throw; legacy rejects it too.
            if (m_AsmdefData[i]?.name == null || !assemblyNames.Add(m_AsmdefData[i].name))
                continue;

            var guid = i < m_AllAssemblyJsonGuids.Length ? m_AllAssemblyJsonGuids[i] : string.Empty;
            var assembly = CustomScriptAssembly.FromCustomScriptAssemblyData(ToProjectFullPath(m_AllAssemblyJsonPaths[i]), guid, m_AsmdefData[i]);
            if (assembly == null || !assembliesByGuid.TryAdd(assembly.GUID ?? string.Empty, assembly))
                continue;
            assemblies.Add(assembly);
        }

        var references = new List<CustomScriptAssemblyReference>();
        var referenceCount = Math.Min(m_AllAssemblyReferenceJsons?.Length ?? 0, m_AllAssemblyReferenceJsonContents?.Length ?? 0);
        for (int i = 0; i < referenceCount; i++)
        {
            CustomScriptAssemblyReference reference;
            try
            {
                reference = CustomScriptAssemblyReference.FromCustomScriptAssemblyReferenceData(
                    ToProjectFullPath(m_AllAssemblyReferenceJsons[i]), CustomScriptAssemblyReferenceData.FromJson(m_AllAssemblyReferenceJsonContents[i]));
            }
            catch
            {
                continue;
            }

            if (reference == null)
                continue;
            if (GUIDReference.IsGUIDReference(reference.Reference)
                && assembliesByGuid.TryGetValue(GUIDReference.GUIDReferenceToGUID(reference.Reference), out var referencedAssembly))
                reference.Reference = referencedAssembly.Name;
            references.Add(reference);
        }

        m_CustomScriptAssemblies = assemblies.ToArray();
        m_CustomScriptAssemblyReferences = references.ToArray();
        return true;
    }

    // Legacy GetCustomTargetAssemblyFromName accepts both "MyAssembly" and "MyAssembly.dll";
    // mirror that. Returns false if the asmdef isn't in the inventory or the inventory hasn't
    // been seeded yet.
    private bool TryGetTargetAssemblyFromName(string assemblyName, out TargetAssembly targetAssembly, out string asmdefPath)
    {
        targetAssembly = null;
        asmdefPath = null;
        if (string.IsNullOrEmpty(assemblyName)
            || m_AllAssemblyJsonPaths == null
            || m_AllAssemblyJsonContents == null
            || m_AllAssemblyJsonGuids == null)
            return false;

        var key = assemblyName.EndsWith(".dll", StringComparison.Ordinal)
            ? assemblyName
            : assemblyName + ".dll";

        EnsureAsmdefIndex();
        if (!m_AsmdefIndexByDllName.TryGetValue(key, out var i))
            return false;

        var guid = (m_AllAssemblyJsonGuids.Length > i) ? m_AllAssemblyJsonGuids[i] : string.Empty;
        var csa = CustomScriptAssembly.FromCustomScriptAssemblyData(m_AllAssemblyJsonPaths[i], guid, m_AsmdefData[i]);
        if (csa == null)
            return false;

        asmdefPath = m_AllAssemblyJsonPaths[i];
        var dict = EditorBuildRules.CreateTargetAssemblies(new[] { csa });
        return dict != null && dict.TryGetValue(key, out targetAssembly);
    }

    // MSBU replacement for CompilationPipeline.GetDefinesFromAssemblyName. Supplies MsBuildCompilation's
    // own versionMetaDatas rather than EditorCompilation's, which is empty under MSBU.
    public string[] GetDefinesFromAssemblyName(string assemblyName, ScriptAssemblySettings settings)
    {
        if (!TryGetTargetAssemblyFromName(assemblyName, out var targetAssembly, out _))
            return null;
        return EditorCompilationInterface.Instance.GetTargetAssemblyDefines(
            targetAssembly, GetVersionMetaDatas(), settings);
    }

    // MSBU replacement for CompilationPipeline.GetResponseFileDefinesFromAssemblyName. Parses the
    // asmdef's csc.rsp on demand: FromCustomScriptAssemblyData only reads asmdef JSON, so the
    // legacy pipeline's separate UpdateCustomTargetAssembliesResponseFileData pass never runs here.
    public string[] GetResponseFileDefinesFromAssemblyName(string assemblyName)
    {
        if (!TryGetTargetAssemblyFromName(assemblyName, out _, out var asmdefPath))
            return null;

        var pathPrefix = Path.GetDirectoryName(asmdefPath);
        if (string.IsNullOrEmpty(pathPrefix))
            return Array.Empty<string>();

        var rspFileProvider = new UnityEditor.Scripting.Compilers.MicrosoftCSharpResponseFileProvider();
        var rspFile = rspFileProvider.GetCompilerSpecific(pathPrefix) ?? rspFileProvider.GetCompilerSpecific("Assets");
        if (string.IsNullOrEmpty(rspFile))
            return Array.Empty<string>();

        var responseFileContent = UnityEditor.Scripting.Compilers.MicrosoftResponseFileParser.GetResponseFileContent(
            Directory.GetParent(Application.dataPath).FullName, rspFile);
        var compilerOptions = UnityEditor.Scripting.Compilers.MicrosoftResponseFileParser.GetCompilerOptions(responseFileContent);
        return UnityEditor.Scripting.Compilers.MicrosoftResponseFileParser.GetDefines(compilerOptions).ToArray();
    }
}
