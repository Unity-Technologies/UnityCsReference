using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Used by manipulators to match events against their requirements.
    /// </summary>
    /// <example>
    /// <code>
    /// public class ClickableTest
    /// {
    ///     public void CreateClickable()
    ///     {
    ///         var clickable = new Clickable(() => { Debug.Log("Clicked!"); });
    ///         clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    ///         clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, clickCount = 2, modifiers = EventModifiers.Control });
    ///     }
    /// }
    /// </code>
    /// </example>
    public struct ManipulatorActivationFilter : IEquatable<ManipulatorActivationFilter>
    {
        /// <summary>
        /// The button that activates the manipulation.
        /// </summary>
        public MouseButton button { get; set; }
        /// <summary>
        /// Any modifier keys (ie. ctrl, alt, ...) that are needed to activate the manipulation.
        /// </summary>
        public EventModifiers modifiers { get; set; }
        /// <summary>
        /// Number of mouse clicks required to activate the manipulator.
        /// </summary>
        public int clickCount { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ManipulatorActivationFilter && Equals((ManipulatorActivationFilter)obj);
        }

        /// <undoc/>
        public bool Equals(ManipulatorActivationFilter other)
        {
            return button == other.button &&
                modifiers == other.modifiers &&
                clickCount == other.clickCount;
        }

        public override int GetHashCode()
        {
            var hashCode = 390957112;
            hashCode = hashCode * -1521134295 + button.GetHashCode();
            hashCode = hashCode * -1521134295 + modifiers.GetHashCode();
            hashCode = hashCode * -1521134295 + clickCount.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Checks whether the current mouse event satisfies the activation requirements.
        /// </summary>
        /// <param name="e">The mouse event.</param>
        /// <returns>True if the event matches the requirements. False otherwise.</returns>
        public bool Matches(IMouseEvent e)
        {
            if (e == null)
            {
                return false;
            }
            // Default clickCount field value is 0 since we're in a struct -- this case is covered if the user
            // did not explicitly set clickCount
            var minClickCount = (clickCount == 0 || (e.clickCount >= clickCount));
            return button == (MouseButton)e.button && HasModifiers(e) && minClickCount;
        }

        private bool HasModifiers(IMouseEvent e)
        {
            if (e == null)
                return false;

            return MatchModifiers(e.altKey, e.ctrlKey, e.shiftKey, e.commandKey);
        }

        /// <summary>
        /// Checks whether the current mouse event satisfies the activation requirements.
        /// </summary>
        /// <param name="e">The mouse event.</param>
        /// <returns>True if the event matches the requirements. False otherwise.</returns>
        public bool Matches(IPointerEvent e)
        {
            if (e == null)
            {
                return false;
            }
            // Default clickCount field value is 0 since we're in a struct -- this case is covered if the user
            // did not explicitly set clickCount
            var minClickCount = (clickCount == 0 || e.clickCount >= clickCount);
            return button == (MouseButton)e.button && HasModifiers(e) && minClickCount;
        }

        private bool HasModifiers(IPointerEvent e)
        {
            if (e == null)
                return false;

            return MatchModifiers(e.altKey, e.ctrlKey, e.shiftKey, e.commandKey);
        }

        private bool MatchModifiers(bool alt, bool ctrl, bool shift, bool command)
        {
            if ((modifiers & EventModifiers.Alt) != 0 && !alt ||
                (modifiers & EventModifiers.Alt) == 0 && alt)
            {
                return false;
            }
            if ((modifiers & EventModifiers.Control) != 0 && !ctrl ||
                (modifiers & EventModifiers.Control) == 0 && ctrl)
            {
                return false;
            }

            if ((modifiers & EventModifiers.Shift) != 0 && !shift ||
                (modifiers & EventModifiers.Shift) == 0 && shift)
            {
                return false;
            }
            return ((modifiers & EventModifiers.Command) == 0 || command) &&
                ((modifiers & EventModifiers.Command) != 0 || !command);
        }

        /// <undoc/>
        public static bool operator==(ManipulatorActivationFilter filter1, ManipulatorActivationFilter filter2)
        {
            return filter1.Equals(filter2);
        }

        /// <undoc/>
        public static bool operator!=(ManipulatorActivationFilter filter1, ManipulatorActivationFilter filter2)
        {
            return !(filter1 == filter2);
        }
    }
}
