// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface for the model of a node that has a single output port.
    /// </summary>
    interface ISingleOutputPortNodeModel
    {
        /// <summary>
        /// Gets the model of the output port for this node.
        /// </summary>
        PortModel OutputPort { get; }
    }
}
