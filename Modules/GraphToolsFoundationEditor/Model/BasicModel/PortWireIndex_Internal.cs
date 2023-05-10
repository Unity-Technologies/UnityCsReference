// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    interface IPortWireIndexModel_Internal
    {
        public PortModel FromPort { get; }
        public PortModel ToPort { get; }
    }

    /// <summary>
    /// Implements an index to quickly retrieve the list of wires that are connected to a port.
    /// </summary>
    /// <remarks>
    /// The index needs to be kept up-to-date. In addition to adding and removing wires to it,
    /// it needs to be notified when any of the ports of a wire changes,
    /// by calling <see cref="WirePortsChanged"/>, or when <see cref="PortModel.UniqueName"/> changes, by calling
    /// <see cref="PortUniqueNameChanged"/>.
    /// </remarks>
    class PortWireIndex_Internal<TWire> where TWire : class, IPortWireIndexModel_Internal

    {
        static readonly IReadOnlyList<TWire> k_EmptyWireModelList = new List<TWire>();

        /// <summary>
        /// Used to send 1 wire to the list reordering method.
        /// </summary>
        static readonly List<TWire> k_OneWireList = new List<TWire>(1) { null };

        IReadOnlyList<TWire> m_WireModels;
        bool m_IsDirty;
        Dictionary<(Hash128 nodeGUID, string portUniqueName, PortDirection direction), List<TWire>> m_WiresByPort;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortWireIndex_Internal{TWire}"/> class to index <paramref name="wireModels"/>.
        /// </summary>
        public PortWireIndex_Internal(IReadOnlyList<TWire> wireModels)
        {
            m_WireModels = wireModels;
            m_WiresByPort = new Dictionary<(Hash128 nodeGUID, string portUniqueName, PortDirection direction), List<TWire>>();
            m_IsDirty = true;
        }

        /// <summary>
        /// Gets the list of wires that are connected to a port.
        /// </summary>
        /// <param name="portModel">The port for which we want the list of connected wires.</param>
        /// <returns>The list of wires connected to the port.</returns>
        public IReadOnlyList<TWire> GetWiresForPort(PortModel portModel)
        {
            if (portModel?.NodeModel == null)
                return k_EmptyWireModelList;

            return TryGetWiresForPort(portModel, out var list) ? list : k_EmptyWireModelList;
        }

        /// <summary>
        /// Gets the list of wires that are connected to a port.
        /// </summary>
        /// <param name="portModel">The port for which we want the list of connected wires.</param>
        /// <param name="wireList">The list of wires connected to the port.</param>
        /// <returns><c>true</c> if the list was found, <c>false</c> otherwise.</returns>
        bool TryGetWiresForPort(PortModel portModel, out List<TWire> wireList)
        {
            if (m_IsDirty)
                Reindex();

            var key = (portModel.NodeModel.Guid, portModel.UniqueName, portModel.Direction);
            return m_WiresByPort.TryGetValue(key, out wireList);
        }

        /// <summary>
        /// Marks the index as needing to be completely rebuilt.
        /// </summary>
        public void MarkDirty()
        {
            m_IsDirty = true;
        }

        /// <summary>
        /// Updates the index when a wire is added to the wire list.
        /// </summary>
        /// <param name="wireModel">The wire added.</param>
        public void WireAdded(TWire wireModel)
        {
            if (m_IsDirty || wireModel == null)
            {
                // Do not bother if index is already dirty: index will be rebuilt soon.
                return;
            }

            if (wireModel.FromPort != null)
            {
                var key = (wireModel.FromPort.NodeModel.Guid, wireModel.FromPort.UniqueName, wireModel.FromPort.Direction);
                AddKeyWire(key, wireModel);
            }

            if (wireModel.ToPort != null)
            {
                var key = (wireModel.ToPort.NodeModel.Guid, wireModel.ToPort.UniqueName, wireModel.ToPort.Direction);
                AddKeyWire(key, wireModel);
            }

            void AddKeyWire((Hash128, string, PortDirection) key, TWire wire)
            {
                if (!m_WiresByPort.TryGetValue(key, out var wireList))
                {
                    wireList = new List<TWire>();
                    m_WiresByPort[key] = wireList;
                }

                if (!wireList.Contains(wire))
                    wireList.Add(wire);
            }
        }

        /// <summary>
        /// Updates a wire in the index, when one of its port changes.
        /// </summary>
        /// <param name="wireModel">The wire to update.</param>
        /// <param name="oldPort">The previous port value.</param>
        /// <param name="newPort">The new port value.</param>
        public void WirePortsChanged(TWire wireModel, PortModel oldPort, PortModel newPort)
        {
            if (m_IsDirty || oldPort == newPort)
            {
                // Do not bother if index is already dirty: index will be rebuilt soon.
                return;
            }

            if (oldPort != null)
            {
                var key = (oldPort.NodeModel.Guid, oldPort.UniqueName, oldPort.Direction);
                if (m_WiresByPort.TryGetValue(key, out var wireList))
                {
                    wireList.Remove(wireModel);
                }
            }

            if (newPort != null)
            {
                var key = (newPort.NodeModel.Guid, newPort.UniqueName, newPort.Direction);
                if (!m_WiresByPort.TryGetValue(key, out var wireList))
                {
                    wireList = new List<TWire>();
                    m_WiresByPort[key] = wireList;
                }

                if (!wireList.Contains(wireModel))
                    wireList.Add(wireModel);
            }
        }

        /// <summary>
        /// Updates the index when the port unique name changes.
        /// </summary>
        /// <param name="portModel">The port model to update.</param>
        /// <param name="oldName">The old unique name of the port.</param>
        /// <param name="newName">The new unique name of the port.</param>
        public void PortUniqueNameChanged(PortModel portModel, string oldName, string newName)
        {
            if (m_IsDirty || oldName == newName || oldName == null || newName == null)
            {
                // Do not bother if index is already dirty: index will be rebuilt soon.
                return;
            }

            var key = (portModel.NodeModel.Guid, oldName, portModel.Direction);
            if (m_WiresByPort.TryGetValue(key, out var wireList))
            {
                m_WiresByPort.Remove(key);
            }

            var newKey = (portModel.NodeModel.Guid, newName, portModel.Direction);
            m_WiresByPort[newKey] = wireList;
        }

        /// <summary>
        /// Updates the index when the port direction changes.
        /// </summary>
        /// <param name="portModel">The port model to update.</param>
        /// <param name="oldDirection">The old direction of the port.</param>
        /// <param name="newDirection">The new direction of the port.</param>
        public void PortDirectionChanged(PortModel portModel, PortDirection oldDirection, PortDirection newDirection)
        {
            if (m_IsDirty || oldDirection == newDirection)
            {
                // Do not bother if index is already dirty: index will be rebuilt soon.
                return;
            }

            var key = (portModel.NodeModel.Guid, portModel.UniqueName, oldDirection);
            if (m_WiresByPort.TryGetValue(key, out var wireList))
            {
                m_WiresByPort.Remove(key);
            }

            var newKey = (portModel.NodeModel.Guid, portModel.UniqueName, newDirection);
            m_WiresByPort[newKey] = wireList;
        }

        /// <summary>
        /// Updates the index when a wire is removed from the wire list.
        /// </summary>
        /// <param name="wireModel">The wire to remove.</param>
        public void WireRemoved(TWire wireModel)
        {
            if (m_IsDirty || wireModel == null)
            {
                // Do not bother if index is already dirty: index will be rebuilt soon.
                return;
            }

            if (wireModel.FromPort != null)
            {
                var key = (wireModel.FromPort.NodeModel.Guid, wireModel.FromPort.UniqueName, wireModel.FromPort.Direction);
                RemoveKeyWire(key, wireModel);
            }

            if (wireModel.ToPort != null)
            {
                var key = (wireModel.ToPort.NodeModel.Guid, wireModel.ToPort.UniqueName, wireModel.ToPort.Direction);
                RemoveKeyWire(key, wireModel);
            }

            void RemoveKeyWire((Hash128, string, PortDirection) key, TWire wire)
            {
                if (m_WiresByPort.TryGetValue(key, out var wireList))
                {
                    wireList.Remove(wire);
                }

                if (wireList != null && wireList.Count == 0)
                {
                    m_WiresByPort.Remove(key);
                }
            }
        }

        void Reindex()
        {
            m_IsDirty = false;

            foreach (var pair in m_WiresByPort)
            {
                pair.Value.Clear();
            }

            foreach (var wireModel in m_WireModels)
            {
                WireAdded(wireModel);
            }

            List<(Hash128 nodeGUID, string portUniqueName, PortDirection direction)> toRemove = null;
            foreach (var pair in m_WiresByPort)
            {
                if (pair.Value.Count == 0)
                {
                    toRemove ??= new List<(Hash128 nodeGUID, string portUniqueName, PortDirection direction)>();
                    toRemove.Add(pair.Key);
                }
            }

            if (toRemove != null)
            {
                foreach (var key in toRemove)
                {
                    m_WiresByPort.Remove(key);
                }
            }
        }

        /// <summary>
        /// Updates the index when a wire is reordered.
        /// </summary>
        /// <param name="wireModel">The wire to move.</param>
        /// <param name="reorderType">The type of move to do.</param>
        public void WireReordered(TWire wireModel, ReorderType reorderType)
        {
            if (TryGetWiresForPort(wireModel.FromPort, out var list))
            {
                k_OneWireList[0] = wireModel;
                list.ReorderElements(k_OneWireList, reorderType);
            }
            else
            {
                throw new IndexOutOfRangeException($"{wireModel} not part of the {typeof(PortWireIndex_Internal<>).Name}.");
            }
        }
    }
}
