// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for a model of a node that has port.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract class PortNodeModel : AbstractNodeModel
    {
        /// <inheritdoc />
        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public override IEnumerable<GraphElementModel> DependentModels => base.DependentModels.Concat(GetPorts());
#pragma warning restore UA2001

        /// <summary>
        /// Retrieves all port models of this node.
        /// </summary>
        /// <returns>The port models.</returns>
        public abstract IReadOnlyList<PortModel> GetPorts();

        /// <summary>
        /// Retrieves the ports of a node that satisfy the requested direction and type.
        /// </summary>
        /// <param name="direction">The direction of the ports to retrieve.</param>
        /// <param name="portType">The type of the ports to retrieve.</param>
        /// <returns>The input ports of the node that satisfy the requested direction and type.</returns>
        public IEnumerable<PortModel> GetPorts(PortDirection direction, PortType portType)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return GetPorts().Where(p => (p.Direction & direction) == direction && p.PortType == portType);
#pragma warning restore UA2001
        }

        /// <summary>
        /// Called when any port on this node model gets connected.
        /// </summary>
        /// <param name="selfConnectedPortModel">The model of the port that got connected on this node.</param>
        /// <param name="otherConnectedPortModel">The model of the port that got connected on the other node.</param>
        public virtual void OnConnection(PortModel selfConnectedPortModel, PortModel otherConnectedPortModel)
        {
            selfConnectedPortModel.OnConnection(otherConnectedPortModel);
        }

        /// <summary>
        /// Called when any port on this node model gets disconnected.
        /// </summary>
        /// <param name="selfConnectedPortModel">The model of the port that got disconnected on this node.</param>
        /// <param name="otherConnectedPortModel">The model of the port that got disconnected on the other node.</param>
        public virtual void OnDisconnection(PortModel selfConnectedPortModel, PortModel otherConnectedPortModel)
        {
            selfConnectedPortModel.OnDisconnection(otherConnectedPortModel);
        }

        /// <summary>
        /// Called when the unique name of any port on this node model has changed.
        /// </summary>
        /// <param name="portModel">The port model.</param>
        /// <param name="oldUniqueName">The old unique name of the port.</param>
        /// <param name="newUniqueName">The new unique name of the port.</param>
        public virtual void OnPortUniqueNameChanged(PortModel portModel, string oldUniqueName, string newUniqueName) { }

        /// <summary>
        /// Updates an input port's constant.
        /// </summary>
        /// <param name="inputPort">The port to update.</param>
        /// <param name="initializationCallback">An initialization method for the constant, called right after the constant is created.</param>
        /// <param name="setterAction">The method called after the constant value changes.</param>
        protected internal virtual void UpdateConstantForInput(PortModel inputPort, Action<Constant> initializationCallback = null, Action<object> setterAction = null) { }

        /// <summary>
        /// Gets the model of a port that would be fit to connect to another port model.
        /// </summary>
        /// <param name="portModel">The model of the port we want to connect to this node.</param>
        /// <returns>A model of a port that would be fit to connect, null if none was found.</returns>
        public abstract PortModel GetPortFitToConnectTo(PortModel portModel);

        /// <summary>
        /// Removes a missing port when it is no longer used.
        /// </summary>
        /// <param name="portModel">The port to remove.</param>
        /// <returns>True if the missing port was removed, False otherwise.</returns>
        public abstract bool RemoveUnusedMissingPort(PortModel portModel);

        /// <inheritdoc />
        public override IEnumerable<WireModel> GetConnectedWires()
        {
            if (GraphModel != null)
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return GetPorts().SelectMany(p => GraphModel.GetWiresForPort(p)).Distinct();
#pragma warning restore UA2001

            return Array.Empty<WireModel>();
        }

        internal virtual void OnPortDataTypeChanged(PortModel portModel, TypeHandle previousType, TypeHandle dataTypeHandle) { }
    }
}
