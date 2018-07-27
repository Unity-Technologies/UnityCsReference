// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using PassType = UnityEngine.Rendering.PassType;
using SphericalHarmonicsL2 = UnityEngine.Rendering.SphericalHarmonicsL2;
using uei = UnityEngine.Internal;

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
        public void SetFloat(int nameID, float value)           { SetFloatImpl(nameID, value); }
        public void SetInt(string name, int value)              { SetFloatImpl(Shader.PropertyToID(name), (float)value); }
        public void SetInt(int nameID, int value)               { SetFloatImpl(nameID, (float)value); }
        public void SetVector(string name, Vector4 value)       { SetVectorImpl(Shader.PropertyToID(name), value); }
        public void SetVector(int nameID, Vector4 value)        { SetVectorImpl(nameID, value); }
        public void SetColor(string name, Color value)          { SetColorImpl(Shader.PropertyToID(name), value); }
        public void SetColor(int nameID, Color value)           { SetColorImpl(nameID, value); }
        public void SetMatrix(string name, Matrix4x4 value)     { SetMatrixImpl(Shader.PropertyToID(name), value); }
        public void SetMatrix(int nameID, Matrix4x4 value)      { SetMatrixImpl(nameID, value); }
        public void SetBuffer(string name, ComputeBuffer value) { SetBufferImpl(Shader.PropertyToID(name), value); }
        public void SetBuffer(int nameID, ComputeBuffer value)  { SetBufferImpl(nameID, value); }
        public void SetTexture(string name, Texture value)      { SetTextureImpl(Shader.PropertyToID(name), value); }
        public void SetTexture(int nameID, Texture value)       { SetTextureImpl(nameID, value); }

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

        public float     GetFloat(string name)      { return GetFloatImpl(Shader.PropertyToID(name)); }
        public float     GetFloat(int nameID)       { return GetFloatImpl(nameID); }
        public int       GetInt(string name)        { return (int)GetFloatImpl(Shader.PropertyToID(name)); }
        public int       GetInt(int nameID)         { return (int)GetFloatImpl(nameID); }
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
        public static void SetGlobalBuffer(string name, ComputeBuffer value)    { SetGlobalBufferImpl(Shader.PropertyToID(name), value); }
        public static void SetGlobalBuffer(int nameID, ComputeBuffer value)     { SetGlobalBufferImpl(nameID, value); }

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
        public void SetFloat(int nameID, float value)           { SetFloatImpl(nameID, value); }
        public void SetInt(string name, int value)              { SetFloatImpl(Shader.PropertyToID(name), (float)value); }
        public void SetInt(int nameID, int value)               { SetFloatImpl(nameID, (float)value); }
        public void SetColor(string name, Color value)          { SetColorImpl(Shader.PropertyToID(name), value); }
        public void SetColor(int nameID, Color value)           { SetColorImpl(nameID, value); }
        public void SetVector(string name, Vector4 value)       { SetColorImpl(Shader.PropertyToID(name), (Color)value); }
        public void SetVector(int nameID, Vector4 value)        { SetColorImpl(nameID, (Color)value); }
        public void SetMatrix(string name, Matrix4x4 value)     { SetMatrixImpl(Shader.PropertyToID(name), value); }
        public void SetMatrix(int nameID, Matrix4x4 value)      { SetMatrixImpl(nameID, value); }
        public void SetTexture(string name, Texture value)      { SetTextureImpl(Shader.PropertyToID(name), value); }
        public void SetTexture(int nameID, Texture value)       { SetTextureImpl(nameID, value); }
        public void SetBuffer(string name, ComputeBuffer value) { SetBufferImpl(Shader.PropertyToID(name), value); }
        public void SetBuffer(int nameID, ComputeBuffer value)  { SetBufferImpl(nameID, value); }

        public void SetFloatArray(string name, List<float> values)  { SetFloatArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetFloatArray(int nameID,    List<float> values) { SetFloatArray(nameID, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetFloatArray(string name, float[] values)      { SetFloatArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetFloatArray(int nameID,    float[] values)    { SetFloatArray(nameID, values, values.Length); }

        public void SetColorArray(string name, List<Color> values)  { SetColorArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetColorArray(int nameID,    List<Color> values) { SetColorArray(nameID, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetColorArray(string name, Color[] values)      { SetColorArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetColorArray(int nameID,    Color[] values)    { SetColorArray(nameID, values, values.Length); }

        public void SetVectorArray(string name, List<Vector4> values)   { SetVectorArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetVectorArray(int nameID,    List<Vector4> values) { SetVectorArray(nameID, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetVectorArray(string name, Vector4[] values)       { SetVectorArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetVectorArray(int nameID,    Vector4[] values)     { SetVectorArray(nameID, values, values.Length); }

        public void SetMatrixArray(string name, List<Matrix4x4> values)   { SetMatrixArray(Shader.PropertyToID(name), NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetMatrixArray(int nameID,    List<Matrix4x4> values) { SetMatrixArray(nameID, NoAllocHelpers.ExtractArrayFromListT(values), values.Count); }
        public void SetMatrixArray(string name, Matrix4x4[] values)       { SetMatrixArray(Shader.PropertyToID(name), values, values.Length); }
        public void SetMatrixArray(int nameID,    Matrix4x4[] values)     { SetMatrixArray(nameID, values, values.Length); }

        public float     GetFloat(string name)   { return GetFloatImpl(Shader.PropertyToID(name)); }
        public float     GetFloat(int nameID)    { return GetFloatImpl(nameID); }
        public int       GetInt(string name)     { return (int)GetFloatImpl(Shader.PropertyToID(name)); }
        public int       GetInt(int nameID)      { return (int)GetFloatImpl(nameID); }
        public Color     GetColor(string name)   { return GetColorImpl(Shader.PropertyToID(name)); }
        public Color     GetColor(int nameID)    { return GetColorImpl(nameID); }
        public Vector4   GetVector(string name)  { return (Vector4)GetColorImpl(Shader.PropertyToID(name)); }
        public Vector4   GetVector(int nameID)   { return (Vector4)GetColorImpl(nameID); }
        public Matrix4x4 GetMatrix(string name)  { return GetMatrixImpl(Shader.PropertyToID(name)); }
        public Matrix4x4 GetMatrix(int nameID)   { return GetMatrixImpl(nameID); }
        public Texture   GetTexture(string name) { return GetTextureImpl(Shader.PropertyToID(name)); }
        public Texture   GetTexture(int nameID)  { return GetTextureImpl(nameID); }

        public float[]      GetFloatArray(string name)  { return GetFloatArray(Shader.PropertyToID(name)); }
        public float[]      GetFloatArray(int nameID)   { return GetFloatArrayCountImpl(nameID) != 0 ? GetFloatArrayImpl(nameID) : null; }
        public Color[]      GetColorArray(string name)  { return GetColorArray(Shader.PropertyToID(name)); }
        public Color[]      GetColorArray(int nameID)   { return GetColorArrayCountImpl(nameID) != 0 ? GetColorArrayImpl(nameID) : null; }
        public Vector4[]    GetVectorArray(string name) { return GetVectorArray(Shader.PropertyToID(name)); }
        public Vector4[]    GetVectorArray(int nameID)  { return GetVectorArrayCountImpl(nameID) != 0 ? GetVectorArrayImpl(nameID) : null; }
        public Matrix4x4[]  GetMatrixArray(string name) { return GetMatrixArray(Shader.PropertyToID(name)); }
        public Matrix4x4[]  GetMatrixArray(int nameID)  { return GetMatrixArrayCountImpl(nameID) != 0 ? GetMatrixArrayImpl(nameID) : null; }

        public void GetFloatArray(string name, List<float> values)      { ExtractFloatArray(Shader.PropertyToID(name), values); }
        public void GetFloatArray(int nameID, List<float> values)       { ExtractFloatArray(nameID, values); }
        public void GetColorArray(string name, List<Color> values)      { ExtractColorArray(Shader.PropertyToID(name), values); }
        public void GetColorArray(int nameID, List<Color> values)       { ExtractColorArray(nameID, values); }
        public void GetVectorArray(string name, List<Vector4> values)   { ExtractVectorArray(Shader.PropertyToID(name), values); }
        public void GetVectorArray(int nameID, List<Vector4> values)    { ExtractVectorArray(nameID, values); }
        public void GetMatrixArray(string name, List<Matrix4x4> values) { ExtractMatrixArray(Shader.PropertyToID(name), values); }
        public void GetMatrixArray(int nameID, List<Matrix4x4> values)  { ExtractMatrixArray(nameID, values); }

        public void SetTextureOffset(string name, Vector2 value) { SetTextureOffsetImpl(Shader.PropertyToID(name), value); }
        public void SetTextureOffset(int nameID, Vector2 value)  { SetTextureOffsetImpl(nameID, value); }
        public void SetTextureScale(string name, Vector2 value)  { SetTextureScaleImpl(Shader.PropertyToID(name), value); }
        public void SetTextureScale(int nameID, Vector2 value)   { SetTextureScaleImpl(nameID, value); }

        public Vector2 GetTextureOffset(string name) { return GetTextureOffset(Shader.PropertyToID(name)); }
        public Vector2 GetTextureOffset(int nameID)  { Vector4 st = GetTextureScaleAndOffsetImpl(nameID); return new Vector2(st.z, st.w); }
        public Vector2 GetTextureScale(string name)  { return GetTextureScale(Shader.PropertyToID(name)); }
        public Vector2 GetTextureScale(int nameID)   { Vector4 st = GetTextureScaleAndOffsetImpl(nameID); return new Vector2(st.x, st.y); }
    }
}


//
// ShaderVariantCollection
//


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
// ComputeShader
//

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

        public void SetTextureFromGlobal(int kernelIndex, string name, string globalTextureName)
        {
            SetTextureFromGlobal(kernelIndex, Shader.PropertyToID(name), Shader.PropertyToID(globalTextureName));
        }

        public void SetBuffer(int kernelIndex, string name, ComputeBuffer buffer)
        {
            SetBuffer(kernelIndex, Shader.PropertyToID(name), buffer);
        }

        public void DispatchIndirect(int kernelIndex, ComputeBuffer argsBuffer, [uei.DefaultValue("0")] uint argsOffset)
        {
            if (argsBuffer == null) throw new ArgumentNullException("argsBuffer");
            if (argsBuffer.m_Ptr == IntPtr.Zero) throw new System.ObjectDisposedException("argsBuffer");

            Internal_DispatchIndirect(kernelIndex, argsBuffer, argsOffset);
        }

        [uei.ExcludeFromDocs]
        public void DispatchIndirect(int kernelIndex, ComputeBuffer argsBuffer)
        {
            DispatchIndirect(kernelIndex, argsBuffer, 0);
        }
    }
}
