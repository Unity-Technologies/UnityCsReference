// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Attribute to mark a field as being a node option, one that appear in the Node Options section
    /// of the model inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    class NodeOptionAttribute : Attribute
    {
        /// <summary>
        /// Whether the node option should only be shown in the inspector.
        /// </summary>
        /// <remarks>All node options should show up in the inspector, but not all node options should show up on the node.</remarks>
        public bool ShowInInspectorOnly { get; }

        /// <summary>
        /// The displayed name of the node option.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeOptionAttribute"/> class.
        /// </summary>
        public NodeOptionAttribute()
        {
            ShowInInspectorOnly = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeOptionAttribute"/> class.
        /// </summary>
        /// <param name="showInInspectorOnly">Whether the node option should only be shown in the inspector.</param>
        /// <param name="displayName">The displayed name of the node option</param>
        public NodeOptionAttribute(bool showInInspectorOnly = false, string displayName = null)
        {
            ShowInInspectorOnly = showInInspectorOnly;
            DisplayName = displayName;
        }
    }
}
