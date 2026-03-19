// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A model that represents a subgraph node in a graph.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class SubgraphNodeModel : NodeModel, IRenamable, IObjectClonedCallbackReceiver
    {
        [Obsolete("Use m_SubgraphReference instead.")]
        [SerializeReference, HideInInspector]
        Subgraph m_Subgraph;

        [Obsolete("Use m_SubgraphReference instead.")]
        [SerializeReference, HideInInspector]
        SubgraphAssetReference m_SubgraphAssetReference;

        [SerializeField, HideInInspector]
        GraphReference m_SubgraphReference;

        [SerializeField, NodeOption(true)]
        new string m_Subtitle;

        // Used by the inspector to change the subgraph asset, when the node refers to an asset subgraph.
        [SerializeField, NodeOption(true)]
        SubgraphAssetProperty m_AssetProperty;

        // This is used to serialize the referenced local subgraph model in copy/paste operations.
        // This should always be null, except during copy/paste operations.
        [SerializeReference]
        GraphModel m_CopyPasteLocalSubgraphModelReference;

        readonly Color m_DefaultColorValue = new(107 / 255f, 204 / 255f, 134 / 255f, 1f);

        bool m_UpdateWasCalled;

        /// <summary>
        /// The default subtitle when the subgraph is a local graph.
        /// </summary>
        public virtual string DefaultLocalSubtitle => "Local Subgraph";

        /// <summary>
        /// The default subtitle when the subgraph is a separate asset.
        /// </summary>
        public virtual string DefaultAssetSubtitle => "Asset Subgraph";

        /// <summary>
        /// Whether this specific subgraph node model can be expanded into its parent graph, meaning all the nodes
        /// contained in the subgraph are moved to the parent graph.
        /// </summary>
        public virtual bool CanBeExpanded => true;

        /// <inheritdoc />
        public override string Title
        {
            get => string.IsNullOrEmpty(m_Title) ? GetSubgraphModel()?.Name ?? string.Empty : m_Title;
            set
            {
                var newName = value?.Trim();


                if (string.IsNullOrEmpty(newName))
                    newName = string.Empty;
                else
                {
                    var subGraphModel = GetSubgraphModel();
                    if (subGraphModel != null && subGraphModel.Name == newName)
                    {
                        newName = string.Empty;
                    }
                }

                m_Title = newName;
                Tooltip = newName;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public override string Subtitle => m_Subtitle;

        /// <summary>
        /// The icon type string for subgraph nodes referencing a local subgraph.
        /// </summary>
        public static readonly string k_LocalSubgraphIconTypeString = "subgraph";

        /// <summary>
        /// The icon type string for subgraph nodes referencing an asset subgraph.
        /// </summary>
        public static readonly string k_AssetSubgraphIconTypeString = "graph-object";

        /// <inheritdoc />
        public override string IconTypeString => IsReferencingLocalSubgraph ? k_LocalSubgraphIconTypeString : k_AssetSubgraphIconTypeString;

        /// <inheritdoc />
        public override bool UseColorAlpha => false;

        /// <inheritdoc />
        public override Color DefaultColor => m_DefaultColorValue;

        public GraphReference SubgraphReference => m_SubgraphReference;

        /// <summary>
        /// Gets the graph model referenced by the subgraph node.
        /// </summary>
        /// <returns>The graph model of the subgraph.</returns>
        public GraphModel GetSubgraphModel() => GraphModel?.ResolveGraphModelFromReference(m_SubgraphReference) ?? m_CopyPasteLocalSubgraphModelReference;

        public bool IsReferencingLocalSubgraph => GetSubgraphModel()?.IsLocalSubgraph ?? false;

        /// <summary>
        /// Sets the graph referenced by the subgraph node.
        /// </summary>
        public void SetSubgraphModel(GraphReference value)
        {
            if (m_SubgraphReference.Equals(value))
                return;

            if (! m_SubgraphReference.HasAssetReference)
            {
                var previousLocalSubGraph = GetSubgraphModel();
                if (previousLocalSubGraph != null && previousLocalSubGraph.IsLocalSubgraph)
                {
                    GraphModel.RemoveLocalSubgraph(previousLocalSubGraph);
                }
            }

            m_SubgraphReference = value;
            m_AssetProperty = new SubgraphAssetProperty(m_SubgraphReference);

            if (GraphModelReferenceIsValid)
            {
                m_Title = null;
                m_Subtitle = IsReferencingLocalSubgraph ? DefaultLocalSubtitle : DefaultAssetSubtitle;
            }

            SetCapability(Editor.Capabilities.Renamable, GraphModelReferenceIsValid);
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

        bool GraphModelReferenceIsValid => GraphModel.ResolveGraphModelFromReference(m_SubgraphReference) != null;

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
            base.OnDuplicateNode(sourceNode);

            if (sourceNode is not SubgraphNodeModel sourceSubgraphNode)
                return;

            if (!sourceSubgraphNode.IsReferencingLocalSubgraph && sourceSubgraphNode.m_CopyPasteLocalSubgraphModelReference == null)
                return;

            var sourceGraphModel = sourceSubgraphNode.GetSubgraphModel();
            if (sourceGraphModel is null)
            {
                SetSubgraphModel(default);
                return;
            }

            // Each duplicated local subgraph node should have their own instance of graph model
            var newSubgraph = GraphModel.DuplicateLocalSubGraph(sourceGraphModel, sourceNode.Title);
            SetSubgraphModel(newSubgraph.GetGraphReference(true));
        }

        /// <summary>
        /// Updates the models of the subgraph node and its connected edges.
        /// </summary>
        /// <returns>A list of elements whose view needs to be updated.</returns>
        public List<GraphElementModel> Update()
        {
            // Get connected wires before the obsolete ones get removed in DefineNode.
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var wiresBeforeDefineNode = GetConnectedWires().ToList();
#pragma warning restore UA2001

            DefineNode();

            var elementsToUpdate = new List<GraphElementModel> { this };

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var wireModel in wiresBeforeDefineNode.OfType<WireModel>())
#pragma warning restore UA2001
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
                portModel = scope.AddInputPort(variableDeclaration.Title, variableDeclaration.DataType, portType, portId, options: options,
                    initializationCallback: c =>
                    {
                        if (variableDeclaration.InitializationModel != null)
                            c.ObjectValue = variableDeclaration.InitializationModel.ObjectValue;
                    });
                InputPortToVariableDeclarationDictionary[portModel] = variableDeclaration;
            }
            else
            {
                portModel = scope.AddOutputPort(variableDeclaration.Title, variableDeclaration.DataType, portType, portId, options: PortModelOptions.NoEmbeddedConstant);
                OutputPortToVariableDeclarationDictionary[portModel] = variableDeclaration;
            }

            // The port tooltip should be the same as the variable if it has a custom tooltip. Else, it should be the default tooltip for ports.
            portModel.ToolTip = variableDeclaration.Tooltip == variableDeclaration.DefaultTooltip ? portModel.DefaultTooltip : variableDeclaration.Tooltip;
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

#pragma warning disable CS0618
            if (m_Subgraph != null)
            {
                var graphObject = m_Subgraph.GetGraphAssetWithoutLoading();
                m_SubgraphAssetReference = new SubgraphAssetReference(graphObject);
                m_Subgraph = null;
            }
#pragma warning restore CS0618
        }

        /// <summary>
        /// Migrates sub graphs saved as sub assets to subgraphs stored in the graph model.
        /// Upgrades graph references from <see cref="GraphAssetReference"/> and <see cref="SubgraphAssetReference"/>
        /// to <see cref="GraphReference"/>.
        /// </summary>
        public void UpgradeGraphReference()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (m_SubgraphReference == default && m_SubgraphAssetReference != null)
            {
                m_SubgraphReference = m_SubgraphAssetReference.ConvertToGraphReference(GraphModel);
                m_SubgraphAssetReference = null;
                m_AssetProperty = new SubgraphAssetProperty(m_SubgraphReference);

                GetSubgraphModel()?.UpgradeGraphReferences();
            }
#pragma warning restore CS0618 // Type or member is obsolete

            // Asset subgraph name was empty by default. Now they have the node title by default.
            var subgraphModel = GetSubgraphModel();
            if (subgraphModel != null && string.IsNullOrEmpty(subgraphModel.Name))
                subgraphModel.Name = Title;

        }

        /// <inheritdoc />
        public void CloneAssets(List<Object> clones, Dictionary<Object, Object> originalToCloneMap)
        {
            if (IsReferencingLocalSubgraph)
            {
                GetSubgraphModel().CloneAssets(clones, originalToCloneMap);
            }
        }

        /// <inheritdoc />
        public void OnAfterAssetClone(IReadOnlyDictionary<Object, Object> originalToCloneMap)
        {
            if (IsReferencingLocalSubgraph)
            {
                // m_SubgraphReference has just been updated: GetSubgraphModel() will return the graph model in the cloned asset.
                GetSubgraphModel().OnAfterAssetClone(originalToCloneMap);
            }
        }

        /// <inheritdoc />
        public override void OnBeforeCopy()
        {
            base.OnBeforeCopy();

            // Local subgraphs need to be duplicated by the copy/paste operation.
            if (IsReferencingLocalSubgraph)
            {
                m_CopyPasteLocalSubgraphModelReference = GetSubgraphModel();
                if (m_CopyPasteLocalSubgraphModelReference is ICopyPasteCallbackReceiver copyPasteCallbackReceiver)
                    copyPasteCallbackReceiver.OnBeforeCopy();
                m_SubgraphReference = default;
            }
            else
            {
                m_CopyPasteLocalSubgraphModelReference = null;
            }
        }

        public override void OnAfterCopy()
        {
            base.OnAfterCopy();

            if (m_CopyPasteLocalSubgraphModelReference != null)
            {
                m_SubgraphReference = GraphModel.GetGraphModelReference(m_CopyPasteLocalSubgraphModelReference, true);
                // Set the reference back to null, as we do not want to serialize the local subgraph model to disk.
                m_CopyPasteLocalSubgraphModelReference = null;
            }
        }

        /// <inheritdoc />
        public override void OnAfterPaste()
        {
            base.OnAfterPaste();

            m_CopyPasteLocalSubgraphModelReference = null;
        }

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems
        {
            get
            {
                var menuItems = new List<ContextualMenuItem>(base.ContextualMenuItems);
                menuItems.AddRange(IsReferencingLocalSubgraph ? s_LocalSubgraphContextualMenuItems : s_AssetSubgraphContextualMenuItems);
                return menuItems;
            }
        }

        static List<ContextualMenuItem> s_LocalSubgraphContextualMenuItems = new() {
            ContextualMenuHelpers.extractContentsToPlacematItem,
            ContextualMenuHelpers.openLocalSubgraphItem,
            ContextualMenuHelpers.convertToAssetSubgraphItem,
        };

        static List<ContextualMenuItem> s_AssetSubgraphContextualMenuItems = new() {
            ContextualMenuHelpers.extractContentsToPlacematItem,
            ContextualMenuHelpers.openAssetSubgraphItem,
            ContextualMenuHelpers.unpackToLocalSubgraphItem,
            ContextualMenuHelpers.findAssetInProjectItem,
        };

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
