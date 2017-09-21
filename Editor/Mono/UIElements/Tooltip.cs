// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.Experimental.UIElements
{
    class TooltipEvent : EventBase<TooltipEvent>, IPropagatableEvent
    {
        public string tooltip { get; set; }
        public Rect rect { get; set; }
    }

    internal static class TooltipExtension
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
                    var tooltipEvent = TooltipEvent.GetPooled();
                    tooltipEvent.target = target;
                    tooltipEvent.tooltip = null;
                    tooltipEvent.rect = Rect.zero;
                    view.visualTree.panel.dispatcher.DispatchEvent(tooltipEvent, panel);

                    if (!string.IsNullOrEmpty(tooltipEvent.tooltip))
                    {
                        Rect rect = tooltipEvent.rect;
                        rect.position += view.screenPosition.position; //SetMouseTooltip expects Screen relative coordinates.

                        GUIStyle.SetMouseTooltip(tooltipEvent.tooltip, rect);
                    }
                }
            }
        }

        internal static void AddTooltip(this VisualElement e, string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip))
            {
                RemoveTooltip(e);
                return;
            }
            TooltipElement tooltipElement = e.Query().Children<TooltipElement>();

            if (tooltipElement == null)
                tooltipElement = new TooltipElement();

            tooltipElement.style.positionType = PositionType.Absolute;
            tooltipElement.style.positionLeft = tooltipElement.style.positionRight = tooltipElement.style.positionTop = tooltipElement.style.positionBottom = 0;

            tooltipElement.tooltip = tooltip;

            e.Add(tooltipElement);
        }

        internal static void RemoveTooltip(this VisualElement e)
        {
            TooltipElement tooltipElement = e.Query().Children<TooltipElement>();
            if (tooltipElement != null)
                e.Remove(tooltipElement);
        }
    }


    class TooltipElement : VisualElement
    {
        public string tooltip { get; set; }

        public TooltipElement()
        {
            RegisterCallback<TooltipEvent>(OnTooltip);
        }

        void OnTooltip(TooltipEvent e)
        {
            e.tooltip = tooltip;
            e.rect = worldBound;
            e.StopPropagation();
        }
    }
}
