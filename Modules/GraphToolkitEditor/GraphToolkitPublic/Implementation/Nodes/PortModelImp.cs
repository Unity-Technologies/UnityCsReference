// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor.Implementation
{
    class PortModelImp : PortModel
    {
        public PortConnectorUI ConnectorUI { get; set; }

        public override string DefaultTooltip => ComputePortLabel(true) + ": " + (Direction == PortDirection.Output ? "Output" : "Input") +
            (((IPort)this).DataType == null ? string.Empty : $" of type {DataTypeHandle.FriendlyName}");

        public PortModelImp(PortNodeModel nodeModel, PortDirection direction, PortOrientation orientation, string portName, PortType portType, TypeHandle dataType, string portId, PortModelOptions options, Attribute[] attributes, PortModel parentPort)
            : base(nodeModel, direction, orientation, portName, portType, dataType, portId, options, attributes, parentPort) { }
    }
}
