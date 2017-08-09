// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using System.Collections.Generic;

// NOTE:
// This file should only contain internal functions of the EditorGUI class
//

namespace UnityEditor
{
    public sealed partial class EditorGUI
    {
        static int s_DropdownButtonHash = "DropdownButton".GetHashCode();
        static int s_MouseDeltaReaderHash = "MouseDeltaReader".GetHashCode();

        internal static bool Button(Rect position, GUIContent content)
        {
            return Button(position, content, EditorStyles.miniButton);
        }

        // We need an EditorGUI.Button that only reacts to left mouse button (GUI.Button reacts to all mouse buttons), so we
        // can handle context click events for button areas etc.
        internal static bool Button(Rect position, GUIContent content, GUIStyle style)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.MouseDown:
                case EventType.MouseUp:
                    if (evt.button != 0)
                        return false; // ignore all input from other buttons than the left mouse button
                    break;
            }

            return GUI.Button(position, content, style);
        }

        // Button used for the icon selector where an icon can be selected by pressing and dragging the
        // mouse cursor around to select different icons
        internal static bool IconButton(int id, Rect position, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            switch (Event.current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    // If the mouse is inside the button, we say that we're the hot control
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        Event.current.Use();
                        return true;
                    }
                    return false;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;

                        // If we got the mousedown, the mouseup is ours as well
                        // (no matter if the click was in the button or not)
                        Event.current.Use();

                        // But we only return true if the button was actually clicked
                        return position.Contains(Event.current.mousePosition);
                    }
                    return false;
                case EventType.MouseDrag:
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        Event.current.Use();
                        return true;
                    }
                    break;
                case EventType.Repaint:
                    style.Draw(position, content, id);
                    break;
            }
            return false;
        }

        internal static float WidthResizer(Rect position, float width, float minWidth, float maxWidth)
        {
            bool hasControl;
            return Resizer.Resize(position, width, minWidth, maxWidth, true, out hasControl);
        }

        internal static float WidthResizer(Rect position, float width, float minWidth, float maxWidth, out bool hasControl)
        {
            return Resizer.Resize(position, width, minWidth, maxWidth, true, out hasControl);
        }

        internal static float HeightResizer(Rect position, float height, float minHeight, float maxHeight)
        {
            bool hasControl;
            return Resizer.Resize(position, height, minHeight, maxHeight, false, out hasControl);
        }

        internal static float HeightResizer(Rect position, float height, float minHeight, float maxHeight, out bool hasControl)
        {
            return Resizer.Resize(position, height, minHeight, maxHeight, false, out hasControl);
        }

        static class Resizer
        {
            static float s_StartSize;
            static Vector2 s_MouseDeltaReaderStartPos;
            internal static float Resize(Rect position, float size, float minSize, float maxSize, bool horizontal, out bool hasControl)
            {
                int id = EditorGUIUtility.GetControlID(s_MouseDeltaReaderHash, FocusType.Passive, position);
                Event evt = Event.current;
                switch (evt.GetTypeForControl(id))
                {
                    case EventType.MouseDown:
                        if (GUIUtility.hotControl == 0 && position.Contains(evt.mousePosition) && evt.button == 0)
                        {
                            GUIUtility.hotControl = id;
                            GUIUtility.keyboardControl = 0;
                            s_MouseDeltaReaderStartPos = GUIClip.Unclip(evt.mousePosition); // We unclip to screenspace to prevent being affected by scrollviews
                            s_StartSize = size;
                            evt.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == id)
                        {
                            evt.Use();
                            Vector2 screenPos = GUIClip.Unclip(evt.mousePosition);  // We unclip to screenspace to prevent being affected by scrollviews
                            float delta = horizontal ? (screenPos - s_MouseDeltaReaderStartPos).x : (screenPos - s_MouseDeltaReaderStartPos).y;
                            float newSize = s_StartSize + delta;
                            if (newSize >= minSize && newSize <= maxSize)
                                size = newSize;
                            else
                                size = Mathf.Clamp(newSize, minSize, maxSize);
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id && evt.button == 0)
                        {
                            GUIUtility.hotControl = 0;
                            evt.Use();
                        }
                        break;
                    case EventType.Repaint:
                        var cursor = horizontal ? MouseCursor.SplitResizeLeftRight : MouseCursor.SplitResizeUpDown;
                        EditorGUIUtility.AddCursorRect(position, cursor, id);
                        break;
                }

                hasControl = GUIUtility.hotControl == id;
                return size;
            }
        }


        // Get mouse delta values in different situations when click-dragging
        static Vector2 s_MouseDeltaReaderLastPos;
        internal static Vector2 MouseDeltaReader(Rect position, bool activated)
        {
            int id = EditorGUIUtility.GetControlID(s_MouseDeltaReaderHash, FocusType.Passive, position);
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (activated && GUIUtility.hotControl == 0 && position.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        GUIUtility.keyboardControl = 0;
                        s_MouseDeltaReaderLastPos = GUIClip.Unclip(evt.mousePosition); // We unclip to screenspace to prevent being affected by scrollviews
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        Vector2 screenPos = GUIClip.Unclip(evt.mousePosition);  // We unclip to screenspace to prevent being affected by scrollviews
                        Vector2 delta = (screenPos - s_MouseDeltaReaderLastPos);
                        s_MouseDeltaReaderLastPos = screenPos;
                        evt.Use();
                        return delta;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && evt.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;
            }
            return Vector2.zero;
        }

        // Shows an active button and a triangle button on the right, which expands the dropdown list
        // Returns true if button was activated, returns false if the the dropdown button was activated or the button was not clicked.
        internal static bool ButtonWithDropdownList(string buttonName, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            var content = EditorGUIUtility.TempContent(buttonName);
            return ButtonWithDropdownList(content, buttonNames, callback, options);
        }

        // Shows an active button and a triangle button on the right, which expands the dropdown list
        // Returns true if button was activated, returns false if the the dropdown button was activated or the button was not clicked.
        internal static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            var rect = GUILayoutUtility.GetRect(content, EditorStyles.dropDownList, options);

            var dropDownRect = rect;
            const float kDropDownButtonWidth = 20f;
            dropDownRect.xMin = dropDownRect.xMax - kDropDownButtonWidth;

            if (Event.current.type == EventType.MouseDown && dropDownRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                for (int i = 0; i != buttonNames.Length; i++)
                    menu.AddItem(new GUIContent(buttonNames[i]), false, callback, i);

                menu.DropDown(rect);
                Event.current.Use();

                return false;
            }

            return GUI.Button(rect, content, EditorStyles.dropDownList);
        }

        internal static void GameViewSizePopup(Rect buttonRect, GameViewSizeGroupType groupType, int selectedIndex, IGameViewSizeMenuUser gameView, GUIStyle guiStyle)
        {
            var group = GameViewSizes.instance.GetGroup(groupType);
            var text = "";
            if (selectedIndex >= 0 && selectedIndex < group.GetTotalCount())
                text = group.GetGameViewSize(selectedIndex).displayText;

            if (EditorGUI.DropdownButton(buttonRect, GUIContent.Temp(text), FocusType.Passive, guiStyle))
            {
                var menuData = new GameViewSizesMenuItemProvider(groupType);
                var flexibleMenu = new GameViewSizeMenu(menuData, selectedIndex, new GameViewSizesMenuModifyItemUI(), gameView);
                PopupWindow.Show(buttonRect, flexibleMenu);
            }
        }

        public static void DrawRect(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color orgColor = GUI.color;
            GUI.color = GUI.color * color;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = orgColor;
        }

        internal static void DrawDelimiterLine(Rect rect)
        {
            DrawRect(rect, kSplitLineSkinnedColor.color);
        }

        internal static void DrawOutline(Rect rect, float size, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color orgColor = GUI.color;
            GUI.color = GUI.color * color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - size, rect.width, size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - size, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);

            GUI.color = orgColor;
        }
    }

    internal struct PropertyGUIData
    {
        public SerializedProperty property;
        public Rect totalPosition;
        public bool wasBoldDefaultFont;
        public bool wasEnabled;
        public Color color;
        public PropertyGUIData(SerializedProperty property, Rect totalPosition, bool wasBoldDefaultFont, bool wasEnabled, Color color)
        {
            this.property = property;
            this.totalPosition = totalPosition;
            this.wasBoldDefaultFont = wasBoldDefaultFont;
            this.wasEnabled = wasEnabled;
            this.color = color;
        }
    }

    internal class DebugUtils
    {
        internal static string ListToString<T>(IEnumerable<T>  list)
        {
            if (list == null)
                return "[null list]";

            string r = "[";
            int count = 0;
            foreach (T item in list)
            {
                if (count != 0)
                    r += ", ";
                if (item != null)
                    r += item.ToString();
                else
                    r += "'null'";
                count++;
            }
            r += "]";

            if (count == 0)
                return "[empty list]";

            return "(" + count + ") " + r;
        }
    }
}
