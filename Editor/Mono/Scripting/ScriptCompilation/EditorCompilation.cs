// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEditor.Modules;
using UnityEditor.Compilation;
using UnityEditor.Scripting.Compilers;
using UnityEditorInternal;
using CompilerMessage = UnityEditor.Scripting.Compilers.CompilerMessage;
using CompilerMessageType = UnityEditor.Scripting.Compilers.CompilerMessageType;
using Directory = System.IO.Directory;
using File = System.IO.File;
using IOException = System.IO.IOException;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class EditorCompilation
    {
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

        [StructLayout(LayoutKind.Sequential)]
        public struct AssemblyCompilerMessages
        {
            public string assemblyFilename;
            public CompilerMessage[] messages;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PackageAssembly
        {
            public string DirectoryPath;
            public string Name;
            public bool IncludeTestAssemblies;
        }

        public struct CustomScriptAssemblyAndReference
        {
            public CustomScriptAssembly Assembly;
            public CustomScriptAssembly Reference;
        }

        [Flags]
        public enum CompilationSetupErrorFlags
        {
            none = 0,
            cyclicReferences = (1 << 0),
            loadError = (1 << 1)
        }

        [Flags]
        public enum CompileScriptAssembliesOptions
        {
            none = 0,
            skipSetupChecks = (1 << 0),
        }

        abstract class UnitySpecificCompilerMessageProcessor
        {
            public abstract bool IsInterestedInMessage(CompilerMessage m);
            public abstract void PostprocessMessage(ref CompilerMessage m);
        }

        class UnsafeErrorProcessor : UnitySpecificCompilerMessageProcessor
        {
            string unityUnsafeMessage;

            public UnsafeErrorProcessor(ScriptAssembly assembly, EditorCompilation editorCompilation)
            {
                var assemblyName = AssetPath.GetAssemblyNameWithoutExtension(assembly.Filename);

                try
                {
                    var customScriptAssembly = editorCompilation.FindCustomScriptAssemblyFromAssemblyName(assemblyName);
                    unityUnsafeMessage = string.Format("Enable \"Allow 'unsafe' code\" in the inspector for '{0}' to fix this error.", customScriptAssembly.FilePath);
                }
                catch
                {
                    unityUnsafeMessage = "Enable \"Allow 'unsafe' code\" in Player Settings to fix this error.";
                }
            }

            public override bool IsInterestedInMessage(CompilerMessage m)
            {
                return m.type == CompilerMessageType.Error && m.message.Contains("CS0227");
            }

            public override void PostprocessMessage(ref CompilerMessage m)
            {
                m.message += ". " + unityUnsafeMessage;
            }
        }

        class ModuleReferenceErrorProcessor : UnitySpecificCompilerMessageProcessor
        {
            Regex messageRegex;

            public ModuleReferenceErrorProcessor()
            {
                messageRegex = new Regex("[`']UnityEngine.(\\w*)Module,");
            }

            public override bool IsInterestedInMessage(CompilerMessage m)
            {
                return m.type == CompilerMessageType.Error && (m.message.Contains("CS1069") || m.message.Contains("CS1070"));
            }

            private static string GetNiceDisplayNameForModule(string name)
            {
                for (int i = 1; i < name.Length; i++)
                    if (char.IsLower(name[i - 1]) && !char.IsLower(name[i]))
                    {
                        name = name.Insert(i, " ");
                        i++;
                    }
                return name;
            }

            public override void PostprocessMessage(ref CompilerMessage message)
            {
                var match = messageRegex.Match(message.message);
                if (match.Success)
                {
                    var index = message.message.IndexOf(EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest ? "Consider adding a reference to that assembly." : "Consider adding a reference to assembly");
                    if (index != -1)
                        message.message = message.message.Substring(0, index);
                    var moduleName = match.Groups[1].Value;
                    var excludingModuleName = ModuleMetadata.GetExcludingModule(moduleName);
                    moduleName = GetNiceDisplayNameForModule(moduleName);
                    excludingModuleName = GetNiceDisplayNameForModule(excludingModuleName);

                    if (moduleName == excludingModuleName)
                        message.message += string.Format("Enable the built in package '{0}' in the Package Manager window to fix this error.", moduleName);
                    else
                        message.message += string.Format("Enable the built in package '{0}', which is required by package '{1}', in the Package Manager window to fix this error.", excludingModuleName, moduleName);
                }
            }
        }

        bool areAllScriptsDirty;
        bool areAllPrecompiledAssembliesDirty;
        string projectDirectory = string.Empty;
        HashSet<string> allScripts = new HashSet<string>();
        HashSet<string> dirtyScripts = new HashSet<string>();
        HashSet<EditorBuildRules.TargetAssembly> dirtyTargetAssemblies = new HashSet<EditorBuildRules.TargetAssembly>();
        HashSet<PrecompiledAssembly> dirtyPrecompiledAssemblies = new HashSet<PrecompiledAssembly>();
        HashSet<string> runScriptUpdaterAssemblies = new HashSet<string>();
        bool recompileAllScriptsOnNextTick;
        PrecompiledAssembly[] precompiledAssemblies;
        CustomScriptAssembly[] customScriptAssemblies = new CustomScriptAssembly[0];
        EditorBuildRules.TargetAssembly[] customTargetAssemblies; // TargetAssemblies for customScriptAssemblies.
        PrecompiledAssembly[] unityAssemblies;
        CompilationTask compilationTask;
        string outputDirectory;
        CompilationSetupErrorFlags setupErrorFlags = CompilationSetupErrorFlags.none;
        List<Compilation.AssemblyBuilder> assemblyBuilders = new List<Compilation.AssemblyBuilder>();

        static readonly string EditorTempPath = "Temp";

        public Action<CompilationSetupErrorFlags> setupErrorFlagsChanged;
        private PackageAssembly[] m_PackageAssemblies;
        public event Action<string> assemblyCompilationStarted;
        public event Action<string, UnityEditor.Compilation.CompilerMessage[]> assemblyCompilationFinished;

        static EditorCompilation()
        {
        }

        internal string GetAssemblyTimestampPath(string editorAssemblyPath)
        {
            return AssetPath.Combine(editorAssemblyPath, "BuiltinAssemblies.stamp");
        }

        internal void SetProjectDirectory(string projectDirectory)
        {
            this.projectDirectory = projectDirectory;
        }

        public void SetAllScripts(string[] allScripts)
        {
            this.allScripts = new HashSet<string>(allScripts);

            foreach (var dirtyScript in dirtyScripts)
                this.allScripts.Add(dirtyScript);
        }

        public bool IsExtensionSupportedByCompiler(string extension)
        {
            var languages = ScriptCompilers.SupportedLanguages;
            return languages.Count(l => l.GetExtensionICanCompile() == extension) > 0;
        }

        public string[] GetExtensionsSupportedByCompiler()
        {
            var languages = ScriptCompilers.SupportedLanguages;
            return languages.Select(language => language.GetExtensionICanCompile()).ToArray();
        }

        public void DirtyPredefinedAssemblyScripts(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            var scriptAssemblySettings = CreateScriptAssemblySettings(platformGroup, platform, options);
            var scriptAssemblies = GetAllScriptAssembliesOfType(scriptAssemblySettings, EditorBuildRules.TargetAssemblyType.Predefined);

            foreach (var assembly in scriptAssemblies)
            {
                foreach (var script in assembly.Files)
                {
                    dirtyScripts.Add(script);
                }
            }
        }

        public void DirtyAllScripts()
        {
            areAllScriptsDirty = true;
        }

        public void DirtyScript(string path)
        {
            allScripts.Add(path);
            dirtyScripts.Add(path);
        }

        public void DirtyMovedScript(string oldPath, string newPath)
        {
            DirtyScript(newPath);

            var targetAssembly = EditorBuildRules.GetTargetAssembly(oldPath, projectDirectory, customTargetAssemblies);

            // The target assembly might not exist any more.
            if (targetAssembly == null)
            {
                areAllScriptsDirty = true;
            }
            else
            {
                dirtyTargetAssemblies.Add(targetAssembly);
            }
        }

        public void DirtyRemovedScript(string path)
        {
            allScripts.Remove(path);
            dirtyScripts.Remove(path);

            var targetAssembly = EditorBuildRules.GetTargetAssembly(path, projectDirectory, customTargetAssemblies);

            // The target assembly might not exist any more.
            if (targetAssembly == null)
            {
                areAllScriptsDirty = true;
            }
            else
            {
                dirtyTargetAssemblies.Add(targetAssembly);
            }
        }

        public void DirtyPrecompiledAssembly(string path)
        {
            var precompiledAssembly = GetPrecompiledAssemblyFromPath(path);

            if (!precompiledAssembly.HasValue)
            {
                areAllPrecompiledAssembliesDirty = true;
                return;
            }

            var explicitlyReferenced = (precompiledAssembly.Value.Flags & AssemblyFlags.ExplicitlyReferenced) == AssemblyFlags.ExplicitlyReferenced;

            // If the precompiled assembly is not explicitly referenced, then
            // all scripts reference it and all scripts must be recompiled.
            if (!explicitlyReferenced)
            {
                areAllPrecompiledAssembliesDirty = true;
            }
            else
            {
                dirtyPrecompiledAssemblies.Add(precompiledAssembly.Value);
            }
        }

        public void RecompileAllScriptsOnNextTick()
        {
            recompileAllScriptsOnNextTick = true;
        }

        public bool WillRecompileAllScriptsOnNextTick()
        {
            return recompileAllScriptsOnNextTick;
        }

        public void ClearDirtyScripts()
        {
            dirtyScripts.Clear();
            areAllScriptsDirty = false;
            dirtyTargetAssemblies.Clear();
            dirtyPrecompiledAssemblies.Clear();
            areAllPrecompiledAssembliesDirty = false;
        }

        public void RunScriptUpdaterOnAssembly(string assemblyFilename)
        {
            runScriptUpdaterAssemblies.Add(assemblyFilename);
        }

        public void SetAllUnityAssemblies(PrecompiledAssembly[] unityAssemblies)
        {
            this.unityAssemblies = unityAssemblies;
        }

        public void SetCompileScriptsOutputDirectory(string directory)
        {
            this.outputDirectory = directory;
        }

        public string GetCompileScriptsOutputDirectory()
        {
            if (string.IsNullOrEmpty(outputDirectory))
                throw new Exception("Must set an output directory through SetCompileScriptsOutputDirectory before compiling");
            return outputDirectory;
        }

        public void SetCompilationSetupErrorFlags(CompilationSetupErrorFlags flags)
        {
            var newFlags = setupErrorFlags | flags;

            if (newFlags != setupErrorFlags)
            {
                setupErrorFlags = newFlags;

                if (setupErrorFlagsChanged != null)
                    setupErrorFlagsChanged(setupErrorFlags);
            }
        }

        public void ClearCompilationSetupErrorFlags(CompilationSetupErrorFlags flags)
        {
            var newFlags = setupErrorFlags & ~flags;

            if (newFlags != setupErrorFlags)
            {
                setupErrorFlags = newFlags;

                if (setupErrorFlagsChanged != null)
                    setupErrorFlagsChanged(setupErrorFlags);
            }
        }

        public bool HaveSetupErrors()
        {
            return setupErrorFlags != CompilationSetupErrorFlags.none;
        }

        public void SetAllPrecompiledAssemblies(PrecompiledAssembly[] precompiledAssemblies)
        {
            this.precompiledAssemblies = precompiledAssemblies;
        }

        public PrecompiledAssembly[] GetAllPrecompiledAssemblies()
        {
            return this.precompiledAssemblies;
        }

        public TargetAssemblyInfo[] GetAllCompiledAndResolvedCustomTargetAssemblies(EditorScriptCompilationOptions options, BuildTarget buildTarget, out CustomScriptAssemblyAndReference[] assembliesWithMissingReference)
        {
            if (customTargetAssemblies == null)
            {
                assembliesWithMissingReference = new CustomScriptAssemblyAndReference[0];
                return new TargetAssemblyInfo[0];
            }

            var customTargetAssemblyCompiledPaths = new Dictionary<EditorBuildRules.TargetAssembly, string>();

            foreach (var assembly in customTargetAssemblies)
            {
                var path = assembly.FullPath(outputDirectory);

                // Collect all assemblies that have been compiled (exist on file system)
                if (File.Exists(path))
                    customTargetAssemblyCompiledPaths.Add(assembly, path);
            }

            bool removed;

            var removedAssemblies = new List<CustomScriptAssemblyAndReference>();

            do
            {
                removed = false;

                if (customTargetAssemblyCompiledPaths.Count > 0)
                {
                    foreach (var assembly in customTargetAssemblies)
                    {
                        if (!customTargetAssemblyCompiledPaths.ContainsKey(assembly))
                            continue;

                        // Check for each compiled assembly that all it's references
                        // have also been compiled. If not, remove it from the list
                        // of compiled assemblies.
                        foreach (var reference in assembly.References)
                        {
                            // Don't check references that are not compatible with the current build target,
                            // as those assemblies have not been compiled.
                            if (!EditorBuildRules.IsCompatibleWithPlatformAndDefines(reference, buildTarget, options))
                                continue;

                            if (!customTargetAssemblyCompiledPaths.ContainsKey(reference))
                            {
                                customTargetAssemblyCompiledPaths.Remove(assembly);

                                var customScriptAssembly = FindCustomTargetAssemblyFromTargetAssembly(assembly);
                                var customScriptAssemblyReference = FindCustomTargetAssemblyFromTargetAssembly(reference);

                                removedAssemblies.Add(new CustomScriptAssemblyAndReference { Assembly = customScriptAssembly, Reference = customScriptAssemblyReference });
                                removed = true;
                                break;
                            }
                        }
                    }
                }
            }
            while (removed);

            var count = customTargetAssemblyCompiledPaths.Count;
            var targetAssemblies = new TargetAssemblyInfo[customTargetAssemblyCompiledPaths.Count];
            int index = 0;

            foreach (var entry in customTargetAssemblyCompiledPaths)
            {
                var assembly = entry.Key;
                targetAssemblies[index++] = ToTargetAssemblyInfo(assembly);
            }

            assembliesWithMissingReference = removedAssemblies.ToArray();

            return targetAssemblies;
        }

        static CustomScriptAssembly LoadCustomScriptAssemblyFromJson(string path)
        {
            var json = File.ReadAllText(path);

            try
            {
                var customScriptAssemblyData = CustomScriptAssemblyData.FromJson(json);
                return CustomScriptAssembly.FromCustomScriptAssemblyData(path, customScriptAssemblyData);
            }
            catch (Exception e)
            {
                throw new Compilation.AssemblyDefinitionException(e.Message, path);
            }
        }

        string[] CustomTargetAssembliesToFilePaths(IEnumerable<EditorBuildRules.TargetAssembly> targetAssemblies)
        {
            var customAssemblies = targetAssemblies.Select(a => FindCustomTargetAssemblyFromTargetAssembly(a));
            var filePaths = customAssemblies.Select(a => a.FilePath).ToArray();
            return filePaths;
        }

        string CustomTargetAssemblyToFilePath(EditorBuildRules.TargetAssembly targetAssembly)
        {
            return FindCustomTargetAssemblyFromTargetAssembly(targetAssembly).FilePath;
        }

        public struct CheckCyclicAssemblyReferencesFunctions
        {
            public Func<EditorBuildRules.TargetAssembly, string> ToFilePathFunc;
            public Func<IEnumerable<EditorBuildRules.TargetAssembly>, string[]> ToFilePathsFunc;
        }

        static void CheckCyclicAssemblyReferencesDFS(EditorBuildRules.TargetAssembly visitAssembly,
            HashSet<EditorBuildRules.TargetAssembly> visited,
            HashSet<EditorBuildRules.TargetAssembly> recursion,
            CheckCyclicAssemblyReferencesFunctions functions)
        {
            visited.Add(visitAssembly);
            recursion.Add(visitAssembly);

            foreach (var reference in visitAssembly.References)
            {
                if (reference.Filename == visitAssembly.Filename)
                {
                    throw new Compilation.AssemblyDefinitionException("Assembly contains a references to itself",
                        functions.ToFilePathFunc(visitAssembly));
                }

                if (recursion.Contains(reference))
                {
                    throw new Compilation.AssemblyDefinitionException("Assembly with cyclic references detected",
                        functions.ToFilePathsFunc(recursion));
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

        public static void CheckCyclicAssemblyReferences(EditorBuildRules.TargetAssembly[] customTargetAssemblies,
            CheckCyclicAssemblyReferencesFunctions functions)
        {
            if (customTargetAssemblies == null || customTargetAssemblies.Length < 1)
                return;

            var visited = new HashSet<EditorBuildRules.TargetAssembly>();

            foreach (var assembly in customTargetAssemblies)
            {
                if (!visited.Contains(assembly))
                {
                    var recursion = new HashSet<EditorBuildRules.TargetAssembly>();

                    CheckCyclicAssemblyReferencesDFS(assembly,
                        visited,
                        recursion,
                        functions);
                }
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
            catch (Exception e)
            {
                SetCompilationSetupErrorFlags(CompilationSetupErrorFlags.cyclicReferences);
                throw e;
            }
        }

        public static Exception[] UpdateCustomScriptAssemblies(CustomScriptAssembly[] customScriptAssemblies,
            PackageAssembly[] packageAssemblies)
        {
            var exceptions = new List<Exception>();

            foreach (var assembly in customScriptAssemblies)
            {
                try
                {
                    if (packageAssemblies != null && !assembly.PackageAssembly.HasValue)
                    {
                        var pathPrefix = assembly.PathPrefix.ToLowerInvariant();

                        foreach (var packageAssembly in packageAssemblies)
                        {
                            var lower = AssetPath.ReplaceSeparators(packageAssembly.DirectoryPath + AssetPath.Separator).ToLowerInvariant();
                            if (pathPrefix.StartsWith(lower, StringComparison.Ordinal))
                            {
                                assembly.PackageAssembly = packageAssembly;
                                break;
                            }
                        }
                    }

                    if (assembly.References != null)
                    {
                        foreach (var reference in assembly.References)
                        {
                            if (!customScriptAssemblies.Any(a => a.Name == reference))
                                throw new Compilation.AssemblyDefinitionException(string.Format("Assembly has reference to non-existent assembly '{0}'", reference), assembly.FilePath);
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
            var exceptions = UpdateCustomScriptAssemblies(customScriptAssemblies, m_PackageAssemblies);

            if (exceptions.Length > 0)
            {
                SetCompilationSetupErrorFlags(CompilationSetupErrorFlags.loadError);
            }

            customTargetAssemblies = EditorBuildRules.CreateTargetAssemblies(customScriptAssemblies, precompiledAssemblies);
            ClearCompilationSetupErrorFlags(CompilationSetupErrorFlags.cyclicReferences);

            // Remap dirtyTargetAssemblies to new objects created due to
            // customTargetAssemblies being updated.
            UpdateDirtyTargetAssemblies();

            return exceptions;
        }

        void UpdateDirtyTargetAssemblies()
        {
            if (dirtyTargetAssemblies.Count == 0)
                return;

            var dirtyTargetAssemblyFilenames = new HashSet<string>();

            foreach (var targetAssembly in dirtyTargetAssemblies)
            {
                dirtyTargetAssemblyFilenames.Add(targetAssembly.Filename);
            }

            var newDirtyTargetAssemblies = new HashSet<EditorBuildRules.TargetAssembly>();

            if (customTargetAssemblies != null)
            {
                foreach (var customTargetAssembly in customTargetAssemblies)
                {
                    if (dirtyTargetAssemblyFilenames.Contains(customTargetAssembly.Filename))
                    {
                        newDirtyTargetAssemblies.Add(customTargetAssembly);
                    }
                }
            }

            var predefinedTargetAssemblies = EditorBuildRules.GetPredefinedTargetAssemblies();

            foreach (var predefinedTargetAssembly in predefinedTargetAssemblies)
            {
                if (dirtyTargetAssemblies.Contains(predefinedTargetAssembly))
                {
                    newDirtyTargetAssemblies.Add(predefinedTargetAssembly);
                }
            }

            dirtyTargetAssemblies = newDirtyTargetAssemblies;
        }

        public Exception[] SetAllCustomScriptAssemblyJsons(string[] paths)
        {
            var assemblies = new List<CustomScriptAssembly>();
            var exceptions = new List<Exception>();

            ClearCompilationSetupErrorFlags(CompilationSetupErrorFlags.loadError);

            foreach (var path in paths)
            {
                var fullPath = AssetPath.IsPathRooted(path) ? AssetPath.GetFullPath(path) : AssetPath.Combine(projectDirectory, path);
                CustomScriptAssembly loadedCustomScriptAssembly = null;

                try
                {
                    loadedCustomScriptAssembly = LoadCustomScriptAssemblyFromJson(fullPath);

                    var duplicates = assemblies.Where(a => string.Equals(a.Name, loadedCustomScriptAssembly.Name, System.StringComparison.OrdinalIgnoreCase));

                    if (duplicates.Any())
                    {
                        var filePaths = new List<string>();
                        filePaths.Add(loadedCustomScriptAssembly.FilePath);
                        filePaths.AddRange(duplicates.Select(a => a.FilePath));
                        throw new Compilation.AssemblyDefinitionException(string.Format("Assembly with name '{0}' already exists", loadedCustomScriptAssembly.Name), filePaths.ToArray());
                    }

                    var samePrefixes = assemblies.Where(a => a.PathPrefix == loadedCustomScriptAssembly.PathPrefix);

                    if (samePrefixes.Any())
                    {
                        var filePaths = new List<string>();
                        filePaths.Add(loadedCustomScriptAssembly.FilePath);
                        filePaths.AddRange(samePrefixes.Select(a => a.FilePath));
                        throw new Compilation.AssemblyDefinitionException(string.Format("Folder '{0}' contains multiple assembly definition files", loadedCustomScriptAssembly.PathPrefix), filePaths.ToArray());
                    }

                    if (loadedCustomScriptAssembly.References == null)
                        loadedCustomScriptAssembly.References = new string[0];

                    if (loadedCustomScriptAssembly.References.Length != loadedCustomScriptAssembly.References.Distinct().Count())
                    {
                        var duplicateRefs = loadedCustomScriptAssembly.References.GroupBy(r => r).Where(g => g.Count() > 0).Select(g => g.Key).ToArray();
                        var duplicateRefsString = string.Join(",", duplicateRefs);

                        throw new Compilation.AssemblyDefinitionException(string.Format("Assembly has duplicate references: {0}",
                            duplicateRefsString),
                            loadedCustomScriptAssembly.FilePath);
                    }
                }
                catch (Exception e)
                {
                    SetCompilationSetupErrorFlags(CompilationSetupErrorFlags.loadError);
                    exceptions.Add(e);
                }

                if (loadedCustomScriptAssembly != null && !assemblies.Any(a => a.Name.Equals(loadedCustomScriptAssembly.Name, StringComparison.OrdinalIgnoreCase)))
                    assemblies.Add(loadedCustomScriptAssembly);
            }

            customScriptAssemblies = assemblies.ToArray();

            var updateCustomTargetAssembliesExceptions = UpdateCustomTargetAssemblies();
            exceptions.AddRange(updateCustomTargetAssembliesExceptions);

            return exceptions.ToArray();
        }

        public bool IsPathInPackageDirectory(string path)
        {
            if (m_PackageAssemblies == null)
                return false;
            return m_PackageAssemblies.Any(p => path.StartsWith(p.DirectoryPath, StringComparison.OrdinalIgnoreCase));
        }

        public void SetAllPackageAssemblies(PackageAssembly[] packageAssemblies)
        {
            m_PackageAssemblies = packageAssemblies;
        }

        private static CustomScriptAssembly CreatePackageCustomScriptAssembly(PackageAssembly packageAssembly)
        {
            var customScriptAssembly = CustomScriptAssembly.Create(packageAssembly.Name, AssetPath.ReplaceSeparators(packageAssembly.DirectoryPath));
            customScriptAssembly.PackageAssembly = packageAssembly;
            return customScriptAssembly;
        }

        // Delete all .dll's that aren't used anymore
        public void DeleteUnusedAssemblies()
        {
            string fullEditorAssemblyPath = AssetPath.Combine(projectDirectory, GetCompileScriptsOutputDirectory());

            if (!Directory.Exists(fullEditorAssemblyPath))
                return;

            var deleteFiles = Directory.GetFiles(fullEditorAssemblyPath).Select(f => AssetPath.ReplaceSeparators(f)).ToList();
            string timestampPath = GetAssemblyTimestampPath(GetCompileScriptsOutputDirectory());
            deleteFiles.Remove(AssetPath.Combine(projectDirectory, timestampPath));

            var scriptAssemblies = GetAllScriptAssemblies(EditorScriptCompilationOptions.BuildingForEditor);

            foreach (var assembly in scriptAssemblies)
            {
                if (assembly.Files.Length > 0)
                {
                    string path = AssetPath.Combine(fullEditorAssemblyPath, assembly.Filename);
                    deleteFiles.Remove(path);
                    deleteFiles.Remove(MDBPath(path));
                    deleteFiles.Remove(PDBPath(path));
                }
            }

            foreach (var path in deleteFiles)
                DeleteFile(path);
        }

        public void CleanScriptAssemblies()
        {
            string fullEditorAssemblyPath = AssetPath.Combine(projectDirectory, GetCompileScriptsOutputDirectory());

            if (!Directory.Exists(fullEditorAssemblyPath))
                return;

            foreach (var path in Directory.GetFiles(fullEditorAssemblyPath))
                DeleteFile(path);
        }

        static void DeleteFile(string path, DeleteFileOptions fileOptions = DeleteFileOptions.LogError)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception)
            {
                if (fileOptions == DeleteFileOptions.LogError)
                    UnityEngine.Debug.LogErrorFormat("Could not delete file '{0}'\n", path);
            }
        }

        static bool MoveOrReplaceFile(string sourcePath, string destinationPath)
        {
            bool fileMoved = true;
            try
            {
                File.Move(sourcePath, destinationPath);
            }
            catch (IOException)
            {
                fileMoved = false;
            }
            if (!fileMoved)
            {
                fileMoved = true;
                var backupFile = destinationPath + ".bak";
                DeleteFile(backupFile, DeleteFileOptions.NoLogError); // Delete any previous backup files.

                try
                {
                    File.Replace(sourcePath, destinationPath, backupFile, true);
                }
                catch (IOException)
                {
                    fileMoved = false;
                }

                // Try to delete backup file. Does not need to exist
                // We will eventually delete the file in DeleteUnusedAssemblies.
                DeleteFile(backupFile, DeleteFileOptions.NoLogError);
            }
            return fileMoved;
        }

        static string PDBPath(string dllPath)
        {
            return dllPath.Replace(".dll", ".pdb");
        }

        static string MDBPath(string dllPath)
        {
            return dllPath + ".mdb";
        }

        static bool CopyAssembly(string sourcePath, string destinationPath)
        {
            if (!MoveOrReplaceFile(sourcePath, destinationPath))
                return false;

            string sourceMdb = MDBPath(sourcePath);
            string destinationMdb = MDBPath(destinationPath);

            if (File.Exists(sourceMdb))
                MoveOrReplaceFile(sourceMdb, destinationMdb);
            else if (File.Exists(destinationMdb))
                DeleteFile(destinationMdb);

            string sourcePdb = PDBPath(sourcePath);
            string destinationPdb = PDBPath(destinationPath);

            if (File.Exists(sourcePdb))
                MoveOrReplaceFile(sourcePdb, destinationPdb);
            else if (File.Exists(destinationPdb))
                DeleteFile(destinationPdb);

            return true;
        }

        public CustomScriptAssembly FindCustomScriptAssemblyFromAssemblyName(string assemblyName)
        {
            assemblyName = AssetPath.GetAssemblyNameWithoutExtension(assemblyName);

            if (customScriptAssemblies != null)
            {
                var result = customScriptAssemblies.FirstOrDefault(a => a.Name == assemblyName);
                if (result != null)
                    return result;
            }

            var exceptionMessage = "Cannot find CustomScriptAssembly with name '" + assemblyName + "'.";

            if (customScriptAssemblies == null)
            {
                exceptionMessage += " customScriptAssemblies is null.";
            }
            else
            {
                var assemblyNames = customScriptAssemblies.Select(a => a.Name).ToArray();
                var assemblyNamesString = string.Join(", ", assemblyNames);
                exceptionMessage += " Assembly names: " + assemblyNamesString;
            }

            throw new InvalidOperationException(exceptionMessage);
        }

        internal CustomScriptAssembly FindCustomScriptAssemblyFromScriptPath(string scriptPath)
        {
            var customTargetAssembly = EditorBuildRules.GetCustomTargetAssembly(scriptPath, projectDirectory, customTargetAssemblies);
            var customScriptAssembly = customTargetAssembly != null ? FindCustomScriptAssemblyFromAssemblyName(customTargetAssembly.Filename) : null;

            return customScriptAssembly;
        }

        internal CustomScriptAssembly FindCustomTargetAssemblyFromTargetAssembly(EditorBuildRules.TargetAssembly assembly)
        {
            var assemblyName = AssetPath.GetAssemblyNameWithoutExtension(assembly.Filename);
            return FindCustomScriptAssemblyFromAssemblyName(assemblyName);
        }

        public CompileStatus CompileScripts(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            var scriptAssemblySettings = CreateScriptAssemblySettings(platformGroup, platform, options);

            EditorBuildRules.TargetAssembly[] notCompiledTargetAssemblies = null;
            string[] notCompiledScripts = null;

            var result = CompileScripts(scriptAssemblySettings, EditorTempPath, options, ref notCompiledTargetAssemblies, ref notCompiledScripts);

            if (notCompiledTargetAssemblies != null)
            {
                foreach (var targetAssembly in notCompiledTargetAssemblies)
                {
                    var customScriptAssembly = customScriptAssemblies.Single(a => a.Name == AssetPath.GetAssemblyNameWithoutExtension(targetAssembly.Filename));

                    var filePath = customScriptAssembly.FilePath;

                    if (filePath.StartsWith(projectDirectory))
                        filePath = filePath.Substring(projectDirectory.Length);

                    UnityEngine.Debug.LogWarning(string.Format("Script assembly '{0}' has not been compiled. Folder containing assembly definition file '{1}' contains script files for different script languages. Folder must only contain script files for one script language.", targetAssembly.Filename, filePath));
                }
            }

            if (notCompiledScripts != null)
            {
                Array.Sort(notCompiledScripts);

                foreach (var script in notCompiledScripts)
                {
                    UnityEngine.Debug.LogWarning(string.Format("Script '{0}' will not be compiled because it exists outside the Assets folder and does not to belong to any assembly definition file.", script));
                }
            }

            return result;
        }

        private static EditorBuildRules.TargetAssembly[] GetPredefinedAssemblyReferences(EditorBuildRules.TargetAssembly[] targetAssemblies)
        {
            var targetAssembliesResult = (targetAssemblies ?? Enumerable.Empty<EditorBuildRules.TargetAssembly>())
                .Where(x => (x.OptionalUnityReferences & OptionalUnityReferences.TestAssemblies) == OptionalUnityReferences.None
                && (x.Flags & AssemblyFlags.ExplicitlyReferenced) == AssemblyFlags.None)
                .ToArray();
            return targetAssembliesResult;
        }

        internal CompileStatus CompileScripts(ScriptAssemblySettings scriptAssemblySettings, string tempBuildDirectory, EditorScriptCompilationOptions options, ref EditorBuildRules.TargetAssembly[] notCompiledTargetAssemblies, ref string[] notCompiledScripts)
        {
            DeleteUnusedAssemblies();

            if (!DoesProjectFolderHaveAnyDirtyScripts() &&
                !ArePrecompiledAssembliesDirty() &&
                runScriptUpdaterAssemblies.Count == 0)
                return CompileStatus.Idle;

            UpdateAllTargetAssemblyDefines(customTargetAssemblies, EditorBuildRules.GetPredefinedTargetAssemblies(), scriptAssemblySettings);

            var assemblies = new EditorBuildRules.CompilationAssemblies
            {
                UnityAssemblies = unityAssemblies,
                PrecompiledAssemblies = precompiledAssemblies,
                CustomTargetAssemblies = customTargetAssemblies,
                PredefinedAssembliesCustomTargetReferences = GetPredefinedAssemblyReferences(customTargetAssemblies),
                EditorAssemblyReferences = ModuleUtils.GetAdditionalReferencesForUserScripts(),
            };

            var allDirtyScripts = (areAllScriptsDirty || areAllPrecompiledAssembliesDirty) ? allScripts.ToArray() : dirtyScripts.ToArray();

            var args = new EditorBuildRules.GenerateChangedScriptAssembliesArgs
            {
                AllSourceFiles = allScripts,
                DirtySourceFiles = allDirtyScripts,
                DirtyTargetAssemblies = dirtyTargetAssemblies,
                DirtyPrecompiledAssemblies = dirtyPrecompiledAssemblies,
                ProjectDirectory = projectDirectory,
                Settings = scriptAssemblySettings,
                Assemblies = assemblies,
                RunUpdaterAssemblies = runScriptUpdaterAssemblies
            };

            var scriptAssemblies = EditorBuildRules.GenerateChangedScriptAssemblies(args);

            foreach (var customTargetAssembly in args.NoScriptsCustomTargetAssemblies)
            {
                var customScriptAssembly = FindCustomTargetAssemblyFromTargetAssembly(customTargetAssembly);
                UnityEngine.Debug.LogWarningFormat("Assembly for Assembly Definition File '{0}' will not be compiled, because it has no scripts associated with it.", customScriptAssembly.FilePath);
            }

            notCompiledTargetAssemblies = args.NotCompiledTargetAssemblies.ToArray();
            notCompiledScripts = args.NotCompiledScripts.ToArray();

            // If only the last script of an assembly is removed, then scriptAssemblies will
            // be empty and dirtyTargetAssemblies will be not empty.
            // Or if only an explicitly referenced precompiled assembly without any
            // references is marked as dirty, then scriptAssemblies will be empty and
            // dirtyPrecompiledAssemblies will not be dirty.
            // Then we should delete unused assemblies and clear compilationTask and
            // retun CompileStatus.CompilationComplete to indicate to native that compilation was
            // successful and that the assemblies should be reloaded.
            // If the last script of assembly was removed or precompiled assemblies are dirty
            // along with modifying other scripts, then scriptAssemblies will not be empty and
            // we will recompile scripts and then reload assemblies if compilation is successful.
            bool returnCompilationComplete = scriptAssemblies.Length == 0 &&
                (dirtyTargetAssemblies.Any() || dirtyPrecompiledAssemblies.Any());

            ClearDirtyScripts();

            if (returnCompilationComplete)
            {
                DeleteUnusedAssemblies();
                compilationTask = null;
                return CompileStatus.CompilationComplete;
            }

            if (!scriptAssemblies.Any())
                return CompileStatus.Idle;

            bool compiling = CompileScriptAssemblies(scriptAssemblies, scriptAssemblySettings, tempBuildDirectory, options, CompilationTaskOptions.StopOnFirstError, CompileScriptAssembliesOptions.none);

            return compiling ? CompileStatus.CompilationStarted : CompileStatus.Idle;
        }

        internal bool CompileCustomScriptAssemblies(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            var scriptAssemblySettings = CreateScriptAssemblySettings(platformGroup, platform, options);
            return CompileCustomScriptAssemblies(scriptAssemblySettings, EditorTempPath, options, platformGroup, platform);
        }

        internal bool CompileCustomScriptAssemblies(ScriptAssemblySettings scriptAssemblySettings, string tempBuildDirectory, EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            var scriptAssemblies = GetAllScriptAssembliesOfType(scriptAssemblySettings, EditorBuildRules.TargetAssemblyType.Custom);
            return CompileScriptAssemblies(scriptAssemblies, scriptAssemblySettings, tempBuildDirectory, options, CompilationTaskOptions.None, CompileScriptAssembliesOptions.skipSetupChecks);
        }

        internal bool CompileScriptAssemblies(ScriptAssembly[] scriptAssemblies,
            ScriptAssemblySettings scriptAssemblySettings,
            string tempBuildDirectory,
            EditorScriptCompilationOptions options,
            CompilationTaskOptions compilationTaskOptions,
            CompileScriptAssembliesOptions compileScriptAssembliesOptions)
        {
            StopAllCompilation();

            bool skipSetupChecks = (compileScriptAssembliesOptions & CompileScriptAssembliesOptions.skipSetupChecks) == CompileScriptAssembliesOptions.skipSetupChecks;

            // Skip setup checks when compiling custom script assemblies on startup,
            // as we only load the ones that been compiled and have all their references
            // fully resolved.
            if (!skipSetupChecks)
            {
                // Do no start compilation if there is an setup error.
                if (setupErrorFlags != CompilationSetupErrorFlags.none)
                    return false;

                CheckCyclicAssemblyReferences();
            }

            DeleteUnusedAssemblies();

            if (!Directory.Exists(scriptAssemblySettings.OutputDirectory))
                Directory.CreateDirectory(scriptAssemblySettings.OutputDirectory);

            if (!Directory.Exists(tempBuildDirectory))
                Directory.CreateDirectory(tempBuildDirectory);

            // Compile to tempBuildDirectory
            compilationTask = new CompilationTask(scriptAssemblies, tempBuildDirectory, options, compilationTaskOptions, UnityEngine.SystemInfo.processorCount);

            compilationTask.OnCompilationStarted += (assembly, phase) =>
            {
                var assemblyOutputPath = AssetPath.Combine(scriptAssemblySettings.OutputDirectory, assembly.Filename);
                Console.WriteLine("- Starting compile {0}", assemblyOutputPath);
                InvokeAssemblyCompilationStarted(assemblyOutputPath);
            };

            compilationTask.OnCompilationFinished += (assembly, messages) =>
            {
                var assemblyOutputPath = AssetPath.Combine(scriptAssemblySettings.OutputDirectory, assembly.Filename);
                Console.WriteLine("- Finished compile {0}", assemblyOutputPath);

                if (runScriptUpdaterAssemblies.Contains(assembly.Filename))
                    runScriptUpdaterAssemblies.Remove(assembly.Filename);

                if (messages.Any(m => m.type == CompilerMessageType.Error))
                {
                    AddUnitySpecificErrorMessages(assembly, messages);

                    InvokeAssemblyCompilationFinished(assemblyOutputPath, messages);
                    return;
                }

                var buildingForEditor = scriptAssemblySettings.BuildingForEditor;
                string enginePath = InternalEditorUtility.GetEngineCoreModuleAssemblyPath();

                string unetPath = UnityEditor.EditorApplication.applicationContentsPath + "/UnityExtensions/Unity/Networking/UnityEngine.Networking.dll";
                if (!Serialization.Weaver.WeaveUnetFromEditor(assembly, tempBuildDirectory, tempBuildDirectory, enginePath, unetPath, buildingForEditor))
                {
                    messages.Add(new CompilerMessage { message = "UNet Weaver failed", type = CompilerMessageType.Error, file = assembly.FullPath, line = -1, column = -1 });
                    StopAllCompilation();
                    InvokeAssemblyCompilationFinished(assemblyOutputPath, messages);
                    return;
                }

                // Copy from tempBuildDirectory to assembly output directory
                if (!CopyAssembly(AssetPath.Combine(tempBuildDirectory, assembly.Filename), assembly.FullPath))
                {
                    messages.Add(new CompilerMessage { message = string.Format("Copying assembly from '{0}' to '{1}' failed", AssetPath.Combine(tempBuildDirectory, assembly.Filename), assembly.FullPath), type = CompilerMessageType.Error, file = assembly.FullPath, line = -1, column = -1 });
                    StopCompilationTask();
                    InvokeAssemblyCompilationFinished(assemblyOutputPath, messages);
                    return;
                }

                InvokeAssemblyCompilationFinished(assemblyOutputPath, messages);
            };

            compilationTask.Poll();
            return true;
        }

        void AddUnitySpecificErrorMessages(ScriptAssembly assembly, List<CompilerMessage> messages)
        {
            var processors = new List<UnitySpecificCompilerMessageProcessor>()
            {
                new UnsafeErrorProcessor(assembly, this),
                new ModuleReferenceErrorProcessor()
            };

            if (!messages.Any(m => processors.Any(p => p.IsInterestedInMessage(m))))
                return;

            List<CompilerMessage> newMessages = new List<CompilerMessage>();

            foreach (var message in messages)
            {
                var newMessage = new CompilerMessage(message);
                foreach (var processor in processors)
                {
                    if (processor.IsInterestedInMessage(message))
                        processor.PostprocessMessage(ref newMessage);
                }
                newMessages.Add(newMessage);
            }

            messages.Clear();
            messages.AddRange(newMessages);
        }

        public void InvokeAssemblyCompilationStarted(string assemblyOutputPath)
        {
            if (assemblyCompilationStarted != null)
                assemblyCompilationStarted(assemblyOutputPath);
        }

        public void InvokeAssemblyCompilationFinished(string assemblyOutputPath, List<CompilerMessage> messages)
        {
            if (assemblyCompilationFinished != null)
            {
                var convertedMessages = ConvertCompilerMessages(messages);
                assemblyCompilationFinished(assemblyOutputPath, convertedMessages);
            }
        }

        public bool AreAllScriptsDirty()
        {
            return areAllScriptsDirty;
        }

        public bool ArePrecompiledAssembliesDirty()
        {
            return areAllPrecompiledAssembliesDirty || dirtyPrecompiledAssemblies.Count > 0;
        }

        public bool DoesProjectFolderHaveAnyDirtyScripts()
        {
            return (areAllScriptsDirty && allScripts.Count > 0) ||
                dirtyScripts.Count > 0 || dirtyTargetAssemblies.Count > 0;
        }

        public bool DoesProjectFolderHaveAnyScripts()
        {
            return allScripts != null && allScripts.Count > 0;
        }

        public bool DoesProjectHaveAnyCustomScriptAssemblies()
        {
            foreach (var script in allScripts)
            {
                var targetAssembly = EditorBuildRules.GetTargetAssembly(script, projectDirectory, customTargetAssemblies);

                if (targetAssembly.Type == EditorBuildRules.TargetAssemblyType.Custom)
                    return true;
            }

            return false;
        }

        ScriptAssemblySettings CreateScriptAssemblySettings(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, EditorScriptCompilationOptions options)
        {
            var predefinedAssembliesCompilerOptions = new ScriptCompilerOptions();

            if ((options & EditorScriptCompilationOptions.BuildingPredefinedAssembliesAllowUnsafeCode) == EditorScriptCompilationOptions.BuildingPredefinedAssembliesAllowUnsafeCode)
                predefinedAssembliesCompilerOptions.AllowUnsafeCode = true;

            var settings = new ScriptAssemblySettings
            {
                BuildTarget = buildTarget,
                BuildTargetGroup = buildTargetGroup,
                OutputDirectory = GetCompileScriptsOutputDirectory(),
                ApiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup),
                CompilationOptions = options,
                PredefinedAssembliesCompilerOptions = predefinedAssembliesCompilerOptions,
                OptionalUnityReferences = ToOptionalUnityReferences(options),
            };

            return settings;
        }

        ScriptAssemblySettings CreateEditorScriptAssemblySettings(EditorScriptCompilationOptions options)
        {
            return CreateScriptAssemblySettings(EditorUserBuildSettings.activeBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget, options);
        }

        public AssemblyCompilerMessages[] GetCompileMessages()
        {
            if (compilationTask == null)
                return null;

            var result = new AssemblyCompilerMessages[compilationTask.CompilerMessages.Count];

            int index = 0;
            foreach (var entry in compilationTask.CompilerMessages)
            {
                var assembly = entry.Key;
                var messages = entry.Value;

                result[index++] = new AssemblyCompilerMessages { assemblyFilename = assembly.Filename, messages = messages };
            }

            // Sort compiler messages by assemby filename to make the order deterministic.
            Array.Sort(result, (m1, m2) => String.Compare(m1.assemblyFilename, m2.assemblyFilename));

            return result;
        }

        public bool IsCompilationPending()
        {
            // If there were any errors in setting up the compilation, then return false.
            if (setupErrorFlags != CompilationSetupErrorFlags.none)
                return false;

            // If we have dirty scripts or script updater has marked assemblies for updated,
            // then compilation will trigger on next TickCompilationPipeline.
            return DoesProjectFolderHaveAnyDirtyScripts() ||
                ArePrecompiledAssembliesDirty() ||
                runScriptUpdaterAssemblies.Count() > 0 ||
                recompileAllScriptsOnNextTick;
        }

        public bool IsAnyAssemblyBuilderCompiling()
        {
            if (assemblyBuilders.Count > 0)
            {
                bool isCompiling = false;

                var removeAssemblyBuilders = new List<Compilation.AssemblyBuilder>();

                // Check status of compile tasks
                foreach (var assemblyBuilder in assemblyBuilders)
                {
                    var status = assemblyBuilder.status;

                    if (status == Compilation.AssemblyBuilderStatus.IsCompiling)
                        isCompiling = true;
                    else if (status == Compilation.AssemblyBuilderStatus.Finished)
                        removeAssemblyBuilders.Add(assemblyBuilder);
                }

                // Remove all compile tasks that finished compiling.
                if (removeAssemblyBuilders.Count > 0)
                    assemblyBuilders.RemoveAll(t => removeAssemblyBuilders.Contains(t));

                return isCompiling;
            }

            return false;
        }

        public bool IsCompiling()
        {
            // Native code expects IsCompiling to be true after marking scripts as dirty,
            // therefore return true if the compilation is pending
            return IsCompilationTaskCompiling() || IsCompilationPending() || IsAnyAssemblyBuilderCompiling();
        }

        public bool IsCompilationTaskCompiling()
        {
            return compilationTask != null && compilationTask.IsCompiling;
        }

        public void StopAllCompilation()
        {
            StopCompilationTask();
            compilationTask = null;
        }

        public void StopCompilationTask()
        {
            if (compilationTask == null)
                return;

            compilationTask.Stop();
        }

        internal static OptionalUnityReferences ToOptionalUnityReferences(EditorScriptCompilationOptions editorScriptCompilationOptions)
        {
            var optinalUnityReferences = OptionalUnityReferences.None;

            var buildingIncludingTestAssemblies = (editorScriptCompilationOptions & EditorScriptCompilationOptions.BuildingIncludingTestAssemblies) == EditorScriptCompilationOptions.BuildingIncludingTestAssemblies;
            if (buildingIncludingTestAssemblies)
            {
                optinalUnityReferences |= OptionalUnityReferences.TestAssemblies;
            }
            return optinalUnityReferences;
        }

        public CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            // Return CompileStatus.Compiling if any compile task is still compiling.
            // This ensures that the compile tasks finish compiling before any
            // scripts in the Assets folder are compiled and a domain reload
            // is triggered.
            if (IsAnyAssemblyBuilderCompiling())
                return CompileStatus.Compiling;

            if (recompileAllScriptsOnNextTick)
            {
                DirtyAllScripts();
                recompileAllScriptsOnNextTick = false;
            }

            // If we are not currently compiling and there are dirty scripts, start compilation.
            if (!IsCompilationTaskCompiling() && IsCompilationPending())
            {
                CompileStatus compileStatus = CompileScripts(options, platformGroup, platform);
                return compileStatus;
            }

            return PollCompilation();
        }

        public CompileStatus PollCompilation()
        {
            if (IsCompilationTaskCompiling())
            {
                if (compilationTask.Poll()) // Returns true when compilation finished.
                    return (compilationTask == null || compilationTask.CompileErrors) ? CompileStatus.CompilationFailed : CompileStatus.CompilationComplete;

                return CompileStatus.Compiling;
            }

            return CompileStatus.Idle;
        }

        public TargetAssemblyInfo[] GetTargetAssemblies()
        {
            EditorBuildRules.TargetAssembly[] predefindTargetAssemblies = EditorBuildRules.GetPredefinedTargetAssemblies();

            TargetAssemblyInfo[] targetAssemblyInfo = new TargetAssemblyInfo[predefindTargetAssemblies.Length + (customTargetAssemblies != null ? customTargetAssemblies.Count() : 0)];

            for (int i = 0; i < predefindTargetAssemblies.Length; ++i)
                targetAssemblyInfo[i] = ToTargetAssemblyInfo(predefindTargetAssemblies[i]);

            if (customTargetAssemblies != null)
                for (int i = 0; i < customTargetAssemblies.Count(); ++i)
                {
                    int j = predefindTargetAssemblies.Length + i;
                    targetAssemblyInfo[j] = ToTargetAssemblyInfo(customTargetAssemblies[i]);
                }

            return targetAssemblyInfo;
        }

        public TargetAssemblyInfo[] GetTargetAssembliesWithScripts(EditorScriptCompilationOptions options)
        {
            ScriptAssemblySettings settings = CreateEditorScriptAssemblySettings(EditorScriptCompilationOptions.BuildingForEditor | options);
            var targetAssemblies = EditorBuildRules.GetTargetAssembliesWithScripts(allScripts, projectDirectory, customTargetAssemblies, settings);

            var targetAssemblyInfos = new TargetAssemblyInfo[targetAssemblies.Length];

            for (int i = 0; i < targetAssemblies.Length; ++i)
                targetAssemblyInfos[i] = ToTargetAssemblyInfo(targetAssemblies[i]);

            return targetAssemblyInfos;
        }

        public ScriptAssembly[] GetAllScriptAssembliesForLanguage<T>(EditorScriptCompilationOptions additionalOptions) where T : SupportedLanguage
        {
            var assemblies = GetAllScriptAssemblies(EditorScriptCompilationOptions.BuildingForEditor).Where(a => a.Language.GetType() == typeof(T)).ToArray();
            return assemblies;
        }

        public ScriptAssembly GetScriptAssemblyForLanguage<T>(string assemblyNameOrPath, EditorScriptCompilationOptions additionalOptions) where T : SupportedLanguage
        {
            var assemblyName = AssetPath.GetAssemblyNameWithoutExtension(assemblyNameOrPath);
            var scriptAssemblies = GetAllScriptAssembliesForLanguage<T>(additionalOptions);
            return scriptAssemblies.SingleOrDefault(a => String.Compare(assemblyName, AssetPath.GetAssemblyNameWithoutExtension(a.Filename), StringComparison.OrdinalIgnoreCase) == 0);
        }

        public EditorBuildRules.TargetAssembly[] GetCustomTargetAssemblies()
        {
            return customTargetAssemblies;
        }

        public PrecompiledAssembly[] GetUnityAssemblies()
        {
            return unityAssemblies;
        }

        public TargetAssemblyInfo GetTargetAssembly(string scriptPath)
        {
            EditorBuildRules.TargetAssembly targetAssembly = EditorBuildRules.GetTargetAssembly(scriptPath, projectDirectory, customTargetAssemblies);

            TargetAssemblyInfo targetAssemblyInfo = ToTargetAssemblyInfo(targetAssembly);
            return targetAssemblyInfo;
        }

        public PrecompiledAssembly? GetPrecompiledAssemblyFromPath(string path)
        {
            foreach (var precompiledAssembly in precompiledAssemblies)
            {
                if (path.Equals(precompiledAssembly.Path, StringComparison.InvariantCultureIgnoreCase))
                    return precompiledAssembly;
            }

            return null;
        }

        public EditorBuildRules.TargetAssembly GetTargetAssemblyDetails(string scriptPath)
        {
            return EditorBuildRules.GetTargetAssembly(scriptPath, projectDirectory, customTargetAssemblies);
        }

        public ScriptAssembly[] GetAllEditorScriptAssemblies(EditorScriptCompilationOptions additionalOptions)
        {
            return GetAllScriptAssemblies(EditorScriptCompilationOptions.BuildingForEditor | EditorScriptCompilationOptions.BuildingIncludingTestAssemblies | additionalOptions);
        }

        public ScriptAssembly[] GetAllScriptAssemblies(EditorScriptCompilationOptions options)
        {
            return GetAllScriptAssemblies(options, unityAssemblies, precompiledAssemblies);
        }

        public ScriptAssembly[] GetAllScriptAssemblies(EditorScriptCompilationOptions options, PrecompiledAssembly[] unityAssembliesArg, PrecompiledAssembly[] precompiledAssembliesArg)
        {
            ScriptAssemblySettings settings = CreateEditorScriptAssemblySettings(options);

            UpdateAllTargetAssemblyDefines(customTargetAssemblies, EditorBuildRules.GetPredefinedTargetAssemblies(), settings);

            var assemblies = new EditorBuildRules.CompilationAssemblies
            {
                UnityAssemblies = unityAssembliesArg,
                PrecompiledAssemblies = precompiledAssembliesArg,
                CustomTargetAssemblies = customTargetAssemblies,
                PredefinedAssembliesCustomTargetReferences = GetPredefinedAssemblyReferences(customTargetAssemblies),
                EditorAssemblyReferences = ModuleUtils.GetAdditionalReferencesForUserScripts()
            };

            return EditorBuildRules.GetAllScriptAssemblies(allScripts, projectDirectory, settings, assemblies);
        }

        private static void UpdateAllTargetAssemblyDefines(EditorBuildRules.TargetAssembly[] customScriptAssemblies, EditorBuildRules.TargetAssembly[] predefinedTargetAssemblies, ScriptAssemblySettings settings)
        {
            var allTargetAssemblies = (customScriptAssemblies ?? new EditorBuildRules.TargetAssembly[0])
                .Concat(predefinedTargetAssemblies ?? new EditorBuildRules.TargetAssembly[0]);

            foreach (var targetAssembly in allTargetAssemblies)
            {
                SetTargetAssemblyDefines(targetAssembly, settings);
            }
        }

        private static void SetTargetAssemblyDefines(EditorBuildRules.TargetAssembly targetAssembly, ScriptAssemblySettings settings)
        {
            var editorOnlyTargetAssembly = (targetAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;
            ApiCompatibilityLevel apiCompatibilityLevel;

            if (editorOnlyTargetAssembly || (settings.BuildingForEditor && settings.ApiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6))
                apiCompatibilityLevel = (EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest) ? ApiCompatibilityLevel.NET_4_6 : ApiCompatibilityLevel.NET_2_0;
            else
                apiCompatibilityLevel = settings.ApiCompatibilityLevel;

            var defines = InternalEditorUtility.GetCompilationDefines(settings.CompilationOptions, settings.BuildTargetGroup, settings.BuildTarget, apiCompatibilityLevel).Concat(settings.ExtraGeneralDefines);
            targetAssembly.Defines = defines.ToArray();
        }

        ScriptAssembly[] GetAllScriptAssembliesOfType(ScriptAssemblySettings settings, EditorBuildRules.TargetAssemblyType type)
        {
            UpdateAllTargetAssemblyDefines(customTargetAssemblies, EditorBuildRules.GetPredefinedTargetAssemblies(), settings);

            var assemblies = new EditorBuildRules.CompilationAssemblies
            {
                UnityAssemblies = unityAssemblies,
                PrecompiledAssemblies = precompiledAssemblies,
                CustomTargetAssemblies = customTargetAssemblies,
                PredefinedAssembliesCustomTargetReferences = GetPredefinedAssemblyReferences(customTargetAssemblies),
                EditorAssemblyReferences = ModuleUtils.GetAdditionalReferencesForUserScripts(),
            };

            return EditorBuildRules.GetAllScriptAssemblies(allScripts, projectDirectory, settings, assemblies, type);
        }

        public MonoIsland[] GetAllMonoIslands(EditorScriptCompilationOptions additionalOptions)
        {
            return GetAllMonoIslands(unityAssemblies, precompiledAssemblies, EditorScriptCompilationOptions.BuildingForEditor | EditorScriptCompilationOptions.BuildingIncludingTestAssemblies | additionalOptions);
        }

        public MonoIsland[] GetAllMonoIslands(PrecompiledAssembly[] unityAssembliesArg, PrecompiledAssembly[] precompiledAssembliesArg, EditorScriptCompilationOptions options)
        {
            var scriptAssemblies = GetAllScriptAssemblies(options, unityAssembliesArg, precompiledAssembliesArg);
            var monoIslands = new MonoIsland[scriptAssemblies.Length];

            for (int i = 0; i < scriptAssemblies.Length; ++i)
                monoIslands[i] = scriptAssemblies[i].ToMonoIsland(EditorScriptCompilationOptions.BuildingForEditor, EditorTempPath);

            return monoIslands;
        }

        public bool IsRuntimeScriptAssembly(string assemblyNameOrPath)
        {
            var assemblyFilename = AssetPath.GetFileName(assemblyNameOrPath);

            if (!assemblyFilename.EndsWith(".dll"))
                assemblyFilename += ".dll";

            var predefinedAssemblyTargets = EditorBuildRules.GetPredefinedTargetAssemblies();

            if (predefinedAssemblyTargets.Any(a => ((a.Flags & AssemblyFlags.EditorOnly) != AssemblyFlags.EditorOnly) && a.Filename == assemblyFilename))
                return true;

            if (customTargetAssemblies != null && customTargetAssemblies.Any(a => ((a.Flags & AssemblyFlags.EditorOnly) != AssemblyFlags.EditorOnly) && a.Filename == assemblyFilename))
                return true;

            return false;
        }

        TargetAssemblyInfo ToTargetAssemblyInfo(EditorBuildRules.TargetAssembly targetAssembly)
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

        static EditorScriptCompilationOptions ToEditorScriptCompilationOptions(Compilation.AssemblyBuilderFlags flags)
        {
            EditorScriptCompilationOptions options = EditorScriptCompilationOptions.BuildingEmpty;

            if ((flags & Compilation.AssemblyBuilderFlags.DevelopmentBuild) == Compilation.AssemblyBuilderFlags.DevelopmentBuild)
                options |= EditorScriptCompilationOptions.BuildingDevelopmentBuild;

            if ((flags & Compilation.AssemblyBuilderFlags.EditorAssembly) == Compilation.AssemblyBuilderFlags.EditorAssembly)
                options |= EditorScriptCompilationOptions.BuildingForEditor;

            return options;
        }

        static AssemblyFlags ToAssemblyFlags(Compilation.AssemblyBuilderFlags assemblyBuilderFlags)
        {
            AssemblyFlags assemblyFlags = AssemblyFlags.None;

            if ((assemblyBuilderFlags & Compilation.AssemblyBuilderFlags.EditorAssembly) == Compilation.AssemblyBuilderFlags.EditorAssembly)
                assemblyFlags |= AssemblyFlags.EditorOnly;

            return assemblyFlags;
        }

        ScriptAssembly InitializeScriptAssemblyWithoutReferencesAndDefines(Compilation.AssemblyBuilder assemblyBuilder)
        {
            var scriptFiles = assemblyBuilder.scriptPaths.Select(p => AssetPath.Combine(projectDirectory, p)).ToArray();
            var assemblyPath = AssetPath.Combine(projectDirectory, assemblyBuilder.assemblyPath);

            var scriptAssembly = new ScriptAssembly();
            scriptAssembly.Flags = ToAssemblyFlags(assemblyBuilder.flags);
            scriptAssembly.BuildTarget = assemblyBuilder.buildTarget;
            scriptAssembly.ApiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(assemblyBuilder.buildTargetGroup);
            scriptAssembly.Language = ScriptCompilers.GetLanguageFromExtension(ScriptCompilers.GetExtensionOfSourceFile(assemblyBuilder.scriptPaths[0]));
            scriptAssembly.Files = scriptFiles;
            scriptAssembly.Filename = AssetPath.GetFileName(assemblyPath);
            scriptAssembly.OutputDirectory = AssetPath.GetDirectoryName(assemblyPath);
            scriptAssembly.CompilerOptions = assemblyBuilder.compilerOptions;
            scriptAssembly.ScriptAssemblyReferences = new ScriptAssembly[0];

            return scriptAssembly;
        }

        public ScriptAssembly CreateScriptAssembly(Compilation.AssemblyBuilder assemblyBuilder)
        {
            var scriptAssembly = InitializeScriptAssemblyWithoutReferencesAndDefines(assemblyBuilder);

            var options = ToEditorScriptCompilationOptions(assemblyBuilder.flags);

            var references = GetAssemblyBuilderDefaultReferences(scriptAssembly, options);

            if (assemblyBuilder.additionalReferences != null && assemblyBuilder.additionalReferences.Length > 0)
                references = references.Concat(assemblyBuilder.additionalReferences).ToArray();

            if (assemblyBuilder.excludeReferences != null && assemblyBuilder.excludeReferences.Length > 0)
                references = references.Where(r => !assemblyBuilder.excludeReferences.Contains(r)).ToArray();

            var defines = GetAssemblyBuilderDefaultDefines(assemblyBuilder);

            if (assemblyBuilder.additionalDefines != null)
                defines = defines.Concat(assemblyBuilder.additionalDefines).ToArray();

            scriptAssembly.References = references.ToArray();
            scriptAssembly.Defines = defines.ToArray();

            return scriptAssembly;
        }

        string[] GetAssemblyBuilderDefaultReferences(ScriptAssembly scriptAssembly, EditorScriptCompilationOptions options)
        {
            bool buildingForEditor = (scriptAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;

            var monolithicEngineAssemblyPath = InternalEditorUtility.GetMonolithicEngineAssemblyPath();
            var unityReferences = EditorBuildRules.GetUnityReferences(scriptAssembly, unityAssemblies, options, EditorBuildRules.UnityReferencesOptions.ExcludeModules);

            var customReferences = EditorBuildRules.GetCompiledCustomAssembliesReferences(scriptAssembly, customTargetAssemblies, GetCompileScriptsOutputDirectory());
            var precompiledReferences = EditorBuildRules.GetPrecompiledReferences(scriptAssembly, EditorBuildRules.TargetAssemblyType.Custom, options, EditorBuildRules.EditorCompatibility.CompatibleWithEditor, precompiledAssemblies);
            var additionalReferences = MonoLibraryHelpers.GetSystemLibraryReferences(scriptAssembly.ApiCompatibilityLevel, scriptAssembly.BuildTarget, scriptAssembly.Language, buildingForEditor, scriptAssembly);
            string[] editorReferences = buildingForEditor ? ModuleUtils.GetAdditionalReferencesForUserScripts() : new string[0];

            var references = new List<string>();
            references.Add(monolithicEngineAssemblyPath);
            references.AddRange(unityReferences.Concat(customReferences).Concat(precompiledReferences).Concat(editorReferences).Concat(additionalReferences));

            return references.ToArray();
        }

        public string[] GetAssemblyBuilderDefaultReferences(AssemblyBuilder assemblyBuilder)
        {
            var scriptAssembly = InitializeScriptAssemblyWithoutReferencesAndDefines(assemblyBuilder);
            var options = ToEditorScriptCompilationOptions(assemblyBuilder.flags);

            var references = GetAssemblyBuilderDefaultReferences(scriptAssembly, options);

            return references;
        }

        public string[] GetAssemblyBuilderDefaultDefines(AssemblyBuilder assemblyBuilder)
        {
            var options = ToEditorScriptCompilationOptions(assemblyBuilder.flags);
            var defines = InternalEditorUtility.GetCompilationDefines(options, assemblyBuilder.buildTargetGroup, assemblyBuilder.buildTarget);
            return defines;
        }

        public void AddAssemblyBuilder(UnityEditor.Compilation.AssemblyBuilder assemblyBuilder)
        {
            assemblyBuilders.Add(assemblyBuilder);
        }

        public static UnityEditor.Compilation.CompilerMessage[] ConvertCompilerMessages(List<CompilerMessage> messages)
        {
            var newMessages = new UnityEditor.Compilation.CompilerMessage[messages.Count];

            int index = 0;
            foreach (var message in messages)
            {
                var newMessage = new UnityEditor.Compilation.CompilerMessage();

                newMessage.message = message.message;
                newMessage.file = message.file;
                newMessage.line = message.line;
                newMessage.column = message.column;

                switch (message.type)
                {
                    case CompilerMessageType.Error:
                        newMessage.type = UnityEditor.Compilation.CompilerMessageType.Error;
                        break;

                    case CompilerMessageType.Warning:
                        newMessage.type = UnityEditor.Compilation.CompilerMessageType.Warning;
                        break;
                }

                newMessages[index++] = newMessage;
            }

            return newMessages;
        }
    }
}
