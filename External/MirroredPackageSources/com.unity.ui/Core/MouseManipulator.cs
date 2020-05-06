using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// MouseManipulators have a list of activation filters.
    /// </summary>
    public abstract class MouseManipulator : Manipulator
    {
        /// <summary>
        /// List of Activationfilters.
        /// </summary>
        public List<ManipulatorActivationFilter> activators { get; private set; }
        private ManipulatorActivationFilter m_currentActivator;

        protected MouseManipulator()
        {
            activators = new List<ManipulatorActivationFilter>();
        }

        /// <summary>
        /// Checks whether MouseEvent satisfies all of the ManipulatorActivationFilter requirements.
        /// </summary>
        /// <param name="e">The MouseEvent to validate.</param>
        /// <returns>True if the event satisfies the requirements. False otherwise.</returns>
        protected bool CanStartManipulation(IMouseEvent e)
        {
            foreach (var activator in activators)
            {
                if (activator.Matches(e))
                {
                    m_currentActivator = activator;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the MouseEvent is related to this Manipulator.
        /// </summary>
        /// <param name="e">MouseEvent to validate.</param>
        /// <returns>True if MouseEvent uses the current activator button. False otherwise.</returns>
        protected bool CanStopManipulation(IMouseEvent e)
        {
            if (e == null)
            {
                return false;
            }

            return ((MouseButton)e.button == m_currentActivator.button);
        }
    }
}
