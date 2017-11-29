// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using PassType = UnityEngine.Rendering.PassType;
using SphericalHarmonicsL2 = UnityEngine.Rendering.SphericalHarmonicsL2;

namespace UnityEngine
{
    public sealed partial class ShaderVariantCollection : Object
    {
        public partial struct ShaderVariant
        {
            public Shader shader;
            public PassType passType;
            public string[] keywords;

            public ShaderVariant(Shader shader, PassType passType, params string[] keywords)
            {
                this.shader = shader; this.passType = passType; this.keywords = keywords;
                string checkMessage = CheckShaderVariant(shader, passType, keywords);
                if (!String.IsNullOrEmpty(checkMessage))
                    throw new ArgumentException(checkMessage);
            }
        }
    }

    public sealed partial class ShaderVariantCollection : Object
    {
        public ShaderVariantCollection() { Internal_Create(this); }

        public bool Add(ShaderVariant variant)      { return AddVariant(variant.shader, variant.passType, variant.keywords); }
        public bool Remove(ShaderVariant variant)   { return RemoveVariant(variant.shader, variant.passType, variant.keywords); }
        public bool Contains(ShaderVariant variant) { return ContainsVariant(variant.shader, variant.passType, variant.keywords); }
    }
}

//
// MaterialPropertyBlock
//


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
    }

    public sealed partial class MaterialPropertyBlock
    {
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

        public void SetFloat(string name, float value)          { SetFloatImpl(Shader.PropertyToID(name), value); }
        public void SetFloat(int name, float value)             { SetFloatImpl(name, value); }
        public void SetVector(string name, Vector4 value)       { SetVectorImpl(Shader.PropertyToID(name), value); }
        public void SetVector(int name, Vector4 value)          { SetVectorImpl(name, value); }
        public void SetColor(string name, Color value)          { SetColorImpl(Shader.PropertyToID(name), value); }
        public void SetColor(int name, Color value)             { SetColorImpl(name, value); }
        public void SetMatrix(string name, Matrix4x4 value)     { SetMatrixImpl(Shader.PropertyToID(name), value); }
        public void SetMatrix(int name, Matrix4x4 value)        { SetMatrixImpl(name, value); }
        public void SetBuffer(string name, ComputeBuffer value) { SetBufferImpl(Shader.PropertyToID(name), value); }
        public void SetBuffer(int name, ComputeBuffer value)    { SetBufferImpl(name, value); }
        public void SetTexture(string name, Texture value)      { SetTextureImpl(Shader.PropertyToID(name), value); }
        public void SetTexture(int name, Texture value)         { SetTextureImpl(name, value); }

        public void SetFloatArray(string name, List<float> values)  { SetFloatArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetFloatArray(int name,    List<float> values)  { SetFloatArray(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetFloatArray(string name, float[] values)      { SetFloatArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetFloatArray(int name,    float[] values)      { SetFloatArray(name, values, values.Length); }

        public void SetVectorArray(string name, List<Vector4> values)   { SetVectorArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetVectorArray(int name,    List<Vector4> values)   { SetVectorArray(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetVectorArray(string name, Vector4[] values)       { SetVectorArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetVectorArray(int name,    Vector4[] values)       { SetVectorArray(name, values, values.Length); }

        public void SetMatrixArray(string name, List<Matrix4x4> values) { SetMatrixArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetMatrixArray(int name,    List<Matrix4x4> values) { SetMatrixArray(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetMatrixArray(string name, Matrix4x4[] values)     { SetMatrixArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetMatrixArray(int name,    Matrix4x4[] values)     { SetMatrixArray(name, values, values.Length); }

        public float     GetFloat(string name)      { return GetFloatImpl(Shader.PropertyToID(name)); }
        public float     GetFloat(int name)         { return GetFloatImpl(name); }
        public Vector4   GetVector(string name)     { return GetVectorImpl(Shader.PropertyToID(name)); }
        public Vector4   GetVector(int name)        { return GetVectorImpl(name); }
        public Color     GetColor(string name)      { return GetColorImpl(Shader.PropertyToID(name)); }
        public Color     GetColor(int name)         { return GetColorImpl(name); }
        public Matrix4x4 GetMatrix(string name)     { return GetMatrixImpl(Shader.PropertyToID(name)); }
        public Matrix4x4 GetMatrix(int name)        { return GetMatrixImpl(name); }
        public Texture   GetTexture(string name)    { return GetTextureImpl(Shader.PropertyToID(name)); }
        public Texture   GetTexture(int name)       { return GetTextureImpl(name); }

        public float[]      GetFloatArray(string name)  { return GetFloatArray(Shader.PropertyToID(name)); }
        public float[]      GetFloatArray(int name)     { return GetFloatArrayCountImpl(name) != 0 ? GetFloatArrayImpl(name) : null; }
        public Vector4[]    GetVectorArray(string name) { return GetVectorArray(Shader.PropertyToID(name)); }
        public Vector4[]    GetVectorArray(int name)    { return GetVectorArrayCountImpl(name) != 0 ? GetVectorArrayImpl(name) : null; }
        public Matrix4x4[]  GetMatrixArray(string name) { return GetMatrixArray(Shader.PropertyToID(name)); }
        public Matrix4x4[]  GetMatrixArray(int name)    { return GetMatrixArrayCountImpl(name) != 0 ? GetMatrixArrayImpl(name) : null; }

        public void GetFloatArray(string name, List<float> values)      { ExtractFloatArray(Shader.PropertyToID(name), values); }
        public void GetFloatArray(int name, List<float> values)         { ExtractFloatArray(name, values); }
        public void GetVectorArray(string name, List<Vector4> values)   { ExtractVectorArray(Shader.PropertyToID(name), values); }
        public void GetVectorArray(int name, List<Vector4> values)      { ExtractVectorArray(name, values); }
        public void GetMatrixArray(string name, List<Matrix4x4> values) { ExtractMatrixArray(Shader.PropertyToID(name), values); }
        public void GetMatrixArray(int name, List<Matrix4x4> values)    { ExtractMatrixArray(name, values); }

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


//
// Shader
//


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
    }

    public sealed partial class Shader
    {
        public static void SetGlobalFloat(string name, float value)             { SetGlobalFloatImpl(Shader.PropertyToID(name), value); }
        public static void SetGlobalFloat(int name, float value)                { SetGlobalFloatImpl(name, value); }
        public static void SetGlobalInt(string name, int value)                 { SetGlobalFloatImpl(Shader.PropertyToID(name), (int)value); }
        public static void SetGlobalInt(int name, int value)                    { SetGlobalFloatImpl(name, (int)value); }
        public static void SetGlobalVector(string name, Vector4 value)          { SetGlobalVectorImpl(Shader.PropertyToID(name), value); }
        public static void SetGlobalVector(int name, Vector4 value)             { SetGlobalVectorImpl(name, value); }
        public static void SetGlobalColor(string name, Color value)             { SetGlobalVectorImpl(Shader.PropertyToID(name), (Vector4)value); }
        public static void SetGlobalColor(int name, Color value)                { SetGlobalVectorImpl(name, (Vector4)value); }
        public static void SetGlobalMatrix(string name, Matrix4x4 value)        { SetGlobalMatrixImpl(Shader.PropertyToID(name), value); }
        public static void SetGlobalMatrix(int name, Matrix4x4 value)           { SetGlobalMatrixImpl(name, value); }
        public static void SetGlobalTexture(string name, Texture value)         { SetGlobalTextureImpl(Shader.PropertyToID(name), value); }
        public static void SetGlobalTexture(int name, Texture value)            { SetGlobalTextureImpl(name, value); }
        public static void SetGlobalBuffer(string name, ComputeBuffer value)    { SetGlobalBufferImpl(Shader.PropertyToID(name), value); }
        public static void SetGlobalBuffer(int name, ComputeBuffer value)       { SetGlobalBufferImpl(name, value); }

        public static void SetGlobalFloatArray(string name, List<float> values) { SetGlobalFloatArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public static void SetGlobalFloatArray(int name, List<float> values)    { SetGlobalFloatArray(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public static void SetGlobalFloatArray(string name, float[] values)     { SetGlobalFloatArray(Shader.PropertyToID(name), values, values.Length); }
        public static void SetGlobalFloatArray(int name, float[] values)        { SetGlobalFloatArray(name, values, values.Length); }

        public static void SetGlobalVectorArray(string name, List<Vector4> values)  { SetGlobalVectorArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public static void SetGlobalVectorArray(int name, List<Vector4> values)     { SetGlobalVectorArray(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public static void SetGlobalVectorArray(string name, Vector4[] values)      { SetGlobalVectorArray(Shader.PropertyToID(name), values, values.Length); }
        public static void SetGlobalVectorArray(int name, Vector4[] values)         { SetGlobalVectorArray(name, values, values.Length); }

        public static void SetGlobalMatrixArray(string name, List<Matrix4x4> values) { SetGlobalMatrixArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public static void SetGlobalMatrixArray(int name, List<Matrix4x4> values)   { SetGlobalMatrixArray(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public static void SetGlobalMatrixArray(string name, Matrix4x4[] values)    { SetGlobalMatrixArray(Shader.PropertyToID(name), values, values.Length); }
        public static void SetGlobalMatrixArray(int name, Matrix4x4[] values)       { SetGlobalMatrixArray(name, values, values.Length); }

        public static float       GetGlobalFloat(string name)       { return GetGlobalFloatImpl(Shader.PropertyToID(name)); }
        public static float       GetGlobalFloat(int name)          { return GetGlobalFloatImpl(name); }
        public static int         GetGlobalInt(string name)         { return (int)GetGlobalFloatImpl(Shader.PropertyToID(name)); }
        public static int         GetGlobalInt(int name)            { return (int)GetGlobalFloatImpl(name); }
        public static Vector4     GetGlobalVector(string name)      { return GetGlobalVectorImpl(Shader.PropertyToID(name)); }
        public static Vector4     GetGlobalVector(int name)         { return GetGlobalVectorImpl(name); }
        public static Color       GetGlobalColor(string name)       { return (Color)GetGlobalVectorImpl(Shader.PropertyToID(name)); }
        public static Color       GetGlobalColor(int name)          { return (Color)GetGlobalVectorImpl(name); }
        public static Matrix4x4   GetGlobalMatrix(string name)      { return GetGlobalMatrixImpl(Shader.PropertyToID(name)); }
        public static Matrix4x4   GetGlobalMatrix(int name)         { return GetGlobalMatrixImpl(name); }
        public static Texture     GetGlobalTexture(string name)     { return GetGlobalTextureImpl(Shader.PropertyToID(name)); }
        public static Texture     GetGlobalTexture(int name)        { return GetGlobalTextureImpl(name); }

        public static float[]     GetGlobalFloatArray(string name)  { return GetGlobalFloatArray(Shader.PropertyToID(name)); }
        public static float[]     GetGlobalFloatArray(int name)     { return GetGlobalFloatArrayCountImpl(name) != 0 ? GetGlobalFloatArrayImpl(name) : null; }
        public static Vector4[]   GetGlobalVectorArray(string name) { return GetGlobalVectorArray(Shader.PropertyToID(name)); }
        public static Vector4[]   GetGlobalVectorArray(int name)    { return GetGlobalVectorArrayCountImpl(name) != 0 ? GetGlobalVectorArrayImpl(name) : null; }
        public static Matrix4x4[] GetGlobalMatrixArray(string name) { return GetGlobalMatrixArray(Shader.PropertyToID(name)); }
        public static Matrix4x4[] GetGlobalMatrixArray(int name)    { return GetGlobalMatrixArrayCountImpl(name) != 0 ? GetGlobalMatrixArrayImpl(name) : null; }

        public static void GetGlobalFloatArray(string name, List<float> values)      { ExtractGlobalFloatArray(Shader.PropertyToID(name), values); }
        public static void GetGlobalFloatArray(int name, List<float> values)         { ExtractGlobalFloatArray(name, values); }
        public static void GetGlobalVectorArray(string name, List<Vector4> values)   { ExtractGlobalVectorArray(Shader.PropertyToID(name), values); }
        public static void GetGlobalVectorArray(int name, List<Vector4> values)      { ExtractGlobalVectorArray(name, values); }
        public static void GetGlobalMatrixArray(string name, List<Matrix4x4> values) { ExtractGlobalMatrixArray(Shader.PropertyToID(name), values); }
        public static void GetGlobalMatrixArray(int name, List<Matrix4x4> values)    { ExtractGlobalMatrixArray(name, values); }

        private Shader() {}
    }
}


//
// Material
//


namespace UnityEngine
{
    public partial class Material
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

        private void SetColorArray(int name, Color[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetColorArrayImpl(name, values, count);
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

        private void ExtractColorArray(int name, List<Color> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetColorArrayCountImpl(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractColorArrayImpl(name, (Color[])NoAllocHelpers.ExtractArrayFromList(values));
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
    }

    public partial class Material
    {
        public void SetFloat(string name, float value)          { SetFloatImpl(Shader.PropertyToID(name), value); }
        public void SetFloat(int name, float value)             { SetFloatImpl(name, value); }
        public void SetInt(string name, int value)              { SetFloatImpl(Shader.PropertyToID(name), (float)value); }
        public void SetInt(int name, int value)                 { SetFloatImpl(name, (float)value); }
        public void SetColor(string name, Color value)          { SetColorImpl(Shader.PropertyToID(name), value); }
        public void SetColor(int name, Color value)             { SetColorImpl(name, value); }
        public void SetVector(string name, Vector4 value)       { SetColorImpl(Shader.PropertyToID(name), (Color)value); }
        public void SetVector(int name, Vector4 value)          { SetColorImpl(name, (Color)value); }
        public void SetMatrix(string name, Matrix4x4 value)     { SetMatrixImpl(Shader.PropertyToID(name), value); }
        public void SetMatrix(int name, Matrix4x4 value)        { SetMatrixImpl(name, value); }
        public void SetTexture(string name, Texture value)      { SetTextureImpl(Shader.PropertyToID(name), value); }
        public void SetTexture(int name, Texture value)         { SetTextureImpl(name, value); }
        public void SetBuffer(string name, ComputeBuffer value) { SetBufferImpl(Shader.PropertyToID(name), value); }
        public void SetBuffer(int name, ComputeBuffer value)    { SetBufferImpl(name, value); }

        public void SetFloatArray(string name, List<float> values)  { SetFloatArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetFloatArray(int name,    List<float> values)  { SetFloatArray(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetFloatArray(string name, float[] values)      { SetFloatArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetFloatArray(int name,    float[] values)      { SetFloatArray(name, values, values.Length); }

        public void SetColorArray(string name, List<Color> values)  { SetColorArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetColorArray(int name,    List<Color> values)  { SetColorArray(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetColorArray(string name, Color[] values)      { SetColorArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetColorArray(int name,    Color[] values)      { SetColorArray(name, values, values.Length); }

        public void SetVectorArray(string name, List<Vector4> values)   { SetVectorArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetVectorArray(int name,    List<Vector4> values)   { SetVectorArray(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetVectorArray(string name, Vector4[] values)       { SetVectorArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetVectorArray(int name,    Vector4[] values)       { SetVectorArray(name, values, values.Length); }

        public void SetMatrixArray(string name, List<Matrix4x4> values) { SetMatrixArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetMatrixArray(int name,    List<Matrix4x4> values) { SetMatrixArray(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetMatrixArray(string name, Matrix4x4[] values)     { SetMatrixArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetMatrixArray(int name,    Matrix4x4[] values)     { SetMatrixArray(name, values, values.Length); }

        public float     GetFloat(string name)  { return GetFloatImpl(Shader.PropertyToID(name)); }
        public float     GetFloat(int name)     { return GetFloatImpl(name); }
        public int       GetInt(string name)    { return (int)GetFloatImpl(Shader.PropertyToID(name)); }
        public int       GetInt(int name)       { return (int)GetFloatImpl(name); }
        public Color     GetColor(string name)  { return GetColorImpl(Shader.PropertyToID(name)); }
        public Color     GetColor(int name)     { return GetColorImpl(name); }
        public Vector4   GetVector(string name) { return (Vector4)GetColorImpl(Shader.PropertyToID(name)); }
        public Vector4   GetVector(int name)    { return (Vector4)GetColorImpl(name); }
        public Matrix4x4 GetMatrix(string name) { return GetMatrixImpl(Shader.PropertyToID(name)); }
        public Matrix4x4 GetMatrix(int name)    { return GetMatrixImpl(name); }
        public Texture   GetTexture(string name) { return GetTextureImpl(Shader.PropertyToID(name)); }
        public Texture   GetTexture(int name)   { return GetTextureImpl(name); }

        public float[]      GetFloatArray(string name)  { return GetFloatArray(Shader.PropertyToID(name)); }
        public float[]      GetFloatArray(int name)     { return GetFloatArrayCountImpl(name) != 0 ? GetFloatArrayImpl(name) : null; }
        public Color[]      GetColorArray(string name)  { return GetColorArray(Shader.PropertyToID(name)); }
        public Color[]      GetColorArray(int name)     { return GetColorArrayCountImpl(name) != 0 ? GetColorArrayImpl(name) : null; }
        public Vector4[]    GetVectorArray(string name) { return GetVectorArray(Shader.PropertyToID(name)); }
        public Vector4[]    GetVectorArray(int name)    { return GetVectorArrayCountImpl(name) != 0 ? GetVectorArrayImpl(name) : null; }
        public Matrix4x4[]  GetMatrixArray(string name) { return GetMatrixArray(Shader.PropertyToID(name)); }
        public Matrix4x4[]  GetMatrixArray(int name)    { return GetMatrixArrayCountImpl(name) != 0 ? GetMatrixArrayImpl(name) : null; }

        public void GetFloatArray(string name, List<float> values)      { ExtractFloatArray(Shader.PropertyToID(name), values); }
        public void GetFloatArray(int name, List<float> values)         { ExtractFloatArray(name, values); }
        public void GetColorArray(string name, List<Color> values)      { ExtractColorArray(Shader.PropertyToID(name), values); }
        public void GetColorArray(int name, List<Color> values)         { ExtractColorArray(name, values); }
        public void GetVectorArray(string name, List<Vector4> values)   { ExtractVectorArray(Shader.PropertyToID(name), values); }
        public void GetVectorArray(int name, List<Vector4> values)      { ExtractVectorArray(name, values); }
        public void GetMatrixArray(string name, List<Matrix4x4> values) { ExtractMatrixArray(Shader.PropertyToID(name), values); }
        public void GetMatrixArray(int name, List<Matrix4x4> values)    { ExtractMatrixArray(name, values); }

        public void SetTextureOffset(string name, Vector2 value) { SetTextureOffsetImpl(Shader.PropertyToID(name), value); }
        public void SetTextureOffset(int name, Vector2 value)    { SetTextureOffsetImpl(name, value); }
        public void SetTextureScale(string name, Vector2 value)  { SetTextureScaleImpl(Shader.PropertyToID(name), value); }
        public void SetTextureScale(int name, Vector2 value)     { SetTextureScaleImpl(name, value); }

        public Vector2 GetTextureOffset(string name) { return GetTextureOffset(Shader.PropertyToID(name)); }
        public Vector2 GetTextureOffset(int name)    { Vector4 st = GetTextureScaleAndOffsetImpl(name); return new Vector2(st.z, st.w); }
        public Vector2 GetTextureScale(string name)  { return GetTextureScale(Shader.PropertyToID(name)); }
        public Vector2 GetTextureScale(int name)     { Vector4 st = GetTextureScaleAndOffsetImpl(name); return new Vector2(st.x, st.y); }
    }
}
