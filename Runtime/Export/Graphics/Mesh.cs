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

namespace UnityEngine
{
    [RequiredByNativeCode] // Used by IMGUI (even on empty projects, it draws development console & watermarks)
    public sealed partial class Mesh : Object
    {
        internal VertexAttribute GetUVChannel(int uvIndex)
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

        private void SetSizedArrayForChannel(VertexAttribute channel, VertexAttributeFormat format, int dim, System.Array values, int valuesArrayLength, int valuesStart, int valuesCount)
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
                SetArrayForChannelImpl(channel, format, dim, values, valuesArrayLength, valuesStart, valuesCount);
            }
            else
                PrintErrorCantAccessChannel(channel);
        }

        private void SetArrayForChannel<T>(VertexAttribute channel, VertexAttributeFormat format, int dim, T[] values)
        {
            var len = NoAllocHelpers.SafeLength(values);
            SetSizedArrayForChannel(channel, format, dim, values, len, 0, len);
        }

        private void SetArrayForChannel<T>(VertexAttribute channel, T[] values)
        {
            var len = NoAllocHelpers.SafeLength(values);
            SetSizedArrayForChannel(channel, VertexAttributeFormat.Float32, DefaultDimensionForChannel(channel), values, len, 0, len);
        }

        private void SetListForChannel<T>(VertexAttribute channel, VertexAttributeFormat format, int dim, List<T> values, int start, int length)
        {
            SetSizedArrayForChannel(channel, format, dim, NoAllocHelpers.ExtractArrayFromList(values), NoAllocHelpers.SafeLength(values), start, length);
        }

        private void SetListForChannel<T>(VertexAttribute channel, List<T> values, int start, int length)
        {
            SetSizedArrayForChannel(channel, VertexAttributeFormat.Float32, DefaultDimensionForChannel(channel), NoAllocHelpers.ExtractArrayFromList(values), NoAllocHelpers.SafeLength(values), start, length);
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
            set { SetArrayForChannel(VertexAttribute.Position, value); }
        }
        public Vector3[] normals
        {
            get { return GetAllocArrayFromChannel<Vector3>(VertexAttribute.Normal); }
            set { SetArrayForChannel(VertexAttribute.Normal, value); }
        }
        public Vector4[] tangents
        {
            get { return GetAllocArrayFromChannel<Vector4>(VertexAttribute.Tangent); }
            set { SetArrayForChannel(VertexAttribute.Tangent, value); }
        }
        public Vector2[] uv
        {
            get { return GetAllocArrayFromChannel<Vector2>(VertexAttribute.TexCoord0); }
            set { SetArrayForChannel(VertexAttribute.TexCoord0, value); }
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
                throw new ArgumentNullException("The result vertices list cannot be null.", "vertices");

            GetListForChannel(vertices, vertexCount, VertexAttribute.Position, DefaultDimensionForChannel(VertexAttribute.Position));
        }

        public void SetVertices(List<Vector3> inVertices)
        {
            SetVertices(inVertices, 0, NoAllocHelpers.SafeLength(inVertices));
        }

        public void SetVertices(List<Vector3> inVertices, int start, int length)
        {
            SetListForChannel(VertexAttribute.Position, inVertices, start, length);
        }

        public void SetVertices(Vector3[] inVertices)
        {
            SetVertices(inVertices, 0, NoAllocHelpers.SafeLength(inVertices));
        }

        public void SetVertices(Vector3[] inVertices, int start, int length)
        {
            SetSizedArrayForChannel(VertexAttribute.Position, VertexAttributeFormat.Float32, DefaultDimensionForChannel(VertexAttribute.Position), inVertices, NoAllocHelpers.SafeLength(inVertices), start, length);
        }

        public void GetNormals(List<Vector3> normals)
        {
            if (normals == null)
                throw new ArgumentNullException("The result normals list cannot be null.", "normals");

            GetListForChannel(normals, vertexCount, VertexAttribute.Normal, DefaultDimensionForChannel(VertexAttribute.Normal));
        }

        public void SetNormals(List<Vector3> inNormals)
        {
            SetNormals(inNormals, 0, NoAllocHelpers.SafeLength(inNormals));
        }

        public void SetNormals(List<Vector3> inNormals, int start, int length)
        {
            SetListForChannel(VertexAttribute.Normal, inNormals, start, length);
        }

        public void SetNormals(Vector3[] inNormals)
        {
            SetNormals(inNormals, 0, NoAllocHelpers.SafeLength(inNormals));
        }

        public void SetNormals(Vector3[] inNormals, int start, int length)
        {
            SetSizedArrayForChannel(VertexAttribute.Normal, VertexAttributeFormat.Float32, DefaultDimensionForChannel(VertexAttribute.Normal), inNormals, NoAllocHelpers.SafeLength(inNormals), start, length);
        }

        public void GetTangents(List<Vector4> tangents)
        {
            if (tangents == null)
                throw new ArgumentNullException("The result tangents list cannot be null.", "tangents");

            GetListForChannel(tangents, vertexCount, VertexAttribute.Tangent, DefaultDimensionForChannel(VertexAttribute.Tangent));
        }

        public void SetTangents(List<Vector4> inTangents)
        {
            SetTangents(inTangents, 0, NoAllocHelpers.SafeLength(inTangents));
        }

        public void SetTangents(List<Vector4> inTangents, int start, int length)
        {
            SetListForChannel(VertexAttribute.Tangent, inTangents, start, length);
        }

        public void SetTangents(Vector4[] inTangents)
        {
            SetTangents(inTangents, 0, NoAllocHelpers.SafeLength(inTangents));
        }

        public void SetTangents(Vector4[] inTangents, int start, int length)
        {
            SetSizedArrayForChannel(VertexAttribute.Tangent, VertexAttributeFormat.Float32, DefaultDimensionForChannel(VertexAttribute.Tangent), inTangents, NoAllocHelpers.SafeLength(inTangents), start, length);
        }

        public void GetColors(List<Color> colors)
        {
            if (colors == null)
                throw new ArgumentNullException("The result colors list cannot be null.", "colors");

            GetListForChannel(colors, vertexCount, VertexAttribute.Color, DefaultDimensionForChannel(VertexAttribute.Color));
        }

        public void SetColors(List<Color> inColors)
        {
            SetColors(inColors, 0, NoAllocHelpers.SafeLength(inColors));
        }

        public void SetColors(List<Color> inColors, int start, int length)
        {
            SetListForChannel(VertexAttribute.Color, inColors, start, length);
        }

        public void SetColors(Color[] inColors)
        {
            SetColors(inColors, 0, NoAllocHelpers.SafeLength(inColors));
        }

        public void SetColors(Color[] inColors, int start, int length)
        {
            SetSizedArrayForChannel(VertexAttribute.Color, VertexAttributeFormat.Float32, DefaultDimensionForChannel(VertexAttribute.Color), inColors, NoAllocHelpers.SafeLength(inColors), start, length);
        }

        public void GetColors(List<Color32> colors)
        {
            if (colors == null)
                throw new ArgumentNullException("The result colors list cannot be null.", "colors");

            GetListForChannel(colors, vertexCount, VertexAttribute.Color, 4, VertexAttributeFormat.UNorm8);
        }

        public void SetColors(List<Color32> inColors)
        {
            SetColors(inColors, 0, NoAllocHelpers.SafeLength(inColors));
        }

        public void SetColors(List<Color32> inColors, int start, int length)
        {
            SetListForChannel(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, inColors, start, length);
        }

        public void SetColors(Color32[] inColors)
        {
            SetColors(inColors, 0, NoAllocHelpers.SafeLength(inColors));
        }

        public void SetColors(Color32[] inColors, int start, int length)
        {
            SetSizedArrayForChannel(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, inColors, NoAllocHelpers.SafeLength(inColors), start, length);
        }

        private void SetUvsImpl<T>(int uvIndex, int dim, List<T> uvs, int start, int length)
        {
            // before this resulted in error *printed* out deep inside c++ code (coming from assert - useless for end-user)
            // while excpetion would make sense we dont want to add exceptions to exisisting apis
            if (uvIndex < 0 || uvIndex > 7)
            {
                Debug.LogError("The uv index is invalid. Must be in the range 0 to 7.");
                return;
            }
            SetListForChannel(GetUVChannel(uvIndex), VertexAttributeFormat.Float32, dim, uvs, start, length);
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

        public void SetUVs(int channel, List<Vector2> uvs, int start, int length)
        {
            SetUvsImpl(channel, 2, uvs, start, length);
        }

        public void SetUVs(int channel, List<Vector3> uvs, int start, int length)
        {
            SetUvsImpl(channel, 3, uvs, start, length);
        }

        public void SetUVs(int channel, List<Vector4> uvs, int start, int length)
        {
            SetUvsImpl(channel, 4, uvs, start, length);
        }

        private void SetUvsImpl(int uvIndex, int dim, System.Array uvs, int arrayStart, int arraySize)
        {
            if (uvIndex < 0 || uvIndex > 7)
                throw new ArgumentOutOfRangeException(nameof(uvIndex), uvIndex, "The uv index is invalid. Must be in the range 0 to 7.");
            SetSizedArrayForChannel(GetUVChannel(uvIndex), VertexAttributeFormat.Float32, dim, uvs, NoAllocHelpers.SafeLength(uvs), arrayStart, arraySize);
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

        public void SetUVs(int channel, Vector2[] uvs, int start, int length)
        {
            SetUvsImpl(channel, 2, uvs, start, length);
        }

        public void SetUVs(int channel, Vector3[] uvs, int start, int length)
        {
            SetUvsImpl(channel, 3, uvs, start, length);
        }

        public void SetUVs(int channel, Vector4[] uvs, int start, int length)
        {
            SetUvsImpl(channel, 4, uvs, start, length);
        }

        private void GetUVsImpl<T>(int uvIndex, List<T> uvs, int dim)
        {
            if (uvs == null)
                throw new ArgumentNullException("The result uvs list cannot be null.", "uvs");
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

        public UnityEngine.Rendering.VertexAttributeDescriptor[] GetVertexAttributes()
        {
            return (UnityEngine.Rendering.VertexAttributeDescriptor[])GetVertexAttributesAlloc();
        }

        public unsafe void SetVertexBufferData<T>(NativeArray<T> data, int dataStart, int meshBufferStart, int count, int stream = 0) where T : struct
        {
            if (!canAccess)
                throw new InvalidOperationException($"Not allowed to access vertex data on mesh '{name}' (isReadable is false; Read/Write must be enabled in import settings)");
            if (dataStart < 0 || meshBufferStart < 0 || count < 0 || dataStart + count > data.Length)
                throw new ArgumentOutOfRangeException($"Bad start/count arguments (dataStart:{dataStart} meshBufferStart:{meshBufferStart} count:{count})");
            InternalSetVertexBufferData(stream, (IntPtr)data.GetUnsafeReadOnlyPtr(), dataStart, meshBufferStart, count, UnsafeUtility.SizeOf<T>());
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
                throw new ArgumentNullException("The result triangles list cannot be null.", "triangles");

            if (submesh < 0 || submesh >= subMeshCount)
                throw new IndexOutOfRangeException("Specified sub mesh is out of range. Must be greater or equal to 0 and less than subMeshCount.");

            NoAllocHelpers.EnsureListElemCount(triangles, 3 * (int)GetTrianglesCountImpl(submesh));
            GetTrianglesNonAllocImpl(NoAllocHelpers.ExtractArrayFromListT(triangles), submesh, applyBaseVertex);
        }

        public void GetTriangles(List<ushort> triangles, int submesh, bool applyBaseVertex = true)
        {
            if (triangles == null)
                throw new ArgumentNullException("The result triangles list cannot be null.", "triangles");

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
                throw new ArgumentNullException("The result indices list cannot be null.", nameof(indices));

            if (submesh < 0 || submesh >= subMeshCount)
                throw new IndexOutOfRangeException("Specified sub mesh is out of range. Must be greater or equal to 0 and less than subMeshCount.");

            NoAllocHelpers.EnsureListElemCount(indices, (int)GetIndexCount(submesh));
            GetIndicesNonAllocImpl(NoAllocHelpers.ExtractArrayFromListT(indices), submesh, applyBaseVertex);
        }

        public void GetIndices(List<ushort> indices, int submesh, bool applyBaseVertex = true)
        {
            if (indices == null)
                throw new ArgumentNullException("The result indices list cannot be null.", nameof(indices));

            if (submesh < 0 || submesh >= subMeshCount)
                throw new IndexOutOfRangeException("Specified sub mesh is out of range. Must be greater or equal to 0 and less than subMeshCount.");

            NoAllocHelpers.EnsureListElemCount(indices, (int)GetIndexCount(submesh));
            GetIndicesNonAllocImpl16(NoAllocHelpers.ExtractArrayFromListT(indices), submesh, applyBaseVertex);
        }

        public unsafe void SetIndexBufferData<T>(NativeArray<T> data, int dataStart, int meshBufferStart, int count) where T : struct
        {
            if (!canAccess)
            {
                PrintErrorCantAccessIndices();
                return;
            }
            if (dataStart < 0 || meshBufferStart < 0 || count < 0 || dataStart + count > data.Length)
                throw new ArgumentOutOfRangeException($"Bad start/count arguments (dataStart:{dataStart} meshBufferStart:{meshBufferStart} count:{count})");
            InternalSetIndexBufferData((IntPtr)data.GetUnsafeReadOnlyPtr(), dataStart, meshBufferStart, count, UnsafeUtility.SizeOf<T>());
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

        private void CheckIndicesArrayRange(System.Array values, int valuesLength, int start, int length)
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
            CheckIndicesArrayRange(triangles, trianglesArrayLength, start, length);
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
            SetTriangles(triangles, 0, NoAllocHelpers.SafeLength(triangles), submesh, calculateBounds, 0);
        }

        public void SetTriangles(List<int> triangles, int trianglesStart, int trianglesLength, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            if (CheckCanAccessSubmeshTriangles(submesh))
                SetTrianglesImpl(submesh, UnityEngine.Rendering.IndexFormat.UInt32, NoAllocHelpers.ExtractArrayFromList(triangles), NoAllocHelpers.SafeLength(triangles), trianglesStart, trianglesLength, calculateBounds, baseVertex);
        }

        public void SetTriangles(List<ushort> triangles, int submesh, bool calculateBounds = true, int baseVertex = 0)
        {
            SetTriangles(triangles, 0, NoAllocHelpers.SafeLength(triangles), submesh, calculateBounds, 0);
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
                CheckIndicesArrayRange(indices, NoAllocHelpers.SafeLength(indices), indicesStart, indicesLength);
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
                CheckIndicesArrayRange(indices, NoAllocHelpers.SafeLength(indices), indicesStart, indicesLength);
                SetIndicesImpl(submesh, topology, UnityEngine.Rendering.IndexFormat.UInt16, indices, indicesStart, indicesLength, calculateBounds, baseVertex);
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
                CheckIndicesArrayRange(indicesArray, NoAllocHelpers.SafeLength(indices), indicesStart, indicesLength);
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
                CheckIndicesArrayRange(indicesArray, NoAllocHelpers.SafeLength(indices), indicesStart, indicesLength);
                SetIndicesImpl(submesh, topology, UnityEngine.Rendering.IndexFormat.UInt16, indicesArray, indicesStart, indicesLength, calculateBounds, baseVertex);
            }
        }

        //

        public void GetBindposes(List<Matrix4x4> bindposes)
        {
            if (bindposes == null)
                throw new ArgumentNullException("The result bindposes list cannot be null.", "bindposes");

            NoAllocHelpers.EnsureListElemCount(bindposes, GetBindposeCount());
            GetBindposesNonAllocImpl(NoAllocHelpers.ExtractArrayFromListT(bindposes));
        }

        public void GetBoneWeights(List<BoneWeight> boneWeights)
        {
            if (boneWeights == null)
                throw new ArgumentNullException("The result boneWeights list cannot be null.", "boneWeights");

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

        public void RecalculateBounds()
        {
            if (canAccess)  RecalculateBoundsImpl();
            else            Debug.LogError(String.Format("Not allowed to call RecalculateBounds() on mesh '{0}'", name));
        }

        public void RecalculateNormals()
        {
            if (canAccess)  RecalculateNormalsImpl();
            else            Debug.LogError(String.Format("Not allowed to call RecalculateNormals() on mesh '{0}'", name));
        }

        public void RecalculateTangents()
        {
            if (canAccess)  RecalculateTangentsImpl();
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
                Debug.LogError(String.Format("Failed getting topology. Submesh index is out of bounds."), this);
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
