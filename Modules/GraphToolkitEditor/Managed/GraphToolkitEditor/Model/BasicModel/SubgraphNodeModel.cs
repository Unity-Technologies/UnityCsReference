// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A model that represents a subgraph node in a graph.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal partial class SubgraphNodeModel : NodeModel, IRenamable, IObjectClonedCallbackReceiver, ISubgraphNodeInternal
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

        readonly Color m_DefaultColorValue = new(107 / 255f, 204 / 255f, 134 / 255f, 1f);

        bool m_UpdateWasCalled;

        /// <summary>
        /// Whether this specific subgraph node model can be expanded into its parent graph, meaning all the nodes
        /// contained in the subgraph are moved to the parent graph.
        /// </summary>
        /// <remarks>
        /// If the parent graph is a state machine and the referenced subgraph is not, then the subgraph node cannot be expanded into the parent graph.
        /// </remarks>
        public virtual bool CanBeExpanded => !GraphModel.IsStateMachineGraph || GetSubgraphModel().IsStateMachineGraph;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override string Subtitle => m_Subtitle;

        /// <inheritdoc />
        public override string IconTypeString => SubgraphNodeModelHelper.IsReferencingLocalSubgraph(GraphModel, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference) ? SubgraphNodeModelHelper.k_LocalSubgraphIconTypeString : SubgraphNodeModelHelper.k_AssetSubgraphIconTypeString;

        /// <inheritdoc />
        public override bool UseColorAlpha => false;

        /// <inheritdoc />
        public override Color DefaultColor => m_DefaultColorValue;

        public GraphReference SubgraphReference => m_SubgraphReference;

        /// <summary>
        /// Gets the graph model referenced by the subgraph node.
        /// </summary>
        /// <returns>The graph model of the subgraph.</returns>
        public GraphModel GetSubgraphModel() => SubgraphNodeModelHelper.GetSubgraphModel(GraphModel, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference);

        public bool IsReferencingLocalSubgraph => SubgraphNodeModelHelper.IsReferencingLocalSubgraph(GraphModel, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference);

        /// <summary>
        /// Sets the graph referenced by the subgraph node.
        /// </summary>
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

        /// <summary>
        /// The input port models on the subgraph node with their corresponding variable declaration models.
        /// </summary>
        public Dictionary<PortModel, VariableDeclarationModelBase> InputPortToVariableDeclarationDictionary { get; } = new();

        /// <summary>
        /// The output port models on the subgraph node with their corresponding variable declaration models.
        /// </summary>
        public Dictionary<PortModel, VariableDeclarationModelBase> OutputPortToVariableDeclarationDictionary { get; } = new();

        bool GraphModelReferenceIsValid => SubgraphNodeModelHelper.GraphModelReferenceIsValid(GraphModel, m_SubgraphReference);

        /// <inheritdoc />
        public void Rename(string name)
        {
            if (!IsRenamable())
                return;

            Title = name;
            GraphModel?.RenameSubgraphNode(this, name);
        }

        /// <inheritdoc />
        public override void OnCreateNode()
        {
            base.OnCreateNode();
            SetCapability(Editor.Capabilities.Renamable, GraphModelReferenceIsValid);
        }

        /// <inheritdoc />
        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            if (sourceNode is SubgraphNodeModel sourceSubgraphNode)
                SubgraphNodeModelHelper.HandleDuplicate(GraphModel, sourceSubgraphNode.GraphModel, sourceSubgraphNode.m_SubgraphReference, sourceSubgraphNode.m_CopyPasteLocalSubgraphModelReference, sourceNode.Title, SetSubgraphModel);

            base.OnDuplicateNode(sourceNode);
        }

        /// <summary>
        /// Updates the models of the subgraph node and its connected edges.
        /// </summary>
        /// <returns>A list of elements whose view needs to be updated.</returns>
        public List<GraphElementModel> Update()
        {
            // Get connected wires before the obsolete ones get removed in DefineNode.
            #pragma warning disable UAC2001 // Avoid Linq
            var wiresBeforeDefineNode = GetConnectedWires().ToList();
#pragma warning restore UAC2001

            DefineNode();

            var elementsToUpdate = new List<GraphElementModel> { this };

            #pragma warning disable UAC2001 // Avoid Linq
            foreach (var wireModel in wiresBeforeDefineNode.OfType<WireModel>())
#pragma warning restore UAC2001
            {
                wireModel.UpdatePortFromCache();
                wireModel.ResetPortCache();

                if (wireModel.ToPort == null || wireModel.FromPort == null)
                {
                    wireModel.AddMissingPorts(out _, out _);
                    elementsToUpdate.Add(wireModel);
                }
            }

            GraphModel.CurrentGraphChangeDescription.AddChangedModels(elementsToUpdate, ChangeHint.Data);

            m_UpdateWasCalled = true;
            return elementsToUpdate;
        }

        /// <inheritdoc />
        protected override void OnDefineNode(NodeDefinitionScope scope)
        {
            SetCapability(Editor.Capabilities.Renamable, GraphModelReferenceIsValid);

            InputPortToVariableDeclarationDictionary.Clear();
            OutputPortToVariableDeclarationDictionary.Clear();

            ProcessVariables(scope);
        }

        /// <inheritdoc />
        protected override void DisconnectPort(PortModel portModel)
        {}

        void ProcessVariables(NodeDefinitionScope scope)
        {
            if (GetSubgraphModel() == null)
                return;

            foreach (var variableDeclaration in GetInputOutputVariables())
            {
                var portType = GetPortTypeForVariable(variableDeclaration);
                AddPort(variableDeclaration, variableDeclaration.Guid.ToString(), variableDeclaration.Modifiers == ModifierFlags.Read, portType, scope);
            }
        }

        /// <summary>
        /// Gets the port type associated with a variable declaration.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable declaration.</param>
        /// <returns>The port type.</returns>
        protected virtual PortType GetPortTypeForVariable(VariableDeclarationModelBase variableDeclarationModel)
        {
            return PortType.Default;
        }

        /// <summary>
        /// Gets a list of the <see cref="VariableDeclarationModelBase"/> inside the subgraph that are either input or output in the subgraph node, in the correct order.
        /// </summary>
        /// <returns>A list of the <see cref="VariableDeclarationModelBase"/> inside the subgraph that are either input or output in the subgraph node, in the correct order.</returns>
        List<VariableDeclarationModelBase> GetInputOutputVariables()
        {
            var inputOutputVariableDeclarations = new List<VariableDeclarationModelBase>();

            // Get the input/output variable declarations from the section models to preserve their displayed order in the Blackboard
            foreach (var section in GetSubgraphModel().SectionModels)
                GetInputOutputVariable(section, ref inputOutputVariableDeclarations);

            return inputOutputVariableDeclarations;
        }

        void GetInputOutputVariable(IGroupItemModel groupItem, ref List<VariableDeclarationModelBase> inputOutputVariables)
        {
            if (groupItem is VariableDeclarationModelBase variable && variable.IsInputOrOutput)
            {
                inputOutputVariables.Add(variable);
            }
            else if (groupItem is GroupModel groupModel)
            {
                foreach (var item in groupModel.Items)
                    GetInputOutputVariable(item, ref inputOutputVariables);
            }
        }

        void AddPort(VariableDeclarationModelBase variableDeclaration, string portId, bool isInput, PortType portType, NodeDefinitionScope scope)
        {
            PortModel portModel;
            if (isInput)
            {
                var options = variableDeclaration.ShowOnInspectorOnly ? PortModelOptions.Hidden : PortModelOptions.Default;
                portModel = scope.AddInputPort(variableDeclaration.Title, variableDeclaration.DataType, portType, portId, options: options, attributes: [new DelayedAttribute()],
                    initializationCallback: c =>
                    {
                        if (variableDeclaration.InitializationModel != null)
                            c.ObjectValue = variableDeclaration.InitializationModel.ObjectValue;
                    });
                InputPortToVariableDeclarationDictionary[portModel] = variableDeclaration;
            }
            else
            {
                portModel = scope.AddOutputPort(variableDeclaration.Title, variableDeclaration.DataType, portType, portId,
                    options: PortModelOptions.NoEmbeddedConstant, attributes: [new DelayedAttribute()]);
                OutputPortToVariableDeclarationDictionary[portModel] = variableDeclaration;
            }

            // The port tooltip should be the same as the variable if it has a custom tooltip. Else, it should be the default tooltip for ports.
            portModel.ToolTip = variableDeclaration.Tooltip == variableDeclaration.DefaultTooltip ? portModel.DefaultTooltip : variableDeclaration.Tooltip;
        }

        /// <summary>
        /// Upgrades graph references: asset subgraph name was empty by default. Now they have the node title by default.
        /// </summary>
        public void UpgradeGraphReference()
        {
            var subgraphModel = GetSubgraphModel();
            if (subgraphModel != null && string.IsNullOrEmpty(subgraphModel.Name))
                subgraphModel.Name = Title;
        }

        /// <inheritdoc />
        public void CloneAssets(List<Object> clones, Dictionary<Object, Object> originalToCloneMap)
            => SubgraphNodeModelHelper.CloneAssets(GraphModel, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference, clones, originalToCloneMap);

        /// <inheritdoc />
        public void OnAfterAssetClone(IReadOnlyDictionary<Object, Object> originalToCloneMap)
            => SubgraphNodeModelHelper.OnAfterAssetClone(GraphModel, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference, originalToCloneMap);

        /// <inheritdoc />
        public override void OnBeforeCopy()
        {
            base.OnBeforeCopy();
            SubgraphNodeModelHelper.BeginCopy(GraphModel, ref m_SubgraphReference, ref m_CopyPasteLocalSubgraphModelReference);
        }

        /// <inheritdoc />
        public override void OnAfterCopy()
        {
            base.OnAfterCopy();
            SubgraphNodeModelHelper.EndCopy(GraphModel, ref m_SubgraphReference, ref m_CopyPasteLocalSubgraphModelReference);
        }

        /// <inheritdoc />
        public override void OnAfterPaste()
        {
            base.OnAfterPaste();
            SubgraphNodeModelHelper.ClearPaste(ref m_CopyPasteLocalSubgraphModelReference);
        }

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems => SubgraphNodeModelHelper.GetContextualMenuItems(GraphModel, m_SubgraphReference, m_CopyPasteLocalSubgraphModelReference, base.ContextualMenuItems);

        public class TestAccess
        {
            public readonly SubgraphNodeModel m_SubgraphNodeModel;

            public TestAccess(SubgraphNodeModel subgraphNodeModel)
            {
                m_SubgraphNodeModel = subgraphNodeModel;
            }

            public bool UpdateWasCalled => m_SubgraphNodeModel.m_UpdateWasCalled;
            public Color DefaultColorValue => m_SubgraphNodeModel.m_DefaultColorValue;
        }
    }
}
