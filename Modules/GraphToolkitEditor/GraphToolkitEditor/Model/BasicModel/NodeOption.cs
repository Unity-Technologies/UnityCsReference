// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Represents a node option, one that appear in the Node Options section
    /// of the model inspector.
    /// </summary>
    [UnityRestricted]
    internal class NodeOption : INodeOption
    {
        /// <summary>
        /// The <see cref="Editor.PortModel"/> owned by the node option.
        /// </summary>
        public PortModel PortModel { get; }

        /// <summary>
        /// Whether the node option is only shown in the inspector. By default, it is shown in both the node and in the inspector.
        /// </summary>
        public bool IsInInspectorOnly { get; }

        /// <summary>
        /// The order in which the option will be displayed among the other options.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeOption"/> class.
        /// </summary>
        /// <param name="portModel">The port model of the node option.</param>
        /// <param name="showInInspectorOnly">Whether the node option is only shown in the inspector. By default, it is shown in both the node and in the inspector.</param>
        /// <param name="order">The order in which the option will be displayed among the other options.</param>
        public NodeOption(PortModel portModel, bool showInInspectorOnly, int order)
        {
            PortModel = portModel;
            IsInInspectorOnly = showInInspectorOnly;
            Order = order;
        }

        Type INodeOption.DataType => PortModel.PortDataType;

        string INodeOption.Name => PortModel.PortId;

        string INodeOption.DisplayName => PortModel.Title;

        bool INodeOption.TryGetValue<T>(out T value)
        {
            return PortModel.EmbeddedValue.TryGetValue(out value);
        }
    }
}
