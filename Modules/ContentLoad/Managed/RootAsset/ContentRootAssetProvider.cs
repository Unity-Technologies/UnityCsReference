// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Loading;
using System;
using UnityEngine.Bindings;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    /// <summary>
    /// Provides the RootAssets stored within ContentDirectories both at Runtime and in the editor
    /// </summary>
    [VisibleToOtherModules("UnityEditor.ContentLoadModule")]
    internal sealed partial class ContentRootAssetProvider : IRootAssetProvider
    {

        private List<Loadable<UnityEngine.Object>> m_RootLoadables = null;
        private Dictionary<string, Object> m_KeyToRootAssetDict = null;
        private Dictionary<Type, List<string>> m_TypeToRootAssetPathsDict = null;

        public ContentRootAssetProvider()
        {
            ContentLoadManager.OnRegisterContentDirectory += ClearRootAssetsCache;
            ContentLoadManager.OnUnregisterContentDirectory += ClearRootAssetsCache;
        }

        /// <summary>
        /// Retrieves all RootAssets from the ContentLoadManager and places them into the data structures. Also loads all loadables and places them in m_RootLoadables
        /// </summary>
        private void GatherRootAssetsAndLoadables()
        {
            // If we have already loaded the root assets and there are no updates, return
            // Note that this dict will get cleared whenever a new content directory is registered
            if (m_KeyToRootAssetDict != null && m_TypeToRootAssetPathsDict != null)
            {
                return;
            }

            var keyToRootAssetLoadables = ContentLoadManager.GetUniqueLoadablesWithKeys();

            // Paths should be valid even if the case is not the same
            m_KeyToRootAssetDict = new Dictionary<string, Object>();
            m_TypeToRootAssetPathsDict = new Dictionary<Type, List<string>>();
            m_RootLoadables = new List<Loadable<UnityEngine.Object>>();

            foreach (var (key, loadable) in keyToRootAssetLoadables)
            {
                m_RootLoadables.Add(loadable);

                var rootAsset = loadable.Load();
                if (rootAsset == null)
                {
                    Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null,
                        "RootAssetManager: Failed to load {0} + at path: {1}", loadable.ToString(), key);
                    continue;
                }

                var type = rootAsset.GetType();

                if (!m_TypeToRootAssetPathsDict.ContainsKey(type))
                {
                    m_TypeToRootAssetPathsDict[type] = new List<string>();
                }

                m_TypeToRootAssetPathsDict[type].Add(key);

                m_KeyToRootAssetDict[key] = rootAsset;
            }

            // TODO: Re-enable logging of existing root assets. See https://jira.unity3d.com/browse/CBD-785
            // var sb = new StringBuilder();
            // sb.AppendLine($"RootAssetManager: Loaded {m_KeyToRootAssetDict.Values.Count} root assets");
            // for (int i = 0; i < m_KeyToRootAssetDict.Values.Count; i++)
            // {
            //     sb.AppendLine($" - {m_KeyToRootAssetDict.Values[i].name}");
            // }
            //
            // Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, sb.ToString());
        }

        public T GetRootAssetByKey<T>(string path) where T : Object
        {
            GatherRootAssetsAndLoadables();
            return RootAssetProviderHelpers.GetRootAssetByKey<T>(m_KeyToRootAssetDict, path);
        }

        public T GetRootAssetByName<T>(string name) where T : Object
        {
            GatherRootAssetsAndLoadables();
            return RootAssetProviderHelpers.GetRootAssetByName<T>(m_KeyToRootAssetDict, name);
        }

        public void AppendUniqueRootAssetsOfType<T>(List<T> assets, HashSet<string> appendedPathsSet) where T : Object
        {
            GatherRootAssetsAndLoadables();
            List<T> typedAssets = RootAssetProviderHelpers.AppendAllRootAssetsOfType<T>(m_TypeToRootAssetPathsDict, m_KeyToRootAssetDict, appendedPathsSet);
            if (typedAssets != null && typedAssets.Count > 0)
                assets.AddRange(typedAssets);
        }

        public T GetRootAssetByType<T>() where T : Object
        {
            GatherRootAssetsAndLoadables();
            return RootAssetProviderHelpers.GetRootAssetByType<T>(m_TypeToRootAssetPathsDict, m_KeyToRootAssetDict);
        }

        public void ClearRootAssetsCache(ContentDirectoryHandle _) => ClearRootAssetsCache();
        public void ClearRootAssetsCache()
        {
            if (m_RootLoadables == null && m_KeyToRootAssetDict == null && m_TypeToRootAssetPathsDict == null)
                return;

            foreach (var rootLoadable in m_RootLoadables)
            {
                rootLoadable.Release();
            }

            m_RootLoadables = null;
            m_KeyToRootAssetDict = null;
            m_TypeToRootAssetPathsDict = null;
        }
    }
}
