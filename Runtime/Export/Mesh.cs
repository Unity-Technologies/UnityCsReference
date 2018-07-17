// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using UnityEngine.Rendering;

namespace UnityEngine
{
    [RequiredByNativeCode] // Used by IMGUI (even on empty projects, it draws development console & watermarks)
    public sealed partial class Mesh : Object
    {
        // WARNING: MUST be kept in sync with InternalScriptingShaderChannel in Runtime/Graphics/GraphicsScriptBindings.h
        internal enum InternalVertexChannelType
        {
            Float = 0,
            Color = 2,
        }

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

        private T[] GetAllocArrayFromChannel<T>(VertexAttribute channel, InternalVertexChannelType format, int dim)
        {
            if (canAccess)
            {
                if (HasChannel(channel))
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
            return GetAllocArrayFromChannel<T>(channel, InternalVertexChannelType.Float, DefaultDimensionForChannel(channel));
        }

        private void SetSizedArrayForChannel(VertexAttribute channel, InternalVertexChannelType format, int dim, System.Array values, int valuesCount)
        {
            if (canAccess)
                SetArrayForChannelImpl(channel, format, dim, values, valuesCount);
            else
                PrintErrorCantAccessChannel(channel);
        }

        private void SetArrayForChannel<T>(VertexAttribute channel, InternalVertexChannelType format, int dim, T[] values)
        {
            SetSizedArrayForChannel(channel, format, dim, values, NoAllocHelpers.SafeLength(values));
        }

        private void SetArrayForChannel<T>(VertexAttribute channel, T[] values)
        {
            SetSizedArrayForChannel(channel, InternalVertexChannelType.Float, DefaultDimensionForChannel(channel), values, NoAllocHelpers.SafeLength(values));
        }

        private void SetListForChannel<T>(VertexAttribute channel, InternalVertexChannelType format, int dim, List<T> values)
        {
            SetSizedArrayForChannel(channel, format, dim, NoAllocHelpers.ExtractArrayFromList(values), NoAllocHelpers.SafeLength(values));
        }

        private void SetListForChannel<T>(VertexAttribute channel, List<T> values)
        {
            SetSizedArrayForChannel(channel, InternalVertexChannelType.Float, DefaultDimensionForChannel(channel), NoAllocHelpers.ExtractArrayFromList(values), NoAllocHelpers.SafeLength(values));
        }

        private void GetListForChannel<T>(List<T> buffer, int capacity, VertexAttribute channel, int dim)
        {
            GetListForChannel(buffer, capacity, channel, dim, InternalVertexChannelType.Float);
        }

        private void GetListForChannel<T>(List<T> buffer, int capacity, VertexAttribute channel, int dim, InternalVertexChannelType channelType)
        {
            buffer.Clear();

            if (!canAccess)
            {
                PrintErrorCantAccessChannel(channel);
                return;
            }

            if (!HasChannel(channel))
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
            get { return GetAllocArrayFromChannel<Color32>(VertexAttribute.Color, InternalVertexChannelType.Color, 1); }
            set { SetArrayForChannel(VertexAttribute.Color, InternalVertexChannelType.Color, 1, value); }
        }

        public void GetVertices(List<Vector3> vertices)
        {
            if (vertices == null)
                throw new ArgumentNullException("The result vertices list cannot be null.", "vertices");

            GetListForChannel(vertices, vertexCount, VertexAttribute.Position, DefaultDimensionForChannel(VertexAttribute.Position));
        }

        public void SetVertices(List<Vector3> inVertices)
        {
            SetListForChannel(VertexAttribute.Position, inVertices);
        }

        public void GetNormals(List<Vector3> normals)
        {
            if (normals == null)
                throw new ArgumentNullException("The result normals list cannot be null.", "normals");

            GetListForChannel(normals, vertexCount, VertexAttribute.Normal, DefaultDimensionForChannel(VertexAttribute.Normal));
        }

        public void SetNormals(List<Vector3> inNormals)
        {
            SetListForChannel(VertexAttribute.Normal, inNormals);
        }

        public void GetTangents(List<Vector4> tangents)
        {
            if (tangents == null)
                throw new ArgumentNullException("The result tangents list cannot be null.", "tangents");

            GetListForChannel(tangents, vertexCount, VertexAttribute.Tangent, DefaultDimensionForChannel(VertexAttribute.Tangent));
        }

        public void SetTangents(List<Vector4> inTangents)
        {
            SetListForChannel(VertexAttribute.Tangent, inTangents);
        }

        public void GetColors(List<Color> colors)
        {
            if (colors == null)
                throw new ArgumentNullException("The result colors list cannot be null.", "colors");

            GetListForChannel(colors, vertexCount, VertexAttribute.Color, DefaultDimensionForChannel(VertexAttribute.Color));
        }

        public void SetColors(List<Color> inColors)
        {
            SetListForChannel(VertexAttribute.Color, inColors);
        }

        public void GetColors(List<Color32> colors)
        {
            if (colors == null)
                throw new ArgumentNullException("The result colors list cannot be null.", "colors");

            GetListForChannel(colors, vertexCount, VertexAttribute.Color, 1, InternalVertexChannelType.Color);
        }

        public void SetColors(List<Color32> inColors)
        {
            SetListForChannel(VertexAttribute.Color, InternalVertexChannelType.Color, 1, inColors);
        }

        private void SetUvsImpl<T>(int uvIndex, int dim, List<T> uvs)
        {
            // before this resulted in error *printed* out deep inside c++ code (coming from assert - useless for end-user)
            // while excpetion would make sense we dont want to add exceptions to exisisting apis
            if (uvIndex < 0 || uvIndex > 7)
            {
                Debug.LogError("The uv index is invalid. Must be in the range 0 to 7.");
                return;
            }
            SetListForChannel(GetUVChannel(uvIndex), InternalVertexChannelType.Float, dim, uvs);
        }

        public void SetUVs(int channel, List<Vector2> uvs)
        {
            SetUvsImpl(channel, 2, uvs);
        }

        public void SetUVs(int channel, List<Vector3> uvs)
        {
            SetUvsImpl(channel, 3, uvs);
        }

        public void SetUVs(int channel, List<Vector4> uvs)
        {
            SetUvsImpl(channel, 4, uvs);
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
                if (canAccess)  SetTrianglesImpl(-1, value, NoAllocHelpers.SafeLength(value), true, 0);
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

            NoAllocHelpers.EnsureListElemCount(triangles, (int)GetIndexCount(submesh));
            GetTrianglesNonAllocImpl(NoAllocHelpers.ExtractArrayFromListT(triangles), submesh, applyBaseVertex);
        }

        public int[] GetIndices(int submesh)
        {
            return GetIndices(submesh, true);
        }

        public int[] GetIndices(int submesh, [DefaultValue("true")] bool applyBaseVertex)
        {
            return CheckCanAccessSubmeshIndices(submesh) ? GetIndicesImpl(submesh, applyBaseVertex) : new int[0];
        }

        public void GetIndices(List<int> indices, int submesh)
        {
            GetIndices(indices, submesh, true);
        }

        public void GetIndices(List<int> indices, int submesh, [DefaultValue("true")] bool applyBaseVertex)
        {
            if (indices == null)
                throw new ArgumentNullException("The result indices list cannot be null.", "indices");

            if (submesh < 0 || submesh >= subMeshCount)
                throw new IndexOutOfRangeException("Specified sub mesh is out of range. Must be greater or equal to 0 and less than subMeshCount.");

            NoAllocHelpers.EnsureListElemCount(indices, (int)GetIndexCount(submesh));
            GetIndicesNonAllocImpl(NoAllocHelpers.ExtractArrayFromListT(indices), submesh, applyBaseVertex);
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

        private void SetTrianglesImpl(int submesh, System.Array triangles, int arraySize, bool calculateBounds, int baseVertex)
        {
            SetIndicesImpl(submesh, MeshTopology.Triangles, triangles, arraySize, calculateBounds, baseVertex);
        }

        // TODO: currently we cannot have proper default args in public api
        public void SetTriangles(int[] triangles, int submesh)
        {
            SetTriangles(triangles, submesh, true, 0);
        }

        public void SetTriangles(int[] triangles, int submesh, bool calculateBounds)
        {
            SetTriangles(triangles, submesh, calculateBounds, 0);
        }

        public void SetTriangles(int[] triangles, int submesh, [DefaultValue("true")] bool calculateBounds, [DefaultValue("0")] int baseVertex)
        {
            if (CheckCanAccessSubmeshTriangles(submesh))
                SetTrianglesImpl(submesh, triangles, NoAllocHelpers.SafeLength(triangles), calculateBounds, baseVertex);
        }

        public void SetTriangles(List<int> triangles, int submesh)
        {
            SetTriangles(triangles, submesh, true, 0);
        }

        public void SetTriangles(List<int> triangles, int submesh, bool calculateBounds)
        {
            SetTriangles(triangles, submesh, calculateBounds, 0);
        }

        public void SetTriangles(List<int> triangles, int submesh, [DefaultValue("true")] bool calculateBounds, [DefaultValue("0")] int baseVertex)
        {
            if (CheckCanAccessSubmeshTriangles(submesh))
                SetTrianglesImpl(submesh, NoAllocHelpers.ExtractArrayFromList(triangles), NoAllocHelpers.SafeLength(triangles), calculateBounds, baseVertex);
        }

        public void SetIndices(int[] indices, MeshTopology topology, int submesh)
        {
            SetIndices(indices, topology, submesh, true, 0);
        }

        public void SetIndices(int[] indices, MeshTopology topology, int submesh, bool calculateBounds)
        {
            SetIndices(indices, topology, submesh, calculateBounds, 0);
        }

        public void SetIndices(int[] indices, MeshTopology topology, int submesh, [DefaultValue("true")] bool calculateBounds, [DefaultValue("0")] int baseVertex)
        {
            if (CheckCanAccessSubmeshIndices(submesh))
                SetIndicesImpl(submesh, topology, indices, NoAllocHelpers.SafeLength(indices), calculateBounds, baseVertex);
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

            NoAllocHelpers.EnsureListElemCount(boneWeights, GetBoneWeightCount());
            GetBoneWeightsNonAllocImpl(NoAllocHelpers.ExtractArrayFromListT(boneWeights));
        }

        //
        //
        //

        public void Clear(bool keepVertexLayout)
        {
            ClearImpl(keepVertexLayout);
        }

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

        public void CombineMeshes(CombineInstance[] combine, bool mergeSubMeshes, bool useMatrices)
        {
            CombineMeshesImpl(combine, mergeSubMeshes, useMatrices, false);
        }

        public void CombineMeshes(CombineInstance[] combine, bool mergeSubMeshes)
        {
            CombineMeshesImpl(combine, mergeSubMeshes, true, false);
        }

        public void CombineMeshes(CombineInstance[] combine)
        {
            CombineMeshesImpl(combine, true, true, false);
        }
    }


    public sealed partial class Mesh : Object
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("This method is no longer supported (UnityUpgradable)", true)]
        public void Optimize()
        {
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
