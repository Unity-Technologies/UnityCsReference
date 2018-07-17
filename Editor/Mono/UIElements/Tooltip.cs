// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.Experimental.UIElements
{
    static class Tooltip
    {
        [RequiredByNativeCode]
        internal static void SetTooltip(float mouseX, float mouseY)
        {
            //mouseX,mouseY are screen relative.
            GUIView view = GUIView.mouseOverView;
            if (view != null && view.visualTree != null && view.visualTree.panel != null)
            {
                var panel = view.visualTree.panel;

                // Pick expect view relative coordinates.
                VisualElement target = panel.Pick(new Vector2(mouseX, mouseY) - view.screenPosition.position);
                if (target != null)
                {
                    using (var tooltipEvent = TooltipEvent.GetPooled())
                    {
                        tooltipEvent.target = target;
                        tooltipEvent.tooltip = null;
                        tooltipEvent.rect = Rect.zero;
                        target.SendEvent(tooltipEvent);

                        if (!string.IsNullOrEmpty(tooltipEvent.tooltip) && !tooltipEvent.isDefaultPrevented)
                        {
                            Rect rect = tooltipEvent.rect;
                            rect.position += view.screenPosition.position; //SetMouseTooltip expects Screen relative coordinates.

                            GUIStyle.SetMouseTooltip(tooltipEvent.tooltip, rect);
                        }
                    }
                }
            }
        }
    }
}
