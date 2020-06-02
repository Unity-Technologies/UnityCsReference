// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using System;
using UnityEditor.Compilation;
using UnityEditor.Scripting.Compilers;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class EditorCompilationInterface
    {
        static EditorCompilation editorCompilation;

        public static EditorCompilation Instance
        {
            get
            {
                if (editorCompilation == null)
                {
                    editorCompilation = new EditorCompilation();
                    editorCompilation.setupErrorFlagsChanged += ClearErrors;

                    // Clear the errors after creating editorCompilation,
                    // as it is accessed in the CompilationPipeline cctor.
                    CompilationPipeline.ClearEditorCompilationErrors(); // Clear all errors on domain reload.
                }

                return editorCompilation;
            }
        }

        static void ClearErrors(EditorCompilation.CompilationSetupErrorFlags flags)
        {
            if (flags == EditorCompilation.CompilationSetupErrorFlags.none)
                CompilationPipeline.ClearEditorCompilationErrors();
        }

        static void LogException(Exception exception)
        {
            var assemblyDefinitionException = exception as AssemblyDefinitionException;
            var precompiledAssemblyException = exception as PrecompiledAssemblyException;

            if (assemblyDefinitionException != null && assemblyDefinitionException.filePaths.Length > 0)
            {
                foreach (var filePath in assemblyDefinitionException.filePaths)
                {
                    var message = string.Format("{0} ({1})", exception.Message, filePath);

                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
                    var instanceID = asset.GetInstanceID();

                    CompilationPipeline.LogEditorCompilationError(message, instanceID);
                }
            }
            else if (precompiledAssemblyException != null)
            {
                foreach (var filePath in precompiledAssemblyException.filePaths)
                {
                    var message = string.Format(exception.Message, filePath);
                    var loadAssetAtPath = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);

                    if (loadAssetAtPath != null)
                    {
                        CompilationPipeline.LogEditorCompilationError(message, loadAssetAtPath.GetInstanceID());
                    }
                    else
                    {
                        UnityEngine.Debug.LogException(exception);
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogException(exception);
            }
        }

        static void EmitExceptionAsError(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }

        static void EmitExceptionsAsErrors(Exception[] exceptions)
        {
            if (exceptions == null || exceptions.Length == 0)
                return;

            foreach (var exception in exceptions)
                LogException(exception);
        }

        static T EmitExceptionAsError<T>(Func<T> func, T returnValue)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                LogException(e);
                return returnValue;
            }
        }

        static void LogWarning(string warning, string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            Debug.LogWarning(warning, asset);
        }

        [RequiredByNativeCode]
        public static void Initialize()
        {
            Instance.Initialize();
        }

        [RequiredByNativeCode]
        public static void SetAssetPathsMetaData(AssetPathMetaData[] assetPathMetaDatas)
        {
            Instance.SetAssetPathsMetaData(assetPathMetaDatas);
        }

        [RequiredByNativeCode]
        public static void SetAllScripts(string[] allScripts, string[] assemblyFilenames)
        {
            Instance.SetAllScripts(allScripts, assemblyFilenames);
        }

        [RequiredByNativeCode]
        public static bool IsExtensionSupportedByCompiler(string extension)
        {
            return Instance.IsExtensionSupportedByCompiler(extension);
        }

        [RequiredByNativeCode]
        public static string[] GetExtensionsSupportedByCompiler()
        {
            return Instance.GetExtensionsSupportedByCompiler();
        }

        [RequiredByNativeCode]
        public static string[] GetChangedAssemblies()
        {
            return Instance.GetChangedAssemblies();
        }

        [RequiredByNativeCode]
        public static void DirtyPredefinedAssemblyScripts(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            EmitExceptionAsError(() => Instance.DirtyPredefinedAssemblyScripts(options, platformGroup, platform));
        }

        [RequiredByNativeCode]
        public static void DirtyAllScripts()
        {
            Instance.DirtyAllScripts();
        }

        [RequiredByNativeCode]
        public static void DirtyScript(string path, string assemblyFilename)
        {
            Instance.DirtyScript(path, assemblyFilename);
        }

        [RequiredByNativeCode]
        public static void DirtyRemovedScript(string path)
        {
            Instance.DirtyRemovedScript(path);
        }

        [RequiredByNativeCode]
        public static void DirtyMovedScript(string oldPath, string newPath)
        {
            Instance.DirtyMovedScript(oldPath, newPath);
        }

        [RequiredByNativeCode]
        public static void DirtyPrecompiledAssemblies(string[] paths)
        {
            EmitExceptionAsError(() => Instance.DirtyPrecompiledAssemblies(paths));
        }

        [RequiredByNativeCode]
        public static void ClearDirtyScripts()
        {
            Instance.ClearDirtyScripts();
        }

        [RequiredByNativeCode]
        public static void RecompileAllScriptsOnNextTick()
        {
            Instance.RecompileAllScriptsOnNextTick();
        }

        [RequiredByNativeCode]
        public static bool WillRecompileAllScriptsOnNextTick()
        {
            return Instance.WillRecompileAllScriptsOnNextTick();
        }

        [RequiredByNativeCode]
        public static void SkipCustomScriptAssemblyGraphValidation(bool skipValidation)
        {
            Instance.SkipCustomScriptAssemblyGraphValidation(skipValidation);
        }

        [RequiredByNativeCode]
        public static void ClearCustomScriptAssemblies()
        {
            Instance.ClearCustomScriptAssemblies();
        }

        [RequiredByNativeCode]
        public static void RunScriptUpdaterOnAssembly(string assemblyFilename)
        {
            Instance.RunScriptUpdaterOnAssembly(assemblyFilename);
        }

        [RequiredByNativeCode]
        public static void SetAllUnityAssemblies(PrecompiledAssembly[] unityAssemblies)
        {
            Instance.SetAllUnityAssemblies(unityAssemblies);
        }

        // Burst package depends on this method, so we can't remove it.
        [RequiredByNativeCode]
        public static void SetCompileScriptsOutputDirectory(string directory)
        {
            Instance.SetCompileScriptsOutputDirectory(directory);
        }

        [RequiredByNativeCode]
        public static void SetAssembliesOutputDirectories(string directory, string editorDirectory)
        {
            Instance.SetAssembliesOutputDirectories(directory, editorDirectory);
        }

        [RequiredByNativeCode]
        public static string GetCompileScriptsOutputDirectory()
        {
            return EmitExceptionAsError(() => Instance.GetCompileScriptsOutputDirectory(), string.Empty);
        }

        [RequiredByNativeCode]
        public static void SetAllCustomScriptAssemblyJsons(string[] allAssemblyJsons, string[] guids)
        {
            EmitExceptionsAsErrors(Instance.SetAllCustomScriptAssemblyJsons(allAssemblyJsons, guids));
        }

        [RequiredByNativeCode]
        public static void SetAllCustomScriptAssemblyReferenceJsons(string[] allAssemblyReferenceJsons, string[] allAssemblyReferenceJsonContents)
        {
            EmitExceptionsAsErrors(Instance.SetAllCustomScriptAssemblyReferenceJsonsContents(allAssemblyReferenceJsons, allAssemblyReferenceJsonContents));
        }

        [RequiredByNativeCode]
        public static void SetAllCustomScriptAssemblyJsonContents(string[] allAssemblyJsonPaths, string[] allAssemblyJsonContents, string[] guids)
        {
            EmitExceptionsAsErrors(Instance.SetAllCustomScriptAssemblyJsonContents(allAssemblyJsonPaths, allAssemblyJsonContents, guids));
        }

        [RequiredByNativeCode]
        public static EditorCompilation.TargetAssemblyInfo[] GetAllCompiledAndResolvedCustomTargetAssemblies(EditorScriptCompilationOptions options, BuildTarget buildTarget)
        {
            EditorCompilation.CustomScriptAssemblyAndReference[] assembliesWithMissingReference = null;

            var result = EmitExceptionAsError(() => Instance.GetAllCompiledAndResolvedCustomTargetAssemblies(options, buildTarget, out assembliesWithMissingReference), new EditorCompilation.TargetAssemblyInfo[0]);

            if (assembliesWithMissingReference.Length > 0)
            {
                foreach (var assemblyAndReference in assembliesWithMissingReference)
                {
                    LogWarning(string.Format("The assembly for Assembly Definition File '{0}' will not be loaded. Because the assembly for its reference '{1}'' does not exist on the file system. " +
                        "This can be caused by the reference assembly not being compiled due to errors or not having any scripts associated with it.",
                        assemblyAndReference.Assembly.FilePath,
                        assemblyAndReference.Reference.FilePath),
                        assemblyAndReference.Assembly.FilePath);
                }
            }

            // Check we do not have any assembly definition references (asmref) without matching assembly definitions (asmdef)
            List<CustomScriptAssemblyReference> referencesWithMissingAssemblies;
            Instance.GetAssemblyDefinitionReferencesWithMissingAssemblies(out referencesWithMissingAssemblies);
            if (referencesWithMissingAssemblies.Count > 0)
            {
                foreach (var asmref in referencesWithMissingAssemblies)
                {
                    var warning = $"The Assembly Definition Reference file '{asmref.FilePath}' will not be used. ";
                    if (string.IsNullOrEmpty(asmref.Reference))
                        warning += "It does not contain a reference to an Assembly Definition File.";
                    else
                        warning += $"The reference to the Assembly Definition File with the name '{asmref.Reference}' could not be found.";
                    LogWarning(warning, asmref.FilePath);
                }
            }

            return result;
        }

        [RequiredByNativeCode]
        public static string[] GetCompiledAssemblyGraph(string assemblyName)
        {
            return EmitExceptionAsError(() => Instance.GetCompiledAssemblyGraph(assemblyName), new string[0]);
        }

        [RequiredByNativeCode]
        public static EditorCompilation.TargetAssemblyInfo[] GetTargetAssembliesWithScripts()
        {
            var options = GetAdditionalEditorScriptCompilationOptions();
            return EmitExceptionAsError(() => Instance.GetTargetAssembliesWithScripts(options), new EditorCompilation.TargetAssemblyInfo[0]);
        }

        [RequiredByNativeCode]
        public static bool HaveSetupErrors()
        {
            return Instance.HaveSetupErrors();
        }

        [RequiredByNativeCode]
        public static void DeleteUnusedAssemblies()
        {
            EmitExceptionAsError(() => Instance.DeleteUnusedAssemblies());
        }

        [RequiredByNativeCode]
        public static EditorCompilation.CompileStatus CompileScripts(EditorScriptCompilationOptions definesOptions, BuildTargetGroup platformGroup, BuildTarget platform, string[] extraScriptingDefines)
        {
            return EmitExceptionAsError(() => Instance.CompileScripts(definesOptions, platformGroup, platform, extraScriptingDefines),
                EditorCompilation.CompileStatus.CompilationFailed);
        }

        [RequiredByNativeCode]
        public static bool CompileCustomScriptAssemblies(EditorScriptCompilationOptions definesOptions, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            return EmitExceptionAsError(() => Instance.CompileCustomScriptAssemblies(definesOptions, platformGroup, platform), false);
        }

        [RequiredByNativeCode]
        public static bool DoesProjectFolderHaveAnyDirtyScripts()
        {
            return Instance.DoesProjectFolderHaveAnyDirtyScripts();
        }

        [RequiredByNativeCode]
        public static bool AreAllScriptsDirty()
        {
            return Instance.AreAllScriptsDirty();
        }

        [RequiredByNativeCode]
        public static bool ArePrecompiledAssembliesDirty()
        {
            return Instance.ArePrecompiledAssembliesDirty();
        }

        [RequiredByNativeCode]
        public static bool DoesProjectFolderHaveAnyScripts()
        {
            return Instance.DoesProjectFolderHaveAnyScripts();
        }

        [RequiredByNativeCode]
        public static bool DoesProjectHaveAnyCustomScriptAssemblies()
        {
            return Instance.DoesProjectHaveAnyCustomScriptAssemblies();
        }

        [RequiredByNativeCode]
        public static EditorCompilation.AssemblyCompilerMessages[] GetCompileMessages()
        {
            return Instance.GetCompileMessages();
        }

        [RequiredByNativeCode]
        public static bool IsCompilationPending()
        {
            return Instance.IsCompilationPending();
        }

        [RequiredByNativeCode]
        public static bool IsCompiling()
        {
            return Instance.IsCompiling();
        }

        [RequiredByNativeCode]
        public static EditorCompilation.CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform, string[] extraScriptingDefines)
        {
            try
            {
                return Instance.TickCompilationPipeline(options, platformGroup, platform, extraScriptingDefines);
            }
            catch (Exception e)
            {
                LogException(e);
                ClearDirtyScripts();
                return EditorCompilation.CompileStatus.CompilationFailed;
            }
        }

        [RequiredByNativeCode]
        public static EditorCompilation.CompileStatus PollCompilation()
        {
            return EmitExceptionAsError(() => Instance.PollCompilation(), EditorCompilation.CompileStatus.Idle);
        }

        [RequiredByNativeCode]
        public static EditorCompilation.TargetAssemblyInfo[] GetTargetAssemblies()
        {
            return Instance.GetTargetAssemblies();
        }

        [RequiredByNativeCode]
        public static EditorCompilation.TargetAssemblyInfo GetTargetAssembly(string scriptPath)
        {
            return Instance.GetTargetAssembly(scriptPath);
        }

        [RequiredByNativeCode]
        public static MonoIsland[] GetAllMonoIslands()
        {
            var options = GetAdditionalEditorScriptCompilationOptions();
            return Instance.GetAllMonoIslands(options);
        }

        public static EditorScriptCompilationOptions GetAdditionalEditorScriptCompilationOptions()
        {
            var options = EditorScriptCompilationOptions.BuildingEmpty;

            if (PlayerSettings.allowUnsafeCode)
                options |= EditorScriptCompilationOptions.BuildingPredefinedAssembliesAllowUnsafeCode;

            if (PlayerSettings.useReferenceAssemblies)
                options |= EditorScriptCompilationOptions.BuildingUseReferenceAssemblies;

            if (PlayerSettings.UseDeterministicCompilation)
                options |= EditorScriptCompilationOptions.BuildingUseDeterministicCompilation;

            return options;
        }

        public static ScriptAssembly[] GetAllScriptAssembliesForLanguage<T>() where T : SupportedLanguage
        {
            var additionalOptions = GetAdditionalEditorScriptCompilationOptions();
            return Instance.GetAllScriptAssembliesForLanguage<T>(additionalOptions);
        }

        public static ScriptAssembly GetScriptAssemblyForLanguage<T>(string assemblyNameOrPath) where T : SupportedLanguage
        {
            var additionalOptions = GetAdditionalEditorScriptCompilationOptions();

            return Instance.GetScriptAssemblyForLanguage<T>(assemblyNameOrPath, additionalOptions);
        }
    }
}
