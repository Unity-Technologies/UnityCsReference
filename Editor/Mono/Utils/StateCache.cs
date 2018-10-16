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
        Dictionary<string, T> m_Cache = new Dictionary<string, T>();

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

        public string cacheFolderPath { get { return m_CacheFolder; } }

        public void SetState(string key, T obj)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key cannot be null or empty string", key);

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
                Debug.LogError(string.Format("Error saving file {0}. Error: {1}", filePath, e.ToString()));
            }

            m_Cache[key] = obj;
        }

        public T GetState(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key cannot be null or empty string", key);

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
                    Debug.LogError(string.Format("Error loading file {0}. Error: {1}", filePath, e.ToString()));
                    return default(T);
                }

                try
                {
                    obj = JsonUtility.FromJson<T>(jsonString);
                }
                catch (ArgumentException exception)
                {
                    Debug.LogError(string.Format("Invalid file content for {0}. Removing file. Error: {1}", filePath, exception.ToString()));
                    RemoveState(key);
                    return default(T);
                }

                m_Cache[key] = obj;
                return obj;
            }

            return default(T);
        }

        public void RemoveState(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key cannot be null or empty string", key);

            m_Cache.Remove(key);

            string filePath = GetFilePathForKey(key);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        public string GetFilePathForKey(string key)
        {
            return m_CacheFolder + key + ".json";
        }
    }
}
