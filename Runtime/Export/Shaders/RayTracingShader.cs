// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    public sealed partial class RayTracingShader : Object
    {
        private RayTracingShader() {}
        public void SetFloat(string name, float val) { SetFloat(Shader.PropertyToID(name), val); }
        public void SetInt(string name, int val) { SetInt(Shader.PropertyToID(name), val); }
        public void SetVector(string name, Vector4 val) { SetVector(Shader.PropertyToID(name), val); }
        public void SetMatrix(string name, Matrix4x4 val) { SetMatrix(Shader.PropertyToID(name), val); }
        public void SetVectorArray(string name, Vector4[] values) { SetVectorArray(Shader.PropertyToID(name), values); }
        public void SetMatrixArray(string name, Matrix4x4[] values) { SetMatrixArray(Shader.PropertyToID(name), values); }
        public void SetFloats(string name, params float[] values) { SetFloatArray(Shader.PropertyToID(name), values); }
        public void SetFloats(int nameID, params float[] values) { SetFloatArray(nameID, values); }
        public void SetInts(string name, params int[] values) { SetIntArray(Shader.PropertyToID(name), values); }
        public void SetInts(int nameID, params int[] values) { SetIntArray(nameID, values); }
        public void SetBool(string name, bool val) { SetInt(Shader.PropertyToID(name), val ? 1 : 0); }
        public void SetBool(int nameID, bool val) { SetInt(nameID, val ? 1 : 0); }

        public void SetTexture(string resourceName, Texture texture)
        {
            SetTexture(Shader.PropertyToID(resourceName), texture);
        }

        public void SetBuffer(string resourceName, ComputeBuffer buffer)
        {
            SetBuffer(Shader.PropertyToID(resourceName), buffer);
        }

        public void SetAccelerationStructure(string name, RayTracingAccelerationStructure accelerationStructure)
        {
            SetAccelerationStructure(Shader.PropertyToID(name), accelerationStructure);
        }

        public void SetTextureFromGlobal(string resourceName, string globalTextureName)
        {
            SetTextureFromGlobal(Shader.PropertyToID(resourceName), Shader.PropertyToID(globalTextureName));
        }
    }
}
