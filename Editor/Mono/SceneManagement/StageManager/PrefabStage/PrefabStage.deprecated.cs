// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.SceneManagement;

namespace UnityEditor.Experimental.SceneManagement
{
    public partial class PrefabStage : Stage
    {
        // Not using API Updater for now since it doesn't support updating to a virtual property,
        // and this caused API Updader tests to fail.
        // (UnityUpgradable) -> assetPath
        [Obsolete("prefabAssetPath has been deprecated. Use assetPath instead.")]
        public string prefabAssetPath
        {
            get { return m_PrefabAssetPath; }
        }
    }
}
