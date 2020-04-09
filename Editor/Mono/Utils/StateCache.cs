// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor
{
    class StateCache<T>
    {
        string m_CacheFolder;
        Dictionary<Hash128, T> m_Cache = new Dictionary<Hash128, T>();

        public string cacheFolderPath { get { return m_CacheFolder; } }

        public StateCache(string cacheFolder)
        {
            if (string.IsNullOrEmpty(cacheFolder))
                throw new ArgumentException("cacheFolder cannot be null or empty string", cacheFolder);

            if (cacheFolder.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
            {
                throw new ArgumentException("Cache folder path has invalid path characters: '" + cacheFolder + "'");
            }

            cacheFolder = cacheFolder.ConvertSeparatorsToUnity();
            if (!cacheFolder.EndsWith("/"))
            {
                Debug.LogError("The cache folder path should end with a forward slash: '/'. Path: " + cacheFolder + ". Fixed up.");
                cacheFolder += "/";
            }
            if (cacheFolder.StartsWith("/"))
            {
                Debug.LogError("The cache folder path should not start with a forward slash: '/'. Path: " + cacheFolder + ". Fixed up."); // since on OSX a leading '/' means the root directory
                cacheFolder = cacheFolder.TrimStart(new[] { '/' });
            }

            m_CacheFolder = cacheFolder;
        }

        public void SetState(Hash128 key, T obj)
        {
            ThrowIfInvalid(key);

            if (obj == null)
                throw new ArgumentNullException("obj");

            string json = JsonUtility.ToJson(obj);
            var filePath = GetFilePathForKey(key);
            try
            {
                string directory = System.IO.Path.GetDirectoryName(filePath);
                System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllText(filePath, json, Encoding.UTF8); // Persist state
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Error saving file {0}. Error: {1}", filePath, e));
            }

            m_Cache[key] = obj;
        }

        public T GetState(Hash128 key, T defaultValue = default(T))
        {
            ThrowIfInvalid(key);

            T obj;
            if (m_Cache.TryGetValue(key, out obj))
                return obj;

            string filePath = GetFilePathForKey(key);
            if (System.IO.File.Exists(filePath))
            {
                string jsonString = null;
                try
                {
                    jsonString = System.IO.File.ReadAllText(filePath, Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format("Error loading file {0}. Error: {1}", filePath, e));
                    return defaultValue;
                }

                try
                {
                    obj = JsonUtility.FromJson<T>(jsonString);
                }
                catch (ArgumentException exception)
                {
                    Debug.LogError(string.Format("Invalid file content for {0}. Removing file. Error: {1}", filePath, exception));
                    RemoveState(key);
                    return defaultValue;
                }

                m_Cache[key] = obj;
                return obj;
            }

            return defaultValue;
        }

        public void RemoveState(Hash128 key)
        {
            ThrowIfInvalid(key);

            m_Cache.Remove(key);

            string filePath = GetFilePathForKey(key);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        void ThrowIfInvalid(Hash128 key)
        {
            if (!key.isValid)
                throw new ArgumentException("Hash128 key is invalid: " + key.ToString());
        }

        public string GetFilePathForKey(Hash128 key)
        {
            // Hashed folder structure to ensure we scale with large amounts of state files.
            // See: https://medium.com/eonian-technologies/file-name-hashing-creating-a-hashed-directory-structure-eabb03aa4091
            string hexKey = key.ToString();
            string hexFolder = hexKey.Substring(0, 2) + "/";
            return m_CacheFolder + hexFolder + hexKey + ".json";
        }
    }
}
