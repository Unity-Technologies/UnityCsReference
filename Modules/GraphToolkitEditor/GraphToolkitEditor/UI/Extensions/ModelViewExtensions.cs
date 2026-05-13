// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;

namespace Unity.GraphToolkit.Editor;

static class ModelViewExtensions
{
    static readonly Dictionary<Type, NodeAttribute> k_NodeAttributeCache = new();

    public static void ApplyCustomStylesheet(this ModelView element, AbstractNodeModel model)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (model is IUserNodeModelImp userNode)
        {
            if (userNode.Node == null)
                return;

            var nodeType = userNode.Node.GetType();
            if (!k_NodeAttributeCache.TryGetValue(nodeType, out var attribute))
            {
                attribute = nodeType.GetCustomAttribute<NodeAttribute>();
                k_NodeAttributeCache[nodeType] = attribute;
            }

            if (attribute != null && !string.IsNullOrEmpty(attribute.Stylesheet))
                    GraphElementHelper.AddStyleSheetPath(element, attribute.Stylesheet);
        }
    }
}
