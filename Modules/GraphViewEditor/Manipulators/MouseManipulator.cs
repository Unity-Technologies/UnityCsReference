// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    abstract class MouseManipulator : Manipulator
    {
        public List<ManipulatorActivationFilter> activators { get; private set; }
        private ManipulatorActivationFilter m_CurrentActivator;

        protected MouseManipulator()
        {
            activators = new List<ManipulatorActivationFilter>();
        }

        protected bool CanStartManipulation(IMouseEvent e)
        {
            foreach (var activator in activators)
            {
                if (activator.Matches(e))
                {
                    m_CurrentActivator = activator;
                    return true;
                }
            }

            return false;
        }

        protected bool CanStopManipulation(IMouseEvent e)
        {
            return (MouseButton)e.button == m_CurrentActivator.button;
        }
    }
}
