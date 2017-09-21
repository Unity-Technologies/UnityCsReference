// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public class Toggle : VisualElement
    {
        Action clickEvent;

        public bool on
        {
            get
            {
                return (pseudoStates & PseudoStates.Checked) == PseudoStates.Checked;
            }
            set
            {
                if (value)
                {
                    pseudoStates |= PseudoStates.Checked;
                }
                else
                {
                    pseudoStates &= ~PseudoStates.Checked;
                }
            }
        }

        public Toggle(System.Action clickEvent)
        {
            this.clickEvent = clickEvent;

            // Click-once behaviour
            this.AddManipulator(new Clickable(OnClick));
        }

        public void OnToggle(Action clickEvent)
        {
            this.clickEvent = clickEvent;
        }

        private void OnClick()
        {
            on = !on;
            if (clickEvent != null)
                clickEvent();
        }
    }
}
