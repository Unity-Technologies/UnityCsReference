// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    // Inspired by UnityEditor.StateCache<T>, which (1) is internal and (2) is lacking the
    // ability to hold different state types.
    sealed class StateCache_Internal : IDisposable
    {
        struct FileData
        {
            public string Data;
            public bool IsDirty;
        }

        Dictionary<string, FileData> m_InMemoryCache = new Dictionary<string, FileData>();
        string m_CacheFolder;

        public StateCache_Internal(string cacheFolder)
        {
            if (string.IsNullOrEmpty(cacheFolder))
                throw new ArgumentException(nameof(cacheFolder) + " cannot be null or empty string", cacheFolder);

            if (cacheFolder.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                throw new ArgumentException("Cache folder path has invalid path characters: '" + cacheFolder + "'");
            }

            cacheFolder = ConvertSeparatorsToUnity(cacheFolder);
            if (!cacheFolder.EndsWith("/"))
            {
                Debug.LogError("The cache folder path should end with a forward slash: '/'. Path: " + cacheFolder + ". Fixed up.");
                cacheFolder += "/";
            }
            if (cacheFolder.StartsWith("/"))
            {
                Debug.LogError("The cache folder path should not start with a forward slash: '/'. Path: " + cacheFolder + ". Fixed up."); // since on OSX a leading '/' means the root directory
                cacheFolder = cacheFolder.TrimStart('/');
            }

            m_CacheFolder = cacheFolder;
        }

        /// <inheritdoc />
        ~StateCache_Internal() {
            Flush();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Flush();
            GC.SuppressFinalize(this);
        }

        static string ConvertSeparatorsToUnity(string path)
        {
            return path.Replace('\\', '/');
        }

        bool FileExists(string path)
        {
            return m_InMemoryCache.TryGetValue(path, out _) || File.Exists(path);
        }

        string ReadFile(string path)
        {
            if (!m_InMemoryCache.TryGetValue(path, out var data))
            {
                try
                {

                    var content = File.ReadAllText(path, Encoding.UTF8);
                    m_InMemoryCache[path] = new FileData { Data = content, IsDirty = false };
                    TruncateCache();
                    return content;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading file {path}. Error: {e}");
                    return null;
                }
            }

            return data.Data;
        }

        void WriteFile(string path, string data)
        {
            m_InMemoryCache[path] = new FileData { Data = data, IsDirty = true };
            TruncateCache();
        }

        void TruncateCache()
        {
            if (m_InMemoryCache.Count > 64)
            {
                Flush();
            }
        }

        public TComponent GetState<TComponent>(Hash128 key, Func<TComponent> defaultValueCreator = null) where TComponent : class, IStateComponent
        {
            ThrowIfInvalid(key);

            TComponent obj = null;
            var filePath = GetFilePathForKey_Internal(key);
            if (FileExists(filePath))
            {
                var serializedData = ReadFile(filePath);
                if (serializedData != null)
                {
                    try
                    {
                        obj = JsonUtility.FromJson<TComponent>(serializedData);
                    }
                    catch (ArgumentException exception)
                    {
                        Debug.LogError($"Invalid file content for {filePath}. Removing file. Error: {exception}");

                        // Remove invalid content
                        RemoveState(key);
                        obj = null;
                    }
                }
            }

            return obj ?? defaultValueCreator?.Invoke();
        }

        public void StoreState(Hash128 key, IStateComponent stateComponent)
        {
            var filePath = GetFilePathForKey_Internal(key);
            var serializedData = JsonUtility.ToJson(stateComponent);
            WriteFile(filePath, serializedData);
        }

        public void RemoveState(Hash128 key)
        {
            ThrowIfInvalid(key);

            string filePath = GetFilePathForKey_Internal(key);
            m_InMemoryCache.Remove(filePath);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public void Flush()
        {
            bool noErr = true;
            foreach (var kv in m_InMemoryCache)
            {
                if (!kv.Value.IsDirty)
                    continue;

                try
                {
                    var directory = Path.GetDirectoryName(kv.Key);
                    if (directory != null)
                    {
                        Directory.CreateDirectory(directory);
                        File.WriteAllText(kv.Key, kv.Value.Data, Encoding.UTF8);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error saving file {kv.Key}. Error: {e}");
                    noErr = false;
                }
            }

            if (noErr)
            {
                m_InMemoryCache.Clear();
            }
        }

        static void ThrowIfInvalid(Hash128 key)
        {
            if (!key.isValid)
                throw new ArgumentException("Hash128 key is invalid: " + key);
        }

        internal string GetFilePathForKey_Internal(Hash128 key)
        {
            // Hashed folder structure to ensure we scale with large amounts of state files.
            // See: https://medium.com/eonian-technologies/file-name-hashing-creating-a-hashed-directory-structure-eabb03aa4091
            string hexKey = key.ToString();
            string hexFolder = hexKey.Substring(0, 2) + "/";
            return m_CacheFolder + hexFolder + hexKey + ".json";
        }
    }
}
