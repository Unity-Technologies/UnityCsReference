// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.RMGUI
{
    public interface IManipulator : IEventHandler
    {
        VisualElement target { get; set; }
    }

    // helper
    public class Manipulator : IManipulator
    {
        public VisualElement target { get; set; }
        public EventPhase phaseInterest { get; set; }
        public IPanel panel
        {
            get
            {
                if (target != null)
                    return target.panel;
                return null;
            }
        }

        public Manipulator()
        {
            phaseInterest = EventPhase.BubbleUp;
        }

        public virtual EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            return EventPropagation.Continue;
        }

        public virtual void OnLostCapture()
        {
        }

        public virtual void OnLostKeyboardFocus()
        {
        }
    }
}
