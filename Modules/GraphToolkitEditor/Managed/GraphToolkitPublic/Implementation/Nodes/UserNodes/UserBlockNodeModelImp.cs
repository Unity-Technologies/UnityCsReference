// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Reflection;

namespace Unity.GraphToolkit.Editor.Implementation
{
    partial class UserBlockNodeModelImp
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
            if (Node == null || PlaceholderModelHelper.IsMissingTypeModel(context))
                return false;
            var blockNodeAttribute = Node.GetType().GetCustomAttribute<UseWithContextAttribute>();

            if (blockNodeAttribute == null)
                return false;
            if (!blockNodeAttribute.IsContextTypeSupported((context as IUserNodeModelImp)?.Node.GetType()))
                return false;

            return base.IsCompatibleWith(context);
        }
    }
}
