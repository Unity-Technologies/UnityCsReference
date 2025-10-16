// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Processor that checks that at most one node refers to a writable data <see cref="VariableDeclarationModelBase"/>.
    /// </summary>
    class VariableNodeGraphProcessor : GraphProcessor
    {
        readonly GraphModel m_GraphModel;

        public VariableNodeGraphProcessor(GraphModel graphModel)
        {
            m_GraphModel = graphModel;
        }

        /// <inheritdoc />
        public override BaseGraphProcessingResult ProcessGraph(GraphChangeDescription changes)
        {
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
                    if (nodeModel is not SubgraphNodeModel subgraphNodeModel)
                        continue;

                    var subgraph = subgraphNodeModel.GetSubgraphModel();
                    if (subgraph == null)
                        continue;

                    if (uniqueGraphs.Add(subgraph))
                        graphsToCheck.Enqueue(subgraph);
                }
                CheckGraphErrors(graphToCheck, res);
            }

            return res;
        }

        static void CheckGraphErrors(GraphModel graphModel, ErrorsAndWarningsResult res)
        {
            for (var i = 0; i < graphModel.NodeModels.Count; i++)
            {
                var nodeModel = graphModel.NodeModels[i];
                if (nodeModel is VariableNodeModel variableNodeModel && ShouldAddError(variableNodeModel.VariableDeclarationModel, graphModel))
                    res.AddError("Only one instance of a data output is allowed in the graph.", variableNodeModel);
            }
        }

        static bool ShouldAddError(VariableDeclarationModelBase variable, GraphModel graphModel)
        {
            if (variable == null)
                return false;

            return graphModel.AllowMultipleDataOutputInstances == AllowMultipleDataOutputInstances.AllowWithWarning
                && variable.DataType != TypeHandle.ExecutionFlow
                && variable.Modifiers == ModifierFlags.Write
                && graphModel.FindReferencesInGraph<VariableNodeModel>(variable).Count > 1;
        }
    }
}
