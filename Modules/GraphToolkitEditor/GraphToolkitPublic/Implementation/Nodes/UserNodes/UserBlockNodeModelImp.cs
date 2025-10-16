// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    [Serializable]
    partial class UserBlockNodeModelImp : BlockNodeModel, IUserNodeModelImp
    {
        public override ContextNodeModel ContextNodeModel
        {
            get => base.ContextNodeModel;
            set
            {
                var previousContextNodeModel = base.ContextNodeModel;
                base.ContextNodeModel = value;

                var newContextNodeModel = base.ContextNodeModel;

                if (newContextNodeModel != previousContextNodeModel)
                {
                    if (previousContextNodeModel != null)
                    {
                        ((UserContextNodeModelImp)previousContextNodeModel).RemoveBlock(this);
                    }
                    if (newContextNodeModel != null)
                    {
                        ((UserContextNodeModelImp)newContextNodeModel).AddBlock(this);
                    }
                }
            }
        }
        public override bool IsCompatibleWith(ContextNodeModel context)
        {
            if (SpawnFlags.HasFlag(SpawnFlags.Orphan))
                return true;
            var blockNodeAttribute = Node.GetType().GetCustomAttribute<UseWithContextAttribute>();

            if (blockNodeAttribute == null)
                return false;
            if (!blockNodeAttribute.IsContextTypeSupported((context as IUserNodeModelImp)?.Node.GetType()))
                return false;

            return base.IsCompatibleWith(context);
        }
    }
}
