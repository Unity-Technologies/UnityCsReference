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
using System.Runtime.CompilerServices;
using UnityEngine.Profiling;

// Temporary until UploadData API is made public
[assembly: InternalsVisibleTo("Unity.Environment.Runtime")]
[assembly: InternalsVisibleTo("Unity.Environment.Baking")]

namespace UnityEngine.Rendering
{
    [Flags]
    public enum GraphicsTextureDescriptorFlags
    {
        None = 0,
        RenderTarget = 1 << 0,
        RandomWriteTarget = 1 << 1
    };

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
        public TextureDimension dimension;
        public int mipCount;
        public int numSamples;
        public GraphicsTextureDescriptorFlags flags;

        [UnityEngine.Internal.ExcludeFromDocs]
        public GraphicsTextureDescriptor()
            : this(0, 0, 1, 0, GraphicsFormat.None, TextureDimension.None)
        {
        }

        internal GraphicsTextureDescriptor(int width, int height, int depth, int arrayLength, GraphicsFormat format, TextureDimension dimension, int mipCount = 0, int numSamples = 1, GraphicsTextureDescriptorFlags flags = GraphicsTextureDescriptorFlags.None)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;
            this.arrayLength = arrayLength;
            this.format = format;
            this.dimension = dimension;
            this.mipCount = mipCount;
            this.numSamples = numSamples;
            this.flags = flags;
        }
    };

    // Keep in sync with GraphicsTextureState in GraphicsTexture.bindings.h
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
        internal IntPtr m_Ptr;

        private GraphicsTexture(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

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

        internal void UploadData(IntPtr data, int size)
        {
            if (data == IntPtr.Zero || size == 0)
            {
                Debug.LogError("No texture data provided to GraphicsTexture.UploadData");
                return;
            }

            UploadBuffer(data, (ulong)size);
        }

        internal void UploadData(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                Debug.LogError("No texture data provided to GraphicsTexture.UploadData");
                return;
            }

            UploadBuffer_Array(data);
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

        [FreeFunction("GraphicsTexture_Bindings::GetActive")] extern private static GraphicsTexture GetActive();
        [FreeFunction("RenderTextureScripting::SetActive")] extern private static void SetActive(GraphicsTexture target);
        public static GraphicsTexture active { get { return GetActive(); } set { SetActive(value); } }

        // --------------------------------------------------------------------
        // Bindings

        [FreeFunction("GraphicsTexture_Bindings::InitBuffer", ThrowsException = true)]
        extern private static IntPtr InitBuffer(GraphicsTextureDescriptor desc);

        [FreeFunction("GraphicsTexture_Bindings::ReleaseBuffer", HasExplicitThis = true, IsThreadSafe = true)]
        extern private void ReleaseBuffer();

        [FreeFunction("GraphicsTexture_Bindings::UploadBuffer", HasExplicitThis = true, ThrowsException = true)]
        extern private bool UploadBuffer(IntPtr data, ulong size);

        [FreeFunction("GraphicsTexture_Bindings::UploadBuffer", HasExplicitThis = true, ThrowsException = true)]
        extern private bool UploadBuffer_Array(byte[] data);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(GraphicsTexture graphicsTexture) => graphicsTexture.m_Ptr;
            public static GraphicsTexture ConvertToManaged(IntPtr ptr) => new GraphicsTexture(ptr);
        }
    }
}
