// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using uei = UnityEngine.Internal;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequiredByNativeCode] // Used by IMGUI (even on empty projects, it draws development console & watermarks)
    public sealed partial class Mesh : Object
    {
        internal static VertexAttribute GetUVChannel(int uvIndex)
        {
            if (uvIndex < 0 || uvIndex > 7)
                throw new ArgumentException("GetUVChannel called for bad uvIndex", "uvIndex");

            return (VertexAttribute)((int)VertexAttribute.TexCoord0 + uvIndex);
        }

        internal static int DefaultDimensionForChannel(VertexAttribute channel)
        {
            if (channel == VertexAttribute.Position || channel == VertexAttribute.Normal)
                return 3;
            else if (channel >= VertexAttribute.TexCoord0 && channel <= VertexAttribute.TexCoord7)
                return 2;
            else if (channel == VertexAttribute.Tangent || channel == VertexAttribute.Color)
                return 4;

            throw new ArgumentException("DefaultDimensionForChannel called for bad channel", "channel");
        }

        private T[] GetAllocArrayFromChannel<T>(VertexAttribute channel, VertexAttributeFormat format, int dim)
        {
            if (canAccess)
            {
                if (HasVertexAttribute(channel))
                    return (T[])GetAllocArrayFromChannelImpl(channel, format, dim);
            }
            else
            {
                PrintErrorCantAccessChannel(channel);
            }
            return new T[0];
        }

        private T[] GetAllocArrayFromChannel<T>(VertexAttribute channel)
        {
            return GetAllocArrayFromChannel<T>(channel, VertexAttributeFormat.Float32, DefaultDimensionForChannel(channel));
        }

        private void SetSizedArrayForChannel(VertexAttribute channel, VertexAttributeFormat format, int dim, System.Array values, int valuesArrayLength, int valuesStart, int valuesCount, UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            if (canAccess)
            {
                if (valuesStart < 0)
                    throw new ArgumentOutOfRangeException(nameof(valuesStart), valuesStart, "Mesh data array start index can't be negative.");
                if (valuesCount < 0)
                    throw new ArgumentOutOfRangeException(nameof(valuesCount), valuesCount, "Mesh data array length can't be negative.");
                if (valuesStart >= valuesArrayLength && valuesCount != 0)
                    throw new ArgumentOutOfRangeException(nameof(valuesStart), valuesStart, "Mesh data array start is outside of array size.");
                if (valuesStart + valuesCount > valuesArrayLength)
                    throw new ArgumentOutOfRangeException(nameof(valuesCount), valuesStart + valuesCount, "Mesh data array start+count is outside of array size.");
                if (values == null)
                    valuesStart = 0;
                SetArrayForChannelImpl(channel, format, dim, values, valuesArrayLength, valuesStart, valuesCount, flags);
            }
            else
                PrintErrorCantAccessChannel(channel);
        }

        private void SetSizedNativeArrayForChannel(VertexAttribute channel, VertexAttributeFormat format, int dim, IntPtr values, int valuesArrayLength, int valuesStart, int valuesCount, UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            if (canAccess)
            {
                if (valuesStart < 0)
                    throw new ArgumentOutOfRangeException(nameof(valuesStart), valuesStart, "Mesh data array start index can't be negative.");
                if (valuesCount < 0)
                    throw new ArgumentOutOfRangeException(nameof(valuesCount), valuesCount, "Mesh data array length can't be negative.");
                if (valuesStart >= valuesArrayLength && valuesCount != 0)
                    throw new ArgumentOutOfRangeException(nameof(valuesStart), valuesStart, "Mesh data array start is outside of array size.");
                if (valuesStart + valuesCount > valuesArrayLength)
                    throw new ArgumentOutOfRangeException(nameof(valuesCount), valuesStart + valuesCount, "Mesh data array start+count is outside of array size.");
                SetNativeArrayForChannelImpl(channel, format, dim, values, valuesArrayLength, valuesStart, valuesCount, flags);
            }
            else
                PrintErrorCantAccessChannel(channel);
        }

        private void SetArrayForChannel<T>(VertexAttribute channel, VertexAttributeFormat format, int dim, T[] values, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default)
        {
            var len = NoAllocHelpers.SafeLength(values);
            SetSizedArrayForChannel(channel, format, dim, values, len, 0, len, flags);
        }

        private void SetArrayForChannel<T>(VertexAttribute channel, T[] values, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default)
        {
            var len = NoAllocHelpers.SafeLength(values);
            SetSizedArrayForChannel(channel, VertexAttributeFormat.Float32, DefaultDimensionForChannel(channel), values, len, 0, len, flags);
        }

        private void SetListForChannel<T>(VertexAttribute channel, VertexAttributeFormat format, int dim, List<T> values, int start, int length, UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetSizedArrayForChannel(channel, format, dim, NoAllocHelpers.ExtractArrayFromList(values), NoAllocHelpers.SafeLength(values), start, length, flags);
        }

        private void SetListForChannel<T>(VertexAttribute channel, List<T> values, int start, int length, UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetSizedArrayForChannel(channel, VertexAttributeFormat.Float32, DefaultDimensionForChannel(channel), NoAllocHelpers.ExtractArrayFromList(values), NoAllocHelpers.SafeLength(values), start, length, flags);
        }

        private void GetListForChannel<T>(List<T> buffer, int capacity, VertexAttribute channel, int dim)
        {
            GetListForChannel(buffer, capacity, channel, dim, VertexAttributeFormat.Float32);
        }

        private void GetListForChannel<T>(List<T> buffer, int capacity, VertexAttribute channel, int dim, VertexAttributeFormat channelType)
        {
            buffer.Clear();

            if (!canAccess)
            {
                PrintErrorCantAccessChannel(channel);
                return;
            }

            if (!HasVertexAttribute(channel))
                return;

            NoAllocHelpers.EnsureListElemCount(buffer, capacity);
            GetArrayFromChannelImpl(channel, channelType, dim, NoAllocHelpers.ExtractArrayFromList(buffer));
        }

        public Vector3[] vertices
        {
            get { return GetAllocArrayFromChannel<Vector3>(VertexAttribute.Position); }
            set { SetArrayForChannel(VertexAttribute.Position, value, UnityEngine.Rendering.MeshUpdateFlags.Default); }
        }
        public Vector3[] normals
        {
            get { return GetAllocArrayFromChannel<Vector3>(VertexAttribute.Normal); }
            set { SetArrayForChannel(VertexAttribute.Normal, value, UnityEngine.Rendering.MeshUpdateFlags.Default); }
        }
        public Vector4[] tangents
        {
            get { return GetAllocArrayFromChannel<Vector4>(VertexAttribute.Tangent); }
            set { SetArrayForChannel(VertexAttribute.Tangent, value, UnityEngine.Rendering.MeshUpdateFlags.Default); }
        }
        public Vector2[] uv
        {
            get { return GetAllocArrayFromChannel<Vector2>(VertexAttribute.TexCoord0); }
            set { SetArrayForChannel(VertexAttribute.TexCoord0, value, UnityEngine.Rendering.MeshUpdateFlags.Default); }
        }
        public Vector2[] uv2
        {
            get { return GetAllocArrayFromChannel<Vector2>(VertexAttribute.TexCoord1); }
            set { SetArrayForChannel(VertexAttribute.TexCoord1, value); }
        }
        public Vector2[] uv3
        {
            get { return GetAllocArrayFromChannel<Vector2>(VertexAttribute.TexCoord2); }
            set { SetArrayForChannel(VertexAttribute.TexCoord2, value); }
        }
        public Vector2[] uv4
        {
            get { return GetAllocArrayFromChannel<Vector2>(VertexAttribute.TexCoord3); }
            set { SetArrayForChannel(VertexAttribute.TexCoord3, value); }
        }
        public Vector2[] uv5
        {
            get { return GetAllocArrayFromChannel<Vector2>(VertexAttribute.TexCoord4); }
            set { SetArrayForChannel(VertexAttribute.TexCoord4, value); }
        }
        public Vector2[] uv6
        {
            get { return GetAllocArrayFromChannel<Vector2>(VertexAttribute.TexCoord5); }
            set { SetArrayForChannel(VertexAttribute.TexCoord5, value); }
        }
        public Vector2[] uv7
        {
            get { return GetAllocArrayFromChannel<Vector2>(VertexAttribute.TexCoord6); }
            set { SetArrayForChannel(VertexAttribute.TexCoord6, value); }
        }
        public Vector2[] uv8
        {
            get { return GetAllocArrayFromChannel<Vector2>(VertexAttribute.TexCoord7); }
            set { SetArrayForChannel(VertexAttribute.TexCoord7, value); }
        }
        public Color[] colors
        {
            get { return GetAllocArrayFromChannel<Color>(VertexAttribute.Color); }
            set { SetArrayForChannel(VertexAttribute.Color, value); }
        }
        public Color32[] colors32
        {
            get { return GetAllocArrayFromChannel<Color32>(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4); }
            set { SetArrayForChannel(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, value); }
        }

        public void GetVertices(List<Vector3> vertices)
        {
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices), "The result vertices list cannot be null.");

            GetListForChannel(vertices, vertexCount, VertexAttribute.Position, DefaultDimensionForChannel(VertexAttribute.Position));
        }

        public void SetVertices(List<Vector3> inVertices)
        {
            SetVertices(inVertices, 0, NoAllocHelpers.SafeLength(inVertices));
        }

        [uei.ExcludeFromDocs] public void SetVertices(List<Vector3> inVertices, int start, int length)
        {
            SetVertices(inVertices, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetVertices(List<Vector3> inVertices, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetListForChannel(VertexAttribute.Position, inVertices, start, length, flags);
        }

        public void SetVertices(Vector3[] inVertices)
        {
            SetVertices(inVertices, 0, NoAllocHelpers.SafeLength(inVertices));
        }

        [uei.ExcludeFromDocs] public void SetVertices(Vector3[] inVertices, int start, int length)
        {
            SetVertices(inVertices, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetVertices(Vector3[] inVertices, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetSizedArrayForChannel(VertexAttribute.Position, VertexAttributeFormat.Float32, DefaultDimensionForChannel(VertexAttribute.Position), inVertices, NoAllocHelpers.SafeLength(inVertices), start, length, flags);
        }

        public void SetVertices<T>(NativeArray<T> inVertices) where T : struct
        {
            SetVertices(inVertices, 0, inVertices.Length);
        }

        [uei.ExcludeFromDocs] public unsafe void SetVertices<T>(NativeArray<T> inVertices, int start, int length) where T : struct
        {
            SetVertices(inVertices, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public unsafe void SetVertices<T>(NativeArray<T> inVertices, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags) where T : struct
        {
            if (UnsafeUtility.SizeOf<T>() != 12)
                throw new ArgumentException($"{nameof(SetVertices)} with NativeArray should use struct type that is 12 bytes (3x float) in size");
            SetSizedNativeArrayForChannel(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, (IntPtr)inVertices.GetUnsafeReadOnlyPtr(), inVertices.Length, start, length, flags);
        }

        public void GetNormals(List<Vector3> normals)
        {
            if (normals == null)
                throw new ArgumentNullException(nameof(normals), "The result normals list cannot be null.");

            GetListForChannel(normals, vertexCount, VertexAttribute.Normal, DefaultDimensionForChannel(VertexAttribute.Normal));
        }

        public void SetNormals(List<Vector3> inNormals)
        {
            SetNormals(inNormals, 0, NoAllocHelpers.SafeLength(inNormals));
        }

        [uei.ExcludeFromDocs] public void SetNormals(List<Vector3> inNormals, int start, int length)
        {
            SetNormals(inNormals, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetNormals(List<Vector3> inNormals, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetListForChannel(VertexAttribute.Normal, inNormals, start, length, flags);
        }

        public void SetNormals(Vector3[] inNormals)
        {
            SetNormals(inNormals, 0, NoAllocHelpers.SafeLength(inNormals));
        }

        [uei.ExcludeFromDocs] public void SetNormals(Vector3[] inNormals, int start, int length)
        {
            SetNormals(inNormals, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetNormals(Vector3[] inNormals, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetSizedArrayForChannel(VertexAttribute.Normal, VertexAttributeFormat.Float32, DefaultDimensionForChannel(VertexAttribute.Normal), inNormals, NoAllocHelpers.SafeLength(inNormals), start, length, flags);
        }

        public void SetNormals<T>(NativeArray<T> inNormals) where T : struct
        {
            SetNormals(inNormals, 0, inNormals.Length);
        }

        [uei.ExcludeFromDocs] public unsafe void SetNormals<T>(NativeArray<T> inNormals, int start, int length) where T : struct
        {
            SetNormals(inNormals, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public unsafe void SetNormals<T>(NativeArray<T> inNormals, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags) where T : struct
        {
            if (UnsafeUtility.SizeOf<T>() != 12)
                throw new ArgumentException($"{nameof(SetNormals)} with NativeArray should use struct type that is 12 bytes (3x float) in size");
            SetSizedNativeArrayForChannel(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, (IntPtr)inNormals.GetUnsafeReadOnlyPtr(), inNormals.Length, start, length, flags);
        }

        public void GetTangents(List<Vector4> tangents)
        {
            if (tangents == null)
                throw new ArgumentNullException(nameof(tangents), "The result tangents list cannot be null.");

            GetListForChannel(tangents, vertexCount, VertexAttribute.Tangent, DefaultDimensionForChannel(VertexAttribute.Tangent));
        }

        public void SetTangents(List<Vector4> inTangents)
        {
            SetTangents(inTangents, 0, NoAllocHelpers.SafeLength(inTangents));
        }

        [uei.ExcludeFromDocs] public void SetTangents(List<Vector4> inTangents, int start, int length)
        {
            SetTangents(inTangents, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetTangents(List<Vector4> inTangents, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetListForChannel(VertexAttribute.Tangent, inTangents, start, length, flags);
        }

        public void SetTangents(Vector4[] inTangents)
        {
            SetTangents(inTangents, 0, NoAllocHelpers.SafeLength(inTangents));
        }

        [uei.ExcludeFromDocs] public void SetTangents(Vector4[] inTangents, int start, int length)
        {
            SetTangents(inTangents, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetTangents(Vector4[] inTangents, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetSizedArrayForChannel(VertexAttribute.Tangent, VertexAttributeFormat.Float32, DefaultDimensionForChannel(VertexAttribute.Tangent), inTangents, NoAllocHelpers.SafeLength(inTangents), start, length, flags);
        }

        public void SetTangents<T>(NativeArray<T> inTangents) where T : struct
        {
            SetTangents(inTangents, 0, inTangents.Length);
        }

        [uei.ExcludeFromDocs] public unsafe void SetTangents<T>(NativeArray<T> inTangents, int start, int length) where T : struct
        {
            SetTangents(inTangents, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public unsafe void SetTangents<T>(NativeArray<T> inTangents, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags) where T : struct
        {
            if (UnsafeUtility.SizeOf<T>() != 16)
                throw new ArgumentException($"{nameof(SetTangents)} with NativeArray should use struct type that is 16 bytes (4x float) in size");
            SetSizedNativeArrayForChannel(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4, (IntPtr)inTangents.GetUnsafeReadOnlyPtr(), inTangents.Length, start, length, flags);
        }

        public void GetColors(List<Color> colors)
        {
            if (colors == null)
                throw new ArgumentNullException(nameof(colors), "The result colors list cannot be null.");

            GetListForChannel(colors, vertexCount, VertexAttribute.Color, DefaultDimensionForChannel(VertexAttribute.Color));
        }

        public void SetColors(List<Color> inColors)
        {
            SetColors(inColors, 0, NoAllocHelpers.SafeLength(inColors));
        }

        [uei.ExcludeFromDocs] public void SetColors(List<Color> inColors, int start, int length)
        {
            SetColors(inColors, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetColors(List<Color> inColors, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetListForChannel(VertexAttribute.Color, inColors, start, length, flags);
        }

        public void SetColors(Color[] inColors)
        {
            SetColors(inColors, 0, NoAllocHelpers.SafeLength(inColors));
        }

        [uei.ExcludeFromDocs] public void SetColors(Color[] inColors, int start, int length)
        {
            SetColors(inColors, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetColors(Color[] inColors, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetSizedArrayForChannel(VertexAttribute.Color, VertexAttributeFormat.Float32, DefaultDimensionForChannel(VertexAttribute.Color), inColors, NoAllocHelpers.SafeLength(inColors), start, length, flags);
        }

        public void GetColors(List<Color32> colors)
        {
            if (colors == null)
                throw new ArgumentNullException(nameof(colors), "The result colors list cannot be null.");

            GetListForChannel(colors, vertexCount, VertexAttribute.Color, 4, VertexAttributeFormat.UNorm8);
        }

        public void SetColors(List<Color32> inColors)
        {
            SetColors(inColors, 0, NoAllocHelpers.SafeLength(inColors));
        }

        [uei.ExcludeFromDocs] public void SetColors(List<Color32> inColors, int start, int length)
        {
            SetColors(inColors, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetColors(List<Color32> inColors, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetListForChannel(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, inColors, start, length, flags);
        }

        public void SetColors(Color32[] inColors)
        {
            SetColors(inColors, 0, NoAllocHelpers.SafeLength(inColors));
        }

        [uei.ExcludeFromDocs] public void SetColors(Color32[] inColors, int start, int length)
        {
            SetColors(inColors, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetColors(Color32[] inColors, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetSizedArrayForChannel(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, inColors, NoAllocHelpers.SafeLength(inColors), start, length, flags);
        }

        public void SetColors<T>(NativeArray<T> inColors) where T : struct
        {
            SetColors(inColors, 0, inColors.Length);
        }

        [uei.ExcludeFromDocs] public unsafe void SetColors<T>(NativeArray<T> inColors, int start, int length) where T : struct
        {
            SetColors(inColors, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public unsafe void SetColors<T>(NativeArray<T> inColors, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags) where T : struct
        {
            var tSize = UnsafeUtility.SizeOf<T>();
            if (tSize != 16 && tSize != 4)
                throw new ArgumentException($"{nameof(SetColors)} with NativeArray should use struct type that is 16 bytes (4x float) or 4 bytes (4x unorm) in size");
            SetSizedNativeArrayForChannel(VertexAttribute.Color, tSize == 4 ? VertexAttributeFormat.UNorm8 : VertexAttributeFormat.Float32, 4, (IntPtr)inColors.GetUnsafeReadOnlyPtr(), inColors.Length, start, length, flags);
        }

        private void SetUvsImpl<T>(int uvIndex, int dim, List<T> uvs, int start, int length, UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            // before this resulted in error *printed* out deep inside c++ code (coming from assert - useless for end-user)
            // while excpetion would make sense we dont want to add exceptions to exisisting apis
            if (uvIndex < 0 || uvIndex > 7)
            {
                Debug.LogError("The uv index is invalid. Must be in the range 0 to 7.");
                return;
            }
            SetListForChannel(GetUVChannel(uvIndex), VertexAttributeFormat.Float32, dim, uvs, start, length, flags);
        }

        public void SetUVs(int channel, List<Vector2> uvs)
        {
            SetUVs(channel, uvs, 0, NoAllocHelpers.SafeLength(uvs));
        }

        public void SetUVs(int channel, List<Vector3> uvs)
        {
            SetUVs(channel, uvs, 0, NoAllocHelpers.SafeLength(uvs));
        }

        public void SetUVs(int channel, List<Vector4> uvs)
        {
            SetUVs(channel, uvs, 0, NoAllocHelpers.SafeLength(uvs));
        }

        [uei.ExcludeFromDocs] public void SetUVs(int channel, List<Vector2> uvs, int start, int length)
        {
            SetUVs(channel, uvs, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetUVs(int channel, List<Vector2> uvs, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetUvsImpl(channel, 2, uvs, start, length, flags);
        }

        [uei.ExcludeFromDocs] public void SetUVs(int channel, List<Vector3> uvs, int start, int length)
        {
            SetUVs(channel, uvs, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetUVs(int channel, List<Vector3> uvs, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetUvsImpl(channel, 3, uvs, start, length, flags);
        }

        [uei.ExcludeFromDocs] public void SetUVs(int channel, List<Vector4> uvs, int start, int length)
        {
            SetUVs(channel, uvs, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetUVs(int channel, List<Vector4> uvs, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetUvsImpl(channel, 4, uvs, start, length, flags);
        }

        private void SetUvsImpl(int uvIndex, int dim, System.Array uvs, int arrayStart, int arraySize, UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            if (uvIndex < 0 || uvIndex > 7)
                throw new ArgumentOutOfRangeException(nameof(uvIndex), uvIndex, "The uv index is invalid. Must be in the range 0 to 7.");
            SetSizedArrayForChannel(GetUVChannel(uvIndex), VertexAttributeFormat.Float32, dim, uvs, NoAllocHelpers.SafeLength(uvs), arrayStart, arraySize, flags);
        }

        public void SetUVs(int channel, Vector2[] uvs)
        {
            SetUVs(channel, uvs, 0, NoAllocHelpers.SafeLength(uvs));
        }

        public void SetUVs(int channel, Vector3[] uvs)
        {
            SetUVs(channel, uvs, 0, NoAllocHelpers.SafeLength(uvs));
        }

        public void SetUVs(int channel, Vector4[] uvs)
        {
            SetUVs(channel, uvs, 0, NoAllocHelpers.SafeLength(uvs));
        }

        [uei.ExcludeFromDocs] public void SetUVs(int channel, Vector2[] uvs, int start, int length)
        {
            SetUVs(channel, uvs, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetUVs(int channel, Vector2[] uvs, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetUvsImpl(channel, 2, uvs, start, length, flags);
        }

        [uei.ExcludeFromDocs] public void SetUVs(int channel, Vector3[] uvs, int start, int length)
        {
            SetUVs(channel, uvs, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetUVs(int channel, Vector3[] uvs, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetUvsImpl(channel, 3, uvs, start, length, flags);
        }

        [uei.ExcludeFromDocs] public void SetUVs(int channel, Vector4[] uvs, int start, int length)
        {
            SetUVs(channel, uvs, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public void SetUVs(int channel, Vector4[] uvs, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            SetUvsImpl(channel, 4, uvs, start, length, flags);
        }

        public void SetUVs<T>(int channel, NativeArray<T> uvs) where T : struct
        {
            SetUVs(channel, uvs, 0, uvs.Length);
        }

        [uei.ExcludeFromDocs] public unsafe void SetUVs<T>(int channel, NativeArray<T> uvs, int start, int length) where T : struct
        {
            SetUVs(channel, uvs, start, length, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public unsafe void SetUVs<T>(int channel, NativeArray<T> uvs, int start, int length, [DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags) where T : struct
        {
            if (channel < 0 || channel > 7)
                throw new ArgumentOutOfRangeException(nameof(channel), channel, "The uv index is invalid. Must be in the range 0 to 7.");

            var tSize = UnsafeUtility.SizeOf<T>();
            if ((tSize & 3) != 0)
                throw new ArgumentException($"{nameof(SetUVs)} with NativeArray should use struct type that is multiple of 4 bytes in size");
            var dim = tSize / 4;
            if (dim < 1 || dim > 4)
                throw new ArgumentException($"{nameof(SetUVs)} with NativeArray should use struct type that is 1..4 floats in size");
            SetSizedNativeArrayForChannel(GetUVChannel(channel), VertexAttributeFormat.Float32, dim, (IntPtr)uvs.GetUnsafeReadOnlyPtr(), uvs.Length, start, length, flags);
        }

        private void GetUVsImpl<T>(int uvIndex, List<T> uvs, int dim)
        {
            if (uvs == null)
                throw new ArgumentNullException(nameof(uvs), "The result uvs list cannot be null.");
            if (uvIndex < 0 || uvIndex > 7)
                throw new IndexOutOfRangeException("The uv index is invalid. Must be in the range 0 to 7.");

            GetListForChannel(uvs, vertexCount, GetUVChannel(uvIndex), dim);
        }

        public void GetUVs(int channel, List<Vector2> uvs)
        {
            GetUVsImpl(channel, uvs, 2);
        }

        public void GetUVs(int channel, List<Vector3> uvs)
        {
            GetUVsImpl(channel, uvs, 3);
        }

        public void GetUVs(int channel, List<Vector4> uvs)
        {
            GetUVsImpl(channel, uvs, 4);
        }

        public int vertexAttributeCount
        {
            get { return GetVertexAttributeCountImpl(); }
        }
        public UnityEngine.Rendering.VertexAttributeDescriptor[] GetVertexAttributes()
        {
            return (UnityEngine.Rendering.VertexAttributeDescriptor[])GetVertexAttributesAlloc();
        }

        public int GetVertexAttributes(UnityEngine.Rendering.VertexAttributeDescriptor[] attributes)
        {
            return GetVertexAttributesArray(attributes);
        }

        public int GetVertexAttributes(List<UnityEngine.Rendering.VertexAttributeDescriptor> attributes)
        {
            return GetVertexAttributesList(attributes);
        }

        public unsafe void SetVertexBufferData<T>(NativeArray<T> data, int dataStart, int meshBufferStart, int count, int stream = 0, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default) where T : struct
        {
            if (!canAccess)
                throw new InvalidOperationException($"Not allowed to access vertex data on mesh '{name}' (isReadable is false; Read/Write must be enabled in import settings)");
            if (dataStart < 0 || meshBufferStart < 0 || count < 0 || dataStart + count > data.Length)
                throw new ArgumentOutOfRangeException($"Bad start/count arguments (dataStart:{dataStart} meshBufferStart:{meshBufferStart} count:{count})");
            InternalSetVertexBufferData(stream, (IntPtr)data.GetUnsafeReadOnlyPtr(), dataStart, meshBufferStart, count, UnsafeUtility.SizeOf<T>(), flags);
        }

        public void SetVertexBufferData<T>(T[] data, int dataStart, int meshBufferStart, int count, int stream = 0, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default) where T : struct
        {
            if (!canAccess)
                throw new InvalidOperationException($"Not allowed to access vertex data on mesh '{name}' (isReadable is false; Read/Write must be enabled in import settings)");
            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException($"Array passed to {nameof(SetVertexBufferData)} must be blittable.\n{UnsafeUtility.GetReasonForArrayNonBlittable(data)}");
            }
            if (dataStart < 0 || meshBufferStart < 0 || count < 0 || dataStart + count > data.Length)
                throw new ArgumentOutOfRangeException($"Bad start/count arguments (dataStart:{dataStart} meshBufferStart:{meshBufferStart} count:{count})");
            InternalSetVertexBufferDataFromArray(stream, data, dataStart, meshBufferStart, count, UnsafeUtility.SizeOf<T>(), flags);
        }

        public void SetVertexBufferData<T>(List<T> data, int dataStart, int meshBufferStart, int count, int stream = 0, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default) where T : struct
        {
            if (!canAccess)
                throw new InvalidOperationException($"Not allowed to access vertex data on mesh '{name}' (isReadable is false; Read/Write must be enabled in import settings)");
            if (!UnsafeUtility.IsGenericListBlittable<T>())
            {
                throw new ArgumentException($"List<{typeof(T)}> passed to {nameof(SetVertexBufferData)} must be blittable.\n{UnsafeUtility.GetReasonForGenericListNonBlittable<T>()}");
            }
            if (dataStart < 0 || meshBufferStart < 0 || count < 0 || dataStart + count > data.Count)
                throw new ArgumentOutOfRangeException($"Bad start/count arguments (dataStart:{dataStart} meshBufferStart:{meshBufferStart} count:{count})");
            InternalSetVertexBufferDataFromArray(stream, NoAllocHelpers.ExtractArrayFromList(data), dataStart, meshBufferStart, count, UnsafeUtility.SizeOf<T>(), flags);
        }

        public static MeshDataArray AcquireReadOnlyMeshData(Mesh mesh)
        {
            return new MeshDataArray(mesh);
        }

        public static MeshDataArray AcquireReadOnlyMeshData(Mesh[] meshes)
        {
            if (meshes == null)
                throw new ArgumentNullException(nameof(meshes), "Mesh array is null");
            return new MeshDataArray(meshes, meshes.Length);
        }

        public static MeshDataArray AcquireReadOnlyMeshData(List<Mesh> meshes)
        {
            if (meshes == null)
                throw new ArgumentNullException(nameof(meshes), "Mesh list is null");
            return new MeshDataArray(NoAllocHelpers.ExtractArrayFromListT(meshes), meshes.Count);
        }

        public static MeshDataArray AllocateWritableMeshData(int meshCount)
        {
            return new MeshDataArray(meshCount);
        }

        public static void ApplyAndDisposeWritableMeshData(MeshDataArray data, Mesh mesh, MeshUpdateFlags flags = MeshUpdateFlags.Default)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh), "Mesh is null");
            if (data.Length != 1)
                throw new InvalidOperationException($"{nameof(MeshDataArray)} length must be 1 to apply to one mesh, was {data.Length}");
            data.ApplyToMeshAndDispose(mesh, flags);
        }

        public static void ApplyAndDisposeWritableMeshData(MeshDataArray data, Mesh[] meshes, MeshUpdateFlags flags = MeshUpdateFlags.Default)
        {
            if (meshes == null)
                throw new ArgumentNullException(nameof(meshes), "Mesh array is null");
            if (data.Length != meshes.Length)
                throw new InvalidOperationException($"{nameof(MeshDataArray)} length ({data.Length}) must match destination meshes array length ({meshes.Length})");
            data.ApplyToMeshesAndDispose(meshes, flags);
        }

        public static void ApplyAndDisposeWritableMeshData(MeshDataArray data, List<Mesh> meshes, MeshUpdateFlags flags = MeshUpdateFlags.Default)
        {
            if (meshes == null)
                throw new ArgumentNullException(nameof(meshes), "Mesh list is null");
            if (data.Length != meshes.Count)
                throw new InvalidOperationException($"{nameof(MeshDataArray)} length ({data.Length}) must match destination meshes list length ({meshes.Count})");
            data.ApplyToMeshesAndDispose(NoAllocHelpers.ExtractArrayFromListT(meshes), flags);
        }

        //
        // triangles/indices
        //

        private void PrintErrorCantAccessIndices()
        {
            Debug.LogError(String.Format("Not allowed to access triangles/indices on mesh '{0}' (isReadable is false; Read/Write must be enabled in import settings)", name));
        }

        private bool CheckCanAccessSubmesh(int submesh, bool errorAboutTriangles)
        {
            if (!canAccess)
            {
                PrintErrorCantAccessIndices();
                return false;
            }
            if (submesh < 0 || submesh >= subMeshCount)
            {
                Debug.LogError(String.Format("Failed getting {0}. Submesh index is out of bounds.", errorAboutTriangles ? "triangles" : "indices"), this);
                return false;
            }
            return true;
        }

        private bool CheckCanAccessSubmeshTriangles(int submesh)    { return CheckCanAccessSubmesh(submesh, true); }
        private bool CheckCanAccessSubmeshIndices(int submesh)      { return CheckCanAccessSubmesh(submesh, false); }

        public int[] triangles
        {
            get
            {
                if (canAccess)  return GetTrianglesImpl(-1, true);
                else            PrintErrorCantAccessIndices();
                return new int[0];
            }
            set
            {
                if (canAccess)  SetTrianglesImpl(-1, UnityEngine.Rendering.IndexFormat.UInt32, value, NoAllocHelpers.SafeLength(value), 0, NoAllocHelpers.SafeLength(value), true, 0);
                else            PrintErrorCantAccessIndices();
            }
        }

        public int[] GetTriangles(int submesh)
        {
            return GetTriangles(submesh, true);
        }

        public int[] GetTriangles(int submesh, [DefaultValue("true")] bool applyBaseVertex)
        {
            return CheckCanAccessSubmeshTriangles(submesh) ? GetTrianglesImpl(submesh, applyBaseVertex) : new int[0];
        }

        public void GetTriangles(List<int> triangles, int submesh)
        {
            GetTriangles(triangles, submesh, true);
        }

        public void GetTriangles(List<int> triangles, int submesh, [DefaultValue("true")] bool applyBaseVertex)
        {
            if (triangles == null)
                throw new ArgumentNullException(nameof(triangles), "The result triangles list cannot be null.");

            if (submesh < 0 || submesh >= subMeshCount)
                throw new IndexOutOfRangeException("Specified sub mesh is out of range. Must be greater or equal to 0 and less than subMeshCount.");

            NoAllocHelpers.EnsureListElemCount(triangles, 3 * (int)GetTrianglesCountImpl(submesh));
            GetTrianglesNonAllocImpl(NoAllocHelpers.ExtractArrayFromListT(triangles), submesh, applyBaseVertex);
        }

        public void GetTriangles(List<ushort> triangles, int submesh, bool applyBaseVertex = true)
        {
            if (triangles == null)
                throw new ArgumentNullException(nameof(triangles), "The result triangles list cannot be null.");

            if (submesh < 0 || submesh >= subMeshCount)
                throw new IndexOutOfRangeException("Specified sub mesh is out of range. Must be greater or equal to 0 and less than subMeshCount.");

            NoAllocHelpers.EnsureListElemCount(triangles, 3 * (int)GetTrianglesCountImpl(submesh));
            GetTrianglesNonAllocImpl16(NoAllocHelpers.ExtractArrayFromListT(triangles), submesh, applyBaseVertex);
        }

        [uei.ExcludeFromDocs]
        public int[] GetIndices(int submesh)
        {
            return GetIndices(submesh, true);
        }

        public int[] GetIndices(int submesh, [DefaultValue("true")] bool applyBaseVertex)
        {
            return CheckCanAccessSubmeshIndices(submesh) ? GetIndicesImpl(submesh, applyBaseVertex) : new int[0];
        }

        [uei.ExcludeFromDocs]
        public void GetIndices(List<int> indices, int submesh)
        {
            GetIndices(indices, submesh, true);
        }

        public void GetIndices(List<int> indices, int submesh, [DefaultValue("true")] bool applyBaseVertex)
        {
            if (indices == null)
                throw new ArgumentNullException(nameof(indices), "The result indices list cannot be null.");

            if (submesh < 0 || submesh >= subMeshCount)
                throw new IndexOutOfRangeException("Specified sub mesh is out of range. Must be greater or equal to 0 and less than subMeshCount.");

            NoAllocHelpers.EnsureListElemCount(indices, (int)GetIndexCount(submesh));
            GetIndicesNonAllocImpl(NoAllocHelpers.ExtractArrayFromListT(indices), submesh, applyBaseVertex);
        }

        public void GetIndices(List<ushort> indices, int submesh, bool applyBaseVertex = true)
        {
            if (indices == null)
                throw new ArgumentNullException(nameof(indices), "The result indices list cannot be null.");

            if (submesh < 0 || submesh >= subMeshCount)
                throw new IndexOutOfRangeException("Specified sub mesh is out of range. Must be greater or equal to 0 and less than subMeshCount.");

            NoAllocHelpers.EnsureListElemCount(indices, (int)GetIndexCount(submesh));
            GetIndicesNonAllocImpl16(NoAllocHelpers.ExtractArrayFromListT(indices), submesh, applyBaseVertex);
        }

        public unsafe void SetIndexBufferData<T>(NativeArray<T> data, int dataStart, int meshBufferStart, int count, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default) where T : struct
        {
            if (!canAccess)
            {
                PrintErrorCantAccessIndices();
                return;
            }
            if (dataStart < 0 || meshBufferStart < 0 || count < 0 || dataStart + count > data.Length)
                throw new ArgumentOutOfRangeException($"Bad start/count arguments (dataStart:{dataStart} meshBufferStart:{meshBufferStart} count:{count})");
            InternalSetIndexBufferData((IntPtr)data.GetUnsafeReadOnlyPtr(), dataStart, meshBufferStart, count, UnsafeUtility.SizeOf<T>(), flags);
        }

        public unsafe void SetIndexBufferData<T>(T[] data, int dataStart, int meshBufferStart, int count, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default) where T : struct
        {
            if (!canAccess)
            {
                PrintErrorCantAccessIndices();
                return;
            }
            if (!UnsafeUtility.IsArrayBlittable(data))
            {
                throw new ArgumentException($"Array passed to {nameof(SetIndexBufferData)} must be blittable.\n{UnsafeUtility.GetReasonForArrayNonBlittable(data)}");
            }
            if (dataStart < 0 || meshBufferStart < 0 || count < 0 || dataStart + count > data.Length)
                throw new ArgumentOutOfRangeException($"Bad start/count arguments (dataStart:{dataStart} meshBufferStart:{meshBufferStart} count:{count})");
            InternalSetIndexBufferDataFromArray(data, dataStart, meshBufferStart, count, UnsafeUtility.SizeOf<T>(), flags);
        }

        public unsafe void SetIndexBufferData<T>(List<T> data, int dataStart, int meshBufferStart, int count, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default) where T : struct
        {
            if (!canAccess)
            {
                PrintErrorCantAccessIndices();
                return;
            }
            if (!UnsafeUtility.IsGenericListBlittable<T>())
            {
                throw new ArgumentException($"List<{typeof(T)}> passed to {nameof(SetIndexBufferData)} must be blittable.\n{UnsafeUtility.GetReasonForGenericListNonBlittable<T>()}");
            }
            if (dataStart < 0 || meshBufferStart < 0 || count < 0 || dataStart + count > data.Count)
                throw new ArgumentOutOfRangeException($"Bad start/count arguments (dataStart:{dataStart} meshBufferStart:{meshBufferStart} count:{count})");
            InternalSetIndexBufferDataFromArray(NoAllocHelpers.ExtractArrayFromList(data), dataStart, meshBufferStart, count, UnsafeUtility.SizeOf<T>(), flags);
        }

        public UInt32 GetIndexStart(int submesh)
        {
            if (submesh < 0 || submesh >= subMeshCount)
                throw new IndexOutOfRangeException("Specified sub mesh is out of range. Must be greater or equal to 0 and less than subMeshCount.");
            return GetIndexStartImpl(submesh);
        }

        public UInt32 GetIndexCount(int submesh)
        {
            if (submesh < 0 || submesh >= subMeshCount)
                throw new IndexOutOfRangeException("Specified sub mesh is out of range. Must be greater or equal to 0 and less than subMeshCount.");
            return GetIndexCountImpl(submesh);
        }

        public UInt32 GetBaseVertex(int submesh)
        {
            if (submesh < 0 || submesh >= subMeshCount)
                throw new IndexOutOfRangeException("Specified sub mesh is out of range. Must be greater or equal to 0 and less than subMeshCount.");
            return GetBaseVertexImpl(submesh);
        }

        private void CheckIndicesArrayRange(int valuesLength, int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start), start, "Mesh indices array start can't be negative.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "Mesh indices array length can't be negative.");
            if (start >= valuesLength && length != 0)
                throw new ArgumentOutOfRangeException(nameof(start), start, "Mesh indices array start is outside of array size.");
            if (start + length > valuesLength)
                throw new ArgumentOutOfRangeException(nameof(length), start + length, "Mesh indices array start+count is outside of array size.");
        }

        private void SetTrianglesImpl(int submesh, UnityEngine.Rendering.IndexFormat indicesFormat, System.Array triangles, int trianglesArrayLength, int start, int length, bool calculateBounds, int baseVertex)
        {
            CheckIndicesArrayRange(trianglesArrayLength, start, length);
            SetIndicesImpl(submesh, MeshTopology.Triangles, indicesFormat, triangles, start, length, calculateBounds, baseVertex);
        }

        // Note: we do have many overloads where seemingly "just use default arg values"
        // would work too. However we can't change that, since it would be an API-breaking
        // change.

        [uei.ExcludeFromDocs]
        public void SetTriangles(int[] triangles, int submesh)
        {
            SetTriangles(triangles, submesh, true, 0);
        }

        [uei.ExcludeFromDocs]
        public void SetTriangles(int[] triangles, int submesh, bool calculateBounds)
        {
            SetTriangles(triangles, submesh, calculateBounds, 0);
        }

        public void SetTriangles(int[] triangles, int submesh, [DefaultValue("true")] bool calculateBounds, [DefaultValue("0")] int baseVertex)
        {
            SetTriangles(triangles, 0, NoAllocHelpers.SafeLength(triangles), submesh, calculateBounds, baseVertex);
        }

        public void SetTriangles(int[] triangles, int trianglesStart, int trianglesLength, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            if (CheckCanAccessSubmeshTriangles(submesh))
                SetTrianglesImpl(submesh, UnityEngine.Rendering.IndexFormat.UInt32, triangles, NoAllocHelpers.SafeLength(triangles), trianglesStart, trianglesLength, calculateBounds, baseVertex);
        }

        public void SetTriangles(ushort[] triangles, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            SetTriangles(triangles, 0, NoAllocHelpers.SafeLength(triangles), submesh, calculateBounds, baseVertex);
        }

        public void SetTriangles(ushort[] triangles, int trianglesStart, int trianglesLength, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            if (CheckCanAccessSubmeshTriangles(submesh))
                SetTrianglesImpl(submesh, UnityEngine.Rendering.IndexFormat.UInt16, triangles, NoAllocHelpers.SafeLength(triangles), trianglesStart, trianglesLength, calculateBounds, baseVertex);
        }

        [uei.ExcludeFromDocs]
        public void SetTriangles(List<int> triangles, int submesh)
        {
            SetTriangles(triangles, submesh, true, 0);
        }

        [uei.ExcludeFromDocs]
        public void SetTriangles(List<int> triangles, int submesh, bool calculateBounds)
        {
            SetTriangles(triangles, submesh, calculateBounds, 0);
        }

        public void SetTriangles(List<int> triangles, int submesh, [DefaultValue("true")] bool calculateBounds, [DefaultValue("0")] int baseVertex)
        {
            SetTriangles(triangles, 0, NoAllocHelpers.SafeLength(triangles), submesh, calculateBounds, baseVertex);
        }

        public void SetTriangles(List<int> triangles, int trianglesStart, int trianglesLength, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            if (CheckCanAccessSubmeshTriangles(submesh))
                SetTrianglesImpl(submesh, UnityEngine.Rendering.IndexFormat.UInt32, NoAllocHelpers.ExtractArrayFromList(triangles), NoAllocHelpers.SafeLength(triangles), trianglesStart, trianglesLength, calculateBounds, baseVertex);
        }

        public void SetTriangles(List<ushort> triangles, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            SetTriangles(triangles, 0, NoAllocHelpers.SafeLength(triangles), submesh, calculateBounds, baseVertex);
        }

        public void SetTriangles(List<ushort> triangles, int trianglesStart, int trianglesLength, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            if (CheckCanAccessSubmeshTriangles(submesh))
                SetTrianglesImpl(submesh, UnityEngine.Rendering.IndexFormat.UInt16, NoAllocHelpers.ExtractArrayFromList(triangles), NoAllocHelpers.SafeLength(triangles), trianglesStart, trianglesLength, calculateBounds, baseVertex);
        }

        [uei.ExcludeFromDocs]
        public void SetIndices(int[] indices, MeshTopology topology, int submesh)
        {
            SetIndices(indices, topology, submesh, true, 0);
        }

        [uei.ExcludeFromDocs]
        public void SetIndices(int[] indices, MeshTopology topology, int submesh, bool calculateBounds)
        {
            SetIndices(indices, topology, submesh, calculateBounds, 0);
        }

        public void SetIndices(int[] indices, MeshTopology topology, int submesh, [DefaultValue("true")] bool calculateBounds, [DefaultValue("0")] int baseVertex)
        {
            SetIndices(indices, 0, NoAllocHelpers.SafeLength(indices), topology, submesh, calculateBounds, baseVertex);
        }

        public void SetIndices(int[] indices, int indicesStart, int indicesLength, MeshTopology topology, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            if (CheckCanAccessSubmeshIndices(submesh))
            {
                CheckIndicesArrayRange(NoAllocHelpers.SafeLength(indices), indicesStart, indicesLength);
                SetIndicesImpl(submesh, topology, UnityEngine.Rendering.IndexFormat.UInt32, indices, indicesStart, indicesLength, calculateBounds, baseVertex);
            }
        }

        public void SetIndices(ushort[] indices, MeshTopology topology, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            SetIndices(indices, 0, NoAllocHelpers.SafeLength(indices), topology, submesh, calculateBounds, baseVertex);
        }

        public void SetIndices(ushort[] indices, int indicesStart, int indicesLength, MeshTopology topology, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            if (CheckCanAccessSubmeshIndices(submesh))
            {
                CheckIndicesArrayRange(NoAllocHelpers.SafeLength(indices), indicesStart, indicesLength);
                SetIndicesImpl(submesh, topology, UnityEngine.Rendering.IndexFormat.UInt16, indices, indicesStart, indicesLength, calculateBounds, baseVertex);
            }
        }

        public void SetIndices<T>(NativeArray<T> indices, MeshTopology topology, int submesh, bool calculateBounds = true, int baseVertex = 0) where T : struct
        {
            SetIndices(indices, 0, indices.Length, topology, submesh, calculateBounds, baseVertex);
        }

        public unsafe void SetIndices<T>(NativeArray<T> indices, int indicesStart, int indicesLength, MeshTopology topology, int submesh, bool calculateBounds = true, int baseVertex = 0) where T : struct
        {
            if (CheckCanAccessSubmeshIndices(submesh))
            {
                var tSize = UnsafeUtility.SizeOf<T>();
                if (tSize != 2 && tSize != 4)
                    throw new ArgumentException($"{nameof(SetIndices)} with NativeArray should use type is 2 or 4 bytes in size");

                CheckIndicesArrayRange(indices.Length, indicesStart, indicesLength);
                SetIndicesNativeArrayImpl(submesh, topology, tSize == 2 ? UnityEngine.Rendering.IndexFormat.UInt16 : UnityEngine.Rendering.IndexFormat.UInt32, (IntPtr)indices.GetUnsafeReadOnlyPtr(), indicesStart, indicesLength, calculateBounds, baseVertex);
            }
        }

        public void SetIndices(List<int> indices, MeshTopology topology, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            SetIndices(indices, 0, NoAllocHelpers.SafeLength(indices), topology, submesh, calculateBounds, baseVertex);
        }

        public void SetIndices(List<int> indices, int indicesStart, int indicesLength, MeshTopology topology, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            if (CheckCanAccessSubmeshIndices(submesh))
            {
                var indicesArray = NoAllocHelpers.ExtractArrayFromList(indices);
                CheckIndicesArrayRange(NoAllocHelpers.SafeLength(indices), indicesStart, indicesLength);
                SetIndicesImpl(submesh, topology, UnityEngine.Rendering.IndexFormat.UInt32, indicesArray, indicesStart, indicesLength, calculateBounds, baseVertex);
            }
        }

        public void SetIndices(List<ushort> indices, MeshTopology topology, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            SetIndices(indices, 0, NoAllocHelpers.SafeLength(indices), topology, submesh, calculateBounds, baseVertex);
        }

        public void SetIndices(List<ushort> indices, int indicesStart, int indicesLength, MeshTopology topology, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            if (CheckCanAccessSubmeshIndices(submesh))
            {
                var indicesArray = NoAllocHelpers.ExtractArrayFromList(indices);
                CheckIndicesArrayRange(NoAllocHelpers.SafeLength(indices), indicesStart, indicesLength);
                SetIndicesImpl(submesh, topology, UnityEngine.Rendering.IndexFormat.UInt16, indicesArray, indicesStart, indicesLength, calculateBounds, baseVertex);
            }
        }

        //
        // submeshes
        //

        public void SetSubMeshes(SubMeshDescriptor[] desc, int start, int count, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default)
        {
            if (count > 0 && desc == null)
                throw new ArgumentNullException("desc", "Array of submeshes cannot be null unless count is zero.");

            int valuesCount = desc?.Length ?? 0;
            if (start < 0 || count < 0 || start + count > valuesCount)
                throw new ArgumentOutOfRangeException($"Bad start/count arguments (start:{start} count:{count} desc.Length:{valuesCount})");

            for (int i = start; i < start + count; ++i)
            {
                MeshTopology t = desc[i].topology;
                if (t < MeshTopology.Triangles || t > MeshTopology.Points)
                    throw new ArgumentException(nameof(desc), $"{i}-th submesh descriptor has invalid topology ({(int)t}).");
                if (t == (MeshTopology)1)
                    throw new ArgumentException(nameof(desc), $"{i}-th submesh descriptor has triangles strip topology, which is no longer supported.");
            }

            SetAllSubMeshesAtOnceFromArray(desc, start, count, flags);
        }

        public void SetSubMeshes(SubMeshDescriptor[] desc, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default)
        {
            SetSubMeshes(desc, 0, desc?.Length ?? 0, flags);
        }

        public void SetSubMeshes(List<SubMeshDescriptor> desc, int start, int count, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default)
        {
            SetSubMeshes(NoAllocHelpers.ExtractArrayFromListT<SubMeshDescriptor>(desc), start, count, flags);
        }

        public void SetSubMeshes(List<SubMeshDescriptor> desc, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default)
        {
            SetSubMeshes(NoAllocHelpers.ExtractArrayFromListT<SubMeshDescriptor>(desc), 0, desc?.Count ?? 0, flags);
        }

        public unsafe void SetSubMeshes<T>(NativeArray<T> desc, int start, int count, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default) where T : struct
        {
            if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<SubMeshDescriptor>())
                throw new ArgumentException($"{nameof(SetSubMeshes)} with NativeArray should use struct type that is {UnsafeUtility.SizeOf<SubMeshDescriptor>()} bytes in size");

            if (start < 0 || count < 0 || start + count > desc.Length)
                throw new ArgumentOutOfRangeException($"Bad start/count arguments (start:{start} count:{count} desc.Length:{desc.Length})");

            SetAllSubMeshesAtOnceFromNativeArray((IntPtr)desc.GetUnsafeReadOnlyPtr(), start, count, UnityEngine.Rendering.MeshUpdateFlags.Default);
        }

        public unsafe void SetSubMeshes<T>(NativeArray<T> desc, UnityEngine.Rendering.MeshUpdateFlags flags = UnityEngine.Rendering.MeshUpdateFlags.Default) where T : struct
        {
            SetSubMeshes(desc, 0, desc.Length, flags);
        }

        //

        public void GetBindposes(List<Matrix4x4> bindposes)
        {
            if (bindposes == null)
                throw new ArgumentNullException(nameof(bindposes), "The result bindposes list cannot be null.");

            NoAllocHelpers.EnsureListElemCount(bindposes, GetBindposeCount());
            GetBindposesNonAllocImpl(NoAllocHelpers.ExtractArrayFromListT(bindposes));
        }

        public void GetBoneWeights(List<BoneWeight> boneWeights)
        {
            if (boneWeights == null)
                throw new ArgumentNullException(nameof(boneWeights), "The result boneWeights list cannot be null.");

            if (HasBoneWeights())
                NoAllocHelpers.EnsureListElemCount(boneWeights, vertexCount);
            GetBoneWeightsNonAllocImpl(NoAllocHelpers.ExtractArrayFromListT(boneWeights));
        }

        public BoneWeight[] boneWeights
        {
            get
            {
                return GetBoneWeightsImpl();
            }
            set
            {
                SetBoneWeightsImpl(value);
            }
        }

        //
        //
        //

        public void Clear([DefaultValue("true")] bool keepVertexLayout)
        {
            ClearImpl(keepVertexLayout);
        }

        [uei.ExcludeFromDocs]
        public void Clear()
        {
            ClearImpl(true);
        }

        [uei.ExcludeFromDocs] public void RecalculateBounds() { RecalculateBounds(UnityEngine.Rendering.MeshUpdateFlags.Default); }
        [uei.ExcludeFromDocs] public void RecalculateNormals() { RecalculateNormals(UnityEngine.Rendering.MeshUpdateFlags.Default); }
        [uei.ExcludeFromDocs] public void RecalculateTangents() { RecalculateTangents(UnityEngine.Rendering.MeshUpdateFlags.Default); }

        public void RecalculateBounds([DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            if (canAccess)  RecalculateBoundsImpl(flags);
            else            Debug.LogError(String.Format("Not allowed to call RecalculateBounds() on mesh '{0}'", name));
        }

        public void RecalculateNormals([DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            if (canAccess)  RecalculateNormalsImpl(flags);
            else            Debug.LogError(String.Format("Not allowed to call RecalculateNormals() on mesh '{0}'", name));
        }

        public void RecalculateTangents([DefaultValue("MeshUpdateFlags.Default")] UnityEngine.Rendering.MeshUpdateFlags flags)
        {
            if (canAccess)  RecalculateTangentsImpl(flags);
            else            Debug.LogError(String.Format("Not allowed to call RecalculateTangents() on mesh '{0}'", name));
        }

        public void MarkDynamic()
        {
            if (canAccess)
                MarkDynamicImpl();
        }

        public void UploadMeshData(bool markNoLongerReadable)
        {
            if (canAccess)
                UploadMeshDataImpl(markNoLongerReadable);
        }

        public void Optimize()
        {
            if (canAccess)
                OptimizeImpl();
            else
                Debug.LogError(String.Format("Not allowed to call Optimize() on mesh '{0}'", name));
        }

        public void OptimizeIndexBuffers()
        {
            if (canAccess)
                OptimizeIndexBuffersImpl();
            else
                Debug.LogError(String.Format("Not allowed to call OptimizeIndexBuffers() on mesh '{0}'", name));
        }

        public void OptimizeReorderVertexBuffer()
        {
            if (canAccess)
                OptimizeReorderVertexBufferImpl();
            else
                Debug.LogError(String.Format("Not allowed to call OptimizeReorderVertexBuffer() on mesh '{0}'", name));
        }

        public MeshTopology GetTopology(int submesh)
        {
            if (submesh < 0 || submesh >= subMeshCount)
            {
                Debug.LogError("Failed getting topology. Submesh index is out of bounds.", this);
                return MeshTopology.Triangles;
            }
            return GetTopologyImpl(submesh);
        }

        public void CombineMeshes(CombineInstance[] combine, [DefaultValue("true")] bool mergeSubMeshes, [DefaultValue("true")] bool useMatrices, [DefaultValue("false")] bool hasLightmapData)
        {
            CombineMeshesImpl(combine, mergeSubMeshes, useMatrices, hasLightmapData);
        }

        [uei.ExcludeFromDocs]
        public void CombineMeshes(CombineInstance[] combine, bool mergeSubMeshes, bool useMatrices)
        {
            CombineMeshesImpl(combine, mergeSubMeshes, useMatrices, false);
        }

        [uei.ExcludeFromDocs]
        public void CombineMeshes(CombineInstance[] combine, bool mergeSubMeshes)
        {
            CombineMeshesImpl(combine, mergeSubMeshes, true, false);
        }

        [uei.ExcludeFromDocs]
        public void CombineMeshes(CombineInstance[] combine)
        {
            CombineMeshesImpl(combine, true, true, false);
        }
    }

    [Serializable]
    [UsedByNativeCode]
    public struct BoneWeight : IEquatable<BoneWeight>
    {
        [SerializeField]
        private float   m_Weight0, m_Weight1, m_Weight2, m_Weight3;
        [SerializeField]
        private int     m_BoneIndex0, m_BoneIndex1, m_BoneIndex2, m_BoneIndex3;

        public float weight0 { get { return m_Weight0; } set { m_Weight0 = value; } }
        public float weight1 { get { return m_Weight1; } set { m_Weight1 = value; } }
        public float weight2 { get { return m_Weight2; } set { m_Weight2 = value; } }
        public float weight3 { get { return m_Weight3; } set { m_Weight3 = value; } }

        public int boneIndex0 { get { return m_BoneIndex0; } set { m_BoneIndex0 = value; } }
        public int boneIndex1 { get { return m_BoneIndex1; } set { m_BoneIndex1 = value; } }
        public int boneIndex2 { get { return m_BoneIndex2; } set { m_BoneIndex2 = value; } }
        public int boneIndex3 { get { return m_BoneIndex3; } set { m_BoneIndex3 = value; } }

        // used to allow BoneWeights to be used as keys in hash tables
        public override int GetHashCode()
        {
            return boneIndex0.GetHashCode() ^ (boneIndex1.GetHashCode() << 2) ^ (boneIndex2.GetHashCode() >> 2) ^ (boneIndex3.GetHashCode() >> 1)
                ^ (weight0.GetHashCode() << 5) ^ (weight1.GetHashCode() << 4) ^ (weight2.GetHashCode() >> 4) ^ (weight3.GetHashCode() >> 3);
        }

        public override bool Equals(object other)
        {
            return other is BoneWeight && Equals((BoneWeight)other);
        }

        public bool Equals(BoneWeight other)
        {
            return boneIndex0.Equals(other.boneIndex0) && boneIndex1.Equals(other.boneIndex1) && boneIndex2.Equals(other.boneIndex2) && boneIndex3.Equals(other.boneIndex3)
                && (new Vector4(weight0, weight1, weight2, weight3)).Equals(new Vector4(other.weight0, other.weight1, other.weight2, other.weight3));
        }

        public static bool operator==(BoneWeight lhs, BoneWeight rhs)
        {
            // Returns false in the presence of NaN values.
            return lhs.boneIndex0 == rhs.boneIndex0 && lhs.boneIndex1 == rhs.boneIndex1 && lhs.boneIndex2 == rhs.boneIndex2 && lhs.boneIndex3 == rhs.boneIndex3
                && (new Vector4(lhs.weight0, lhs.weight1, lhs.weight2, lhs.weight3) == new Vector4(rhs.weight0, rhs.weight1, rhs.weight2, rhs.weight3));
        }

        public static bool operator!=(BoneWeight lhs, BoneWeight rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }
    }

    [Serializable]
    [UsedByNativeCode]
    public struct BoneWeight1 : IEquatable<BoneWeight1>
    {
        [SerializeField]
        private float m_Weight;
        [SerializeField]
        private int m_BoneIndex;

        public float weight { get { return m_Weight; } set { m_Weight = value; } }

        public int boneIndex { get { return m_BoneIndex; } set { m_BoneIndex = value; } }

        public override bool Equals(object other)
        {
            return other is BoneWeight1 && Equals((BoneWeight1)other);
        }

        public bool Equals(BoneWeight1 other)
        {
            return boneIndex.Equals(other.boneIndex) && weight.Equals(other.weight);
        }

        // used to allow BoneWeights to be used as keys in hash tables
        public override int GetHashCode()
        {
            return boneIndex.GetHashCode() ^ weight.GetHashCode();
        }

        public static bool operator==(BoneWeight1 lhs, BoneWeight1 rhs)
        {
            // Returns false in the presence of NaN values.
            return lhs.boneIndex == rhs.boneIndex && lhs.weight == rhs.weight;
        }

        public static bool operator!=(BoneWeight1 lhs, BoneWeight1 rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }
    }

    public struct CombineInstance
    {
        private int m_MeshInstanceID;
        private int m_SubMeshIndex;
        private Matrix4x4 m_Transform;
        private Vector4 m_LightmapScaleOffset;
        private Vector4 m_RealtimeLightmapScaleOffset;

        public Mesh mesh { get { return Mesh.FromInstanceID(m_MeshInstanceID); } set { m_MeshInstanceID = value != null ? value.GetInstanceID() : 0; } }
        public int subMeshIndex { get { return m_SubMeshIndex; } set { m_SubMeshIndex = value; } }
        public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }
        public Vector4 lightmapScaleOffset { get { return m_LightmapScaleOffset; } set { m_LightmapScaleOffset = value; } }
        public Vector4 realtimeLightmapScaleOffset { get { return m_RealtimeLightmapScaleOffset; } set { m_RealtimeLightmapScaleOffset = value; } }
    }
}
