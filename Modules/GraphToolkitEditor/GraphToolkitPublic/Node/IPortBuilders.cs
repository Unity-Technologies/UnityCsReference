// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base interface representing a generic port builder. Used in a builder pattern to configure and construct ports.
    /// </summary>
    /// <typeparam name="T">The concrete builder type returned by configuration methods.</typeparam>
    /// <remarks>
    /// This interface supports a builder pattern, where each method returns the builder instance, allowing chained configuration
    /// of port settings before final construction using <see cref="Build"/>.
    /// Use derived interfaces such as <see cref="IInputPortBuilder"/> or <see cref="IOutputPortBuilder"/> to build specific port types.
    /// </remarks>
    /// <example>
    /// <code>
    /// context.AddInputPort("MyInput")
    ///        .WithDataType(typeof(int))
    ///        .WithDisplayName("Input Value")
    ///        .Build();
    /// </code>
    /// </example>
    public interface IPortBuilder<T>
    {
        /// <summary>
        /// Builds and returns the final <see cref="IPort"/> instance based on the current configuration of the builder.
        /// </summary>
        /// <returns>The constructed <see cref="IPort"/>.</returns>
        /// <remarks>
        /// Call this method after setting all desired configuration options using the builder methods.
        /// The builder captures options such as the port's data type, display name, connector style, and default value, if applicable.
        /// This method finalizes the port and adds it to the graph. After calling <c>Build()</c>, do not modify the builder further.
        /// This method is typically called at the end of a chain.
        /// </remarks>
        /// <example>
        /// <code>
        /// context.AddInputPort("MyPort")
        ///        .WithDataType(typeof(float))
        ///        .WithDisplayName("My Float Port")
        ///        .WithConnectorUI(PortConnectorUI.Circle)
        ///        .Build();
        /// </code>
        /// </example>
        IPort Build();

        /// <summary>
        /// Configures the display name of the port being built.
        /// </summary>
        /// <param name="displayName">The display name to assign to the port.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        /// <remarks>
        /// Use this method to assign a custom label to the port. This label appears in the user interface next to the port and helps clarify its purpose.
        /// Set the display name before calling <see cref="Build"/>. The value does not affect functionality but improves usability and readability.
        /// If not set, the port name passed during creation (see: <see cref="Node.IPortDefinitionContext.AddInputPort"/> and <see cref="Node.IPortDefinitionContext.AddOutputPort"/>) is used as the fallback display name.
        /// </remarks>
        T WithDisplayName(string displayName);

        /// <summary>
        /// Configures the tooltip text of the port being built.
        /// </summary>
        /// <param name="tooltip">The tooltip text to assign to the port.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        /// <remarks>
        /// Use this method to assign a custom tooltip description to the port. The tooltip appears in the user interface when hovering over the port name.
        /// Set the tooltip before calling <see cref="Build"/>.
        /// If not set, the display name and type of the port are used as a fallback in the format "DISPLAY NAME: Input of type PORT TYPE"
        /// </remarks>
        T WithTooltip(string tooltip);

        /// <summary>
        /// Configures the connector UI shape for the port being built.
        /// </summary>
        /// <param name="connectorUI">The <see cref="PortConnectorUI"/> shape to use.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        /// <remarks>
        /// Use this method to control the appearance of the port’s connector in the UI. The <see cref="PortConnectorUI"/> enum provides options such as
        /// <see cref="PortConnectorUI.Circle"/> or <see cref="PortConnectorUI.Arrowhead"/>. These shapes help users visually distinguish between different kinds of ports or flows.
        /// This setting affects only the UI and does not impact port behavior or connectivity.
        /// Call this method before <see cref="IPortBuilder{T}.Build"/> to ensure the selected style is applied to the constructed port.
        /// </remarks>
        T WithConnectorUI(PortConnectorUI connectorUI);

        /// <summary>
        /// Configures the port to be built as a vertical port.
        /// </summary>
        /// <returns>The current builder instance for method chaining.</returns>
        /// <remarks>
        /// Use this method to place the port at the top (as an input) or bottom (as an output) of the node. This allows your graph to flow vertically, from top to bottom.
        /// Vertical ports can connect to horizontal ports of the same type and vice versa. The name label for the ports is not displayed on the node. The name labels are only displayed via the port tooltip.
        /// Ports are built as horizontal (i.e. left to right) by default. Call this method before <see cref="IPortBuilder{T}.Build"/> to ensure a port is built as a vertical port. 
        /// However, this method is not supported for ports on a Block node, which are always displayed horizontally.
        /// </remarks>
        T AsVertical();
    }

    /// <summary>
    /// Base interface for input port builders in a port definition context.
    /// </summary>
    /// <typeparam name="T">The specific builder type implementing this interface.</typeparam>
    /// <remarks>
    /// Use this interface when defining input ports in a custom node. It extends the generic <see cref="IPortBuilder{T}"/> interface with input-specific configuration options.
    /// The generic parameter <typeparamref name="T"/> allows method chaining using the builder pattern.
    /// </remarks>
    public interface IInputBasePortBuilder<T> : IPortBuilder<T>
    {
        /// <summary>
        /// Configures the input port to use the <see cref="DelayedAttribute"/>.
        /// </summary>
        /// <returns>The current builder instance for method chaining.</returns>
        /// <remarks>
        /// Apply this setting when the port’s value should only update after the user finishes editing input in the UI.
        /// This is useful for optimizing performance or avoiding intermediate updates during data entry.
        /// </remarks>
        T Delayed();

        /// <summary>
        /// Configures the input port to use the <see cref="TextAreaAttribute"/>.
        /// </summary>
        /// <param name="minLines">The maximum amount of lines the text area can show before it starts using a scrollbar. Defaults to 3.</param>
        /// <param name="maxLines">The minimum amount of lines the text area will use. Defaults to 3.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        /// <remarks>
        /// Applies only to string input ports. Use this setting to make the port value a Text Area.
        /// A Text Area is a multi-line input field that allows users to enter large amounts of text.
        /// Its height automatically adjusts between specified minimum and maximum lines, and a scrollbar appears if the content exceeds the visible area.
        /// </remarks>
        T AsTextArea(int minLines = 3, int maxLines = 3);
    }

    /// <summary>
    /// Interface for defining an output port.
    /// </summary>
    /// <remarks>
    /// Use this interface to create an output port before you assign its data type.
    /// To assign a data type, call <see cref="WithDataType(Type)"/> or <see cref="WithDataType{TData}"/>.
    /// </remarks>
    public interface IOutputPortBuilder : IPortBuilder<IOutputPortBuilder>
    {
        /// <summary>
        /// Configures the data type of the output port.
        /// </summary>
        /// <param name="portType">The data type of the port.</param>
        /// <returns>An output port builder with the specified data type.</returns>
        IOutputPortBuilder WithDataType(Type portType);

        /// <summary>
        /// Configures the generic data type <typeparamref name="TData"/> of the output port.
        /// </summary>
        /// <typeparam name="TData">The data type of the output port.</typeparam>
        /// <returns>An output port builder with the specified data type.</returns>
        IOutputPortBuilder<TData> WithDataType<TData>();
    }

    /// <summary>
    /// Interface for defining an input port.
    /// </summary>
    public interface IInputPortBuilder : IInputBasePortBuilder<IInputPortBuilder>
    {
        /// <summary>
        /// Configures the data type of the input port.
        /// </summary>
        /// <param name="portType">The data type of the port.</param>
        /// <returns>An input port builder that supports typed values.</returns>
        ITypedInputPortBuilder WithDataType(Type portType);

        /// <summary>
        /// Configures the generic data type <typeparamref name="TData"/> of the input port.
        /// </summary>
        /// <typeparam name="TData">The data type of the input port.</typeparam>
        /// <returns>An input port builder with the specified data type.</returns>
        IInputPortBuilder<TData> WithDataType<TData>();

    }

    /// <summary>
    /// Interface for defining a typed input port.
    /// </summary>
    /// <remarks>
    /// Use this interface to configure additional features for a typed input port, such as default values.
    /// </remarks>
    public interface ITypedInputPortBuilder : IInputBasePortBuilder<ITypedInputPortBuilder>
    {
        /// <summary>
        /// Configures a default value for the input port.
        /// </summary>
        /// <param name="defaultValue">The default value for the port.</param>
        /// <returns>An input port builder configured with the default value.</returns>
        ITypedInputPortBuilder WithDefaultValue(object defaultValue);
    }

    /// <summary>
    /// Interface for defining a typed output port.
    /// </summary>
    /// <remarks>
    /// Use this interface to further customize a typed output port.
    /// </remarks>
    public interface ITypedOutputPortBuilder : IPortBuilder<ITypedOutputPortBuilder>
    {
    }

    /// <summary>
    /// Interface for defining an output port that uses a specific generic data type.
    /// </summary>
    /// <typeparam name="TData">The data type of the output port.</typeparam>
    /// <remarks>
    /// This interface supports method chaining and further customization for typed output ports.
    /// </remarks>
    public interface IOutputPortBuilder<TData> : IPortBuilder<IOutputPortBuilder<TData>>
    {
    }

    /// <summary>
    /// Interface for defining an input port that uses a specific generic data type.
    /// </summary>
    /// <typeparam name="TData">The data type of the input port.</typeparam>
    /// <remarks>
    /// Use this interface to assign a typed default value to the port.
    /// </remarks>
    public interface IInputPortBuilder<TData> : IInputBasePortBuilder<IInputPortBuilder<TData>>
    {
        /// <summary>
        /// Configures a default value of type <typeparamref name="TData"/> to the input port.
        /// </summary>
        /// <param name="defaultValue">The default value of type <typeparamref name="TData"/>.</param>
        /// <returns>An input port builder configured with the default value.</returns>
        IInputPortBuilder<TData> WithDefaultValue(TData defaultValue);
    }
}
