// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal interface IDragAndDrop
    {
        void StartDrag(StartDragArgs args, Vector3 pointerPosition);
        void UpdateDrag(Vector3 pointerPosition);
        void AcceptDrag();
        void DragCleanup();
        void SetVisualMode(DragVisualMode visualMode);
        DragAndDropData data { get; }
    }

    internal interface IDragAndDropData
    {
        object GetGenericData(string key);
        object userData { get; }
        IEnumerable<Object> unityObjectReferences { get; }
    }

    /// <summary>
    /// Data stored during drag-and-drop operations, enabling information to be carried throughout the process.
    /// </summary>
    internal abstract class DragAndDropData : IDragAndDropData
    {
        internal const string dragSourceKey = "__unity-drag-and-drop__source-view";

        /// <summary>
        /// Gets data associated with the current drag-and-drop operation.
        /// </summary>
        /// <param name="key">The key for this entry.</param>
        /// <returns>The object stored for this key.</returns>
        public abstract object GetGenericData(string key);

        object IDragAndDropData.userData => GetGenericData(dragSourceKey);

        /// <summary>
        /// Sets data associated with the current drag-and-drop operation.
        /// </summary>
        /// <param name="key">The key for this entry.</param>
        /// <param name="data">The data to store.</param>
        public abstract void SetGenericData(string key, object data);

        /// <summary>
        /// The object that started the drag.
        /// </summary>
        /// <remarks>
        /// For collection views such as <see cref="ListView" />, <see cref="TreeView" />, and others, this refers to
        /// the corresponding view that started the drag-and-drop.
        /// </remarks>
        public abstract object source { get; }

        /// <summary>
        /// The state of the current drag operation.
        /// </summary>
        public abstract DragVisualMode visualMode { get; }

        /// <summary>
        /// Unity Object references being dragged.
        /// </summary>
        public abstract IEnumerable<Object> unityObjectReferences { get; }
    }
}
