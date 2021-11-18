// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NiceIO;
using Bee.BeeDriver;
using ScriptCompilationBuildProgram.Data;
using Unity.Profiling;
using UnityEditor.Compilation;
using UnityEditor.Modules;
using UnityEditor.Scripting.Compilers;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using CompilerMessage = UnityEditor.Scripting.Compilers.CompilerMessage;
using CompilerMessageType = UnityEditor.Scripting.Compilers.CompilerMessageType;
using Directory = System.IO.Directory;
using File = System.IO.File;
using UnityEditor.Build;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal interface IEditorCompilation
    {
        PrecompiledAssemblyProviderBase PrecompiledAssemblyProvider { get; set; }

        ScriptAssembly[] GetAllScriptAssemblies(
            EditorScriptCompilationOptions options,
            PrecompiledAssembly[] unityAssembliesArg,
            Dictionary<string, PrecompiledAssembly> precompiledAssembliesArg,
            string[] defines);
    }

    class EditorCompilation : IEditorCompilation
    {
        const int kLogIdentifierFor_EditorMessages = 1234;
        const int kLogIdentifierFor_PlayerMessages = 1235;

        public enum CompileStatus
        {
            Idle,
            Compiling,
            CompilationStarted,
            CompilationFailed,
            CompilationComplete
        }

        public enum DeleteFileOptions
        {
            NoLogError = 0,
            LogError = 1,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TargetAssemblyInfo
        {
            public string Name;
            public AssemblyFlags Flags;
        }

        public struct CustomScriptAssemblyAndReference
        {
            public CustomScriptAssembly Assembly;
            public CustomScriptAssembly Reference;
        }

        public PrecompiledAssemblyProviderBase PrecompiledAssemblyProvider { get; set; } = new PrecompiledAssemblyProvider();
        public ICompilationSetupErrorsTracker CompilationSetupErrorsTracker { get; set; } = new CompilationSetupErrorsTracker();
        public ResponseFileProvider ResponseFileProvider { get; set; } = new MicrosoftCSharpResponseFileProvider();
        public ILoadingAssemblyDefinition loadingAssemblyDefinition { get; set; } = new LoadingAssemblyDefinition();
        public IVersionDefinesConsoleLogs VersionDefinesConsoleLogs { get; set; } = new VersionDefinesConsoleLogs();
        public ICompilationSetupWarningTracker CompilationSetupWarningTracker { get; set; } = new CompilationSetupWarningTracker();
        public ISafeModeInfo SafeModeInfo { get; set; } = new SafeModeInfo();
        public bool EnableDiagnostics => (bool)Debug.GetDiagnosticSwitch("EnableDomainReloadTimings").value;

        internal string projectDirectory = string.Empty;
        Dictionary<string, string> allScripts = new Dictionary<string, string>();

        bool m_ScriptsForEditorHaveBeenCompiledSinceLastDomainReload;
        RequestScriptCompilationOptions? m_ScriptCompilationRequest = null;
        List<CustomScriptAssemblyReference> customScriptAssemblyReferences = new List<CustomScriptAssemblyReference>();
        Dictionary<string, TargetAssembly> customTargetAssemblies = new Dictionary<string, TargetAssembly>(); // TargetAssemblies for customScriptAssemblies.
        PrecompiledAssembly[] unityAssemblies;

        string outputDirectory;
        bool skipCustomScriptAssemblyGraphValidation = false;
        List<AssemblyBuilder> assemblyBuilders = new List<AssemblyBuilder>();
        bool _logCompilationMessages = true;

        AssetPathMetaData[] m_AssetPathsMetaData;
        Dictionary<string, VersionMetaData> m_VersionMetaDatas;
        private readonly CachedVersionRangesFactory<UnityVersion> m_UnityVersionRanges = new CachedVersionRangesFactory<UnityVersion>();
        private readonly CachedVersionRangesFactory<SemVersion> m_SemVersionRanges = new CachedVersionRangesFactory<SemVersion>();

        public event Action<object> compilationStarted;
        public event Action<object> compilationFinished;
        public event Action<ScriptAssembly, UnityEditor.Compilation.CompilerMessage[]> assemblyCompilationFinished;

        class BeeScriptCompilationState
        {
            public BeeDriver Driver;
            public ScriptAssembly[] assemblies;

            public ScriptAssemblySettings settings { get; set; }
        }

        BeeScriptCompilationState activeBeeBuild;
        CompilerMessage[] _currentEditorCompilationCompilerMessages = new CompilerMessage[0];
        public bool IsRunningRoslynAnalysisSynchronously { get; private set; }

        public void SetProjectDirectory(string projectDirectory)
        {
            this.projectDirectory = projectDirectory;
        }

        public Exception[] SetAssetPathsMetaData(AssetPathMetaData[] assetPathMetaDatas)
        {
            m_AssetPathsMetaData = assetPathMetaDatas;

            var versionMetaDataComparer = new VersionMetaDataComparer();

            m_VersionMetaDatas = assetPathMetaDatas ?
                .Where(x => x.VersionMetaData != null)
                .Select(x => x.VersionMetaData)
                .Distinct(versionMetaDataComparer)
                .ToDictionary(x => x.Name, x => x);

            return UpdateCustomTargetAssembliesAssetPathsMetaData(
                loadingAssemblyDefinition.CustomScriptAssemblies,
                assetPathMetaDatas,
                forceUpdate: true);
        }

        public void SetAdditionalVersionMetaDatas(VersionMetaData[] versionMetaDatas)
        {
            Assert.IsTrue(m_VersionMetaDatas != null, "EditorCompilation.SetAssetPathsMetaData() must be called before EditorCompilation.SetAdditionalVersionMetaDatas()");
            foreach (var versionMetaData in versionMetaDatas)
            {
                m_VersionMetaDatas[versionMetaData.Name] = versionMetaData;
            }
        }

        public AssetPathMetaData[] GetAssetPathsMetaData()
        {
            return m_AssetPathsMetaData;
        }

        public Dictionary<string, VersionMetaData> GetVersionMetaDatas()
        {
            return m_VersionMetaDatas;
        }

        public void SetAllScripts(string[] allScripts, string[] assemblyFilenames)
        {
            this.allScripts = new Dictionary<string, string>();

            for (int i = 0; i < allScripts.Length; ++i)
            {
                this.allScripts[allScripts[i]] = assemblyFilenames[i];
            }
        }

        public bool HaveScriptsForEditorBeenCompiledSinceLastDomainReload()
        {
            return m_ScriptsForEditorHaveBeenCompiledSinceLastDomainReload;
        }

        public void RequestScriptCompilation(string reason = null, RequestScriptCompilationOptions options = RequestScriptCompilationOptions.None)
        {
            if (reason != null)
            {
                Console.WriteLine($"[ScriptCompilation] Requested script compilation because: {reason}");
            }
            PrecompiledAssemblyProvider.Dirty();
            m_ScriptCompilationRequest = options;
            CancelActiveBuild();
        }

        internal static void CleanCache()
        {
            new NPath("Library/Bee").DeleteIfExists(DeleteMode.Soft);
            Mono.Utils.Pram.PramDataDirectory.DeleteIfExists(DeleteMode.Soft);
        }

        public void SetAllUnityAssemblies(PrecompiledAssembly[] unityAssemblies)
        {
            this.unityAssemblies = unityAssemblies;
        }

        // Burst package depends on this method, so we can't remove it.
        public void SetCompileScriptsOutputDirectory(string directory)
        {
            outputDirectory = directory;
        }

        public string GetCompileScriptsOutputDirectory()
        {
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new Exception("Must set an output directory through SetCompileScriptsOutputDirectory before compiling");
            }
            return outputDirectory;
        }

        //Used by the TestRunner package.
        public PrecompiledAssembly[] GetAllPrecompiledAssemblies()
        {
            return PrecompiledAssemblyProvider.GetPrecompiledAssemblies(true, EditorUserBuildSettings.activeBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget);
        }

        Dictionary<string, PrecompiledAssembly> GetPrecompiledAssembliesDictionaryWithSetupErrorsTracking(bool isEditor, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, string[] extraScriptingDefines = null)
        {
            Dictionary<string, PrecompiledAssembly> precompiledAssemblies;
            CompilationSetupErrorsTracker.ClearCompilationSetupErrors(CompilationSetupErrors.PrecompiledAssemblyError); // this will also remove the console errors associated with the setup error flags set in the past
            try
            {
                precompiledAssemblies = PrecompiledAssemblyProvider.GetPrecompiledAssembliesDictionary(isEditor, buildTargetGroup, buildTarget, extraScriptingDefines);
            }
            catch (PrecompiledAssemblyException exception)
            {
                CompilationSetupErrorsTracker.ProcessPrecompiledAssemblyException(exception);
                throw;
            }

            return precompiledAssemblies;
        }

        public PrecompiledAssembly[] GetPrecompiledAssembliesWithSetupErrorsTracking(bool isEditor, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, string[] extraScriptingDefines = null)
        {
            return GetPrecompiledAssembliesDictionaryWithSetupErrorsTracking(isEditor, buildTargetGroup, buildTarget, extraScriptingDefines)?.Values.ToArray();
        }

        public void GetAssemblyDefinitionReferencesWithMissingAssemblies(out List<CustomScriptAssemblyReference> referencesWithMissingAssemblies)
        {
            var nameLookup = loadingAssemblyDefinition.CustomScriptAssemblies.ToDictionary(x => x.Name);
            referencesWithMissingAssemblies = new List<CustomScriptAssemblyReference>();
            foreach (var asmref in loadingAssemblyDefinition.CustomScriptAssemblyReferences)
            {
                if (!nameLookup.ContainsKey(asmref.Reference))
                {
                    referencesWithMissingAssemblies.Add(asmref);
                }
            }
        }

        public TargetAssembly GetCustomTargetAssemblyFromName(string name)
        {
            TargetAssembly targetAssembly;

            if (name.EndsWith(".dll", StringComparison.Ordinal))
            {
                customTargetAssemblies.TryGetValue(name, out targetAssembly);
            }
            else
            {
                customTargetAssemblies.TryGetValue(name + ".dll", out targetAssembly);
            }

            if (targetAssembly == null)
            {
                throw new ArgumentException("Assembly not found", name);
            }

            return targetAssembly;
        }

        public TargetAssemblyInfo[] GetAllCompiledAndResolvedTargetAssemblies(
            EditorScriptCompilationOptions options,
            BuildTarget buildTarget,
            out CustomScriptAssemblyAndReference[] assembliesWithMissingReference)
        {
            var allTargetAssemblies = GetTargetAssemblies();
            var targetAssemblyCompiledPaths = CollectCompiledAssemblies(allTargetAssemblies);


            var removedCustomAssemblies = new List<CustomScriptAssemblyAndReference>();
            var assembliesWithScripts = GetTargetAssembliesWithScriptsHashSet(options);

            bool removed;
            do
            {
                removed = false;

                if (targetAssemblyCompiledPaths.Count <= 0)
                {
                    continue;
                }

                foreach (var assembly in allTargetAssemblies)
                {
                    if (!targetAssemblyCompiledPaths.ContainsKey(assembly))
                    {
                        continue;
                    }

                    removed = VerifyReferencesIsCompiled(options, buildTarget, assembly, assembliesWithScripts, targetAssemblyCompiledPaths, removedCustomAssemblies, removed);
                }
            }
            while (removed);

            var count = targetAssemblyCompiledPaths.Count;
            var targetAssemblies = new TargetAssemblyInfo[count];
            int index = 0;

            foreach (var entry in targetAssemblyCompiledPaths)
            {
                var assembly = entry.Key;
                targetAssemblies[index++] = ToTargetAssemblyInfo(assembly);
            }

            assembliesWithMissingReference = removedCustomAssemblies.ToArray();
            return targetAssemblies;
        }

        /// <returns>A map of all target assemblies that exists on disk and their full path</returns>
        Dictionary<TargetAssembly, string> CollectCompiledAssemblies(TargetAssembly[] allTargetAssemblies)
        {
            var targetAssemblyCompiledPaths = new Dictionary<TargetAssembly, string>();
            foreach (var assembly in allTargetAssemblies)
            {
                var path = assembly.FullPath(outputDirectory);

                if (File.Exists(path))
                {
                    targetAssemblyCompiledPaths.Add(assembly, path);
                }
            }

            return targetAssemblyCompiledPaths;
        }

        /// <summary>
        /// Check for each compiled assembly that all its references have also been compiled.
        /// If not, remove it from the list of compiled assemblies.
        /// We update the removedCustomAssemblies with the assembly and the removed reference.
        ///
        /// References that are not compatible with the current build target, will not be checked as these haven't been compiled.
        /// The same is true if the reference does not have any scripts,
        /// or if the compiled assembly type is undefined or predefined
        /// </summary>
        /// <returns>Whether a reference was removed</returns>
        bool VerifyReferencesIsCompiled(
            EditorScriptCompilationOptions options,
            BuildTarget buildTarget,
            TargetAssembly assembly,
            HashSet<TargetAssembly> assembliesWithScripts,
            Dictionary<TargetAssembly, string> targetAssemblyCompiledPaths,
            List<CustomScriptAssemblyAndReference> removedCustomAssemblies,
            bool removed)
        {
            foreach (var reference in assembly.References)
            {
                if (!EditorBuildRules.IsCompatibleWithPlatformAndDefines(reference, buildTarget, options))
                {
                    continue;
                }

                if (!assembliesWithScripts.Contains(reference))
                {
                    continue;
                }

                if (assembly.Type == TargetAssemblyType.Predefined
                    || assembly.Type == TargetAssemblyType.Undefined
                    || targetAssemblyCompiledPaths.ContainsKey(reference))
                {
                    continue;
                }

                targetAssemblyCompiledPaths.Remove(assembly);

                var removedAssemblyAndReference = new CustomScriptAssemblyAndReference
                {
                    Assembly = FindCustomTargetAssemblyFromTargetAssembly(assembly),
                    Reference = FindCustomTargetAssemblyFromTargetAssembly(reference)
                };
                removedCustomAssemblies.Add(removedAssemblyAndReference);
                removed = true;
                break;
            }

            return removed;
        }

        string[] CustomTargetAssembliesToFilePaths(IEnumerable<TargetAssembly> targetAssemblies)
        {
            var customAssemblies = targetAssemblies.Select(FindCustomTargetAssemblyFromTargetAssembly);
            var filePaths = customAssemblies.Select(a => a.FilePath).ToArray();
            return filePaths;
        }

        string CustomTargetAssemblyToFilePath(TargetAssembly targetAssembly)
        {
            return FindCustomTargetAssemblyFromTargetAssembly(targetAssembly).FilePath;
        }

        public struct CheckCyclicAssemblyReferencesFunctions
        {
            public Func<TargetAssembly, string> ToFilePathFunc;
            public Func<IEnumerable<TargetAssembly>, string[]> ToFilePathsFunc;
        }

        static void CheckCyclicAssemblyReferencesDFS(TargetAssembly visitAssembly,
            HashSet<TargetAssembly> visited,
            HashSet<TargetAssembly> recursion,
            CheckCyclicAssemblyReferencesFunctions functions)
        {
            visited.Add(visitAssembly);
            recursion.Add(visitAssembly);

            foreach (var reference in visitAssembly.References)
            {
                if (reference.Filename == visitAssembly.Filename)
                {
                    throw new AssemblyDefinitionException("Assembly contains a references to itself",
                        AssemblyDefinitionErrorType.CyclicReferences, functions.ToFilePathFunc(visitAssembly));
                }

                if (recursion.Contains(reference))
                {
                    throw new AssemblyDefinitionException("Assembly with cyclic references detected",
                        AssemblyDefinitionErrorType.CyclicReferences, functions.ToFilePathsFunc(recursion));
                }

                if (!visited.Contains(reference))
                {
                    CheckCyclicAssemblyReferencesDFS(reference,
                        visited,
                        recursion,
                        functions);
                }
            }

            recursion.Remove(visitAssembly);
        }

        public static void CheckCyclicAssemblyReferences(IDictionary<string, TargetAssembly> customTargetAssemblies,
            CheckCyclicAssemblyReferencesFunctions functions)
        {
            if (customTargetAssemblies == null || customTargetAssemblies.Count < 1)
            {
                return;
            }

            var visited = new HashSet<TargetAssembly>();

            foreach (var entry in customTargetAssemblies)
            {
                var assembly = entry.Value;
                if (visited.Contains(assembly))
                {
                    continue;
                }

                var recursion = new HashSet<TargetAssembly>();
                CheckCyclicAssemblyReferencesDFS(assembly,
                    visited,
                    recursion,
                    functions);
            }
        }

        void CheckCyclicAssemblyReferences()
        {
            try
            {
                CheckCyclicAssemblyReferencesFunctions functions;

                functions.ToFilePathFunc = CustomTargetAssemblyToFilePath;
                functions.ToFilePathsFunc = CustomTargetAssembliesToFilePaths;

                CheckCyclicAssemblyReferences(customTargetAssemblies, functions);
            }
            catch (AssemblyDefinitionException e)
            {
                if (e.errorType == AssemblyDefinitionErrorType.CyclicReferences)
                {
                    CompilationSetupErrorsTracker.SetCompilationSetupErrors(CompilationSetupErrors.CyclicReferences);
                }
                throw;
            }
        }

        public static Exception[] UpdateCustomScriptAssemblies(CustomScriptAssembly[] customScriptAssemblies,
            List<CustomScriptAssemblyReference> customScriptAssemblyReferences,
            AssetPathMetaData[] assetPathsMetaData,
            ResponseFileProvider responseFileProvider)
        {
            var asmrefLookup = customScriptAssemblyReferences.ToLookup(x => x.Reference);

            AddAdditionalPrefixes(customScriptAssemblies, asmrefLookup);
            UpdateCustomTargetAssembliesResponseFileData(customScriptAssemblies, responseFileProvider);
            return UpdateCustomTargetAssembliesAssetPathsMetaData(customScriptAssemblies, assetPathsMetaData);
        }

        /// <summary>
        /// Assign the found references or null.
        /// We need to assign null so as not to hold onto references that may have been removed/changed.
        /// </summary>
        static void AddAdditionalPrefixes(CustomScriptAssembly[] customScriptAssemblies, ILookup<string, CustomScriptAssemblyReference> asmrefLookup)
        {
            foreach (var assembly in customScriptAssemblies)
            {
                var foundAsmRefs = asmrefLookup[assembly.Name];

                assembly.AdditionalPrefixes = foundAsmRefs.Any() ? foundAsmRefs.Select(ar => ar.PathPrefix).ToArray() : null;
            }
        }

        static void UpdateCustomTargetAssembliesResponseFileData(CustomScriptAssembly[] customScriptAssemblies, ResponseFileProvider responseFileProvider)
        {
            foreach (var assembly in customScriptAssemblies)
            {
                string rspFile = responseFileProvider.Get(assembly.PathPrefix)
                    .SingleOrDefault();
                if (!string.IsNullOrEmpty(rspFile))
                {
                    var responseFileContent = MicrosoftResponseFileParser.GetResponseFileContent(Directory.GetParent(Application.dataPath).FullName, rspFile);
                    var compilerOptions = MicrosoftResponseFileParser.GetCompilerOptions(responseFileContent);
                    assembly.ResponseFileDefines = MicrosoftResponseFileParser.GetDefines(compilerOptions).ToArray();
                }
            }
        }

        static Exception[] UpdateCustomTargetAssembliesAssetPathsMetaData(
            CustomScriptAssembly[] customScriptAssemblies,
            AssetPathMetaData[] assetPathsMetaData,
            bool forceUpdate = false)
        {
            if (assetPathsMetaData == null)
            {
                return new Exception[0];
            }

            var assetMetaDataPaths = new string[assetPathsMetaData.Length];
            var lowerAssetMetaDataPaths = new string[assetPathsMetaData.Length];

            for (int i = 0; i < assetPathsMetaData.Length; ++i)
            {
                var assetPathMetaData = assetPathsMetaData[i];
                assetMetaDataPaths[i] = AssetPath.ReplaceSeparators(assetPathMetaData.DirectoryPath + AssetPath.Separator);
                lowerAssetMetaDataPaths[i] = Utility.FastToLower(assetMetaDataPaths[i]);
            }

            var exceptions = new List<Exception>();
            foreach (var assembly in customScriptAssemblies)
            {
                if (assembly.AssetPathMetaData != null && !forceUpdate)
                {
                    continue;
                }

                try
                {
                    for (int i = 0; i < assetMetaDataPaths.Length; ++i)
                    {
                        var path = assetMetaDataPaths[i];
                        var lowerPath = lowerAssetMetaDataPaths[i];

                        if (Utility.FastStartsWith(assembly.PathPrefix, path, lowerPath))
                        {
                            assembly.AssetPathMetaData = assetPathsMetaData[i];
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            return exceptions.ToArray();
        }

        Exception[] UpdateCustomTargetAssemblies()
        {
            var exceptions = UpdateCustomScriptAssemblies(
                loadingAssemblyDefinition.CustomScriptAssemblies,
                loadingAssemblyDefinition.CustomScriptAssemblyReferences,
                m_AssetPathsMetaData,
                ResponseFileProvider);

            if (exceptions.Length > 0)
            {
                CompilationSetupErrorsTracker.SetCompilationSetupErrors(CompilationSetupErrors.LoadError);
            }

            customTargetAssemblies = EditorBuildRules.CreateTargetAssemblies(loadingAssemblyDefinition.CustomScriptAssemblies);

            CompilationSetupErrorsTracker.ClearCompilationSetupErrors(CompilationSetupErrors.CyclicReferences);

            return exceptions;
        }

        public void SkipCustomScriptAssemblyGraphValidation(bool skipChecks)
        {
            skipCustomScriptAssemblyGraphValidation = skipChecks;
        }

        public Exception[] SetAllCustomScriptAssemblyReferenceJsons(string[] paths)
        {
            return SetAllCustomScriptAssemblyReferenceJsonsContents(paths, null);
        }

        public Exception[] SetAllCustomScriptAssemblyReferenceJsonsContents(string[] paths, string[] contents)
        {
            RefreshLoadingAssemblyDefinition();
            loadingAssemblyDefinition.SetAllCustomScriptAssemblyReferenceJsonsContents(paths, contents);
            return GetLoadingExceptions();
        }

        public Exception[] SetAllCustomScriptAssemblyJsons(string[] paths, string[] guids)
        {
            return SetAllCustomScriptAssemblyJsonContents(paths, null, guids);
        }

        public Exception[] SetAllCustomScriptAssemblyJsonContents(string[] paths, string[] contents, string[] guids)
        {
            RefreshLoadingAssemblyDefinition();
            loadingAssemblyDefinition.SetAllCustomScriptAssemblyJsonContents(paths, contents, guids);
            return GetLoadingExceptions();
        }

        Exception[] GetLoadingExceptions()
        {
            return loadingAssemblyDefinition.Exceptions.Concat(UpdateCustomTargetAssemblies()).ToArray();
        }

        void RefreshLoadingAssemblyDefinition()
        {
            loadingAssemblyDefinition.Refresh(
                CompilationSetupErrorsTracker,
                skipCustomScriptAssemblyGraphValidation,
                projectDirectory);
        }

        public void ClearCustomScriptAssemblies()
        {
            loadingAssemblyDefinition.ClearCustomScriptAssemblies();
        }

        public bool IsPathInPackageDirectory(string path)
        {
            if (m_AssetPathsMetaData == null)
            {
                return false;
            }
            return m_AssetPathsMetaData.Any(p => path.StartsWith(p.DirectoryPath, StringComparison.OrdinalIgnoreCase));
        }

        public void DeleteScriptAssemblies()
        {
            NPath fullEditorAssemblyPath = AssetPath.Combine(projectDirectory, GetCompileScriptsOutputDirectory());
            fullEditorAssemblyPath.DeleteIfExists(DeleteMode.Soft);
        }

        public bool TryFindCustomScriptAssemblyFromAssemblyName(string assemblyName, out CustomScriptAssembly customScriptAssembly)
        {
            assemblyName = AssetPath.GetAssemblyNameWithoutExtension(assemblyName);

            if (loadingAssemblyDefinition.CustomScriptAssemblies == null)
            {
                customScriptAssembly = null;
                return false;
            }

            foreach (var assembly in loadingAssemblyDefinition.CustomScriptAssemblies)
            {
                if (assembly.Name != assemblyName)
                {
                    continue;
                }

                customScriptAssembly = assembly;
                return true;
            }

            customScriptAssembly = null;
            return false;
        }

        public CustomScriptAssembly FindCustomScriptAssemblyFromAssemblyName(string assemblyName)
        {
            if (TryFindCustomScriptAssemblyFromAssemblyName(assemblyName, out CustomScriptAssembly customScriptAssembly))
            {
                return customScriptAssembly;
            }

            assemblyName = AssetPath.GetAssemblyNameWithoutExtension(assemblyName);

            var result = loadingAssemblyDefinition.CustomScriptAssemblies?.FirstOrDefault(a => a.Name == assemblyName);
            if (result != null)
            {
                return result;
            }

            var exceptionMessage = "Cannot find CustomScriptAssembly with name '" + assemblyName + "'.";

            if (loadingAssemblyDefinition.CustomScriptAssemblies == null)
            {
                exceptionMessage += " customScriptAssemblies is null.";
            }
            else
            {
                var assemblyNames = loadingAssemblyDefinition.CustomScriptAssemblies.Select(a => a.Name).ToArray();
                var assemblyNamesString = string.Join(", ", assemblyNames);
                exceptionMessage += " Assembly names: " + assemblyNamesString;
            }

            throw new InvalidOperationException(exceptionMessage);
        }

        public bool TryFindCustomScriptAssemblyFromScriptPath(string scriptPath, out CustomScriptAssembly customScriptAssembly)
        {
            var customTargetAssembly = EditorBuildRules.GetCustomTargetAssembly(scriptPath, projectDirectory, customTargetAssemblies);
            if (customTargetAssembly != null)
            {
                return TryFindCustomScriptAssemblyFromAssemblyName(customTargetAssembly.Filename, out customScriptAssembly);
            }

            customScriptAssembly = null;
            return false;
        }

        public CustomScriptAssembly FindCustomScriptAssemblyFromScriptPath(string scriptPath)
        {
            var customTargetAssembly = EditorBuildRules.GetCustomTargetAssembly(scriptPath, projectDirectory, customTargetAssemblies);
            var customScriptAssembly = customTargetAssembly != null ? FindCustomScriptAssemblyFromAssemblyName(customTargetAssembly.Filename) : null;

            return customScriptAssembly;
        }

        public CustomScriptAssembly FindCustomTargetAssemblyFromTargetAssembly(TargetAssembly assembly)
        {
            var assemblyName = AssetPath.GetAssemblyNameWithoutExtension(assembly.Filename);
            return FindCustomScriptAssemblyFromAssemblyName(assemblyName);
        }

        public bool TryFindCustomScriptAssemblyFromAssemblyReference(string reference, out CustomScriptAssembly customScriptAssembly)
        {
            if (!GUIDReference.IsGUIDReference(reference))
            {
                return TryFindCustomScriptAssemblyFromAssemblyName(reference, out customScriptAssembly);
            }

            if (loadingAssemblyDefinition.CustomScriptAssemblies != null)
            {
                var guid = GUIDReference.GUIDReferenceToGUID(reference);
                var result = loadingAssemblyDefinition.CustomScriptAssemblies.FirstOrDefault(a => string.Equals(a.GUID, guid, StringComparison.OrdinalIgnoreCase));

                if (result != null)
                {
                    customScriptAssembly = result;
                    return true;
                }
            }

            customScriptAssembly = null;
            return false;
        }

        public CustomScriptAssembly FindCustomScriptAssemblyFromAssemblyReference(string reference)
        {
            if (TryFindCustomScriptAssemblyFromAssemblyReference(reference, out CustomScriptAssembly customScriptAssembly))
            {
                return customScriptAssembly;
            }

            throw new InvalidOperationException($"Cannot find CustomScriptAssembly with reference '{reference}'");
        }

        public CompileStatus CompileScripts(
            EditorScriptCompilationOptions editorScriptCompilationOptions,
            BuildTargetGroup platformGroup,
            BuildTarget platform,
            int subtarget,
            string[] extraScriptingDefines
        )
        {
            IsRunningRoslynAnalysisSynchronously =
                (editorScriptCompilationOptions & EditorScriptCompilationOptions.BuildingWithRoslynAnalysis) != 0 && PlayerSettings.EnableRoslynAnalyzers;

            var scriptAssemblySettings = CreateScriptAssemblySettings(platformGroup, platform, editorScriptCompilationOptions, extraScriptingDefines);

            CompileStatus compilationResult;
            using (new ProfilerMarker("Initiating Script Compilation").Auto())
            {
                try
                {
                    compilationResult = CompileScriptsWithSettings(scriptAssemblySettings);
                }
                catch (PrecompiledAssemblyException)
                {
                    // The setup errors for this exception has already been logged earlier, so there's no need to log the exception.
                    compilationResult = CompileStatus.CompilationFailed;
                }
            }

            if (compilationResult != CompileStatus.Idle)
            {
                WarnIfThereAreScriptsThatDoNotBelongToAnyAssembly();
            }

            return compilationResult;
        }

        static string PDBPath(string dllPath)
        {
            return dllPath.Replace(".dll", ".pdb");
        }

        static string MDBPath(string dllPath)
        {
            return dllPath + ".mdb";
        }

        // Delete all .dll's that aren't used anymore
        public void DeleteUnusedAssemblies(ScriptAssemblySettings settings)
        {
            string fullEditorAssemblyPath = AssetPath.Combine(projectDirectory, GetCompileScriptsOutputDirectory());

            if (!Directory.Exists(fullEditorAssemblyPath))
            {
                return;
            }

            var deleteFiles = Directory.GetFiles(fullEditorAssemblyPath).Select(AssetPath.ReplaceSeparators).ToList();

            var targetAssemblies = GetTargetAssembliesWithScripts(settings);

            foreach (var assembly in targetAssemblies)
            {
                string path = AssetPath.Combine(fullEditorAssemblyPath, assembly.Name);
                deleteFiles.Remove(path);
                deleteFiles.Remove(MDBPath(path));
                deleteFiles.Remove(PDBPath(path));
            }

            foreach (var path in deleteFiles)
            {
                path.ToNPath().Delete(DeleteMode.Soft);
            }
        }

        void CancelActiveBuild()
        {
            activeBeeBuild?.Driver.CancelBuild();
        }

        void WarnIfThereAreScriptsThatDoNotBelongToAnyAssembly()
        {
            foreach (var script in GetScriptsThatDoNotBelongToAnyAssembly().OrderBy(s => s))
            {
                Debug.LogWarning($"Script '{script}' will not be compiled because it exists outside the Assets folder and does not to belong to any assembly definition file.");
            }
        }

        void WarnIfThereAreAssembliesWithoutAnyScripts(ScriptAssemblySettings scriptAssemblySettings, ScriptAssembly[] scriptAssemblies)
        {
            foreach (var targetAssembly in customTargetAssemblies.Values)
            {
                if (!EditorBuildRules.IsCompatibleWithPlatformAndDefines(targetAssembly, scriptAssemblySettings))
                {
                    continue;
                }

                if (scriptAssemblies.Any(s => s.Filename == targetAssembly.Filename))
                {
                    continue;
                }

                var customTargetAssembly = FindCustomTargetAssemblyFromTargetAssembly(targetAssembly);
                Debug.LogWarningFormat(
                    "Assembly for Assembly Definition File '{0}' will not be compiled, because it has no scripts associated with it.",
                    customTargetAssembly.FilePath);
            }
        }

        public IEnumerable<string> GetScriptsThatDoNotBelongToAnyAssembly()
        {
            return allScripts
                .Where(e => EditorBuildRules.GetTargetAssembly(e.Key, e.Value, projectDirectory, customTargetAssemblies) == null)
                .Select(e => e.Key);
        }

        static TargetAssembly[] GetPredefinedAssemblyReferences(IDictionary<string, TargetAssembly> targetAssemblies)
        {
            var targetAssembliesResult = targetAssemblies.Values
                .Where(x => (x.Flags & AssemblyFlags.ExplicitlyReferenced) == AssemblyFlags.None)
                .ToArray();
            return targetAssembliesResult;
        }

        public CompileStatus CompileScriptsWithSettings(ScriptAssemblySettings scriptAssemblySettings)
        {
            m_ScriptCompilationRequest = null;

            DeleteUnusedAssemblies(scriptAssemblySettings);
            VersionDefinesConsoleLogs?.ClearVersionDefineErrors();
            m_UnityVersionRanges.Clear();
            m_SemVersionRanges.Clear();

            IsRunningRoslynAnalysisSynchronously =
                PlayerSettings.EnableRoslynAnalyzers &&
                (scriptAssemblySettings.CompilationOptions & EditorScriptCompilationOptions.BuildingWithRoslynAnalysis) != 0;

            // If we have successfully compiled and reloaded all assemblies, then we can
            // skip checks on the asmdef compilation graph to ensure there no
            // setup errors like cyclic references, duplicate assembly names, etc.
            // If there is compilation errors n Safe Mode domain or a partial domain (if SafeMode is forcefully exited),
            // Then we need to keep the validation checks to rediscover potential setup errors in subsequent compilations.
            if (!skipCustomScriptAssemblyGraphValidation)
            {
                CheckCyclicAssemblyReferences();
            }

            ScriptAssembly[] scriptAssemblies;
            try
            {
                CompilationSetupWarningTracker.ClearAssetWarnings();
                scriptAssemblies = GetAllScriptAssembliesOfType(scriptAssemblySettings, TargetAssemblyType.Undefined, CompilationSetupWarningTracker);
            }
            catch (PrecompiledAssemblyException)
            {
                // The setup errors for this exception has already been logged earlier, so there's no need to log the exception.
                return CompileStatus.Idle;
            }

            // Do no start compilation if there is an setup error.
            if (CompilationSetupErrorsTracker.HaveCompilationSetupErrors())
            {
                return CompileStatus.Idle;
            }

            WarnIfThereAreAssembliesWithoutAnyScripts(scriptAssemblySettings, scriptAssemblies);

            var debug = scriptAssemblySettings.CodeOptimization == CodeOptimization.Debug;

            //we're going to hash the output directory path into the dag name. We do this because when users build players into different directories,
            //we'd like to treat those as different dags. If we wouldn't do this, you could run into situations where building into directory2 will make
            //bee delete the previously built game from directory1.
            Hash128 hash = Hash128.Parse(scriptAssemblySettings.OutputDirectory);

            var config =
                hash.ToString().Substring(0, 5) +
                $"{(scriptAssemblySettings.BuildingForEditor ? "E" : "P")}" +
                $"{(scriptAssemblySettings.BuildingDevelopmentBuild ? "Dev" : "")}" +
                $"{(debug ? "Dbg" : "")}" +
                "";

            BuildTarget buildTarget = scriptAssemblySettings.BuildTarget;
            activeBeeBuild = new BeeScriptCompilationState()
            {
                Driver = UnityBeeDriver.Make(ScriptCompilationBuildProgram, this, $"{(int)buildTarget}{config}", useScriptUpdater: !scriptAssemblySettings.BuildingWithoutScriptUpdater),
                settings = scriptAssemblySettings,
                assemblies = scriptAssemblies,
            };

            BeeScriptCompilation.AddScriptCompilationData(
                activeBeeBuild.Driver,
                this,
                activeBeeBuild.assemblies,
                debug,
                scriptAssemblySettings.OutputDirectory,
                buildTarget,
                scriptAssemblySettings.BuildingForEditor,
                scriptAssemblySettings.ExtraGeneralDefines
            );

            InvokeCompilationStarted(activeBeeBuild);

            if (scriptAssemblySettings.CompilationOptions.HasFlag(EditorScriptCompilationOptions.BuildingExtractTypeDB))
                activeBeeBuild.Driver.BuildAsync(Constants.ScriptAssembliesAndTypeDBTarget);
            else
                activeBeeBuild.Driver.BuildAsync(Constants.ScriptAssembliesTarget);

            return CompileStatus.CompilationStarted;
        }

        public static SystemProcessRunnableProgram ScriptCompilationBuildProgram { get; } = MakeScriptCompilationBuildProgram();

        static SystemProcessRunnableProgram MakeScriptCompilationBuildProgram()
        {
            var buildProgramAssembly = new NPath($"{EditorApplication.applicationContentsPath}/Tools/BuildPipeline/ScriptCompilationBuildProgram.exe");
            return new SystemProcessRunnableProgram($"{EditorApplication.applicationContentsPath}/Tools/netcorerun/netcorerun{BeeScriptCompilation.ExecutableExtension}", buildProgramAssembly.InQuotes(SlashMode.Native));
        }

        public void InvokeCompilationStarted(object context)
        {
            compilationStarted?.Invoke(context);
        }

        public void InvokeCompilationFinished(object context)
        {
            compilationFinished?.Invoke(context);
        }

        public bool DoesProjectFolderHaveAnyScripts()
        {
            return allScripts != null && allScripts.Count > 0;
        }

        public ScriptAssemblySettings CreateScriptAssemblySettings(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, EditorScriptCompilationOptions options)
        {
            return CreateScriptAssemblySettings(buildTargetGroup, buildTarget, options, new string[] {});
        }

        public ScriptAssemblySettings CreateScriptAssemblySettings(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, EditorScriptCompilationOptions options, string[] extraScriptingDefines)
        {
            var predefinedAssembliesCompilerOptions = new ScriptCompilerOptions();

            if ((options & EditorScriptCompilationOptions.BuildingPredefinedAssembliesAllowUnsafeCode) == EditorScriptCompilationOptions.BuildingPredefinedAssembliesAllowUnsafeCode)
            {
                predefinedAssembliesCompilerOptions.AllowUnsafeCode = true;
            }

            if ((options & EditorScriptCompilationOptions.BuildingUseDeterministicCompilation) == EditorScriptCompilationOptions.BuildingUseDeterministicCompilation)
            {
                predefinedAssembliesCompilerOptions.UseDeterministicCompilation = true;
            }

            predefinedAssembliesCompilerOptions.ApiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));

            ICompilationExtension compilationExtension = null;
            if ((options & EditorScriptCompilationOptions.BuildingForEditor) == 0)
            {
                compilationExtension = ModuleManager.FindPlatformSupportModule(ModuleManager.GetTargetStringFromBuildTarget(buildTarget))?.CreateCompilationExtension();
            }


            List<string> additionalCompilationArguments = new List<string>(PlayerSettings.GetAdditionalCompilerArguments(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup)));

            if (PlayerSettings.suppressCommonWarnings)
            {
                additionalCompilationArguments.Add("/nowarn:0169");
                additionalCompilationArguments.Add("/nowarn:0649");

                // The msbuild tool disables warnings 1701 and 1702 by default, so Unity should do the same.
                additionalCompilationArguments.Add("/nowarn:1701");
                additionalCompilationArguments.Add("/nowarn:1702");
            }

            var additionalCompilationArgumentsArray = additionalCompilationArguments.Where(s => !string.IsNullOrEmpty(s)).Distinct().ToArray();

            var settings = new ScriptAssemblySettings
            {
                BuildTarget = buildTarget,
                BuildTargetGroup = buildTargetGroup,
                OutputDirectory = GetCompileScriptsOutputDirectory(),
                CompilationOptions = options,
                PredefinedAssembliesCompilerOptions = predefinedAssembliesCompilerOptions,
                CompilationExtension = compilationExtension,
                EditorCodeOptimization = CompilationPipeline.codeOptimization,
                ExtraGeneralDefines = extraScriptingDefines,
                ProjectRootNamespace = EditorSettings.projectGenerationRootNamespace,
                ProjectDirectory = projectDirectory,
                AdditionalCompilerArguments = additionalCompilationArgumentsArray
            };

            return settings;
        }

        ScriptAssemblySettings CreateEditorScriptAssemblySettings(EditorScriptCompilationOptions options)
        {
            return CreateScriptAssemblySettings(EditorUserBuildSettings.activeBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget, options);
        }

        static CompilerMessage AsCompilerMessage(BeeDriverResult.Message message)
        {
            return new CompilerMessage
            {
                message = message.Text,
                type = message.Kind == BeeDriverResult.MessageKind.Error
                    ? CompilerMessageType.Error
                    : CompilerMessageType.Warning,
            };
        }

        //only used in for tests to peek in.
        public CompilerMessage[] GetCompileMessages() => _currentEditorCompilationCompilerMessages;

        public bool IsScriptCompilationRequested()
        {
            return m_ScriptCompilationRequest != null;
        }

        public bool IsAnyAssemblyBuilderCompiling()
        {
            if (assemblyBuilders.Count <= 0)
            {
                return false;
            }

            var isCompiling = false;

            var removeAssemblyBuilders = new List<AssemblyBuilder>();

            // Check status of compile tasks
            foreach (var assemblyBuilder in assemblyBuilders)
            {
                switch (assemblyBuilder.status)
                {
                    case AssemblyBuilderStatus.IsCompiling:
                        isCompiling = true;
                        break;
                    case AssemblyBuilderStatus.Finished:
                        removeAssemblyBuilders.Add(assemblyBuilder);
                        break;
                    case AssemblyBuilderStatus.NotStarted:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unknown builder status: {assemblyBuilder.status}");
                }
            }

            // Remove all compile tasks that finished compiling.
            if (removeAssemblyBuilders.Count > 0)
            {
                assemblyBuilders.RemoveAll(t => removeAssemblyBuilders.Contains(t));
            }

            return isCompiling;
        }

        public bool IsCompiling()
        {
            // Native code expects IsCompiling to be true after requesting a script reload,
            // therefore return true if the compilation is pending
            return IsCompilationTaskCompiling() || IsScriptCompilationRequested() || IsAnyAssemblyBuilderCompiling();
        }

        public bool IsCompilationTaskCompiling()
        {
            return activeBeeBuild != null;
        }

        public CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform, int subtarget, string[] extraScriptingDefines)
        {
            // Return CompileStatus.Compiling if any compile task is still compiling.
            // This ensures that the compile tasks finish compiling before any
            // scripts in the Assets folder are compiled and a domain reload
            // is triggered.
            if (IsAnyAssemblyBuilderCompiling())
            {
                return CompileStatus.Compiling;
            }

            // If we are not currently compiling and there are new dirty assemblies, start compilation.
            if (!IsCompilationTaskCompiling() && IsScriptCompilationRequested())
            {
                Profiler.BeginSample("CompilationPipeline.CompileScripts");
                if (m_ScriptCompilationRequest == RequestScriptCompilationOptions.CleanBuildCache)
                    CleanCache();
                CompileStatus compileStatus;
                try
                {
                    compileStatus = CompileScripts(options, platformGroup, platform, subtarget, extraScriptingDefines);
                }
                finally
                {
                    Profiler.EndSample();
                }

                return compileStatus;
            }

            if (activeBeeBuild == null)
            {
                return CompileStatus.Idle;
            }

            var result = activeBeeBuild.Driver.Tick();
            if (result == null)
            {
                return CompileStatus.Compiling;
            }

            var messagesForNodeResults = ProcessCompilationResult(activeBeeBuild.assemblies, result, activeBeeBuild.settings.BuildingForEditor, activeBeeBuild);

            int logIdentifier = activeBeeBuild.settings.BuildingForEditor
                //these numbers are "randomly picked". they are used to so that when you log a message with a certain identifier, later all messages with that identifier can be cleared.
                //one means "compilation error for compiling-assemblies-for-editor"  the other means "compilation error for building a player".
                ? kLogIdentifierFor_EditorMessages
                : kLogIdentifierFor_PlayerMessages;
            var compilerMessages = result.BeeDriverMessages.Select(AsCompilerMessage).Concat(messagesForNodeResults.SelectMany(m => m)).ToArray();

            if (activeBeeBuild.settings.BuildingForEditor)
            {
                _currentEditorCompilationCompilerMessages = compilerMessages;
            }

            if (_logCompilationMessages)
            {
                Debug.RemoveLogEntriesByIdentifier(logIdentifier);
                foreach (var message in compilerMessages)
                {
                    // Ensure that we don't emit info messages (user cannot do anything with these and they are generated by DiagnosticSuppressors)
                    if (message.type != CompilerMessageType.Information)
                        Debug.LogCompilerMessage(message.message, message.file, message.line, message.column, activeBeeBuild.settings.BuildingForEditor, message.type == CompilerMessageType.Error, logIdentifier);
                }
            }

            activeBeeBuild = null;
            return result.Success
                ? CompileStatus.CompilationComplete
                : CompileStatus.CompilationFailed;
        }

        public void DisableLoggingEditorCompilerMessages()
        {
            _logCompilationMessages = false;
        }

        public CompilerMessage[][] ProcessCompilationResult(ScriptAssembly[] assemblies, BeeDriverResult result, bool buildingForEditor, object context)
        {
            var compilerMessagesForNodeResults = BeeScriptCompilation.ParseAllNodeResultsIntoCompilerMessages(result.NodeResults, this);
            InvokeAssemblyCompilationFinished(assemblies, result, buildingForEditor, compilerMessagesForNodeResults);
            InvokeCompilationFinished(context);
            return compilerMessagesForNodeResults;
        }

        void InvokeAssemblyCompilationFinished(ScriptAssembly[] assemblies, BeeDriverResult beeDriverResult, bool buildingForEditor, CompilerMessage[][] compilerMessagesForNodeResults)
        {
            bool Belongs(ScriptAssembly scriptAssembly, NodeResult nodeResult) => new NPath(nodeResult.outputfile).FileName == scriptAssembly.Filename;

            //we want to send callbacks for assemblies that were copied, for assemblies that failed to compile, but not for assemblies that were compiled but not copied (because they ended up identically)
            bool RequiresCallbackInvocation(ScriptAssembly scriptAssembly)
            {
                var relatedNodes = beeDriverResult.NodeResults.Where(nodeResult => Belongs(scriptAssembly, nodeResult));
                return relatedNodes.Any(n => n.exitcode != 0 || n.annotation.StartsWith("CopyFiles"));
            }

            foreach (var scriptAssembly in assemblies)
            {
                if (!RequiresCallbackInvocation(scriptAssembly))
                {
                    continue;
                }

                // Only set this flag if we actually changed any assemblies
                if (buildingForEditor)
                    m_ScriptsForEditorHaveBeenCompiledSinceLastDomainReload = true;

                var nodeResultIndicesRelatedToAssembly = beeDriverResult.NodeResults
                    .Select((result, index) => (result, index))
                    .Where(c => Belongs(scriptAssembly, c.result))
                    .Select(c => c.index);

                var messagesForAssembly = nodeResultIndicesRelatedToAssembly.SelectMany(index => compilerMessagesForNodeResults[index]).ToArray();
                scriptAssembly.HasCompileErrors = !beeDriverResult.Success;
                assemblyCompilationFinished?.Invoke(scriptAssembly, ConvertCompilerMessages(messagesForAssembly));
            }
        }

        public TargetAssemblyInfo[] GetTargetAssemblyInfos(ScriptAssemblySettings scriptAssemblySettings = null)
        {
            TargetAssembly[] predefindTargetAssemblies = EditorBuildRules.GetPredefinedTargetAssemblies();

            TargetAssemblyInfo[] targetAssemblyInfo = new TargetAssemblyInfo[predefindTargetAssemblies.Length + (customTargetAssemblies?.Count ?? 0)];

            int assembliesSize = 0;
            foreach (var assembly in predefindTargetAssemblies)
            {
                if (!ShouldAddTargetAssemblyToList(assembly, scriptAssemblySettings))
                {
                    continue;
                }

                targetAssemblyInfo[assembliesSize] = ToTargetAssemblyInfo(predefindTargetAssemblies[assembliesSize]);
                assembliesSize++;
            }

            if (customTargetAssemblies != null)
            {
                foreach (var entry in customTargetAssemblies)
                {
                    var customTargetAssembly = entry.Value;

                    if (!ShouldAddTargetAssemblyToList(customTargetAssembly, scriptAssemblySettings))
                    {
                        continue;
                    }

                    targetAssemblyInfo[assembliesSize] = ToTargetAssemblyInfo(customTargetAssembly);
                    assembliesSize++;
                }
                Array.Resize(ref targetAssemblyInfo, assembliesSize);
            }

            return targetAssemblyInfo;

            bool ShouldAddTargetAssemblyToList(TargetAssembly targetAssembly, ScriptAssemblySettings scriptAssemblySettings)
            {
                if (scriptAssemblySettings != null)
                {
                    return EditorBuildRules.IsCompatibleWithPlatformAndDefines(targetAssembly, scriptAssemblySettings);
                }
                return true;
            }
        }

        TargetAssembly[] GetTargetAssemblies()
        {
            TargetAssembly[] predefindTargetAssemblies = EditorBuildRules.GetPredefinedTargetAssemblies();

            TargetAssembly[] targetAssemblies = new TargetAssembly[predefindTargetAssemblies.Length + (customTargetAssemblies?.Count ?? 0)];

            for (int i = 0; i < predefindTargetAssemblies.Length; ++i)
            {
                targetAssemblies[i] = predefindTargetAssemblies[i];
            }

            if (customTargetAssemblies != null)
            {
                int i = predefindTargetAssemblies.Length;
                foreach (var entry in customTargetAssemblies)
                {
                    var customTargetAssembly = entry.Value;
                    targetAssemblies[i] = customTargetAssembly;
                    i++;
                }
            }

            return targetAssemblies;
        }

        public TargetAssemblyInfo[] GetTargetAssembliesWithScripts(EditorScriptCompilationOptions options)
        {
            ScriptAssemblySettings settings = CreateEditorScriptAssemblySettings(EditorScriptCompilationOptions.BuildingForEditor | options);
            return GetTargetAssembliesWithScripts(settings);
        }

        public TargetAssemblyInfo[] GetTargetAssembliesWithScripts(ScriptAssemblySettings settings)
        {
            UpdateAllTargetAssemblyDefines(customTargetAssemblies, EditorBuildRules.GetPredefinedTargetAssemblies(), m_VersionMetaDatas, settings);

            var targetAssemblies = EditorBuildRules.GetTargetAssembliesWithScripts(allScripts, projectDirectory, customTargetAssemblies, settings);

            var targetAssemblyInfos = new TargetAssemblyInfo[targetAssemblies.Length];

            for (int i = 0; i < targetAssemblies.Length; ++i)
            {
                targetAssemblyInfos[i] = ToTargetAssemblyInfo(targetAssemblies[i]);
            }

            return targetAssemblyInfos;
        }

        public HashSet<TargetAssembly> GetTargetAssembliesWithScriptsHashSet(EditorScriptCompilationOptions options)
        {
            ScriptAssemblySettings settings = CreateEditorScriptAssemblySettings(EditorScriptCompilationOptions.BuildingForEditor | options);
            var targetAssemblies = EditorBuildRules.GetTargetAssembliesWithScriptsHashSet(allScripts, projectDirectory, customTargetAssemblies, settings);

            return targetAssemblies;
        }

        public TargetAssembly[] GetCustomTargetAssemblies()
        {
            return customTargetAssemblies.Values.ToArray();
        }

        public CustomScriptAssembly[] GetCustomScriptAssemblies()
        {
            return loadingAssemblyDefinition.CustomScriptAssemblies;
        }

        public PrecompiledAssembly[] GetUnityAssemblies()
        {
            return unityAssemblies;
        }

        public TargetAssemblyInfo GetTargetAssembly(string scriptPath)
        {
            TargetAssembly targetAssembly = EditorBuildRules.GetTargetAssemblyLinearSearch(scriptPath, projectDirectory, customTargetAssemblies);

            TargetAssemblyInfo targetAssemblyInfo = ToTargetAssemblyInfo(targetAssembly);
            return targetAssemblyInfo;
        }

        public TargetAssembly GetTargetAssemblyDetails(string scriptPath)
        {
            return EditorBuildRules.GetTargetAssemblyLinearSearch(scriptPath, projectDirectory, customTargetAssemblies);
        }

        public ScriptAssembly[] GetAllEditorScriptAssemblies(EditorScriptCompilationOptions additionalOptions)
        {
            return GetAllScriptAssemblies(EditorScriptCompilationOptions.BuildingForEditor | EditorScriptCompilationOptions.BuildingIncludingTestAssemblies | additionalOptions, null);
        }

        public ScriptAssembly[] GetAllEditorScriptAssemblies(EditorScriptCompilationOptions additionalOptions, string[] defines)
        {
            return GetAllScriptAssemblies(EditorScriptCompilationOptions.BuildingForEditor | EditorScriptCompilationOptions.BuildingIncludingTestAssemblies | additionalOptions, defines);
        }

        public ScriptAssembly[] GetAllScriptAssemblies(EditorScriptCompilationOptions options, string[] defines)
        {
            var isForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;
            var precompiledAssemblies = GetPrecompiledAssembliesDictionaryWithSetupErrorsTracking(
                isForEditor, EditorUserBuildSettings.activeBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget, defines);
            return GetAllScriptAssemblies(options, unityAssemblies, precompiledAssemblies, defines);
        }

        public ScriptAssembly[] GetAllScriptAssemblies(
            EditorScriptCompilationOptions options,
            PrecompiledAssembly[] unityAssembliesArg,
            Dictionary<string, PrecompiledAssembly> precompiledAssembliesArg,
            string[] defines)
        {
            var settings = CreateEditorScriptAssemblySettings(options);

            return GetAllScriptAssemblies(
                settings,
                unityAssembliesArg,
                precompiledAssembliesArg,
                defines);
        }

        public ScriptAssembly[] GetAllScriptAssemblies(
            ScriptAssemblySettings settings,
            PrecompiledAssembly[] unityAssembliesArg,
            Dictionary<string, PrecompiledAssembly> precompiledAssembliesArg,
            string[] defines,
            Func<TargetAssembly, bool> targetAssemblyCondition = null)
        {
            if (defines != null)
            {
                settings.ExtraGeneralDefines = defines;
            }

            UpdateAllTargetAssemblyDefines(customTargetAssemblies, EditorBuildRules.GetPredefinedTargetAssemblies(), m_VersionMetaDatas, settings);

            var assemblies = new EditorBuildRules.CompilationAssemblies
            {
                UnityAssemblies = unityAssembliesArg,
                PrecompiledAssemblies = precompiledAssembliesArg,
                CustomTargetAssemblies = customTargetAssemblies,
                RoslynAnalyzerDllPaths = PrecompiledAssemblyProvider.GetRoslynAnalyzerPaths(),
                PredefinedAssembliesCustomTargetReferences = GetPredefinedAssemblyReferences(customTargetAssemblies),
                EditorAssemblyReferences = ModuleUtils.GetAdditionalReferencesForUserScripts(),
            };

            return EditorBuildRules.GetAllScriptAssemblies(
                allScripts,
                projectDirectory,
                settings,
                assemblies,
                SafeModeInfo,
                targetAssemblyCondition: targetAssemblyCondition);
        }

        public string[] GetTargetAssemblyDefines(TargetAssembly targetAssembly, ScriptAssemblySettings settings)
        {
            var versionMetaDatas = GetVersionMetaDatas();
            var editorOnlyCompatibleDefines = InternalEditorUtility.GetCompilationDefines(settings.CompilationOptions, settings.BuildTargetGroup, settings.BuildTarget, ApiCompatibilityLevel.NET_Unity_4_8, settings.ExtraGeneralDefines);
            var playerAssembliesDefines = InternalEditorUtility.GetCompilationDefines(settings.CompilationOptions, settings.BuildTargetGroup, settings.BuildTarget, settings.PredefinedAssembliesCompilerOptions.ApiCompatibilityLevel, settings.ExtraGeneralDefines);

            return GetTargetAssemblyDefines(targetAssembly, versionMetaDatas, editorOnlyCompatibleDefines, playerAssembliesDefines, settings);
        }

        // TODO: Get rid of calls to this method and ensure that the defines are always setup correctly at all times.
        void UpdateAllTargetAssemblyDefines(IDictionary<string, TargetAssembly> customScriptAssemblies, TargetAssembly[] predefinedTargetAssemblies,
            Dictionary<string, VersionMetaData> versionMetaDatas, ScriptAssemblySettings settings)
        {
            var allTargetAssemblies = customScriptAssemblies.Values.ToArray()
                .Concat(predefinedTargetAssemblies ?? new TargetAssembly[0]);

            string[] editorOnlyCompatibleDefines = InternalEditorUtility.GetCompilationDefines(settings.CompilationOptions, settings.BuildTargetGroup, settings.BuildTarget, ApiCompatibilityLevel.NET_Unity_4_8, settings.ExtraGeneralDefines);

            var playerAssembliesDefines = InternalEditorUtility.GetCompilationDefines(settings.CompilationOptions, settings.BuildTargetGroup, settings.BuildTarget, settings.PredefinedAssembliesCompilerOptions.ApiCompatibilityLevel, settings.ExtraGeneralDefines);

            foreach (var targetAssembly in allTargetAssemblies)
            {
                SetTargetAssemblyDefines(targetAssembly, versionMetaDatas, editorOnlyCompatibleDefines, playerAssembliesDefines, settings);
            }
        }

        void SetTargetAssemblyDefines(TargetAssembly targetAssembly, Dictionary<string, VersionMetaData> versionMetaDatas, string[] editorOnlyCompatibleDefines, string[] playerAssembliesDefines, ScriptAssemblySettings settings)
        {
            targetAssembly.Defines = GetTargetAssemblyDefines(targetAssembly, versionMetaDatas, editorOnlyCompatibleDefines, playerAssembliesDefines, settings);
        }

        string[] GetTargetAssemblyDefines(TargetAssembly targetAssembly, Dictionary<string, VersionMetaData> versionMetaDatas, string[] editorOnlyCompatibleDefines, string[] playerAssembliesDefines, ScriptAssemblySettings settings)
        {
            string[] settingsExtraGeneralDefines = settings.ExtraGeneralDefines;
            int populatedVersionDefinesCount = 0;

            var compilationDefines =
                (targetAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly
                ? editorOnlyCompatibleDefines
                : playerAssembliesDefines;

            string[] defines = new string[compilationDefines.Length + targetAssembly.VersionDefines.Count + settingsExtraGeneralDefines.Length];

            Array.Copy(settingsExtraGeneralDefines, defines, settingsExtraGeneralDefines.Length);
            populatedVersionDefinesCount += settingsExtraGeneralDefines.Length;
            Array.Copy(compilationDefines, 0, defines, populatedVersionDefinesCount, compilationDefines.Length);
            populatedVersionDefinesCount += compilationDefines.Length;

            if (versionMetaDatas == null)
            {
                return defines;
            }

            var targetAssemblyVersionDefines = targetAssembly.VersionDefines;

            foreach (var targetAssemblyVersionDefine in targetAssemblyVersionDefines)
            {
                if (!versionMetaDatas.ContainsKey(targetAssemblyVersionDefine.name))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(targetAssemblyVersionDefine.expression))
                {
                    var define = targetAssemblyVersionDefine.define;
                    if (!string.IsNullOrEmpty(define))
                    {
                        defines[populatedVersionDefinesCount] = define;
                        ++populatedVersionDefinesCount;
                    }

                    continue;
                }

                try
                {
                    var versionMetaData = versionMetaDatas[targetAssemblyVersionDefine.name];
                    var versionString = versionMetaData.Version;
                    bool isValid;
                    switch (versionMetaData.Type)
                    {
                        case VersionType.VersionTypeUnity:
                        {
                            var versionDefineExpression = m_UnityVersionRanges.GetExpression(targetAssemblyVersionDefine.expression);
                            if (versionDefineExpression.ValidationError != null)
                            {
                                VersionDefinesConsoleLogs?.LogVersionDefineError(targetAssembly, versionDefineExpression.ValidationError);
                                isValid = false;
                                break;
                            }
                            var unityVersion = UnityVersionParser.Parse(versionString);
                            isValid = versionDefineExpression.Expression.IsValid(unityVersion);
                            break;
                        }

                        case VersionType.VersionTypePackage:
                        {
                            var versionDefineExpression = m_SemVersionRanges.GetExpression(targetAssemblyVersionDefine.expression);
                            if (versionDefineExpression.ValidationError != null)
                            {
                                VersionDefinesConsoleLogs?.LogVersionDefineError(targetAssembly, versionDefineExpression.ValidationError);
                                isValid = false;
                                break;
                            }
                            var semVersion = SemVersionParser.Parse(versionString);
                            isValid = versionDefineExpression.Expression.IsValid(semVersion);
                            break;
                        }

                        default:
                            throw new NotImplementedException($"EditorCompilation does not recognize versionMetaData.Type {versionMetaData.Type}. UNIMPLEMENTED");
                    }

                    if (isValid)
                    {
                        defines[populatedVersionDefinesCount] = targetAssemblyVersionDefine.define;
                        ++populatedVersionDefinesCount;
                    }
                }
                catch (Exception e)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(EditorCompilationInterface.Instance.FindCustomTargetAssemblyFromTargetAssembly(targetAssembly).FilePath);
                    Debug.LogException(e, asset);
                }
            }

            Array.Resize(ref defines, populatedVersionDefinesCount);
            return defines;
        }

        public ScriptAssembly[] GetAllScriptAssembliesOfType(ScriptAssemblySettings settings, TargetAssemblyType type, ICompilationSetupWarningTracker warningSink)
        {
            using (new ProfilerMarker(nameof(GetAllScriptAssembliesOfType)).Auto())
            {
                var precompiledAssemblies =
                    GetPrecompiledAssembliesDictionaryWithSetupErrorsTracking(settings.BuildingForEditor,
                        settings.BuildTargetGroup, settings.BuildTarget, settings.ExtraGeneralDefines);

                UpdateAllTargetAssemblyDefines(customTargetAssemblies, EditorBuildRules.GetPredefinedTargetAssemblies(), m_VersionMetaDatas, settings);

                var assemblies = new EditorBuildRules.CompilationAssemblies
                {
                    UnityAssemblies = unityAssemblies,
                    PrecompiledAssemblies = precompiledAssemblies,
                    CustomTargetAssemblies = customTargetAssemblies,
                    RoslynAnalyzerDllPaths = PrecompiledAssemblyProvider.GetRoslynAnalyzerPaths(),
                    PredefinedAssembliesCustomTargetReferences = GetPredefinedAssemblyReferences(customTargetAssemblies),
                    EditorAssemblyReferences = ModuleUtils.GetAdditionalReferencesForUserScripts(),
                };

                return EditorBuildRules.GetAllScriptAssemblies(allScripts, projectDirectory, settings, assemblies, SafeModeInfo, type, warningSink: CompilationSetupWarningTracker);
            }
        }

        public bool IsRuntimeScriptAssembly(string assemblyNameOrPath)
        {
            var assemblyFilename = AssetPath.GetFileName(assemblyNameOrPath);

            if (!assemblyFilename.EndsWith(".dll"))
            {
                assemblyFilename += ".dll";
            }

            var predefinedAssemblyTargets = EditorBuildRules.GetPredefinedTargetAssemblies();

            if (predefinedAssemblyTargets.Any(a => ((a.Flags & AssemblyFlags.EditorOnly) != AssemblyFlags.EditorOnly) && a.Filename == assemblyFilename))
            {
                return true;
            }

            if (customTargetAssemblies != null && customTargetAssemblies.Any(a => ((a.Value.Flags & AssemblyFlags.EditorOnly) != AssemblyFlags.EditorOnly) && a.Value.Filename == assemblyFilename))
            {
                return true;
            }

            return false;
        }

        TargetAssemblyInfo ToTargetAssemblyInfo(TargetAssembly targetAssembly)
        {
            TargetAssemblyInfo targetAssemblyInfo = new TargetAssemblyInfo();

            if (targetAssembly != null)
            {
                targetAssemblyInfo.Name = targetAssembly.Filename;
                targetAssemblyInfo.Flags = targetAssembly.Flags;
            }
            else
            {
                targetAssemblyInfo.Name = "";
                targetAssemblyInfo.Flags = AssemblyFlags.None;
            }

            return targetAssemblyInfo;
        }

        static EditorScriptCompilationOptions ToEditorScriptCompilationOptions(AssemblyBuilderFlags flags)
        {
            EditorScriptCompilationOptions options = EditorScriptCompilationOptions.BuildingEmpty;

            if ((flags & AssemblyBuilderFlags.DevelopmentBuild) == AssemblyBuilderFlags.DevelopmentBuild)
            {
                options |= EditorScriptCompilationOptions.BuildingDevelopmentBuild;
            }

            if ((flags & AssemblyBuilderFlags.EditorAssembly) == AssemblyBuilderFlags.EditorAssembly)
            {
                options |= EditorScriptCompilationOptions.BuildingForEditor;
            }

            return options;
        }

        static AssemblyFlags ToAssemblyFlags(AssemblyBuilderFlags assemblyBuilderFlags)
        {
            AssemblyFlags assemblyFlags = AssemblyFlags.None;

            if ((assemblyBuilderFlags & AssemblyBuilderFlags.EditorAssembly) == AssemblyBuilderFlags.EditorAssembly)
            {
                assemblyFlags |= AssemblyFlags.EditorOnly;
            }

            return assemblyFlags;
        }

        static EditorBuildRules.UnityReferencesOptions ToUnityReferencesOptions(ReferencesOptions options)
        {
            var result = EditorBuildRules.UnityReferencesOptions.ExcludeModules;

            if ((options & ReferencesOptions.UseEngineModules) == ReferencesOptions.UseEngineModules)
            {
                result = EditorBuildRules.UnityReferencesOptions.None;
            }

            return result;
        }

        ScriptAssembly InitializeScriptAssemblyWithoutReferencesAndDefines(AssemblyBuilder assemblyBuilder)
        {
            var scriptFiles = assemblyBuilder.scriptPaths.Select(p => AssetPath.Combine(projectDirectory, p)).ToArray();
            var assemblyPath = AssetPath.Combine(projectDirectory, assemblyBuilder.assemblyPath);

            var scriptAssembly = new ScriptAssembly
            {
                Flags = ToAssemblyFlags(assemblyBuilder.flags),
                BuildTarget = assemblyBuilder.buildTarget,
                Files = scriptFiles,
                Filename = AssetPath.GetFileName(assemblyPath),
                OutputDirectory = AssetPath.GetDirectoryName(assemblyPath),
                CompilerOptions = assemblyBuilder.compilerOptions,
                ScriptAssemblyReferences = new ScriptAssembly[0],
                RootNamespace = string.Empty
            };
            scriptAssembly.CompilerOptions.ApiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(assemblyBuilder.buildTargetGroup);

            return scriptAssembly;
        }

        public ScriptAssembly CreateScriptAssembly(AssemblyBuilder assemblyBuilder)
        {
            var scriptAssembly = InitializeScriptAssemblyWithoutReferencesAndDefines(assemblyBuilder);

            var options = ToEditorScriptCompilationOptions(assemblyBuilder.flags);
            var referencesOptions = ToUnityReferencesOptions(assemblyBuilder.referencesOptions);

            var references = GetAssemblyBuilderDefaultReferences(scriptAssembly, options, referencesOptions);

            if (assemblyBuilder.additionalReferences != null && assemblyBuilder.additionalReferences.Length > 0)
            {
                references = references.Concat(assemblyBuilder.additionalReferences).ToArray();
            }

            if (assemblyBuilder.excludeReferences != null && assemblyBuilder.excludeReferences.Length > 0)
            {
                references = references.Where(r => !assemblyBuilder.excludeReferences.Contains(r)).ToArray();
            }

            var defines = GetAssemblyBuilderDefaultDefines(assemblyBuilder);

            if (assemblyBuilder.additionalDefines != null)
            {
                defines = defines.Concat(assemblyBuilder.additionalDefines).ToArray();
            }

            scriptAssembly.References = references.ToArray();
            scriptAssembly.Defines = defines.ToArray();

            return scriptAssembly;
        }

        string[] GetAssemblyBuilderDefaultReferences(ScriptAssembly scriptAssembly, EditorScriptCompilationOptions options, EditorBuildRules.UnityReferencesOptions unityReferencesOptions)
        {
            bool buildingForEditor = (scriptAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;

            var monolithicEngineAssemblyPath = InternalEditorUtility.GetMonolithicEngineAssemblyPath();

            var unityReferences = EditorBuildRules.GetUnityReferences(scriptAssembly, unityAssemblies, options, unityReferencesOptions);

            var customReferences = EditorBuildRules.GetCompiledCustomAssembliesReferences(scriptAssembly, customTargetAssemblies, GetCompileScriptsOutputDirectory());

            var precompiledAssemblies = GetPrecompiledAssembliesWithSetupErrorsTracking(buildingForEditor, EditorUserBuildSettings.activeBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget);
            // todo split implicit/explicit precompiled references
            var precompiledReferences = EditorBuildRules.GetPrecompiledReferences(scriptAssembly, TargetAssemblyType.Custom, options, EditorCompatibility.CompatibleWithEditor, precompiledAssemblies, null, null);
            var additionalReferences = MonoLibraryHelpers.GetSystemLibraryReferences(scriptAssembly.CompilerOptions.ApiCompatibilityLevel);
            string[] editorReferences = buildingForEditor ? ModuleUtils.GetAdditionalReferencesForUserScripts() : new string[0];

            var references = new List<string>();

            if (unityReferencesOptions == EditorBuildRules.UnityReferencesOptions.ExcludeModules)
            {
                references.Add(monolithicEngineAssemblyPath);
            }

            references.AddRange(unityReferences.Values); // unity references paths
            references.AddRange(customReferences);
            references.AddRange(precompiledReferences);
            references.AddRange(editorReferences);
            references.AddRange(additionalReferences);

            return references.ToArray();
        }

        public string[] GetAssemblyBuilderDefaultReferences(AssemblyBuilder assemblyBuilder)
        {
            var scriptAssembly = InitializeScriptAssemblyWithoutReferencesAndDefines(assemblyBuilder);
            var options = ToEditorScriptCompilationOptions(assemblyBuilder.flags);
            var referencesOptions = ToUnityReferencesOptions(assemblyBuilder.referencesOptions);
            var references = GetAssemblyBuilderDefaultReferences(scriptAssembly, options, referencesOptions);

            return references;
        }

        public string[] GetAssemblyBuilderDefaultDefines(AssemblyBuilder assemblyBuilder)
        {
            var options = ToEditorScriptCompilationOptions(assemblyBuilder.flags);
            var defines = InternalEditorUtility.GetCompilationDefines(options, assemblyBuilder.buildTargetGroup, assemblyBuilder.buildTarget);
            return defines;
        }

        public void AddAssemblyBuilder(AssemblyBuilder assemblyBuilder)
        {
            assemblyBuilders.Add(assemblyBuilder);
        }

        public static UnityEditor.Compilation.CompilerMessage[] ConvertCompilerMessages(CompilerMessage[] messages)
        {
            static Compilation.CompilerMessageType TypeFor(CompilerMessageType compilerMessageType)
            {
                switch (compilerMessageType)
                {
                    case CompilerMessageType.Error:
                        return UnityEditor.Compilation.CompilerMessageType.Error;
                    case CompilerMessageType.Warning:
                        return UnityEditor.Compilation.CompilerMessageType.Warning;
                    case CompilerMessageType.Information:
                        return UnityEditor.Compilation.CompilerMessageType.Info;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return messages.Select(message => new UnityEditor.Compilation.CompilerMessage
            {
                message = message.message,
                file = message.file,
                line = message.line,
                column = message.column,
                type = TypeFor(message.type)
            }).ToArray();
        }
    }
}
