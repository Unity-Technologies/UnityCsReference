// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    enum SystemDataStoreError
    {
        None,
        SystemDataIsNull,
        SystemDataPlayerDataIsNullOrEmpty,
        SystemDataPlayersAreNull,
        SystemDataPlayersHaveInvalidIdentifiers,
    }

    class SystemDataStore
    {
        internal const string Filename = "SystemData.json";
        const string SystemDataKey = "SystemData";

        static readonly string DataStorePathRelativeToMainEditor = Paths.CurrentProjectVirtualProjectsFolder;
        static readonly string DataStorePathRelativeToCloneEditor = Paths.GetCurrentProjectDataPath("..", "..");
        static FileSystemDelegates s_FileSystemDelegates;
        static ParsingSystemDelegates s_ParsingSystemDelegates;

        readonly InMemoryRepository<string, SystemData> m_Cache = new();
        readonly string m_FullPath;

        static string MainPath { get; } = Path.Combine(DataStorePathRelativeToMainEditor, Filename);
        static string ClonePath { get; } = Path.Combine(DataStorePathRelativeToCloneEditor, Filename);
        internal static string GetMainLastWriteTime() => GetLastWriteTime(MainPath);
        internal static string GetCloneLastWriteTime() => GetLastWriteTime(ClonePath);
        static string GetLastWriteTime(string path) => s_FileSystemDelegates.LastFileWriteTimeFunc(path);

        internal static void Initialize(FileSystemDelegates fileSystemDelegates, ParsingSystemDelegates parsingSystemDelegates)
        {
            s_FileSystemDelegates = fileSystemDelegates;
            s_ParsingSystemDelegates = parsingSystemDelegates;
        }

        internal static SystemDataStore GetMain()
        {
            var directoryPath = DataStorePathRelativeToMainEditor;
            var dataStore = new SystemDataStore(MainPath);
            s_FileSystemDelegates.CreateDirectoryFunc(directoryPath);

            var hasNoSystemDataOnDisk = !s_FileSystemDelegates.ExistsFileFunc(dataStore.m_FullPath);
            if (hasNoSystemDataOnDisk)
            {
                // In case the user deletes the library folder with the editor open
                var state = new SystemData
                {
                    IsMppmActive = true,
                    Data =
                    {
                        {
                            1, PlayerStateJson.NewMain()
                        },
                        {
                            2, PlayerStateJson.NewClone(2)
                        },
                        {
                            3, PlayerStateJson.NewClone(3)
                        },
                        {
                            4, PlayerStateJson.NewClone(4)
                        },
                    },
                };
                dataStore.m_Cache.Create(SystemDataKey, state);
                SerializeToPath(dataStore.m_FullPath, state);
            }
            else
            {
                ReadFromFilePopulateDataStore(dataStore);
            }

            return dataStore;
        }

        internal static SystemDataStore GetClone()
        {
            Debug.Assert(s_FileSystemDelegates.ExistsFileFunc(ClonePath), "A clone started up without a system data store. The main editor should have already created this!");
            var dataStore = new SystemDataStore(ClonePath);
            ReadFromFilePopulateDataStore(dataStore);
            return dataStore;
        }

        internal static SystemDataStore GetTest(string directoryPath, bool shouldCreateSystemData = false, bool shouldPopulateData = false)
        {
            var fullPath = Path.Combine(directoryPath, Filename);
            var dataStore = new SystemDataStore(fullPath);

            if (shouldCreateSystemData)
            {
                var state = new SystemData();
                if (shouldPopulateData)
                {
                    state.IsMppmActive = true;
                    state.Data.Add(1, PlayerStateJson.NewMain());
                    state.Data.Add(2, PlayerStateJson.NewClone(2));
                    state.Data.Add(3, PlayerStateJson.NewClone(3));
                    state.Data.Add(4, PlayerStateJson.NewClone(4));
                }
                dataStore.m_Cache.Create(SystemDataKey, state);
                SerializeToPath(dataStore.m_FullPath, state);
            }

            return dataStore;
        }

        SystemDataStore(string fullPath)
        {
            m_FullPath = fullPath;
        }

        public void SavePlayerJson(int index, PlayerStateJson playerStateJson)
        {
            SystemData state;
            if (!m_Cache.ContainsKey(SystemDataKey))
            {
                state = new SystemData
                {
                    Data =
                    {
                        [index] = playerStateJson,
                    },
                };
                m_Cache.Create(SystemDataKey, state);
            }
            else
            {
                m_Cache.Update(SystemDataKey, players => players.Data[index] = playerStateJson, out state);
            }

            SerializeToPath(m_FullPath, state);
        }

        public bool TryLoadPlayerJson(int index, out PlayerStateJson playerStateJson)
        {
            playerStateJson = null;
            return m_Cache.TryGetValue(SystemDataKey, out var state) && state.Data.TryGetValue(index, out playerStateJson);
        }

        public Dictionary<int, PlayerStateJson> LoadAllPlayerJson()
        {
            if (!m_Cache.TryGetValue(SystemDataKey, out var state))
            {
                state = new SystemData();
                m_Cache.Create(SystemDataKey, state);
            }

            // Make sure we have no null tags
            foreach (var kv in state.Data)
            {
                kv.Value.Tags ??= new List<string>();
            }

            return state.Data;
        }

        public bool GetIsMppmActive()
        {
            // NOTE: We are active by default
            return !m_Cache.TryGetValue(SystemDataKey, out var state) || state.IsMppmActive;
        }

        // Should only being called by MultiplayerPlayModeSettings
        public void UpdateIsMppmActive(bool isActive)
        {
            SystemData state;
            if (!m_Cache.ContainsKey(SystemDataKey))
            {
                state = new SystemData
                {
                    IsMppmActive = isActive,
                };
                m_Cache.Create(SystemDataKey, state);
            }
            else
            {
                m_Cache.Update(SystemDataKey, players => players.IsMppmActive = isActive, out state);
            }

            SerializeToPath(m_FullPath, state);
        }

        public bool GetMutePlayers()
        {
            return m_Cache.TryGetValue(SystemDataKey, out var state) && state.IsMutePlayers;
        }

        public void UpdateMutePlayers(bool value)
        {
            SystemData state;
            if (!m_Cache.ContainsKey(SystemDataKey))
            {
                state = new SystemData
                {
                    IsMutePlayers = value,
                };
                m_Cache.Create(SystemDataKey, state);
            }
            else
            {
                m_Cache.Update(SystemDataKey, players => players.IsMutePlayers = value, out state);
            }

            SerializeToPath(m_FullPath, state);
        }

        public void DeleteAll() // Note: This deletes the editor version data as well
        {
            m_Cache.Delete(SystemDataKey);
            s_FileSystemDelegates.DeleteFileFunc(m_FullPath);
        }

        public static void ReadFromFilePopulateDataStore(SystemDataStore dataStore)
        {
            var playerJson = DeserializeFromPath(dataStore.m_FullPath);
            if (IsNullOrInvalid(playerJson, out var error))
            {
                MppmLog.Warning($"Player data at '{dataStore.m_FullPath}' is null or invalid! [{error}]");
            }

            dataStore.m_Cache.Create(SystemDataKey, playerJson);
        }

        static void SerializeToPath(string path, SystemData systemData)
        {
            s_FileSystemDelegates.WriteBytesFunc(path, Encoding.UTF8.GetBytes(SystemData.Serialize(s_ParsingSystemDelegates, systemData)));
        }

        static SystemData DeserializeFromPath(string path)
        {
            var json = Encoding.UTF8.GetString(s_FileSystemDelegates.ReadBytesFunc(path));
            var hasDeserialized = SystemData.TryDeserialize(s_ParsingSystemDelegates, json, out var data);
            Debug.Assert(hasDeserialized, $"Failed to Deserialize System Data [{json}] at path '{path}'.");
            return data;
        }

        internal static bool IsNullOrInvalid(SystemData systemData, out SystemDataStoreError error)
        {
            if (systemData == null)
            {
                error = SystemDataStoreError.SystemDataIsNull;
                return true;
            }

            if (systemData.Data == null || systemData.Data.Count == 0)
            {
                error = SystemDataStoreError.SystemDataPlayerDataIsNullOrEmpty;
                return true;
            }

            foreach (var p in systemData.Data.Values)
            {
                // We currently don't have null/empty players. but if we ever decide to do so then this needs to change
                if (p == null)
                {
                    error = SystemDataStoreError.SystemDataPlayersAreNull;
                    return true;
                }
                if (p.PlayerIdentifier == null || p.PlayerIdentifier.Guid == Guid.Empty)
                {
                    error = SystemDataStoreError.SystemDataPlayersHaveInvalidIdentifiers;
                    return true;
                }
            }

            error = SystemDataStoreError.None;
            return false;
        }
    }
}
