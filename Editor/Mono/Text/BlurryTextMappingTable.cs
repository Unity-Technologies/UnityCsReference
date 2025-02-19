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
        public List<FontAsset> m_SrcFontAsset = new ();
        [SerializeField]
        public List<FontAsset> m_FontAssetCorrespondingFontAssets = new ();
        [SerializeField]
        public List<int> m_FontAssetPointSize = new ();

        Dictionary<Tuple<int, FontAsset>, FontAsset> m_FontAssetPointSizeLookup = new ();

        public void InitializeLookups()
        {
            if (m_FontAssetPointSizeLookup.Count != 0)
                return;

            m_FontAssetPointSizeLookup.Clear();

            for (int i = 0; i < m_SrcFontAsset.Count; i++)
            {
                var key = Tuple.Create(m_FontAssetPointSize[i], m_SrcFontAsset[i]);
                m_FontAssetPointSizeLookup.TryAdd(key, m_FontAssetCorrespondingFontAssets[i]);
            }
        }

        public void Add(FontAsset srcFontAsset, int pointSize, FontAsset dstFontAsset)
        {
            m_SrcFontAsset.Add(srcFontAsset);
            m_FontAssetPointSize.Add(pointSize);
            m_FontAssetCorrespondingFontAssets.Add(dstFontAsset);

            m_FontAssetPointSizeLookup.Add(Tuple.Create(pointSize, srcFontAsset), dstFontAsset);
        }

        public FontAsset Find(FontAsset srcFontAsset, int pointSize)
        {
            return m_FontAssetPointSizeLookup.TryGetValue(Tuple.Create(pointSize, srcFontAsset), out var dstFontAsset) ? dstFontAsset : null;
        }
    }
}
