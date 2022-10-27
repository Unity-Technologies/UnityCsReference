// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    [Flags]
    enum SnapDirection
    {
        SnapNone = 0,
        SnapX = 1,
        SnapY = 2
    }

    /// <summary>
    /// Class that defines how an element position is constrained when being moved by the <see cref="SelectionDragger"/>.
    /// </summary>
    abstract class SnapStrategy
    {
        protected const float k_DefaultSnapDistance = 8.0f;

        /// <summary>
        /// Whether this strategy is enabled.
        /// </summary>
        /// <remarks>If the strategy is not enabled, it will not get the chance to snap elements.</remarks>
        public virtual bool Enabled { get; set; }

        /// <summary>
        /// The snap distance.
        /// </summary>
        protected float SnapDistance { get; }

        /// <summary>
        /// Whether this strategy is temporarily deactivated.
        /// </summary>
        protected bool IsPaused { get; private set; }

        /// <summary>
        ///  Whether this strategy is active.
        /// </summary>
        /// <remarks>The strategy is active if it is currently participating in the snapping of an element.</remarks>
        protected bool IsActive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapStrategy"/> class.
        /// </summary>
        protected SnapStrategy()
        {
            SnapDistance = k_DefaultSnapDistance;
        }

        /// <summary>
        /// Begins a snapping operation. Called when the <paramref name="selectedElement"/> starts moving.
        /// </summary>
        /// <param name="selectedElement">The element to snap.</param>
        public virtual void BeginSnap(GraphElement selectedElement)
        {
            if (IsActive)
            {
                throw new InvalidOperationException($"SnapStrategy.BeginSnap: {GetType()} already active. Call EndSnap() first.");
            }
            IsActive = true;
        }

        /// <summary>
        /// Computes a suggested snapping rectangle and snapping offset.
        /// </summary>
        /// <param name="snapDirection">Whether to snap in X and Y directions.</param>
        /// <param name="sourceRect">The initial position and dimensions of the element to snap.</param>
        /// <param name="selectedElement">The element to snap.</param>
        /// <returns>The computed snapped position.</returns>
        public Vector2 GetSnappedPosition(out SnapDirection snapDirection, Rect sourceRect, GraphElement selectedElement)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException($"SnapStrategy.GetSnappedRect: {GetType()} not active. Call BeginSnap() first.");
            }

            if (IsPaused)
            {
                snapDirection = SnapDirection.SnapNone;
                return sourceRect.position;
            }

            return ComputeSnappedPosition(out snapDirection, sourceRect, selectedElement);
        }

        /// <summary>
        /// Computes the suggested snapping rectangle and snapping offset.
        /// </summary>
        /// <param name="snapDirection">Whether to snap in X and Y directions.</param>
        /// <param name="sourceRect">The initial position and dimensions of the element to snap.</param>
        /// <param name="selectedElement">The element to snap.</param>
        /// <returns>The computed snapping position.</returns>
        protected abstract Vector2 ComputeSnappedPosition(out SnapDirection snapDirection, Rect sourceRect, GraphElement selectedElement);

        /// <summary>
        /// Ends the snapping operation.
        /// </summary>
        public virtual void EndSnap()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException($"SnapStrategy.EndSnap: {GetType()} already inactive. Call BeginSnap() first.");
            }
            IsActive = false;
        }

        /// <summary>
        /// Temporarily deactivate this strategy.
        /// </summary>
        /// <remarks>This is called when the shift key is pressed, indicating that the user wants to move elements without any snapping.</remarks>
        /// <param name="isPaused">True is the strategy should be paused.</param>
        public virtual void PauseSnap(bool isPaused)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException($"SnapStrategy.PauseSnap: {GetType()} is not active. Call BeginSnap() first.");
            }

            IsPaused = isPaused;
        }
    }
}
