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

        static readonly string EditorAssemblyPath;
        static readonly string EditorTempPath = "Temp";
        static readonly string AssemblyTimestampPath;

        static EditorCompilation()
        {
            EditorAssemblyPath = Path.Combine("Library", "ScriptAssemblies");
            AssemblyTimestampPath = Path.Combine(EditorAssemblyPath, "BuiltinAssemblies.stamp");
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
                                loadedCustomScriptAssembly.Name.Length,
                                loadedCustomScriptAssembly.FilePath));

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
            string fullEditorAssemblyPath = Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath), EditorAssemblyPath);

            if (!Directory.Exists(fullEditorAssemblyPath))
                return;

            var deleteFiles = new List<string>(Directory.GetFiles(fullEditorAssemblyPath));
            deleteFiles.Remove(Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath), AssemblyTimestampPath));

            var scriptAssemblies = GetAllScriptAssemblies(BuildFlags.BuildingForEditor, EditorScriptCompilationOptions.BuildingForEditor);

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
            string fullEditorAssemblyPath = Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath), EditorAssemblyPath);

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

        public bool CompileScripts(EditorScriptCompilationOptions definesOptions, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            var scriptAssemblySettings = CreateScriptAssemblySettings(platformGroup, platform, definesOptions);

            BuildFlags buildFlags = BuildFlags.None;

            if ((definesOptions & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor)
                buildFlags |= BuildFlags.BuildingForEditor;

            if ((definesOptions & EditorScriptCompilationOptions.BuildingDevelopmentBuild) == EditorScriptCompilationOptions.BuildingDevelopmentBuild)
                buildFlags |= BuildFlags.BuildingDevelopmentBuild;

            EditorBuildRules.TargetAssembly[] notCompiledTargetAssemblies = null;
            bool result = CompileScripts(scriptAssemblySettings, EditorTempPath, buildFlags, ref notCompiledTargetAssemblies);

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

        internal bool CompileScripts(ScriptAssemblySettings scriptAssemblySettings, string tempBuildDirectory, BuildFlags buildflags, ref EditorBuildRules.TargetAssembly[] notCompiledTargetAssemblies)
        {
            DeleteUnusedAssemblies();
            allScripts.RemoveWhere(path => !File.Exists(Path.Combine(projectDirectory, path)));

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
                BuildFlags = buildflags,
                Settings = scriptAssemblySettings,
                Assemblies = assemblies,
                RunUpdaterAssemblies = runScriptUpdaterAssemblies
            };

            var scriptAssemblies = EditorBuildRules.GenerateChangedScriptAssemblies(args);

            notCompiledTargetAssemblies = args.NotCompiledTargetAssemblies.ToArray();

            if (!scriptAssemblies.Any())
                return false;

            // Compile to tempBuildDirectory
            compilationTask = new CompilationTask(scriptAssemblies, tempBuildDirectory, buildflags, UnityEngine.SystemInfo.processorCount);

            compilationTask.OnCompilationStarted += (assembly, phase) =>
                {
                    Console.WriteLine("- Starting compile {0}", Path.Combine(scriptAssemblySettings.OutputDirectory, assembly.Filename));
                };

            var compilingMonoIslands = GetAllMonoIslands().Where(i => 0 < i._files.Length);
            compilationTask.OnCompilationFinished += (assembly, messages) =>
                {
                    Console.WriteLine("- Finished compile {0}", Path.Combine(scriptAssemblySettings.OutputDirectory, assembly.Filename));

                    if (runScriptUpdaterAssemblies.Contains(assembly.Filename))
                        runScriptUpdaterAssemblies.Remove(assembly.Filename);

                    if (messages.Any(m => m.type == CompilerMessageType.Error))
                        return;

                    string enginePath = InternalEditorUtility.GetEngineAssemblyPath();
                    string unetPath = UnityEditor.EditorApplication.applicationContentsPath + "/UnityExtensions/Unity/Networking/UnityEngine.Networking.dll";
                    if (!Serialization.Weaver.WeaveUnetFromEditor(compilingMonoIslands, Path.Combine(tempBuildDirectory, assembly.Filename), Path.Combine(EditorTempPath, assembly.Filename),
                            enginePath, unetPath, (buildflags & BuildFlags.BuildingForEditor) != 0))
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

        // TODO: Native should always keep allScripts in sync so that removing scripts
        // in this method becomes unnecesssary.
        // Ideally this method would just be: "return (areAllScriptsDirty && allScripts.Count > 0) || dirtyScripts.Count > 0;"
        public bool DoesProjectFolderHaveAnyDirtyScripts()
        {
            if (dirtyScripts.Count > 0)
                return true;
            if (!areAllScriptsDirty)
                return false;
            allScripts.RemoveWhere(path => !File.Exists(Path.Combine(projectDirectory, path)));
            return allScripts.Count > 0;
        }

        public bool DoesProjectFolderHaveAnyScripts()
        {
            return allScripts != null && allScripts.Count > 0;
        }

        ScriptAssemblySettings CreateScriptAssemblySettings(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, EditorScriptCompilationOptions definesOptions)
        {
            var defines = InternalEditorUtility.GetCompilationDefines(definesOptions, buildTargetGroup, buildTarget);

            var settings = new ScriptAssemblySettings
            {
                BuildTarget = buildTarget,
                BuildTargetGroup = buildTargetGroup,
                OutputDirectory = EditorAssemblyPath,
                Defines = defines,
                ApiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup),
                FilenameSuffix = assemblySuffix
            };

            return settings;
        }

        ScriptAssemblySettings CreateEditorScriptAssemblySettings(EditorScriptCompilationOptions defines)
        {
            return CreateScriptAssemblySettings(EditorUserBuildSettings.activeBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget, defines);
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

        public CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform)
        {
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

        ScriptAssembly[] GetAllScriptAssemblies(BuildFlags buildFlags, EditorScriptCompilationOptions options)
        {
            return GetAllScriptAssemblies(buildFlags, options, unityAssemblies, precompiledAssemblies);
        }

        ScriptAssembly[] GetAllScriptAssemblies(BuildFlags buildFlags, EditorScriptCompilationOptions options, PrecompiledAssembly[] unityAssembliesArg, PrecompiledAssembly[] precompiledAssembliesArg)
        {
            var assemblies = new EditorBuildRules.CompilationAssemblies
            {
                UnityAssemblies = unityAssembliesArg,
                PrecompiledAssemblies = precompiledAssembliesArg,
                CustomTargetAssemblies = customTargetAssemblies,
                EditorAssemblyReferences = ModuleUtils.GetAdditionalReferencesForUserScripts()
            };

            var settings = CreateEditorScriptAssemblySettings(options);
            return EditorBuildRules.GetAllScriptAssemblies(allScripts, projectDirectory, buildFlags, settings, assemblies);
        }

        public MonoIsland[] GetAllMonoIslands()
        {
            return GetAllMonoIslands(unityAssemblies, precompiledAssemblies, BuildFlags.BuildingForEditor);
        }

        public MonoIsland[] GetAllMonoIslands(PrecompiledAssembly[] unityAssembliesArg, PrecompiledAssembly[] precompiledAssembliesArg, BuildFlags buildFlags)
        {
            var compilationOptions = (buildFlags & BuildFlags.BuildingForEditor) != 0 ? EditorScriptCompilationOptions.BuildingForEditor : EditorScriptCompilationOptions.BuildingEmpty;
            var scriptAssemblies = GetAllScriptAssemblies(buildFlags, compilationOptions, unityAssembliesArg, precompiledAssembliesArg);
            var monoIslands = new MonoIsland[scriptAssemblies.Length];

            for (int i = 0; i < scriptAssemblies.Length; ++i)
                monoIslands[i] = scriptAssemblies[i].ToMonoIsland(BuildFlags.BuildingForEditor, EditorTempPath);

            return monoIslands;
        }
    }
}
