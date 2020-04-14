// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting.APIUpdating;

using Unity.Jobs;

namespace UnityEngine.U2D
{
    [NativeHeader("Runtime/2D/Common/SpriteDataAccess.h")]
    [NativeHeader("Runtime/2D/Common/SpriteDataMarshalling.h")]
    [MovedFrom("UnityEngine.Experimental.U2D")]
    [RequiredByNativeCode]
    [NativeType(CodegenOptions.Custom, "ScriptingSpriteBone")]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct SpriteBone
    {
        [SerializeField]
        [NativeNameAttribute("name")]
        string m_Name;
        [SerializeField]
        [NativeNameAttribute("position")]
        Vector3 m_Position;
        [SerializeField]
        [NativeNameAttribute("rotation")]
        Quaternion m_Rotation;
        [SerializeField]
        [NativeNameAttribute("length")]
        float m_Length;
        [SerializeField]
        [NativeNameAttribute("parentId")]
        int m_ParentId;

        public string name { get { return m_Name; } set { m_Name = value; } }
        public Vector3 position { get { return m_Position; } set { m_Position = value; } }
        public Quaternion rotation { get { return m_Rotation; } set { m_Rotation = value; } }
        public float length { get { return m_Length; } set { m_Length = value; } }
        public int parentId { get { return m_ParentId; } set { m_ParentId = value; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules]
    internal struct SpriteChannelInfo
    {
        [NativeNameAttribute("buffer")]
        IntPtr m_Buffer;
        [NativeNameAttribute("count")]
        int m_Count;
        [NativeNameAttribute("offset")]
        int m_Offset;
        [NativeNameAttribute("stride")]
        int m_Stride;

        unsafe public void* buffer { get { return (void*)m_Buffer; } set { m_Buffer = (IntPtr)value; } }
        public int count { get { return m_Count; } set { m_Count = value; } }
        public int offset { get { return m_Offset; } set { m_Offset = value; } }
        public int stride { get { return m_Stride; } set { m_Stride = value; } }
    }

    [NativeHeader("Runtime/Graphics/SpriteFrame.h")]
    [NativeHeader("Runtime/2D/Common/SpriteDataAccess.h")]
    public static class SpriteDataAccessExtensions
    {
        private static void CheckAttributeTypeMatchesAndThrow<T>(VertexAttribute channel)
        {
            var channelTypeMatches = false;
            switch (channel)
            {
                case VertexAttribute.Position:
                case VertexAttribute.Normal:
                    channelTypeMatches = typeof(T) == typeof(Vector3); break;
                case VertexAttribute.Tangent:
                    channelTypeMatches = typeof(T) == typeof(Vector4); break;
                case VertexAttribute.Color:
                    channelTypeMatches = typeof(T) == typeof(Color32); break;
                case VertexAttribute.TexCoord0:
                case VertexAttribute.TexCoord1:
                case VertexAttribute.TexCoord2:
                case VertexAttribute.TexCoord3:
                case VertexAttribute.TexCoord4:
                case VertexAttribute.TexCoord5:
                case VertexAttribute.TexCoord6:
                case VertexAttribute.TexCoord7:
                    channelTypeMatches = typeof(T) == typeof(Vector2); break;
                case VertexAttribute.BlendWeight:
                    channelTypeMatches = typeof(T) == typeof(BoneWeight); break;
                default:
                    throw new InvalidOperationException(String.Format("The requested channel '{0}' is unknown.", channel));
            }

            if (!channelTypeMatches)
                throw new InvalidOperationException(String.Format("The requested channel '{0}' does not match the return type {1}.", channel, typeof(T).Name));
        }

        public unsafe static NativeSlice<T> GetVertexAttribute<T>(this Sprite sprite, VertexAttribute channel) where T : struct
        {
            CheckAttributeTypeMatchesAndThrow<T>(channel);
            var info = GetChannelInfo(sprite, channel);
            var buffer = (byte*)(info.buffer) + info.offset;
            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(buffer, info.stride, info.count);
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, sprite.GetSafetyHandle());
            return slice;
        }

        unsafe public static void SetVertexAttribute<T>(this Sprite sprite, VertexAttribute channel, NativeArray<T> src) where T : struct
        {
            CheckAttributeTypeMatchesAndThrow<T>(channel);
            SetChannelData(sprite, channel, src.GetUnsafeReadOnlyPtr());
        }

        unsafe public static NativeArray<Matrix4x4> GetBindPoses(this Sprite sprite)
        {
            var info = GetBindPoseInfo(sprite);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Matrix4x4>(info.buffer, info.count, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, sprite.GetSafetyHandle());
            return arr;
        }

        unsafe public static void SetBindPoses(this Sprite sprite, NativeArray<Matrix4x4> src)
        {
            SetBindPoseData(sprite, src.GetUnsafeReadOnlyPtr(), src.Length);
        }

        unsafe public static NativeArray<ushort> GetIndices(this Sprite sprite)
        {
            var info = GetIndicesInfo(sprite);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ushort>(info.buffer, info.count, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, sprite.GetSafetyHandle());
            return arr;
        }

        unsafe public static void SetIndices(this Sprite sprite, NativeArray<ushort> src)
        {
            SetIndicesData(sprite, src.GetUnsafeReadOnlyPtr(), src.Length);
        }

        public static SpriteBone[] GetBones(this Sprite sprite)
        {
            return GetBoneInfo(sprite);
        }

        public static void SetBones(this Sprite sprite, SpriteBone[] src)
        {
            SetBoneData(sprite, src);
        }

        [NativeName("HasChannel")]
        extern public static bool HasVertexAttribute([NotNull] this Sprite sprite, VertexAttribute channel);

        // The only way to change the vertex count
        extern public static void SetVertexCount([NotNull] this Sprite sprite, int count);
        extern public static int GetVertexCount([NotNull] this Sprite sprite);

        // This lenght is not tied to vertexCount
        extern private static SpriteChannelInfo GetBindPoseInfo([NotNull] Sprite sprite);
        unsafe extern private static void SetBindPoseData([NotNull] Sprite sprite, void* src, int count);

        extern private static SpriteChannelInfo GetIndicesInfo([NotNull] Sprite sprite);
        unsafe extern private static void SetIndicesData([NotNull] Sprite sprite, void* src, int count);

        extern private static SpriteChannelInfo GetChannelInfo([NotNull] Sprite sprite, VertexAttribute channel);
        unsafe extern private static void SetChannelData([NotNull] Sprite sprite, VertexAttribute channel, void* src);

        extern private static SpriteBone[] GetBoneInfo([NotNull] Sprite sprite);
        extern private static void SetBoneData([NotNull] Sprite sprite, SpriteBone[] src);

        extern internal static int GetPrimaryVertexStreamSize(Sprite sprite);

        extern internal static AtomicSafetyHandle GetSafetyHandle([NotNull] this Sprite sprite);
    }

    [NativeHeader("Runtime/2D/Common/SpriteDataAccess.h")]
    [NativeHeader("Runtime/Graphics/Mesh/SpriteRenderer.h")]
    public static class SpriteRendererDataAccessExtensions
    {
        internal unsafe static void SetDeformableBuffer(this SpriteRenderer spriteRenderer, NativeArray<byte> src)
        {
            if (spriteRenderer.sprite == null)
                throw new ArgumentException(String.Format("spriteRenderer does not have a valid sprite set."));

            if (src.Length != SpriteDataAccessExtensions.GetPrimaryVertexStreamSize(spriteRenderer.sprite))
                throw new InvalidOperationException(String.Format("custom sprite vertex data size must match sprite asset's vertex data size {0} {1}", src.Length, SpriteDataAccessExtensions.GetPrimaryVertexStreamSize(spriteRenderer.sprite)));

            SetDeformableBuffer(spriteRenderer, src.GetUnsafeReadOnlyPtr(), src.Length);
        }

        internal unsafe static void SetDeformableBuffer(this SpriteRenderer spriteRenderer, NativeArray<Vector3> src)
        {
            if (spriteRenderer.sprite == null)
                throw new InvalidOperationException("spriteRenderer does not have a valid sprite set.");

            if (src.Length != spriteRenderer.sprite.GetVertexCount())
                throw new InvalidOperationException(String.Format("The src length {0} must match the vertex count of source Sprite {1}.", src.Length, spriteRenderer.sprite.GetVertexCount()));

            SetDeformableBuffer(spriteRenderer, src.GetUnsafeReadOnlyPtr(), src.Length);
        }

        internal unsafe static void SetBatchDeformableBufferAndLocalAABBArray(SpriteRenderer[] spriteRenderers, NativeArray<IntPtr> buffers, NativeArray<int> bufferSizes, NativeArray<Bounds> bounds)
        {
            int count = spriteRenderers.Length;
            if (count != buffers.Length
                || count != bufferSizes.Length
                || count != bounds.Length)
            {
                throw new ArgumentException("Input array sizes are not the same.");
            }

            SetBatchDeformableBufferAndLocalAABBArray(spriteRenderers, buffers.GetUnsafeReadOnlyPtr(), bufferSizes.GetUnsafeReadOnlyPtr(), bounds.GetUnsafeReadOnlyPtr(), count);
        }

        extern public static void DeactivateDeformableBuffer([NotNull] this SpriteRenderer renderer);
        extern internal static void SetLocalAABB([NotNull] this SpriteRenderer renderer, Bounds aabb);
        extern private unsafe static void SetDeformableBuffer([NotNull] SpriteRenderer spriteRenderer, void* src, int count);

        extern private unsafe static void SetBatchDeformableBufferAndLocalAABBArray(SpriteRenderer[] spriteRenderers, void* buffers, void* bufferSizes, void* bounds, int count);
    }
}
