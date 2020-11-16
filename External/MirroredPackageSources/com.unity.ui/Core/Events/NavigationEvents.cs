using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for all navigation events.
    /// </summary>
    public interface INavigationEvent
    {
    }

    /// <summary>
    /// Navigation events abstract base class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class NavigationEventBase<T> : EventBase<T>, INavigationEvent where T : NavigationEventBase<T>, new()
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        protected NavigationEventBase()
        {
            LocalInit();
        }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown | EventPropagation.Cancellable;
            propagateToIMGUI = false;
        }
    }

    /// <summary>
    /// Event typically sent when the user presses the D-pad, moves a joystick or presses the arrow keys.
    /// </summary>
    public class NavigationMoveEvent : NavigationEventBase<NavigationMoveEvent>
    {
        /// <summary>
        /// Move event direction.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// No specific direction.
            /// </summary>
            None,
            /// <summary>
            /// Lefto.
            /// </summary>
            Left,
            /// <summary>
            /// Upo.
            /// </summary>
            Up,
            /// <summary>
            /// Righto.
            /// </summary>
            Right,
            /// <summary>
            /// Downo.
            /// </summary>
            Down
        }

        internal static Direction DetermineMoveDirection(float x, float y, float deadZone = 0.6f)
        {
            // if vector is too small... just return
            if (new Vector2(x, y).sqrMagnitude < deadZone * deadZone)
                return Direction.None;

            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                if (x > 0)
                    return Direction.Right;
                return Direction.Left;
            }
            else
            {
                if (y > 0)
                    return Direction.Up;
                return Direction.Down;
            }
        }

        /// <summary>
        /// The direction of the navigation.
        /// </summary>
        public Direction direction { get; private set; }

        /// <summary>
        /// The move vector.
        /// </summary>
        public Vector2 move { get; private set; }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values.
        /// Use this function instead of creating new events.
        /// Events obtained from this method should be released back to the pool using Dispose().
        /// </summary>
        /// <param name="moveVector">The move vector.</param>
        /// <returns>An initialized navigation event.</returns>
        public static NavigationMoveEvent GetPooled(Vector2 moveVector)
        {
            NavigationMoveEvent e = GetPooled();
            e.direction = DetermineMoveDirection(moveVector.x, moveVector.y);
            e.move = moveVector;
            return e;
        }

        /// <summary>
        /// Initialize the event members.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            direction = Direction.None;
            move = Vector2.zero;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public NavigationMoveEvent()
        {
            Init();
        }
    }

    // This event is not used in current UIElementsEventSystem implementation but is used by the
    // Input System integration branch.
    // Ideally this would be made public and replace the KeyDownEvent with KeyCode.Tab in UIElementsEventSystem too.
    /// <summary>
    /// Event typically sent when the user presses L1/R1 or presses tab or shift+tab key.
    /// </summary>
    internal class NavigationTabEvent : NavigationEventBase<NavigationTabEvent>
    {
        /// <summary>
        /// Tab event direction.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// No specific direction.
            /// </summary>
            None,
            /// <summary>
            /// Forwards, toward next element.
            /// </summary>
            Next,
            /// <summary>
            /// Backwards, toward previous element.
            /// </summary>
            Previous,
        }

        /// <summary>
        /// The direction of the navigation
        /// </summary>
        public Direction direction { get; private set; }

        internal static Direction DetermineMoveDirection(int moveValue)
        {
            return moveValue > 0 ? Direction.Next : moveValue < 0 ? Direction.Previous : Direction.None;
        }

        public static NavigationTabEvent GetPooled(int moveValue)
        {
            NavigationTabEvent e = GetPooled();
            e.direction = DetermineMoveDirection(moveValue);
            return e;
        }

        /// <summary>
        /// Initialize the event members.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            direction = Direction.None;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public NavigationTabEvent()
        {
            Init();
        }
    }

    /// <summary>
    /// Event sent when the user presses the cancel button.
    /// </summary>
    public class NavigationCancelEvent : NavigationEventBase<NavigationCancelEvent>
    {
    }

    /// <summary>
    /// Event sent when the user presses the submit button.
    /// </summary>
    public class NavigationSubmitEvent : NavigationEventBase<NavigationSubmitEvent>
    {
    }
}
