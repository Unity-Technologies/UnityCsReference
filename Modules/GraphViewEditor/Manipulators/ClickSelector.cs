// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class ClickSelector : MouseManipulator
    {
        public ClickSelector()
        {
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.RightMouse});
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown, Capture.Capture);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown, Capture.Capture);
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (!(e.currentTarget is ISelectable))
            {
                return;
            }

            if (CanStartManipulation(e))
            {
                var ge = e.currentTarget as GraphElement;
                if (ge != null)
                {
                    VisualElement c = ge.shadow.parent;
                    while (c != null && !(c is GraphView))
                    {
                        c = c.shadow.parent;
                    }

                    var gv = c as GraphView;
                    if (!ge.IsSelected(gv))
                    {
                        ge.Select(gv, e.shiftKey || e.ctrlKey);
                    }
                }
            }
        }
    }
}
