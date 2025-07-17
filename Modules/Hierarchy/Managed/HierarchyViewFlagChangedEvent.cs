// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// This is the type of the HierarchyViewFlagChangedEvent
    /// </summary>
    internal enum HierarchyViewFlagChangedEventType
    {
        /// <summary>
        /// Indicate that node flags were cleared.
        /// </summary>
        Clear,

        /// <summary>
        /// Indicate that node flags were toggled.
        /// </summary>
        Toggle,

        /// <summary>
        /// Indicate that node flags were Set.
        /// </summary>
        Set
    }

    /// <summary>
    /// Event that is fired when a node's flags are changed.
    /// </summary>
    internal readonly ref struct HierarchyViewFlagChangedEvent
    {
        /// <summary>
        /// This is the type of the HierarchyViewFlagChangedEvent
        /// </summary>
        public readonly HierarchyViewFlagChangedEventType EventType;

        /// <summary>
        /// Were all nodes in hierarchy impacted.
        /// </summary>
        public bool AllNodes => Nodes == null;

        /// <summary>
        /// Was the flag changed recursive.
        /// </summary>
        public readonly bool Recursive;

        /// <summary>
        /// List of nodes that were used as input to trigger a flag changed event. Note that this list is not necessarily the complete list of nodes that changed.
        /// Ex in case of a SetFlags(Selected, node1, true) Nodes will only contain node1 and not the whole recursive lists of nodes that might have changed.
        /// </summary>
        public readonly ReadOnlySpan<HierarchyNode> Nodes;

        /// <summary>
        ///  Which flags were changed.
        /// </summary>
        public readonly HierarchyNodeFlags Flags;

        internal HierarchyViewFlagChangedEvent(HierarchyViewFlagChangedEventType evtType, HierarchyNodeFlags flags)
        {
            EventType = evtType;
            Flags = flags;
            Recursive = false;
            Nodes = null;
        }

        internal HierarchyViewFlagChangedEvent(HierarchyViewFlagChangedEventType evtType, HierarchyNodeFlags flags, ReadOnlySpan<HierarchyNode> nodes, bool recursive = false)
        {
            EventType = evtType;
            Flags = flags;
            Recursive = recursive;
            Nodes = nodes;
        }

        internal HierarchyViewFlagChangedEvent(HierarchyViewFlagChangedEventType evtType, HierarchyNodeFlags flags, in HierarchyNode node, bool recursive = false)
        {
            EventType = evtType;
            Flags = flags;
            Nodes = new[] { node };
            Recursive = recursive;
        }
    }
}
