// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class MouseManipulator : Manipulator
    {
        public List<ManipulatorActivationFilter> activators { get; private set; }
        private ManipulatorActivationFilter m_currentActivator;

        public MouseManipulator()
        {
            activators = new List<ManipulatorActivationFilter>();
        }

        protected bool CanStartManipulation(Event evt)
        {
            foreach (var activator in activators)
            {
                if (activator.Matches(evt))
                {
                    m_currentActivator = activator;
                    return true;
                }
            }

            return false;
        }

        protected bool CanStopManipulation(Event evt)
        {
            return ((MouseButton)evt.button == m_currentActivator.button) && this.HasCapture();
        }
    }
}
