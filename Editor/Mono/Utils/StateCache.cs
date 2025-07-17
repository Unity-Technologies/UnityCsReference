// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor
{
    class StateCache<T>
    {
        string m_CacheFolder;
        PersistenceFormat m_PersistenceFormat;
        Dictionary<Hash128, T> m_Cache = new Dictionary<Hash128, T>();
        Action<BinaryWriter, T> m_CustomBinarySerializationCallback = null;
        Func<BinaryReader, T> m_CustomBinaryDeserializationCallback = null;

        public string cacheFolderPath { get { return m_CacheFolder; } }

        public enum PersistenceFormat
        {
            JSON,
            Binary
        }

        // Construct JSON serialization StateCache
        public StateCache(string cacheFolder)
        {
            m_PersistenceFormat = PersistenceFormat.JSON;
            SetupCacheFolder(cacheFolder);
        }

        // Construct binary serialization StateCache
        public StateCache(string cacheFolder, Action<BinaryWriter, T> customBinarySerFunc, Func<BinaryReader, T> customBinaryDeserFunc)
        {
            m_PersistenceFormat = PersistenceFormat.Binary;
            m_CustomBinarySerializationCallback = customBinarySerFunc;
            m_CustomBinaryDeserializationCallback = customBinaryDeserFunc;
            SetupCacheFolder(cacheFolder);
        }

        void SetupCacheFolder(string cacheFolder)
        {
            if (string.IsNullOrEmpty(cacheFolder))
                throw new ArgumentException("cacheFolder cannot be null or empty string", cacheFolder);

            if (cacheFolder.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new ArgumentException("Cache folder path has invalid path characters: '" + cacheFolder + "'");

            cacheFolder = cacheFolder.ConvertSeparatorsToUnity();
            if (!cacheFolder.EndsWith("/"))
            {
                Debug.LogError($"The cache folder path should end with a forward slash: '/'. Path: {cacheFolder}. Fixed up.");
                cacheFolder += "/";
            }
            if (cacheFolder.StartsWith("/"))
            {
                Debug.LogError($"The cache folder path should not start with a forward slash: '/'. Path: {cacheFolder}. Fixed up."); // since on OSX a leading '/' means the root directory
                cacheFolder = cacheFolder.TrimStart(new[] { '/' });
            }

            m_CacheFolder = cacheFolder;
        }

        public void SetState(Hash128 key, T obj)
        {
            ThrowIfInvalid(key);

            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var filePath = GetFilePathForKey(key);
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(directory);

                switch (m_PersistenceFormat)
                {
                    case PersistenceFormat.Binary: SaveBinaryState(filePath, obj); break;
                    case PersistenceFormat.JSON: SaveJsonState(filePath, obj); break;
                    default: throw new NotImplementedException(m_PersistenceFormat.ToString());
                }
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

            if (m_Cache.TryGetValue(key, out T obj))
                return obj;

            string filePath = GetFilePathForKey(key);
            if (File.Exists(filePath))
            {
                switch (m_PersistenceFormat)
                {
                    case PersistenceFormat.Binary: obj = LoadBinaryState(key, filePath, defaultValue); break;
                    case PersistenceFormat.JSON: obj = LoadJsonState(key, filePath, defaultValue); break;
                    default: throw new NotImplementedException(m_PersistenceFormat.ToString());
                }

                m_Cache[key] = obj;
                return obj;
            }

            return defaultValue;
        }

        void SaveBinaryState(string filePath, T value)
        {
            if (m_CustomBinarySerializationCallback is null)
            {
                Debug.LogError($"StateCache for '{typeof(T).Name}' does not specify a callback to handle binary serialization.");
                return;
            }

            using (var fs = new FileStream(filePath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                try
                {
                    m_CustomBinarySerializationCallback(writer, value);
                }
                catch (Exception e)
                {
                    fs.Close();
                    Debug.LogError($"Serialization failed for {filePath}. Error: {e}");
                }
            }
        }

        T LoadBinaryState(Hash128 key, string filePath, T defaultValue)
        {
            if (m_CustomBinaryDeserializationCallback is null)
            {
                Debug.LogError($"StateCache for '{typeof(T).Name}' does not specify a callback to handle binary deserialization.");
                return defaultValue;
            }

            using (var fs = new FileStream(filePath, FileMode.Open))
            using (var reader = new BinaryReader(fs))
            {
                try
                {
                    return m_CustomBinaryDeserializationCallback(reader);
                }
                catch (Exception e)
                {
                    fs.Close();
                    Debug.LogError($"Invalid file content for {filePath}. Removing file. Error: {e}");
                    RemoveState(key);
                    return defaultValue;
                }
            }
        }

        void SaveJsonState(string filePath, T value)
        {
            string json = JsonUtility.ToJson(value);
            File.WriteAllText(filePath, json, Encoding.UTF8); // Persist state
        }

        T LoadJsonState(Hash128 key, string filePath, T defaultValue)
        {
            string jsonString = null;
            try
            {
                jsonString = File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading file {filePath}. Error: {e}");
                return defaultValue;
            }

            try
            {
                return JsonUtility.FromJson<T>(jsonString);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"Invalid file content for {filePath}. Removing file. Error: {exception}");
                RemoveState(key);
                return defaultValue;
            }
        }

        public void RemoveState(Hash128 key)
        {
            ThrowIfInvalid(key);

            m_Cache.Remove(key);

            string filePath = GetFilePathForKey(key);
            if (File.Exists(filePath))
                File.Delete(filePath);
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
            string fileExtension = string.Empty;
            switch (m_PersistenceFormat)
            {
                case PersistenceFormat.Binary: fileExtension = ".bin"; break;
                case PersistenceFormat.JSON: fileExtension = ".json"; break;
                default: throw new NotImplementedException(m_PersistenceFormat.ToString());
            }
            return m_CacheFolder + hexFolder + hexKey + fileExtension;
        }
    }
}
