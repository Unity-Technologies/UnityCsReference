// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

    interface IPrecompiledAssemblyProvider
    {
        PrecompiledAssembly[] GetPrecompiledAssemblies(bool isEditor, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget);
    }

    interface IPrecompiledAssemblyProviderCache : IPrecompiledAssemblyProvider
    {
        PrecompiledAssembly[] CachedEditorPrecompiledAssemblies { get; }
        void Dirty();
    }

    class PrecompiledAssemblyProvider : IPrecompiledAssemblyProviderCache
    {
        private PrecompiledAssembly[] m_EditorPrecompiledAssemblies;
        public PrecompiledAssembly[] CachedEditorPrecompiledAssemblies => m_EditorPrecompiledAssemblies;

        public PrecompiledAssembly[] GetPrecompiledAssemblies(bool isEditor, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            if (isEditor && m_EditorPrecompiledAssemblies != null)
            {
                return m_EditorPrecompiledAssemblies;
            }

            var precompiledAssembliesInternal = GetPrecompiledAssembliesInternal(isEditor, buildTargetGroup, buildTarget);
            if (isEditor)
            {
                m_EditorPrecompiledAssemblies = precompiledAssembliesInternal;
            }

            return precompiledAssembliesInternal;
        }

        public void Dirty()
        {
            m_EditorPrecompiledAssemblies = null;
        }

        [FreeFunction("GetPrecompiledAssembliesManaged")]
        extern internal static PrecompiledAssembly[] GetPrecompiledAssembliesInternal(bool buildingForEditor, BuildTargetGroup buildTargetGroup, BuildTarget target);
    }
}
