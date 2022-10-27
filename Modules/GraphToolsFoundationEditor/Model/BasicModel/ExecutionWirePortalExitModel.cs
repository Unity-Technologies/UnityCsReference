// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Model for execution exit portals.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class ExecutionWirePortalExitModel : WirePortalModel, ISingleOutputPortNodeModel
    {
        /// <inheritdoc />
        public PortModel OutputPort => OutputsById.Values.FirstOrDefault();

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            this.AddExecutionOutputPort("");
        }
    }
}
