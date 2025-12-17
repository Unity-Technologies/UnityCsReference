// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor.Implementation
{
    class GraphToolImp : GraphTool
    {
        public override void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            base.Dispatch(command, diagnosticsFlags);

            if (command is LoadGraphCommand) // Update the tool name when a graph is loaded. For now the tool name is the graph type name.
            {
                Name = (ToolState.GraphModel as GraphModelImp)?.Graph?.GetType()?.Name ?? "Unknown Tool";
            }
        }
    }
}
