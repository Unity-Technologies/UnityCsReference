using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for Manipulator objects.
    /// </summary>
    public interface IManipulator
    {
        /// <summary>
        /// VisualElement being manipulated.
        /// </summary>
        VisualElement target { get; set; }
    }

    /// <summary>
    /// Base class for all Manipulator implementations.
    /// </summary>
    public abstract class Manipulator : IManipulator
    {
        /// <summary>
        /// Called to register event callbacks on the target element.
        /// </summary>
        protected abstract void RegisterCallbacksOnTarget();
        /// <summary>
        /// Called to unregister event callbacks from the target element.
        /// </summary>
        protected abstract void UnregisterCallbacksFromTarget();

        VisualElement m_Target;
        /// <summary>
        /// VisualElement being manipulated.
        /// </summary>
        public VisualElement target
        {
            get { return m_Target; }
            set
            {
                if (target != null)
                {
                    UnregisterCallbacksFromTarget();
                }
                m_Target = value;
                if (target != null)
                {
                    RegisterCallbacksOnTarget();
                }
            }
        }
    }
}
