// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using System;
using UnityEditor.Compilation;
using UnityEditorInternal;
using System.Collections.Generic;

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

        [RequiredByNativeCode]
        public static void SetAssemblySuffix(string suffix)
        {
            Instance.SetAssemblySuffix(suffix);
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
            EmitExceptionAsError(() => Instance.SetAllCustomScriptAssemblyJsons(allAssemblyJsons));
        }

        [RequiredByNativeCode]
        public static void SetAllPackageAssemblies(EditorCompilation.PackageAssembly[] packageAssemblies)
        {
            EmitExceptionAsError(() => Instance.SetAllPackageAssemblies(packageAssemblies));
        }

        [RequiredByNativeCode]
        public static EditorCompilation.TargetAssemblyInfo[] GetAllCompiledAndResolvedCustomTargetAssemblies()
        {
            return EmitExceptionAsError(() => Instance.GetAllCompiledAndResolvedCustomTargetAssemblies(), new EditorCompilation.TargetAssemblyInfo[0]);
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
        public static bool CompileScripts(EditorScriptCompilationOptions definesOptions, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            return EmitExceptionAsError(() => Instance.CompileScripts(definesOptions, platformGroup, platform), false);
        }

        [RequiredByNativeCode]
        public static bool DoesProjectFolderHaveAnyDirtyScripts()
        {
            return Instance.DoesProjectFolderHaveAnyDirtyScripts();
        }

        [RequiredByNativeCode]
        public static bool DoesProjectFolderHaveAnyScripts()
        {
            return Instance.DoesProjectFolderHaveAnyScripts();
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
        public static void StopAllCompilation()
        {
            Instance.StopAllCompilation();
        }

        [RequiredByNativeCode]
        public static EditorCompilation.CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            return EmitExceptionAsError(() => Instance.TickCompilationPipeline(options, platformGroup, platform), EditorCompilation.CompileStatus.Idle);
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
            return Instance.GetAllMonoIslands();
        }
    }
}
