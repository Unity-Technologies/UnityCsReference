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

    interface IPrecompiledAssemblyProviderCache
    {
        PrecompiledAssembly[] CachedEditorPrecompiledAssemblies { get; }
        void Dirty();
    }

    abstract class PrecompiledAssemblyProviderBase : IPrecompiledAssemblyProviderCache
    {
        public abstract PrecompiledAssembly[] GetPrecompiledAssemblies(bool isEditor, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget);
        public abstract Dictionary<string, PrecompiledAssembly> GetPrecompiledAssembliesDictionary(bool isEditor, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget);
        public abstract PrecompiledAssembly[] CachedEditorPrecompiledAssemblies { get; }
        public abstract void Dirty();

        public virtual PrecompiledAssembly[] GetPrecompiledAssembliesInternal(bool buildingForEditor, BuildTargetGroup buildTargetGroup, BuildTarget target)
        {
            return GetPrecompiledAssembliesNative(buildingForEditor, buildTargetGroup, target);
        }

        [FreeFunction("GetPrecompiledAssembliesManaged")]
        private static extern PrecompiledAssembly[] GetPrecompiledAssembliesNative(bool buildingForEditor, BuildTargetGroup buildTargetGroup, BuildTarget target);
    }

    class PrecompiledAssemblyProvider : PrecompiledAssemblyProviderBase
    {
        private Dictionary<string, PrecompiledAssembly> m_EditorPrecompiledAssemblies;
        public override PrecompiledAssembly[] CachedEditorPrecompiledAssemblies => m_EditorPrecompiledAssemblies?.Values.ToArray();

        public override PrecompiledAssembly[] GetPrecompiledAssemblies(bool isEditor, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            return GetPrecompiledAssembliesDictionary(isEditor, buildTargetGroup, buildTarget).Values.ToArray();
        }

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
                    $"Multiple precompiled assemblies with the same name {precompiledAssemblyNameToIndexes.Key} included or the current platform. Only one assembly with the same name is allowed per platform. Assembly paths: {paths}");
            }

            return fileNameToUserPrecompiledAssemblies;
        }
    }
}
