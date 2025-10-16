// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A source of variables that is a <see cref="GraphObject"/> stored in another asset file.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "", "", "GraphAssetVariableSource")]
    [UnityRestricted]
    internal class GraphObjectVariableSource : ExternalVariableSource, IObjectClonedCallbackReceiver
    {
        [Obsolete("Use m_GraphReference"), SerializeField]
        protected GraphAssetReference m_GraphAssetReference;

        [SerializeField]
        protected GraphReference m_GraphReference;

        /// <inheritdoc />
        public GUID GetAssetGUID() => m_GraphReference.AssetGuid;

        GraphObject GetGraphAsset() => GetGraphModel().GraphObject;

        // internal for tests
        internal GraphModel GetGraphModel()
        {
            return GraphReference.ResolveGraphModel(m_GraphReference);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphObjectVariableSource"/> class.
        /// </summary>
        /// <param name="graphReference">The reference to the graph object to use as the source.</param>
        protected GraphObjectVariableSource(GraphReference graphReference)
        {
            m_GraphReference = graphReference;

#pragma warning disable CS0618 // Type or member is obsolete
            m_GraphAssetReference = null;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphObjectVariableSource"/> class.
        /// </summary>
        /// <param name="graphObject">The graph to use as the source.</param>
        public GraphObjectVariableSource(GraphObject graphObject)
            : this(graphObject.GraphModel.GetGraphReference()) { }

        /// <inheritdoc />
        public override bool IsSame(ExternalVariableSource other)
        {
            return other is GraphObjectVariableSource otherGraphAssetVariableSource &&
                otherGraphAssetVariableSource.m_GraphReference.Equals(m_GraphReference);
        }

        /// <inheritdoc />
        public override void GetVariableDeclarations(List<VariableDeclarationModelBase> outList)
        {
            outList.Clear();

            if (GetGraphModel() == null)
                return;

            foreach (var variableDeclarationModel in GetGraphModel().VariableDeclarations)
            {
                if (variableDeclarationModel is ExternalVariableDeclarationModelBase)
                    continue;

                outList.Add(variableDeclarationModel);
            }
        }

        /// <inheritdoc />
        public override VariableDeclarationModelBase GetVariableDeclaration(Hash128 variableGuid)
        {
            if (GetGraphModel() != null &&
                GetGraphModel().TryGetModelFromGuid(variableGuid, out var model) &&
                model is VariableDeclarationModelBase declaration)
            {
                return declaration;
            }

            return null;
        }

        /// <inheritdoc />
        public override void SetDirty()
        {
            GetGraphAsset().Dirty = true;
        }

        /// <inheritdoc />
        public virtual void CloneAssets(List<Object> clones, Dictionary<Object, Object> originalToCloneMap)
        {
        }

        /// <inheritdoc />
        public virtual void OnAfterAssetClone(IReadOnlyDictionary<Object, Object> originalToCloneMap)
        {
            if (originalToCloneMap.TryGetValue(GetGraphAsset(), out var clonedSourceObject) && clonedSourceObject is GraphObject clonedSourceAsset)
            {
                m_GraphReference = clonedSourceAsset.GraphModel.GetGraphReference();
            }
        }

        /// <summary>
        /// Upgrades the graph reference to an <see cref="GraphReference"/>.
        /// </summary>
        public virtual void UpgradeGraphReference()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (m_GraphReference == default && m_GraphAssetReference != null)
            {
                m_GraphReference = m_GraphAssetReference.ConvertToGraphReference(null);
                m_GraphAssetReference = null;
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
