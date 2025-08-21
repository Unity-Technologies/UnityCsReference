// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class UnityPlayerTags
    {
        readonly ProjectDataStore m_ProjectDataStore;
        readonly SystemDataStore m_SystemDataStore;

        internal UnityPlayerTags(ProjectDataStore projectDataStore, SystemDataStore systemDataStore)
        {
            m_ProjectDataStore = projectDataStore ?? throw new ArgumentNullException();
            m_SystemDataStore = systemDataStore ?? throw new ArgumentNullException();
        }

        public event Action OnUpdated;

        public string[] Tags => m_ProjectDataStore.GetAllPlayerTags();

        public bool Add(string tag, out TagError error)
        {
            error = TagError.None;

            if (EditorApplication.isPlaying)
            {
                error = TagError.InPlayMode;
                return error == TagError.None;
            }
            if (!m_ProjectDataStore.Add(tag, out var e))
            {
                error = e switch
                {
                    ProjectDataStore.ProjectDataStoreError.TagEmpty => TagError.Empty,
                    ProjectDataStore.ProjectDataStoreError.TagDuplicate => TagError.Duplicate,
                    ProjectDataStore.ProjectDataStoreError.TagMissing => TagError.DoesNotExist,
                    ProjectDataStore.ProjectDataStoreError.None => throw new ArgumentOutOfRangeException(),
                    _ => throw new ArgumentOutOfRangeException(),
                };
                return error == TagError.None;
            }

            return error == TagError.None;
        }

        public bool Contains(string tag)
        {
            return m_ProjectDataStore.Has(tag);
        }

        public bool Remove(string tag, out PlayerIdentifier[] playersWhoLostTags, out TagError error)
        {
            error = TagError.None;
            playersWhoLostTags = new PlayerIdentifier[] { };

            if (EditorApplication.isPlaying)
            {
                error = TagError.InPlayMode;
                return error == TagError.None;
            }
            if (!m_ProjectDataStore.Delete(tag, out var e))
            {
                error = e switch
                {
                    ProjectDataStore.ProjectDataStoreError.TagEmpty => TagError.Empty,
                    ProjectDataStore.ProjectDataStoreError.TagDuplicate => TagError.Duplicate,
                    ProjectDataStore.ProjectDataStoreError.TagMissing => TagError.DoesNotExist,
                    ProjectDataStore.ProjectDataStoreError.None => throw new ArgumentOutOfRangeException(),
                    _ => throw new ArgumentOutOfRangeException(),
                };
                return error == TagError.None;
            }

            // When the project data store updates,
            // we update the system data store to keep it in sync
            var playersWhoDeleted = new List<PlayerIdentifier>();
            foreach (var player in UnityPlayer.GetPlayers(m_SystemDataStore))
            {
                if (player.Tags.Contains(tag))
                {
                    player.Tags.Remove(tag);
                    m_SystemDataStore.SavePlayerJson(player.Index, player);
                    playersWhoDeleted.Add(player.PlayerIdentifier);
                }
            }
            playersWhoLostTags = playersWhoDeleted.ToArray();

            OnUpdated?.Invoke();
            return error == TagError.None;
        }
    }
}
