// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Port model for state nodes.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class StatePortModel : PortModel
    {
        /// <inheritdoc />
        public override Constant EmbeddedValue => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatePortModel"/> class.
        /// </summary>
        /// <param name="direction">The port direction.</param>
        /// <param name="node">The node hosting this port.</param>
        /// <param name="portId">The port id.</param>
        public StatePortModel(PortDirection direction, PortNodeModel node, string portId)
            : base(
                node,
                direction,
                PortOrientation.Horizontal,
                null,
                PortType.State,
                TypeHandle.Int,
                portId,
                PortModelOptions.NoEmbeddedConstant | PortModelOptions.Hidden,
                null,
                null
            )
        {
        }

        /// <summary>
        /// Computes the offset for a new transition.
        /// </summary>
        /// <returns>The offset for the transition.</returns>
        public float ComputeOffsetForNewSingleStateTransition()
        {
            const float transitionWidth = 30.0f;

            var maxOffset = 0.0f;
            foreach (var wire in GetConnectedWires())
            {
                if (wire is TransitionSupportModel transitionModel && transitionModel.TransitionSupportKind != TransitionSupportKind.StateToState)
                {
                    if (transitionModel.ToNodeAnchorSide == AnchorSide.Top && transitionModel.ToNodeAnchorOffset > maxOffset)
                    {
                        maxOffset = transitionModel.ToNodeAnchorOffset;
                    }
                }
            }

            maxOffset += transitionWidth;
            return maxOffset;
        }

        internal void UpdateAllOffsets()
        {
            const float transitionWidth = 30.0f;

            var maxOffset = transitionWidth;
            foreach (var wire in GetConnectedWires())
            {
                if (wire is TransitionSupportModel transitionModel && transitionModel.TransitionSupportKind != TransitionSupportKind.StateToState)
                {
                    if (transitionModel.ToNodeAnchorSide == AnchorSide.Top)
                    {
                        transitionModel.ToNodeAnchorOffset = maxOffset;
                    }
                    maxOffset += transitionWidth;
                }
            }
        }
    }
}
