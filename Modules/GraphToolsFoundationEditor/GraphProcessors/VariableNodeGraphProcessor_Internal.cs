// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Processor that checks that at most one node refers to a writable data <see cref="VariableDeclarationModel"/>.
    /// </summary>
    class VariableNodeGraphProcessor_Internal : GraphProcessor
    {
        readonly GraphModel m_GraphModel;

        public VariableNodeGraphProcessor_Internal(GraphModel graphModel)
        {
            m_GraphModel = graphModel;
        }

        /// <inheritdoc />
        public override BaseGraphProcessingResult ProcessGraph(GraphChangeDescription changes)
        {
            var res = new ErrorsAndWarningsResult();

            foreach (var variableNodeModel in m_GraphModel.NodeModels.OfType<VariableNodeModel>().Where(v => ShouldAddError(v.VariableDeclarationModel)))
            {
                res.AddError("Only one instance of a data output is allowed in the graph.", variableNodeModel);
            }

            return res;
        }

        bool ShouldAddError(VariableDeclarationModel variable)
        {
            return m_GraphModel.Stencil.AllowMultipleDataOutputInstances == AllowMultipleDataOutputInstances.AllowWithWarning
                && variable.Modifiers == ModifierFlags.Write
                && !variable.IsInputOrOutputTrigger()
                && m_GraphModel.FindReferencesInGraph<VariableNodeModel>(variable).Count() > 1 ;
        }
    }
}
