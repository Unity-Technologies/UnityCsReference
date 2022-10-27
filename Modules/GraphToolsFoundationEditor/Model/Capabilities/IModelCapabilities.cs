// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Implements a title and a display title.
    /// </summary>
    interface IHasTitle
    {
        /// <summary>
        /// Title of the declaration model.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Version of the title to display.
        /// </summary>
        string DisplayTitle { get; }
    }

    /// <summary>
    /// Interface for elements that can track their progression.
    /// </summary>
    interface IHasProgress
    {
        /// <summary>
        /// Whether the element have some way to track progression.
        /// </summary>
        bool HasProgress { get; }
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    /// <summary>
    /// An element that can be collapsed.
    /// </summary>
    interface ICollapsible
    {
        /// <summary>
        /// Whether the element is collapsed.
        /// </summary>
        bool Collapsed { get; set; }
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    /// <summary>
    /// An element that can be resized.
    /// </summary>
    interface IResizable
    {
        /// <summary>
        /// The position and size of the element.
        /// </summary>
        Rect PositionAndSize { get; set; }
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    /// <summary>
    /// An element that can be moved.
    /// </summary>
    interface IMovable
    {
        /// <summary>
        /// The position of the element.
        /// </summary>
        Vector2 Position { get; set; }

        /// <summary>
        /// Moves the element.
        /// </summary>
        /// <param name="delta">The amount of the move in the x and y directions.</param>
        void Move(Vector2 delta);
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    /// <summary>
    /// An element that can be renamed.
    /// </summary>
    interface IRenamable
    {
        /// <summary>
        /// Change the name of the declaration model.
        /// </summary>
        /// <param name="name">New name to give to the model.</param>
        void Rename(string name);
    }

    /// <summary>
    /// Interface for temporary wires.
    /// </summary>
    interface IGhostWire
    {
        /// <summary>
        /// The position of the end of the wire.
        /// </summary>
        Vector2 EndPoint { get; }
    }
}
