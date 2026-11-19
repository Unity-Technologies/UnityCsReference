// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Holds information about a subgraph.
    /// </summary>
    [Obsolete("Use GraphReference instead."), Serializable]
    [UnityRestricted]
    internal class Subgraph : ISerializationCallbackReceiver
    {
        static readonly EntityId k_InstanceIDNone = EntityId.None;

        [SerializeField]
        string m_AssetGUID; // For serialization only. Otherwise use m_AssetGuid128.

        [SerializeField]
        long m_AssetLocalId;

        [SerializeField]
        EntityId m_GraphAssetObjectInstanceID;

        [SerializeField]
        string m_Title;

        [SerializeField]
        GraphObject m_GraphAsset;

        bool m_IsNativeAsset;
        bool m_IsAssetSubgraph;
        GUID m_AssetGuid128;

        public Subgraph(GraphObject graphAsset)
        {
            m_GraphAsset = graphAsset;
        }

        internal Subgraph(GUID assetGuid, long assetLocalId, EntityId instanceID)
        {
            m_AssetGuid128 = assetGuid;
            m_AssetLocalId = assetLocalId;
            m_GraphAssetObjectInstanceID = instanceID;
            m_GraphAsset = null;
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
        public GUID AssetGuid => m_AssetGuid128;

        /// <summary>
        /// The local id of the subgraph.
        /// </summary>
        public long AssetLocalId => m_AssetLocalId;

        public EntityId AssetInstanceId => m_GraphAssetObjectInstanceID;
        /// <summary>
        /// Gets the graph model of the subgraph.
        /// </summary>
        public GraphModel GetGraphModel()
        {
            EnsureGraphAssetIsLoaded();
            SetReferenceGraphAsset();
            return m_GraphAsset != null ? m_GraphAsset.GraphModel : null;
        }

        internal GraphModel GetGraphModelWithoutLoading() => m_GraphAsset != null ? m_GraphAsset.GraphModel : null;
        internal GraphObject GetGraphAssetWithoutLoading() => m_GraphAsset;

        void EnsureGraphAssetIsLoaded()
        {
            var asset = m_GraphAsset as Object;

            if (asset != null)
                return;

            var graphAssetPath = AssetDatabase.GUIDToAssetPath(m_AssetGuid128);
            if (graphAssetPath != null && m_AssetLocalId != 0)
            {
                if (TryLoad(graphAssetPath, m_AssetLocalId, m_AssetGuid128, out var graphAsset))
                {
                    m_GraphAsset = graphAsset;
                }
            }

            if (asset == null && m_GraphAssetObjectInstanceID != k_InstanceIDNone)
            {
                var graphAsset = EditorUtility.EntityIdToObject(m_GraphAssetObjectInstanceID) as GraphObject;
                m_GraphAsset = graphAsset;
            }
        }

        void SetReferenceGraphAsset()
        {
            if (m_GraphAsset is not Object asset || asset == null)
                return;

            m_IsNativeAsset = AssetDatabase.IsNativeAsset(asset);
            m_IsAssetSubgraph = !m_GraphAsset.GraphModel.IsLocalSubgraph;
            AssetDatabaseHelper.TryGetGUIDAndLocalFileIdentifier(asset, out m_AssetGuid128, out m_AssetLocalId);
            m_GraphAssetObjectInstanceID = asset.GetEntityId();
            m_AssetGUID = null;
        }

        static bool TryLoad(string path, long localFileId, GUID assetGuid, out GraphObject graphAsset)
        {
            graphAsset = null;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (!AssetDatabaseHelper.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long localId))
                    continue;

                // We want to load an asset with the same guid and localId
                if (assetGuid == guid && localId == localFileId)
                {
                    graphAsset = asset as GraphObject;
                    return graphAsset != null;
                }
            }

            return false;
        }

        /// <inheritdoc cref="ISerializationCallbackReceiver.OnBeforeSerialize()"/>
        public void OnBeforeSerialize()
        {
            m_AssetGUID = m_AssetGuid128.ToString();

            // Only save the object instance id for memory-based assets. This is needed for copy-paste operations.
            if (!m_AssetGuid128.Empty())
            {
                m_GraphAssetObjectInstanceID = k_InstanceIDNone;
            }
        }

        /// <inheritdoc cref="ISerializationCallbackReceiver.OnAfterDeserialize()"/>
        public void OnAfterDeserialize()
        {
            if (m_IsNativeAsset || m_IsAssetSubgraph)
                m_GraphAsset = null;

            m_AssetGuid128 = new GUID(m_AssetGUID);
            m_AssetGUID = null;
        }
    }
}
