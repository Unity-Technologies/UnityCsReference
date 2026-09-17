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
        protected List<NodeOption> m_NodeOptions = new List<NodeOption>();
        protected Dictionary<string, NodeOption> m_NodeOptionsByName = new Dictionary<string, NodeOption>();

        internal bool IsPortBeingReused { get; set; }

        /// <inheritdoc />
        #pragma warning disable UAC2001 // Avoid Linq
        public override IEnumerable<GraphElementModel> DependentModels => base.DependentModels.Concat(GetPorts());
#pragma warning restore UAC2001

        /// <summary>
        /// The list of <see cref="NodeOption"/>.
        /// </summary>
        /// <remarks>The options in this list are created without the use of the <see cref="NodeOptionAttribute"/>.</remarks>
        public IReadOnlyList<NodeOption> NodeOptions => m_NodeOptions;

        /// <summary>
        /// The list of <see cref="NodeOption"/> indexed by a string unique to the option.
        /// </summary>
        /// <remarks>The options in this dictionary are created without the use of the <see cref="NodeOptionAttribute"/>.</remarks>
        public IReadOnlyDictionary<string, NodeOption> NodeOptionsByName => m_NodeOptionsByName;

        /// <summary>
        /// The constants backing this node's input ports and node options, indexed by <see cref="PortModel.UniqueName"/>.
        /// </summary>
        /// <remarks>Null when the model stores no embedded constant. Implementations must return a stored field: this
        /// property is on the hot path of <see cref="PortModel.EmbeddedValue"/>.</remarks>
        internal virtual IDictionary<string, Constant> ConstantsByPortName => null;

        /// <summary>
        /// Instantiates the ports and the node options of this node.
        /// </summary>
        /// <remarks>Does nothing by default.</remarks>
        public virtual void DefineNode() { }

        /// <summary>
        /// Adds a node option to this node.
        /// </summary>
        /// <param name="nodeOption">The node option to add.</param>
        /// <returns>The added node option.</returns>
        internal NodeOption AddNodeOption(NodeOption nodeOption)
        {
            m_NodeOptions.Add(nodeOption);
            m_NodeOptionsByName[nodeOption.Id] = nodeOption;
            return m_NodeOptions[^1];
        }

        /// <summary>
        /// Removes every node option from this node.
        /// </summary>
        internal void ClearNodeOptions()
        {
            m_NodeOptions.Clear();
            m_NodeOptionsByName.Clear();
        }

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
            #pragma warning disable UAC2001 // Avoid Linq
            return GetPorts().Where(p => (p.Direction & direction) == direction && p.PortType == portType);
#pragma warning restore UAC2001
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
        /// <remarks>Does nothing when the node stores no embedded constant, that is when <see cref="ConstantsByPortName"/> is null.</remarks>
        protected internal virtual void UpdateConstantForInput(PortModel inputPort, Action<Constant> initializationCallback = null, Action<object> setterAction = null)
        {
            var constantsByPortName = ConstantsByPortName;
            if (constantsByPortName == null)
                return;

            var id = inputPort.UniqueName;
            if ((inputPort.Options & PortModelOptions.NoEmbeddedConstant) != 0)
            {
                constantsByPortName.Remove(id);
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Unspecified);
                return;
            }

            Constant newConstant = null;
            if (constantsByPortName.TryGetValue(id, out var existingConstant))
            {
                newConstant = GraphModel?.CreateConstantValue(inputPort.DataTypeHandle);
                var portDefinitionType = newConstant != null ? newConstant.Type : inputPort.DataTypeHandle.Resolve();

                if (!existingConstant.IsAssignableFrom(portDefinitionType))
                {
                    // Destroy incompatible constant
                    constantsByPortName.Remove(id);
                    GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Unspecified);
                }
                else
                {
                    // Reuse compatible constant.
                    existingConstant.OwnerModel = inputPort;
                    existingConstant.SetterMethod = setterAction;
                    return;
                }
            }

            // Create new constant if needed
            if (inputPort.CreateEmbeddedValueIfNeeded
                && inputPort.DataTypeHandle != TypeHandle.Unknown)
            {
                newConstant ??= GraphModel?.CreateConstantValue(inputPort.DataTypeHandle);
                if (newConstant != null)
                {
                    newConstant.OwnerModel = inputPort;
                    initializationCallback?.Invoke(newConstant);
                    newConstant.SetterMethod = setterAction;
                    constantsByPortName[id] = newConstant;
                    GraphModel.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Unspecified);
                }
            }
        }

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
                #pragma warning disable UAC2001 // Avoid Linq
                return GetPorts().SelectMany(p => GraphModel.GetWiresForPort(p)).Distinct();
#pragma warning restore UAC2001

            return Array.Empty<WireModel>();
        }

        internal virtual void OnPortDataTypeChanged(PortModel portModel, TypeHandle previousType, TypeHandle dataTypeHandle) { }
    }
}
