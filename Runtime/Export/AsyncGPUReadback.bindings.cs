// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Rendering
{
    [NativeHeader("Runtime/Graphics/AsyncGPUReadbackManaged.h")]
    [NativeHeader("Runtime/Graphics/Texture.h")]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public struct AsyncGPUReadbackRequest
    {
        internal IntPtr m_Ptr;
        internal int m_Version;

        extern public void Update();
        extern public void WaitForCompletion();

        public unsafe NativeArray<T> GetData<T>(int layer = 0) where T : struct
        {
            if (!done || hasError)
                throw new InvalidOperationException("Cannot access the data as it is not available");

            if (layer < 0 || layer >= layerCount)
                throw new ArgumentException(string.Format("Layer index is out of range {0} / {1}", layer, layerCount));

            int stride = UnsafeUtility.SizeOf<T>();

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)GetDataRaw(layer), GetLayerDataSize() / stride, Allocator.None);
            return array;
        }

        public bool done { get { return IsDone(); } }
        public bool hasError { get { return HasError(); } }
        public int layerCount { get { return GetLayerCount(); } }


        private extern bool IsDone();
        private extern bool HasError();
        private extern int GetLayerCount();

        unsafe private extern IntPtr GetDataRaw(int layer);
        private extern int GetLayerDataSize();
    }

    [StaticAccessor("AsyncGPUReadbackManager::GetInstance()", StaticAccessorType.Dot)]
    public static class AsyncGPUReadback
    {
        static public AsyncGPUReadbackRequest Request(ComputeBuffer src)
        {
            if (src == null)
                throw new ArgumentNullException();

            AsyncGPUReadbackRequest request = Request_Internal_ComputeBuffer_1(src);
            return request;
        }

        static public AsyncGPUReadbackRequest Request(ComputeBuffer src, int size, int offset)
        {
            if (src == null)
                throw new ArgumentNullException();

            AsyncGPUReadbackRequest request = Request_Internal_ComputeBuffer_2(src, size, offset);
            return request;
        }

        static public AsyncGPUReadbackRequest Request(Texture src, int mipIndex = 0)
        {
            if (src == null)
                throw new ArgumentNullException();

            AsyncGPUReadbackRequest request = Request_Internal_Texture_1(src, mipIndex);
            return request;
        }

        static public AsyncGPUReadbackRequest Request(Texture src, int mipIndex, TextureFormat dstFormat)
        {
            if (src == null)
                throw new ArgumentNullException();

            AsyncGPUReadbackRequest request = Request_Internal_Texture_2(src, mipIndex, dstFormat);
            return request;
        }

        static public AsyncGPUReadbackRequest Request(Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth)
        {
            if (src == null)
                throw new ArgumentNullException();

            AsyncGPUReadbackRequest request = Request_Internal_Texture_3(src, mipIndex, x, width, y, height, z, depth);
            return request;
        }

        static public AsyncGPUReadbackRequest Request(Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, TextureFormat dstFormat)
        {
            if (src == null)
                throw new ArgumentNullException();

            AsyncGPUReadbackRequest request = Request_Internal_Texture_4(src, mipIndex, x, width, y, height, z, depth, dstFormat);
            return request;
        }

        [NativeMethod("Request")]
        static private extern AsyncGPUReadbackRequest Request_Internal_ComputeBuffer_1(ComputeBuffer buffer);
        [NativeMethod("Request")]
        static private extern AsyncGPUReadbackRequest Request_Internal_ComputeBuffer_2(ComputeBuffer src, int size, int offset);
        [NativeMethod("Request")]
        static private extern AsyncGPUReadbackRequest Request_Internal_Texture_1(Texture src, int mipIndex);
        [NativeMethod("Request")]
        static private extern AsyncGPUReadbackRequest Request_Internal_Texture_2(Texture src, int mipIndex, TextureFormat dstFormat);
        [NativeMethod("Request")]
        static private extern AsyncGPUReadbackRequest Request_Internal_Texture_3(Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth);
        [NativeMethod("Request")]
        static private extern AsyncGPUReadbackRequest Request_Internal_Texture_4(Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, TextureFormat dstFormat);
    }
}
