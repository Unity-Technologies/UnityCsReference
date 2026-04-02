// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor.Implementation
{
    class BlackboardContentModelImp : BlackboardContentModel
    {
        public BlackboardContentModelImp(GraphTool graphTool) : base(graphTool)
        { }

        public override string GetTitle()
        {
            // If the graph label is different from the graph model name, it means the users has navigated from a subgraph node with a name that differs from the subgraph asset name.
            // In this case, we display the name of the subgraph node instead of the name of the subgraph asset to avoid confusion.
            var currentGraphLabel = base.GetTitle();
            if (currentGraphLabel != GraphModel.Name)
                return currentGraphLabel;

            return (GraphModel as GraphModelImp)?.Graph.Name ?? "Graph";
        }

        public override string GetSubTitle()
        {
            var subTitle = (GraphModel as GraphModelImp)?.Graph.GetType().Name;
            return subTitle != null ? $"({subTitle})" : String.Empty;
        }
    }
}
