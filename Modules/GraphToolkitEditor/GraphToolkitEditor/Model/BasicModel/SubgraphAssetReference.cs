// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Holds information about a subgraph asset.
    /// </summary>
    [Obsolete("Use GraphReference instead."), Serializable]
    [UnityRestricted]
    internal class SubgraphAssetReference : GraphAssetReference
    {
        [SerializeField]
        GraphObject m_SerializedGraphAsset;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubgraphAssetReference"/> class.
        /// </summary>
        /// <param name="graphAsset">The graph asset of the subgraph.</param>
        internal SubgraphAssetReference(GraphObject graphAsset) : base(graphAsset)
        {
            m_SerializedGraphAsset = graphAsset;
        }

        /// <inheritdoc />
        public override GraphReference ConvertToGraphReference(GraphModel parentGraphModel)
        {
            if (m_SerializedGraphAsset != null)
            {
                return ConvertGraphAssetToGraphReference(m_SerializedGraphAsset, parentGraphModel);
            }

            return base.ConvertToGraphReference(parentGraphModel);
        }
    }
}
