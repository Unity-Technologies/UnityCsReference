// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for a model of a node that has port.
    /// </summary>
    [Serializable]
    abstract class PortNodeModel : AbstractNodeModel
    {
        /// <inheritdoc />
        public override IEnumerable<GraphElementModel> DependentModels => base.DependentModels.Concat(Ports);

        /// <summary>
        /// Gets all the port models this node has.
        /// </summary>
        public abstract IEnumerable<PortModel> Ports { get; }

        // PF: Add PortsById and PortsByDisplayOrder?

        // PF: these should probably be removed.
        /// <summary>
        /// Called when any port on this node model gets connected.
        /// </summary>
        /// <param name="selfConnectedPortModel">The model of the port that got connected on this node.</param>
        /// <param name="otherConnectedPortModel">The model of the port that got connected on the other node.</param>
        public virtual void OnConnection(PortModel selfConnectedPortModel, PortModel otherConnectedPortModel) { }

        /// <summary>
        /// Called when any port on this node model gets disconnected.
        /// </summary>
        /// <param name="selfConnectedPortModel">The model of the port that got disconnected on this node.</param>
        /// <param name="otherConnectedPortModel">The model of the port that got disconnected on the other node.</param>
        public virtual void OnDisconnection(PortModel selfConnectedPortModel, PortModel otherConnectedPortModel) { }

        /// <summary>
        /// Gets the model of a port that would be fit to connect to another port model.
        /// </summary>
        /// <param name="portModel">The model of the port we want to connect to this node.</param>
        /// <returns>A model of a port that would be fit to connect, null if none was found.</returns>
        public abstract PortModel GetPortFitToConnectTo(PortModel portModel);

        /// <summary>
        /// Remove a missing port that no longer has any connection.
        /// </summary>
        /// <returns>True if the missing port was removed, False otherwise.</returns>
        public abstract bool RemoveUnusedMissingPort(PortModel portModel);

        /// <inheritdoc />
        public override IEnumerable<WireModel> GetConnectedWires()
        {
            if (GraphModel != null)
                return Ports.SelectMany(p => GraphModel.GetWiresForPort(p));

            return Enumerable.Empty<WireModel>();
        }
    }
}
