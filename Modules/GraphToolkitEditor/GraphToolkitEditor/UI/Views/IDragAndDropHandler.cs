// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Provides methods to handle drag-and-drop operations.
    /// </summary>
    /// <remarks>
    /// 'IDragAndDropHandler' provides methods to manage drag-and-drop operations within the graph.
    /// Implement this interface to define custom behavior for handling items that are dragged and dropped.
    /// </remarks>
    [PublicAPI]
    [UnityRestricted]
    internal interface IDragAndDropHandler
    {
        /// <summary>
        /// Tells whether this handler wants to actively accept or reject the dropped objects
        /// and eventually perform the drop operation.
        /// </summary>
        /// <returns>True if the handler wants to handle the operation, false if it does not.</returns>
        bool CanHandleDrop();

        /// <summary>
        /// Handler for any OnDragLeave passed to this element
        /// </summary>
        /// <param name="evt">event passed.</param>
        void OnDragEnter(DragEnterEvent evt);

        /// <summary>
        /// Handler for any DragLeaveEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed.</param>
        void OnDragLeave(DragLeaveEvent evt);

        /// <summary>
        /// Handler for any DragUpdatedEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed.</param>
        void OnDragUpdated(DragUpdatedEvent evt);

        /// <summary>
        /// Handler for any DragPerformEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed.</param>
        void OnDragPerform(DragPerformEvent evt);

        /// <summary>
        /// Handler for any DragExitedEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed.</param>
        void OnDragExited(DragExitedEvent evt);
    }
}
