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

                    var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(filePath);
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

                    CompilationPipeline.LogEditorCompilationError(message, loadAssetAtPath.GetInstanceID());
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
            var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assetPath);
            Debug.LogWarning(warning, asset);
        }

        [RequiredByNativeCode]
        public static void SetAllScripts(string[] allScripts)
        {
            Instance.SetAllScripts(allScripts);
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
        public static void DirtyScript(string path)
        {
            Instance.DirtyScript(path);
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
        public static void DirtyPrecompiledAssembly(string path)
        {
            Instance.DirtyPrecompiledAssembly(path);
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
        public static void RunScriptUpdaterOnAssembly(string assemblyFilename)
        {
            Instance.RunScriptUpdaterOnAssembly(assemblyFilename);
        }

        [RequiredByNativeCode]
        public static void SetAllPrecompiledAssemblies(PrecompiledAssembly[] precompiledAssemblies)
        {
            Instance.SetAllPrecompiledAssemblies(precompiledAssemblies);
        }

        [RequiredByNativeCode]
        public static void SetAllUnityAssemblies(PrecompiledAssembly[] unityAssemblies)
        {
            Instance.SetAllUnityAssemblies(unityAssemblies);
        }

        [RequiredByNativeCode]
        public static void SetCompileScriptsOutputDirectory(string directory)
        {
            Instance.SetCompileScriptsOutputDirectory(directory);
        }

        [RequiredByNativeCode]
        public static string GetCompileScriptsOutputDirectory()
        {
            return EmitExceptionAsError(() => Instance.GetCompileScriptsOutputDirectory(), string.Empty);
        }

        [RequiredByNativeCode]
        public static void SetAllCustomScriptAssemblyJsons(string[] allAssemblyJsons)
        {
            EmitExceptionsAsErrors(Instance.SetAllCustomScriptAssemblyJsons(allAssemblyJsons));
        }

        [RequiredByNativeCode]
        public static void SetAllPackageAssemblies(EditorCompilation.PackageAssembly[] packageAssemblies)
        {
            Instance.SetAllPackageAssemblies(packageAssemblies);
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

            return result;
        }

        [RequiredByNativeCode]
        public static EditorCompilation.TargetAssemblyInfo[] GetTargetAssembliesWithScripts()
        {
            var options = GetAdditionalEditorScriptCompilationOptions();
            return Instance.GetTargetAssembliesWithScripts(options);
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
        public static EditorCompilation.CompileStatus CompileScripts(EditorScriptCompilationOptions definesOptions, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            return EmitExceptionAsError(() => Instance.CompileScripts(definesOptions, platformGroup, platform),
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
        public static EditorCompilation.CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            return EmitExceptionAsError(() => Instance.TickCompilationPipeline(options, platformGroup, platform), EditorCompilation.CompileStatus.Idle);
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
