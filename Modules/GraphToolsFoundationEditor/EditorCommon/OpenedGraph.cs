// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Holds information about a graph displayed in the graph view editor window.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Editor")]
    struct OpenedGraph : ISerializationCallbackReceiver, IEquatable<OpenedGraph>
    {
        [SerializeField]
        string m_AssetGuid;

        [SerializeField]
        int m_GraphAssetObjectInstanceID;

        [SerializeField]
        long m_AssetLocalId;

        [SerializeField]
        GameObject m_BoundObject;

        GraphAsset m_GraphAsset;

        internal GraphAsset GetGraphAssetWithoutLoading_Internal() => m_GraphAsset;

        /// <summary>
        /// Gets the graph asset.
        /// </summary>
        /// <returns>The graph asset.</returns>
        public GraphAsset GetGraphAsset()
        {
            EnsureGraphAssetIsLoaded();
            return m_GraphAsset;
        }

        /// <summary>
        /// Gets the path of the graph asset file on disk.
        /// </summary>
        /// <returns>The path of the graph asset file.</returns>
        public string GetGraphAssetPath()
        {
            EnsureGraphAssetIsLoaded();
            return m_GraphAsset == null ? null : m_GraphAsset.FilePath;
        }

        /// <summary>
        /// The GUID of the graph asset.
        /// </summary>
        public string GraphAssetGuid => m_AssetGuid;

        /// <summary>
        /// The GameObject bound to this graph.
        /// </summary>
        public GameObject BoundObject => m_BoundObject;

        /// <summary>
        /// The file id of the graph asset in the asset file.
        /// </summary>
        public long AssetLocalId => m_AssetLocalId;

        /// <summary>
        /// Checks whether this instance holds a valid graph asset.
        /// </summary>
        /// <returns>True if the graph asset is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return GetGraphAsset() != null;
        }

        /// <summary>
        /// Gets the graph model stored in the asset file.
        /// </summary>
        /// <returns>The graph model.</returns>
        public GraphModel GetGraphModel()
        {
            EnsureGraphAssetIsLoaded();
            return m_GraphAsset == null ? null : m_GraphAsset.GraphModel;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenedGraph" /> class.
        /// </summary>
        /// <param name="graphModel">The graph model.</param>
        /// <param name="boundObject">The GameObject bound to the graph.</param>
        public OpenedGraph(GraphModel graphModel, GameObject boundObject)
        {
            var graphAsset = graphModel?.Asset;
            if (graphAsset == null ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(graphAsset, out m_AssetGuid, out m_AssetLocalId))
            {
                m_AssetGuid = "";
                m_AssetLocalId = 0L;
            }

            m_GraphAsset = graphAsset;
            m_GraphAssetObjectInstanceID = graphAsset == null ? 0 : graphAsset.GetInstanceID();
            m_BoundObject = boundObject;
        }

        void EnsureGraphAssetIsLoaded()
        {
            // GUIDToAssetPath cannot be done in ISerializationCallbackReceiver.OnAfterDeserialize so we do it here.

            if (!m_GraphAsset)
            {
                // Try to load object from its GUID. Will fail if it is a memory based asset or if the asset was deleted.
                if (!string.IsNullOrEmpty(m_AssetGuid))
                {
                    var graphAssetPath = AssetDatabase.GUIDToAssetPath(m_AssetGuid);
                    m_GraphAsset = Load_Internal(graphAssetPath, m_AssetLocalId);
                }

                // If it failed, try to retrieve object from its instance id (memory based asset).
                if (!m_GraphAsset && m_GraphAssetObjectInstanceID != 0)
                {
                    m_GraphAsset = EditorUtility.InstanceIDToObject(m_GraphAssetObjectInstanceID) as GraphAsset;
                }

                if (!m_GraphAsset)
                {
                    m_GraphAsset = null; // operator== above is overloaded for Unity.Object.
                    m_AssetGuid = null;
                    m_AssetLocalId = 0;
                    m_GraphAssetObjectInstanceID = 0;
                }
                else
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_GraphAsset, out m_AssetGuid, out m_AssetLocalId);
                    m_GraphAssetObjectInstanceID = m_GraphAsset.GetInstanceID();
                }
            }
        }

        /// <summary>
        /// Loads a graph asset from file.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="localFileId">The id of the asset in the file. If 0, the first asset of type <see cref="GraphAsset"/> will be loaded.</param>
        /// <returns>The loaded asset, or null if it was not found or if the asset found is not an <see cref="GraphAsset"/>.</returns>
        internal static GraphAsset Load_Internal(string path, long localFileId)
        {
            GraphAsset asset = null;

            if (localFileId != 0L)
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in assets)
                {
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out _, out long localId))
                        continue;

                    if (localId == localFileId)
                    {
                        return a as GraphAsset;
                    }
                }
            }
            else
            {
                asset = (GraphAsset)AssetDatabase.LoadAssetAtPath(path, typeof(GraphAsset));
            }

            return asset;
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            m_GraphAsset = null;
        }

        /// <inheritdoc />
        public bool Equals(OpenedGraph other)
        {
            return m_AssetGuid == other.m_AssetGuid &&
                m_GraphAssetObjectInstanceID == other.m_GraphAssetObjectInstanceID &&
                m_AssetLocalId == other.m_AssetLocalId &&
                m_BoundObject == other.m_BoundObject;
        }
    }
}
