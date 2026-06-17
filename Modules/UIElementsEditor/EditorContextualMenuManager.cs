// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    class EditorContextualMenuManager : ContextualMenuManager
    {
        static EditorContextualMenuManager()
        {
            EditorUtility.onResetMouseDown += () =>
            {
                // UUM-73201: Reset mouse buttons when the ContextualMenu is called from IMGUI.
                ResetPointerDown(PointerId.mousePointerId);

                // UUM-138133: A pen or touch can also open the native menu, which consumes the matching pointer-up and
                // would otherwise leave the pointer stuck in a pressed state (keeping controls like ScrollView
                // drag-scroll active).
                ResetPointerDown(PointerId.penPointerIdBase);
                ResetPointerDown(PointerId.touchPointerIdBase);
            };
        }

        public override void DisplayMenuIfEventMatches(EventBase evt, IEventHandler eventHandler)
        {
            if (CheckIfEventMatches(evt))
            {
                DisplayMenu(evt, eventHandler);
                evt.StopPropagation();
            }
        }

        internal override bool CheckIfEventMatches(EventBase evt)
        {
            if (UIElementsUtility.isOSXContextualMenuPlatform)
            {
                if (evt.eventTypeId == PointerDownEvent.TypeId() ||
                    evt.eventTypeId == PointerMoveEvent.TypeId() && ((PointerMoveEvent)evt).isPointerDown)
                {
                    IPointerEvent e = (IPointerEvent) evt;

                    if (e.button == (int)MouseButton.RightMouse ||
                        (e.button == (int)MouseButton.LeftMouse && e.modifiers == EventModifiers.Control))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (evt.eventTypeId == PointerUpEvent.TypeId() ||
                    evt.eventTypeId == PointerMoveEvent.TypeId() && ((PointerMoveEvent)evt).isPointerUp)
                {
                    IPointerEvent e = (IPointerEvent) evt;
                    if (e.button == (int)MouseButton.RightMouse)
                    {
                        return true;
                    }
                }
            }

            if (evt.eventTypeId == KeyUpEvent.TypeId())
            {
                KeyUpEvent e = evt as KeyUpEvent;
                if (e.keyCode == KeyCode.Menu)
                {
                    return true;
                }
            }

            return false;
        }

        protected internal override void DoDisplayMenu(DropdownMenu menu, EventBase triggerEvent)
        {
            // Force repaint on the panel because they won't get another chance when the menu is up
            if (menu.repaintPanelBeforeDisplay && triggerEvent.elementTarget?.elementPanel?.ownerObject is GUIView view)
            {
                view.RepaintImmediately();
            }

            menu.DoDisplayEditorMenu(triggerEvent);
        }
    }
}
