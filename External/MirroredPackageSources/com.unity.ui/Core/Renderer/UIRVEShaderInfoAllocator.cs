using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    internal enum OwnedState : byte
    {
        Inherited = 0,
        Owned = 1,
    }

    internal struct BMPAlloc
    {
        public static readonly BMPAlloc Invalid = new BMPAlloc() { page = -1 };
        public bool Equals(BMPAlloc other) { return page == other.page && pageLine == other.pageLine && bitIndex == other.bitIndex; }
        public bool IsValid() { return page >= 0; }
        public override string ToString() { return string.Format("{0},{1},{2}", page, pageLine, bitIndex); }

        public int page;
        public ushort pageLine;
        public byte bitIndex;
        public OwnedState ownedState;
    }

    // The BitmapAllocator32 always scans for allocations from the first page and upwards.
    // Thus if a returned allocation is at a certain location, it is guaranteed that all preceding
    // locations are occupied. This property is relied on in UIRVEShaderInfoAllocator below to report
    // OOM when the allocation returned exceeds the allowed constant buffer size but fits in a BMPAlloc page.
    // This allocator is not multi-threading safe.
    internal struct BitmapAllocator32
    {
        struct Page
        {
            public UInt16 x, y; // Location of this page in the atlas. These coordinates are the top-left corner of the page.
            public int freeSlots;
        }

        // Bits represented in lines of 32-bit ints
        public const int kPageWidth = 32; // Must match bit count of the type of m_AllocMap
        int m_PageHeight;
        List<Page> m_Pages;
        List<UInt32> m_AllocMap; // Each page takes kPageHeight sequential entries/lines in this array, 0 is allocated, 1 is available
        int m_EntryWidth, m_EntryHeight;

        public void Construct(int pageHeight, int entryWidth = 1, int entryHeight = 1)
        {
            m_PageHeight = pageHeight;
            m_Pages = new List<Page>(1);
            m_AllocMap = new List<UInt32>(m_PageHeight * m_Pages.Capacity);
            m_EntryWidth = entryWidth;
            m_EntryHeight = entryHeight;
        }

        public void ForceFirstAlloc(ushort firstPageX, ushort firstPageY)
        {
            m_AllocMap.Add(0xFFFFFFFE); // Reserve first slot
            for (int i = 1; i < m_PageHeight; i++)
                m_AllocMap.Add(0xFFFFFFFF);
            m_Pages.Add(new Page() { x = firstPageX, y = firstPageY, freeSlots = kPageWidth * m_PageHeight - 1 });
        }

        public BMPAlloc Allocate(UIRAtlasManager atlasManager)
        {
            int pageCount = m_Pages.Count;
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                var pageInfo = m_Pages[pageIndex];
                if (pageInfo.freeSlots == 0)
                    continue;

                int line = pageIndex * m_PageHeight;
                int endLine = line + m_PageHeight;
                for (; line < endLine; line++)
                {
                    var allocBits = m_AllocMap[line];
                    if (allocBits == 0)
                        continue;
                    byte allocIndex = CountTrailingZeroes(allocBits);
                    m_AllocMap[line] = allocBits & (~(1U << allocIndex));
                    pageInfo.freeSlots--;
                    m_Pages[pageIndex] = pageInfo;
                    return new BMPAlloc() { page = pageIndex, pageLine = (ushort)(line - pageIndex * m_PageHeight), bitIndex = allocIndex, ownedState = OwnedState.Owned };
                } // For each line
            } // For each page

            RectInt uvRect;
            if ((atlasManager == null) || !atlasManager.AllocateRect(kPageWidth * m_EntryWidth, m_PageHeight * m_EntryHeight, out uvRect))
                return BMPAlloc.Invalid;

            m_AllocMap.Capacity += m_PageHeight;
            m_AllocMap.Add(0xFFFFFFFE); // Reserve first slot
            for (int i = 1; i < m_PageHeight; i++)
                m_AllocMap.Add(0xFFFFFFFF);

            m_Pages.Add(new Page() { x = (UInt16)uvRect.xMin, y = (UInt16)uvRect.yMin, freeSlots = kPageWidth * m_PageHeight - 1 });
            return new BMPAlloc() { page = m_Pages.Count - 1, ownedState = OwnedState.Owned };
        }

        public void Free(BMPAlloc alloc)
        {
            Debug.Assert(alloc.ownedState == OwnedState.Owned);
            int line = alloc.page * m_PageHeight + alloc.pageLine;
            m_AllocMap[line] = m_AllocMap[line] | (1U << alloc.bitIndex);
            var page = m_Pages[alloc.page];
            page.freeSlots++;
            m_Pages[alloc.page] = page;
        }

        public int entryWidth { get { return m_EntryWidth; } }
        public int entryHeight { get { return m_EntryHeight; } }

        internal void GetAllocPageAtlasLocation(int page, out UInt16 x, out UInt16 y) { var p = m_Pages[page]; x = p.x; y = p.y; }

        static byte CountTrailingZeroes(UInt32 val)
        {
            // This entire function is implemented on hardware in one instruction... sigh
            byte trailingZeroes = 0;
            if ((val & 0xFFFF) == 0)
            {
                val >>= 16;
                trailingZeroes = 16;
            }

            if ((val & 0xFF) == 0)
            {
                val >>= 8;
                trailingZeroes += 8;
            }

            if ((val & 0xF) == 0)
            {
                val >>= 4;
                trailingZeroes += 4;
            }

            if ((val & 3) == 0)
            {
                val >>= 2;
                trailingZeroes += 2;
            }

            if ((val & 1) == 0)
                trailingZeroes += 1;

            return trailingZeroes;
        }
    }

    internal struct UIRVEShaderInfoAllocator
    {
        UIRAtlasManager m_Atlas;
        BitmapAllocator32 m_TransformAllocator, m_ClipRectAllocator, m_OpacityAllocator; // All allocators take pages from the same atlas
        bool m_AtlasReallyCreated;

        // Support for absence of vertex texturing
        bool m_VertexTexturingEnabled;
        NativeArray<Transform3x4> m_Transforms;
        NativeArray<Vector4> m_ClipRects;

        static int pageWidth { get { return BitmapAllocator32.kPageWidth; } }
        static int pageHeight { get { return 8; } } // 32*8 = 256, can be stored in a byte

        // The page coordinates correspond to the atlas's internal algorithm's results.
        // If that algorithm changes, the new results must be put here to match
        internal static readonly Vector2Int identityTransformTexel = new Vector2Int(0, 0);
        internal static readonly Vector2Int infiniteClipRectTexel = new Vector2Int(0, 32);
        internal static readonly Vector2Int fullOpacityTexel = new Vector2Int(32, 32);

        internal static readonly Vector4 identityTransformRow0Value = new Vector4(1, 0, 0, 0);
        internal static readonly Vector4 identityTransformRow1Value = new Vector4(0, 1, 0, 0);
        internal static readonly Vector4 identityTransformRow2Value = new Vector4(0, 0, 1, 0);
        internal static readonly Vector4 infiniteClipRectValue = new Vector4(-float.MaxValue, -float.MaxValue, float.MaxValue, float.MaxValue);
        internal static readonly Vector4 fullOpacityValue = new Vector4(1, 1, 1, 1);

        // Default allocations. All their members are 0 including "owned"
#pragma warning disable 649
        public static readonly BMPAlloc identityTransform, infiniteClipRect, fullOpacity;
#pragma warning restore 649

        static Vector2Int AllocToTexelCoord(ref BitmapAllocator32 allocator, BMPAlloc alloc)
        {
            UInt16 x, y;
            allocator.GetAllocPageAtlasLocation(alloc.page, out x, out y);
            return new Vector2Int(
                alloc.bitIndex * allocator.entryWidth + x,
                alloc.pageLine * allocator.entryHeight + y);
        }

        static int AllocToConstantBufferIndex(BMPAlloc alloc)
        {
            return alloc.pageLine * pageWidth + alloc.bitIndex;
        }

        static bool AtlasRectMatchesPage(ref BitmapAllocator32 allocator, BMPAlloc defAlloc, RectInt atlasRect)
        {
            UInt16 x, y;
            allocator.GetAllocPageAtlasLocation(defAlloc.page, out x, out y);
            return (x == atlasRect.xMin) && (y == atlasRect.yMin) &&
                (allocator.entryWidth * pageWidth == atlasRect.width) &&
                (allocator.entryHeight * pageHeight == atlasRect.height);
        }

        public NativeSlice<Transform3x4> transformConstants { get { return m_Transforms; } }
        public NativeSlice<Vector4> clipRectConstants { get { return m_ClipRects; } }
        public Texture atlas
        {
            get
            {
                if (m_AtlasReallyCreated)
                    return m_Atlas.atlas;
                return m_VertexTexturingEnabled ? UIRenderDevice.defaultShaderInfoTexFloat : UIRenderDevice.defaultShaderInfoTexARGB8;
            }
        }
        public bool internalAtlasCreated { get { return m_AtlasReallyCreated; } } // For diagnostics really

        public bool isReleased { get { return m_AtlasReallyCreated && m_Atlas?.IsReleased() == true; } }

        public void Construct()
        {
            // The default allocs refer to three startup pages to be allocated as below from the atlas
            // once the atlas is used for the first time. The page coordinates correspond to the atlas's
            // internal algorithm's results. If that algorithm changes, the new results must be put here to match
            m_OpacityAllocator = m_ClipRectAllocator = m_TransformAllocator = new BitmapAllocator32();
            m_TransformAllocator.Construct(pageHeight, 1, 3);
            m_TransformAllocator.ForceFirstAlloc((ushort)identityTransformTexel.x, (ushort)identityTransformTexel.y);
            m_ClipRectAllocator.Construct(pageHeight);
            m_ClipRectAllocator.ForceFirstAlloc((ushort)infiniteClipRectTexel.x, (ushort)infiniteClipRectTexel.y);
            m_OpacityAllocator.Construct(pageHeight);
            m_OpacityAllocator.ForceFirstAlloc((ushort)fullOpacityTexel.x, (ushort)fullOpacityTexel.y);

            m_VertexTexturingEnabled = UIRenderDevice.vertexTexturingIsAvailable;
            if (!m_VertexTexturingEnabled)
            {
                int constantCount = 20; // Once custom materials are exposed, this number must be parameterized
                // Note that we can't do a lazy late allocation on the constants array size here (e.g. allocate only the default entry)
                // because once the material receives an array parameter value for the first time, it clings to its size and never
                // allows size changes afterwards without recreating the material, so we need to get the size right pessimistically
                m_Transforms = new NativeArray<Transform3x4>(constantCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                m_ClipRects = new NativeArray<Vector4>(constantCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                m_Transforms[0] = new Transform3x4() { v0 = identityTransformRow0Value, v1 = identityTransformRow1Value, v2 = identityTransformRow2Value };
                m_ClipRects[0] = infiniteClipRectValue;
            }
        }

        void ReallyCreateAtlas()
        {
            m_Atlas = new UIRAtlasManager(
                m_VertexTexturingEnabled ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGB32, // If no vertex texturing, only store opacity in ARGB32
                FilterMode.Point, // Filtering is never needed for this texture
                Math.Max(pageWidth, pageHeight * 3), // Each transform row is stored vertically
                64); // Because we want predictable placement of first pages, 64 will fit all three default allocs

            // The order of allocation from the atlas below is important. See the comment at the beginning of Construct().
            RectInt rcTransform, rcClipRect, rcOpacity;
            m_Atlas.AllocateRect(pageWidth * m_TransformAllocator.entryWidth, pageHeight * m_TransformAllocator.entryHeight, out rcTransform);
            m_Atlas.AllocateRect(pageWidth * m_ClipRectAllocator.entryWidth, pageHeight * m_ClipRectAllocator.entryHeight, out rcClipRect);
            m_Atlas.AllocateRect(pageWidth * m_OpacityAllocator.entryWidth, pageHeight * m_OpacityAllocator.entryHeight, out rcOpacity);

            if (!AtlasRectMatchesPage(ref m_TransformAllocator, identityTransform, rcTransform))
                throw new Exception("Atlas identity transform allocation failed unexpectedly");

            if (!AtlasRectMatchesPage(ref m_ClipRectAllocator, infiniteClipRect, rcClipRect))
                throw new Exception("Atlas infinite clip rect allocation failed unexpectedly");

            if (!AtlasRectMatchesPage(ref m_OpacityAllocator, fullOpacity, rcOpacity))
                throw new Exception("Atlas full opacity allocation failed unexpectedly");

            var whiteTexel = UIRenderDevice.whiteTexel;
            if (m_VertexTexturingEnabled)
            {
                var allocXY = AllocToTexelCoord(ref m_TransformAllocator, identityTransform);
                m_Atlas.EnqueueBlit(whiteTexel, allocXY.x, allocXY.y + 0, false, identityTransformRow0Value);
                m_Atlas.EnqueueBlit(whiteTexel, allocXY.x, allocXY.y + 1, false, identityTransformRow1Value);
                m_Atlas.EnqueueBlit(whiteTexel, allocXY.x, allocXY.y + 2, false, identityTransformRow2Value);

                allocXY = AllocToTexelCoord(ref m_ClipRectAllocator, infiniteClipRect);
                m_Atlas.EnqueueBlit(whiteTexel, allocXY.x, allocXY.y, false, infiniteClipRectValue);
            }
            {
                var allocXY = AllocToTexelCoord(ref m_OpacityAllocator, fullOpacity);
                m_Atlas.EnqueueBlit(whiteTexel, allocXY.x, allocXY.y, false, fullOpacityValue);
            }

            m_AtlasReallyCreated = true;
        }

        public void Dispose()
        {
            if (m_Atlas != null)
                m_Atlas.Dispose();
            m_Atlas = null;
            if (m_ClipRects.IsCreated)
                m_ClipRects.Dispose();
            if (m_Transforms.IsCreated)
                m_Transforms.Dispose();
            m_AtlasReallyCreated = false;
        }

        public void IssuePendingAtlasBlits()
        {
            m_Atlas?.Commit();
        }

        public BMPAlloc AllocTransform()
        {
            if (!m_AtlasReallyCreated)
                ReallyCreateAtlas();

            if (m_VertexTexturingEnabled)
                return m_TransformAllocator.Allocate(m_Atlas);

            var alloc = m_TransformAllocator.Allocate(null); // Don't want to allow new pages

            // If the returned alloc address fits within the constant buffer then succeed, otherwise fail
            if (AllocToConstantBufferIndex(alloc) < m_Transforms.Length)
                return alloc;
            m_TransformAllocator.Free(alloc); // Not really necessary, but feels cleaner
            return BMPAlloc.Invalid;
        }

        public BMPAlloc AllocClipRect()
        {
            if (!m_AtlasReallyCreated)
                ReallyCreateAtlas();

            if (m_VertexTexturingEnabled)
                return m_ClipRectAllocator.Allocate(m_Atlas);

            var alloc = m_ClipRectAllocator.Allocate(null); // Don't want to allow new pages

            // If the returned alloc address fits within the constant buffer then succeed, otherwise fail
            if (AllocToConstantBufferIndex(alloc) < m_ClipRects.Length)
                return alloc;
            m_ClipRectAllocator.Free(alloc); // Not really necessary, but feels cleaner
            return BMPAlloc.Invalid;
        }

        public BMPAlloc AllocOpacity()
        {
            if (!m_AtlasReallyCreated)
                ReallyCreateAtlas();

            return m_OpacityAllocator.Allocate(m_Atlas);
        }

        public void SetTransformValue(BMPAlloc alloc, Matrix4x4 xform)
        {
            Debug.Assert(alloc.IsValid());
            if (m_VertexTexturingEnabled)
            {
                var allocXY = AllocToTexelCoord(ref m_TransformAllocator, alloc);
                m_Atlas.EnqueueBlit(UIRenderDevice.whiteTexel, allocXY.x, allocXY.y + 0, false, xform.GetRow(0));
                m_Atlas.EnqueueBlit(UIRenderDevice.whiteTexel, allocXY.x, allocXY.y + 1, false, xform.GetRow(1));
                m_Atlas.EnqueueBlit(UIRenderDevice.whiteTexel, allocXY.x, allocXY.y + 2, false, xform.GetRow(2));
            }
            else
                m_Transforms[AllocToConstantBufferIndex(alloc)] = new Transform3x4()
                {
                    v0 = xform.GetRow(0),
                    v1 = xform.GetRow(1),
                    v2 = xform.GetRow(2)
                };
        }

        public void SetClipRectValue(BMPAlloc alloc, Vector4 clipRect)
        {
            Debug.Assert(alloc.IsValid());
            if (m_VertexTexturingEnabled)
            {
                var allocXY = AllocToTexelCoord(ref m_ClipRectAllocator, alloc);
                m_Atlas.EnqueueBlit(UIRenderDevice.whiteTexel, allocXY.x, allocXY.y, false, clipRect);
            }
            else m_ClipRects[AllocToConstantBufferIndex(alloc)] = clipRect;
        }

        public void SetOpacityValue(BMPAlloc alloc, float opacity)
        {
            Debug.Assert(alloc.IsValid());
            var allocXY = AllocToTexelCoord(ref m_OpacityAllocator, alloc);
            m_Atlas.EnqueueBlit(UIRenderDevice.whiteTexel, allocXY.x, allocXY.y, false, new Color(1, 1, 1, opacity));
        }

        public void FreeTransform(BMPAlloc alloc)
        {
            Debug.Assert(alloc.IsValid());
            m_TransformAllocator.Free(alloc);
        }

        public void FreeClipRect(BMPAlloc alloc)
        {
            Debug.Assert(alloc.IsValid());
            m_ClipRectAllocator.Free(alloc);
        }

        public void FreeOpacity(BMPAlloc alloc)
        {
            Debug.Assert(alloc.IsValid());
            m_OpacityAllocator.Free(alloc);
        }

        public Color32 TransformAllocToVertexData(BMPAlloc alloc)
        {
            Debug.Assert(pageWidth == 32 && pageHeight == 8); // Match the bit-shift values below for fast integer division
            UInt16 x = 0, y = 0;
            if (m_VertexTexturingEnabled)
                m_TransformAllocator.GetAllocPageAtlasLocation(alloc.page, out x, out y);
            return new Color32((byte)(x >> 5), (byte)(y >> 3), (byte)(alloc.pageLine * pageWidth + alloc.bitIndex), 0);
        }

        public Color32 ClipRectAllocToVertexData(BMPAlloc alloc)
        {
            Debug.Assert(pageWidth == 32 && pageHeight == 8); // Match the bit-shift values below for fast integer division
            UInt16 x = 0, y = 0;
            if (m_VertexTexturingEnabled)
                m_ClipRectAllocator.GetAllocPageAtlasLocation(alloc.page, out x, out y);
            return new Color32((byte)(x >> 5), (byte)(y >> 3), (byte)(alloc.pageLine * pageWidth + alloc.bitIndex), 0);
        }

        public Color32 OpacityAllocToVertexData(BMPAlloc alloc)
        {
            Debug.Assert(pageWidth == 32 && pageHeight == 8); // Match the bit-shift values below for fast integer division
            UInt16 x, y;
            m_OpacityAllocator.GetAllocPageAtlasLocation(alloc.page, out x, out y);
            return new Color32((byte)(x >> 5), (byte)(y >> 3), (byte)(alloc.pageLine * pageWidth + alloc.bitIndex), 0);
        }
    }
}
