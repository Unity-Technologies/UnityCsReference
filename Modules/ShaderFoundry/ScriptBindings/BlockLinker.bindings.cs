// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Linker/BlockLinker.h")]
    [FoundryAPI]
    internal class BlockLinker : IDisposable
    {
        private IntPtr m_Ptr;
        public BlockLinker()
            : this(Internal_Create())
        {
        }

        private BlockLinker(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        ~BlockLinker()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(BlockLinker obj) => obj.m_Ptr;
            public static BlockLinker ConvertToManaged(IntPtr ptr) => new BlockLinker(ptr);
        }

        [NativeMethod(IsThreadSafe = true)] private extern static IntPtr Internal_Create();
        [NativeMethod(IsThreadSafe = true)] private static extern void Internal_Destroy(IntPtr ptr);

        [NativeMethod(IsThreadSafe = true)] extern string Run(ShaderContainer container, FoundryHandle handle);
        public string Run(ShaderContainer container, BlockShader blockShader) => Run(container, blockShader.handle);
        public extern bool HasErrors { [NativeMethod(Name = "HasErrors", IsThreadSafe = true)] get; }
        [NativeMethod(IsThreadSafe = true)] public extern string[] GetErrors();
    }
}
