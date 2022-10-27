// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Model for execution entry portals.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class ExecutionWirePortalEntryModel : WirePortalModel, ISingleInputPortNodeModel
    {
        /// <inheritdoc />
        public PortModel InputPort { get; private set; }

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            InputPort = this.AddExecutionInputPort("");
        }
    }
}
