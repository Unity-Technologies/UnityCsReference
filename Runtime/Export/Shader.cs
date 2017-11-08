// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using PassType = UnityEngine.Rendering.PassType;

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
        internal void SetValue<T>(int name, T value)    { SetValueImpl(name, value, typeof(T)); }
        internal void SetValue<T>(string name, T value) { SetValueImpl(Shader.PropertyToID(name), value, typeof(T)); }

        internal T    GetValue<T>(int name)     { return (T)GetValueImpl(name, typeof(T)); }
        internal T    GetValue<T>(string name)  { return (T)GetValueImpl(Shader.PropertyToID(name), typeof(T)); }

        internal void SetValueArray<T>(int name, List<T> values)    { SetValueArrayImpl(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count, typeof(T)); }
        internal void SetValueArray<T>(string name, List<T> values) { SetValueArray<T>(Shader.PropertyToID(name), values); }
        internal void SetValueArray<T>(int name, T[] values)        { SetValueArrayImpl(name, values, values.Length, typeof(T)); }
        internal void SetValueArray<T>(string name, T[] values)     { SetValueArray<T>(Shader.PropertyToID(name), values); }

        internal int  GetValueArrayCount<T>(int name)   { return GetValueArrayCountImpl(name, typeof(T)); }

        // currently we do not support returning null from native if return type is array
        internal T[] GetValueArray<T>(int name)     { return GetValueArrayCount<T>(name) != 0 ? (T[])GetValueArrayImpl(name, typeof(T)) : null; }
        internal T[] GetValueArray<T>(string name)  { return GetValueArray<T>(Shader.PropertyToID(name)); }

        internal void ExtractValueArray<T>(int name, List<T> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetValueArrayCount<T>(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractValueArrayImpl(name, NoAllocHelpers.ExtractArrayFromList(values), typeof(T));
            }
        }

        internal void ExtractValueArray<T>(string name, List<T> values) { ExtractValueArray<T>(Shader.PropertyToID(name), values); }
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

        public void SetFloat(string name, float value)          { SetValue(name, value); }
        public void SetFloat(int name, float value)             { SetValue(name, value); }
        public void SetVector(string name, Vector4 value)       { SetValue(name, value); }
        public void SetVector(int name, Vector4 value)          { SetValue(name, value); }
        public void SetColor(string name, Color value)          { SetValue(name, value); }
        public void SetColor(int name, Color value)             { SetValue(name, value); }
        public void SetMatrix(string name, Matrix4x4 value)     { SetValue(name, value); }
        public void SetMatrix(int name, Matrix4x4 value)        { SetValue(name, value); }
        public void SetBuffer(string name, ComputeBuffer value) { SetValue(name, value); }
        public void SetBuffer(int name, ComputeBuffer value)    { SetValue(name, value); }
        public void SetTexture(string name, Texture value)      { SetValue(name, value); }
        public void SetTexture(int name, Texture value)         { SetValue(name, value); }

        public void SetFloatArray(string name, List<float> values)  { SetValueArray(name, values); }
        public void SetFloatArray(int name,    List<float> values)  { SetValueArray(name, values); }
        public void SetFloatArray(string name, float[] values)      { SetValueArray(name, values); }
        public void SetFloatArray(int name,    float[] values)      { SetValueArray(name, values); }

        public void SetVectorArray(string name, List<Vector4> values)   { SetValueArray(name, values); }
        public void SetVectorArray(int name,    List<Vector4> values)   { SetValueArray(name, values); }
        public void SetVectorArray(string name, Vector4[] values)       { SetValueArray(name, values); }
        public void SetVectorArray(int name,    Vector4[] values)       { SetValueArray(name, values); }

        public void SetMatrixArray(string name, List<Matrix4x4> values) { SetValueArray(name, values); }
        public void SetMatrixArray(int name,    List<Matrix4x4> values) { SetValueArray(name, values); }
        public void SetMatrixArray(string name, Matrix4x4[] values)     { SetValueArray(name, values); }
        public void SetMatrixArray(int name,    Matrix4x4[] values)     { SetValueArray(name, values); }

        public float     GetFloat(string name)      { return GetValue<float>(name); }
        public float     GetFloat(int name)         { return GetValue<float>(name); }
        public Vector4   GetVector(string name)     { return GetValue<Vector4>(name); }
        public Vector4   GetVector(int name)        { return GetValue<Vector4>(name); }
        public Color     GetColor(string name)      { return GetValue<Color>(name); }
        public Color     GetColor(int name)         { return GetValue<Color>(name); }
        public Matrix4x4 GetMatrix(string name)     { return GetValue<Matrix4x4>(name); }
        public Matrix4x4 GetMatrix(int name)        { return GetValue<Matrix4x4>(name); }
        public Texture   GetTexture(string name)    { return GetValue<Texture>(name); }
        public Texture   GetTexture(int name)       { return GetValue<Texture>(name); }

        public float[]      GetFloatArray(string name)  { return GetValueArray<float>(name); }
        public float[]      GetFloatArray(int name)     { return GetValueArray<float>(name); }
        public Vector4[]    GetVectorArray(string name) { return GetValueArray<Vector4>(name); }
        public Vector4[]    GetVectorArray(int name)    { return GetValueArray<Vector4>(name); }
        public Matrix4x4[]  GetMatrixArray(string name) { return GetValueArray<Matrix4x4>(name); }
        public Matrix4x4[]  GetMatrixArray(int name)    { return GetValueArray<Matrix4x4>(name); }

        public void GetFloatArray(string name, List<float> values)      { ExtractValueArray<float>(name, values); }
        public void GetFloatArray(int name, List<float> values)         { ExtractValueArray<float>(name, values); }
        public void GetVectorArray(string name, List<Vector4> values)   { ExtractValueArray<Vector4>(name, values); }
        public void GetVectorArray(int name, List<Vector4> values)      { ExtractValueArray<Vector4>(name, values); }
        public void GetMatrixArray(string name, List<Matrix4x4> values) { ExtractValueArray<Matrix4x4>(name, values); }
        public void GetMatrixArray(int name, List<Matrix4x4> values)    { ExtractValueArray<Matrix4x4>(name, values); }
    }
}


//
// Shader
//


namespace UnityEngine
{
    // internal generic methods to have one entry point for using native-scripting glue
    public sealed partial class Shader
    {
        internal static void SetGlobalValue<T>(int name, T value)       { SetGlobalValueImpl(name, value, typeof(T)); }
        internal static void SetGlobalValue<T>(string name, T value)    { SetGlobalValueImpl(Shader.PropertyToID(name), value, typeof(T)); }

        internal static T    GetGlobalValue<T>(int name)    { return (T)GetGlobalValueImpl(name, typeof(T)); }
        internal static T    GetGlobalValue<T>(string name) { return (T)GetGlobalValueImpl(Shader.PropertyToID(name), typeof(T)); }

        internal static void SetGlobalValueArray<T>(int name, List<T> values)    { SetGlobalValueArrayImpl(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count, typeof(T)); }
        internal static void SetGlobalValueArray<T>(string name, List<T> values) { SetGlobalValueArray<T>(Shader.PropertyToID(name), values); }
        internal static void SetGlobalValueArray<T>(int name, T[] values)        { SetGlobalValueArrayImpl(name, values, values.Length, typeof(T)); }
        internal static void SetGlobalValueArray<T>(string name, T[] values)     { SetGlobalValueArray<T>(Shader.PropertyToID(name), values); }

        internal static int  GetGlobalValueArrayCount<T>(int name) { return GetGlobalValueArrayCountImpl(name, typeof(T)); }

        // currently we do not support returning null from native if return type is array
        internal static T[] GetGlobalValueArray<T>(int name)    { return GetGlobalValueArrayCount<T>(name) != 0 ? (T[])GetGlobalValueArrayImpl(name, typeof(T)) : null; }
        internal static T[] GetGlobalValueArray<T>(string name) { return GetGlobalValueArray<T>(Shader.PropertyToID(name)); }

        internal static void ExtractGlobalValueArray<T>(int name, List<T> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetGlobalValueArrayCount<T>(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractGlobalValueArrayImpl(name, NoAllocHelpers.ExtractArrayFromList(values), typeof(T));
            }
        }

        internal static void ExtractGlobalValueArray<T>(string name, List<T> values) { ExtractGlobalValueArray<T>(Shader.PropertyToID(name), values); }
    }

    public sealed partial class Shader
    {
        public static void SetGlobalFloat(string name, float value)             { SetGlobalValue(name, value); }
        public static void SetGlobalFloat(int name, float value)                { SetGlobalValue(name, value); }
        public static void SetGlobalInt(string name, int value)                 { SetGlobalValue(name, value); }
        public static void SetGlobalInt(int name, int value)                    { SetGlobalValue(name, value); }
        public static void SetGlobalVector(string name, Vector4 value)          { SetGlobalValue(name, value); }
        public static void SetGlobalVector(int name, Vector4 value)             { SetGlobalValue(name, value); }
        public static void SetGlobalColor(string name, Color value)             { SetGlobalValue(name, value); }
        public static void SetGlobalColor(int name, Color value)                { SetGlobalValue(name, value); }
        public static void SetGlobalMatrix(string name, Matrix4x4 value)        { SetGlobalValue(name, value); }
        public static void SetGlobalMatrix(int name, Matrix4x4 value)           { SetGlobalValue(name, value); }
        public static void SetGlobalTexture(string name, Texture value)         { SetGlobalValue(name, value); }
        public static void SetGlobalTexture(int name, Texture value)            { SetGlobalValue(name, value); }
        public static void SetGlobalBuffer(string name, ComputeBuffer value)    { SetGlobalValue(name, value); }
        public static void SetGlobalBuffer(int name, ComputeBuffer value)       { SetGlobalValue(name, value); }

        public static void SetGlobalFloatArray(string name, List<float> values) { SetGlobalValueArray(name, values); }
        public static void SetGlobalFloatArray(int name, List<float> values)    { SetGlobalValueArray(name, values); }
        public static void SetGlobalFloatArray(string name, float[] values)     { SetGlobalValueArray(name, values); }
        public static void SetGlobalFloatArray(int name, float[] values)        { SetGlobalValueArray(name, values); }

        public static void SetGlobalVectorArray(string name, List<Vector4> values)  { SetGlobalValueArray(name, values); }
        public static void SetGlobalVectorArray(int name, List<Vector4> values)     { SetGlobalValueArray(name, values); }
        public static void SetGlobalVectorArray(string name, Vector4[] values)      { SetGlobalValueArray(name, values); }
        public static void SetGlobalVectorArray(int name, Vector4[] values)         { SetGlobalValueArray(name, values); }

        public static void SetGlobalMatrixArray(string name, List<Matrix4x4> values) { SetGlobalValueArray(name, values); }
        public static void SetGlobalMatrixArray(int name, List<Matrix4x4> values)   { SetGlobalValueArray(name, values); }
        public static void SetGlobalMatrixArray(string name, Matrix4x4[] values)    { SetGlobalValueArray(name, values); }
        public static void SetGlobalMatrixArray(int name, Matrix4x4[] values)       { SetGlobalValueArray(name, values); }

        public static float       GetGlobalFloat(string name)       { return GetGlobalValue<float>(name); }
        public static float       GetGlobalFloat(int name)          { return GetGlobalValue<float>(name); }
        public static int         GetGlobalInt(string name)         { return GetGlobalValue<int>(name); }
        public static int         GetGlobalInt(int name)            { return GetGlobalValue<int>(name); }
        public static Vector4     GetGlobalVector(string name)      { return GetGlobalValue<Vector4>(name); }
        public static Vector4     GetGlobalVector(int name)         { return GetGlobalValue<Vector4>(name); }
        public static Color       GetGlobalColor(string name)       { return GetGlobalValue<Color>(name); }
        public static Color       GetGlobalColor(int name)          { return GetGlobalValue<Color>(name); }
        public static Matrix4x4   GetGlobalMatrix(string name)      { return GetGlobalValue<Matrix4x4>(name); }
        public static Matrix4x4   GetGlobalMatrix(int name)         { return GetGlobalValue<Matrix4x4>(name); }
        public static Texture     GetGlobalTexture(string name)     { return GetGlobalValue<Texture>(name); }
        public static Texture     GetGlobalTexture(int name)        { return GetGlobalValue<Texture>(name); }

        public static float[]     GetGlobalFloatArray(string name)  { return GetGlobalValueArray<float>(name); }
        public static float[]     GetGlobalFloatArray(int name)     { return GetGlobalValueArray<float>(name); }
        public static Vector4[]   GetGlobalVectorArray(string name) { return GetGlobalValueArray<Vector4>(name); }
        public static Vector4[]   GetGlobalVectorArray(int name)    { return GetGlobalValueArray<Vector4>(name); }
        public static Matrix4x4[] GetGlobalMatrixArray(string name) { return GetGlobalValueArray<Matrix4x4>(name); }
        public static Matrix4x4[] GetGlobalMatrixArray(int name)    { return GetGlobalValueArray<Matrix4x4>(name); }

        public static void GetGlobalFloatArray(string name, List<float> values)      { ExtractGlobalValueArray<float>(name, values); }
        public static void GetGlobalFloatArray(int name, List<float> values)         { ExtractGlobalValueArray<float>(name, values); }
        public static void GetGlobalVectorArray(string name, List<Vector4> values)   { ExtractGlobalValueArray<Vector4>(name, values); }
        public static void GetGlobalVectorArray(int name, List<Vector4> values)      { ExtractGlobalValueArray<Vector4>(name, values); }
        public static void GetGlobalMatrixArray(string name, List<Matrix4x4> values) { ExtractGlobalValueArray<Matrix4x4>(name, values); }
        public static void GetGlobalMatrixArray(int name, List<Matrix4x4> values)    { ExtractGlobalValueArray<Matrix4x4>(name, values); }

        private Shader() {}
    }
}


//
// Material
//


namespace UnityEngine
{
    // internal generic methods to have one entry point for using native-scripting glue
    public partial class Material
    {
        internal void SetValue<T>(int name, T value)       { SetValueImpl(name, value, typeof(T)); }
        internal void SetValue<T>(string name, T value)    { SetValueImpl(Shader.PropertyToID(name), value, typeof(T)); }

        internal T    GetValue<T>(int name)     { return (T)GetValueImpl(name, typeof(T)); }
        internal T    GetValue<T>(string name)  { return (T)GetValueImpl(Shader.PropertyToID(name), typeof(T)); }

        internal void SetValueArray<T>(int name, List<T> values)    { SetValueArrayImpl(name, NoAllocHelpers.ExtractArrayFromListT(values), values.Count, typeof(T)); }
        internal void SetValueArray<T>(string name, List<T> values) { SetValueArray<T>(Shader.PropertyToID(name), values); }
        internal void SetValueArray<T>(int name, T[] values)        { SetValueArrayImpl(name, values, values.Length, typeof(T)); }
        internal void SetValueArray<T>(string name, T[] values)     { SetValueArray<T>(Shader.PropertyToID(name), values); }

        internal int  GetValueArrayCount<T>(int name)   { return GetValueArrayCountImpl(name, typeof(T)); }

        // currently we do not support returning null from native if return type is array
        internal T[] GetValueArray<T>(int name)     { return GetValueArrayCount<T>(name) != 0 ? (T[])GetValueArrayImpl(name, typeof(T)) : null; }
        internal T[] GetValueArray<T>(string name)  { return GetValueArray<T>(Shader.PropertyToID(name)); }

        internal void ExtractValueArray<T>(int name, List<T> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            values.Clear();

            int count = GetValueArrayCount<T>(name);
            if (count > 0)
            {
                NoAllocHelpers.EnsureListElemCount(values, count);
                ExtractValueArrayImpl(name, NoAllocHelpers.ExtractArrayFromList(values), typeof(T));
            }
        }

        internal void ExtractValueArray<T>(string name, List<T> values) { ExtractValueArray<T>(Shader.PropertyToID(name), values); }
    }

    public partial class Material
    {
        public void SetFloat(string name, float value)          { SetValue(name, value); }
        public void SetFloat(int name, float value)             { SetValue(name, value); }
        public void SetInt(string name, int value)              { SetValue(name, value); }
        public void SetInt(int name, int value)                 { SetValue(name, value); }
        public void SetColor(string name, Color value)          { SetValue(name, value); }
        public void SetColor(int name, Color value)             { SetValue(name, value); }
        public void SetVector(string name, Vector4 value)       { SetValue(name, value); }
        public void SetVector(int name, Vector4 value)          { SetValue(name, value); }
        public void SetMatrix(string name, Matrix4x4 value)     { SetValue(name, value); }
        public void SetMatrix(int name, Matrix4x4 value)        { SetValue(name, value); }
        public void SetTexture(string name, Texture value)      { SetValue(name, value); }
        public void SetTexture(int name, Texture value)         { SetValue(name, value); }
        public void SetBuffer(string name, ComputeBuffer value) { SetValue(name, value); }
        public void SetBuffer(int name, ComputeBuffer value)    { SetValue(name, value); }

        public void SetFloatArray(string name, List<float> values)  { SetValueArray(name, values); }
        public void SetFloatArray(int name,    List<float> values)  { SetValueArray(name, values); }
        public void SetFloatArray(string name, float[] values)      { SetValueArray(name, values); }
        public void SetFloatArray(int name,    float[] values)      { SetValueArray(name, values); }

        public void SetColorArray(string name, List<Color> values)  { SetValueArray(name, values); }
        public void SetColorArray(int name,    List<Color> values)  { SetValueArray(name, values); }
        public void SetColorArray(string name, Color[] values)      { SetValueArray(name, values); }
        public void SetColorArray(int name,    Color[] values)      { SetValueArray(name, values); }

        public void SetVectorArray(string name, List<Vector4> values)   { SetValueArray(name, values); }
        public void SetVectorArray(int name,    List<Vector4> values)   { SetValueArray(name, values); }
        public void SetVectorArray(string name, Vector4[] values)       { SetValueArray(name, values); }
        public void SetVectorArray(int name,    Vector4[] values)       { SetValueArray(name, values); }

        public void SetMatrixArray(string name, List<Matrix4x4> values) { SetValueArray(name, values); }
        public void SetMatrixArray(int name,    List<Matrix4x4> values) { SetValueArray(name, values); }
        public void SetMatrixArray(string name, Matrix4x4[] values)     { SetValueArray(name, values); }
        public void SetMatrixArray(int name,    Matrix4x4[] values)     { SetValueArray(name, values); }

        public float     GetFloat(string name)  { return GetValue<float>(name); }
        public float     GetFloat(int name)     { return GetValue<float>(name); }
        public int       GetInt(string name)    { return GetValue<int>(name); }
        public int       GetInt(int name)       { return GetValue<int>(name); }
        public Color     GetColor(string name)  { return GetValue<Color>(name); }
        public Color     GetColor(int name)     { return GetValue<Color>(name); }
        public Vector4   GetVector(string name) { return GetValue<Vector4>(name); }
        public Vector4   GetVector(int name)    { return GetValue<Vector4>(name); }
        public Matrix4x4 GetMatrix(string name) { return GetValue<Matrix4x4>(name); }
        public Matrix4x4 GetMatrix(int name)    { return GetValue<Matrix4x4>(name); }
        public Texture   GetTexture(string name) { return GetValue<Texture>(name); }
        public Texture   GetTexture(int name)   { return GetValue<Texture>(name); }

        public float[]      GetFloatArray(string name)  { return GetValueArray<float>(name); }
        public float[]      GetFloatArray(int name)     { return GetValueArray<float>(name); }
        public Color[]      GetColorArray(string name)  { return GetValueArray<Color>(name); }
        public Color[]      GetColorArray(int name)     { return GetValueArray<Color>(name); }
        public Vector4[]    GetVectorArray(string name) { return GetValueArray<Vector4>(name); }
        public Vector4[]    GetVectorArray(int name)    { return GetValueArray<Vector4>(name); }
        public Matrix4x4[]  GetMatrixArray(string name) { return GetValueArray<Matrix4x4>(name); }
        public Matrix4x4[]  GetMatrixArray(int name)    { return GetValueArray<Matrix4x4>(name); }

        public void GetFloatArray(string name, List<float> values)      { ExtractValueArray<float>(name, values); }
        public void GetFloatArray(int name, List<float> values)         { ExtractValueArray<float>(name, values); }
        public void GetColorArray(string name, List<Color> values)      { ExtractValueArray<Color>(name, values); }
        public void GetColorArray(int name, List<Color> values)         { ExtractValueArray<Color>(name, values); }
        public void GetVectorArray(string name, List<Vector4> values)   { ExtractValueArray<Vector4>(name, values); }
        public void GetVectorArray(int name, List<Vector4> values)      { ExtractValueArray<Vector4>(name, values); }
        public void GetMatrixArray(string name, List<Matrix4x4> values) { ExtractValueArray<Matrix4x4>(name, values); }
        public void GetMatrixArray(int name, List<Matrix4x4> values)    { ExtractValueArray<Matrix4x4>(name, values); }

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
