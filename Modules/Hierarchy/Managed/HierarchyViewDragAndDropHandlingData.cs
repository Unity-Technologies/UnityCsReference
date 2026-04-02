// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Contains the data needed to handle a drag and drop operation in a <see cref="HierarchyView"/>.
    /// </summary>
    public readonly ref struct HierarchyViewDragAndDropHandlingData
    {
        readonly DragAndDropData m_DragAndDropData;

        /// <summary>
        /// Gets the parent <see cref="HierarchyNode"/> that accepts the new nodes from the drag and drop operation. This is either the parent of the <see cref="Target"/>, or the <see cref="Target"/> itself if <see cref="DropPosition"/> is <see cref="DragAndDropPosition.OverItem"/>.
        /// </summary>
        public HierarchyNode Parent { get; }

        /// <summary>
        /// Gets the target <see cref="HierarchyNode"/> under the cursor.
        /// </summary>
        public HierarchyNode Target { get; }

        /// <summary>
        /// Gets the insertion index in the <see cref="HierarchyView"/>.
        /// </summary>
        public int InsertAtIndex { get; }

        /// <summary>
        /// Gets the child index in the <see cref="HierarchyView"/>.
        /// </summary>
        public int ChildIndex { get; }

        /// <summary>
        /// Gets the <see cref="DragAndDropPosition"/> of the drop relative to the <see cref="Target"/>.
        /// </summary>
        public DragAndDropPosition DropPosition { get; }

        /// <summary>
        /// Gets the <see cref="HierarchyView"/> where the drag and drop operation occurs.
        /// </summary>
        public HierarchyView View { get; }

        /// <summary>
        /// Gets the <see cref="EntityId"/> values involved in the drag and drop operation.
        /// </summary>
        public IReadOnlyList<EntityId> EntityIds => m_DragAndDropData.entityIds;

        /// <summary>
        /// Gets the paths to the assets involved in the drag and drop operation.
        /// </summary>
        public string[] Paths => m_DragAndDropData.paths;

        /// <summary>
        /// Gets the object that started the drag and drop operation.
        /// </summary>
        public object Source => m_DragAndDropData.source;

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal EventModifiers EventModifiers { get; }

        /// <summary>
        /// Gets generic data from the drag and drop operation.
        /// </summary>
        /// <param name="key">The key for this entry.</param>
        /// <returns>The generic data associated with the specified key.</returns>
        public object GetGenericData(string key)
        {
            return m_DragAndDropData.GetGenericData(key);
        }

        internal HierarchyViewDragAndDropHandlingData(in HierarchyNode parent, in HierarchyNode target, int insertAtIndex, int childIndex, DragAndDropPosition dropPosition, DragAndDropData dragAndDropData, HierarchyView view, EventModifiers eventModifiers)
        {
            Parent = parent;
            Target = target;
            InsertAtIndex = insertAtIndex;
            ChildIndex = childIndex;
            DropPosition = dropPosition;
            m_DragAndDropData = dragAndDropData;
            View = view;
            EventModifiers = eventModifiers;
        }
    }
}
