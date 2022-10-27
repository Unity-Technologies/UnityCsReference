// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    class VariableNodeGraphProcessor_Internal : GraphProcessor
    {
        /// <inheritdoc />
        public override GraphProcessingResult ProcessGraph(GraphModel graphModel, GraphChangeDescription changes)
        {
            var res = new GraphProcessingResult();

            foreach (var variableNodeModel in graphModel.NodeModels.OfType<VariableNodeModel>().Where(v => ShouldAddError(v.VariableDeclarationModel, graphModel)))
            {
                res.AddError("Only one instance of a data output is allowed in the graph.", variableNodeModel);
            }

            return res;
        }

        static bool ShouldAddError(VariableDeclarationModel variable, GraphModel graphModel)
        {
            return graphModel.Stencil.AllowMultipleDataOutputInstances == AllowMultipleDataOutputInstances.AllowWithWarning
                && variable.Modifiers == ModifierFlags.Write
                && !variable.IsInputOrOutputTrigger()
                && graphModel.FindReferencesInGraph<VariableNodeModel>(variable).Count() > 1 ;
        }
    }
}
