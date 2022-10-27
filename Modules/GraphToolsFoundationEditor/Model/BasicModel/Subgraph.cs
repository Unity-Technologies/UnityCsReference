// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Holds information about a subgraph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class Subgraph : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_AssetGUID;

        [SerializeField]
        long m_AssetLocalId;

        [SerializeField]
        int m_GraphAssetObjectInstanceID;

        [SerializeField]
        string m_Title;

        GraphAsset m_GraphAsset;

        public Subgraph(GraphAsset graphAsset)
        {
            m_GraphAsset = graphAsset;
            SetReferenceGraphAsset();
        }

        /// <summary>
        /// The title of the subgraph.
        /// </summary>
        public string Title
        {
            get
            {
                var graphModel = GetGraphModel();
                if (graphModel != null)
                {
                    m_Title = graphModel.Name;
                    return m_Title;
                }
                return "! MISSING ! " + m_Title;
            }
        }

        /// <summary>
        /// The guid of the subgraph.
        /// </summary>
        public string AssetGuid => m_AssetGUID;

        /// <summary>
        /// Gets the graph model of the subgraph.
        /// </summary>
        public GraphModel GetGraphModel()
        {
            EnsureGraphAssetIsLoaded();
            SetReferenceGraphAsset();
            return m_GraphAsset != null ? m_GraphAsset.GraphModel : null;
        }

        void EnsureGraphAssetIsLoaded()
        {
            var asset = m_GraphAsset as Object;

            if (asset != null)
                return;

            if (!string.IsNullOrEmpty(m_AssetGUID) && m_AssetLocalId != 0)
            {
                var graphAssetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
                if (TryLoad(graphAssetPath, m_AssetLocalId, m_AssetGUID, out var graphAsset))
                {
                    m_GraphAsset = graphAsset;
                }
            }

            if (asset == null && m_GraphAssetObjectInstanceID != 0)
            {
                var graphAsset = EditorUtility.InstanceIDToObject(m_GraphAssetObjectInstanceID) as GraphAsset;
                m_GraphAsset = graphAsset;
            }
        }

        void SetReferenceGraphAsset()
        {
            var asset = m_GraphAsset as Object;
            if (asset != null)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out m_AssetGUID, out m_AssetLocalId);
                m_GraphAssetObjectInstanceID = asset.GetInstanceID();
            }
        }

        static bool TryLoad(string path, long localFileId, string assetGuid, out GraphAsset graphAsset)
        {
            graphAsset = null;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long localId))
                    continue;

                // We want to load an asset with the same guid and localId
                if (assetGuid == guid && localId == localFileId)
                {
                    graphAsset = asset as GraphAsset;
                    return graphAsset != null;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
            // Only save the object instance id for memory-based assets. This is needed for copy-paste operations.
            if (m_AssetGUID.Any(c => c != '0'))
            {
                m_GraphAssetObjectInstanceID = 0;
            }
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            m_GraphAsset = null;
        }
    }
}
