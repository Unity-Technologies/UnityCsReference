// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class ClickSelector : MouseManipulator
    {
        public ClickSelector()
        {
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.RightMouse});
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse, modifiers = EventModifiers.Control});
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift});
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (!(e.currentTarget is ISelectable))
            {
                return;
            }

            if (CanStartManipulation(e))
            {
                if (!(target as ISelectable).HitTest(e.localMousePosition))
                {
                    return;
                }
                var ge = e.currentTarget as GraphElement;
                if (ge != null)
                {
                    VisualElement c = ge.shadow.parent;
                    while (c != null && !(c is GraphView))
                    {
                        c = c.shadow.parent;
                    }

                    var gv = c as GraphView;
                    if (ge.IsSelected(gv) && e.ctrlKey)
                    {
                        ge.Unselect(gv);
                    }
                    else
                    {
                        ge.Select(gv, e.shiftKey || e.ctrlKey);
                    }
                }
            }
        }
    }
}
