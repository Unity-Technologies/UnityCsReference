// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A state model that references another graph (a regular graph or another state machine graph).
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class SubgraphStateModel : StateModel, IObjectClonedCallbackReceiver, ISubgraphNodeInternal
    {
        [SerializeField, HideInInspector]
        GraphReference m_SubgraphReference;

        [SerializeReference]
        GraphModel m_CopyPasteLocalSubgraphModelReference;

        [SerializeField, NodeOption(true)]
        new string m_Subtitle;

        // Used by the inspector to change the subgraph asset, when the node refers to an asset subgraph.
        [SerializeField, NodeOption(true)]
        SubgraphAssetProperty m_AssetProperty;

        public override string IconTypeString
        {
            get
            {
                var subgraph = GetSubgraphModel();
                if (subgraph == null || subgraph.IsLocalSubgraph)
                    return SubgraphNodeModelHelper.k_LocalSubgraphIconTypeString;

                return subgraph.IsStateMachineGraph ? "state" : SubgraphNodeModelHelper.k_AssetSubgraphIconTypeString;
            }
        }

        public override string Subtitle => m_Subtitle;

        public override string Title
        {
            get => SubgraphNodeModelHelper.GetTitle(GraphModel, m_Title, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference);
            set
            {
                m_Title = SubgraphNodeModelHelper.ComputeNewTitle(GraphModel, value, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference);
                Tooltip = m_Title;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        public GraphReference SubgraphReference => m_SubgraphReference;

        // A subgraph state can only be expanded when the referenced subgraph is also a state machine.
        // Expanding a regular graph into a state machine is not supported.
        public bool CanBeExpanded => GetSubgraphModel()?.IsStateMachineGraph == true;

        public GraphModel GetSubgraphModel() => SubgraphNodeModelHelper.GetSubgraphModel(GraphModel, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference);

        /// <inheritdoc />
        public bool IsReferencingLocalSubgraph => SubgraphNodeModelHelper.IsReferencingLocalSubgraph(GraphModel, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference);

        bool GraphModelReferenceIsValid => SubgraphNodeModelHelper.GraphModelReferenceIsValid(GraphModel, m_SubgraphReference);

        public void SetSubgraphModel(GraphReference value)
        {
            if (!SubgraphNodeModelHelper.SetSubgraphReference(GraphModel, value, ref m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference, out var assetProp, out var referenceIsValid, out var isReferencingLocal))
                return;

            m_AssetProperty = assetProp;

            if (referenceIsValid)
            {
                m_Title = null;
                m_Subtitle = isReferencingLocal ? SubgraphNodeModelHelper.DefaultLocalSubtitle : SubgraphNodeModelHelper.DefaultAssetSubtitle;
            }

            SetCapability(Editor.Capabilities.Renamable, referenceIsValid);
            DefineNode();
            GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
        }

        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            if (sourceNode is SubgraphStateModel sourceSubgraphState)
                SubgraphNodeModelHelper.HandleDuplicate(GraphModel, sourceSubgraphState.GraphModel, sourceSubgraphState.m_SubgraphReference, sourceSubgraphState.m_CopyPasteLocalSubgraphModelReference, sourceNode.Title, SetSubgraphModel);

            base.OnDuplicateNode(sourceNode);
        }

        public override void Rename(string name)
        {
            if (!IsRenamable())
                return;

            Title = name;
            GraphModel?.RenameSubgraphNode(this, name);
        }

        public void CloneAssets(List<Object> clones, Dictionary<Object, Object> originalToCloneMap)
            => SubgraphNodeModelHelper.CloneAssets(GraphModel, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference, clones, originalToCloneMap);

        public void OnAfterAssetClone(IReadOnlyDictionary<Object, Object> originalToCloneMap)
            => SubgraphNodeModelHelper.OnAfterAssetClone(GraphModel, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference, originalToCloneMap);

        public override void OnBeforeCopy()
        {
            base.OnBeforeCopy();
            SubgraphNodeModelHelper.BeginCopy(GraphModel, ref m_SubgraphReference, ref m_CopyPasteLocalSubgraphModelReference);
        }

        public override void OnAfterCopy()
        {
            base.OnAfterCopy();
            SubgraphNodeModelHelper.EndCopy(GraphModel, ref m_SubgraphReference, ref m_CopyPasteLocalSubgraphModelReference);
        }

        public override void OnAfterPaste()
        {
            base.OnAfterPaste();
            SubgraphNodeModelHelper.ClearPaste(ref m_CopyPasteLocalSubgraphModelReference);
        }

        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems => SubgraphNodeModelHelper.GetContextualMenuItems(GraphModel, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference, base.ContextualMenuItems);
    }
}
