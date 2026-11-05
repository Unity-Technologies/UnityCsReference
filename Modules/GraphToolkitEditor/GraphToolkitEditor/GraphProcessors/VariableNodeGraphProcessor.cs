// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Processor that checks that at most one node refers to a writable data <see cref="VariableDeclarationModelBase"/>.
    /// </summary>
    partial class VariableNodeGraphProcessor : GraphProcessor
    {
        readonly GraphModel m_GraphModel;
        readonly Dictionary<Hash128, ErrorsAndWarningsResult> m_PortErrorsByNode = new();
        bool m_PortErrorsInitialized;

        public VariableNodeGraphProcessor(GraphModel graphModel)
        {
            m_GraphModel = graphModel;
        }

        /// <inheritdoc />
        public override BaseGraphProcessingResult ProcessGraph(GraphChangeDescription changes)
        {
            UpdatePortErrors(changes);

            var res = new ErrorsAndWarningsResult();
            var uniqueGraphs = new HashSet<GraphModel>();
            var graphsToCheck = new Queue<GraphModel>();

            uniqueGraphs.Add(m_GraphModel);
            graphsToCheck.Enqueue(m_GraphModel);

            while (graphsToCheck.Count > 0)
            {
                var graphToCheck = graphsToCheck.Dequeue();
                for (var i = 0; i < graphToCheck.NodeModels.Count; i++)
                {
                    var nodeModel = graphToCheck.NodeModels[i];
                    CheckNodeForErrors(graphToCheck, nodeModel, res);

                    if (nodeModel is not SubgraphNodeModel subgraphNodeModel)
                        continue;

                    var subgraph = subgraphNodeModel.GetSubgraphModel();
                    if (subgraph == null)
                        continue;

                    if (uniqueGraphs.Add(subgraph))
                        graphsToCheck.Enqueue(subgraph);
                }
            }

            return res;
        }

        void CheckNodeForErrors(GraphModel graphModel, AbstractNodeModel abstractNodeModel, ErrorsAndWarningsResult res)
        {
            if (graphModel == m_GraphModel)
            {
                if (m_PortErrorsByNode.TryGetValue(abstractNodeModel.Guid, out var portErrors))
                    res.AddErrors(portErrors.Errors);
            }
            else if (abstractNodeModel is NodeModel nodeModel)
            {
                nodeModel.CheckNodeErrors(res);
            }

            if (abstractNodeModel is VariableNodeModel variableNodeModel && ShouldAddError(variableNodeModel.VariableDeclarationModel, graphModel))
                res.AddError("Only one instance of an output variable is allowed in the graph.", variableNodeModel);
        }

        static bool ShouldAddError(VariableDeclarationModelBase variable, GraphModel graphModel)
        {
            if (variable == null)
                return false;

            return graphModel.AllowMultipleDataOutputInstances == AllowMultipleDataOutputInstances.AllowWithWarning
                && variable.Modifiers == ModifierFlags.Write
                && graphModel.FindReferencesInGraph<VariableNodeModel>(variable).Count > 1;
        }
    }
}
