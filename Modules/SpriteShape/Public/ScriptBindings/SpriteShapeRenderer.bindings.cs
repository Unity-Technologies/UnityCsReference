// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting.APIUpdating;

using Unity.Jobs;

namespace UnityEngine.U2D
{
    /// <summary>
    /// SpriteShapeParameters contains SpriteShape properties that are used for generating it.
    /// </summary>
    [MovedFrom("UnityEngine.Experimental.U2D")]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct SpriteShapeParameters
    {
        public Matrix4x4 transform;
        public Texture2D fillTexture;
        public uint fillScale;                          // A Fill Scale of 0 means NO fill.
        public uint splineDetail;
        public float angleThreshold;
        public float borderPivot;
        public float bevelCutoff;
        public float bevelSize;

        public bool carpet;                             // Carpets have Fills.
        public bool smartSprite;                        // Enabling this would mean a specialized Shape using only one Texture for all Sprites. If enabled must define CarpetInfo.
        public bool adaptiveUV;                         // Adaptive UV.
        public bool spriteBorders;                      // Allow 9 - Splice Corners to be used.
        public bool stretchUV;                          // Fill UVs are stretched.
    }

    /// <summary>
    /// SpriteShapeSegment contains data for each segment of mesh generated for SpriteShape.
    /// </summary>
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    public struct SpriteShapeSegment
    {
        private int m_GeomIndex;
        private int m_IndexCount;
        private int m_VertexCount;
        private int m_SpriteIndex;

        public int geomIndex
        {
            get { return m_GeomIndex; }
            set { m_GeomIndex = value; }
        }
        public int indexCount
        {
            get { return m_IndexCount; }
            set { m_IndexCount = value; }
        }
        public int vertexCount
        {
            get { return m_VertexCount; }
            set { m_VertexCount = value; }
        }
        public int spriteIndex
        {
            get { return m_SpriteIndex; }
            set { m_SpriteIndex = value; }
        }
    }

    internal enum SpriteShapeDataType
    {
        Index,
        Segment,
        BoundingBox,
        ChannelVertex,
        ChannelTexCoord0,
        ChannelNormal,
        ChannelTangent,
        DataCount
    }

    [NativeType(Header = "Modules/SpriteShape/Public/SpriteShapeRenderer.h")]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    public class SpriteShapeRenderer : Renderer
    {
        public extern Color color
        {
            get;
            set;
        }

        public extern SpriteMaskInteraction maskInteraction
        {
            get;
            set;
        }


        extern internal int GetVertexCount();
        extern internal int GetIndexCount();
        extern internal Bounds GetLocalAABB();

        extern public void Prepare(JobHandle handle, SpriteShapeParameters shapeParams, Sprite[] sprites);

        extern private void RefreshSafetyHandle(SpriteShapeDataType arrayType);
        extern private AtomicSafetyHandle GetSafetyHandle(SpriteShapeDataType arrayType);
        unsafe private NativeArray<T> GetNativeDataArray<T>(SpriteShapeDataType dataType) where T : struct
        {
            RefreshSafetyHandle(dataType);

            var info = GetDataInfo(dataType);
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(info.buffer, info.count, Allocator.Invalid);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, GetSafetyHandle(dataType));
            return array;
        }

        unsafe private NativeSlice<T> GetChannelDataArray<T>(SpriteShapeDataType dataType, VertexAttribute channel) where T : struct
        {
            RefreshSafetyHandle(dataType);

            var info = GetChannelInfo(channel);
            var buffer = (byte*)(info.buffer) + info.offset;
            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(buffer, info.stride, info.count);

            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, GetSafetyHandle(dataType));
            return slice;
        }

        extern private void SetSegmentCount(int geomCount);
        extern private void SetMeshDataCount(int vertexCount, int indexCount);
        extern private void SetMeshChannelInfo(int vertexCount, int indexCount, int hotChannelMask);
        extern private SpriteChannelInfo GetDataInfo(SpriteShapeDataType arrayType);
        extern private SpriteChannelInfo GetChannelInfo(VertexAttribute channel);

        /// <summary>
        /// Returns Bounds of SpriteShapeRenderer in a NativeArray so C# Job can access it.
        /// </summary>
        /// <returns>Returns a NativeArray of Bounds with size always 1.</returns>
        unsafe public NativeArray<Bounds> GetBounds()
        {
            return GetNativeDataArray<Bounds>(SpriteShapeDataType.BoundingBox);
        }

        /// <summary>
        /// Returns a NativeArray of SpriteShapeSegment.
        /// </summary>
        /// <param name="dataSize">Size of the Array requested.</param>
        /// <returns>Returns a NativeArray of SpriteShapeSegments with requested Array size.</returns>
        unsafe public NativeArray<SpriteShapeSegment> GetSegments(int dataSize)
        {
            SetSegmentCount(dataSize);
            return GetNativeDataArray<SpriteShapeSegment>(SpriteShapeDataType.Segment);
        }

        /// <summary>
        /// Gets NativeArrays of SpriteShapeSegment.
        /// </summary>
        /// <param name="dataSize">Size of the NativeArray requested.</param>
        /// <param name="indices">NativeArray of indices.</param>
        /// <param name="vertices">NativeSlice of vertices.</param>
        /// <param name="texcoords">NativeSlice of texture coordinate for channel 0.</param>
        unsafe public void GetChannels(int dataSize, out NativeArray<ushort> indices, out NativeSlice<Vector3> vertices, out NativeSlice<Vector2> texcoords)
        {
            SetMeshDataCount(dataSize, dataSize);
            indices = GetNativeDataArray<ushort>(SpriteShapeDataType.Index);
            vertices = GetChannelDataArray<Vector3>(SpriteShapeDataType.ChannelVertex, VertexAttribute.Position);
            texcoords = GetChannelDataArray<Vector2>(SpriteShapeDataType.ChannelTexCoord0, VertexAttribute.TexCoord0);
        }

        /// <summary>
        /// Gets NativeArrays of SpriteShapeSegment.
        /// </summary>
        /// <param name="dataSize">Size of the NativeArray requested.</param>
        /// <param name="indices">NativeArray of indices.</param>
        /// <param name="vertices">NativeSlice of vertices.</param>
        /// <param name="texcoords">NativeSlice of texture coordinate for channel 0.</param>
        /// <param name="tangents">NativeSlice of tangents.</param>
        unsafe public void GetChannels(int dataSize, out NativeArray<ushort> indices, out NativeSlice<Vector3> vertices, out NativeSlice<Vector2> texcoords, out NativeSlice<Vector4> tangents)
        {
            SetMeshChannelInfo(dataSize, dataSize, (int)(1 << (int)VertexAttribute.Tangent));
            indices = GetNativeDataArray<ushort>(SpriteShapeDataType.Index);
            vertices = GetChannelDataArray<Vector3>(SpriteShapeDataType.ChannelVertex, VertexAttribute.Position);
            texcoords = GetChannelDataArray<Vector2>(SpriteShapeDataType.ChannelTexCoord0, VertexAttribute.TexCoord0);
            tangents = GetChannelDataArray<Vector4>(SpriteShapeDataType.ChannelTangent, VertexAttribute.Tangent);
        }

        /// <summary>
        /// Gets NativeArrays of SpriteShapeSegment.
        /// </summary>
        /// <param name="dataSize">Size of the NativeArray requested.</param>
        /// <param name="indices">NativeArray of indices.</param>
        /// <param name="vertices">NativeSlice of vertices.</param>
        /// <param name="texcoords">NativeSlice of texture coordinate for channel 0.</param>
        /// <param name="tangents">NativeSlice of tangents.</param>///
        /// <param name="normals">NativeSlice of normals.</param>
        unsafe public void GetChannels(int dataSize, out NativeArray<ushort> indices, out NativeSlice<Vector3> vertices, out NativeSlice<Vector2> texcoords, out NativeSlice<Vector4> tangents, out NativeSlice<Vector3> normals)
        {
            SetMeshChannelInfo(dataSize, dataSize, (int)((1 << (int)VertexAttribute.Normal) | (1 << (int)VertexAttribute.Tangent)));
            indices = GetNativeDataArray<ushort>(SpriteShapeDataType.Index);
            vertices = GetChannelDataArray<Vector3>(SpriteShapeDataType.ChannelVertex, VertexAttribute.Position);
            texcoords = GetChannelDataArray<Vector2>(SpriteShapeDataType.ChannelTexCoord0, VertexAttribute.TexCoord0);
            tangents = GetChannelDataArray<Vector4>(SpriteShapeDataType.ChannelTangent, VertexAttribute.Tangent);
            normals = GetChannelDataArray<Vector3>(SpriteShapeDataType.ChannelNormal, VertexAttribute.Normal);
        }
    }
}
