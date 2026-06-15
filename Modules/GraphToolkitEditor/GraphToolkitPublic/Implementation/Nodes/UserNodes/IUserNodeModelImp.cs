// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    interface IUserNodeModelImp : INode
    {
        public Node Node { get; }

        bool OnEnableCalled { get; set; }

        void CustomOnDefineNode(NodeModel.NodeDefinitionScope definitionScope)
        {
            if (Node == null)
                return;

            try
            {
                Node.CallOnDefineOptions(definitionScope);
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
            OnEnableCalled = true;
        }

        void CallOnDisable()
        {
            OnEnableCalled = false;
            Node?.OnDisable();
        }
    }
}
