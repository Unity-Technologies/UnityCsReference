// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A container for the <see cref="PortModel"/>s of a node.
    /// </summary>
    class OrderedPorts : IReadOnlyDictionary<string, PortModel>, IReadOnlyList<PortModel>
    {
        Dictionary<string, PortModel> m_Dictionary;
        List<PortModel> m_PortModels;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedPorts"/> class.
        /// </summary>
        /// <param name="capacity">Initial capacity of the container.</param>
        public OrderedPorts(int capacity = 0)
        {
            m_Dictionary = new Dictionary<string, PortModel>(capacity);
            m_PortModels = new List<PortModel>(capacity);
        }

        /// <summary>
        /// Adds a port at the end of the container.
        /// </summary>
        /// <param name="portModel">The port to add.</param>
        public void Add(PortModel portModel)
        {
            m_Dictionary.Add(portModel.UniqueName, portModel);
            m_PortModels.Add(portModel);

            var graphModel = portModel.NodeModel?.GraphModel;
            graphModel?.PortWireIndex.MarkDirty();
        }

        public void InsertRange(int index, IReadOnlyList<PortModel> portModels)
        {
            m_Dictionary.EnsureCapacity(m_Dictionary.Count + portModels.Count);
            m_PortModels.Capacity = m_PortModels.Count + portModels.Count;

            for (int i = 0; i < portModels.Count; i++)
            {
                m_Dictionary.Add(portModels[i].UniqueName, portModels[i]);
                m_PortModels.Insert(index + i, portModels[i]);
            }
        }

        /// <summary>
        /// Removes a port from the container.
        /// </summary>
        /// <param name="portModel">The port model to remove.</param>
        /// <returns>True if the port was removed. False otherwise.</returns>
        public bool Remove(PortModel portModel)
        {
            return Remove(portModel.UniqueName);
        }

        /// <summary>
        /// Change a port name while keeping the same order.
        /// </summary>
        /// <param name="oldPortName">The old name of the port. If not specified, it will be searched.</param>
        /// <param name="model">The port.</param>
        /// <returns>True if the port was found, false otherwise.</returns>
        public bool ChangePortName(PortModel model, string oldPortName = null)
        {
            if (oldPortName == null)
            {
                foreach (var kvp in m_Dictionary)
                {
                    if (kvp.Value == model)
                    {
                        oldPortName = kvp.Key;
                        break;
                    }
                }
            }
            if (m_Dictionary.Remove(oldPortName, out var _))
            {
                m_Dictionary.Add(model.UniqueName, model);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a port from the container using its unique name.
        /// </summary>
        /// <param name="uniqueName">The unique name of the port model to remove.</param>
        /// <returns>True if the port was removed. False otherwise.</returns>
        public bool Remove(string uniqueName)
        {
            bool found = false;
            if (m_Dictionary.TryGetValue(uniqueName, out var portModel))
            {
                m_Dictionary.Remove(uniqueName);
                found = true;
                m_PortModels.Remove(portModel);

                var graphModel = portModel.NodeModel?.GraphModel;
                graphModel?.PortWireIndex.MarkDirty();
            }

            return found;
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, PortModel>> GetEnumerator() => m_Dictionary.GetEnumerator();

        /// <summary>
        /// Gets an enumerator for the ports stored in the container.
        /// </summary>
        /// <returns>An enumerator for the ports stored in the container.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// The number of ports in the container.
        /// </summary>
        public int Count => m_Dictionary.Count;

        /// <summary>
        /// Checks whether the container contains a port with the name <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The name of the port.</param>
        /// <returns></returns>
        public bool ContainsKey(string key) => m_Dictionary.ContainsKey(key);

        /// <summary>
        /// Tries retrieving a port using its name.
        /// </summary>
        /// <param name="key">The name of the port.</param>
        /// <param name="value">The port found, if any.</param>
        /// <returns>True if a port matching the name was found.</returns>
        public bool TryGetValue(string key, out PortModel value)
        {
            return m_Dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets the port using its <see cref="PortModel.UniqueName"/>.
        /// </summary>
        /// <param name="key">The name of the port.</param>
        public PortModel this[string key] => m_Dictionary[key];

        /// <summary>
        /// The keys of the ports stored in the container, which are their <see cref="PortModel.UniqueName"/>.
        /// </summary>
        public IEnumerable<string> Keys => m_Dictionary.Keys;

        /// <summary>
        /// The ports stored in the container.
        /// </summary>
        public IEnumerable<PortModel> Values => m_PortModels;

        /// <summary>
        /// Gets an enumerator for the ports stored in the container.
        /// </summary>
        /// <returns>An enumerator for the ports stored in the container.</returns>
        IEnumerator<PortModel> IEnumerable<PortModel>.GetEnumerator()
        {
            return m_PortModels.GetEnumerator();
        }

        /// <summary>
        /// Gets the port at index.
        /// </summary>
        /// <param name="index">The index.</param>
        public PortModel this[int index] => m_PortModels[index];
    }
}
