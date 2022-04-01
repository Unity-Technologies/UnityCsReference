// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using Bee.BeeDriver;
using ScriptCompilationBuildProgram.Data;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.Compilation
{
    public enum AssemblyBuilderStatus
    {
        NotStarted = 0,
        IsCompiling = 1,
        Finished = 2,
    }

    [Flags]
    public enum AssemblyBuilderFlags
    {
        None = 0,
        EditorAssembly = 1,
        DevelopmentBuild = 2,
    }

    [Flags]
    public enum ReferencesOptions
    {
        None = 0,
        UseEngineModules = 1
    }

    public class AssemblyBuilder
    {
        public event Action<string> buildStarted;
        public event Action<string, CompilerMessage[]> buildFinished;

        public string[] scriptPaths { get; private set; }
        public string assemblyPath { get; private set; }
        public string[] additionalDefines { get; set; }
        public string[] additionalReferences { get; set; }
        public string[] excludeReferences { get; set; }
        public ScriptCompilerOptions compilerOptions { get; set; }
        public ReferencesOptions referencesOptions { get; set; }

        public AssemblyBuilderFlags flags { get; set; }
        public BuildTargetGroup buildTargetGroup { get; set; }
        public BuildTarget buildTarget { get; set; }

        public string[] defaultReferences => GetDefaultReferences(EditorCompilationInterface.Instance);
        public string[] defaultDefines => GetDefaultDefines(EditorCompilationInterface.Instance);

        private class BeeScriptCompilationState
        {
            public ScriptAssembly[] assemblies;
            public BeeDriver Driver;
            public EditorCompilation editorCompilation;
            public bool finishedCompiling;
        }
        private BeeScriptCompilationState activeBeeBuild;

        public AssemblyBuilder(string assemblyPath, params string[] scriptPaths)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                throw new ArgumentException("assemblyPath cannot be null or empty");

            if (scriptPaths == null || scriptPaths.Length == 0)
                throw new ArgumentException("scriptPaths cannot be null or empty");

            this.scriptPaths = scriptPaths;
            this.assemblyPath = assemblyPath;

            compilerOptions = new ScriptCompilerOptions();
            flags = AssemblyBuilderFlags.None;
            referencesOptions = ReferencesOptions.None;
            buildTargetGroup = EditorUserBuildSettings.activeBuildTargetGroup;
            buildTarget = EditorUserBuildSettings.activeBuildTarget;
        }

        public bool Build()
        {
            return Build(EditorCompilationInterface.Instance);
        }

        internal bool Build(EditorCompilation editorCompilation)
        {
            if (editorCompilation.IsCompilationTaskCompiling()
                || editorCompilation.IsAnyAssemblyBuilderCompiling())
            {
                return false;
            }

            if (status != AssemblyBuilderStatus.NotStarted)
                throw new Exception(string.Format("Cannot start AssemblyBuilder with status {0}. Expected {1}", status, AssemblyBuilderStatus.NotStarted));

            var assembly = editorCompilation.CreateScriptAssembly(this);
            var assemblies = assembly.AllRecursiveScripAssemblyReferencesIncludingSelf().ToArray();

            // Start clean everytime
            const string beeAssemblyBuilderDirectory = "Library/BeeAssemblyBuilder";
            string beeAssemblyBuilderDirectoryInProjectDirectory =
                string.IsNullOrEmpty(editorCompilation.projectDirectory)
                ? beeAssemblyBuilderDirectory
                : Path.Combine(editorCompilation.projectDirectory, beeAssemblyBuilderDirectory);

            if (Directory.Exists(beeAssemblyBuilderDirectoryInProjectDirectory))
                Directory.Delete(beeAssemblyBuilderDirectoryInProjectDirectory, true);

            var debug = compilerOptions.CodeOptimization == CodeOptimization.Debug;
            activeBeeBuild = new BeeScriptCompilationState
            {
                assemblies = new[] { assembly },
                Driver = UnityBeeDriver.Make(EditorCompilation.ScriptCompilationBuildProgram, editorCompilation, $"{(int)assembly.BuildTarget}{"AB"}", beeAssemblyBuilderDirectory),
                editorCompilation = editorCompilation,
                finishedCompiling = false,
            };
            BeeScriptCompilation.AddScriptCompilationData(activeBeeBuild.Driver, editorCompilation, assemblies, debug, assembly.OutputDirectory, assembly.BuildTarget, true);
            activeBeeBuild.Driver.BuildAsync(Constants.ScriptAssembliesTarget);
            activeBeeBuild.editorCompilation.AddAssemblyBuilder(this);

            InvokeBuildStarted();
            return true;
        }

        private string[] GetDefaultReferences(EditorCompilation editorCompilation)
        {
            return editorCompilation.GetAssemblyBuilderDefaultReferences(this);
        }

        private string[] GetDefaultDefines(EditorCompilation editorCompilation)
        {
            return editorCompilation.GetAssemblyBuilderDefaultDefines(this);
        }

        public AssemblyBuilderStatus status
        {
            get
            {
                if (activeBeeBuild == null)
                    return AssemblyBuilderStatus.NotStarted;

                if (activeBeeBuild.finishedCompiling)
                    return AssemblyBuilderStatus.Finished;

                var result = activeBeeBuild.Driver.Tick();
                if (result == null)
                    return AssemblyBuilderStatus.IsCompiling;

                activeBeeBuild.finishedCompiling = true;
                InvokeBuildFinished(result);

                return AssemblyBuilderStatus.Finished;
            }
        }

        private void InvokeBuildStarted()
        {
            try
            {
                buildStarted?.Invoke(assemblyPath);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            activeBeeBuild.editorCompilation.InvokeCompilationStarted(this);
        }

        private void InvokeBuildFinished(BeeDriverResult result)
        {
            activeBeeBuild.editorCompilation.ProcessCompilationResult(activeBeeBuild.assemblies, result, false, this);
            try
            {
                buildFinished?.Invoke(assemblyPath, EditorCompilation.ConvertCompilerMessages(BeeScriptCompilation
                    .ParseAllResultsIntoCompilerMessages(result.BeeDriverMessages, result.NodeResults, EditorCompilationInterface.Instance)
                    .SelectMany(a => a).ToArray()));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
