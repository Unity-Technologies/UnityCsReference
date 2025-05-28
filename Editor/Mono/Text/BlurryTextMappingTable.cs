// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace UnityEditor
{
    // Stores FontAsset to ensure they are not destroyed after domain reload
    [Serializable]
    internal class BlurryTextCaching
    {
        [SerializeField]
        private List<FontAssetEntry> m_Entries = new();

        private Dictionary<Tuple<int, FontAsset, bool>, FontAsset> m_FontAssetPointSizeLookup = new();

        public void InitializeLookups()
        {
            if (m_FontAssetPointSizeLookup.Count != 0)
                return;

            m_FontAssetPointSizeLookup.Clear();

            foreach (var entry in m_Entries)
            {
                var key = Tuple.Create(entry.pointSize, entry.sourceFontAsset, entry.isRaster);
                m_FontAssetPointSizeLookup.TryAdd(key, entry.correspondingFontAsset);
            }
        }

        public void Add(FontAsset sourceFontAsset, int pointSize, bool isRaster, FontAsset correspondingFontAsset)
        {
            m_Entries.Add(new FontAssetEntry
            {
                sourceFontAsset = sourceFontAsset,
                pointSize = pointSize,
                isRaster = isRaster,
                correspondingFontAsset = correspondingFontAsset
            });

            m_FontAssetPointSizeLookup.Add(Tuple.Create(pointSize, sourceFontAsset, isRaster), correspondingFontAsset);
        }

        public FontAsset Find(FontAsset sourceFontAsset, int pointSize, bool isRaster)
        {
            return m_FontAssetPointSizeLookup.TryGetValue(Tuple.Create(pointSize, sourceFontAsset, isRaster), out var correspondingFontAsset) ? correspondingFontAsset : null;
        }

        [Serializable]
        private class FontAssetEntry
        {
            public FontAsset sourceFontAsset;
            public FontAsset correspondingFontAsset;
            public int pointSize;
            public bool isRaster;
        }
    }
}
