// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;

namespace Unity.GraphToolsFoundation.Editor
{
    static class StencilHelper
    {
        /// <summary>
        /// Returns true if the given node is one of the common nodes of the BasicModel that should authorize copy/paste.
        /// </summary>
        /// <param name="nodeModel">The node model to be tested.</param>
        /// <returns>True if the given node is one of the common nodes of the BasicModel that should be copy/pasted.</returns>
        public static bool IsCommonNodeThatCanBePasted(AbstractNodeModel nodeModel)
        {
            return nodeModel is ConstantNodeModel || nodeModel is VariableNodeModel || nodeModel is ContextNodeModel || nodeModel is WirePortalModel;
        }
    }
}
