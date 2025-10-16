// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Model for entry portals.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class WirePortalEntryModel : WirePortalModel, ISingleInputPortNodeModel
    {
        /// <inheritdoc />
        public PortModel InputPort { get; protected set; }

        /// <inheritdoc />
        public override bool CanHaveAnotherPortalWithSameDirectionAndDeclaration() => false;

        /// <inheritdoc />
        protected override void OnDefineNode(NodeDefinitionScope scope)
        {
            InputPort = scope.AddInputPort("", GetPortDataTypeHandle(), PortType);
        }
    }
}
