// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements.UIR
{
    class ShaderInfoStorage : IDisposable
    {
        [NoAutoStaticsCleanup] // monotonic counter for unique texture names; safe to persist
        static int s_TextureCounter;
        internal static readonly ProfilerMarker s_MarkerCopyTexture = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.ShaderInfoStorage.CopyTexture");
        internal static readonly ProfilerMarker s_MarkerGetTextureData = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.ShaderInfoStorage.GetTextureData");
        internal static readonly ProfilerMarker s_MarkerUpdateTexture = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.ShaderInfoStorage.UpdateTexture");

        readonly int m_InitialSize;
        readonly int m_MaxSize;

        UIRAtlasAllocator m_Allocator;

        Texture2D m_Texture;
        int m_CachedTextureWidth;
        NativeArray<Vector4> m_Texels; // Owned by the texture. Usable between GetRawTextureData and Apply.

        bool m_Dirty; // texel content changed since the last Apply
        bool m_CompareWritesNow; // skip-identical-writes switch; cleared on the first change, re-armed on upload

        public ShaderInfoStorage(int initialSize = 64, int maxSize = 4096)
        {
            Debug.Assert(maxSize <= SystemInfo.maxTextureSize);
            Debug.Assert(initialSize <= maxSize);
            Debug.Assert(Mathf.IsPowerOfTwo(initialSize));
            Debug.Assert(Mathf.IsPowerOfTwo(maxSize));

            m_InitialSize = initialSize;
            m_MaxSize = maxSize;
        }

        #region Dispose Pattern

        bool disposed { get; set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                UIRUtility.Destroy(m_Texture);
                m_Texture = null;
                m_Texels = new NativeArray<Vector4>();
                m_Allocator?.Dispose();
                m_Allocator = null;
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        public Texture2D texture => m_Texture;

        // True when texels were written since the last UpdateTexture (an upload is pending).
        public bool hasPendingChanges => m_Dirty;

        public bool AllocateRect(int width, int height, out RectInt uvs)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                uvs = new RectInt();
                return false;
            }

            if (m_Allocator == null)
                m_Allocator = new UIRAtlasAllocator(m_InitialSize, m_MaxSize, 0);

            if (!m_Allocator.TryAllocate(width, height, out uvs))
                return false;

            uvs = new RectInt(uvs.x, uvs.y, width, height);

            // We don't want to call this every time SetTexel is called and we don't expect AllocateRect to be called
            // often, so we create/expand the texture immediately. For this reason, a sensible initial size should be
            // set to avoid copies during the initialization phase, where multiple allocs may occur.
            CreateOrExpandTexture();

            return true;
        }

        // The caller must ensure that the texel has been allocated.
        // The coordinates are from the bottom-left corner.
        public void SetTexel(int x, int y, in Vector4 value)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            if (!m_Texels.IsCreated)
            {
                using (s_MarkerGetTextureData.Auto())
                {
                    m_Texels = m_Texture.GetRawTextureData<Vector4>();
                }
            }

            int index = x + y * m_CachedTextureWidth;

            // While comparing, identical writes don't dirty the storage. On the
            // first detected change the upload becomes inevitable, so comparing stops until the next
            // UpdateTexture restores it (single-bool check on the hot path).
            if (m_CompareWritesNow)
            {
                if (SameBits(value, m_Texels[index]))
                    return;
                m_CompareWritesNow = false;
            }

            m_Texels[index] = value;
            m_Dirty = true;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct TexelData
        {
            [FieldOffset(0)] public Vector4 v;
            [FieldOffset(0)] public ulong u0;
            [FieldOffset(8)] public ulong u1;
        }

        // Bit-level equality: texels hold arbitrary payloads (rects, indices, flags...), so the
        // compare must be exact — Vector4 == is approximate, and float == never matches a NaN
        // payload against itself, which would make its rewrites dirty the storage forever.
        static bool SameBits(in Vector4 a, in Vector4 b)
        {
            TexelData ta = default, tb = default;
            ta.v = a;
            tb.v = b;
            return ta.u0 == tb.u0 && ta.u1 == tb.u1;
        }

        public void UpdateTexture()
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            if (m_Texture == null || !m_Dirty)
                return;

            using (s_MarkerUpdateTexture.Auto())
            {
                m_Texture.Apply(false, false);
                m_Dirty = false;
                m_CompareWritesNow = true; // re-arm comparing for the next batch
                // The native array can't be used after Apply has been called. By reseting it, we implicitly set IsCreated
                // to false, which we use as the early-exit condition to prevent unnecessary calls to Apply.
                m_Texels = new NativeArray<Vector4>();
            }
        }

        void CreateOrExpandTexture()
        {
            int newWidth = m_Allocator.physicalWidth;
            int newHeight = m_Allocator.physicalHeight;

            bool copy = false;
            if (m_Texture != null)
            {
                if (m_Texture.width == newWidth && m_Texture.height == newHeight)
                    return;
                copy = true;
            }

            var newTexture = new Texture2D(m_Allocator.physicalWidth, m_Allocator.physicalHeight, TextureFormat.RGBAFloat, false)
            {
                name = "UIR Shader Info " + s_TextureCounter++,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point, // UUM-68128: Make sure to prevent any filtering and precision issues
            };

            if (copy)
            {
                using (s_MarkerCopyTexture.Auto())
                {
                    var oldTexels = m_Texels.IsCreated ? m_Texels : m_Texture.GetRawTextureData<Vector4>();
                    var newTexels = newTexture.GetRawTextureData<Vector4>();
                    CpuBlit(oldTexels, m_Texture.width, m_Texture.height, newTexels, newTexture.width, newTexture.height);
                    m_Texels = newTexels;
                }
            }
            else
                m_Texels = new NativeArray<Vector4>();

            // Content is initially considered dirty
            m_Dirty = true;
            m_CompareWritesNow = false;

            UIRUtility.Destroy(m_Texture);
            m_Texture = newTexture;
            m_CachedTextureWidth = m_Texture.width;
        }

        // The src and dst texels are laid out per row from the bottom-left.
        // We blit src into the bottom-left corner of dst.
        static void CpuBlit(NativeArray<Vector4> src, int srcWidth, int srcHeight, NativeArray<Vector4> dst, int dstWidth, int dstHeight)
        {
            Debug.Assert(dstWidth >= srcWidth && dstHeight >= srcHeight); // We only support expansion

            int widthDiff = dstWidth - srcWidth;
            int heightDiff = dstHeight - srcHeight;
            int srcCount = srcWidth * srcHeight;

            int srcIndex = 0;
            int dstIndex = 0;
            int srcBreak = srcWidth;
            while (srcIndex < srcCount)
            {
                while (srcIndex < srcBreak)
                {
                    dst[dstIndex] = src[srcIndex];
                    ++dstIndex;
                    ++srcIndex;
                }
                srcBreak += srcWidth;
                dstIndex += widthDiff; // Skip the extra columns from the destination
            }
        }
    }
}
