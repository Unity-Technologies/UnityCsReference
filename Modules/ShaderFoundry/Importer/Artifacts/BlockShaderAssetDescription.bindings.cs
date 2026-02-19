// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.ShaderFoundry;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Importer/Artifacts/BlockShaderAssetDescription.h")]
    internal sealed class BlockShaderAssetDescription : IDisposable
    {
        IntPtr m_Ptr;
        internal bool IsValid => m_Ptr != IntPtr.Zero;
        // Denotes that the object's IntPtr was allocated by managed code and so de-allocation should be triggered
        // by managed as well.
        private readonly bool m_IsOwnedByManaged = false;

        [NativeMethod(IsThreadSafe = true)] internal static extern IntPtr Internal_Create();
        [NativeMethod(IsThreadSafe = true)] internal static extern void Internal_Destroy(IntPtr ptr);

        [NativeMethod(IsThreadSafe = true)] internal extern BlockShaderContainer GetContainer();
        [NativeMethod(IsThreadSafe = true)] internal extern Shader[] GetGeneratedShaders();
        [NativeMethod(IsThreadSafe = true)] internal extern BlockShaderSourceArtifact[] GetGeneratedShaderSource();
        [NativeMethod(IsThreadSafe = true)] internal extern BlockShaderErrors GetErrors();
        [NativeMethod(IsThreadSafe = true)] internal extern BlockShaderContainer[] GetDependencies();

        // If any errors occurred during import, the container may contain symbols in an invalid state.
        // In this case, we don't expose the container at all.
        private BlockShaderContainer GetContainerIfValid()
        {
            var errorsAsset = Errors;
            if (errorsAsset != null && !errorsAsset.HasErrors)
                return GetContainer();
            return null;
        }

        public BlockShaderContainer Container => GetContainerIfValid();
        public IEnumerable<Shader> GeneratedShaders => GetGeneratedShaders();
        public IEnumerable<BlockShaderSourceArtifact> GeneratedShaderSource => GetGeneratedShaderSource();
        public BlockShaderErrors Errors => GetErrors();
        public IEnumerable<BlockShaderContainer> Dependencies => GetDependencies();

        public BlockShaderAssetDescription()
            : this(Internal_Create())
        {
            m_IsOwnedByManaged = true;
        }

        private BlockShaderAssetDescription(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        ~BlockShaderAssetDescription()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                if (m_IsOwnedByManaged)
                    Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(BlockShaderAssetDescription obj) => obj.m_Ptr;
            public static BlockShaderAssetDescription ConvertToManaged(IntPtr ptr) => new BlockShaderAssetDescription(ptr);
        }
    }
}
