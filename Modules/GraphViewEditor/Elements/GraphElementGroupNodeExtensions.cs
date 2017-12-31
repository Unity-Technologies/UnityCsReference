// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public static class GraphElementGroupNodeExtensions
    {
        public static GroupNode GetContainingGroupNode(this GraphElement element)
        {
            if (element == null)
                return null;

            GraphView graphView = element.GetFirstAncestorOfType<GraphView>();

            if (graphView == null)
                return null;

            List<GroupNode> groups = graphView.Query<GroupNode>().ToList();

            foreach (GroupNode group in groups)
            {
                if (group.ContainsElement(element))
                    return group;
            }
            return null;
        }
    }
}
