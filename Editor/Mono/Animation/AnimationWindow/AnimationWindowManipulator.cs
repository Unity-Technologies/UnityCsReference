// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class AnimationWindowManipulator
    {
        public delegate bool OnStartDragDelegate(AnimationWindowManipulator manipulator, Event evt);
        public delegate bool OnDragDelegate(AnimationWindowManipulator manipulator, Event evt);
        public delegate bool OnEndDragDelegate(AnimationWindowManipulator manipulator, Event evt);

        public OnStartDragDelegate onStartDrag;
        public OnDragDelegate onDrag;
        public OnEndDragDelegate onEndDrag;

        public Rect rect;
        public int controlID;

        public AnimationWindowManipulator()
        {
            // NoOps...
            onStartDrag += (AnimationWindowManipulator manipulator, Event evt) => { return false; };
            onDrag += (AnimationWindowManipulator manipulator, Event evt) => { return false; };
            onEndDrag += (AnimationWindowManipulator manipulator, Event evt) => { return false; };
        }

        public virtual void HandleEvents()
        {
            controlID = GUIUtility.GetControlID(FocusType.Passive);

            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(controlID);

            bool handled = false;
            switch (eventType)
            {
                case EventType.MouseDown:
                    if (evt.button == 0)
                    {
                        handled = onStartDrag(this, evt);

                        if (handled)
                            GUIUtility.hotControl = controlID;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        handled = onDrag(this, evt);
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        handled = onEndDrag(this, evt);
                        GUIUtility.hotControl = 0;
                    }
                    break;
            }

            if (handled)
                evt.Use();
        }

        public virtual void IgnoreEvents()
        {
            GUIUtility.GetControlID(FocusType.Passive);
        }
    }

    internal class AreaManipulator : AnimationWindowManipulator
    {
        private GUIStyle m_Style;
        private MouseCursor m_Cursor;

        public AreaManipulator(GUIStyle style, MouseCursor cursor)
        {
            m_Style = style;
            m_Cursor = cursor;
        }

        public AreaManipulator(GUIStyle style)
        {
            m_Style = style;
            m_Cursor = MouseCursor.Arrow;
        }

        public void OnGUI(Rect widgetRect)
        {
            if (m_Style == null)
                return;

            rect = widgetRect;

            if (Mathf.Approximately(widgetRect.width * widgetRect.height, 0f))
                return;

            GUI.Label(widgetRect, GUIContent.none, m_Style);

            if (GUIUtility.hotControl == 0 && m_Cursor != MouseCursor.Arrow)
            {
                EditorGUIUtility.AddCursorRect(widgetRect, m_Cursor);
            }
            else if (GUIUtility.hotControl == controlID)
            {
                Vector2 mousePosition = Event.current.mousePosition;
                EditorGUIUtility.AddCursorRect(new Rect(mousePosition.x - 10, mousePosition.y - 10, 20, 20), m_Cursor);
            }
        }
    }
}
