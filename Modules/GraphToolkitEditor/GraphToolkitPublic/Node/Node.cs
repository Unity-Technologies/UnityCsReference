// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The base class for all user-accessible nodes in a graph.
    /// </summary>
    /// <remarks>
    /// Inherit from this class to define custom node types that appear in the graph. The <see cref="Node"/> class provides
    /// lifecycle hooks, serialization support, and the structure needed to define ports, UI behaviors, and custom logic.
    /// This class forms the foundation of all user-defined nodes in a graph-based tool, including variable nodes, context nodes,
    /// and subgraph nodes.
    /// <br/>
    /// <br/>
    /// To create a custom node, derive from <see cref="Node"/>, define its input and output ports using a port builder in <see cref="OnDefinePorts"/>,
    /// and define its <see cref="INodeOption"/>s in <see cref="OnDefineOptions"/>.
    /// <br/>
    /// <br/>
    /// This class is used in combination with other types like <see cref="INode"/>, <see cref="IPort"/>, and <see cref="Graph"/>
    /// to construct and manage node-based workflows.
    /// <br/>
    /// <br/>
    /// See also:
    /// <list type="bullet">
    /// <item><description><see cref="INode"/> for the interface this class implements</description></item>
    /// <item><description><see cref="ContextNode"/> and <see cref="BlockNode"/> for composition patterns</description></item>
    /// <item><description><see cref="IVariableNode"/> for how to work with variable-based nodes</description></item>
    /// <item><description><see cref="ISubgraphNode"/> for how to work with subgraph-based nodes</description></item>
    /// </list>
    /// </remarks>
    [Serializable]
    public abstract partial class Node : INode
    {
        /// <summary>
        /// Interface that provides methods to declare node options inside a node.
        /// </summary>
        /// <remarks>
        /// Use to add node options on nodes. Node options appear under the node header and in the inspector when a node is selected. They are appropriate for parameters that affect how a node behaves or changes its topology,
        /// such as modifying the number of ports.
        /// </remarks>
        public interface IOptionDefinitionContext
        {
            /// <summary>
            /// Adds a new node option.
            /// </summary>
            /// <param name="name">The unique identifier of the option.</param>
            /// <param name="dataType">The data type of the option.</param>
            /// <returns>An <see cref="IOptionBuilder"/> to further configure the option.</returns>
            /// <remarks>
            /// <c>name</c> is used to identify the option. It must be unique among ports and options on the node. This name is used as the ID when calling <see cref="GetNodeOptionByName(string)"/>.
            /// If <see cref="IOptionBuilder.WithDisplayName(string)"/> is not used, this name is also used as the option's display label.
            /// </remarks>
            /// <example>
            /// <code>
            /// protected override void OnDefineOptions(IOptionDefinitionContext context)
            /// {
            ///     context.AddOption("MyOption", typeof(int)))
            ///         .WithDefaultValue(2)
            ///         .Delayed()
            /// }
            /// </code>
            /// </example>
            IOptionBuilder AddOption(string name, Type dataType);

            /// <summary>
            /// Adds a new node option.
            /// </summary>
            /// <typeparam name="TData">The data type of the option.</typeparam>
            /// <param name="name">The unique identifier of the option.</param>
            /// <returns>An <see cref="IOptionBuilder"/> to further configure the option.</returns>
            /// <remarks>
            /// <c>name</c> is used to identify the option. It must be unique among ports and options on the node. This name is used as the ID when calling <see cref="GetNodeOptionByName(string)"/>.
            /// If <see cref="IOptionBuilder{TData}.WithDisplayName(string)"/> is not used, this name is also used as the option's display label.
            /// </remarks>
            /// <example>
            /// <code>
            /// protected override void OnDefineOptions(IOptionDefinitionContext context)
            /// {
            ///     context.AddOption&lt;int&gt;("MyOption")
            ///         .WithDefaultValue(2)
            ///         .Delayed()
            /// }
            /// </code>
            /// </example>
            IOptionBuilder<TData> AddOption<TData>(string name);
        }

        /// <summary>
        /// Interface that provides methods to define input and output ports during <see cref="Node.OnDefinePorts"/> execution.
        /// </summary>
        /// <remarks>
        /// Use this interface within <see cref="Node.OnDefinePorts"/> to declare the ports a node exposes.
        /// Ports define how the node connects to other nodes by specifying inputs and outputs.
        /// </remarks>
        public interface IPortDefinitionContext
        {
            /// <summary>
            /// Adds a new input port.
            /// </summary>
            /// <param name="portName">The unique identifier of the input port.</param>
            /// <returns>An <see cref="IInputPortBuilder"/> to further configure the input port.</returns>
            /// <remarks>
            /// <c>portName</c> is used to identify the port. It must be unique among input ports and node options on the node. This name is used as the ID when calling <see cref="GetInputPortByName(string)"/>.
            /// If <see cref="IPortBuilder{T}.WithDisplayName(string)"/> is not used, this name is also used as the port's display label.
            /// <br/>
            /// <br/>
            /// <b>Warning:</b> Changing a port's name will break any existing connections, as the name is used as the port's unique ID.
            /// <br/>
            /// <br/>
            /// Use the returned builder to configure port properties and then call <see cref="IPortBuilder{T}.Build"/> to create the port.
            /// </remarks>
            /// <example>
            /// <code>
            /// var port = context.AddInputPort("myInput")
            ///     .WithDisplayName("My Input Port")
            ///     .WithDataType&lt;int&gt;()
            ///     .WithConnectorUI(PortConnectorUI.Circle)
            ///     .Build();
            /// </code>
            /// </example>
            IInputPortBuilder AddInputPort(string portName);

            /// <summary>
            /// Adds a new output port with the specified name.
            /// </summary>
            /// <param name="portName">The unique identifier of the output port.</param>
            /// <returns>An <see cref="IOutputPortBuilder"/> to further configure the output port.</returns>
            /// <remarks>
            /// <c>portName</c> is used to identify the port. It must be unique among output ports on the node. This name is used as the ID when calling <see cref="GetOutputPortByName(string)"/>.
            /// If <see cref="IPortBuilder{T}.WithDisplayName(string)"/> is not used, this name is also used as the port's display label.
            /// <br/>
            /// <br/>
            /// <b>Warning:</b> Changing a port's name will break any existing connections, as the name is used as the port's unique ID.
            /// <br/>
            /// <br/>
            /// Use the returned builder to configure port properties and then call <see cref="IPortBuilder{T}.Build"/> to create the port.
            /// </remarks>
            /// <example>
            /// <code>
            /// var port = context.AddOutputPort("myOutput")
            ///     .WithDisplayName("My Output Port")
            ///     .WithDataType(typeof(float))
            ///     .WithConnectorUI(PortConnectorUI.Arrowhead)
            ///     .Build();
            /// </code>
            /// </example>
            IOutputPortBuilder AddOutputPort(string portName);

            /// <summary>
            /// Adds a new typed input port with the specified name.
            /// </summary>
            /// <typeparam name="T">The data type of the input port.</typeparam>
            /// <param name="portName">The unique identifier of the input port.</param>
            /// <returns>An <see cref="IInputPortBuilder{T}"/> to further configure the typed input port.</returns>
            /// <remarks>
            /// <c>portName</c> is used to identify the port. It must be unique among input ports on the node. This name is used as the ID when calling <see cref="GetInputPortByName(string)"/>.
            /// If <see cref="IPortBuilder{T}.WithDisplayName(string)"/> is not used, this name is also used as the port's display label.
            /// <br/>
            /// <br/>
            /// <b>Warning:</b> Changing a port's name will break any existing connections, as the name is used as the port's unique ID.
            /// <br/>
            /// <br/>
            /// Use the returned builder to configure port properties and then call <see cref="IPortBuilder{T}.Build"/> to create the port.
            /// </remarks>
            /// <example>
            /// <code>
            /// var port = context.AddInputPort&lt;string&gt;("stringInput")
            ///     .WithDisplayName("String Input")
            ///     .WithDefaultValue("default text")
            ///     .Build();
            /// </code>
            /// </example>
            IInputPortBuilder<T> AddInputPort<T>(string portName)
            {
                return AddInputPort(portName).WithDataType<T>();
            }

            /// <summary>
            /// Adds a new typed output port with the specified name.
            /// </summary>
            /// <typeparam name="T">The data type of the output port.</typeparam>
            /// <param name="portName">The unique identifier of the output port.</param>
            /// <returns>An <see cref="IOutputPortBuilder{T}"/> to further configure the typed output port.</returns>
            /// <remarks>
            /// <c>portName</c> is used to identify the port. It must be unique among output ports on the node. This name is used as the ID when calling <see cref="GetOutputPortByName(string)"/>.
            /// If <see cref="IPortBuilder{T}.WithDisplayName(string)"/> is not used, this name is also used as the port's display label.
            /// <br/>
            /// <br/>
            /// <b>Warning:</b> Changing a port's name will break any existing connections, as the name is used as the port's unique ID.
            /// <br/>
            /// <br/>
            /// Use the returned builder to configure port properties and then call <see cref="IPortBuilder{T}.Build"/> to create the port.
            /// </remarks>
            /// <example>
            /// <code>
            /// var port = context.AddOutputPort&lt;bool&gt;("boolOutput")
            ///     .WithDisplayName("Boolean Output")
            ///     .Build();
            /// </code>
            /// </example>
            IOutputPortBuilder<T> AddOutputPort<T>(string portName)
            {
                return AddOutputPort(portName).WithDataType<T>();
            }
        }

        /// <summary>
        /// The Graph that contains this node.
        /// </summary>
        public Graph Graph => (m_Implementation?.GraphModel as GraphModelImp)?.Graph;

        /// <summary>
        /// The globally unique identifier for this node.
        /// </summary>
        /// <remarks>
        /// This GUID uniquely identifies the node instance and persists across sessions.
        /// When a node is duplicated or copy-pasted, it receives a new GUID.
        /// </remarks>
        public Hash128 Guid => m_Implementation?.Guid ?? default;

        /// <summary>
        /// The text displayed when hovering over the node's header.
        /// </summary>
        public string Tooltip
        {
            get => m_Implementation.Tooltip;
            set => m_Implementation.Tooltip = value;
        }

        /// <summary>
        /// The main text displayed in the node's header.
        /// </summary>
        /// <remarks>
        /// Use this property to specify the node title displayed in the graph view.
        /// To modify the node title displayed in the graph item library, use <see cref="NodeAttribute.Title"/>.
        /// </remarks>
        /// <seealso cref="NodeAttribute.Title"/>
        public string Title
        {
            get => m_Implementation.Title;
            set => m_Implementation.Title = value;
        }

        /// <summary>
        /// The secondary text displayed in the node's header.
        /// </summary>
        public string Subtitle
        {
            get => m_Implementation.Subtitle;
            set => m_Implementation.Subtitle = value;
        }

        /// <summary>
        /// The highlight color of the node. The highlight is located on the upper border of nodes, and on the upper and lower borders of context nodes.
        /// </summary>
        public Color DefaultColor
        {
            get => m_Implementation.DefaultColor;
            set => m_Implementation.DefaultColor = value;
        }

        /// <summary>
        /// Called when the node is created or when the graph is enabled.
        /// </summary>
        /// <remarks>
        /// Use this method to perform initialization logic.
        /// </remarks>
        public virtual void OnEnable() { }

        /// <summary>
        /// Called when the node is removed or when the graph is disabled.
        /// </summary>
        /// <remarks>
        /// Use this method to perform any cleanup logic.
        /// </remarks>
        public virtual void OnDisable() { }

        /// <summary>
        /// Defines the structure of the node by building its ports and options.
        /// </summary>
        /// <remarks>
        /// This method calls both <see cref="OnDefineOptions"/> and <see cref="OnDefinePorts"/>
        /// to allow custom definition of the node.
        /// </remarks>
        public void DefineNode()
        {
            if (Graph == null)
                return;
            (m_Implementation as NodeModel)?.DefineNode();
        }

        /// <summary>
        /// Called during <see cref="DefineNode"/> to define the options available on the node.
        /// </summary>
        /// <param name="context">Provides methods for defining node options.</param>
        /// <remarks>
        /// This method is called before <see cref="OnDefinePorts"/>. Override this method to add node options using the provided <see cref="IOptionDefinitionContext"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void OnDefineOptions(IOptionDefinitionContext context)
        /// {
        ///     context.AddNodeOption&lt;bool&gt;(
        ///         optionName: "My Bool",
        ///         optionDisplayName: "myBoolId");
        ///
        ///     context.AddNodeOption(
        ///         optionName: "Label",
        ///         dataType: typeof(string),
        ///         optionDisplayName: "labelId",
        ///         tooltip: "A label.",
        ///         defaultValue: "Default Value");
        /// }
        /// </code>
        /// </example>
        protected virtual void OnDefineOptions(IOptionDefinitionContext context) { }

        /// <summary>
        /// Called during <see cref="DefineNode"/> to define the input and output ports of the node.
        /// </summary>
        /// <param name="context">Provides methods for defining input and output ports.</param>
        /// <remarks>
        /// This method is called after <see cref="OnDefineOptions"/> and is used to declare the structure of the node's connectivity.
        /// Use the provided <see cref="IPortDefinitionContext"/> to define input and output ports using a builder pattern.
        /// The port builder pattern enables fluent configuration of ports by chaining methods such as
        /// <c>WithDisplayName</c>, <c>WithDefaultValue</c>, or <c>WithConnectorUI</c>, followed by <c>Build()</c> to finalize the port.
        /// The <c>portName</c> parameter passed to <c>AddInputPort</c> or <c>AddOutputPort</c> serves as the port's unique identifier.
        /// The <c>portName</c> parameter must be unique within its direction (input or output) on the node and is also used as the display name unless you explicitly call <c>WithDisplayName</c>.
        /// You also use the <c>portName</c> identifier to call <see cref="Node.GetInputPortByName"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override void OnDefinePorts(IPortDefinitionContext context)
        /// {
        ///     var inputPort = context.AddInputPort&lt;string&gt;("stringInput")
        ///         .WithDisplayName("String Input")
        ///         .WithDefaultValue("Default Value")
        ///         .Build();
        ///
        ///     var outputPort = context.AddOutputPort("myOutput")
        ///         .WithDisplayName("My Output Port")
        ///         .WithDataType(typeof(float))
        ///         .WithConnectorUI(PortConnectorUI.Arrowhead)
        ///         .Build();
        /// }
        /// </code>
        /// </example>
        protected virtual void OnDefinePorts(IPortDefinitionContext context) { }

        /// <summary>
        /// The number of node options defined in the node.
        /// </summary>
        public int NodeOptionCount => ((INode)m_Implementation).NodeOptionCount;

        /// <summary>
        /// Retrieves a node option using its zero-based index.
        /// </summary>
        /// <param name="index">Index of the node option, based on display order.</param>
        /// <returns>The node option at the specified index.</returns>
        /// <remarks>
        /// The index is zero-based.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown if the index is out of bounds.
        /// </exception>
        public INodeOption GetNodeOption(int index) => ((INode)m_Implementation).GetNodeOption(index);

        /// <summary>
        /// The node options defined on this node.
        /// </summary>
        public IEnumerable<INodeOption> NodeOptions => ((INode)m_Implementation).NodeOptions;

        /// <summary>
        /// Retrieves a node option using its name.
        /// </summary>
        /// <param name="name">The unique name of the node option.</param>
        /// <returns>The node option with the specified name, or null if none is found.</returns>
        /// <remarks>The node option's name is unique within the node's input ports and node options.</remarks>
        public INodeOption GetNodeOptionByName(string name) => ((INode)m_Implementation).GetNodeOptionByName(name);

        /// <summary>
        /// The number of input ports on the node.
        /// </summary>
        public int InputPortCount => ((INode)m_Implementation).InputPortCount;

        /// <summary>
        /// Retrieves an input port using its index.
        /// </summary>
        /// <param name="index">The index of the input port.</param>
        /// <returns>The input port at the specified index.</returns>
        /// <remarks>
        /// The index is zero-based. The list of input ports is ordered according to their display order in the node.
        /// </remarks>
        public IPort GetInputPort(int index) => ((INode)m_Implementation).GetInputPort(index);

        /// <summary>
        /// Retrieves all input ports on the node in the order they are displayed.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> of input ports.</returns>
        public IEnumerable<IPort> GetInputPorts() => ((INode)m_Implementation).GetInputPorts();

        /// <summary>
        /// Retrieves an input port using its name.
        /// </summary>
        /// <param name="name">The unique name of the input port within this node.</param>
        /// <returns>The input port with the specified name, or null if no match is found.</returns>
        /// <remarks>The input port's name is unique within the node's input ports and node options.</remarks>
        public IPort GetInputPortByName(string name) => ((INode)m_Implementation).GetInputPortByName(name);

        /// <summary>
        /// The number of output ports on the node.
        /// </summary>
        public int OutputPortCount => ((INode)m_Implementation).OutputPortCount;

        /// <summary>
        /// Retrieves an output port using its index in the displayed order.
        /// </summary>
        /// <param name="index">The zero-based index of the output port.</param>
        /// <returns>The output port at the specified index.</returns>
        /// <remarks>
        /// The index is zero-based. The list of output ports is ordered according to their display order in the node.
        /// </remarks>
        public IPort GetOutputPort(int index) => ((INode)m_Implementation).GetOutputPort(index);

        /// <summary>
        /// Retrieves all output ports on the node in the order they are displayed.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> of output ports.</returns>
        public IEnumerable<IPort> GetOutputPorts() => ((INode)m_Implementation).GetOutputPorts();

        /// <summary>
        /// Retrieves an output port using its name.
        /// </summary>
        /// <param name="name">The unique name of the output port within this node.</param>
        /// <returns>The output port with the specified name, or null if no match is found.</returns>
        /// <remarks>The output port's name is unique within the node's output ports.</remarks>
        public IPort GetOutputPortByName(string name) => ((INode)m_Implementation).GetOutputPortByName(name);

        /// <summary>
        /// The position of the node in the graph.
        /// </summary>
        public Vector2 Position
        {
            get => m_Implementation?.Position ?? Vector2.zero;
            set => NodeModificationExtensions.SetNodeModelPosition(GetImplementation(), value);
        }

        /// <summary>
        /// Removes the node from its graph.
        /// </summary>
        public void RemoveFromGraph() => ((INode)m_Implementation).RemoveFromGraph();
    }
}
