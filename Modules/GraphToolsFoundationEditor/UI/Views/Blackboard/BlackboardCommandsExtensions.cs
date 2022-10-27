// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    static class BlackboardCommandsExtensions
    {
        public static void DispatchSelectUnusedVariables(this BlackboardView self)
        {
            List<GraphElementModel> selectables = new List<GraphElementModel>();

            foreach (var variable in self.BlackboardViewModel.ParentGraphView.GraphModel.VariableDeclarations)
            {
                if( ! variable.IsUsed())
                    selectables.Add(variable);
            }

            self.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, selectables));
        }
    }
}
