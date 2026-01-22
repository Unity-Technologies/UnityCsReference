// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.BuildService;
using UnityEditor.Compilation;
using UnityEditor.MSBuild;
using Unity.Scripting;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

enum CompileTarget
{
    // Maps to the Debug configuration used by the Editor
    EditorDebug,
    // Maps to the Release configuration used by the Editor
    EditorRelease,
    PlayerDebug,
    PlayerRelease,
    PlayerWithTests
}

class MsBuildCompilation
{
    private MSBuildHostProgram _hostProgram = new MSBuildHostProgram();
    private Task<BuildResultMessage> _currentBuildTask;

    private MSBuildCompilationBuildState _currentBuildState;
    private bool _shouldRestore = false;
    private bool _requestedBuild = false;

    private bool _editorAssembliesMightBeDirty = false;

    private static readonly TimeSpan _connectionTimeout = TimeSpan.FromMinutes(10);
    private ICompilerClient _compilerClient;

    public void Initialize(bool createInitCsprojs, MSBuildCompilationOptions compilationOptions)
    {
        if (createInitCsprojs)
            ProjectGenerator.Instance.GenerateUnityProjectIfMissing(Path.Combine("Assets", "Runtime", "Runtime.csproj"));

        UnityEditorMSBuildPropsTargetsGeneration.UpdateGeneratedMSBuildFileIfNeeded(EditorUserBuildSettings.activeBuildTarget, compilationOptions);

        EnsureCompilerClientInitialized();
    }

    private void EnsureCompilerClientInitialized()
    {
        if (_compilerClient != null)
            return;

        var socketOrNamedPipe = _hostProgram.EnsureRunningAndGetSocketOrNamedPipe();
        _compilerClient = ClientFactory.CreateChannel(socketOrNamedPipe, _connectionTimeout);
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
        EnsureCompilerClientInitialized();

        var buildState = new MSBuildCompilationBuildState(_compilerClient);
        var disableNugetRestore = Application.HasARGV("disable-nuget-restore");

        //throw new NotImplementedException();
        result = new CompilationDoneResult();
        var buildResult = buildState
            .GetLastBuildResultAsync(GetMsBuildConfiguration(target, EditorUserBuildSettings.activeBuildTarget),
                !disableNugetRestore).Result;
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

        // If build is running return compiling
        if (_currentBuildTask != null && !_currentBuildTask.IsCompleted)
            return EditorCompilation.CompileStatus.Compiling;

        // Start a Build if requested
        if (_requestedBuild && _currentBuildTask == null)
        {
            EnsureCompilerClientInitialized();

            _currentBuildState = new MSBuildCompilationBuildState(_compilerClient);
            _requestedBuild = false;

            Console.WriteLine($"Beginning build. Restoring: {_shouldRestore}");

            UnityEditorMSBuildPropsTargetsGeneration.UpdateGeneratedMSBuildFileIfNeeded(buildTarget, compilationOptions);

            var generateBinLog = (bool)UnityEngine.Debug.GetDiagnosticSwitch("ScriptCompilationMsBuildBinlog").value;
            var disableNugetRestore = Application.HasARGV("disable-nuget-restore");

            _currentBuildTask = _currentBuildState.BuildAsync(_shouldRestore, generateBinLog, GetMsBuildConfiguration(target, buildTarget), !disableNugetRestore);

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
                EditorUtility.DisplayCancelableProgressBar("Compiling Scripts", buildEvent.Text ?? "Building...", buildEvent.Progress);
            }

            BuildResultMessage buildResult = _currentBuildTask.Result;
            _editorAssembliesMightBeDirty = true;
            UnityMSBuildLogger.LogCompilerMessages(buildResult, IsEditorTarget(target));
            if (generateBinLog)
            {
                UnityMSBuildLogger.LogProjectBuildManagerMessages(buildResult);
            }

            var compilationStatus = GetCompileStatus(buildResult);

            ReportBuildFinished(target, buildResult);
            _currentBuildState = null;
            _currentBuildTask = null;
            return GetCompileStatus(buildResult);
        }

        if (_currentBuildTask.IsCompletedSuccessfully)
        {
            var buildResult = _currentBuildTask.Result;
            _currentBuildState = null;
            _currentBuildTask = null;

            UnityMSBuildLogger.LogCompilerMessages(buildResult, IsEditorTarget(target));
            ReportBuildFinished(target, buildResult);
            _editorAssembliesMightBeDirty = true;
            return GetCompileStatus(buildResult);
        }

        if (_currentBuildTask.IsCanceled || _currentBuildTask.IsFaulted)
        {
            if (_currentBuildTask.IsFaulted)
                UnityEngine.Debug.LogError("Internal BuildSystem Error: " + _currentBuildTask.Exception);

            _currentBuildState = null;
            _currentBuildTask = null;
            _editorAssembliesMightBeDirty = false;
            return EditorCompilation.CompileStatus.CompilationFailed;
        }

        _currentBuildState = null;
        _currentBuildTask = null;
        return EditorCompilation.CompileStatus.Idle;
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

        // AsmDefToCSProj block removed: DLL not available in reference source
    }

    private string ConvertPath(string path)
    {
        // Work with full paths to not rely on auto resolved relative paths inside to converter library.
        return AssetPath.GetFullPath(path);
    }
}
