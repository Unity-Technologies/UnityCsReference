// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    abstract class BaseShaderInfoStorage : IDisposable
    {
        protected static int s_TextureCounter;
        internal static ProfilerMarker s_MarkerCopyTexture = new ProfilerMarker("UIR.ShaderInfoStorage.CopyTexture");
        internal static ProfilerMarker s_MarkerGetTextureData = new ProfilerMarker("UIR.ShaderInfoStorage.GetTextureData");
        internal static ProfilerMarker s_MarkerUpdateTexture = new ProfilerMarker("UIR.ShaderInfoStorage.UpdateTexture");

        public abstract Texture2D texture { get; }
        public abstract bool AllocateRect(int width, int height, out RectInt uvs);
        public abstract void SetTexel(int x, int y, Color color);
        public abstract void UpdateTexture();

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (!disposing)
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }

    class ShaderInfoStorage<T> : BaseShaderInfoStorage where T : struct
    {
        readonly int m_InitialSize;
        readonly int m_MaxSize;
        readonly TextureFormat m_Format;
        readonly Func<Color, T> m_Convert;

        UIRAtlasAllocator m_Allocator;

        Texture2D m_Texture;
        NativeArray<T> m_Texels; // Owned by the texture. Usable between GetRawTextureData and Apply.

        public ShaderInfoStorage(TextureFormat format, Func<Color, T> convert, int initialSize = 64, int maxSize = 4096)
        {
            Debug.Assert(maxSize <= SystemInfo.maxTextureSize);
            Debug.Assert(initialSize <= maxSize);
            Debug.Assert(Mathf.IsPowerOfTwo(initialSize));
            Debug.Assert(Mathf.IsPowerOfTwo(maxSize));
            Debug.Assert(convert != null);

            m_InitialSize = initialSize;
            m_MaxSize = maxSize;
            m_Format = format;
            m_Convert = convert;
        }

        #region Dispose Pattern

        protected override void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                UIRUtility.Destroy(m_Texture);
                m_Texture = null;
                m_Texels = new NativeArray<T>();
                m_Allocator?.Dispose();
                m_Allocator = null;
            }

            base.Dispose(disposing);
        }

        #endregion // Dispose Pattern

        public override Texture2D texture => m_Texture;

        public override bool AllocateRect(int width, int height, out RectInt uvs)
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
        public override void SetTexel(int x, int y, Color color)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            if (!m_Texels.IsCreated)
            {
                s_MarkerGetTextureData.Begin();
                m_Texels = m_Texture.GetRawTextureData<T>();
                s_MarkerGetTextureData.End();
            }

            m_Texels[x + y * m_Texture.width] = m_Convert(color);
        }

        public override void UpdateTexture()
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            if (m_Texture == null || !m_Texels.IsCreated)
                return;

            s_MarkerUpdateTexture.Begin();
            m_Texture.Apply(false, false);
            // The native array can't be used after Apply has been called. By reseting it, we implicitly set IsCreated
            // to false, which we use as the early-exit condition to prevent unnecessary calls to Apply.
            m_Texels = new NativeArray<T>();
            s_MarkerUpdateTexture.End();
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

            var newTexture = new Texture2D(m_Allocator.physicalWidth, m_Allocator.physicalHeight, m_Format, false)
            {
                name = "UIR Shader Info " + s_TextureCounter++,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point, // UUM-68128: Make sure to prevent any filtering and precision issues
            };

            if (copy)
            {
                s_MarkerCopyTexture.Begin();
                var oldTexels = m_Texels.IsCreated ? m_Texels : m_Texture.GetRawTextureData<T>();
                var newTexels = newTexture.GetRawTextureData<T>();
                CpuBlit(oldTexels, m_Texture.width, m_Texture.height, newTexels, newTexture.width, newTexture.height);
                m_Texels = newTexels;
                s_MarkerCopyTexture.End();
            }
            else
                m_Texels = new NativeArray<T>();

            UIRUtility.Destroy(m_Texture);
            m_Texture = newTexture;
        }

        // The src and dst texels are laid out per row from the bottom-left.
        // We blit src into the bottom-left corner of dst.
        static void CpuBlit(NativeArray<T> src, int srcWidth, int srcHeight, NativeArray<T> dst, int dstWidth, int dstHeight)
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

    class ShaderInfoStorageRGBA32 : ShaderInfoStorage<Color32>
    {
        static readonly Func<Color, Color32> s_Convert = c => c;

        public ShaderInfoStorageRGBA32(int initialSize = 64, int maxSize = 4096) :
            base(TextureFormat.RGBA32, s_Convert, initialSize, maxSize)
        {
        }
    }

    class ShaderInfoStorageRGBAFloat : ShaderInfoStorage<Color>
    {
        static readonly Func<Color, Color> s_Convert = c => c;

        public ShaderInfoStorageRGBAFloat(int initialSize = 64, int maxSize = 4096) :
            base(TextureFormat.RGBAFloat, s_Convert, initialSize, maxSize)
        {
        }
    }
}
