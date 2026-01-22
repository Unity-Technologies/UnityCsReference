// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    [Serializable]
    class SubgraphNodeModelImp : SubgraphNodeModel, ISubgraphNode
    {
        public Graph GetSubgraph()
        {
            var graphModel = GetSubgraphModel();

            return (graphModel as GraphModelImp)?.Graph;
        }

        protected override void OnDefineNode(NodeDefinitionScope definitionScope)
        {
            base.OnDefineNode(definitionScope);

            GetSubgraph().CallOnDefineSubgraphNodeOptions(definitionScope);
        }
    }
}
