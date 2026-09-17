// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    interface IUserNodeModelImp : INode, IUserModelImp
    {
        public Node Node { get; }

        bool IUserModelImp.IsMissingDefinition => Node == null;

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

        void IUserModelImp.CallOnEnable()
        {
            Node?.OnEnable();
            OnEnableCalled = true;
        }

        void IUserModelImp.CallOnDisable()
        {
            OnEnableCalled = false;
            Node?.OnDisable();
        }

        void CallOnPortDataTypeChanged(PortModel portModel, TypeHandle previousType, TypeHandle newType)
        {
            if (Node == null)
                return;

            try
            {
                Node.CallOnPortDataTypeChanged(portModel, previousType.Resolve(), newType.Resolve());
            }
            catch (Exception e)
            {
                Debug.LogException(e, ((AbstractNodeModel)this).GraphModel?.GraphObject);
            }
        }
    }
}
