// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using SphericalHarmonicsL2 = UnityEngine.Rendering.SphericalHarmonicsL2;

namespace UnityEngine
{
    public sealed partial class MaterialPropertyBlock
    {
        // at some point we were having generic methods so all set/set-array/get/get-array could be routed through one impl where we were doing all the checks
        // alas it seems c# typeof and Type comparison alloc, meaning we create garbage for no reason whatsoever (and it is slow on top)
        // meaning we are back to copy-pasting checks/impl details

        private void SetFloatArray(int name, float[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetFloatArrayImpl(name, values, count);
        }

        private void SetVectorArray(int name, Vector4[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetVectorArrayImpl(name, values, count);
        }

        private void SetMatrixArray(int name, Matrix4x4[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetMatrixArrayImpl(name, values, count);
        }

        private void ExtractFloatArray(int name, List<float> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetFloatArrayCountImpl(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractFloatArrayImpl(name, (float[])NoAllocHelpers.ExtractArrayFromList(values));
            }
        }

        private void ExtractVectorArray(int name, List<Vector4> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetVectorArrayCountImpl(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractVectorArrayImpl(name, (Vector4[])NoAllocHelpers.ExtractArrayFromList(values));
            }
        }

        private void ExtractMatrixArray(int name, List<Matrix4x4> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetMatrixArrayCountImpl(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractMatrixArrayImpl(name, (Matrix4x4[])NoAllocHelpers.ExtractArrayFromList(values));
            }
        }

        internal IntPtr m_Ptr;

        public MaterialPropertyBlock()    { m_Ptr = CreateImpl(); }
        ~MaterialPropertyBlock()          { Dispose(); }

        // should we make it IDisposable?
        private void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                DestroyImpl(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        public void SetInt(string name, int value)              { SetFloatImpl(Shader.PropertyToID(name), (float)value); }
        public void SetInt(int nameID, int value)               { SetFloatImpl(nameID, (float)value); }

        public void SetFloat(string name, float value)          { SetFloatImpl(Shader.PropertyToID(name), value); }
        public void SetFloat(int nameID, float value)           { SetFloatImpl(nameID, value); }
        public void SetInteger(string name, int value)          { SetIntImpl(Shader.PropertyToID(name), value); }
        public void SetInteger(int nameID, int value)           { SetIntImpl(nameID, value); }
        public void SetVector(string name, Vector4 value)       { SetVectorImpl(Shader.PropertyToID(name), value); }
        public void SetVector(int nameID, Vector4 value)        { SetVectorImpl(nameID, value); }
        public void SetColor(string name, Color value)          { SetColorImpl(Shader.PropertyToID(name), value); }
        public void SetColor(int nameID, Color value)           { SetColorImpl(nameID, value); }
        public void SetMatrix(string name, Matrix4x4 value)     { SetMatrixImpl(Shader.PropertyToID(name), value); }
        public void SetMatrix(int nameID, Matrix4x4 value)      { SetMatrixImpl(nameID, value); }
        public void SetBuffer(string name, ComputeBuffer value) { SetBufferImpl(Shader.PropertyToID(name), value); }
        public void SetBuffer(int nameID, ComputeBuffer value)  { SetBufferImpl(nameID, value); }
        public void SetBuffer(string name, GraphicsBuffer value) { SetGraphicsBufferImpl(Shader.PropertyToID(name), value); }
        public void SetBuffer(int nameID, GraphicsBuffer value)  { SetGraphicsBufferImpl(nameID, value); }
        public void SetTexture(string name, Texture value)      { SetTextureImpl(Shader.PropertyToID(name), value); }
        public void SetTexture(int nameID, Texture value)       { SetTextureImpl(nameID, value); }
        public void SetTexture(string name, RenderTexture value, Rendering.RenderTextureSubElement element) { SetRenderTextureImpl(Shader.PropertyToID(name), value, element); }
        public void SetTexture(int nameID, RenderTexture value, Rendering.RenderTextureSubElement element) { SetRenderTextureImpl(nameID, value, element); }
        public void SetConstantBuffer(string name, ComputeBuffer value, int offset, int size) { SetConstantBufferImpl(Shader.PropertyToID(name), value, offset, size); }
        public void SetConstantBuffer(int nameID, ComputeBuffer value, int offset, int size) { SetConstantBufferImpl(nameID, value, offset, size); }
        public void SetConstantBuffer(string name, GraphicsBuffer value, int offset, int size) { SetConstantGraphicsBufferImpl(Shader.PropertyToID(name), value, offset, size); }
        public void SetConstantBuffer(int nameID, GraphicsBuffer value, int offset, int size) { SetConstantGraphicsBufferImpl(nameID, value, offset, size); }

        public void SetFloatArray(string name, List<float> values)  { SetFloatArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetFloatArray(int nameID,  List<float> values)  { SetFloatArray(nameID, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetFloatArray(string name, float[] values)      { SetFloatArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetFloatArray(int nameID,  float[] values)      { SetFloatArray(nameID, values, values.Length); }

        public void SetVectorArray(string name, List<Vector4> values)   { SetVectorArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetVectorArray(int nameID,  List<Vector4> values)   { SetVectorArray(nameID, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetVectorArray(string name, Vector4[] values)       { SetVectorArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetVectorArray(int nameID,  Vector4[] values)       { SetVectorArray(nameID, values, values.Length); }

        public void SetMatrixArray(string name, List<Matrix4x4> values) { SetMatrixArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetMatrixArray(int nameID,  List<Matrix4x4> values) { SetMatrixArray(nameID, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetMatrixArray(string name, Matrix4x4[] values)     { SetMatrixArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetMatrixArray(int nameID,  Matrix4x4[] values)     { SetMatrixArray(nameID, values, values.Length); }

        public bool HasProperty(string name) { return HasPropertyImpl(Shader.PropertyToID(name)); }
        public bool HasProperty(int nameID) { return HasPropertyImpl(nameID); }

        public bool HasInt(string name) { return HasFloatImpl(Shader.PropertyToID(name)); }
        public bool HasInt(int nameID) { return HasFloatImpl(nameID); }

        public bool HasFloat(string name) { return HasFloatImpl(Shader.PropertyToID(name)); }
        public bool HasFloat(int nameID) { return HasFloatImpl(nameID); }
        public bool HasInteger(string name) { return HasIntImpl(Shader.PropertyToID(name)); }
        public bool HasInteger(int nameID) { return HasIntImpl(nameID); }
        public bool HasTexture(string name) { return HasTextureImpl(Shader.PropertyToID(name)); }
        public bool HasTexture(int nameID) { return HasTextureImpl(nameID); }
        public bool HasMatrix(string name) { return HasMatrixImpl(Shader.PropertyToID(name)); }
        public bool HasMatrix(int nameID) { return HasMatrixImpl(nameID); }
        public bool HasVector(string name) { return HasVectorImpl(Shader.PropertyToID(name)); }
        public bool HasVector(int nameID) { return HasVectorImpl(nameID); }
        public bool HasColor(string name) { return HasVectorImpl(Shader.PropertyToID(name)); }
        public bool HasColor(int nameID) { return HasVectorImpl(nameID); }
        public bool HasBuffer(string name) { return HasBufferImpl(Shader.PropertyToID(name)); }
        public bool HasBuffer(int nameID) { return HasBufferImpl(nameID); }
        public bool HasConstantBuffer(string name) { return HasConstantBufferImpl(Shader.PropertyToID(name)); }
        public bool HasConstantBuffer(int nameID) { return HasConstantBufferImpl(nameID); }

        public float     GetFloat(string name)      { return GetFloatImpl(Shader.PropertyToID(name)); }
        public float     GetFloat(int nameID)       { return GetFloatImpl(nameID); }

        public int       GetInt(string name)        { return (int)GetFloatImpl(Shader.PropertyToID(name)); }
        public int       GetInt(int nameID)         { return (int)GetFloatImpl(nameID); }

        public int       GetInteger(string name)    { return GetIntImpl(Shader.PropertyToID(name)); }
        public int       GetInteger(int nameID)     { return GetIntImpl(nameID); }
        public Vector4   GetVector(string name)     { return GetVectorImpl(Shader.PropertyToID(name)); }
        public Vector4   GetVector(int nameID)      { return GetVectorImpl(nameID); }
        public Color     GetColor(string name)      { return GetColorImpl(Shader.PropertyToID(name)); }
        public Color     GetColor(int nameID)       { return GetColorImpl(nameID); }
        public Matrix4x4 GetMatrix(string name)     { return GetMatrixImpl(Shader.PropertyToID(name)); }
        public Matrix4x4 GetMatrix(int nameID)      { return GetMatrixImpl(nameID); }
        public Texture   GetTexture(string name)    { return GetTextureImpl(Shader.PropertyToID(name)); }
        public Texture   GetTexture(int nameID)     { return GetTextureImpl(nameID); }

        public float[]      GetFloatArray(string name)  { return GetFloatArray(Shader.PropertyToID(name)); }
        public float[]      GetFloatArray(int nameID)   { return GetFloatArrayCountImpl(nameID) != 0 ? GetFloatArrayImpl(nameID) : null; }
        public Vector4[]    GetVectorArray(string name) { return GetVectorArray(Shader.PropertyToID(name)); }
        public Vector4[]    GetVectorArray(int nameID)  { return GetVectorArrayCountImpl(nameID) != 0 ? GetVectorArrayImpl(nameID) : null; }
        public Matrix4x4[]  GetMatrixArray(string name) { return GetMatrixArray(Shader.PropertyToID(name)); }
        public Matrix4x4[]  GetMatrixArray(int nameID)  { return GetMatrixArrayCountImpl(nameID) != 0 ? GetMatrixArrayImpl(nameID) : null; }

        public void GetFloatArray(string name, List<float> values)      { ExtractFloatArray(Shader.PropertyToID(name), values); }
        public void GetFloatArray(int nameID, List<float> values)       { ExtractFloatArray(nameID, values); }
        public void GetVectorArray(string name, List<Vector4> values)   { ExtractVectorArray(Shader.PropertyToID(name), values); }
        public void GetVectorArray(int nameID, List<Vector4> values)    { ExtractVectorArray(nameID, values); }
        public void GetMatrixArray(string name, List<Matrix4x4> values) { ExtractMatrixArray(Shader.PropertyToID(name), values); }
        public void GetMatrixArray(int nameID, List<Matrix4x4> values)  { ExtractMatrixArray(nameID, values); }

        public void CopySHCoefficientArraysFrom(List<SphericalHarmonicsL2> lightProbes)
        {
            if (lightProbes == null)
                throw new ArgumentNullException("lightProbes");
            CopySHCoefficientArraysFrom(NoAllocHelpers.ExtractArrayFromListT(lightProbes), 0, 0, lightProbes.Count);
        }

        public void CopySHCoefficientArraysFrom(SphericalHarmonicsL2[] lightProbes)
        {
            if (lightProbes == null)
                throw new ArgumentNullException("lightProbes");
            CopySHCoefficientArraysFrom(lightProbes, 0, 0, lightProbes.Length);
        }

        public void CopySHCoefficientArraysFrom(List<SphericalHarmonicsL2> lightProbes, int sourceStart, int destStart, int count)
        {
            CopySHCoefficientArraysFrom(NoAllocHelpers.ExtractArrayFromListT(lightProbes), sourceStart, destStart, count);
        }

        public void CopySHCoefficientArraysFrom(SphericalHarmonicsL2[] lightProbes, int sourceStart, int destStart, int count)
        {
            if (lightProbes == null)
                throw new ArgumentNullException("lightProbes");
            else if (sourceStart < 0)
                throw new ArgumentOutOfRangeException("sourceStart", "Argument sourceStart must not be negative.");
            else if (destStart < 0)
                throw new ArgumentOutOfRangeException("sourceStart", "Argument destStart must not be negative.");
            else if (count < 0)
                throw new ArgumentOutOfRangeException("count", "Argument count must not be negative.");
            else if (lightProbes.Length < sourceStart + count)
                throw new ArgumentOutOfRangeException("The specified source start index or count is out of the range.");

            Internal_CopySHCoefficientArraysFrom(this, lightProbes, sourceStart, destStart, count);
        }

        public void CopyProbeOcclusionArrayFrom(List<Vector4> occlusionProbes)
        {
            if (occlusionProbes == null)
                throw new ArgumentNullException("occlusionProbes");
            CopyProbeOcclusionArrayFrom(NoAllocHelpers.ExtractArrayFromListT(occlusionProbes), 0, 0, occlusionProbes.Count);
        }

        public void CopyProbeOcclusionArrayFrom(Vector4[] occlusionProbes)
        {
            if (occlusionProbes == null)
                throw new ArgumentNullException("occlusionProbes");
            CopyProbeOcclusionArrayFrom(occlusionProbes, 0, 0, occlusionProbes.Length);
        }

        public void CopyProbeOcclusionArrayFrom(List<Vector4> occlusionProbes, int sourceStart, int destStart, int count)
        {
            CopyProbeOcclusionArrayFrom(NoAllocHelpers.ExtractArrayFromListT(occlusionProbes), sourceStart, destStart, count);
        }

        public void CopyProbeOcclusionArrayFrom(Vector4[] occlusionProbes, int sourceStart, int destStart, int count)
        {
            if (occlusionProbes == null)
                throw new ArgumentNullException("occlusionProbes");
            else if (sourceStart < 0)
                throw new ArgumentOutOfRangeException("sourceStart", "Argument sourceStart must not be negative.");
            else if (destStart < 0)
                throw new ArgumentOutOfRangeException("sourceStart", "Argument destStart must not be negative.");
            else if (count < 0)
                throw new ArgumentOutOfRangeException("count", "Argument count must not be negative.");
            else if (occlusionProbes.Length < sourceStart + count)
                throw new ArgumentOutOfRangeException("The specified source start index or count is out of the range.");

            Internal_CopyProbeOcclusionArrayFrom(this, occlusionProbes, sourceStart, destStart, count);
        }
    }
}
