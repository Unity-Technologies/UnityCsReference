// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Modules;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
using UnityEditorInternal;
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
        }

        bool areAllScriptsDirty;
        string projectDirectory = string.Empty;
        string assemblySuffix = string.Empty;
        private HashSet<string> allScripts = new HashSet<string>();
        HashSet<string> dirtyScripts = new HashSet<string>();
        HashSet<string> runScriptUpdaterAssemblies = new HashSet<string>();
        PrecompiledAssembly[] precompiledAssemblies;
        CustomScriptAssembly[] customScriptAssemblies;
        CustomScriptAssembly[] packageCustomScriptAssemblies;
        EditorBuildRules.TargetAssembly[] customTargetAssemblies; // TargetAssemblies for customScriptAssemblies.
        PrecompiledAssembly[] unityAssemblies;
        CompilationTask compilationTask;
        string outputDirectory;
        List<Compilation.AssemblyBuilder> assemblyBuilders = new List<Compilation.AssemblyBuilder>();

        static readonly string EditorTempPath = "Temp";

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

        internal void SetAssemblySuffix(string assemblySuffix)
        {
            this.assemblySuffix = assemblySuffix;
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

        public void DirtyAllScripts()
        {
            areAllScriptsDirty = true;
        }

        public void DirtyScript(string path)
        {
            allScripts.Add(path);
            dirtyScripts.Add(path);
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

        public void SetAllPrecompiledAssemblies(PrecompiledAssembly[] precompiledAssemblies)
        {
            this.precompiledAssemblies = precompiledAssemblies;
        }

        public TargetAssemblyInfo[] GetAllCompiledAndResolvedCustomTargetAssemblies()
        {
            if (customTargetAssemblies == null)
                return new TargetAssemblyInfo[0];

            var customTargetAssemblyCompiledPaths = new Dictionary<EditorBuildRules.TargetAssembly, string>();

            foreach (var assembly in customTargetAssemblies)
            {
                var path = assembly.FullPath(outputDirectory, assemblySuffix);

                // Collect all assemblies that have been compiled (exist on file system)
                if (File.Exists(path))
                    customTargetAssemblyCompiledPaths.Add(assembly, path);
            }

            bool removed;

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
                            if (!customTargetAssemblyCompiledPaths.ContainsKey(reference))
                            {
                                customTargetAssemblyCompiledPaths.Remove(assembly);
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

            return targetAssemblies;
        }

        static CustomScriptAssembly LoadCustomScriptAssemblyFromJson(string path)
        {
            var json = File.ReadAllText(path);
            var customScriptAssemblyData = CustomScriptAssemblyData.FromJson(json);
            return CustomScriptAssembly.FromCustomScriptAssemblyData(path, customScriptAssemblyData);
        }

        static void CheckCyclicAssemblyReferencesDFS(EditorBuildRules.TargetAssembly visitAssembly, HashSet<EditorBuildRules.TargetAssembly> visited,
            IDictionary<string, EditorBuildRules.TargetAssembly> filenameToTargetAssembly)
        {
            if (visited.Contains(visitAssembly))
                throw new Exception(string.Format("Cyclic assembly references detected. Assemblies: {0}",
                        string.Join(", ", visited.Select(a => string.Format("'{0}'", AssetPath.GetFileNameWithoutExtension(a.Filename))).ToArray())));

            visited.Add(visitAssembly);

            foreach (var reference in visitAssembly.References.Select(a => a.Filename))
            {
                EditorBuildRules.TargetAssembly referenceAssembly;
                if (!filenameToTargetAssembly.TryGetValue(reference, out referenceAssembly))
                {
                    throw new Exception(string.Format("Reference to non-existent assembly. Assembly {0} has a reference to {1}", AssetPath.GetFileNameWithoutExtension(visitAssembly.Filename), reference));
                }
                CheckCyclicAssemblyReferencesDFS(referenceAssembly, visited, filenameToTargetAssembly);
            }

            visited.Remove(visitAssembly);
        }

        static void CheckCyclicAssemblyReferences(EditorBuildRules.TargetAssembly[] customTargetAssemblies)
        {
            if (customTargetAssemblies == null || customTargetAssemblies.Length < 2)
                return;

            var filenameToCustomScriptAssembly = new Dictionary<string, EditorBuildRules.TargetAssembly>();

            foreach (var customTargetAssembly in customTargetAssemblies)
                filenameToCustomScriptAssembly[customTargetAssembly.Filename] = customTargetAssembly;

            var visited = new HashSet<EditorBuildRules.TargetAssembly>();

            foreach (var assembly in customTargetAssemblies)
                CheckCyclicAssemblyReferencesDFS(assembly, visited, filenameToCustomScriptAssembly);
        }

        void CheckCyclickAssemblyReferences()
        {
            try
            {
                CheckCyclicAssemblyReferences(customTargetAssemblies);
            }
            catch (Exception e)
            {
                customTargetAssemblies = null;
                throw e;
            }
        }

        void UpdateCustomTargetAssemblies()
        {
            var allCustomScriptAssemblies = new List<CustomScriptAssembly>();

            if (customScriptAssemblies != null)
                allCustomScriptAssemblies.AddRange(customScriptAssemblies);

            if (packageCustomScriptAssemblies != null)
            {
                if (customScriptAssemblies == null)
                {
                    // There are no other custom script assemblies, add default customs script assemblies for all packages.
                    allCustomScriptAssemblies.AddRange(packageCustomScriptAssemblies.Select(a => CustomScriptAssembly.Create(a.Name, a.FilePath)));
                }
                else
                {
                    foreach (var packageAssembly in packageCustomScriptAssemblies)
                    {
                        var pathPrefix = packageAssembly.PathPrefix.ToLower();

                        // We have found an assembly.json in the package directory, do not
                        // add a default custom script assembly for the package.
                        if (customScriptAssemblies.Any(a => a.PathPrefix.ToLower().StartsWith(pathPrefix)))
                            continue;

                        allCustomScriptAssemblies.Add(CustomScriptAssembly.Create(packageAssembly.Name, packageAssembly.FilePath));
                    }
                }
            }

            customTargetAssemblies = EditorBuildRules.CreateTargetAssemblies(allCustomScriptAssemblies);
        }

        public void SetAllCustomScriptAssemblyJsons(string[] paths)
        {
            var assemblies = new List<CustomScriptAssembly>();

            foreach (var path in paths)
            {
                try
                {
                    var fullPath = AssetPath.IsPathRooted(path) ? AssetPath.GetFullPath(path) : AssetPath.Combine(projectDirectory, path);

                    var loadedCustomScriptAssembly = LoadCustomScriptAssemblyFromJson(fullPath);

                    if (assemblies.Any(a => string.Equals(a.Name, loadedCustomScriptAssembly.Name, System.StringComparison.OrdinalIgnoreCase)))
                        throw new Exception(string.Format("Assembly with name '{0}' is already defined ({1})",
                                loadedCustomScriptAssembly.Name,
                                loadedCustomScriptAssembly.FilePath));

                    if (assemblies.Any(a => a.PathPrefix == loadedCustomScriptAssembly.PathPrefix))
                        throw new Exception(string.Format("Folder '{0}' contains multiple assembly.json files", loadedCustomScriptAssembly.PathPrefix));

                    if (loadedCustomScriptAssembly.References == null)
                        loadedCustomScriptAssembly.References = new string[0];
                    if (loadedCustomScriptAssembly.References.Length != loadedCustomScriptAssembly.References.Distinct().Count())
                        throw new Exception(string.Format("Duplicate assembly references in {0}", loadedCustomScriptAssembly.FilePath));

                    assemblies.Add(loadedCustomScriptAssembly);
                }
                catch (System.Exception e)
                {
                    throw new System.Exception(e.Message + " - '" + path + "'");
                }
            }

            customScriptAssemblies = assemblies.ToArray();

            UpdateCustomTargetAssemblies();
        }

        public void SetAllPackageAssemblies(PackageAssembly[] packageAssemblies)
        {
            this.packageCustomScriptAssemblies = packageAssemblies.Select(a => CustomScriptAssembly.Create(a.Name, AssetPath.GetFullPath(a.DirectoryPath))).ToArray();
            UpdateCustomTargetAssemblies();
        }

        // Delete all .dll's that aren't used anymore
        public void DeleteUnusedAssemblies()
        {
            string fullEditorAssemblyPath = AssetPath.Combine(AssetPath.GetDirectoryName(UnityEngine.Application.dataPath), GetCompileScriptsOutputDirectory());

            if (!Directory.Exists(fullEditorAssemblyPath))
                return;

            var deleteFiles = Directory.GetFiles(fullEditorAssemblyPath).Select(f => AssetPath.ReplaceSeparators(f)).ToList();
            string timestampPath = GetAssemblyTimestampPath(GetCompileScriptsOutputDirectory());
            deleteFiles.Remove(AssetPath.Combine(AssetPath.GetDirectoryName(UnityEngine.Application.dataPath), timestampPath));

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
            {
                File.Delete(path);
            }
        }

        public void CleanScriptAssemblies()
        {
            string fullEditorAssemblyPath = AssetPath.Combine(AssetPath.GetDirectoryName(UnityEngine.Application.dataPath), GetCompileScriptsOutputDirectory());

            if (!Directory.Exists(fullEditorAssemblyPath))
                return;

            foreach (var path in Directory.GetFiles(fullEditorAssemblyPath))
                File.Delete(path);
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
                try
                {
                    File.Replace(sourcePath, destinationPath, null);
                }
                catch (IOException)
                {
                    fileMoved = false;
                }
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
                File.Delete(destinationMdb);

            string sourcePdb = PDBPath(sourcePath);
            string destinationPdb = PDBPath(destinationPath);

            if (File.Exists(sourcePdb))
                MoveOrReplaceFile(sourcePdb, destinationPdb);
            else if (File.Exists(destinationPdb))
                File.Delete(destinationPdb);

            return true;
        }

        internal CustomScriptAssembly FindCustomScriptAssembly(string scriptPath)
        {
            var customTargetAssembly = EditorBuildRules.GetCustomTargetAssembly(scriptPath, projectDirectory, customTargetAssemblies);
            var customScriptAssembly = customScriptAssemblies.Single(a => a.Name == AssetPath.GetFileNameWithoutExtension(customTargetAssembly.Filename));

            return customScriptAssembly;
        }

        public bool CompileScripts(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            var scriptAssemblySettings = CreateScriptAssemblySettings(platformGroup, platform, options);

            EditorBuildRules.TargetAssembly[] notCompiledTargetAssemblies = null;
            bool result = CompileScripts(scriptAssemblySettings, EditorTempPath, options, ref notCompiledTargetAssemblies);

            if (notCompiledTargetAssemblies != null)
                foreach (var targetAssembly in notCompiledTargetAssemblies)
                {
                    var customScriptAssembly = customScriptAssemblies.Single(a => a.Name == AssetPath.GetFileNameWithoutExtension(targetAssembly.Filename));

                    var filePath = customScriptAssembly.FilePath;

                    if (filePath.StartsWith(projectDirectory))
                        filePath = filePath.Substring(projectDirectory.Length);

                    UnityEngine.Debug.LogWarning(string.Format("Script assembly '{0}' has not been compiled. Folder containing assembly definition file '{1}' contains script files for different script languages. Folder must only contain script files for one script language.", targetAssembly.Filename, filePath));
                }

            return result;
        }

        internal bool CompileScripts(ScriptAssemblySettings scriptAssemblySettings, string tempBuildDirectory, EditorScriptCompilationOptions options, ref EditorBuildRules.TargetAssembly[] notCompiledTargetAssemblies)
        {
            CheckCyclickAssemblyReferences();

            DeleteUnusedAssemblies();

            StopAllCompilation();

            if (!Directory.Exists(scriptAssemblySettings.OutputDirectory))
                Directory.CreateDirectory(scriptAssemblySettings.OutputDirectory);

            if (!Directory.Exists(tempBuildDirectory))
                Directory.CreateDirectory(tempBuildDirectory);

            IEnumerable<string> allDirtyScripts = areAllScriptsDirty ? allScripts.ToArray() : dirtyScripts.ToArray();

            areAllScriptsDirty = false;
            dirtyScripts.Clear();

            if (!allDirtyScripts.Any() && runScriptUpdaterAssemblies.Count == 0)
                return false;

            var assemblies = new EditorBuildRules.CompilationAssemblies
            {
                UnityAssemblies = unityAssemblies,
                PrecompiledAssemblies = precompiledAssemblies,
                CustomTargetAssemblies = customTargetAssemblies,
                EditorAssemblyReferences = ModuleUtils.GetAdditionalReferencesForUserScripts()
            };

            var args = new EditorBuildRules.GenerateChangedScriptAssembliesArgs
            {
                AllSourceFiles = allScripts,
                DirtySourceFiles = allDirtyScripts,
                ProjectDirectory = projectDirectory,
                Settings = scriptAssemblySettings,
                Assemblies = assemblies,
                RunUpdaterAssemblies = runScriptUpdaterAssemblies
            };

            var scriptAssemblies = EditorBuildRules.GenerateChangedScriptAssemblies(args);

            notCompiledTargetAssemblies = args.NotCompiledTargetAssemblies.ToArray();

            if (!scriptAssemblies.Any())
                return false;

            // Compile to tempBuildDirectory
            compilationTask = new CompilationTask(scriptAssemblies, tempBuildDirectory, options, UnityEngine.SystemInfo.processorCount);

            compilationTask.OnCompilationStarted += (assembly, phase) =>
                {
                    Console.WriteLine("- Starting compile {0}", AssetPath.Combine(scriptAssemblySettings.OutputDirectory, assembly.Filename));
                };

            compilationTask.OnCompilationFinished += (assembly, messages) =>
                {
                    Console.WriteLine("- Finished compile {0}", AssetPath.Combine(scriptAssemblySettings.OutputDirectory, assembly.Filename));

                    if (runScriptUpdaterAssemblies.Contains(assembly.Filename))
                        runScriptUpdaterAssemblies.Remove(assembly.Filename);

                    if (messages.Any(m => m.type == CompilerMessageType.Error))
                        return;

                    var buildingForEditor = scriptAssemblySettings.BuildingForEditor;
                    string enginePath = InternalEditorUtility.GetEngineCoreModuleAssemblyPath();
                    // When using non-modular assemblies, the types UNETWeaver cares about are in the monolithic UnityEngine.dll
                    if (!buildingForEditor && !BuildPipeline.IsFeatureSupported("ENABLE_MODULAR_UNITYENGINE_ASSEMBLIES", scriptAssemblySettings.BuildTarget))
                        enginePath = UnityEditor.EditorApplication.applicationContentsPath + "/Managed/UnityEngine.dll";

                    string unetPath = UnityEditor.EditorApplication.applicationContentsPath + "/UnityExtensions/Unity/Networking/UnityEngine.Networking.dll";
                    if (!Serialization.Weaver.WeaveUnetFromEditor(assembly, tempBuildDirectory, tempBuildDirectory, enginePath, unetPath, buildingForEditor))
                    {
                        messages.Add(new CompilerMessage { message = "UNet Weaver failed", type = CompilerMessageType.Error, file = assembly.FullPath, line = -1, column = -1 });
                        StopAllCompilation();
                        return;
                    }

                    // Copy from tempBuildDirectory to assembly output directory
                    if (!CopyAssembly(AssetPath.Combine(tempBuildDirectory, assembly.Filename), assembly.FullPath))
                    {
                        messages.Add(new CompilerMessage { message = string.Format("Copying assembly from directory {0} to {1} failed", tempBuildDirectory, assembly.OutputDirectory), type = CompilerMessageType.Error, file = assembly.FullPath, line = -1, column = -1 });
                        StopAllCompilation();
                        return;
                    }
                };

            compilationTask.Poll();
            return true;
        }

        public bool DoesProjectFolderHaveAnyDirtyScripts()
        {
            return (areAllScriptsDirty && allScripts.Count > 0) || dirtyScripts.Count > 0;
        }

        public bool DoesProjectFolderHaveAnyScripts()
        {
            return allScripts != null && allScripts.Count > 0;
        }

        ScriptAssemblySettings CreateScriptAssemblySettings(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, EditorScriptCompilationOptions options)
        {
            var defines = InternalEditorUtility.GetCompilationDefines(options, buildTargetGroup, buildTarget);

            var settings = new ScriptAssemblySettings
            {
                BuildTarget = buildTarget,
                BuildTargetGroup = buildTargetGroup,
                OutputDirectory = GetCompileScriptsOutputDirectory(),
                Defines = defines,
                ApiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup),
                CompilationOptions = options,
                FilenameSuffix = assemblySuffix
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
            // If we have dirty scripts or script updater has marked assemblies for updated,
            // then compilation will trigger on next TickCompilationPipeline.
            return DoesProjectFolderHaveAnyDirtyScripts() || runScriptUpdaterAssemblies.Count() > 0;
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
            if (compilationTask != null)
            {
                compilationTask.Stop();
                compilationTask = null;
            }
        }

        public CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            // Return CompileStatus.Compiling if any compile task is still compiling.
            // This ensures that the compile tasks finish compiling before any
            // scripts in the Assets folder are compiled and a domain reload
            // is triggered.
            if (IsAnyAssemblyBuilderCompiling())
                return CompileStatus.Compiling;

            // If we are not currently compiling and there are dirty scripts, start compilation.
            if (!IsCompilationTaskCompiling() && IsCompilationPending())
            {
                if (CompileScripts(options, platformGroup, platform))
                    return CompileStatus.CompilationStarted;
            }

            if (IsCompilationTaskCompiling())
            {
                if (compilationTask.Poll()) // Returns true when compilation finished.
                    return (compilationTask == null || compilationTask.CompileErrors) ? CompileStatus.CompilationFailed : CompileStatus.CompilationComplete;

                return CompileStatus.Compiling;
            }
            return CompileStatus.Idle;
        }

        string AssemblyFilenameWithSuffix(string assemblyFilename)
        {
            if (!string.IsNullOrEmpty(assemblySuffix))
            {
                var basename = AssetPath.GetFileNameWithoutExtension(assemblyFilename);
                var extension = AssetPath.GetExtension(assemblyFilename);
                return string.Concat(basename, assemblySuffix, extension);
            }

            return assemblyFilename;
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

        public ScriptAssembly[] GetAllScriptAssembliesForLanguage<T>() where T : SupportedLanguage
        {
            var assemblies = GetAllScriptAssemblies(EditorScriptCompilationOptions.BuildingForEditor).Where(a => a.Language.GetType() == typeof(T)).ToArray();
            return assemblies;
        }

        public ScriptAssembly GetScriptAssemblyForLanguage<T>(string assemblyNameOrPath) where T : SupportedLanguage
        {
            var assemblyName = AssetPath.GetFileNameWithoutExtension(assemblyNameOrPath);
            var scriptAssemblies = GetAllScriptAssembliesForLanguage<T>();
            return scriptAssemblies.SingleOrDefault(a => String.Compare(assemblyName, AssetPath.GetFileNameWithoutExtension(a.Filename), StringComparison.OrdinalIgnoreCase) == 0);
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

        public EditorBuildRules.TargetAssembly GetTargetAssemblyDetails(string scriptPath)
        {
            return EditorBuildRules.GetTargetAssembly(scriptPath, projectDirectory, customTargetAssemblies);
        }

        ScriptAssembly[] GetAllScriptAssemblies(EditorScriptCompilationOptions options)
        {
            return GetAllScriptAssemblies(options, unityAssemblies, precompiledAssemblies);
        }

        ScriptAssembly[] GetAllScriptAssemblies(EditorScriptCompilationOptions options, PrecompiledAssembly[] unityAssembliesArg, PrecompiledAssembly[] precompiledAssembliesArg)
        {
            var assemblies = new EditorBuildRules.CompilationAssemblies
            {
                UnityAssemblies = unityAssembliesArg,
                PrecompiledAssemblies = precompiledAssembliesArg,
                CustomTargetAssemblies = customTargetAssemblies,
                EditorAssemblyReferences = ModuleUtils.GetAdditionalReferencesForUserScripts()
            };

            var settings = CreateEditorScriptAssemblySettings(options);
            return EditorBuildRules.GetAllScriptAssemblies(allScripts, projectDirectory, settings, assemblies);
        }

        public MonoIsland[] GetAllMonoIslands()
        {
            return GetAllMonoIslands(unityAssemblies, precompiledAssemblies, EditorScriptCompilationOptions.BuildingForEditor);
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

            targetAssemblyInfo.Name = AssemblyFilenameWithSuffix(targetAssembly.Filename);
            targetAssemblyInfo.Flags = targetAssembly.Flags;

            return targetAssemblyInfo;
        }

        public ScriptAssembly CreateScriptAssembly(Compilation.AssemblyBuilder assemblyBuilder)
        {
            EditorScriptCompilationOptions options = EditorScriptCompilationOptions.BuildingEmpty;
            AssemblyFlags assemblyFlags = AssemblyFlags.None;

            bool buildingForEditor = false;

            if ((assemblyBuilder.flags & Compilation.AssemblyBuilderFlags.DevelopmentBuild) == Compilation.AssemblyBuilderFlags.DevelopmentBuild)
                options |= EditorScriptCompilationOptions.BuildingDevelopmentBuild;

            if ((assemblyBuilder.flags & Compilation.AssemblyBuilderFlags.EditorAssembly) == Compilation.AssemblyBuilderFlags.EditorAssembly)
            {
                options |= EditorScriptCompilationOptions.BuildingForEditor;
                assemblyFlags |= AssemblyFlags.EditorOnly;
                buildingForEditor = true;
            }

            var scriptFiles = assemblyBuilder.scriptPaths.Select(p => AssetPath.Combine(projectDirectory, p)).ToArray();
            var assemblyPath = AssetPath.Combine(projectDirectory, assemblyBuilder.assemblyPath);
            var defines = InternalEditorUtility.GetCompilationDefines(options, assemblyBuilder.buildTargetGroup, assemblyBuilder.buildTarget);

            if (assemblyBuilder.additionalDefines != null)
                defines = defines.Concat(assemblyBuilder.additionalDefines).ToArray();

            var scriptAssembly = new ScriptAssembly();
            scriptAssembly.Flags = assemblyFlags;
            scriptAssembly.BuildTarget = assemblyBuilder.buildTarget;
            scriptAssembly.ApiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(assemblyBuilder.buildTargetGroup);
            scriptAssembly.Language = ScriptCompilers.GetLanguageFromExtension(ScriptCompilers.GetExtensionOfSourceFile(assemblyBuilder.scriptPaths[0]));
            scriptAssembly.Files = scriptFiles;
            scriptAssembly.Filename = AssetPath.GetFileName(assemblyPath);
            scriptAssembly.OutputDirectory = AssetPath.GetDirectoryName(assemblyPath);
            scriptAssembly.Defines = defines;
            scriptAssembly.ScriptAssemblyReferences = new ScriptAssembly[0];

            var unityReferences = EditorBuildRules.GetUnityReferences(scriptAssembly, unityAssemblies, options);
            var customReferences = EditorBuildRules.GetCompiledCustomAssembliesReferences(scriptAssembly, customTargetAssemblies, GetCompileScriptsOutputDirectory(), assemblySuffix);
            var precompiledReferences = EditorBuildRules.GetPrecompiledReferences(scriptAssembly, precompiledAssemblies);
            string[] editorReferences = buildingForEditor ? ModuleUtils.GetAdditionalReferencesForUserScripts() : new string[0];

            var references = unityReferences.Concat(customReferences).Concat(precompiledReferences).Concat(editorReferences);

            if (assemblyBuilder.additionalReferences != null)
                references = references.Concat(assemblyBuilder.additionalReferences);

            scriptAssembly.References = references.ToArray();

            return scriptAssembly;
        }

        public void AddAssemblyBuilder(UnityEditor.Compilation.AssemblyBuilder assemblyBuilder)
        {
            assemblyBuilders.Add(assemblyBuilder);
        }
    }
}
