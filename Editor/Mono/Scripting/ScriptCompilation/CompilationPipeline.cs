// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Scripting.ScriptCompilation;
using System.Linq;
using UnityEditor.Scripting.Compilers;
using sc = UnityEditor.Scripting.ScriptCompilation;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Compilation
{
    [Flags]
    public enum AssemblyFlags
    {
        None = 0,
        EditorAssembly = (1 << 0)
    }

    public class ScriptCompilerOptions
    {
        public bool AllowUnsafeCode { get; set; }
        public bool EmitReferenceAssembly { get; set; }

        internal bool UseDeterministicCompilation { get; set; }

        public CodeOptimization CodeOptimization { get; set; }
        public ApiCompatibilityLevel ApiCompatibilityLevel { get; set; }
        public string[] ResponseFiles { get; set; }

        public ScriptCompilerOptions()
        {
            AllowUnsafeCode = false;
            ApiCompatibilityLevel = ApiCompatibilityLevel.NET_4_6;
            ResponseFiles = new string[0];
        }
    }

    public enum AssembliesType
    {
        Editor = 0,
        Player = 1,
        PlayerWithoutTestAssemblies = 2,
    }

    public enum AssemblyDefinitionReferenceType
    {
        Name = 0,
        Guid = 1
    }

    public enum CodeOptimization
    {
        None = 0,
        Debug = 1,
        Release = 2
    }

    public class Assembly
    {
        public string name { get; private set; }
        public string rootNamespace { get; private set; }
        public string outputPath { get; private set; }
        public string[] sourceFiles { get; private set; }
        public string[] defines { get; private set; }
        public Assembly[] assemblyReferences { get; internal set; }
        public string[] compiledAssemblyReferences { get; private set; }
        public AssemblyFlags flags { get; private set; }
        public ScriptCompilerOptions compilerOptions { get; private set; }

        public string[] allReferences { get { return assemblyReferences.Select(a => a.outputPath).Concat(compiledAssemblyReferences).ToArray(); } }

        public Assembly(string name,
                        string outputPath,
                        string[] sourceFiles,
                        string[] defines,
                        Assembly[] assemblyReferences,
                        string[] compiledAssemblyReferences,
                        AssemblyFlags flags)
            : this(name,
            outputPath,
            sourceFiles,
            defines,
            assemblyReferences,
            compiledAssemblyReferences,
            flags,
            new ScriptCompilerOptions(),
            string.Empty)
        {
        }

        public Assembly(string name,
                        string outputPath,
                        string[] sourceFiles,
                        string[] defines,
                        Assembly[] assemblyReferences,
                        string[] compiledAssemblyReferences,
                        AssemblyFlags flags,
                        ScriptCompilerOptions compilerOptions)
            : this(name,
            outputPath,
            sourceFiles,
            defines,
            assemblyReferences,
            compiledAssemblyReferences,
            flags,
            compilerOptions,
            string.Empty)
        {
        }

        public Assembly(string name,
                        string outputPath,
                        string[] sourceFiles,
                        string[] defines,
                        Assembly[] assemblyReferences,
                        string[] compiledAssemblyReferences,
                        AssemblyFlags flags,
                        ScriptCompilerOptions compilerOptions,
                        string rootNamespace)
        {
            this.name = name;
            this.outputPath = outputPath;
            this.sourceFiles = sourceFiles;
            this.defines = defines;
            this.assemblyReferences = assemblyReferences;
            this.compiledAssemblyReferences = compiledAssemblyReferences;
            this.flags = flags;
            this.compilerOptions = compilerOptions;
            this.rootNamespace = rootNamespace;
        }
    }

    public class ResponseFileData
    {
        public string[] Defines;
        public string[] FullPathReferences;
        public string[] Errors;
        public string[] OtherArguments;
        public bool Unsafe;
    }

    public struct AssemblyDefinitionPlatform
    {
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public BuildTarget BuildTarget { get; private set; }

        internal AssemblyDefinitionPlatform(string name, string displayName, BuildTarget buildTarget) : this()
        {
            Name = name;
            DisplayName = displayName;
            BuildTarget = buildTarget;
        }
    }

    public static partial class CompilationPipeline
    {
        static AssemblyDefinitionPlatform[] assemblyDefinitionPlatforms;

        public static event Action<object> compilationStarted;
        public static event Action<object> compilationFinished;
        public static event Action<string> assemblyCompilationStarted;
        public static event Action<string, CompilerMessage[]> assemblyCompilationFinished;

        public static event Action<CodeOptimization> codeOptimizationChanged;

        public static CodeOptimization codeOptimization
        {
            get { return IsScriptDebugInfoEnabled() ? CodeOptimization.Debug : CodeOptimization.Release; }
            set
            {
                if (value == codeOptimization)
                {
                    return;
                }

                switch (value)
                {
                    case CodeOptimization.Debug:
                    {
                        EnableScriptDebugInfo();
                        break;
                    }

                    case CodeOptimization.Release:
                    {
                        DisableScriptDebugInfo();
                        break;
                    }

                    default:
                    {
                        throw new ArgumentException(string.Format("Invalid argument {0} provided.", value.ToString()));
                    }
                }
            }
        }

        static CompilationPipeline()
        {
            SubscribeToEvents(EditorCompilationInterface.Instance);
        }

        internal static void SubscribeToEvents(EditorCompilation editorCompilation)
        {
            editorCompilation.compilationStarted += (context) =>
            {
                try
                {
                    if (compilationStarted != null)
                        compilationStarted(context);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log(e);
                }
            };

            editorCompilation.compilationFinished += (context) =>
            {
                try
                {
                    if (compilationFinished != null)
                        compilationFinished(context);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log(e);
                }
            };

            editorCompilation.assemblyCompilationStarted += (assemblyPath) =>
            {
                try
                {
                    if (assemblyCompilationStarted != null)
                        assemblyCompilationStarted(assemblyPath);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            };

            editorCompilation.assemblyCompilationFinished += (assembly, messages, editorScriptCompilationSettings) =>
            {
                try
                {
                    if (assemblyCompilationFinished != null)
                        assemblyCompilationFinished(assembly.FullPath, messages);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            };
        }

        public static string[] GetSystemAssemblyDirectories(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            return MonoLibraryHelpers.GetSystemReferenceDirectories(apiCompatibilityLevel);
        }

        public static ResponseFileData ParseResponseFile(string relativePath, string projectDirectory, string[] systemReferenceDirectories)
        {
            return MicrosoftResponseFileParser.ParseResponseFileFromFile(relativePath, projectDirectory, systemReferenceDirectories);
        }

        public static Assembly[] GetAssemblies()
        {
            return GetAssemblies(AssembliesType.Editor);
        }

        public static Assembly[] GetAssemblies(AssembliesType assembliesType)
        {
            return GetAssemblies(EditorCompilationInterface.Instance, assembliesType);
        }

        internal static Assembly[] GetAssemblies(EditorCompilation editorCompilation, AssembliesType assembliesType)
        {
            var options = EditorCompilationInterface.GetAdditionalEditorScriptCompilationOptions();

            switch (assembliesType)
            {
                case AssembliesType.Editor:
                    return GetEditorAssemblies(editorCompilation, options | EditorScriptCompilationOptions.BuildingIncludingTestAssemblies, null);
                case AssembliesType.Player:
                    return GetPlayerAssemblies(editorCompilation, options | EditorScriptCompilationOptions.BuildingIncludingTestAssemblies, null);
                case AssembliesType.PlayerWithoutTestAssemblies:
                    return GetPlayerAssemblies(editorCompilation, options, null);
                default:
                    throw new ArgumentOutOfRangeException("assembliesType");
            }
        }

        public static string GetAssemblyNameFromScriptPath(string sourceFilePath)
        {
            return GetAssemblyNameFromScriptPath(EditorCompilationInterface.Instance, sourceFilePath);
        }

        public static string GetAssemblyDefinitionFilePathFromScriptPath(string sourceFilePath)
        {
            return GetAssemblyDefinitionFilePathFromScriptPath(EditorCompilationInterface.Instance, sourceFilePath);
        }

        public static string GetAssemblyDefinitionFilePathFromAssemblyName(string assemblyName)
        {
            return GetAssemblyDefinitionFilePathFromAssemblyName(EditorCompilationInterface.Instance, assemblyName);
        }

        public static string GetAssemblyDefinitionFilePathFromAssemblyReference(string reference)
        {
            return GetAssemblyDefinitionFilePathFromAssemblyReference(EditorCompilationInterface.Instance, reference);
        }

        public static AssemblyDefinitionReferenceType GetAssemblyDefinitionReferenceType(string reference)
        {
            return GUIDReference.IsGUIDReference(reference) ? AssemblyDefinitionReferenceType.Guid : AssemblyDefinitionReferenceType.Name;
        }

        public static string GUIDToAssemblyDefinitionReferenceGUID(string guid)
        {
            return GUIDReference.GUIDToGUIDReference(guid);
        }

        public static string AssemblyDefinitionReferenceGUIDToGUID(string reference)
        {
            if (GetAssemblyDefinitionReferenceType(reference) != AssemblyDefinitionReferenceType.Guid)
                throw new ArgumentException($"{reference} is not a GUID reference", "reference");

            return GUIDReference.GUIDReferenceToGUID(reference);
        }

        public static string GetAssemblyRootNamespaceFromScriptPath(string sourceFilePath)
        {
            var projectRootNamespace = UnityEditor.EditorSettings.projectGenerationRootNamespace;
            return GetAssemblyRootNamespaceFromScriptPath(EditorCompilationInterface.Instance, projectRootNamespace, sourceFilePath);
        }

        public static AssemblyDefinitionPlatform[] GetAssemblyDefinitionPlatforms()
        {
            if (assemblyDefinitionPlatforms == null)
            {
                assemblyDefinitionPlatforms = CustomScriptAssembly.Platforms.Select(p => new AssemblyDefinitionPlatform(p.Name, p.DisplayName, p.BuildTarget)).ToArray();
                Array.Sort(assemblyDefinitionPlatforms, CompareAssemblyDefinitionPlatformByDisplayName);
            }

            return assemblyDefinitionPlatforms;
        }

        public static string[] GetDefinesFromAssemblyName(string assemblyName)
        {
            return GetDefinesFromAssemblyName(EditorCompilationInterface.Instance, assemblyName);
        }

        internal static string[] GetDefinesFromAssemblyName(EditorCompilation editorCompilation, string assemblyName)
        {
            try
            {
                var assembly = editorCompilation.GetCustomTargetAssemblyFromName(assemblyName);
                return assembly?.Defines;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static string[] GetPrecompiledAssemblyNames()
        {
            var precompiledAssemblyProvider = EditorCompilationInterface.Instance.PrecompiledAssemblyProvider;
            return GetPrecompiledAssemblyNames(precompiledAssemblyProvider);
        }

        internal static string[] GetPrecompiledAssemblyNames(PrecompiledAssemblyProviderBase precompiledAssemblyProvider)
        {
            return precompiledAssemblyProvider.GetPrecompiledAssemblies(true, EditorUserBuildSettings.activeBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget)
                .Where(x => (x.Flags & sc.AssemblyFlags.UserAssembly) == sc.AssemblyFlags.UserAssembly)
                .Select(x => AssetPath.GetFileName(x.Path))
                .ToArray();
        }

        public static bool IsDefineConstraintsCompatible(string[] defines, string[] defineConstraints)
        {
            return DefineConstraintsHelper.IsDefineConstraintsCompatible(defines, defineConstraints);
        }

        [Flags]
        public enum PrecompiledAssemblySources
        {
            UserAssembly = 1 << 0,
            UnityEngine = 1 << 1,
            UnityEditor = 1 << 2,
            SystemAssembly = 1 << 3,
            All = ~0
        }

        public static string[] GetPrecompiledAssemblyPaths(PrecompiledAssemblySources precompiledAssemblySources)
        {
            var precompiledAssemblyProvider = EditorCompilationInterface.Instance.PrecompiledAssemblyProvider;
            return GetPrecompiledAssemblyPaths(precompiledAssemblySources, precompiledAssemblyProvider);
        }

        internal static string[] GetPrecompiledAssemblyPaths(PrecompiledAssemblySources precompiledAssemblySources, PrecompiledAssemblyProviderBase precompiledAssemblyProvider)
        {
            HashSet<string> assemblyNames = new HashSet<string>();
            sc.AssemblyFlags flags = sc.AssemblyFlags.None;
            if ((precompiledAssemblySources & PrecompiledAssemblySources.SystemAssembly) != 0)
            {
                foreach (var a in MonoLibraryHelpers.GetSystemLibraryReferences(ApiCompatibilityLevel.NET_4_6, Scripting.ScriptCompilers.CSharpSupportedLanguage))
                {
                    assemblyNames.Add(a);
                }
            }

            if ((precompiledAssemblySources & PrecompiledAssemblySources.UnityEngine) != 0)
                flags |= sc.AssemblyFlags.UnityModule;

            if ((precompiledAssemblySources & PrecompiledAssemblySources.UnityEditor) != 0)
                flags |= sc.AssemblyFlags.EditorOnly;

            if ((precompiledAssemblySources & PrecompiledAssemblySources.UserAssembly) != 0)
                flags |= sc.AssemblyFlags.UserAssembly;

            var precompiledAssemblies = precompiledAssemblyProvider.GetPrecompiledAssemblies(true, EditorUserBuildSettings.activeBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget).Concat(EditorCompilationInterface.Instance.GetUnityAssemblies());
            foreach (var a in precompiledAssemblies.Where(x => (x.Flags & flags) != 0))
                assemblyNames.Add(a.Path);

            return assemblyNames.ToArray();
        }

        public static string GetPrecompiledAssemblyPathFromAssemblyName(string assemblyName)
        {
            var precompiledAssemblyProvider = EditorCompilationInterface.Instance.PrecompiledAssemblyProvider;
            return GetPrecompiledAssemblyPathFromAssemblyName(assemblyName, precompiledAssemblyProvider);
        }

        internal static string GetPrecompiledAssemblyPathFromAssemblyName(string assemblyName, PrecompiledAssemblyProviderBase precompiledAssemblyProvider)
        {
            var precompiledAssemblies = precompiledAssemblyProvider.GetPrecompiledAssemblies(true, EditorUserBuildSettings.activeBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget);

            foreach (var assembly in precompiledAssemblies)
            {
                if ((assembly.Flags & sc.AssemblyFlags.UserAssembly) == sc.AssemblyFlags.UserAssembly && AssetPath.GetFileName(assembly.Path) == assemblyName)
                {
                    return assembly.Path;
                }
            }
            return null;
        }

        private static Assembly[] GetEditorAssemblies(EditorCompilation editorCompilation, EditorScriptCompilationOptions additionalOptions, string[] defines)
        {
            var scriptAssemblies = editorCompilation.GetAllScriptAssemblies(EditorScriptCompilationOptions.BuildingForEditor | additionalOptions, defines);
            return ToAssemblies(scriptAssemblies);
        }

        internal static Assembly[] GetPlayerAssemblies(EditorCompilation editorCompilation, EditorScriptCompilationOptions options, string[] defines)
        {
            var group = EditorUserBuildSettings.activeBuildTargetGroup;
            var target = EditorUserBuildSettings.activeBuildTarget;

            PrecompiledAssembly[] unityAssemblies = InternalEditorUtility.GetUnityAssemblies(false, group, target);
            var precompiledAssemblies = EditorCompilationInterface.Instance.PrecompiledAssemblyProvider.GetPrecompiledAssembliesDictionary(false, group, target);

            var scriptAssemblies = editorCompilation.GetAllScriptAssemblies(options, unityAssemblies, precompiledAssemblies, defines);
            return ToAssemblies(scriptAssemblies);
        }

        internal static Assembly[] ToAssemblies(ScriptAssembly[] scriptAssemblies)
        {
            var assemblies = new Assembly[scriptAssemblies.Length];

            for (int i = 0; i < scriptAssemblies.Length; ++i)
            {
                var scriptAssembly = scriptAssemblies[i];

                var name = AssetPath.GetAssemblyNameWithoutExtension(scriptAssembly.Filename);
                var outputPath = scriptAssembly.FullPath;
                var sourceFiles = scriptAssembly.Files;
                var defines = scriptAssembly.Defines;
                var compiledAssemblyReferences = scriptAssembly.References;

                var flags = AssemblyFlags.None;

                if ((scriptAssembly.Flags & sc.AssemblyFlags.EditorOnly) == sc.AssemblyFlags.EditorOnly)
                    flags |= AssemblyFlags.EditorAssembly;

                var compilerOptions = scriptAssembly.CompilerOptions;
                compilerOptions.ResponseFiles = scriptAssembly.GetResponseFiles();

                assemblies[i] = new Assembly(name,
                    outputPath,
                    sourceFiles,
                    defines,
                    null,
                    compiledAssemblyReferences,
                    flags,
                    compilerOptions,
                    scriptAssembly.RootNamespace);
            }

            var scriptAssemblyToAssembly = new Dictionary<ScriptAssembly, Assembly>();

            for (int i = 0; i < scriptAssemblies.Length; ++i)
                scriptAssemblyToAssembly.Add(scriptAssemblies[i], assemblies[i]);

            for (int i = 0; i < scriptAssemblies.Length; ++i)
            {
                var scriptAssembly = scriptAssemblies[i];
                var assemblyReferences = scriptAssembly.ScriptAssemblyReferences.Select(a => scriptAssemblyToAssembly[a]).Where(a => !IsInternalPlugin(a.outputPath)).ToArray();

                assemblies[i].assemblyReferences = assemblyReferences;
            }


            return assemblies;
        }

        static bool IsInternalPlugin(string fullReference)
        {
            if (AssemblyHelper.IsInternalAssembly(fullReference))
            {
                if (!Modules.ModuleUtils.GetAdditionalReferencesForEditorCsharpProject().Contains(fullReference))
                    return true;
            }
            return false;
        }

        static int CompareAssemblyDefinitionPlatformByDisplayName(AssemblyDefinitionPlatform p1, AssemblyDefinitionPlatform p2)
        {
            return string.Compare(p1.DisplayName, p2.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        internal static string GetAssemblyNameFromScriptPath(EditorCompilation editorCompilation, string sourceFilePath)
        {
            try
            {
                var targetAssembly = editorCompilation.GetTargetAssembly(sourceFilePath);
                return targetAssembly.Name;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static string GetAssemblyDefinitionFilePathFromAssemblyName(EditorCompilation editorCompilation, string assemblyName)
        {
            try
            {
                var customScriptAssembly = editorCompilation.FindCustomScriptAssemblyFromAssemblyName(assemblyName);
                return customScriptAssembly.FilePath;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static string GetAssemblyDefinitionFilePathFromAssemblyReference(EditorCompilation editorCompilation,
            string reference)
        {
            try
            {
                var customScriptAssembly = editorCompilation.FindCustomScriptAssemblyFromAssemblyReference(reference);
                return customScriptAssembly.FilePath;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static string GetAssemblyRootNamespaceFromScriptPath(EditorCompilation editorCompilation, string projectRootNamespace, string sourceFilePath)
        {
            try
            {
                var csa = editorCompilation.FindCustomScriptAssemblyFromScriptPath(sourceFilePath);
                return csa != null ? csa.RootNamespace : projectRootNamespace;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static string GetAssemblyDefinitionFilePathFromScriptPath(EditorCompilation editorCompilation, string sourceFilePath)
        {
            try
            {
                var customScriptAssembly = editorCompilation.FindCustomScriptAssemblyFromScriptPath(sourceFilePath);
                return customScriptAssembly != null ? customScriptAssembly.FilePath : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void RequestScriptCompilation()
        {
            RequestScriptCompilation(EditorCompilationInterface.Instance);
        }

        internal static void RequestScriptCompilation(EditorCompilation editorCompilation)
        {
            editorCompilation.DirtyAllScripts();
        }

        [RequiredByNativeCode]
        internal static void OnCodeOptimizationChanged(bool scriptDebugInfoEnabled)
        {
            if (codeOptimizationChanged != null)
            {
                codeOptimizationChanged(scriptDebugInfoEnabled ? CodeOptimization.Debug : CodeOptimization.Release);
            }
        }
    }
}
