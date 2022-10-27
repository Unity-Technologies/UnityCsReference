// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A model that represents a subgraph node in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class SubgraphNodeModel : NodeModel
    {
        [SerializeReference]
        Subgraph m_Subgraph;

        /// <inheritdoc />
        public override string Title => m_Subgraph.Title;

        /// <summary>
        /// The graph referenced by the subgraph node.
        /// </summary>
        public GraphModel SubgraphModel
        {
            get => m_Subgraph?.GetGraphModel();

            set
            {
                if (value != null)
                {
                    m_Subgraph ??= new Subgraph(value.Asset);
                    DefineNode();
                }
                else
                {
                    m_Subgraph = null;
                }
            }
        }

        /// <summary>
        /// The guid of the graph referenced by the subgraph node.
        /// </summary>
        public string SubgraphGuid => m_Subgraph.AssetGuid;

        /// <summary>
        /// The data input port models on the subgraph node with their corresponding variable declaration models.
        /// </summary>
        public Dictionary<PortModel, VariableDeclarationModel> DataInputPortToVariableDeclarationDictionary { get; } = new Dictionary<PortModel, VariableDeclarationModel>();

        /// <summary>
        /// The data output port models on the subgraph node with their corresponding variable declaration models.
        /// </summary>
        public Dictionary<PortModel, VariableDeclarationModel> DataOutputPortToVariableDeclarationDictionary { get; } = new Dictionary<PortModel, VariableDeclarationModel>();

        /// <summary>
        /// The execution input port models on the subgraph node with their corresponding variable declaration models.
        /// </summary>
        public Dictionary<PortModel, VariableDeclarationModel> ExecutionInputPortToVariableDeclarationDictionary { get; } = new Dictionary<PortModel, VariableDeclarationModel>();

        /// <summary>
        /// The execution output port models on the subgraph node with their corresponding variable declaration models.
        /// </summary>
        public Dictionary<PortModel, VariableDeclarationModel> ExecutionOutputPortToVariableDeclarationDictionary { get; } = new Dictionary<PortModel, VariableDeclarationModel>();

        /// <summary>
        /// Updates the models of the subgraph node and its connected edges.
        /// </summary>
        /// <returns>A list of elements whose view needs to be updated.</returns>
        public List<GraphElementModel> Update()
        {
            // Get connected wires before the obsolete ones get removed in DefineNode.
            var wiresBeforeDefineNode = GetConnectedWires().ToList();

            DefineNode();

            var elementsToUpdate = new List<GraphElementModel> { this };

            foreach (var wireModel in wiresBeforeDefineNode.OfType<WireModel>())
            {
                wireModel.UpdatePortFromCache();
                wireModel.ResetPortCache();

                if (wireModel.ToPort == null || wireModel.FromPort == null)
                {
                    wireModel.AddMissingPorts(out _, out _);
                    elementsToUpdate.Add(wireModel);
                }
            }

            return elementsToUpdate;
        }

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            DataInputPortToVariableDeclarationDictionary.Clear();
            DataOutputPortToVariableDeclarationDictionary.Clear();
            ExecutionInputPortToVariableDeclarationDictionary.Clear();
            ExecutionOutputPortToVariableDeclarationDictionary.Clear();

            ProcessVariables();
        }

        /// <inheritdoc />
        protected override void DisconnectPort(PortModel portModel)
        {}

        void ProcessVariables()
        {
            if (SubgraphModel == null)
                return;

            foreach (var variableDeclaration in GetInputOutputVariables())
                AddPort(variableDeclaration, variableDeclaration.Guid.ToString(), variableDeclaration.Modifiers == ModifierFlags.Read, !variableDeclaration.IsInputOrOutputTrigger());
        }

        List<VariableDeclarationModel> GetInputOutputVariables()
        {
            var inputOutputVariableDeclarations = new List<VariableDeclarationModel>();

            // Get the input/output variable declarations from the section models to preserve their displayed order in the Blackboard
            foreach (var section in SubgraphModel.SectionModels)
                GetInputOutputVariable(section, ref inputOutputVariableDeclarations);

            return inputOutputVariableDeclarations;
        }

        void GetInputOutputVariable(IGroupItemModel groupItem, ref List<VariableDeclarationModel> inputOutputVariables)
        {
            if (groupItem is VariableDeclarationModel variable && variable.IsInputOrOutput())
            {
                inputOutputVariables.Add(variable);
            }
            else if (groupItem is GroupModel groupModel)
            {
                foreach (var item in groupModel.Items)
                    GetInputOutputVariable(item, ref inputOutputVariables);
            }
        }

        void AddPort(VariableDeclarationModel variableDeclaration, string portId, bool isInput, bool isData)
        {
            if (isInput)
            {
                if (isData)
                    DataInputPortToVariableDeclarationDictionary[this.AddDataInputPort(variableDeclaration.Title, variableDeclaration.DataType, portId)] = variableDeclaration;
                else
                    ExecutionInputPortToVariableDeclarationDictionary[this.AddExecutionInputPort(variableDeclaration.Title, portId)] = variableDeclaration;
            }
            else
            {
                if (isData)
                    DataOutputPortToVariableDeclarationDictionary[this.AddDataOutputPort(variableDeclaration.Title, variableDeclaration.DataType, portId, options: PortModelOptions.NoEmbeddedConstant)] = variableDeclaration;
                else
                    ExecutionOutputPortToVariableDeclarationDictionary[this.AddExecutionOutputPort(variableDeclaration.Title, portId)] = variableDeclaration;
            }
        }
    }
}
