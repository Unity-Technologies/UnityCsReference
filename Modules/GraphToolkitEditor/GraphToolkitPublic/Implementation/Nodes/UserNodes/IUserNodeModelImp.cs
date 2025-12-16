// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{

    interface IUserNodeModelImp : INode
    {
        public Node Node { get; }

        Dictionary<string, INodeOption> NodeOptionsByName { get; }
        IReadOnlyList<NodeOption> NodeOptions { get; }

        INodeOption GetNodeOptionByName(string name) => NodeOptionsByName.GetValueOrDefault(name);
        void CustomOnDefineNode(NodeModel.NodeDefinitionScope definitionScope)
        {
            if (Node == null)
                return;
            NodeOptionsByName.Clear();

            try
            {
                Node.CallOnDefineOptions(definitionScope);
                foreach (var nodeOption in NodeOptions)
                {
                    NodeOptionsByName[nodeOption.Id] = nodeOption;
                }

                Node.CallOnDefineNode(definitionScope);
            }
            catch (Exception e)
            {
                Debug.LogException(e, ((AbstractNodeModel)this).GraphModel?.GraphObject);
            }
        }

        void CallOnEnable()
        {
            Node?.OnEnable();
        }

        void CallOnDisable()
        {
            Node?.OnDisable();
        }
    }
}
