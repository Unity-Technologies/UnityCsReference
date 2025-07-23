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
    /// Container holding the data needed to handle a drag and drop operation.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal readonly ref struct HierarchyViewDragAndDropHandlingData
    {
        readonly DragAndDropData m_DragAndDropData;

        /// <summary>
        /// The parent <see cref="HierarchyNode"/> which will accept the new nodes created by the drag and drop operation. This can be the parent of the <see cref="Target"/>, or the <see cref="Target"/> itself if <see cref="DropPosition"/> is <see cref="DragAndDropPosition.OverItem"/>.
        /// </summary>
        public HierarchyNode Parent { get; }

        /// <summary>
        /// The target <see cref="HierarchyNode"/> under the cursor.
        /// </summary>
        public HierarchyNode Target { get; }

        /// <summary>
        /// The insertion index in the <see cref="View"/>.
        /// </summary>
        public int InsertAtIndex { get; }

        /// <summary>
        /// The <see cref="DragAndDropPosition"/>.
        /// </summary>
        public DragAndDropPosition DropPosition { get; }

        /// <summary>
        /// The <see cref="HierarchyView"/> where the drag and drop operation is happening.
        /// </summary>
        public HierarchyView View { get; }

        /// <summary>
        /// The Entity Ids references being dragged.
        /// </summary>
        public IReadOnlyList<EntityId> EntityIds => m_DragAndDropData.entityIds;

        /// <summary>
        /// A list of paths to the assets being dragged.
        /// </summary>
        public string[] Paths => m_DragAndDropData.paths;

        /// <summary>
        /// The object that started the drag.
        /// </summary>
        public object Source => m_DragAndDropData.source;

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal EventModifiers EventModifiers { get; }

        /// <summary>
        /// Gets generic data from the drag and drop operation.
        /// </summary>
        /// <param name="key">The key for this entry.</param>
        /// <returns>The generic data.</returns>
        public object GetGenericData(string key)
        {
            return m_DragAndDropData.GetGenericData(key);
        }

        internal HierarchyViewDragAndDropHandlingData(in HierarchyNode parent, in HierarchyNode target, int insertAtIndex, DragAndDropPosition dropPosition, DragAndDropData dragAndDropData, HierarchyView view, EventModifiers eventModifiers)
        {
            Parent = parent;
            Target = target;
            InsertAtIndex = insertAtIndex;
            DropPosition = dropPosition;
            m_DragAndDropData = dragAndDropData;
            View = view;
            EventModifiers = eventModifiers;
        }
    }
}
