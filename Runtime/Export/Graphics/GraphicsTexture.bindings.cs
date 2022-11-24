// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Experimental;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Export/Graphics/GraphicsTexture.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [Serializable]
    public struct GraphicsTextureDescriptor
    {
        public int width;
        public int height;
        public int depth;
        public int arrayLength;
        public GraphicsFormat format;
        public int mipCount;
        public TextureDimension dimension;
    };

    [NativeType("Runtime/Export/Graphics/GraphicsTexture.bindings.h")]
    [UsedByNativeCode]
    public enum GraphicsTextureState
    {
        Constructed = 0,
        Initializing = 1,
        InitializedOnRenderThread = 2,
        DestroyQueued = 3,
        Destroyed = 4
    };

    [NativeHeader("Runtime/Export/Graphics/GraphicsTexture.bindings.h")]
    [NativeType("Runtime/Graphics/Texture/GraphicsTexture.h")]
    [UsedByNativeCode]
    public partial class GraphicsTexture : IDisposable
    {
#pragma warning disable 414
        internal IntPtr m_Ptr;
#pragma warning restore 414

        // --------------------------------------------------------------------
        // IDisposable implementation

        ~GraphicsTexture()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // we don't have any managed references, so 'disposing' part of
            // standard IDisposable pattern does not apply
            // Release native resources
            ReleaseBuffer();
            m_Ptr = IntPtr.Zero;
        }

        // --------------------------------------------------------------------
        // Actual API

        public GraphicsTexture(GraphicsTextureDescriptor desc)
        {
            m_Ptr = InitBuffer(desc);
        }

        extern public GraphicsTextureDescriptor descriptor
        {
            [FreeFunction("GraphicsTexture_Bindings::GetDescriptor", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
            get;
        }

        extern public GraphicsTextureState state
        {
            [FreeFunction("GraphicsTexture_Bindings::GetState", HasExplicitThis = true, IsThreadSafe = true)]
            get;
        }

        // --------------------------------------------------------------------
        // Bindings

        [FreeFunction("GraphicsTexture_Bindings::InitBuffer")]
        extern private static IntPtr InitBuffer(GraphicsTextureDescriptor desc);

        [FreeFunction("GraphicsTexture_Bindings::ReleaseBuffer", HasExplicitThis = true, IsThreadSafe = true)]
        extern private void ReleaseBuffer();
    }
}
