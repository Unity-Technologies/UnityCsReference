// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.InputForUI
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal struct NavigationEvent : IEventProperties
    {
        public enum Type
        {
            Move = 1,
            Submit = 2,
            Cancel = 3
        }

        /// <summary>
        /// Move event direction.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// No specific direction.
            /// </summary>
            None = 0,
            /// <summary>
            /// Left.
            /// </summary>
            Left = 1,
            /// <summary>
            /// Up.
            /// </summary>
            Up = 2,
            /// <summary>
            /// Right.
            /// </summary>
            Right = 3,
            /// <summary>
            /// Down.
            /// </summary>
            Down = 4,

            /// <summary>
            /// Forwards, toward next element.
            /// </summary>
            Next = 5,
            /// <summary>
            /// Backwards, toward previous element.
            /// </summary>
            Previous = 6,
        }

        public Type type;
        public Direction direction;
        public bool shouldBeUsed;

        public DiscreteTime timestamp { get; set; }
        public EventSource eventSource { get; set; }
        public uint playerId { get; set; }
        public EventModifiers eventModifiers { get; set; }

        public override string ToString()
        {
            return $"Navigation {type}" +
                   (type == Type.Move ? $" {direction}" : "") +
                   (eventSource != EventSource.Keyboard ? $" {eventSource}" : "");
        }

        /// <summary>
        /// Given an input movement, determine the best Direction.
        /// </summary>
        /// <param name="vec">Input movement as a vector.</param>
        /// <param name="deadZone">Dead zone.</param>
        internal static Direction DetermineMoveDirection(Vector2 vec, float deadZone = 0.6f)
        {
            // if vector is too small... just return
            if (vec.sqrMagnitude < deadZone * deadZone)
                return Direction.None;

            if (Mathf.Abs(vec.x) > Mathf.Abs(vec.y))
                return vec.x > 0 ? Direction.Right : Direction.Left;
            return vec.y > 0 ? Direction.Up : Direction.Down;
        }
    }
}
