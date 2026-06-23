// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditorInternal.Profiling
{
    // Packs many small thumbnails into a small number of large Texture2D atlases, so the
    // Screenshots strip can hold thousands of thumbs without bumping the editor's global
    // 2048 TextureId cap. Each thumb writes into a fixed-size slot via a single persistent
    // staging texture + Graphics.CopyTexture; UV coords on the displaying Image element
    // select that slot's sub-region for rendering. Slots are pinned for the pool's lifetime
    // — no eviction, so scroll-back never reloads.
    internal sealed class ThumbnailAtlasPool : IDisposable
    {
        const int k_AtlasSize = 2048;
        const int k_SlotWidth = 128;
        const int k_SlotHeight = 72;
        const int k_SlotsPerRow = k_AtlasSize / k_SlotWidth;   // 16
        const int k_RowsPerAtlas = k_AtlasSize / k_SlotHeight; // 28 (2016 of 2048 used vertically)
        const int k_SlotsPerAtlas = k_SlotsPerRow * k_RowsPerAtlas; // 448

        readonly List<Texture2D> m_Atlases = new List<Texture2D>();
        int m_CurrentAtlasIndex = -1;
        int m_NextSlotInAtlas = k_SlotsPerAtlas; // force a new atlas on first allocate

        Texture2D m_Staging;
        byte[] m_StagingBuffer;

        public struct AtlasSlot
        {
            public Texture2D Atlas;
            // Bottom-left origin pixel coords, matching Graphics.CopyTexture's dst convention
            // and Image.uv's UV-space origin.
            public int X;
            public int Y;
        }

        public ThumbnailAtlasPool()
        {
            // The pool only exists while the Profiler window is open, which guarantees a live graphics
            // device, and every desktop Editor backend (D3D11/12, Metal, Vulkan, GL core) supports the
            // basic Graphics.CopyTexture (Texture2D→Texture2D) this relies on. Assert just in case.
            Debug.Assert((SystemInfo.copyTextureSupport & CopyTextureSupport.Basic) != 0,
                "ThumbnailAtlasPool requires Graphics.CopyTexture (Basic) support; screenshot thumbnails will not render without it.");

            m_Staging = new Texture2D(k_SlotWidth, k_SlotHeight, TextureFormat.RGBA32, false);
            m_Staging.hideFlags = HideFlags.DontSave;
            m_Staging.name = "ScreenshotsTimelineAtlasStaging";
            m_StagingBuffer = new byte[k_SlotWidth * k_SlotHeight * 4];
        }

        public AtlasSlot AllocateSlot()
        {
            if (m_NextSlotInAtlas >= k_SlotsPerAtlas)
            {
                var atlas = new Texture2D(k_AtlasSize, k_AtlasSize, TextureFormat.RGBA32, false);
                atlas.hideFlags = HideFlags.DontSave;
                atlas.name = $"ScreenshotsTimelineAtlas_{m_Atlases.Count}";
                atlas.filterMode = FilterMode.Bilinear;
                atlas.wrapMode = TextureWrapMode.Clamp;
                // Drop the unused CPU-side copy — atlas is only ever written via
                // Graphics.CopyTexture, which doesn't require the destination to be readable.
                atlas.Apply(false, true);
                m_Atlases.Add(atlas);
                m_CurrentAtlasIndex = m_Atlases.Count - 1;
                m_NextSlotInAtlas = 0;
            }

            var slotIndex = m_NextSlotInAtlas++;
            var col = slotIndex % k_SlotsPerRow;
            var row = slotIndex / k_SlotsPerRow;
            return new AtlasSlot
            {
                Atlas = m_Atlases[m_CurrentAtlasIndex],
                X = col * k_SlotWidth,
                Y = row * k_SlotHeight,
            };
        }

        public void Upload(AtlasSlot slot, byte[] scaledBytes, int thumbWidth, int thumbHeight)
        {
            if (slot.Atlas == null || scaledBytes == null)
                return;
            if (thumbWidth <= 0 || thumbHeight <= 0 || thumbWidth > k_SlotWidth || thumbHeight > k_SlotHeight)
                return;

            if (thumbWidth == k_SlotWidth && thumbHeight == k_SlotHeight)
            {
                m_Staging.LoadRawTextureData(scaledBytes);
            }
            else
            {
                // Copy thumb rows into the bottom-left of the staging buffer. Remaining bytes
                // are uninitialised but never read — CopyTexture only takes thumbW × thumbH
                // from (0,0). LoadRawTextureData requires bytes for the entire staging texture.
                var stagingRowBytes = k_SlotWidth * 4;
                var thumbRowBytes = thumbWidth * 4;
                if (scaledBytes.Length < thumbHeight * thumbRowBytes)
                    return;

                for (var y = 0; y < thumbHeight; y++)
                    Buffer.BlockCopy(scaledBytes, y * thumbRowBytes, m_StagingBuffer, y * stagingRowBytes, thumbRowBytes);
                m_Staging.LoadRawTextureData(m_StagingBuffer);
            }

            m_Staging.Apply(false, false);
            Graphics.CopyTexture(m_Staging, 0, 0, 0, 0, thumbWidth, thumbHeight, slot.Atlas, 0, 0, slot.X, slot.Y);
        }

        // Normalised UV (bottom-left origin) for assignment to Image.uv. We use Image.uv rather
        // than Image.sourceRect (top-left) to avoid the Y-flip on every assignment.
        public static Rect ComputeUv(AtlasSlot slot, int thumbWidth, int thumbHeight)
        {
            const float inv = 1f / k_AtlasSize;
            return new Rect(slot.X * inv, slot.Y * inv, thumbWidth * inv, thumbHeight * inv);
        }


        public void Dispose()
        {
            foreach (var atlas in m_Atlases)
            {
                if (atlas != null)
                    UnityEngine.Object.DestroyImmediate(atlas);
            }
            m_Atlases.Clear();
            m_CurrentAtlasIndex = -1;
            m_NextSlotInAtlas = k_SlotsPerAtlas;

            if (m_Staging != null)
            {
                UnityEngine.Object.DestroyImmediate(m_Staging);
                m_Staging = null;
            }
            m_StagingBuffer = null;
        }
    }
}
