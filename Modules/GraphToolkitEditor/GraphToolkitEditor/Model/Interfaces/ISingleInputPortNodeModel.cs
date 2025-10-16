// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for the model of a node that has a single input port.
    /// </summary>
    [UnityRestricted]
    internal interface ISingleInputPortNodeModel
    {
        /// <summary>
        /// Gets the model of the input port for this node.
        /// </summary>
        PortModel InputPort { get; }
    }
}
