// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class EditorCompilationInterface
    {
        static readonly EditorCompilation editorCompilation = new EditorCompilation();

        public static EditorCompilation Instance
        {
            get { return editorCompilation; }
        }

        static void EmitExceptionAsError(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
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
                UnityEngine.Debug.LogError(e.Message);
                return returnValue;
            }
        }

        [RequiredByNativeCode]
        public static void SetAssemblySuffix(string suffix)
        {
            editorCompilation.SetAssemblySuffix(suffix);
        }

        [RequiredByNativeCode]
        public static void SetAllScripts(string[] allScripts)
        {
            editorCompilation.SetAllScripts(allScripts);
        }

        [RequiredByNativeCode]
        public static bool IsExtensionSupportedByCompiler(string extension)
        {
            return editorCompilation.IsExtensionSupportedByCompiler(extension);
        }

        [RequiredByNativeCode]
        public static void DirtyAllScripts()
        {
            editorCompilation.DirtyAllScripts();
        }

        [RequiredByNativeCode]
        public static void DirtyScript(string path)
        {
            editorCompilation.DirtyScript(path);
        }

        [RequiredByNativeCode]
        public static void RunScriptUpdaterOnAssembly(string assemblyFilename)
        {
            editorCompilation.RunScriptUpdaterOnAssembly(assemblyFilename);
        }

        [RequiredByNativeCode]
        public static void SetAllPrecompiledAssemblies(PrecompiledAssembly[] precompiledAssemblies)
        {
            editorCompilation.SetAllPrecompiledAssemblies(precompiledAssemblies);
        }

        [RequiredByNativeCode]
        public static void SetAllUnityAssemblies(PrecompiledAssembly[] unityAssemblies)
        {
            editorCompilation.SetAllUnityAssemblies(unityAssemblies);
        }

        [RequiredByNativeCode]
        public static void SetCompileScriptsOutputDirectory(string directory)
        {
            editorCompilation.SetCompileScriptsOutputDirectory(directory);
        }

        [RequiredByNativeCode]
        public static string GetCompileScriptsOutputDirectory()
        {
            return EmitExceptionAsError(() => editorCompilation.GetCompileScriptsOutputDirectory(), string.Empty);
        }

        [RequiredByNativeCode]
        public static void SetAllCustomScriptAssemblyJsons(string[] allAssemblyJsons)
        {
            EmitExceptionAsError(() => editorCompilation.SetAllCustomScriptAssemblyJsons(allAssemblyJsons));
        }

        [RequiredByNativeCode]
        public static void SetAllPackageAssemblies(EditorCompilation.PackageAssembly[] packageAssemblies)
        {
            EmitExceptionAsError(() => editorCompilation.SetAllPackageAssemblies(packageAssemblies));
        }

        [RequiredByNativeCode]
        public static EditorCompilation.TargetAssemblyInfo[] GetAllCompiledAndResolvedCustomTargetAssemblies()
        {
            return EmitExceptionAsError(() => editorCompilation.GetAllCompiledAndResolvedCustomTargetAssemblies(), new EditorCompilation.TargetAssemblyInfo[0]);
        }

        [RequiredByNativeCode]
        public static void DeleteUnusedAssemblies()
        {
            EmitExceptionAsError(() => editorCompilation.DeleteUnusedAssemblies());
        }

        [RequiredByNativeCode]
        public static bool CompileScripts(EditorScriptCompilationOptions definesOptions, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            return EmitExceptionAsError(() => editorCompilation.CompileScripts(definesOptions, platformGroup, platform), false);
        }

        [RequiredByNativeCode]
        public static bool DoesProjectFolderHaveAnyDirtyScripts()
        {
            return editorCompilation.DoesProjectFolderHaveAnyDirtyScripts();
        }

        [RequiredByNativeCode]
        public static bool DoesProjectFolderHaveAnyScripts()
        {
            return editorCompilation.DoesProjectFolderHaveAnyScripts();
        }

        [RequiredByNativeCode]
        public static EditorCompilation.AssemblyCompilerMessages[] GetCompileMessages()
        {
            return editorCompilation.GetCompileMessages();
        }

        [RequiredByNativeCode]
        public static bool IsCompilationPending()
        {
            return editorCompilation.IsCompilationPending();
        }

        [RequiredByNativeCode]
        public static bool IsCompiling()
        {
            return editorCompilation.IsCompiling();
        }

        [RequiredByNativeCode]
        public static void StopAllCompilation()
        {
            editorCompilation.StopAllCompilation();
        }

        [RequiredByNativeCode]
        public static EditorCompilation.CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            return EmitExceptionAsError(() => editorCompilation.TickCompilationPipeline(options, platformGroup, platform), EditorCompilation.CompileStatus.Idle);
        }

        [RequiredByNativeCode]
        public static EditorCompilation.TargetAssemblyInfo[] GetTargetAssemblies()
        {
            return editorCompilation.GetTargetAssemblies();
        }

        [RequiredByNativeCode]
        public static EditorCompilation.TargetAssemblyInfo GetTargetAssembly(string scriptPath)
        {
            return editorCompilation.GetTargetAssembly(scriptPath);
        }

        [RequiredByNativeCode]
        public static MonoIsland[] GetAllMonoIslands()
        {
            return editorCompilation.GetAllMonoIslands();
        }
    }
}
