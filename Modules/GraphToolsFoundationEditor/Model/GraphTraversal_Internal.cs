// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    abstract class GraphTraversal_Internal
    {
        public void VisitGraph(GraphModel graphModel)
        {
            if (graphModel?.Stencil == null)
                return;

            var visitedNodes = new HashSet<AbstractNodeModel>();
            foreach (var entryPoint in graphModel.Stencil.GetEntryPoints())
            {
                VisitNode(entryPoint, visitedNodes);
            }

            // floating nodes
            foreach (var node in graphModel.NodeModels)
            {
                if (node == null || visitedNodes.Contains(node))
                    continue;

                VisitNode(node, visitedNodes);
            }

            foreach (var variableDeclaration in graphModel.VariableDeclarations)
            {
                VisitVariableDeclaration(variableDeclaration);
            }

            foreach (var wireModel in graphModel.WireModels)
            {
                VisitWire(wireModel);
            }
        }

        protected virtual void VisitWire(WireModel wireModel)
        {
        }

        protected virtual void VisitNode(AbstractNodeModel nodeModel, HashSet<AbstractNodeModel> visitedNodes)
        {
            if (nodeModel == null)
                return;

            visitedNodes.Add(nodeModel);

            if (nodeModel is InputOutputPortsNodeModel portHolder)
            {
                foreach (var inputPortModel in portHolder.InputsById.Values)
                {
                    if (inputPortModel.IsConnected())
                        foreach (var connectionPortModel in inputPortModel.GetConnectedPorts())
                        {
                            if (!visitedNodes.Contains(connectionPortModel.NodeModel))
                                VisitNode(connectionPortModel.NodeModel, visitedNodes);
                        }
                }
            }
        }

        protected virtual void VisitVariableDeclaration(VariableDeclarationModel variableDeclarationModel) {}
    }
}
