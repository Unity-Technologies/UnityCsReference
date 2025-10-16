// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The type of port. This is different from the port data type.
    /// </summary>
    [UnityRestricted]
    internal class PortType : Enumeration
    {
        /// <summary>
        /// The port is used for the graph flow.
        /// </summary>
        public static readonly PortType Default = new PortType(0, nameof(Default));

        /// <summary>
        /// The port is used as a placeholder for a missing port.
        /// </summary>
        public static readonly PortType MissingPort = new PortType(1, nameof(MissingPort));

        /// <summary>
        /// The port is used as a connection point for transitions in state machines.
        /// </summary>
        public static readonly PortType State = new PortType(2, nameof(State));

        /// <summary>
        /// Base id for port types defined by a tool.
        /// </summary>
        protected static readonly int k_ToolBasePortTypeId = 1000;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortType"/> class.
        /// </summary>
        protected PortType(int id, string name)
            : base(id, name)
        {
        }
    }
}
