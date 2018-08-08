// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
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

        public extern void Update();
        public extern void WaitForCompletion();

        public unsafe NativeArray<T> GetData<T>(int layer = 0) where T : struct
        {
            if (!done || hasError)
                throw new InvalidOperationException("Cannot access the data as it is not available");

            if (layer < 0 || layer >= layerCount)
                throw new ArgumentException(string.Format("Layer index is out of range {0} / {1}", layer, layerCount));

            int stride = UnsafeUtility.SizeOf<T>();

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)GetDataRaw(layer), layerDataSize / stride, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, GetSafetyHandle());
            return array;
        }

        public bool done { get { return IsDone(); } }
        public bool hasError { get { return HasError(); } }
        public int layerCount { get { return GetLayerCount(); } }
        public int layerDataSize { get { return GetLayerDataSize(); } }
        public int width { get { return GetWidth(); } }
        public int height { get { return GetHeight(); } }
        public int depth { get { return GetDepth(); } }

        private extern bool IsDone();
        private extern bool HasError();
        private extern int GetLayerCount();
        private extern int GetLayerDataSize();
        private extern int GetWidth();
        private extern int GetHeight();
        private extern int GetDepth();

        internal extern void CreateSafetyHandle();
        private extern AtomicSafetyHandle GetSafetyHandle();
        internal extern void SetScriptingCallback(Action<AsyncGPUReadbackRequest> callback);
        unsafe private extern IntPtr GetDataRaw(int layer);
    }

    [StaticAccessor("AsyncGPUReadbackManager::GetInstance()", StaticAccessorType.Dot)]
    public static class AsyncGPUReadback
    {
        static private void SetUpScriptingRequest(AsyncGPUReadbackRequest request, Action<AsyncGPUReadbackRequest> callback)
        {
            request.SetScriptingCallback(callback);
            request.CreateSafetyHandle();
        }

        static public AsyncGPUReadbackRequest Request(ComputeBuffer src, Action<AsyncGPUReadbackRequest> callback = null)
        {
            AsyncGPUReadbackRequest request = Request_Internal_ComputeBuffer_1(src);
            SetUpScriptingRequest(request, callback);
            return request;
        }

        static public AsyncGPUReadbackRequest Request(ComputeBuffer src, int size, int offset, Action<AsyncGPUReadbackRequest> callback = null)
        {
            AsyncGPUReadbackRequest request = Request_Internal_ComputeBuffer_2(src, size, offset);
            SetUpScriptingRequest(request, callback);
            return request;
        }

        static public AsyncGPUReadbackRequest Request(Texture src, int mipIndex = 0, Action<AsyncGPUReadbackRequest> callback = null)
        {
            AsyncGPUReadbackRequest request = Request_Internal_Texture_1(src, mipIndex);
            SetUpScriptingRequest(request, callback);
            return request;
        }

        static public AsyncGPUReadbackRequest Request(Texture src, int mipIndex, TextureFormat dstFormat, Action<AsyncGPUReadbackRequest> callback = null)
        {
            AsyncGPUReadbackRequest request = Request_Internal_Texture_2(src, mipIndex, dstFormat);
            SetUpScriptingRequest(request, callback);
            return request;
        }

        static public AsyncGPUReadbackRequest Request(Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, Action<AsyncGPUReadbackRequest> callback = null)
        {
            AsyncGPUReadbackRequest request = Request_Internal_Texture_3(src, mipIndex, x, width, y, height, z, depth);
            SetUpScriptingRequest(request, callback);
            return request;
        }

        static public AsyncGPUReadbackRequest Request(Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, TextureFormat dstFormat, Action<AsyncGPUReadbackRequest> callback = null)
        {
            AsyncGPUReadbackRequest request = Request_Internal_Texture_4(src, mipIndex, x, width, y, height, z, depth, dstFormat);
            SetUpScriptingRequest(request, callback);
            return request;
        }

        [NativeMethod("Request")]
        static private extern AsyncGPUReadbackRequest Request_Internal_ComputeBuffer_1([NotNull] ComputeBuffer buffer);
        [NativeMethod("Request")]
        static private extern AsyncGPUReadbackRequest Request_Internal_ComputeBuffer_2([NotNull] ComputeBuffer src, int size, int offset);
        [NativeMethod("Request")]
        static private extern AsyncGPUReadbackRequest Request_Internal_Texture_1([NotNull] Texture src, int mipIndex);
        [NativeMethod("Request")]
        static private extern AsyncGPUReadbackRequest Request_Internal_Texture_2([NotNull] Texture src, int mipIndex, TextureFormat dstFormat);
        [NativeMethod("Request")]
        static private extern AsyncGPUReadbackRequest Request_Internal_Texture_3([NotNull] Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth);
        [NativeMethod("Request")]
        static private extern AsyncGPUReadbackRequest Request_Internal_Texture_4([NotNull] Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, TextureFormat dstFormat);
    }
}
