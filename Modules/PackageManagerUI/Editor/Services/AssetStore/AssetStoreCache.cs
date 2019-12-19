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
        internal class AssetStoreCacheInternal : ScriptableSingleton<AssetStoreCacheInternal>, IAssetStoreCache, ISerializationCallbackReceiver
        {
            private Dictionary<string, string> m_ETags = new Dictionary<string, string>();

            private Dictionary<string, long> m_Categories = new Dictionary<string, long>();

            [SerializeField]
            private string[] m_SerializedKeys = new string[0];

            [SerializeField]
            private string[] m_SerializedETags = new string[0];

            [SerializeField]
            private string[] m_SerializedCategories = new string[0];

            [SerializeField]
            private long[] m_SerializedCategoryCounts = new long[0];

            public void OnBeforeSerialize()
            {
                m_SerializedKeys = m_ETags.Keys.ToArray();
                m_SerializedETags = m_ETags.Values.ToArray();

                m_SerializedCategories = m_Categories.Keys.ToArray();
                m_SerializedCategoryCounts = m_Categories.Values.ToArray();
            }

            public void OnAfterDeserialize()
            {
                for (var i = 0; i < m_SerializedKeys.Length; i++)
                    m_ETags[m_SerializedKeys[i]] = m_SerializedETags[i];

                for (var i = 0; i < m_SerializedCategories.Length; i++)
                    m_Categories[m_SerializedCategories[i]] = m_SerializedCategoryCounts[i];
            }

            public string GetLastETag(string key)
            {
                return m_ETags.ContainsKey(key) ? m_ETags[key] : string.Empty;
            }

            public void SetLastETag(string key, string etag)
            {
                m_ETags[key] = etag;
            }

            public void SetCategory(string category, long count)
            {
                m_Categories[category] = count;
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
                m_ETags.Clear();
                m_Categories.Clear();
            }
        }
    }
}
