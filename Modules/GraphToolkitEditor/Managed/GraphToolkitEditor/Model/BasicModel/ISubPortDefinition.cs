// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The scope used to define sub ports on a port.
    /// </summary>
    /// <seealso cref="GraphModel.OnDefineSubPorts"/>
    [UnityRestricted]
    internal interface ISubPortDefinition
    {
        /// <summary>
        /// If true, the sub ports must be specified, because the port is either expanded of one of its former sub ports has a wire connected to it.
        /// </summary>
        bool MustSpecifySubPorts { get; }

        /// <summary>
        /// Add a sub port to an expandable port.
        /// </summary>
        /// <param name="portName">The name of the sub port.</param>
        /// <param name="dataType">The <see cref="TypeHandle"/> of the sub port.</param>
        /// <param name="portId">The id of the sub port. portName will be used if not specified.</param>
        /// <param name="options">The options of the sub port.</param>
        /// <param name="attributes">The attributes of the sub port.</param>
        /// <returns>The new sub port.</returns>
        /// <remarks>The UniqueName of a sub port is computed with the parent port UniqueName and the portId or portName. </remarks>
        PortModel AddSubPort(string portName, TypeHandle dataType, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null);

        /// <summary>
        /// Add a sub port to an expandable port.
        /// </summary>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
        /// <typeparam name="TDataType">The data type of the port.</typeparam>
        /// <returns>The newly created input port.</returns>
        /// <remarks>The UniqueName of a sub port is computed with the parent port UniqueName and the portId or portName. </remarks>
        PortModel AddSubPort<TDataType>(string portName, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null);

        /// <summary>
        /// Adds a new sub port, linked to a field of the parent port type.
        /// </summary>
        /// <param name="fieldInfo">A <see cref="FieldInfo"/> for a field from the type of the parent port.</param>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
        /// <returns>The newly created sub port.</returns>
        PortModel AddFieldSubPort(FieldInfo fieldInfo, string portName = null, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null);

        /// <summary>
        /// Adds a new sub port, linked to a property of the parent port type.
        /// </summary>
        /// <param name="propertyInfo">A <see cref="PropertyInfo"/> for a property from the type of the parent port.</param>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
        /// <returns>The newly created sub port.</returns>
        PortModel AddPropertySubPort(PropertyInfo propertyInfo, string portName = null, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null);

        /// <summary>
        /// Adds a new input sub port on the node.
        /// </summary>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="typeHandle">The data type of the port.</param>
        /// <param name="getter">A delegate that take no parameter and returns a value of the type from the resolved <see cref="TypeHandle"/>.</param>
        /// <param name="setter">A delegate that take a parameter of the type from the resolved <see cref="TypeHandle"/>.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
        /// <returns>The newly created input port.</returns>
        PortModel AddInputSubPort(string portName, TypeHandle typeHandle, Func<object> getter, Action<object> setter, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null);

        /// <summary>
        /// Adds a new input sub port on the node, with its value specified by delegates.
        /// </summary>
        /// <param name="portName">The name of port to create.</param>
        /// <param name="getter">A delegate that take no parameter and returns a value of the type from the <typeparamref name="TDataType"/>.</param>
        /// <param name="setter">A delegate that take a parameter of the type from the <typeparamref name="TDataType"/>.</param>
        /// <param name="portId">The ID of the port to create.</param>
        /// <param name="options">The options for the port to create.</param>
        /// <param name="attributes">The attributes used to convey information about the port, if any.</param>
        /// <typeparam name="TDataType">The data type of the port.</typeparam>
        /// <returns>The newly created input port.</returns>
        PortModel AddInputSubPort<TDataType>(string portName, Func<TDataType> getter, Action<TDataType> setter, string portId = null, PortModelOptions options = PortModelOptions.Default, Attribute[] attributes = null);
    }
}
