// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A model that represents the placeholder of a context node.
    /// </summary>
    [Serializable]
    class ContextNodePlaceholder : ContextNodeModel, IPlaceholder
    {
        /// <inheritdoc />
        public long ReferenceId { get; set; }

        /// <inheritdoc />
        protected override void OnDefineNode(NodeDefinitionScope scope)
        {
            base.OnDefineNode(scope);
            PlaceholderModelHelper.SetPlaceholderCapabilities(this);
        }

        /// <inheritdoc />
        protected override void DisconnectPort(PortModel portModel)
        {
            // We do not want to disconnect ports that are unused, to create missing ports.
        }
    }
}
