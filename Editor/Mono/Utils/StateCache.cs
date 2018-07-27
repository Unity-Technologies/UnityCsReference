// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor
{
    class StateCache
    {
        string m_CacheFolder = "Library/StateCache/";
        Dictionary<string, string> m_Cache = new Dictionary<string, string>();

        public StateCache()
        {
        }

        public StateCache(string cacheFolderPath)
        {
            if (ValidateCacheFolder(cacheFolderPath))
                m_CacheFolder = cacheFolderPath;
        }

        public void SetState<T>(string key, T obj)
        {
            string json = JsonUtility.ToJson(obj);
            m_Cache[key] = json;
            var filePath = GetFilePath(key);
            string directory = System.IO.Path.GetDirectoryName(filePath);
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.WriteAllText(filePath, json, Encoding.UTF8); // Persist state
        }

        public T GetState<T>(string key)
        {
            string jsonString;
            if (!m_Cache.TryGetValue(key, out jsonString))
            {
                if (System.IO.File.Exists(GetFilePath(key)))
                    jsonString = System.IO.File.ReadAllText(GetFilePath(key)); // Fetch persisted state
            }
            return JsonUtility.FromJson<T>(jsonString);
        }

        public void RemoveState(string key)
        {
            m_Cache.Remove(key);
            System.IO.File.Delete(GetFilePath(key));
        }

        string GetFilePath(string fileName)
        {
            return m_CacheFolder + fileName + ".json";
        }

        bool ValidateCacheFolder(string cacheFolder)
        {
            if (string.IsNullOrEmpty(cacheFolder))
            {
                Debug.LogError("Invalid cache folder: Is null or empty");
                return false;
            }

            if (!m_CacheFolder.EndsWith("/"))
            {
                Debug.LogError("The cache folder path provided should end with a forward slash: '/'. Path provided: " + m_CacheFolder);
                return false;
            }

            return true;
        }
    }
}
