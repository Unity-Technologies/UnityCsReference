// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public sealed partial class Shader
    {
        // at some point we were having generic methods so all set/set-array/get/get-array could be routed through one impl where we were doing all the checks
        // alas it seems c# typeof and Type comparison alloc, meaning we create garbage for no reason whatsoever (and it is slow on top)
        // meaning we are back to copy-pasting checks/impl details

        private static void SetGlobalFloatArray(int name, float[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetGlobalFloatArrayImpl(name, values, count);
        }

        private static void SetGlobalVectorArray(int name, Vector4[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetGlobalVectorArrayImpl(name, values, count);
        }

        private static void SetGlobalMatrixArray(int name, Matrix4x4[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetGlobalMatrixArrayImpl(name, values, count);
        }

        private static void ExtractGlobalFloatArray(int name, List<float> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetGlobalFloatArrayCountImpl(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractGlobalFloatArrayImpl(name, (float[])NoAllocHelpers.ExtractArrayFromList(values));
            }
        }

        private static void ExtractGlobalVectorArray(int name, List<Vector4> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetGlobalVectorArrayCountImpl(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractGlobalVectorArrayImpl(name, (Vector4[])NoAllocHelpers.ExtractArrayFromList(values));
            }
        }

        private static void ExtractGlobalMatrixArray(int name, List<Matrix4x4> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetGlobalMatrixArrayCountImpl(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractGlobalMatrixArrayImpl(name, (Matrix4x4[])NoAllocHelpers.ExtractArrayFromList(values));
            }
        }

        public static void SetGlobalFloat(string name, float value)             { SetGlobalFloatImpl(Shader.PropertyToID(name), value); }
        public static void SetGlobalFloat(int nameID, float value)              { SetGlobalFloatImpl(nameID, value); }
        public static void SetGlobalInt(string name, int value)                 { SetGlobalFloatImpl(Shader.PropertyToID(name), (float)value); }
        public static void SetGlobalInt(int nameID, int value)                  { SetGlobalFloatImpl(nameID, (float)value); }
        public static void SetGlobalVector(string name, Vector4 value)          { SetGlobalVectorImpl(Shader.PropertyToID(name), value); }
        public static void SetGlobalVector(int nameID, Vector4 value)           { SetGlobalVectorImpl(nameID, value); }
        public static void SetGlobalColor(string name, Color value)             { SetGlobalVectorImpl(Shader.PropertyToID(name), (Vector4)value); }
        public static void SetGlobalColor(int nameID, Color value)              { SetGlobalVectorImpl(nameID, (Vector4)value); }
        public static void SetGlobalMatrix(string name, Matrix4x4 value)        { SetGlobalMatrixImpl(Shader.PropertyToID(name), value); }
        public static void SetGlobalMatrix(int nameID, Matrix4x4 value)         { SetGlobalMatrixImpl(nameID, value); }

        public static void SetGlobalTexture(string name, Texture value)         { SetGlobalTextureImpl(Shader.PropertyToID(name), value); }
        public static void SetGlobalTexture(int nameID, Texture value)          { SetGlobalTextureImpl(nameID, value); }
        public static void SetGlobalTexture(string name, RenderTexture value, Rendering.RenderTextureSubElement element) { SetGlobalRenderTextureImpl(Shader.PropertyToID(name), value, element); }
        public static void SetGlobalTexture(int nameID, RenderTexture value, Rendering.RenderTextureSubElement element) { SetGlobalRenderTextureImpl(nameID, value, element); }

        public static void SetGlobalBuffer(string name, ComputeBuffer value)    { SetGlobalBufferImpl(Shader.PropertyToID(name), value); }
        public static void SetGlobalBuffer(int nameID, ComputeBuffer value)     { SetGlobalBufferImpl(nameID, value); }
        public static void SetGlobalBuffer(string name, GraphicsBuffer value)    { SetGlobalGraphicsBufferImpl(Shader.PropertyToID(name), value); }
        public static void SetGlobalBuffer(int nameID, GraphicsBuffer value)     { SetGlobalGraphicsBufferImpl(nameID, value); }
        public static void SetGlobalConstantBuffer(string name, ComputeBuffer value, int offset, int size) { SetGlobalConstantBufferImpl(Shader.PropertyToID(name), value, offset, size); }
        public static void SetGlobalConstantBuffer(int nameID, ComputeBuffer value, int offset, int size) { SetGlobalConstantBufferImpl(nameID, value, offset, size); }
        public static void SetGlobalConstantBuffer(string name, GraphicsBuffer value, int offset, int size) { SetGlobalConstantGraphicsBufferImpl(Shader.PropertyToID(name), value, offset, size); }
        public static void SetGlobalConstantBuffer(int nameID, GraphicsBuffer value, int offset, int size) { SetGlobalConstantGraphicsBufferImpl(nameID, value, offset, size); }

        public static void SetGlobalFloatArray(string name, List<float> values) { SetGlobalFloatArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public static void SetGlobalFloatArray(int nameID, List<float> values)  { SetGlobalFloatArray(nameID, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public static void SetGlobalFloatArray(string name, float[] values)     { SetGlobalFloatArray(Shader.PropertyToID(name), values, values.Length); }
        public static void SetGlobalFloatArray(int nameID, float[] values)      { SetGlobalFloatArray(nameID, values, values.Length); }

        public static void SetGlobalVectorArray(string name, List<Vector4> values)  { SetGlobalVectorArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public static void SetGlobalVectorArray(int nameID, List<Vector4> values)   { SetGlobalVectorArray(nameID, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public static void SetGlobalVectorArray(string name, Vector4[] values)      { SetGlobalVectorArray(Shader.PropertyToID(name), values, values.Length); }
        public static void SetGlobalVectorArray(int nameID, Vector4[] values)       { SetGlobalVectorArray(nameID, values, values.Length); }

        public static void SetGlobalMatrixArray(string name, List<Matrix4x4> values) { SetGlobalMatrixArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public static void SetGlobalMatrixArray(int nameID, List<Matrix4x4> values) { SetGlobalMatrixArray(nameID, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public static void SetGlobalMatrixArray(string name, Matrix4x4[] values)    { SetGlobalMatrixArray(Shader.PropertyToID(name), values, values.Length); }
        public static void SetGlobalMatrixArray(int nameID, Matrix4x4[] values)     { SetGlobalMatrixArray(nameID, values, values.Length); }

        public static float       GetGlobalFloat(string name)       { return GetGlobalFloatImpl(Shader.PropertyToID(name)); }
        public static float       GetGlobalFloat(int nameID)        { return GetGlobalFloatImpl(nameID); }
        public static int         GetGlobalInt(string name)         { return (int)GetGlobalFloatImpl(Shader.PropertyToID(name)); }
        public static int         GetGlobalInt(int nameID)          { return (int)GetGlobalFloatImpl(nameID); }
        public static Vector4     GetGlobalVector(string name)      { return GetGlobalVectorImpl(Shader.PropertyToID(name)); }
        public static Vector4     GetGlobalVector(int nameID)       { return GetGlobalVectorImpl(nameID); }
        public static Color       GetGlobalColor(string name)       { return (Color)GetGlobalVectorImpl(Shader.PropertyToID(name)); }
        public static Color       GetGlobalColor(int nameID)        { return (Color)GetGlobalVectorImpl(nameID); }
        public static Matrix4x4   GetGlobalMatrix(string name)      { return GetGlobalMatrixImpl(Shader.PropertyToID(name)); }
        public static Matrix4x4   GetGlobalMatrix(int nameID)       { return GetGlobalMatrixImpl(nameID); }
        public static Texture     GetGlobalTexture(string name)     { return GetGlobalTextureImpl(Shader.PropertyToID(name)); }
        public static Texture     GetGlobalTexture(int nameID)      { return GetGlobalTextureImpl(nameID); }

        public static float[]     GetGlobalFloatArray(string name)  { return GetGlobalFloatArray(Shader.PropertyToID(name)); }
        public static float[]     GetGlobalFloatArray(int nameID)   { return GetGlobalFloatArrayCountImpl(nameID) != 0 ? GetGlobalFloatArrayImpl(nameID) : null; }
        public static Vector4[]   GetGlobalVectorArray(string name) { return GetGlobalVectorArray(Shader.PropertyToID(name)); }
        public static Vector4[]   GetGlobalVectorArray(int nameID)  { return GetGlobalVectorArrayCountImpl(nameID) != 0 ? GetGlobalVectorArrayImpl(nameID) : null; }
        public static Matrix4x4[] GetGlobalMatrixArray(string name) { return GetGlobalMatrixArray(Shader.PropertyToID(name)); }
        public static Matrix4x4[] GetGlobalMatrixArray(int nameID)  { return GetGlobalMatrixArrayCountImpl(nameID) != 0 ? GetGlobalMatrixArrayImpl(nameID) : null; }

        public static void GetGlobalFloatArray(string name, List<float> values)      { ExtractGlobalFloatArray(Shader.PropertyToID(name), values); }
        public static void GetGlobalFloatArray(int nameID, List<float> values)       { ExtractGlobalFloatArray(nameID, values); }
        public static void GetGlobalVectorArray(string name, List<Vector4> values)   { ExtractGlobalVectorArray(Shader.PropertyToID(name), values); }
        public static void GetGlobalVectorArray(int nameID, List<Vector4> values)    { ExtractGlobalVectorArray(nameID, values); }
        public static void GetGlobalMatrixArray(string name, List<Matrix4x4> values) { ExtractGlobalMatrixArray(Shader.PropertyToID(name), values); }
        public static void GetGlobalMatrixArray(int nameID, List<Matrix4x4> values)  { ExtractGlobalMatrixArray(nameID, values); }

        private Shader() {}
    }
}
