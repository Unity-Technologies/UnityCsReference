// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class ProjectDataStore
    {
        public enum ProjectDataStoreError
        {
            None,
            TagEmpty,
            TagDuplicate,
            TagMissing,
        }

        internal const string k_Filename = "VirtualProjectsConfig.json";
        const string k_InMemoryPlayerTagsKey = "playerTags";

        static readonly string DataStorePathRelativeToMainEditor = Paths.GetCurrentProjectDataPath("..", "ProjectSettings");

        // Use both the InMemoryRepository and VirtualProjectConfig to store player tags -
        // InMemoryRepository is the cache, PlayerTagsData(JSON File) is the persistence.
        readonly InMemoryRepository<string, List<string>> m_TagsRepository = new();
        readonly string m_Path;
        public readonly bool HasChangedVersion;
        PlayerTagsData m_PlayerTagsData;

        internal static ProjectDataStore GetMain()
        {
            return new ProjectDataStore(DataStorePathRelativeToMainEditor);
        }

        internal ProjectDataStore(string directory)
        {
            m_Path = Path.Combine(directory, k_Filename);

            var assembly = typeof(ProjectDataStore).Assembly;
            var currentVersion = Application.unityVersion;

            if (File.Exists(m_Path))
            {
                m_PlayerTagsData = Deserialize(m_Path);
            }

            // Create the initial cache from the persistent config
            var tags = ReadPlayerTags();
            m_TagsRepository.Create(k_InMemoryPlayerTagsKey, tags);

            if (m_PlayerTagsData.version != currentVersion)
            {
                if (!string.IsNullOrWhiteSpace(m_PlayerTagsData.version))
                {
                    HasChangedVersion = true;
                    ReimportOldScenarioAssets();
                }

                m_PlayerTagsData.version = currentVersion;
                m_PlayerTagsData.PlayerTags = tags;
                var json = JsonConvert.SerializeObject(m_PlayerTagsData, Formatting.Indented);
                // MTTB-566
                UnityEditor.AssetDatabase.MakeEditable(m_Path);
                File.WriteAllBytes(m_Path, Encoding.UTF8.GetBytes(json));
            }
        }

        private void ReimportOldScenarioAssets()
        {
            try
            {
                var majorVersion = int.Parse(m_PlayerTagsData.version.Split('.')[0]);
                if (majorVersion >= 6000)
                    return;
            }
            catch (Exception)
            {
                return;
            }

            // In the case where there are old scenario assets (from before 6.3), those assets need to be reimported
            // to update their type GUIDs in the asset database so they can be found using the AssetDatabase.FindAssets API.
            // To prevent the overhead of scanning all assets in project, this will work only for assets under the default path,
            // so users with assets outside of it will need to reimport them manually.
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { PlayModeScenarioUtils.k_ConfigAssetsPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!typeof(PlayModeScenario).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path)))
                    continue;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        public string[] GetAllPlayerTags()
        {
            var nonNullTags = new List<string>();
            if (m_TagsRepository.TryGetValue(k_InMemoryPlayerTagsKey, out var tags))
            {
                foreach (var tag in tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        nonNullTags.Add(tag);
                    }
                }
            }
            return nonNullTags.ToArray();
        }

        public bool Add(string tag, out ProjectDataStoreError error)
        {
            error = ProjectDataStoreError.None;
            if (string.IsNullOrWhiteSpace(tag))
            {
                error = ProjectDataStoreError.TagEmpty;
                return false;
            }

            if (Contains(tag))
            {
                error = ProjectDataStoreError.TagDuplicate;
                return false;
            }

            var tags = new List<string>();
            if (!m_TagsRepository.ContainsKey(k_InMemoryPlayerTagsKey))
            {
                tags.Add(tag);
                m_TagsRepository.Create(k_InMemoryPlayerTagsKey, tags);
            }
            else
            {
                m_TagsRepository.Update(k_InMemoryPlayerTagsKey, t => { t.Add(tag); }, out tags);
            }

            WritePlayerTags(tags);
            return true;
        }

        public bool Has(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && Contains(tag);
        }

        public bool Delete(string tag, out ProjectDataStoreError error)
        {
            error = ProjectDataStoreError.None;
            if (string.IsNullOrWhiteSpace(tag))
            {
                error = ProjectDataStoreError.TagEmpty;
                return false;
            }

            if (!Contains(tag))
            {
                error = ProjectDataStoreError.TagMissing;
                return false;
            }

            m_TagsRepository.Update(k_InMemoryPlayerTagsKey, tags => tags.Remove(tag), out var state);

            WritePlayerTags(state);
            return true;
        }

        internal void DeleteAll()
        {
            m_TagsRepository.Delete(k_InMemoryPlayerTagsKey);
            WritePlayerTags(new List<string>());
        }

        bool Contains(string tag)
        {
            return m_TagsRepository.TryGetValue(k_InMemoryPlayerTagsKey, out var tags) && tags.Contains(tag);
        }

        internal List<string> ReadPlayerTags()
        {
            if (!File.Exists(m_Path))
            {
                return new List<string>();
            }

            m_PlayerTagsData = Deserialize(m_Path);
            return m_PlayerTagsData.PlayerTags;
        }

        internal void WritePlayerTags(List<string> playerTags)
        {
            m_PlayerTagsData.PlayerTags = playerTags;
            var json = JsonConvert.SerializeObject(m_PlayerTagsData, Formatting.Indented);
            File.WriteAllBytes(m_Path, Encoding.UTF8.GetBytes(json));
        }

        static PlayerTagsData Deserialize(string path)
        {
            try
            {
                return JsonConvert.DeserializeObject<PlayerTagsData>(Encoding.UTF8.GetString(File.ReadAllBytes(path)));
            }
            catch (Exception e)
            {
                MppmLog.Error($"Could not load configuration file at {path} with error: {e.Message}");
                return new PlayerTagsData();
            }
        }
    }
}
