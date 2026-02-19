// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    static class NodeModificationExtensions
    {
        public static void SetNodeModelPosition(this NodeModel nodeModel, Vector2 position)
        {
            if (nodeModel == null)
                return;

            if (nodeModel is BlockNodeModel)
            {
                UnityEngine.Debug.LogWarning($"Cannot set the position of a BlockNode ({nodeModel.GetType().Name}). BlockNodes are automatically positioned by their Context.");
                return;
            }

            (nodeModel.GraphModel as GraphModelImp)?.CheckModificationLock();

            nodeModel.Position = position;
        }
    }
}
