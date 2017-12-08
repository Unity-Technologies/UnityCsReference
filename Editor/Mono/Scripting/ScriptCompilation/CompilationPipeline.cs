// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Scripting.ScriptCompilation;
using System.Linq;
using sc = UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.Compilation
{
    [Flags]
    public enum AssemblyFlags
    {
        None = 0,
        EditorAssembly = (1 << 0)
    }

    public class Assembly
    {
        public string name { get; private set; }
        public string outputPath { get; private set; }
        public string[] sourceFiles { get; private set; }
        public string[] defines { get; private set; }
        public Assembly[] assemblyReferences { get; private set; }
        public string[] compiledAssemblyReferences { get; private set; }
        public AssemblyFlags flags { get; private set; }

        public string[] allReferences { get { return assemblyReferences.Select(a => a.outputPath).Concat(compiledAssemblyReferences).ToArray(); } }

        public Assembly(string name, string outputPath, string[] sourceFiles, string[] defines, Assembly[] assemblyReferences, string[] compiledAssemblyReferences, AssemblyFlags flags)
        {
            this.name = name;
            this.outputPath = outputPath;
            this.sourceFiles = sourceFiles;
            this.defines = defines;
            this.assemblyReferences = assemblyReferences;
            this.compiledAssemblyReferences = compiledAssemblyReferences;
            this.flags = flags;
        }
    }

    public struct AssemblyDefinitionPlatform
    {
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public BuildTarget BuildTarget { get; private set; }

        internal AssemblyDefinitionPlatform(string name, string displayName, BuildTarget buildTarget) : this()
        {
            Name = name;
            DisplayName = displayName;
            BuildTarget = buildTarget;
        }
    }

    public static partial class CompilationPipeline
    {
        static AssemblyDefinitionPlatform[] assemblyDefinitionPlatforms;

        public static event Action<string> assemblyCompilationStarted;
        public static event Action<string, CompilerMessage[]> assemblyCompilationFinished;

        static CompilationPipeline()
        {
            SubscribeToEvents(EditorCompilationInterface.Instance);
        }

        internal static void SubscribeToEvents(EditorCompilation editorCompilation)
        {
            editorCompilation.assemblyCompilationStarted += (assemblyPath) =>
                {
                    try
                    {
                        if (assemblyCompilationStarted != null)
                            assemblyCompilationStarted(assemblyPath);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                };

            editorCompilation.assemblyCompilationFinished += (assemblyPath, messages) =>
                {
                    try
                    {
                        if (assemblyCompilationFinished != null)
                            assemblyCompilationFinished(assemblyPath, messages);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                };
        }

        public static Assembly[] GetAssemblies()
        {
            return GetAssemblies(EditorCompilationInterface.Instance);
        }

        public static string GetAssemblyNameFromScriptPath(string sourceFilePath)
        {
            return GetAssemblyNameFromScriptPath(EditorCompilationInterface.Instance, sourceFilePath);
        }

        public static string GetAssemblyDefinitionFilePathFromScriptPath(string sourceFilePath)
        {
            return GetAssemblyDefinitionFilePathFromScriptPath(EditorCompilationInterface.Instance, sourceFilePath);
        }

        public static string GetAssemblyDefinitionFilePathFromAssemblyName(string assemblyName)
        {
            return GetAssemblyDefinitionFilePathFromAssemblyName(EditorCompilationInterface.Instance, assemblyName);
        }

        public static AssemblyDefinitionPlatform[] GetAssemblyDefinitionPlatforms()
        {
            if (assemblyDefinitionPlatforms == null)
            {
                assemblyDefinitionPlatforms = CustomScriptAssembly.Platforms.Select(p => new AssemblyDefinitionPlatform(p.Name, p.DisplayName, p.BuildTarget)).ToArray();
                Array.Sort(assemblyDefinitionPlatforms, CompareAssemblyDefinitionPlatformByDisplayName);
            }

            return assemblyDefinitionPlatforms;
        }

        internal static Assembly[] GetAssemblies(EditorCompilation editorCompilation)
        {
            var scriptAssemblies = editorCompilation.GetAllEditorScriptAssemblies();
            var assemblies = new Assembly[scriptAssemblies.Length];

            var scriptAssemblyToAssembly = new Dictionary<ScriptAssembly, Assembly>();

            for (int i = 0; i < scriptAssemblies.Length; ++i)
                scriptAssemblyToAssembly.Add(scriptAssemblies[i], assemblies[i]);

            for (int i = 0; i < scriptAssemblies.Length; ++i)
            {
                var scriptAssembly = scriptAssemblies[i];

                var name = AssetPath.GetAssemblyNameWithoutExtension(scriptAssembly.Filename);
                var outputPath = scriptAssembly.FullPath;
                var sourceFiles = scriptAssembly.Files;
                var defines = scriptAssembly.Defines;
                var compiledAssemblyReferences = scriptAssembly.References;
                var assemblyReferences = scriptAssembly.ScriptAssemblyReferences.Select(a => scriptAssemblyToAssembly[a]).ToArray();

                var flags = AssemblyFlags.None;

                if ((scriptAssembly.Flags & sc.AssemblyFlags.EditorOnly) == sc.AssemblyFlags.EditorOnly)
                    flags |= AssemblyFlags.EditorAssembly;

                assemblies[i] = new Assembly(name, outputPath, sourceFiles, defines, assemblyReferences, compiledAssemblyReferences, flags);
            }

            return assemblies;
        }

        static int CompareAssemblyDefinitionPlatformByDisplayName(AssemblyDefinitionPlatform p1, AssemblyDefinitionPlatform p2)
        {
            return string.Compare(p1.DisplayName, p2.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        internal static string GetAssemblyNameFromScriptPath(EditorCompilation editorCompilation, string sourceFilePath)
        {
            try
            {
                var targetAssembly = editorCompilation.GetTargetAssembly(sourceFilePath);
                return targetAssembly.Name;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static string GetAssemblyDefinitionFilePathFromAssemblyName(EditorCompilation editorCompilation, string assemblyName)
        {
            try
            {
                var customScriptAssembly = editorCompilation.FindCustomScriptAssemblyFromAssemblyName(assemblyName);
                return customScriptAssembly.FilePath;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static string GetAssemblyDefinitionFilePathFromScriptPath(EditorCompilation editorCompilation, string sourceFilePath)
        {
            try
            {
                var customScriptAssembly = editorCompilation.FindCustomScriptAssemblyFromScriptPath(sourceFilePath);
                return customScriptAssembly != null ? customScriptAssembly.FilePath : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
