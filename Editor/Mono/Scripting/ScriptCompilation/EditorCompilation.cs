// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Modules;
using UnityEditor.Scripting.Compilers;
using UnityEditorInternal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using System;

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

        bool areAllScriptsDirty;
        string projectDirectory = string.Empty;
        string assemblySuffix = string.Empty;
        HashSet<string> allScripts = null;
        HashSet<string> dirtyScripts = new HashSet<string>();
        HashSet<string> runScriptUpdaterAssemblies = new HashSet<string>();
        PrecompiledAssembly[] precompiledAssemblies;
        CustomScriptAssembly[] customScriptAssemblies;
        EditorBuildRules.TargetAssembly[] customTargetAssemblies; // TargetAssemblies for customScriptAssemblies.
        PrecompiledAssembly[] unityAssemblies;
        CompilationTask compilationTask;
        string outputDirectory;

        static readonly string EditorTempPath = "Temp";

        static EditorCompilation()
        {
        }

        internal string GetAssemblyTimestampPath(string editorAssemblyPath)
        {
            return Path.Combine(editorAssemblyPath, "BuiltinAssemblies.stamp");
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

        static CustomScriptAssembly LoadCustomScriptAssemblyFromJson(string path)
        {
            var json = File.ReadAllText(path);
            var customScriptAssemblyData = CustomScriptAssemblyData.FromJson(json);
            return CustomScriptAssembly.FromCustomScriptAssemblyData(path, customScriptAssemblyData);
        }

        static void CheckCyclicAssemblyReferencesDFS(CustomScriptAssembly visitAssembly, HashSet<CustomScriptAssembly> visited,
            IDictionary<string, CustomScriptAssembly> nameToCustomScriptAssembly)
        {
            if (visited.Contains(visitAssembly))
                throw new Exception(string.Format("Cyclic assembly references detected. Assemblies: {0}",
                        string.Join(", ", visited.Select(a => string.Format("'{0}'", a.Name)).ToArray())));

            visited.Add(visitAssembly);

            foreach (var reference in visitAssembly.References)
            {
                CustomScriptAssembly referenceAssembly;
                if (!nameToCustomScriptAssembly.TryGetValue(reference, out referenceAssembly))
                {
                    throw new Exception(string.Format("Reference to non-existent assembly. Assembly {0} has a reference to {1}", visitAssembly.Name, reference));
                }
                CheckCyclicAssemblyReferencesDFS(referenceAssembly, visited, nameToCustomScriptAssembly);
            }

            visited.Remove(visitAssembly);
        }

        static void CheckCyclicAssemblyReferences(CustomScriptAssembly[] customScriptAssemblies)
        {
            if (customScriptAssemblies == null || customScriptAssemblies.Length < 2)
                return;

            var nameToCustomScriptAssembly = new Dictionary<string, CustomScriptAssembly>();

            foreach (var customScriptAssembly in customScriptAssemblies)
                nameToCustomScriptAssembly[customScriptAssembly.Name] = customScriptAssembly;

            var visited = new HashSet<CustomScriptAssembly>();

            foreach (var assembly in customScriptAssemblies)
                CheckCyclicAssemblyReferencesDFS(assembly, visited, nameToCustomScriptAssembly);
        }

        public void SetAllCustomScriptAssemblyJsons(string[] paths)
        {
            var assemblies = new List<CustomScriptAssembly>();

            foreach (var path in paths)
            {
                try
                {
                    var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(projectDirectory, path);

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

            try
            {
                CheckCyclicAssemblyReferences(customScriptAssemblies);
            }
            catch (Exception e)
            {
                customScriptAssemblies = null;
                customTargetAssemblies = null;
                throw e;
            }

            customTargetAssemblies = EditorBuildRules.CreateTargetAssemblies(customScriptAssemblies);
        }

        // Delete all .dll's that aren't used anymore
        public void DeleteUnusedAssemblies()
        {
            string fullEditorAssemblyPath = Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath), GetCompileScriptsOutputDirectory());

            if (!Directory.Exists(fullEditorAssemblyPath))
                return;

            var deleteFiles = new List<string>(Directory.GetFiles(fullEditorAssemblyPath));
            string timestampPath = GetAssemblyTimestampPath(GetCompileScriptsOutputDirectory());
            deleteFiles.Remove(Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath), timestampPath));

            var scriptAssemblies = GetAllScriptAssemblies(EditorScriptCompilationOptions.BuildingForEditor);

            foreach (var assembly in scriptAssemblies)
            {
                if (assembly.Files.Length > 0)
                {
                    string path = Path.Combine(fullEditorAssemblyPath, assembly.Filename);
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
            string fullEditorAssemblyPath = Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath), GetCompileScriptsOutputDirectory());

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

        static void CopyAssembly(string sourcePath, string destinationPath)
        {
            if (MoveOrReplaceFile(sourcePath, destinationPath))
            {
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
            }
        }

        internal CustomScriptAssembly FindCustomScriptAssembly(string scriptPath)
        {
            var customTargetAssembly = EditorBuildRules.GetCustomTargetAssembly(scriptPath, projectDirectory, customTargetAssemblies);
            var customScriptAssembly = customScriptAssemblies.Single(a => a.Name == Path.GetFileNameWithoutExtension(customTargetAssembly.Filename));

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
                    var customScriptAssembly = customScriptAssemblies.Single(a => a.Name == Path.GetFileNameWithoutExtension(targetAssembly.Filename));

                    var filePath = customScriptAssembly.FilePath;

                    if (filePath.StartsWith(projectDirectory))
                        filePath = filePath.Substring(projectDirectory.Length);

                    UnityEngine.Debug.LogWarning(string.Format("Script assembly '{0}' has not been compiled. Folder containing assembly definition file '{1}' contains script files for different script languages. Folder must only contain script files for one script language.", targetAssembly.Filename, filePath));
                }

            return result;
        }

        internal bool CompileScripts(ScriptAssemblySettings scriptAssemblySettings, string tempBuildDirectory, EditorScriptCompilationOptions options, ref EditorBuildRules.TargetAssembly[] notCompiledTargetAssemblies)
        {
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
                    Console.WriteLine("- Starting compile {0}", Path.Combine(scriptAssemblySettings.OutputDirectory, assembly.Filename));
                };

            compilationTask.OnCompilationFinished += (assembly, messages) =>
                {
                    Console.WriteLine("- Finished compile {0}", Path.Combine(scriptAssemblySettings.OutputDirectory, assembly.Filename));

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
                    CopyAssembly(Path.Combine(tempBuildDirectory, assembly.Filename), assembly.FullPath);
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

        public bool IsCompiling()
        {
            // Native code expects IsCompiling to be true after marking scripts as dirty,
            // therefore return true if the compilation is pending
            return IsCompilationTaskCompiling() || IsCompilationPending();
        }

        bool IsCompilationTaskCompiling()
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

        public CompileStatus TickCompilationPipeline()
        {
            // If we are not currently compiling and there are dirty scripts, start compilation.
            if (!IsCompilationTaskCompiling() && IsCompilationPending())
            {
                if (CompileScripts(EditorScriptCompilationOptions.BuildingForEditor, EditorUserBuildSettings.activeBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget))
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
                var basename = Path.GetFileNameWithoutExtension(assemblyFilename);
                var extension = Path.GetExtension(assemblyFilename);
                return string.Concat(basename, assemblySuffix, extension);
            }

            return assemblyFilename;
        }

        public TargetAssemblyInfo[] GetTargetAssemblies()
        {
            EditorBuildRules.TargetAssembly[] predefindTargetAssemblies = EditorBuildRules.GetPredefinedTargetAssemblies();

            TargetAssemblyInfo[] targetAssemblyInfo = new TargetAssemblyInfo[predefindTargetAssemblies.Length + (customTargetAssemblies != null ? customTargetAssemblies.Count() : 0)];

            for (int i = 0; i < predefindTargetAssemblies.Length; ++i)
            {
                targetAssemblyInfo[i].Name = AssemblyFilenameWithSuffix(predefindTargetAssemblies[i].Filename);
                targetAssemblyInfo[i].Flags = predefindTargetAssemblies[i].Flags;
            }

            if (customTargetAssemblies != null)
                for (int i = 0; i < customTargetAssemblies.Count(); ++i)
                {
                    int j = predefindTargetAssemblies.Length + i;

                    targetAssemblyInfo[j].Name = AssemblyFilenameWithSuffix(customTargetAssemblies[i].Filename);
                    targetAssemblyInfo[j].Flags = customTargetAssemblies[i].Flags;
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
            var assemblyName = Path.GetFileNameWithoutExtension(assemblyNameOrPath);
            var scriptAssemblies = GetAllScriptAssembliesForLanguage<T>();
            return scriptAssemblies.SingleOrDefault(a => String.Compare(assemblyName, Path.GetFileNameWithoutExtension(a.Filename), StringComparison.OrdinalIgnoreCase) == 0);
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
            TargetAssemblyInfo targetAssemblyInfo;

            targetAssemblyInfo.Name = AssemblyFilenameWithSuffix(targetAssembly.Filename);
            targetAssemblyInfo.Flags = targetAssembly.Flags;
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
            var assemblyFilename = Path.GetFileName(assemblyNameOrPath);

            if (!assemblyFilename.EndsWith(".dll"))
                assemblyFilename += ".dll";

            var predefinedAssemblyTargets = EditorBuildRules.GetPredefinedTargetAssemblies();

            if (predefinedAssemblyTargets.Any(a => ((a.Flags & AssemblyFlags.EditorOnly) != AssemblyFlags.EditorOnly) && a.Filename == assemblyFilename))
                return true;

            if (customTargetAssemblies.Any(a => ((a.Flags & AssemblyFlags.EditorOnly) != AssemblyFlags.EditorOnly) && a.Filename == assemblyFilename))
                return true;

            return false;
        }
    }
}
