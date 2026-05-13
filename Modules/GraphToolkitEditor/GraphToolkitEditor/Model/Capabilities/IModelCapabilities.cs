// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Implements a title.
    /// </summary>
    [UnityRestricted]
    internal interface IHasTitle
    {
        /// <summary>
        /// Title of the declaration model.
        /// </summary>
        string Title { get; set; }
    }

    /// <summary>
    /// Interface for elements that can track their progression.
    /// </summary>
    [UnityRestricted]
    internal interface IHasProgress
    {
        /// <summary>
        /// The progress value.
        /// </summary>
        /// <remarks>No units are implied. The model and the UI must agree on the meaning of the value. Percent (0-100) are often used.</remarks>
        int Progress { get; }
    }

    /// <summary>
    /// Interface for elements that have an <see cref="ElementColor"/>.
    /// </summary>
    interface IHasElementColor
    {
        /// <summary>
        /// The element color.
        /// </summary>
        ElementColor ElementColor { get; }

        /// <summary>
        /// Sets the color of the element.
        /// </summary>
        /// <param name="color">The new color.</param>
        /// <remarks>Setting a color different from <see cref="DefaultColor"/> sets <see cref="ElementColor.HasUserColor"/> to true. Setting it to the default color sets it to false.</remarks>
        void SetColor(Color color);

        /// <summary>
        /// Default <see cref="Color"/> to use when no user color is provided.
        /// </summary>
        Color DefaultColor { get; }

        /// <summary>
        /// Whether the color picker must show the alpha editing controls.
        /// </summary>
        bool UseColorAlpha { get; }
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    /// <summary>
    /// An element that can be collapsed.
    /// </summary>
    [UnityRestricted]
    internal interface ICollapsible
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
    [UnityRestricted]
    internal interface IResizable
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
    [UnityRestricted]
    internal interface IMovable
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
    [UnityRestricted]
    internal interface IRenamable
    {
        /// <summary>
        /// Change the name of the model.
        /// </summary>
        /// <param name="name">New name to give to the model.</param>
        void Rename(string name);
    }

    /// <summary>
    /// An element that can be animated.
    /// </summary>
    [UnityRestricted]
    internal interface IAnimatable
    {
        /// <summary>
        /// Whether the element is animating. Set to <see langword="true"/> to start playback at the current
        /// <see cref="AnimationSpeed"/>; set to <see langword="false"/> to stop.
        /// </summary>
        bool IsAnimating { get; set; }

        /// <summary>
        /// The playback speed of the animation on the element.
        /// </summary>
        float AnimationSpeed { get; set; }
    }
}
