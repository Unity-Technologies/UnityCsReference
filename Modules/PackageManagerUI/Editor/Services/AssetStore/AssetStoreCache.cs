// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class AssetStoreCache
    {
        static IAssetStoreCache s_Instance = null;
        public static IAssetStoreCache instance => s_Instance ?? AssetStoreCacheInternal.instance;

        [Serializable]
        [FilePathAttribute("Asset Store/Cache/AssetStore.cache", FilePathAttribute.Location.AppDataFolder)]
        internal class AssetStoreCacheInternal : ScriptableSingleton<AssetStoreCacheInternal>, IAssetStoreCache, ISerializationCallbackReceiver
        {
            private Dictionary<long, string> m_ProductETags = new Dictionary<long, string>();

            [NonSerialized]
            private bool m_IsModified;

            [SerializeField]
            private long[] m_SerializedIds = new long[0];

            [SerializeField]
            private string[] m_SerializedETags = new string[0];

            public void OnBeforeSerialize()
            {
                m_SerializedIds = m_ProductETags.Keys.ToArray();
                m_SerializedETags = m_ProductETags.Values.ToArray();
            }

            public void OnAfterDeserialize()
            {
                for (var i = 0; i < m_SerializedIds.Length; i++)
                {
                    m_ProductETags[m_SerializedIds[i]] = m_SerializedETags[i];
                }
            }

            private void OnDisable()
            {
                if (m_IsModified)
                {
                    Save(true);
                    m_IsModified = false;
                }
            }

            public string GetLastETag(long productId)
            {
                return m_ProductETags.ContainsKey(productId) ? m_ProductETags[productId] : string.Empty;
            }

            public void SetLastETag(long productId, string etag)
            {
                var lastEtag = GetLastETag(productId);
                if (etag != lastEtag)
                {
                    m_ProductETags[productId] = etag;
                    m_IsModified = true;
                }
            }

            public Texture2D LoadImage(long productId, string url)
            {
                if (string.IsNullOrEmpty(url))
                    return null;

                var hash = Hash128.Compute(url);
                var path = Paths.Combine(ApplicationUtil.instance.userAppDataPath, "Asset Store", "Cache", "Images", productId.ToString(), hash.ToString());
                if (File.Exists(path))
                {
                    var texture = new Texture2D(2, 2);
                    if (texture.LoadImage(File.ReadAllBytes(path)))
                        return texture;
                }

                return null;
            }

            public void SaveImage(long productId, string url, Texture2D texture)
            {
                if (string.IsNullOrEmpty(url) || texture == null)
                    return;

                var path = Paths.Combine(ApplicationUtil.instance.userAppDataPath, "Asset Store", "Cache", "Images", productId.ToString());
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var hash = Hash128.Compute(url);
                path = Paths.Combine(path, hash.ToString());
                File.WriteAllBytes(path, texture.EncodeToJPG());
            }

            public void ClearCache()
            {
                m_ProductETags.Clear();
                m_IsModified = false;
            }
        }
    }
}
