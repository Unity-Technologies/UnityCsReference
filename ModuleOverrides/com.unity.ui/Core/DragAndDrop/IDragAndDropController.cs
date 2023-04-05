// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    internal interface IDragAndDropController<in TArgs>
    {
        bool CanStartDrag(IEnumerable<int> itemIds);
        StartDragArgs SetupDragAndDrop(IEnumerable<int> itemIds, bool skipText = false);
        DragVisualMode HandleDragAndDrop(TArgs args);
        void OnDrop(TArgs args);

        void DragCleanup() { }
        void HandleAutoExpand(ReusableCollectionItem item, Vector2 pointerPosition) { }
        IEnumerable<int> GetSortedSelectedIds() => Enumerable.Empty<int>();
    }

    /// <summary>
    /// The status of a drag-and-drop operation.
    /// </summary>
    public enum DragVisualMode
    {
        /// <summary>
        /// The drag-and-drop is currently unhandled.
        /// </summary>
        None,
        /// <summary>
        /// The drag-and-drop handlers want to copy data.
        /// </summary>
        Copy,
        /// <summary>
        /// The drag-and-drop handlers want to move data.
        /// </summary>
        Move,
        /// <summary>
        /// The drag-and-drop operation is being rejected by the handlers.
        /// </summary>
        Rejected
    }

    /// <summary>
    /// Information about a drag-and-drop operation that is about to start.
    /// See <see cref="BaseVerticalCollectionView.canStartDrag"/>.
    /// </summary>
    public readonly struct CanStartDragArgs
    {
        /// <summary>
        /// The element on which the drag operation is starting.
        /// </summary>
        public readonly VisualElement draggedElement;

        /// <summary>
        /// The ID of the dragged element.
        /// </summary>
        public readonly int id;

        /// <summary>
        /// The selected IDs in the source.
        /// </summary>
        public readonly IEnumerable<int> selectedIds;

        internal CanStartDragArgs(VisualElement draggedElement, int id, IEnumerable<int> selectedIds)
        {
            this.draggedElement = draggedElement;
            this.id = id;
            this.selectedIds = selectedIds;
        }
    }

    /// <summary>
    /// Information about a drag-and-drop operation that just started.
    /// You can use it to store generic data for the rest of the drag.
    /// See <see cref="BaseVerticalCollectionView.setupDragAndDrop"/>.
    /// </summary>
    public readonly struct SetupDragAndDropArgs
    {
        /// <summary>
        /// The element on which the drag operation started.
        /// </summary>
        public readonly VisualElement draggedElement;

        /// <summary>
        /// The selected IDs in the source.
        /// </summary>
        public readonly IEnumerable<int> selectedIds;

        /// <summary>
        /// Provides entry points to initialize data and visual of the new drag-and-drop operation.
        /// </summary>
        public readonly StartDragArgs startDragArgs;

        internal SetupDragAndDropArgs(VisualElement draggedElement, IEnumerable<int> selectedIds, StartDragArgs startDragArgs)
        {
            this.draggedElement = draggedElement;
            this.selectedIds = selectedIds;
            this.startDragArgs = startDragArgs;
        }
    }

    /// <summary>
    /// Information about a drag-and-drop operation in progress.
    /// See <see cref="BaseVerticalCollectionView.dragAndDropUpdate"/> and <see cref="BaseVerticalCollectionView.handleDrop"/>.
    /// </summary>
    public readonly struct HandleDragAndDropArgs
    {
        readonly DragAndDropArgs m_DragAndDropArgs;

        /// <summary>
        /// The world position of the pointer.
        /// </summary>
        public Vector2 position { get; }

        /// <summary>
        /// The target of the drop. There is only a target when hovering over an item. <see cref="DropPosition.OverItem"/>
        /// </summary>
        public object target => m_DragAndDropArgs.target;

        /// <summary>
        /// The index at which the drop operation wants to happen.
        /// </summary>
        public int insertAtIndex => m_DragAndDropArgs.insertAtIndex;

        /// <summary>
        /// The new parent targeted by the drag-and-drop operation. Used only for trees.
        /// </summary>
        /// <remarks>
        /// Will always be -1 for drag-and-drop operations in list views.
        /// </remarks>
        public int parentId => m_DragAndDropArgs.parentId;

        /// <summary>
        /// The child index under the <see cref="parentId"/> that the drag-and-drop operation targets. Used only for trees.
        /// </summary>
        /// <remarks>
        /// Will always be -1 for drag-and-drop operations in list views.
        /// </remarks>
        public int childIndex => m_DragAndDropArgs.childIndex;

        /// <summary>
        /// The type of drop position.
        /// </summary>
        public DragAndDropPosition dropPosition => m_DragAndDropArgs.dragAndDropPosition;

        /// <summary>
        /// Data stored for the drag-and-drop operation.
        /// </summary>
        public DragAndDropData dragAndDropData => m_DragAndDropArgs.dragAndDropData;

        internal HandleDragAndDropArgs(Vector2 position, DragAndDropArgs dragAndDropArgs)
        {
            this.position = position;
            m_DragAndDropArgs = dragAndDropArgs;
        }
    }

    /// <summary>
    /// Provides entry points to initialize the new drag-and-drop operation.
    /// </summary>
    public struct StartDragArgs
    {
        /// <summary>
        /// Initializes a <see cref="StartDragArgs"/>.
        /// </summary>
        /// <param name="title">The text to use during the drag.</param>
        /// <param name="visualMode">The visual mode the drag starts with.</param>
        public StartDragArgs(string title, DragVisualMode visualMode)
        {
            this.title = title;
            this.visualMode = visualMode;
            genericData = null;
            unityObjectReferences = null;
        }

        // This API is used by com.unity.entities, we cannot remove it yet.
        internal StartDragArgs(string title, object target)
        {
            this.title = title;
            visualMode = DragVisualMode.Move;
            genericData = null;
            unityObjectReferences = null;
            SetGenericData(DragAndDropData.dragSourceKey, target);
        }

        /// <summary>
        /// The title displayed near the pointer to identify what is being dragged.
        /// Should be set during the <see cref="BaseVerticalCollectionView.setupDragAndDrop"/> callback.
        /// </summary>
        public string title { get; }

        /// <summary>
        /// The mode to use for this drag-and-drop operation.
        /// </summary>
        public DragVisualMode visualMode { get; }

        internal Hashtable genericData { get; private set; }
        internal IEnumerable<Object> unityObjectReferences { get; private set; }

        /// <summary>
        /// Sets data associated with the current drag-and-drop operation.
        /// </summary>
        /// <param name="key">The key for this entry.</param>
        /// <param name="data">The data to store.</param>
        public void SetGenericData(string key, object data)
        {
            genericData ??= new Hashtable();
            genericData[key] = data;
        }

        /// <summary>
        /// Sets Unity Objects associated with the current drag-and-drop operation.
        /// </summary>
        /// <param name="references">The Unity Object references.</param>
        public void SetUnityObjectReferences(IEnumerable<Object> references)
        {
            unityObjectReferences = references;
        }
    }
}
