// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Represents a dependency between two nodes linked together by a wire.
    /// </summary>
    class LinkedNodesDependency : IDependency
    {
        /// <summary>
        /// The dependent port.
        /// </summary>
        public PortModel DependentPort { get; set; }

        /// <summary>
        /// The parent port.
        /// </summary>
        public PortModel ParentPort { get; set; }

        /// <inheritdoc />
        public AbstractNodeModel DependentNode => DependentPort.NodeModel;

        /// <summary>
        /// The number of such a dependency in a graph.
        /// </summary>
        public int Count { get; set; }
    }
}
