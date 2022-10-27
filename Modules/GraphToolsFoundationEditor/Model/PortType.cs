// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The type of port. This is different from the port data type.
    /// </summary>
    class PortType : Enumeration
    {
        /// <summary>
        /// The port is used for the execution flow.
        /// </summary>
        public static readonly PortType Execution = new PortType(0, nameof(Execution));

        /// <summary>
        /// The port is used for data flow.
        /// </summary>
        public static readonly PortType Data = new PortType(1, nameof(Data));

        /// <summary>
        /// The port is used as a placeholder for a missing port.
        /// </summary>
        public static readonly PortType MissingPort = new PortType(2, nameof(MissingPort));

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
