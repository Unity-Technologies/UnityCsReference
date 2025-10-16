// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class GhostTransitionSupportModel : AbstractGhostTransitionSupportModel
    {
        /// <inheritdoc />
        public override PortModel FromPort { get; set; }

        /// <inheritdoc />
        public override string FromPortId => FromPort?.UniqueName;

        /// <inheritdoc />
        public override string ToPortId => ToPort?.UniqueName;

        /// <inheritdoc />
        public override Hash128 FromNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        /// <inheritdoc />
        public override Hash128 ToNodeGuid => ToPort?.NodeModel?.Guid ?? default;

        /// <inheritdoc />
        public override PortModel ToPort { get; set; }

        /// <inheritdoc />
        public override string WireBubbleText { get; set; }

        /// <inheritdoc />
        public override void SetPorts(PortModel toPortModel, PortModel fromPortModel)
        {
            FromPort = fromPortModel;
            ToPort = toPortModel;
        }

        /// <inheritdoc />
        public override ValueTuple<PortMigrationResult, PortMigrationResult> AddMissingPorts(out AbstractNodeModel inputNode, out AbstractNodeModel outputNode)
        {
            inputNode = null;
            outputNode = null;
            return (PortMigrationResult.None, PortMigrationResult.None);
        }
    }
}
