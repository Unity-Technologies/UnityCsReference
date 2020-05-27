// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Compilation;
using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [DebuggerDisplay("{Path}")]
    [NativeHeader("Editor/Src/ScriptCompilation/ScriptCompilationPipeline.h")]
    [StructLayout(LayoutKind.Sequential)]
    struct PrecompiledAssembly
    {
        [NativeName("path")]
        public string Path;
        [NativeName("flags")]
        public AssemblyFlags Flags;
    }

    abstract class PrecompiledAssemblyProviderBase
    {
        public abstract PrecompiledAssembly[] GetPrecompiledAssemblies(bool isEditor, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget);
        public abstract Dictionary<string, PrecompiledAssembly> GetPrecompiledAssembliesDictionary(bool isEditor, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget);
        public abstract PrecompiledAssembly[] GetUnityAssemblies(bool isEditor, BuildTarget buildTarget);
        public abstract PrecompiledAssembly[] CachedEditorPrecompiledAssemblies { get; }
        public abstract PrecompiledAssembly[] CachedUnityAssemblies { get; }
        public abstract void Dirty();

        protected virtual PrecompiledAssembly[] GetUnityAssembliesInternal(bool isEditor, BuildTarget buildTarget)
        {
            return GetUnityAssembliesNative(isEditor, buildTarget);
        }

        protected virtual PrecompiledAssembly[] GetPrecompiledAssembliesInternal(bool buildingForEditor, BuildTargetGroup buildTargetGroup, BuildTarget target)
        {
            return GetPrecompiledAssembliesNative(buildingForEditor, buildTargetGroup, target);
        }

        [FreeFunction("GetPrecompiledAssembliesManaged")]
        protected static extern PrecompiledAssembly[] GetPrecompiledAssembliesNative(bool buildingForEditor, BuildTargetGroup buildTargetGroup, BuildTarget target);

        [FreeFunction("GetUnityAssembliesManaged")]
        protected static extern PrecompiledAssembly[] GetUnityAssembliesNative(bool buildingForEditor, BuildTarget target);
    }

    [NativeHeader("Editor/Src/ScriptCompilation/RoslynAnalyzers.bindings.h")]
    class PrecompiledAssemblyProvider : PrecompiledAssemblyProviderBase
    {
        private class UnityAssembliesKey
        {
            public UnityAssembliesKey(bool isEditor, BuildTarget buildTarget)
            {
                IsEditor = isEditor;
                BuildTarget = buildTarget;
            }

            public bool IsEditor { get; }
            public BuildTarget BuildTarget { get; }

            protected bool Equals(UnityAssembliesKey other)
            {
                return IsEditor == other.IsEditor && BuildTarget == other.BuildTarget;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((UnityAssembliesKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (IsEditor.GetHashCode() * 397) ^ (int)BuildTarget;
                }
            }
        }

        private Dictionary<UnityAssembliesKey, PrecompiledAssembly[]> m_UnityAssemblies;
        public override PrecompiledAssembly[] CachedUnityAssemblies => m_UnityAssemblies?.Values
        .SelectMany(x => x).ToArray();

        private Dictionary<string, PrecompiledAssembly> m_EditorPrecompiledAssemblies;
        public override PrecompiledAssembly[] CachedEditorPrecompiledAssemblies => m_EditorPrecompiledAssemblies?.Values.ToArray();

        public override PrecompiledAssembly[] GetUnityAssemblies(bool isEditor, BuildTarget buildTarget)
        {
            m_UnityAssemblies = m_UnityAssemblies ?? new Dictionary<UnityAssembliesKey, PrecompiledAssembly[]>();

            var unityAssembliesKey = new UnityAssembliesKey(isEditor, buildTarget);

            if (m_UnityAssemblies.TryGetValue(unityAssembliesKey, out var assemblies))
            {
                return assemblies;
            }

            var unityAssembliesInternal = GetUnityAssembliesInternal(isEditor, buildTarget);

            m_UnityAssemblies[unityAssembliesKey] = unityAssembliesInternal;

            return unityAssembliesInternal;
        }

        public override PrecompiledAssembly[] GetPrecompiledAssemblies(bool isEditor, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            return GetPrecompiledAssembliesDictionary(isEditor, buildTargetGroup, buildTarget).Values.ToArray();
        }

        [FreeFunction("GetAllRoslynAnalyzerPaths")]
        internal static extern string[] GetRoslynAnalyzerPaths();

        public override Dictionary<string, PrecompiledAssembly> GetPrecompiledAssembliesDictionary(bool isEditor, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            if (isEditor && m_EditorPrecompiledAssemblies != null)
            {
                return m_EditorPrecompiledAssemblies;
            }

            var precompiledAssembliesInternal = GetPrecompiledAssembliesInternal(isEditor, buildTargetGroup, buildTarget);
            var fileNameToPrecompiledAssembly = ValidateAndGetNameToPrecompiledAssembly(precompiledAssembliesInternal);

            if (isEditor)
            {
                m_EditorPrecompiledAssemblies = fileNameToPrecompiledAssembly;
            }

            return fileNameToPrecompiledAssembly;
        }

        public override void Dirty()
        {
            m_EditorPrecompiledAssemblies = null;
            m_UnityAssemblies = null;
        }

        private static Dictionary<string, PrecompiledAssembly> ValidateAndGetNameToPrecompiledAssembly(PrecompiledAssembly[] precompiledAssemblies)
        {
            if (precompiledAssemblies == null)
            {
                return new Dictionary<string, PrecompiledAssembly>(0);
            }

            Dictionary<string, PrecompiledAssembly> fileNameToUserPrecompiledAssemblies = new Dictionary<string, PrecompiledAssembly>(precompiledAssemblies.Length);

            var sameNamedPrecompiledAssemblies = new Dictionary<string, List<string>>(precompiledAssemblies.Length);
            for (int i = 0; i < precompiledAssemblies.Length; i++)
            {
                var precompiledAssembly = precompiledAssemblies[i];

                var fileName = AssetPath.GetFileName(precompiledAssembly.Path);
                if (!fileNameToUserPrecompiledAssemblies.ContainsKey(fileName))
                {
                    fileNameToUserPrecompiledAssemblies.Add(fileName, precompiledAssembly);
                }
                else
                {
                    if (!sameNamedPrecompiledAssemblies.ContainsKey(fileName))
                    {
                        sameNamedPrecompiledAssemblies.Add(fileName, new List<string>
                        {
                            fileNameToUserPrecompiledAssemblies[fileName].Path
                        });
                    }
                    sameNamedPrecompiledAssemblies[fileName].Add(precompiledAssembly.Path);
                }
            }

            foreach (var precompiledAssemblyNameToIndexes in sameNamedPrecompiledAssemblies)
            {
                string paths = string.Empty;
                foreach (var precompiledPath in precompiledAssemblyNameToIndexes.Value)
                {
                    paths += $"{Environment.NewLine}{precompiledPath}";
                }

                throw new PrecompiledAssemblyException(
                    $"Multiple precompiled assemblies with the same name {precompiledAssemblyNameToIndexes.Key} included or the current platform. Only one assembly with the same name is allowed per platform. Assembly paths: {paths}", paths);
            }

            return fileNameToUserPrecompiledAssemblies;
        }
    }
}
