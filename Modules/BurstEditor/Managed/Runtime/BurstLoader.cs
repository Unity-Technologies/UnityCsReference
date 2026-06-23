// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code.
using System.Linq;
#pragma warning restore UA2001
using System.Reflection;
using Unity.Burst.LowLevel;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.Scripting;
using UnityEditor.Scripting.ScriptCompilation;
using System.Runtime.CompilerServices;
using UnityEditor.PackageManager;

[assembly: InternalsVisibleTo("Unity.Burst")]
[assembly: InternalsVisibleTo("Unity.Burst.Editor")]

namespace Unity.Burst.Editor
{
    /// <summary>
    /// Main entry point for initializing the burst compiler service for both JIT and AOT
    /// </summary>
    internal partial class BurstLoader
    {
        // Cache the delegate to make sure it doesn't get collected.
        private static readonly BurstCompilerService.ExtractCompilerFlags TryGetOptionsFromMemberDelegate = TryGetOptionsFromMember;

        /// <summary>
        /// Gets the location to the runtime path of burst.
        /// </summary>
        public static string RuntimePath { get; private set; }

        public static BclConfiguration BclConfiguration { get; private set; }

        public static bool IsDebugging { get; private set; }

        private static bool UnityBurstRuntimePathOverwritten(out string path)
        {
            path = Environment.GetEnvironmentVariable("UNITY_BURST_RUNTIME_PATH");
            return Directory.Exists(path);
        }

        private const string msbuildReadySessionKey = "BURST_MSBUILD_READY";
        private const string packagesChangedSessionKey = "BURST_PACKAGES_CHANGED";

        private static CompilationTaskReason _currentBuildKind;

        // We need this to be queried each domain reload in a static constructor so that it is called on the main thread only!
        private static readonly bool IsScriptDebugInfoEnabled = UnityEditor.Compilation.CompilationPipeline.IsScriptDebugInfoEnabled();

        [Unity.Scripting.LifecycleManagement.BurstInitialize]
        static void InitBurstLoader()
        {
            BurstCompiler.AllocateDelegateHandles();

            BurstCompiler.IsScriptDebugInfoEnabled = IsScriptDebugInfoEnabled;
            if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Secondary
                || UnityEditor.AssetDatabase.IsAssetImportWorkerProcess())
            {
                BurstCompilerOptions.IsSecondaryUnityProcess = true;
            }

            if (BurstCompilerOptions.ForceDisableBurstCompilation)
            {
                if (!BurstCompilerOptions.IsSecondaryUnityProcess)
                {
                    UnityEngine.Debug.LogWarning("[com.unity.burst] Burst is disabled entirely from the command line");
                }
                return;
            }

            // This can be setup to get more diagnostics
            var debuggingStr = Environment.GetEnvironmentVariable("UNITY_BURST_DEBUG");
            IsDebugging = debuggingStr != null && int.TryParse(debuggingStr, out var debugLevel) && debugLevel > 0;
            if (IsDebugging)
            {
                UnityEngine.Debug.LogWarning("[com.unity.burst] Extra debugging is turned on.");
            }

            // Try to load the runtime through an environment variable
            var isRuntimePathOverwritten = UnityBurstRuntimePathOverwritten(out var path);
            if (!isRuntimePathOverwritten)
            {
                // Otherwise try to load it from the package itself
                // On macOS: Contents/Resources/Burst/Client
                // On Windows/Linux: Data/Tools/Burst/Client
                var toolsSubfolder = Application.platform == RuntimePlatform.OSXEditor ? "Resources" : "Tools";
                path = Path.Combine(EditorApplication.applicationContentsPath, toolsSubfolder, "Burst", "Client");
            }

            RuntimePath = path;

            BclConfiguration = GetBclConfiguration(path, isRuntimePathOverwritten);

            if (IsDebugging)
            {
                UnityEngine.Debug.LogWarning($"[com.unity.burst] Runtime directory set to {RuntimePath}");
            }

            BurstCompilerService.Initialize(RuntimePath, TryGetOptionsFromMemberDelegate);

            var dotnetPath = Environment.GetEnvironmentVariable("UNITY_BURST_DOTNET_PATH")
                             ?? NetCoreProgram.GetDotNetMuxerPath();

            BurstCompiler.Initialize(
                dotnetPath,
                RuntimePath,
                EditorApplication.applicationToolsPath,
                GetAssemblyFolders(),
                BurstAssemblyDisable.GetDisabledAssemblies(BurstAssemblyDisable.DisableType.Editor, ""));

            // It's important that this call comes *after* BurstCompilerService.Initialize,
            // otherwise any calls from within EnsureSynchronized to BurstCompilerService,
            // such as BurstCompiler.Disable(), will silently fail.
            BurstEditorOptions.EnsureSynchronized();

            EditorApplication.quitting += OnEditorApplicationQuitting;

            UnityEditor.Compilation.CompilationPipeline.compilationStarted += OnCompilationStarted;
            UnityEditor.Compilation.CompilationPipeline.compilationFinished += OnCompilationFinished;

            // We use this internal event because it's the only way to get access to the ScriptAssembly.HasCompileErrors,
            // which tells us whether C# compilation succeeded or failed for this assembly.
            EditorCompilationInterface.Instance.assemblyCompilationFinished += OnAssemblyCompilationFinished;

            UnityEditor.Compilation.CompilationPipeline.assemblyCompilationNotRequired += OnAssemblyCompilationNotRequired;

            EditorApplication.playModeStateChanged += EditorApplicationOnPlayModeStateChanged;

            Events.registeringPackages += PackageRegistrationEvent;

            // Notify the compiler about a domain reload
            if (IsDebugging)
            {
                UnityEngine.Debug.Log("Burst - Domain Reload");
            }

            BurstCompiler.OnProgress += OnProgress;

            BurstCompiler.EagerCompilationLoggingEnabled = true;

            // Make sure BurstRuntime is initialized. This needs to happen before BurstCompiler.DomainReload,
            // because that can cause calls to BurstRuntime.Log.
            BurstRuntime.Initialize();

            // Apply FloatMode from BurstAotSettings to JIT compilation
            var aotSettings = BurstPlatformAotSettings.GetOrCreateSettings(EditorUserBuildSettings.activeBuildTarget, false);
            BurstCompiler.Options.JitFloatMode = aotSettings.FloatMode;

            // Notify the JitCompilerService about a domain reload
            BurstCompiler.SetDefaultOptions();

            // We need to send the list of assemblies if
            // (a) we have never done that before in this Editor instance, or
            // (b) we have done it before, but now the scripting code optimization mode has changed
            //     from Debug to Release or vice-versa.
            // This is because these are the two cases in which CompilerClient will be
            // destroyed and recreated.
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code.
            var assemblyNamesAndDefines = (IEnumerable<(string, string[])>)Array.Empty<(string, string[])>();
#pragma warning restore UA2001

            if (BurstCompilerService.DequeuePendingBurstLoad())
            {
                const string sessionKey = "BURST_INITIALIZED";
                var isInitialedEditorSetup = SessionState.GetBool(sessionKey, false);

                // Avoid getting the assemblies on the very first code reload
                if (!isInitialedEditorSetup)
                {
                    SessionState.SetBool(sessionKey, true);
                }
                else
                {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code.
                    // Gather list of assemblies to compile (only actually used at Editor startup)
                    assemblyNamesAndDefines = UnityEditor.Compilation.CompilationPipeline
                        .GetAssemblies(UnityEditor.Compilation.AssembliesType.Editor)
                        .Where(x => File.Exists(x.outputPath)) // If C# compilation fails, it won't exist on disk
                        .Select(x => (x.name, x.defines));
#pragma warning restore UA2001
                }
            }

            var packagesChanged = SessionState.GetBool(packagesChangedSessionKey, false);
            BurstCompiler.DomainReload(assemblyNamesAndDefines, packagesChanged);
            SessionState.SetBool(packagesChangedSessionKey, false);

            BurstCompiler.OnBeginProgressBar += (msg) => EditorUtility.DisplayProgressBar("Burst", msg, -1);
            BurstCompiler.OnEndProgressBar += EditorUtility.ClearProgressBar;

            BurstCompiler.OnProfileBegin += OnProfileBegin;
            BurstCompiler.OnProfileEnd += OnProfileEnd;
            BurstCompiler.SetProfilerCallbacks();

#pragma warning disable CS0618 // ManagedDebugger.isEnabled is obsolete on CoreCLR (always true)
            if (ManagedDebugger.isEnabled)
                BurstCompiler.InitialiseDebuggerHooks();
#pragma warning restore CS0618
        }

        private static void PackageRegistrationEvent(PackageRegistrationEventArgs obj)
        {
            SessionState.SetBool(packagesChangedSessionKey, true);
        }

        private static bool _isQuitting;
        private static void OnEditorApplicationQuitting()
        {
            _isQuitting = true;
        }

        public static Action OnBurstShutdown;

        private static BclConfiguration GetBclConfiguration(string runtimePath, bool isRuntimePathOverwritten)
        {
            if (isRuntimePathOverwritten)
            {
                var bclDllFilePath = Path.Combine(runtimePath, "bcl.dll");
                if (!File.Exists(bclDllFilePath))
                {
                    bclDllFilePath = Path.Combine(runtimePath, "bcl", "bcl.dll");
                }

                var isDotNet = File.Exists(bclDllFilePath);
                if (!isDotNet)
                {
                    bclDllFilePath = Path.Combine(runtimePath, "bcl", "bcl.dll");
                    isDotNet = File.Exists(bclDllFilePath);
                }
                var executablePath = isDotNet
                    ? bclDllFilePath
                    : Path.Combine(runtimePath, "bcl.exe");
                return new BclConfiguration
                {
                    FolderPath = runtimePath,
                    ExecutablePath = executablePath,
                    IsDotNet = isDotNet,
                };
            }
            else
            {
                var bclFolderPath = Path.Combine(runtimePath, "bcl");
                if (Directory.Exists(bclFolderPath))
                {
                    return new BclConfiguration
                    {
                        FolderPath = bclFolderPath,
                        ExecutablePath = Path.Combine(bclFolderPath, "bcl.dll"),
                        IsDotNet = true,
                    };
                }

                return new BclConfiguration
                {
                    FolderPath = runtimePath,
                    ExecutablePath = Path.Combine(runtimePath, "bcl.exe"),
                    IsDotNet = false,
                };
            }
        }

        // Don't initialize to 0 because that could be a valid progress ID.
        private static int BurstProgressId = -1;

        // If this enum changes, update the benchmarks tool accordingly as we rely on integer value related to this enum
        internal enum BurstEagerCompilationStatus
        {
            NotScheduled,
            Scheduled,
            Completed
        }

        // For the time being, this field is only read through reflection
        internal static BurstEagerCompilationStatus EagerCompilationStatus;

        private static void OnProgress(int current, int total)
        {
            if (current == total)
            {
                EagerCompilationStatus = BurstEagerCompilationStatus.Completed;
            }

            // OnProgress is called from a background thread,
            // but we need to update the progress UI on the main thread.
            EditorApplication.CallDelayed(() =>
            {
                if (current == total)
                {
                    // We've finished - remove progress bar.
                    if (Progress.Exists(BurstProgressId))
                    {
                        Progress.Remove(BurstProgressId);
                        BurstProgressId = -1;
                    }
                }
                else
                {
                    // Do we need to create the progress bar?
                    if (!Progress.Exists(BurstProgressId))
                    {
                        BurstProgressId = Progress.Start(
                            "Burst",
                            "Compiling...",
                            Progress.Options.Unmanaged);
                    }

                    Progress.Report(
                        BurstProgressId,
                        current / (float)total,
                        $"Compiled {current} / {total} libraries");
                }
            });
        }

        [ThreadStatic]
        private static Dictionary<string, IntPtr> ProfilerMarkers;

        private static unsafe void OnProfileBegin(string markerName, string metadataName, string metadataValue)
        {
            if (ProfilerMarkers == null)
            {
                // Initialize thread-static dictionary.
                ProfilerMarkers = new Dictionary<string, IntPtr>();
            }

            if (!ProfilerMarkers.TryGetValue(markerName, out var markerPtr))
            {
                ProfilerMarkers.Add(markerName, markerPtr = ProfilerUnsafeUtility.CreateMarker(
                    markerName,
                    ProfilerUnsafeUtility.CategoryScripts,
                    MarkerFlags.Script,
                    metadataName != null ? 1 : 0));

                // metadataName is assumed to be consistent for a given markerName.
                if (metadataName != null)
                {
                    ProfilerUnsafeUtility.SetMarkerMetadata(
                        markerPtr,
                        0,
                        metadataName,
                        (byte)ProfilerMarkerDataType.String16,
                        (byte)ProfilerMarkerDataUnit.Undefined);
                }
            }

            if (metadataName != null && metadataValue != null)
            {
                fixed (char* methodNamePtr = metadataValue)
                {
                    var metadata = new ProfilerMarkerData
                    {
                        Type = (byte)ProfilerMarkerDataType.String16,
                        Size = ((uint)metadataValue.Length + 1) * 2,
                        Ptr = methodNamePtr
                    };
                    ProfilerUnsafeUtility.BeginSampleWithMetadata(markerPtr, 1, &metadata);
                }
            }
            else
            {
                ProfilerUnsafeUtility.BeginSample(markerPtr);
            }
        }

        private static void OnProfileEnd(string markerName)
        {
            if (ProfilerMarkers == null)
            {
                // If we got here it means we had a domain reload between when we called profile begin and
                // now profile end, and so we need to bail out.
                return;
            }

            if (!ProfilerMarkers.TryGetValue(markerName, out var markerPtr))
            {
                return;
            }

            ProfilerUnsafeUtility.EndSample(markerPtr);
        }

        private static void EditorApplicationOnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (IsDebugging)
            {
                UnityEngine.Debug.Log($"Burst - Change of Editor State: {state}");
            }

            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    // Cleanup any loaded burst natives so users have a clean point to update the libraries.
                    BurstCompiler.UnloadAdditionalLibraries();
                    break;
            }
        }

        enum CompilationTaskReason
        {
            IsForEditor,                // Compilation should proceed as its for an editor build
            IsForPlayer,                // Skip this compilation
            IsForPreviousScriptingMode, // We are about to enter a domain reload, don't start any new compilations
            IsForAssemblyBuilder,       // Request is coming from an 'AssemblyBuilder' and should be skipped as not supported
        }

        static CompilationTaskReason CurrentCompilationTaskShouldStart()
        {
            try
            {
                if (BurstCompilerService.WasScriptDebugInfoEnabledAtDomainReload != UnityEditor.Compilation.CompilationPipeline.IsScriptDebugInfoEnabled())
                {
                    // If the scripting compilation mode has changed since we last had our domain reloaded, then we ignore all requests, and act as if
                    //loading for the first time. This is to avoid having compilations kick off right before a Shutdown triggered by domain reload, that
                    //would cause the a significant stall as we had to wait for those compilations to finish, thus blocking the main thread.
                    return CompilationTaskReason.IsForPreviousScriptingMode;
                }

                var inst = EditorCompilationInterface.Instance;

                var editorCompilationType = inst.GetType();
                var activeBeeBuildField = editorCompilationType.GetField("_currentBeeScriptCompilationState", BindingFlags.Instance | BindingFlags.NonPublic);
                if (activeBeeBuildField == null)
                {
                    activeBeeBuildField = editorCompilationType.GetField("activeBeeBuild", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                var activeBeeBuild = activeBeeBuildField.GetValue(inst);

                // If a user is doing an `AssemblyBuilder` compilation, we do not support that in Burst.
                // This seems to manifest as a null `activeBeeBuild`, so we bail here if that happens.
                if (activeBeeBuild == null)
                {
                    return CompilationTaskReason.IsForAssemblyBuilder;
                }

                var settings = activeBeeBuild.GetType().GetProperty("settings", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance).GetValue(activeBeeBuild);
                var opt = (EditorScriptCompilationOptions)settings.GetType().GetProperty("CompilationOptions").GetValue(settings);

                if ((opt & EditorScriptCompilationOptions.BuildingSkipCompile) != 0)
                {
                    return CompilationTaskReason.IsForPlayer;
                }

                if ((opt & EditorScriptCompilationOptions.BuildingForEditor) != 0)
                {
                    return CompilationTaskReason.IsForEditor;
                }

                return CompilationTaskReason.IsForPlayer;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("Burst - Unknown private compilation pipeline API\nAssuming editor build\n" + ex.ToString());

                return CompilationTaskReason.IsForEditor;
            }
        }

        private static void OnCompilationStarted(object value)
        {
            _currentBuildKind = CurrentCompilationTaskShouldStart();
            if (_currentBuildKind != CompilationTaskReason.IsForEditor)
            {
                if (IsDebugging)
                {
                    UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - not handling '{value}' because '{_currentBuildKind}'");
                }
                return;
            }

            if (IsDebugging)
            {
                UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - compilation started for '{value}'");
            }

            BurstCompiler.NotifyCompilationStarted(GetAssemblyFolders(),
                BurstAssemblyDisable.GetDisabledAssemblies(BurstAssemblyDisable.DisableType.Editor,"") );
        }

        private static string[] GetAssemblyFolders()
        {
            var assemblyFolders = new HashSet<string>();

            // First, we get the path to Mono system libraries. This will be something like
            // <EditorPath>/Data/MonoBleedingEdge/lib/mono/unityjit-win32
            //
            // You might think we could use MonoLibraryHelpers.GetSystemReferenceDirectories
            // here, but we can't, because that returns the _reference assembly_ directories,
            // not the actual implementation assembly directory.
            var systemLibraryDirectory = Path.GetDirectoryName(typeof(object).Assembly.GetLoadedAssemblyPath());
            assemblyFolders.Add(systemLibraryDirectory);

            // Also add the Facades directory, since that contains netstandard. Without this,
            // we'll potentially resolve the "wrong" netstandard from a dotnet compiler host.
            // The Facades directory is only a thing for the Mono editor!
            assemblyFolders.Add(Path.Combine(systemLibraryDirectory, "Facades"));

            // Now add the default assembly search paths.
            // This will include
            // - Unity dlls in <EditorPath>/Data/Managed and <EditorPath>/Data/Managed/UnityEngine
            // - Platform support dlls e.g. <EditorPath>/Data/PlaybackEngines/WindowsStandaloneSupport
            // - Package paths. These are interesting because they are "virtual" paths, of the form
            //   Packages/<MyPackageName>. They need to be resolved to physical paths.
            // - Library/ScriptAssemblies. This needs to be resolved to the full path.
            var defaultAssemblySearchPaths = AssemblyHelper.GetDefaultAssemblySearchPaths();
            // In CoreCLR editor we don't actually get the Managed dlls from defaultAssemblySearchPaths!
            assemblyFolders.Add(Path.Combine(EditorApplication.applicationContentsPath, "Managed"));
            foreach (var searchPath in defaultAssemblySearchPaths)
            {
                var resolvedPath = FileUtil.PathToAbsolutePath(searchPath);
                if (!string.IsNullOrEmpty(resolvedPath))
                {
                    assemblyFolders.Add(resolvedPath);
                }
            }

            if (IsDebugging)
            {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code.
                UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - AssemblyFolders : \n{string.Join("\n", assemblyFolders)}");
#pragma warning restore UA2001
            }

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code.
            return assemblyFolders.ToArray();
#pragma warning restore UA2001
        }

        private static void OnCompilationFinished(object value)
        {
            if (_currentBuildKind!=CompilationTaskReason.IsForEditor)
            {
                if (IsDebugging)
                {
                    UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - ignoring finished compilation '{value}' because it's '{_currentBuildKind}'");
                }

                _currentBuildKind = CompilationTaskReason.IsForEditor;
                return;
            }

            if (IsDebugging)
            {
                UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - compilation finished for '{value}'");
            }

            BurstCompiler.NotifyCompilationFinished();
        }

        private static void OnAssemblyCompilationFinished(ScriptAssembly assembly, CompilerMessage[] messages)
        {
            if (_currentBuildKind!=CompilationTaskReason.IsForEditor)
            {
                if (IsDebugging)
                {
                    UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - ignoring '{assembly.Filename}' because it's '{_currentBuildKind}'");
                }

                return;
            }

            if (IsDebugging)
            {
                UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - Assembly compilation finished for '{assembly.Filename}'");
            }

            if (assembly.HasCompileErrors)
            {
                if (IsDebugging)
                {
                    UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - ignoring '{assembly.Filename}' because it failed C# compilation");
                }

                return;
            }

            BurstCompiler.NotifyAssemblyCompilationFinished(Path.GetFileNameWithoutExtension(assembly.Filename), assembly.Defines);
        }

        private static void OnAssemblyCompilationNotRequired(string arg1)
        {
            if (_currentBuildKind!=CompilationTaskReason.IsForEditor)
            {
                if (IsDebugging)
                {
                    UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - ignoring '{arg1}' because it's '{_currentBuildKind}'");
                }

                return;
            }

            if (IsDebugging)
            {
                UnityEngine.Debug.Log($"{DateTime.UtcNow} Burst - Assembly compilation not required for '{arg1}'");
            }

            BurstCompiler.NotifyAssemblyCompilationNotRequired(Path.GetFileNameWithoutExtension(arg1));
        }

        private static bool TryGetOptionsFromMember(MemberInfo member, out string flagsOut)
        {
            return BurstCompiler.Options.TryGetOptions(member, out flagsOut);
        }

        //[Scripting.LifecycleManagement.OnCodeUnloading]
        private static void BeforeCodeUnload()
        {
            if (IsDebugging)
            {
                UnityEngine.Debug.Log($"Burst - BeforeCodeUnload");
            }

            BurstCompiler.FreeGCHandles();
        }


        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        private static void BeforeAssembliesUnload()
        {
            if (IsDebugging)
            {
                UnityEngine.Debug.Log($"Burst - OnDomainUnload");
            }

            BurstCompiler.Cancel();

            // This check here is to execute shutdown after all OnDisable's. EditorApplication.quitting event is called before OnDisable's, so we need to shutdown in here.
            if (_isQuitting)
            {
                BurstCompiler.Shutdown();
            }

            // Because of a check in Unity (specifically SCRIPTINGAPI_THREAD_AND_SERIALIZATION_CHECK),
            // we are not allowed to call thread-unsafe methods (like Progress.Exists) after the
            // kApplicationTerminating bit has been set. And because the domain is unloaded
            // (thus triggering AppDomain.DomainUnload) *after* that bit is set, we can't call Progress.Exists
            // during shutdown. So we check _isQuitting here. When quitting, it's fine for the progress item
            // not to be removed since it's all being torn down anyway.
            if (!_isQuitting && Progress.Exists(BurstProgressId))
            {
                Progress.Remove(BurstProgressId);
                BurstProgressId = -1;
            }
        }
    }

    internal class BclConfiguration
    {
        public string FolderPath { get; set; }
        public string ExecutablePath { get; set; }
        public bool IsDotNet { get; set; }
    }
}
