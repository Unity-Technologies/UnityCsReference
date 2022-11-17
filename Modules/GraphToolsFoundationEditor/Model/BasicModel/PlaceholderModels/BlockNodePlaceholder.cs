// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A model that represents the placeholder of a block node.
    /// </summary>
    [Serializable]
    class BlockNodePlaceholder : BlockNodeModel, IPlaceholder
    {
        /// <inheritdoc />
        public long ReferenceId { get; set; }

        /// <inheritdoc />
        protected override void DisconnectPort(PortModel portModel)
        {
            // We do not want to disconnect ports that are unused, to create missing ports.
        }

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            this.ClearCapabilities();
            this.SetCapability(Editor.Capabilities.Deletable, true);
            this.SetCapability(Editor.Capabilities.Selectable, true);
        }
    }
}
