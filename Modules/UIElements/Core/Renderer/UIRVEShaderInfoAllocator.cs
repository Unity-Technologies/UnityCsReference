// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;

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
        public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", page, pageLine, bitIndex); }

        public int page;
        public ushort pageLine;
        public byte bitIndex;
        public OwnedState ownedState;
    }

    // The BitmapAllocator32 always scans for allocations from the first page and upwards.
    // Thus if a returned allocation is at a certain location, it is guaranteed that all preceding
    // locations are occupied. This property is relied on in ShaderInfoAllocator below to report
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

        public BMPAlloc Allocate(BaseShaderInfoStorage storage)
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
            if ((storage == null) || !storage.AllocateRect(kPageWidth * m_EntryWidth, m_PageHeight * m_EntryHeight, out uvRect))
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
        public int pageCount { get { return m_Pages.Count; } }

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

    class ShaderInfoAllocator
    {
        BaseShaderInfoStorage m_Storage;
        BitmapAllocator32 m_TransformAllocator, m_ClipRectAllocator, m_OpacityAllocator, m_ColorAllocator, m_TextSettingsAllocator; // All allocators take pages from the same storage
        bool m_StorageReallyCreated;
        ColorSpace m_ColorSpace;

        // Per-allocator-type page-position tables. Each entry is the atlas (x,y) of the page's
        // top-left corner. .zw are padding
        public const int kMaxPages = 32;
        readonly Vector4[] m_XformPagePos = new Vector4[kMaxPages];
        readonly Vector4[] m_ClipPagePos = new Vector4[kMaxPages];
        readonly Vector4[] m_OpacityPagePos = new Vector4[kMaxPages];
        readonly Vector4[] m_ColorPagePos = new Vector4[kMaxPages];
        readonly Vector4[] m_TextCorePagePos = new Vector4[kMaxPages];
        bool m_TransformPagesErrored, m_ClipRectPagesErrored, m_OpacityPagesErrored, m_ColorPagesErrored, m_TextCorePagesErrored;

        internal Vector4[] transformPagePositions { get { return m_XformPagePos; } }
        internal Vector4[] clipRectPagePositions { get { return m_ClipPagePos; } }
        internal Vector4[] opacityPagePositions { get { return m_OpacityPagePos; } }
        internal Vector4[] colorPagePositions { get { return m_ColorPagePos; } }
        internal Vector4[] textCorePagePositions { get { return m_TextCorePagePos; } }

        // Returns the underlying allocator by value. BitmapAllocator32 holds a List<Page>,
        // so the copy shares storage with the original — safe for read-only inspection.
        public static class Testing
        {
            public static BitmapAllocator32 GetTransformAllocator(ShaderInfoAllocator a) => a.m_TransformAllocator;
            public static BitmapAllocator32 GetClipRectAllocator(ShaderInfoAllocator a) => a.m_ClipRectAllocator;
            public static BitmapAllocator32 GetOpacityAllocator(ShaderInfoAllocator a) => a.m_OpacityAllocator;
            public static BitmapAllocator32 GetColorAllocator(ShaderInfoAllocator a) => a.m_ColorAllocator;
            public static BitmapAllocator32 GetTextSettingsAllocator(ShaderInfoAllocator a) => a.m_TextSettingsAllocator;
        }

        static int pageWidth { get { return BitmapAllocator32.kPageWidth; } }
        static int pageHeight { get { return 8; } } // 32*8 = 256, can be stored in a byte

        // The page coordinates correspond to the atlas's internal algorithm's results.
        // If that algorithm changes, the new results must be put here to match
        static readonly Vector2Int identityTransformTexel = new Vector2Int(0, 0);
        static readonly Vector2Int infiniteClipRectTexel = new Vector2Int(0, 32);
        static readonly Vector2Int fullOpacityTexel = new Vector2Int(32, 32);
        static readonly Vector2Int clearColorTexel = new Vector2Int(0, 40);
        static readonly Vector2Int defaultTextCoreSettingsTexel = new Vector2Int(32, 0);

        static readonly Matrix4x4 identityTransformValue = Matrix4x4.identity;
        static readonly Vector4 identityTransformRow0Value = identityTransformValue.GetRow(0);
        static readonly Vector4 identityTransformRow1Value = identityTransformValue.GetRow(1);
        static readonly Vector4 identityTransformRow2Value = identityTransformValue.GetRow(2);
        static readonly Vector4 infiniteClipRectValue = new Vector4(0, 0, 0, 0);
        static readonly Vector4 fullOpacityValue = new Vector4(1, 1, 1, 1);
        static readonly Vector4 clearColorValue = new Vector4(0, 0, 0, 0);
        static readonly TextCoreSettings defaultTextCoreSettingsValue = new TextCoreSettings() {
            faceColor = Color.white,
            outlineColor = Color.clear,
            outlineWidth = 0.0f,
            underlayColor = Color.clear,
            underlayOffset = Vector2.zero,
            underlaySoftness = 0.0f
        };

        // Default allocations. All their members are 0 including "owned"
#pragma warning disable 649
        public static readonly BMPAlloc identityTransform, infiniteClipRect, fullOpacity, clearColor, defaultTextCoreSettings;
#pragma warning restore 649

        static int s_DefaultShaderInfoTextureRefCount;
        static Texture2D s_DefaultShaderInfoTexture;
        static void AcquireDefaultShaderInfoTexture()
        {
            if (++s_DefaultShaderInfoTextureRefCount == 1)
            {
                s_DefaultShaderInfoTexture = new Texture2D(64, 64, TextureFormat.RGBAFloat, false); // No mips
                s_DefaultShaderInfoTexture.name = "DefaultShaderInfoTexFloat";
                s_DefaultShaderInfoTexture.hideFlags = HideFlags.HideAndDontSave;
                s_DefaultShaderInfoTexture.filterMode = FilterMode.Point;
                s_DefaultShaderInfoTexture.SetPixel(identityTransformTexel.x, identityTransformTexel.y + 0, identityTransformRow0Value);
                s_DefaultShaderInfoTexture.SetPixel(identityTransformTexel.x, identityTransformTexel.y + 1, identityTransformRow1Value);
                s_DefaultShaderInfoTexture.SetPixel(identityTransformTexel.x, identityTransformTexel.y + 2, identityTransformRow2Value);
                s_DefaultShaderInfoTexture.SetPixel(infiniteClipRectTexel.x, infiniteClipRectTexel.y, infiniteClipRectValue);
                s_DefaultShaderInfoTexture.SetPixel(fullOpacityTexel.x, fullOpacityTexel.y, fullOpacityValue);
                s_DefaultShaderInfoTexture.SetPixel(defaultTextCoreSettingsTexel.x, defaultTextCoreSettingsTexel.y + 0, Color.white);
                s_DefaultShaderInfoTexture.SetPixel(defaultTextCoreSettingsTexel.x, defaultTextCoreSettingsTexel.y + 1, Color.clear);
                s_DefaultShaderInfoTexture.SetPixel(defaultTextCoreSettingsTexel.x, defaultTextCoreSettingsTexel.y + 2, Color.clear);
                s_DefaultShaderInfoTexture.SetPixel(defaultTextCoreSettingsTexel.x, defaultTextCoreSettingsTexel.y + 3, Color.clear);
                s_DefaultShaderInfoTexture.Apply(false, true);
            }
        }

        static void ReleaseDefaultShaderInfoTexture()
        {
            if (--s_DefaultShaderInfoTextureRefCount == 0)
            {
                UIRUtility.Destroy(s_DefaultShaderInfoTexture);
                s_DefaultShaderInfoTexture = null;
            }
        }

        static Vector2Int AllocToTexelCoord(ref BitmapAllocator32 allocator, BMPAlloc alloc)
        {
            UInt16 x, y;
            allocator.GetAllocPageAtlasLocation(alloc.page, out x, out y);
            return new Vector2Int(
                alloc.bitIndex * allocator.entryWidth + x,
                alloc.pageLine * allocator.entryHeight + y);
        }

        static bool AtlasRectMatchesPage(ref BitmapAllocator32 allocator, BMPAlloc defAlloc, RectInt atlasRect)
        {
            UInt16 x, y;
            allocator.GetAllocPageAtlasLocation(defAlloc.page, out x, out y);
            return (x == atlasRect.xMin) && (y == atlasRect.yMin) &&
                (allocator.entryWidth * pageWidth == atlasRect.width) &&
                (allocator.entryHeight * pageHeight == atlasRect.height);
        }

        public Texture atlas
        {
            get
            {
                if (m_StorageReallyCreated)
                    return m_Storage.texture;
                return s_DefaultShaderInfoTexture;
            }
        }
        public bool internalAtlasCreated { get { return m_StorageReallyCreated; } } // For diagnostics really

        public ShaderInfoAllocator(ColorSpace colorSpace)
        {
            m_ColorSpace = colorSpace;

            // The default allocs refer to four startup pages to be allocated as below from the atlas
            // once the atlas is used for the first time. The page coordinates correspond to the atlas's
            // internal algorithm's results. If that algorithm changes, the new results must be put here to match
            m_OpacityAllocator = m_ColorAllocator = m_ClipRectAllocator = m_TransformAllocator = m_TextSettingsAllocator = new BitmapAllocator32();
            m_TransformAllocator.Construct(pageHeight, 1, 3);
            m_TransformAllocator.ForceFirstAlloc((ushort)identityTransformTexel.x, (ushort)identityTransformTexel.y);
            m_ClipRectAllocator.Construct(pageHeight);
            m_ClipRectAllocator.ForceFirstAlloc((ushort)infiniteClipRectTexel.x, (ushort)infiniteClipRectTexel.y);
            m_OpacityAllocator.Construct(pageHeight);
            m_OpacityAllocator.ForceFirstAlloc((ushort)fullOpacityTexel.x, (ushort)fullOpacityTexel.y);
            m_ColorAllocator.Construct(pageHeight);
            m_ColorAllocator.ForceFirstAlloc((ushort)clearColorTexel.x, (ushort)clearColorTexel.y);
            m_TextSettingsAllocator.Construct(pageHeight, 1, 4);
            m_TextSettingsAllocator.ForceFirstAlloc((ushort)defaultTextCoreSettingsTexel.x, (ushort)defaultTextCoreSettingsTexel.y);

            // Seed page 0 of every allocator with sensible defaults
            m_XformPagePos[0]    = new Vector4(identityTransformTexel.x,        identityTransformTexel.y,        0, 0);
            m_ClipPagePos[0]     = new Vector4(infiniteClipRectTexel.x,         infiniteClipRectTexel.y,         0, 0);
            m_OpacityPagePos[0]  = new Vector4(fullOpacityTexel.x,              fullOpacityTexel.y,              0, 0);
            m_ColorPagePos[0]    = new Vector4(clearColorTexel.x,               clearColorTexel.y,               0, 0);
            m_TextCorePagePos[0] = new Vector4(defaultTextCoreSettingsTexel.x,  defaultTextCoreSettingsTexel.y,  0, 0);

            AcquireDefaultShaderInfoTexture();
        }

        BMPAlloc AllocateAndRecordPage(ref BitmapAllocator32 allocator, Vector4[] pageTable, ref bool errored, string allocName)
        {
            int prevPageCount = allocator.pageCount;

            // If we can't allocate new pages, try allocating into existing pages only
            BMPAlloc bmp = (prevPageCount >= kMaxPages) ? allocator.Allocate(null) : allocator.Allocate(m_Storage);
            if (bmp.IsValid())
            {
                if (allocator.pageCount > prevPageCount)
                {
                    Debug.Assert(allocator.pageCount <= kMaxPages, "page count exceeds kMaxPages cap");
                    allocator.GetAllocPageAtlasLocation(bmp.page, out ushort x, out ushort y);
                    pageTable[bmp.page] = new Vector4(x, y, 0, 0);
                }
            }
            else if (!errored)
            {
                errored = true;
                int slotsPerPage = BitmapAllocator32.kPageWidth * pageHeight;
                Debug.LogError($"UIE shader-info {allocName} allocator exhausted at {kMaxPages} pages × {slotsPerPage} slots = {kMaxPages * slotsPerPage} entries. Subsequent allocations will fall back to defaults.");
            }
            return bmp;
        }

        void ReallyCreateStorage()
        {
            // Because we want predictable placement of first pages, 64 will fit all default allocs
            m_Storage = new ShaderInfoStorageRGBAFloat(64);

            // The order of allocation from the atlas below is important. See the comment at the beginning of Construct().
            RectInt rcTransform, rcClipRect, rcOpacity, rcColor, rcTextCoreSettings;
            m_Storage.AllocateRect(pageWidth * m_TransformAllocator.entryWidth, pageHeight * m_TransformAllocator.entryHeight, out rcTransform);
            m_Storage.AllocateRect(pageWidth * m_ClipRectAllocator.entryWidth, pageHeight * m_ClipRectAllocator.entryHeight, out rcClipRect);
            m_Storage.AllocateRect(pageWidth * m_OpacityAllocator.entryWidth, pageHeight * m_OpacityAllocator.entryHeight, out rcOpacity);
            m_Storage.AllocateRect(pageWidth * m_ColorAllocator.entryWidth, pageHeight * m_ColorAllocator.entryHeight, out rcColor);
            m_Storage.AllocateRect(pageWidth * m_TextSettingsAllocator.entryWidth, pageHeight * m_TextSettingsAllocator.entryHeight, out rcTextCoreSettings);

            if (!AtlasRectMatchesPage(ref m_TransformAllocator, identityTransform, rcTransform))
                throw new Exception("Atlas identity transform allocation failed unexpectedly");

            if (!AtlasRectMatchesPage(ref m_ClipRectAllocator, infiniteClipRect, rcClipRect))
                throw new Exception("Atlas infinite clip rect allocation failed unexpectedly");

            if (!AtlasRectMatchesPage(ref m_OpacityAllocator, fullOpacity, rcOpacity))
                throw new Exception("Atlas full opacity allocation failed unexpectedly");

            if (!AtlasRectMatchesPage(ref m_ColorAllocator, clearColor, rcColor))
                throw new Exception("Atlas clear color allocation failed unexpectedly");

            if (!AtlasRectMatchesPage(ref m_TextSettingsAllocator, defaultTextCoreSettings, rcTextCoreSettings))
                throw new Exception("Atlas text setting allocation failed unexpectedly");

            SetTransformValue(identityTransform, identityTransformValue);
            SetClipRectValue(infiniteClipRect, infiniteClipRectValue);
            SetOpacityValue(fullOpacity, fullOpacityValue.w);
            SetColorValue(clearColor, clearColorValue); // color is saturated, no need to check the colorspace
            SetTextCoreSettingValue(defaultTextCoreSettings, defaultTextCoreSettingsValue); // colors are saturated, no need to check the colorspace

            m_StorageReallyCreated = true;
        }

        public void Dispose()
        {
            if (m_Storage != null)
                m_Storage.Dispose();
            m_Storage = null;
            m_StorageReallyCreated = false;
            ReleaseDefaultShaderInfoTexture();
        }

        public void IssuePendingStorageChanges()
        {
            m_Storage?.UpdateTexture();
        }

        public BMPAlloc AllocTransform()
        {
            if (!m_StorageReallyCreated)
                ReallyCreateStorage();

            return AllocateAndRecordPage(ref m_TransformAllocator, m_XformPagePos, ref m_TransformPagesErrored, "Transform");
        }

        public BMPAlloc AllocClipRect()
        {
            if (!m_StorageReallyCreated)
                ReallyCreateStorage();

            return AllocateAndRecordPage(ref m_ClipRectAllocator, m_ClipPagePos, ref m_ClipRectPagesErrored, "ClipRect");
        }

        public BMPAlloc AllocOpacity()
        {
            if (!m_StorageReallyCreated)
                ReallyCreateStorage();

            return AllocateAndRecordPage(ref m_OpacityAllocator, m_OpacityPagePos, ref m_OpacityPagesErrored, "Opacity");
        }

        public BMPAlloc AllocColor()
        {
            if (!m_StorageReallyCreated)
                ReallyCreateStorage();

            return AllocateAndRecordPage(ref m_ColorAllocator, m_ColorPagePos, ref m_ColorPagesErrored, "Color");
        }

        public BMPAlloc AllocTextCoreSettings(TextCoreSettings settings)
        {
            if (!m_StorageReallyCreated)
                ReallyCreateStorage();

            return AllocateAndRecordPage(ref m_TextSettingsAllocator, m_TextCorePagePos, ref m_TextCorePagesErrored, "TextCore");
        }

        public void SetTransformValue(BMPAlloc alloc, Matrix4x4 xform)
        {
            Debug.Assert(alloc.IsValid());
            var allocXY = AllocToTexelCoord(ref m_TransformAllocator, alloc);
            m_Storage.SetTexel(allocXY.x, allocXY.y + 0, xform.GetRow(0));
            m_Storage.SetTexel(allocXY.x, allocXY.y + 1, xform.GetRow(1));
            m_Storage.SetTexel(allocXY.x, allocXY.y + 2, xform.GetRow(2));
        }

        public void SetClipRectValue(BMPAlloc alloc, Vector4 clipRect)
        {
            Debug.Assert(alloc.IsValid());
            var allocXY = AllocToTexelCoord(ref m_ClipRectAllocator, alloc);
            m_Storage.SetTexel(allocXY.x, allocXY.y, clipRect);
        }

        public void SetOpacityValue(BMPAlloc alloc, float opacity)
        {
            Debug.Assert(alloc.IsValid());
            var allocXY = AllocToTexelCoord(ref m_OpacityAllocator, alloc);
            m_Storage.SetTexel(allocXY.x, allocXY.y, new Color(1, 1, 1, opacity));
        }

        public void SetColorValue(BMPAlloc alloc, Color color)
        {
            Debug.Assert(alloc.IsValid());
            var allocXY = AllocToTexelCoord(ref m_ColorAllocator, alloc);

            // Dynamic colors are converted to linear colorspace (when needed) after
            // being used as a tint on the vertex color. 
            m_Storage.SetTexel(allocXY.x, allocXY.y, color);
        }

        public void SetTextCoreSettingValue(BMPAlloc alloc, TextCoreSettings settings)
        {
            Debug.Assert(alloc.IsValid());

            var allocXY = AllocToTexelCoord(ref m_TextSettingsAllocator, alloc);
            var settingValues = new Color(-settings.underlayOffset.x, settings.underlayOffset.y, settings.underlaySoftness, settings.outlineWidth);

            // The face color is converted to linear colorspace (when needed) after
            // being used as a tint on the vertex color. 
            m_Storage.SetTexel(allocXY.x, allocXY.y + 0, settings.faceColor);

            if (m_ColorSpace == ColorSpace.Linear)
            {
                m_Storage.SetTexel(allocXY.x, allocXY.y + 1, settings.outlineColor.linear);
                m_Storage.SetTexel(allocXY.x, allocXY.y + 2, settings.underlayColor.linear);
            }
            else
            {
                m_Storage.SetTexel(allocXY.x, allocXY.y + 1, settings.outlineColor);
                m_Storage.SetTexel(allocXY.x, allocXY.y + 2, settings.underlayColor);
            }

            m_Storage.SetTexel(allocXY.x, allocXY.y + 3, settingValues);
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

        public void FreeColor(BMPAlloc alloc)
        {
            Debug.Assert(alloc.IsValid());
            m_ColorAllocator.Free(alloc);
        }

        public void FreeTextCoreSettings(BMPAlloc alloc)
        {
            Debug.Assert(alloc.IsValid());
            m_TextSettingsAllocator.Free(alloc);
        }

        // Encodes a BMPAlloc into the 16-bit ID layout
        // [reserved:3][pageIdx:5][bitInPage:8]
        public static ushort BMPAllocToId(BMPAlloc alloc)
        {
            if (!alloc.IsValid())
                return 0;
            Debug.Assert(alloc.page < kMaxPages, "page index exceeds kMaxPages cap");
            uint bitInPage = (uint)(alloc.pageLine * BitmapAllocator32.kPageWidth + alloc.bitIndex);
            return (ushort)(((uint)alloc.page << 8) | bitInPage);
        }
    }
}
