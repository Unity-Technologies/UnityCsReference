// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.UIElements.UIR
{
    // Places the textures of the compositor's draw operations into shared render textures (pages). The compositor plans
    // a frame by replaying its execution order: Allocate when an operation writes, Free after its last reader. Pages are
    // only created when the operations run, and a page that isn't read by a render tree quad is released after its last use.
    class RenderTreeAtlas : IDisposable
    {
        // Add an artifical margins to force users to use the UV-rect in their own filter implementation.
        const int k_LeftMargin = 10;
        const int k_TopMargin = 10;
        const int k_RightMargin = 5;
        const int k_BottomMargin = 5;

        const int k_MinPageSize = 128;
        const int k_MaxPageSize = 4096;
        const int k_DedicatedSizeStep = 256;

        // Largest block content size whose texture (content + internal margins) stays within the GPU limit.
        static public int maxBlockWidth => SystemInfo.maxRenderTextureSize - k_LeftMargin - k_RightMargin;
        static public int maxBlockHeight => SystemInfo.maxRenderTextureSize - k_TopMargin - k_BottomMargin;

        [Flags]
        public enum PageTraits
        {
            GammaEncoded = 1 << 0,
            DepthStencil = 1 << 1,
            Output = 1 << 2, // Read by a render tree quad: kept until the next frame
        }

        const int k_TraitsCount = 1 << 3;

        public struct AtlasBlock
        {
            public int width;
            public int height;

            // Only valid once placed: the content in page pixels (y up, as used for the viewport) and in page UVs
            public RectInt rect;
            public Rect uvRect;

            public int page; // -1 until placed
            public PageTraits traits;
            public RectInt region; // Content and margins, in packer space (y down)
            public bool needsClear; // The region may cover pixels written earlier in the frame

            public int regionWidth
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => width + k_LeftMargin + k_RightMargin;
            }

            public int regionHeight
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => height + k_TopMargin + k_BottomMargin;
            }

            // The content's top-left corner in page pixels, y down
            public Vector2Int contentTopLeft => new Vector2Int(region.x + k_LeftMargin, region.y + k_TopMargin);
        }

        class Page
        {
            public PageTraits traits;
            public int width;
            public int height;
            public bool dedicated;
            public readonly RectPacker packer = new();
            public readonly ShelfPacker shelfPacker = new();
            public bool usesShelves; // Output pages are filled once, tallest first, and never freed: rows fit them for much less work
            public readonly List<RectInt> liveRegions = new();
            public bool hasDeadSpace; // A region died since the packer was last rebuilt
            public bool dirtyFreeSpace; // The packer's free space may cover pixels written earlier in the frame
            public bool clearPending; // The packer was reset over dead pixels: clear the page before its next region
            public int lastUse;
            public RenderTexture texture;
            public TextureId textureId;

            public void Init(PageTraits traits, int width, int height, bool dedicated)
            {
                this.traits = traits;
                this.width = width;
                this.height = height;
                this.dedicated = dedicated;
                usesShelves = (traits & PageTraits.Output) != 0;
                ResetPacker();
                liveRegions.Clear();
                hasDeadSpace = false;
                dirtyFreeSpace = false;
                clearPending = false;
                lastUse = -1;
                texture = null;
                textureId = TextureId.invalid;
            }

            public void ResetPacker()
            {
                if (usesShelves)
                    shelfPacker.Reset(width, height);
                else
                    packer.Reset(width, height);
            }

            public bool TryAllocate(int regionWidth, int regionHeight, out RectInt region)
            {
                return usesShelves
                    ? shelfPacker.TryAllocate(regionWidth, regionHeight, out region)
                    : packer.TryAllocate(regionWidth, regionHeight, out region);
            }
        }

        readonly TextureRegistry m_TextureRegistry;
        readonly List<Page> m_Pages = new();
        readonly Stack<Page> m_PagePool = new();

        struct PageStep
        {
            public int step;
            public int page;
        }

        // The execution replays both lists in step order with a cursor
        readonly List<PageStep> m_PageClears = new();
        int m_NextPageClear;
        readonly List<PageStep> m_PageReleases = new(); // The intermediate pages, at the last step that uses them
        int m_NextPageRelease;

        [NoAutoStaticsCleanup] // stateless sort comparator; no captured state
        static readonly Comparison<PageStep> k_ByStep = (a, b) => a.step.CompareTo(b.step);

        struct PendingRegion
        {
            public int page;
            public RectInt region;
            public bool paintMargins;
        }

        // The regions of a batch's blocks, prepared together before the batch draws
        readonly List<PendingRegion> m_PendingRegions = new();

        [NoAutoStaticsCleanup] // stateless sort comparator; no captured state
        static readonly Comparison<PendingRegion> k_ByPage = (a, b) => a.page.CompareTo(b.page);

        // Per PageTraits value: the measured peak of live region area, and the largest sides of a shared region
        readonly long[] m_LiveArea = new long[k_TraitsCount];
        readonly long[] m_PeakArea = new long[k_TraitsCount];
        readonly int[] m_MaxRegionWidth = new int[k_TraitsCount];
        readonly int[] m_MaxRegionHeight = new int[k_TraitsCount];
        readonly long[] m_SharedLiveArea = new long[k_TraitsCount];
        readonly long[] m_SharedPeakArea = new long[k_TraitsCount];
        readonly long[] m_BatchArea = new long[k_TraitsCount];
        readonly long[] m_MaxBatchArea = new long[k_TraitsCount]; // Of the shared regions that one batch writes
        readonly int[] m_PageWidth = new int[k_TraitsCount];
        readonly int[] m_PageHeight = new int[k_TraitsCount];
        readonly int[] m_OversizedFrames = new int[k_TraitsCount];

        int m_MaxPageSize;
        int m_MaxTextureSize;
        bool m_FailureLogged;

        public RenderTreeAtlas(TextureRegistry textureRegistry)
        {
            m_TextureRegistry = textureRegistry;
        }

        static public bool ReserveSize(int width, int height, out AtlasBlock block)
        {
            return ReserveSize(width, height, maxBlockWidth, maxBlockHeight, out block);
        }

        // maxWidth and maxHeight are maxBlockWidth and maxBlockHeight, read once per frame by the caller
        static public bool ReserveSize(int width, int height, int maxWidth, int maxHeight, out AtlasBlock block)
        {
            // The compositor crops oversized content and reports it; this is the last guard before we ask for
            // a texture beyond the GPU limit, which fails to create and then composites stale VRAM (UUM-147797).
            // Non-positive limits mean the graphics caps aren't usable yet: let the allocation fail instead.
            if (maxWidth > 0 && maxHeight > 0)
            {
                width = Mathf.Min(width, maxWidth);
                height = Mathf.Min(height, maxHeight);
            }

            block = new AtlasBlock { width = width, height = height, page = -1 };
            return true;
        }

        public void BeginPlan()
        {
            ReleaseAll();

            // Non-positive means the graphics caps aren't usable yet: plan anyway and let the texture creation fail
            m_MaxTextureSize = SystemInfo.maxRenderTextureSize;
            if (m_MaxTextureSize <= 0)
                m_MaxTextureSize = k_MaxPageSize;
            m_MaxPageSize = Mathf.Min(k_MaxPageSize, Mathf.ClosestPowerOfTwo(m_MaxTextureSize));
            if (m_MaxPageSize > m_MaxTextureSize)
                m_MaxPageSize /= 2;

            Array.Clear(m_LiveArea, 0, k_TraitsCount);
            Array.Clear(m_PeakArea, 0, k_TraitsCount);
            Array.Clear(m_MaxRegionWidth, 0, k_TraitsCount);
            Array.Clear(m_MaxRegionHeight, 0, k_TraitsCount);
            Array.Clear(m_SharedLiveArea, 0, k_TraitsCount);
            Array.Clear(m_SharedPeakArea, 0, k_TraitsCount);
            Array.Clear(m_BatchArea, 0, k_TraitsCount);
            Array.Clear(m_MaxBatchArea, 0, k_TraitsCount);
            m_PageClears.Clear();
            m_NextPageClear = 0;
            m_PageReleases.Clear();
            m_NextPageRelease = 0;
        }

        // After the last NoteUse
        public void EndPlan()
        {
            for (int i = 0; i < m_Pages.Count; ++i)
            {
                var page = m_Pages[i];
                if (page.lastUse >= 0 && (page.traits & PageTraits.Output) == 0)
                    m_PageReleases.Add(new PageStep { step = page.lastUse, page = i });
            }
            m_PageReleases.Sort(k_ByStep);
        }

        bool IsDedicated(int regionWidth, int regionHeight) => Mathf.Max(regionWidth, regionHeight) > m_MaxPageSize / 2;

        // First planning pass: replays the frame without placing anything, to size the pages from the peak live area
        public void MeasureAlloc(PageTraits traits, in AtlasBlock block)
        {
            int t = (int)traits;
            long area = (long)block.regionWidth * block.regionHeight;
            m_LiveArea[t] += area;
            m_PeakArea[t] = Math.Max(m_PeakArea[t], m_LiveArea[t]);
            if (!IsDedicated(block.regionWidth, block.regionHeight))
            {
                m_MaxRegionWidth[t] = Mathf.Max(m_MaxRegionWidth[t], block.regionWidth);
                m_MaxRegionHeight[t] = Mathf.Max(m_MaxRegionHeight[t], block.regionHeight);
                m_BatchArea[t] += area;
                m_SharedLiveArea[t] += area;
                m_SharedPeakArea[t] = Math.Max(m_SharedPeakArea[t], m_SharedLiveArea[t]);
            }
        }

        public void MeasureFree(PageTraits traits, in AtlasBlock block)
        {
            int t = (int)traits;
            long area = (long)block.regionWidth * block.regionHeight;
            m_LiveArea[t] -= area;
            if (!IsDedicated(block.regionWidth, block.regionHeight))
                m_SharedLiveArea[t] -= area;
        }

        // After the MeasureAlloc calls of a batch; EndMeasure ends the last one
        public void EndMeasureBatch()
        {
            for (int t = 0; t < k_TraitsCount; ++t)
            {
                m_MaxBatchArea[t] = Math.Max(m_MaxBatchArea[t], m_BatchArea[t]);
                m_BatchArea[t] = 0;
            }
        }

        public void EndMeasure()
        {
            EndMeasureBatch();
            for (int t = 0; t < k_TraitsCount; ++t)
                FitPagesToBatches(t);
        }

        const int k_PageSizeStep = 128;
        const int k_ShrinkDelayFrames = 30;

        // A page holds the largest batch that writes these traits, so that the batch draws once. The batch that reads it
        // writes another page, so ping-pong passes use two pages, not one that holds both. Outputs stay alive all frame,
        // so their pages hold all of them. A page only shrinks after it stayed twice too large for a while: the texture
        // pool only reuses textures of the same size.
        void FitPagesToBatches(int t)
        {
            bool isOutput = (t & (int)PageTraits.Output) != 0;
            long area = isOutput ? m_SharedPeakArea[t] : m_MaxBatchArea[t];
            if (area == 0)
                return;

            long needed = area + area / 4; // Packing is never perfect
            int regionWidth = Mathf.Max(m_MaxRegionWidth[t], 1);
            int regionHeight = Mathf.Max(m_MaxRegionHeight[t], 1);
            ComputePageShape(needed, regionWidth, regionHeight, out int width, out int height);

            int previousWidth = m_PageWidth[t];
            int previousHeight = m_PageHeight[t];
            if (UsableArea(previousWidth, previousHeight, regionWidth, regionHeight) >= needed)
            {
                if ((long)previousWidth * previousHeight <= 2L * width * height)
                {
                    m_OversizedFrames[t] = 0;
                    return;
                }
                if (++m_OversizedFrames[t] < k_ShrinkDelayFrames)
                    return;
            }

            m_PageWidth[t] = width;
            m_PageHeight[t] = height;
            m_OversizedFrames[t] = 0;
        }

        // Only whole columns and rows of the largest region count: two 151 px regions don't fit side by side in 256 px
        static long UsableArea(int width, int height, int regionWidth, int regionHeight)
        {
            return (long)(width / regionWidth * regionWidth) * (height / regionHeight * regionHeight);
        }

        // The smallest page, in steps of k_PageSizeStep and at most twice as long as wide, that holds the area
        void ComputePageShape(long needed, int regionWidth, int regionHeight, out int width, out int height)
        {
            int minWidth = Mathf.Min(RoundUp(Mathf.Max(regionWidth, k_MinPageSize), k_PageSizeStep), m_MaxPageSize);
            int minHeight = Mathf.Min(RoundUp(Mathf.Max(regionHeight, k_MinPageSize), k_PageSizeStep), m_MaxPageSize);
            width = height = m_MaxPageSize;
            long bestArea = long.MaxValue;
            for (int w = minWidth; w <= m_MaxPageSize; w += k_PageSizeStep)
            {
                long usableWidth = w / regionWidth * regionWidth;
                long rows = (needed + usableWidth - 1) / usableWidth;
                long usableRows = (rows + regionHeight - 1) / regionHeight * regionHeight;
                if (usableRows > m_MaxPageSize)
                    continue;
                int h = Mathf.Max(minHeight, RoundUp((int)usableRows, k_PageSizeStep));
                if (h > m_MaxPageSize || h > 2 * w || w > 2 * h)
                    continue;
                long pageArea = (long)w * h;
                if (pageArea < bestArea || (pageArea == bestArea && Mathf.Max(w, h) < Mathf.Max(width, height)))
                {
                    bestArea = pageArea;
                    width = w;
                    height = h;
                }
            }
        }

        // Second planning pass, at the given execution step. A page listed in forbiddenPages is read by the operation,
        // so it can't be its target.
        public void Allocate(PageTraits traits, ref AtlasBlock block, List<int> forbiddenPages, int step)
        {
            int regionWidth = block.regionWidth;
            int regionHeight = block.regionHeight;
            block.traits = traits;

            if (IsDedicated(regionWidth, regionHeight))
            {
                int width = Mathf.Min(RoundUp(regionWidth, k_DedicatedSizeStep), m_MaxTextureSize);
                int height = Mathf.Min(RoundUp(regionHeight, k_DedicatedSizeStep), m_MaxTextureSize);
                var page = CreatePage(traits, width, height, true);
                Place(m_Pages.Count - 1, page, regionWidth, regionHeight, step, ref block);
                return;
            }

            for (int i = 0; i < m_Pages.Count; ++i)
            {
                var page = m_Pages[i];
                if (page.traits != traits || page.dedicated || forbiddenPages.Contains(i))
                    continue;
                if (Place(i, page, regionWidth, regionHeight, step, ref block))
                    return;
            }

            var newPage = CreatePage(traits, m_PageWidth[(int)traits], m_PageHeight[(int)traits], false);
            bool placed = Place(m_Pages.Count - 1, newPage, regionWidth, regionHeight, step, ref block);
            Debug.Assert(placed, "A new page must fit any shared region.");
        }

        // Between chunks: a page whose regions all died is packed from scratch, after one clear of the whole page
        public void RecycleEmptyPages()
        {
            foreach (var page in m_Pages)
            {
                if (page.dedicated || page.liveRegions.Count > 0 || !page.hasDeadSpace)
                    continue;
                page.ResetPacker();
                page.hasDeadSpace = false;
                page.clearPending = true;
            }
        }

        static int RoundUp(int value, int step) => (value + step - 1) / step * step;

        Page CreatePage(PageTraits traits, int width, int height, bool dedicated)
        {
            var page = m_PagePool.Count > 0 ? m_PagePool.Pop() : new Page();
            page.Init(traits, width, height, dedicated);
            if ((traits & PageTraits.Output) != 0)
                page.textureId = m_TextureRegistry.AllocAndAcquireDynamic();
            m_Pages.Add(page);
            return page;
        }

        bool Place(int pageIndex, Page page, int regionWidth, int regionHeight, int step, ref AtlasBlock block)
        {
            if (!page.TryAllocate(regionWidth, regionHeight, out RectInt region))
            {
                if (!page.hasDeadSpace)
                    return false;

                // Only the packer takes the live regions as obstacles
                page.usesShelves = false;
                page.packer.Reset(page.width, page.height);
                foreach (var live in page.liveRegions)
                    page.packer.AddObstacle(live);
                page.hasDeadSpace = false;
                if (page.liveRegions.Count == 0)
                    page.clearPending = true;
                else
                    page.dirtyFreeSpace = true;

                if (!page.packer.TryAllocate(regionWidth, regionHeight, out region))
                    return false;
            }

            // Nothing reads the page at this step: no region of it is live
            if (page.clearPending)
            {
                m_PageClears.Add(new PageStep { step = step, page = pageIndex });
                page.clearPending = false;
                page.dirtyFreeSpace = false;
            }

            page.liveRegions.Add(region);

            int x = region.x + k_LeftMargin;
            int y = page.height - (region.y + k_TopMargin + block.height); // Flip to match UI Toolkit's coordinate system
            block.page = pageIndex;
            block.region = region;
            block.needsClear = page.dirtyFreeSpace;
            block.rect = new RectInt(x, y, block.width, block.height);
            block.uvRect = new Rect(x / (float)page.width, y / (float)page.height, block.width / (float)page.width, block.height / (float)page.height);
            return true;
        }

        public void Free(in AtlasBlock block)
        {
            var page = m_Pages[block.page];
            page.liveRegions.Remove(block.region);
            page.hasDeadSpace = true;
        }

        // The page can be released once the compositor step with this index has run
        public void NoteUse(int pageIndex, int step)
        {
            var page = m_Pages[pageIndex];
            page.lastUse = Mathf.Max(page.lastUse, step);
        }

        public TextureId GetTextureId(int pageIndex) => m_Pages[pageIndex].textureId;

        public Vector2Int GetPageSize(in AtlasBlock block) => new Vector2Int(m_Pages[block.page].width, m_Pages[block.page].height);

        public RenderTexture GetTexture(in AtlasBlock block) => block.page >= 0 ? m_Pages[block.page].texture : null;

        // Creates the page texture on its first use and queues the region of the block for PrepareRegions. paintMargins only
        // applies in the editor.
        public bool BeginDraw(in AtlasBlock block, bool paintMargins)
        {
            Debug.Assert(block.page >= 0, "The block was not placed.");
            var page = m_Pages[block.page];
            if (page.texture == null && !CreateTexture(page))
            {
                if (!m_FailureLogged)
                {
                    m_FailureLogged = true;
                    Debug.LogError($"Failed to create a {page.width}x{page.height} texture for filtered elements.");
                }
                return false;
            }

            if (block.needsClear || paintMargins)
                m_PendingRegions.Add(new PendingRegion { page = block.page, region = block.region, paintMargins = paintMargins });
            return true;
        }

        // Makes the regions that BeginDraw queued transparent, with one draw per page. In the editor, the same draw paints
        // pink the margins of the regions that a custom pass reads, to show custom filter creators when they sample outside
        // the atlas UVs. The pass writes without blending.
        public void PrepareRegions()
        {
            int count = m_PendingRegions.Count;
            if (count == 0)
                return;

            m_PendingRegions.Sort(k_ByPage);
            var old = RenderTexture.active;
            GL.PushMatrix();
            try
            {
                GL.modelview = Matrix4x4.identity;
                for (int start = 0; start < count;)
                {
                    int pageIndex = m_PendingRegions[start].page;
                    var page = m_Pages[pageIndex];
                    RenderTexture.active = page.texture;
                    GL.Viewport(new Rect(0, 0, page.width, page.height));
                    GL.LoadProjectionMatrix(Matrix4x4.Ortho(0, page.width, page.height, 0, -1, 1)); // Packer space, y down
                    UIRUtility.SetVertexColorPass();

                    GL.Begin(GL.QUADS);
                    GL.Color(Color.clear);
                    for (; start < count && m_PendingRegions[start].page == pageIndex; ++start)
                    {
                        var pending = m_PendingRegions[start];
                        var region = pending.region;
                        if (pending.paintMargins)
                        {
                            // The whole region in pink, then the inside transparent: the content and the 2 pixels around it, so
                            // that bilinear samples at its edge don't pick up the pink
                            GL.Color(Color.pink);
                            AddQuad(region.xMin, region.yMin, region.xMax, region.yMax);
                            GL.Color(Color.clear);
                            AddQuad(region.xMin + k_LeftMargin - 2, region.yMin + k_TopMargin - 2, region.xMax - k_RightMargin + 2, region.yMax - k_BottomMargin + 2);
                        }
                        else
                            AddQuad(region.xMin, region.yMin, region.xMax, region.yMax);
                    }
                    GL.End();
                }
            }
            finally
            {
                GL.PopMatrix();
                RenderTexture.active = old;
                m_PendingRegions.Clear();
            }
        }

        static void AddQuad(int xMin, int yMin, int xMax, int yMax)
        {
            GL.Vertex3(xMin, yMin, 0);
            GL.Vertex3(xMax, yMin, 0);
            GL.Vertex3(xMax, yMax, 0);
            GL.Vertex3(xMin, yMax, 0);
        }

        bool CreateTexture(Page page)
        {
            bool depthStencil = (page.traits & PageTraits.DepthStencil) != 0;
            bool gammaEncoded = (page.traits & PageTraits.GammaEncoded) != 0 || QualitySettings.activeColorSpace == ColorSpace.Gamma;
            var graphicsFormat = gammaEncoded ? GraphicsFormat.R8G8B8A8_UNorm : GraphicsFormat.R8G8B8A8_SRGB;
            var descriptor = new RenderTextureDescriptor(page.width, page.height, graphicsFormat, depthStencil ? GraphicsFormat.D24_UNorm_S8_UInt : GraphicsFormat.None);
            descriptor.useMipMap = false;

            var texture = RenderTexture.GetTemporary(descriptor);
            if (texture == null)
                return false;

            // An uncreated texture would composite stale VRAM (UUM-147797)
            if (!texture.IsCreated() && !texture.Create())
            {
                RenderTexture.ReleaseTemporary(texture);
                return false;
            }

            page.texture = texture;
            ClearTexture(page);
            if (page.textureId.IsValid())
                m_TextureRegistry.UpdateDynamic(page.textureId, texture);
            return true;
        }

        static void ClearTexture(Page page)
        {
            var old = RenderTexture.active;
            RenderTexture.active = page.texture;
            GL.Clear((page.traits & PageTraits.DepthStencil) != 0, true, Color.clear, UIRUtility.k_ClearZ);
            RenderTexture.active = old;
        }

        // Clears the pages that the plan recycled at this step. A page without a texture yet is cleared at its creation.
        public void BeginExecution(int step)
        {
            for (; m_NextPageClear < m_PageClears.Count && m_PageClears[m_NextPageClear].step <= step; ++m_NextPageClear)
            {
                var page = m_Pages[m_PageClears[m_NextPageClear].page];
                if (page.texture != null)
                    ClearTexture(page);
            }
        }

        // Releases the textures that no step after this one reads or writes
        public void EndExecution(int step)
        {
            if (Testing.keepTextures)
                return;
            for (; m_NextPageRelease < m_PageReleases.Count && m_PageReleases[m_NextPageRelease].step <= step; ++m_NextPageRelease)
                ReleaseTexture(m_Pages[m_PageReleases[m_NextPageRelease].page]);
        }

        static void ReleaseTexture(Page page)
        {
            if (page.texture == null)
                return;
            RenderTexture.ReleaseTemporary(page.texture);
            page.texture = null;
        }

        public void ReleaseAll()
        {
            foreach (var page in m_Pages)
            {
                ReleaseTexture(page);
                if (page.textureId.IsValid())
                {
                    m_TextureRegistry.Release(page.textureId);
                    page.textureId = TextureId.invalid;
                }
                m_PagePool.Push(page);
            }
            m_Pages.Clear();
        }

        public void Dispose()
        {
            ReleaseAll();
        }

        public static class Testing
        {
            // Keeps the textures of the pages until the next frame, so that a test can read an intermediate page
            [NoAutoStaticsCleanup] // test-only switch, reset by the tests that set it
            public static bool keepTextures;

            public static int CountLiveTextures(RenderTreeAtlas atlas)
            {
                int count = 0;
                foreach (var page in atlas.m_Pages)
                {
                    if (page.texture != null)
                        ++count;
                }
                return count;
            }

            public static int CountLiveIntermediateTextures(RenderTreeAtlas atlas)
            {
                int count = 0;
                foreach (var page in atlas.m_Pages)
                {
                    if (page.texture != null && (page.traits & PageTraits.Output) == 0)
                        ++count;
                }
                return count;
            }

            // Until the next plan: the pages of the last frame
            public static int CountPages(RenderTreeAtlas atlas) => atlas.m_Pages.Count;

            public static long CountPagePixels(RenderTreeAtlas atlas)
            {
                long pixels = 0;
                foreach (var page in atlas.m_Pages)
                    pixels += (long)page.width * page.height;
                return pixels;
            }

            public static int CountPageClears(RenderTreeAtlas atlas) => atlas.m_PageClears.Count;

            // The sum of the per-traits peaks of the regions that aren't outputs, which bounds their combined peak
            public static long PeakTransientArea(RenderTreeAtlas atlas)
            {
                long area = 0;
                for (int t = 0; t < k_TraitsCount; ++t)
                {
                    if ((t & (int)PageTraits.Output) == 0)
                        area += atlas.m_PeakArea[t];
                }
                return area;
            }
        }
    }
}
