namespace UnityEngine.UIElements
{
    /// <summary>
    /// PointerManipulators have a list of activation filters.
    /// </summary>
    public abstract class PointerManipulator : MouseManipulator
    {
        private int m_CurrentPointerId;

        /// <summary>
        /// Checks whether PointerEvent satisfies all of the ManipulatorActivationFilter requirements.
        /// </summary>
        /// <param name="e">The PointerEvent to validate.</param>
        /// <returns>True if the event satisfies the requirements. False otherwise.</returns>
        protected bool CanStartManipulation(IPointerEvent e)
        {
            foreach (var activator in activators)
            {
                if (activator.Matches(e))
                {
                    m_CurrentPointerId = e.pointerId;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the PointerEvent is related to this Manipulator.
        /// </summary>
        /// <param name="e">PointerEvent to validate.</param>
        /// <returns>True if PointerEvent uses the current activator button. False otherwise.</returns>
        protected bool CanStopManipulation(IPointerEvent e)
        {
            if (e == null)
                return false;

            return e.pointerId == m_CurrentPointerId;
        }
    }
}
