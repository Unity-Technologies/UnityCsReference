// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A reference to a <see cref="GraphObject"/>.
    /// </summary>
    [Serializable]
    [Obsolete("GraphAssetReference is deprecated. Use GraphReference instead.")]
    [UnityRestricted]
    internal class GraphAssetReference : ISerializationCallbackReceiver, IEquatable<GraphAssetReference>
    {
        protected static readonly EntityId k_InstanceIDNone = EntityId.None;

        [SerializeField]
        protected string m_AssetGUID; // For serialization only. Otherwise use m_AssetGuid128.

        [SerializeField]
        protected long m_AssetLocalId;

        [SerializeField]
        protected GameObject m_BoundObject;

        [SerializeField]
        protected string m_Title;

        [SerializeField]
        protected EntityId m_GraphAssetObjectInstanceID;

        protected GraphObject m_GraphAsset;
        protected GUID m_AssetGuid128;

        /// <summary>
        /// The graph asset associated with the graph.
        /// </summary>
        protected virtual GraphObject GraphAsset
        {
            get => m_GraphAsset;
            private set => m_GraphAsset = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphAssetReference"/> class.
        /// </summary>
        /// <param name="graphAsset">The graph asset this object is referring to.</param>
        protected GraphAssetReference(GraphObject graphAsset)
        {
            m_GraphAsset = graphAsset;
        }

        /// <summary>
        /// Loads the graph asset if it is not already loaded.
        /// </summary>
        protected virtual void EnsureGraphAssetIsLoaded()
        {
            // GUIDToAssetPath cannot be done in ISerializationCallbackReceiver.OnAfterDeserialize so we do it here.
            if (GraphAsset)
                return;

            // Try to load object from its GUID. Will fail if it is a memory based asset or if the asset was deleted.
            var graphAssetPath = AssetDatabase.GUIDToAssetPath(m_AssetGuid128);
            if (graphAssetPath != null)
            {
                GraphAsset = Load(graphAssetPath, m_AssetLocalId, m_AssetGuid128);
            }

            // If it failed, try to retrieve object from its instance id (memory based asset).
            if (!GraphAsset && m_GraphAssetObjectInstanceID != k_InstanceIDNone)
            {
                GraphAsset = EditorUtility.EntityIdToObject(m_GraphAssetObjectInstanceID) as GraphObject;
            }

            if (!GraphAsset)
            {
                GraphAsset = null; // operator== above is overloaded for Unity.Object.
            }
            else
            {
                AssetDatabaseHelper.TryGetGUIDAndLocalFileIdentifier(GraphAsset, out m_AssetGuid128, out m_AssetLocalId);
            }
        }

        /// <summary>
        /// Loads a graph asset from file.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="localFileId">The id of the asset in the file. If 0, the first asset of type <see cref="GraphAsset"/> will be loaded.</param>
        /// <param name="assetGuid"></param>
        /// <returns>The loaded asset, or null if it was not found or if the asset found is not an <see cref="GraphAsset"/>.</returns>
        internal static GraphObject Load(string path, long localFileId, GUID assetGuid)
        {
            if (localFileId == 0L)
                return (GraphObject)AssetDatabase.LoadAssetAtPath(path, typeof(GraphObject));

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (!AssetDatabaseHelper.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out var localId))
                    continue;

                // We want to load an asset with the same guid and localId
                if (assetGuid == guid && localId == localFileId)
                    return asset as GraphObject;
            }

            return null;
        }

        /// <inheritdoc />
        public virtual void OnBeforeSerialize()
        {
        }

        /// <inheritdoc />
        public virtual void OnAfterDeserialize()
        {
            GraphAsset = null;
            m_AssetGuid128 = new GUID(m_AssetGUID);
            m_AssetGUID = null;
        }


        protected static GraphReference ConvertGraphAssetToGraphReference(GraphObject subGraphAsset, GraphModel parentGraphModel)
        {
            if (subGraphAsset == null || subGraphAsset.GraphModel == null)
                return default;
            if (subGraphAsset.AssetFileGuid == parentGraphModel?.GraphObject?.AssetFileGuid) //This is a local subgraph
            {
                var subGraphModel = subGraphAsset.GraphModel;
                subGraphAsset.IsLocalSubgraphMigrated = true;
                parentGraphModel.AddLocalSubgraph(subGraphModel);

                return subGraphModel.GetGraphReference(true);
            }

            return subGraphAsset.GraphModel.GetGraphReference();
        }

        /// <summary>
        /// Migrates sub graphs saved as sub assets to sub graphs stored in the graph model.
        /// Creates a new <see cref="GraphReference"/> to replace this.
        /// </summary>
        public virtual GraphReference ConvertToGraphReference(GraphModel parentGraphModel)
        {
            if (m_AssetGuid128 == default)
                return default;

            EnsureGraphAssetIsLoaded();
            if (GraphAsset != null)
            {
                return ConvertGraphAssetToGraphReference(GraphAsset, parentGraphModel);
            }

            Debug.LogError($"Could not upgrade graph reference to asset guid {m_AssetGuid128} with local id {m_AssetLocalId}");
            return default;
        }

        public bool Equals(GraphAssetReference other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return m_AssetGUID == other.m_AssetGUID && m_AssetLocalId == other.m_AssetLocalId && Equals(m_BoundObject, other.m_BoundObject) && m_Title == other.m_Title && m_GraphAssetObjectInstanceID == other.m_GraphAssetObjectInstanceID && m_AssetGuid128.Equals(other.m_AssetGuid128);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((GraphAssetReference)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(m_AssetGUID, m_AssetLocalId, m_BoundObject, m_Title, m_GraphAssetObjectInstanceID, m_AssetGuid128);
        }
    }
}
