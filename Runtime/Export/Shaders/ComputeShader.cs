// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using uei = UnityEngine.Internal;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;

namespace UnityEngine
{
    public sealed partial class ComputeShader : Object
    {
        private ComputeShader() {}

        public void SetFloat(string name, float val)        { SetFloat(Shader.PropertyToID(name), val); }
        public void SetInt(string name, int val)            { SetInt(Shader.PropertyToID(name), val); }
        public void SetVector(string name, Vector4 val)     { SetVector(Shader.PropertyToID(name), val); }
        public void SetMatrix(string name, Matrix4x4 val)   { SetMatrix(Shader.PropertyToID(name), val); }

        public void SetVectorArray(string name, Vector4[] values)   { SetVectorArray(Shader.PropertyToID(name), values); }
        public void SetMatrixArray(string name, Matrix4x4[] values) { SetMatrixArray(Shader.PropertyToID(name), values); }
        public void SetFloats(string name, params float[] values)   { SetFloatArray(Shader.PropertyToID(name), values); }
        public void SetFloats(int nameID, params float[] values)    { SetFloatArray(nameID, values); }
        public void SetInts(string name, params int[] values)       { SetIntArray(Shader.PropertyToID(name), values); }
        public void SetInts(int nameID, params int[] values)        { SetIntArray(nameID, values); }

        public void SetBool(string name, bool val)          { SetInt(Shader.PropertyToID(name), val ? 1 : 0); }
        public void SetBool(int nameID, bool val)           { SetInt(nameID, val ? 1 : 0); }

        public void SetTexture(int kernelIndex, int nameID, Texture texture)
        {
            SetTexture(kernelIndex, nameID, texture, 0);
        }

        public void SetTexture(int kernelIndex, string name, Texture texture)
        {
            SetTexture(kernelIndex, Shader.PropertyToID(name), texture, 0);
        }

        public void SetTexture(int kernelIndex, string name, Texture texture, int mipLevel)
        {
            SetTexture(kernelIndex, Shader.PropertyToID(name), texture, mipLevel);
        }

        public void SetTexture(int kernelIndex, int nameID, RenderTexture texture, int mipLevel, Rendering.RenderTextureSubElement element)
        {
            SetRenderTexture(kernelIndex, nameID, texture, mipLevel, element);
        }

        public void SetTexture(int kernelIndex, string name, RenderTexture texture, int mipLevel, Rendering.RenderTextureSubElement element)
        {
            SetRenderTexture(kernelIndex, Shader.PropertyToID(name), texture, mipLevel, element);
        }

        public void SetTextureFromGlobal(int kernelIndex, string name, string globalTextureName)
        {
            SetTextureFromGlobal(kernelIndex, Shader.PropertyToID(name), Shader.PropertyToID(globalTextureName));
        }

        public void SetBuffer(int kernelIndex, string name, ComputeBuffer buffer)
        {
            SetBuffer(kernelIndex, Shader.PropertyToID(name), buffer);
        }

        public void SetBuffer(int kernelIndex, string name, GraphicsBuffer buffer)
        {
            SetBuffer(kernelIndex, Shader.PropertyToID(name), buffer);
        }

        public void SetRayTracingAccelerationStructure(int kernelIndex, string name, Rendering.RayTracingAccelerationStructure accelerationStructure)
        {
            SetRayTracingAccelerationStructure(kernelIndex, Shader.PropertyToID(name), accelerationStructure);
        }

        public void SetConstantBuffer(int nameID, ComputeBuffer buffer, int offset, int size)
        {
            SetConstantComputeBuffer(nameID, buffer, offset, size);
        }

        public void SetConstantBuffer(string name, ComputeBuffer buffer, int offset, int size)
        {
            SetConstantBuffer(Shader.PropertyToID(name), buffer, offset, size);
        }

        public void SetConstantBuffer(int nameID, GraphicsBuffer buffer, int offset, int size)
        {
            SetConstantGraphicsBuffer(nameID, buffer, offset, size);
        }

        public void SetConstantBuffer(string name, GraphicsBuffer buffer, int offset, int size)
        {
            SetConstantBuffer(Shader.PropertyToID(name), buffer, offset, size);
        }

        public void DispatchIndirect(int kernelIndex, ComputeBuffer argsBuffer, [uei.DefaultValue("0")] uint argsOffset)
        {
            if (argsBuffer == null) throw new ArgumentNullException("argsBuffer");
            if (argsBuffer.m_Ptr == IntPtr.Zero) throw new System.ObjectDisposedException("argsBuffer");

            // currently only metal has special case for compute and indirect arguments buffers
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal && !SystemInfo.supportsIndirectArgumentsBuffer)
                throw new InvalidOperationException("Indirect argument buffers are not supported.");

            Internal_DispatchIndirect(kernelIndex, argsBuffer, argsOffset);
        }

        [uei.ExcludeFromDocs]
        public void DispatchIndirect(int kernelIndex, ComputeBuffer argsBuffer)
        {
            DispatchIndirect(kernelIndex, argsBuffer, 0);
        }

        public void DispatchIndirect(int kernelIndex, GraphicsBuffer argsBuffer, [uei.DefaultValue("0")] uint argsOffset)
        {
            if (argsBuffer == null) throw new ArgumentNullException("argsBuffer");
            if (argsBuffer.m_Ptr == IntPtr.Zero) throw new System.ObjectDisposedException("argsBuffer");

            // currently only metal has special case for compute and indirect arguments buffers
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal && !SystemInfo.supportsIndirectArgumentsBuffer)
                throw new InvalidOperationException("Indirect argument buffers are not supported.");

            Internal_DispatchIndirectGraphicsBuffer(kernelIndex, argsBuffer, argsOffset);
        }

        [uei.ExcludeFromDocs]
        public void DispatchIndirect(int kernelIndex, GraphicsBuffer argsBuffer)
        {
            DispatchIndirect(kernelIndex, argsBuffer, 0);
        }
    }
}
