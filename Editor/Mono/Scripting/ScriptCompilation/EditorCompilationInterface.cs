// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Scripting.Compilers;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using System.IO;
using System.Linq;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class EditorCompilationInterface
    {
        static readonly EditorCompilation editorCompilation = new EditorCompilation();

        public static EditorCompilation Instance
        {
            get { return editorCompilation; }
        }

        [RequiredByNativeCodeAttribute]
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

        [RequiredByNativeCodeAttribute]
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
        public static void SetAllCustomScriptAssemblyJsons(string[] allAssemblyJsons)
        {
            editorCompilation.SetAllCustomScriptAssemblyJsons(allAssemblyJsons);
        }

        [RequiredByNativeCode]
        public static void DeleteUnusedAssemblies()
        {
            editorCompilation.DeleteUnusedAssemblies();
        }

        [RequiredByNativeCode]
        public static bool CompileScripts(EditorScriptCompilationOptions definesOptions, BuildTargetGroup platformGroup, BuildTarget platform)
        {
            return editorCompilation.CompileScripts(definesOptions, platformGroup, platform);
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
            return editorCompilation.TickCompilationPipeline(options, platformGroup, platform);
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

        public static EditorBuildRules.TargetAssembly GetTargetAssemblyDetails(string scriptPath)
        {
            return editorCompilation.GetTargetAssemblyDetails(scriptPath);
        }

        [RequiredByNativeCode]
        public static MonoIsland[] GetAllMonoIslands()
        {
            return editorCompilation.GetAllMonoIslands();
        }

        [RequiredByNativeCode]
        public static MonoIsland[] GetAllMonoIslandsExt(PrecompiledAssembly[] unityAssemblies, PrecompiledAssembly[] precompiledAssemblies, BuildFlags buildFlags)
        {
            return editorCompilation.GetAllMonoIslands(unityAssemblies, precompiledAssemblies, buildFlags);
        }
    }
}
