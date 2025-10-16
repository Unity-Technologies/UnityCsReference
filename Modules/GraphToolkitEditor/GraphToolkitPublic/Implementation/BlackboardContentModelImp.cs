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
            return (GraphModel as GraphModelImp)?.Graph.Name ?? "Graph";
        }

        public override string GetSubTitle()
        {
            var subTitle = (GraphModel as GraphModelImp)?.Graph.GetType().Name;
            return subTitle != null ? $"({subTitle})" : String.Empty;
        }
    }
}
