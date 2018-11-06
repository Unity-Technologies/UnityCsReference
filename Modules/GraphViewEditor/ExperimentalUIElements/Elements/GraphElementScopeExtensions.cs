// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public static class GraphElementScopeExtensions
    {
        public static Scope GetContainingScope(this GraphElement element)
        {
            if (element == null)
                return null;

            GraphView graphView = element.GetFirstAncestorOfType<GraphView>();

            if (graphView == null)
                return null;

            return graphView.Query<Scope>().Where(scope => scope.containedElements.Contains(element)).First();
        }
    }
}
