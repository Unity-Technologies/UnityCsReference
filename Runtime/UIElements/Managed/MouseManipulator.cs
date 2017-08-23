// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public abstract class MouseManipulator : Manipulator
    {
        public List<ManipulatorActivationFilter> activators { get; private set; }
        private ManipulatorActivationFilter m_currentActivator;

        public MouseManipulator()
        {
            activators = new List<ManipulatorActivationFilter>();
        }

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

        protected bool CanStopManipulation(IMouseEvent e)
        {
            return ((MouseButton)e.button == m_currentActivator.button) && target.HasCapture();
        }
    }
}
