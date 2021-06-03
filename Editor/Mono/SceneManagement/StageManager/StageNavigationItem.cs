// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.SceneManagement
{
    [Serializable]
    internal class StageNavigationItem
    {
        internal static StageNavigationItem CreateMainStage() { return new StageNavigationItem(); }
        internal static StageNavigationItem CreatePrefabStage(string prefabAssetPath) { return new StageNavigationItem(prefabAssetPath); }

        string m_PrefabAssetPath;   // main state that are used when stage is in memory
        Texture2D m_PrefabIcon;     // we cache prefab icon so we can show the right icon for stages where the prefab has been deleted

        StageNavigationItem() {}

        StageNavigationItem(string prefabAssetPath)
        {
            SetPrefabAssetPath(prefabAssetPath);
        }

        public bool setSelectionAndScrollWhenBecomingCurrentStage { get; set; } = true;  // transient state since it is set every time we switch stage
        public bool isMainStage { get { return !isPrefabStage; } }
        public bool isPrefabStage { get { return !string.IsNullOrEmpty(m_PrefabAssetPath); } }
        public Texture2D prefabIcon { get { return m_PrefabIcon; } }

        public PrefabStage prefabStage
        {
            get
            {
                if (!isPrefabStage)
                    return null;

                return StageNavigationManager.instance.GetPrefabStage(prefabAssetPath);
            }
        }

        public string prefabAssetPath
        {
            get { return m_PrefabAssetPath; }
        }

        public bool prefabAssetExists
        {
            get { return File.Exists(m_PrefabAssetPath); }
        }

        public bool valid
        {
            get { return isMainStage || prefabAssetExists; }
        }

        public string displayName
        {
            get
            {
                if (isMainStage)
                    return "Scenes";
                return Path.GetFileNameWithoutExtension(prefabAssetPath);
            }
        }

        public void SetPrefabAssetPath(string prefabAssetPath)
        {
            if (string.IsNullOrEmpty(prefabAssetPath))
                throw new ArgumentNullException("prefabAssetPath");

            m_PrefabAssetPath = prefabAssetPath;
            if (string.IsNullOrEmpty(m_PrefabAssetPath))
                throw new ArgumentException("Prefab Asset not found when creating Stage.", prefabAssetPath);

            m_PrefabIcon = (Texture2D)AssetDatabase.GetCachedIcon(prefabAssetPath);
        }

        public void OnAssetsChangedOnHDD(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!isPrefabStage)
                return;

            for (int i = 0; i < movedFromAssetPaths.Length; ++i)
            {
                if (movedFromAssetPaths[i].Equals(m_PrefabAssetPath, StringComparison.OrdinalIgnoreCase))
                {
                    m_PrefabAssetPath = movedAssets[i]; // Prefab was moved (update cached path)
                    break;
                }
            }
        }

        public override string ToString()
        {
            return UnityString.Format("[Stage: {0}]", displayName);
        }
    }
}
