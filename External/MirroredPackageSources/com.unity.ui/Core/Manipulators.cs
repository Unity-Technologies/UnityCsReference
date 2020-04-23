using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public interface IManipulator
    {
        VisualElement target { get; set; }
    }

    public abstract class Manipulator : IManipulator
    {
        protected abstract void RegisterCallbacksOnTarget();
        protected abstract void UnregisterCallbacksFromTarget();

        VisualElement m_Target;
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
