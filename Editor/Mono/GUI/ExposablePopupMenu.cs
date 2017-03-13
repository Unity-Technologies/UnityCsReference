// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEditorInternal;
using UnityEngine;


namespace UnityEditor
{
    internal class ExposablePopupMenu
    {
        public class ItemData
        {
            public ItemData(GUIContent content, GUIStyle style, bool on, bool enabled, object userData)
            {
                m_GUIContent = content;
                m_Style = style;
                m_On = on;
                m_Enabled = enabled;
                m_UserData = userData;
            }

            public GUIContent m_GUIContent;
            public GUIStyle m_Style;
            public bool m_On;
            public bool m_Enabled;
            public object m_UserData;
            public float m_Width;
        }

        public class PopupButtonData
        {
            public PopupButtonData(GUIContent content, GUIStyle style)
            {
                m_GUIContent = content;
                m_Style = style;
            }

            public GUIContent m_GUIContent;
            public GUIStyle m_Style;
        }

        List<ItemData> m_Items;
        float m_WidthOfButtons;
        float m_ItemSpacing;
        PopupButtonData m_PopupButtonData;
        float m_WidthOfPopup;
        float m_MinWidthOfPopup;
        System.Action<ItemData> m_SelectionChangedCallback = null; // <userData>

        public void Init(List<ItemData> items, float itemSpacing, float minWidthOfPopup, PopupButtonData popupButtonData, System.Action<ItemData> selectionChangedCallback)
        {
            m_Items = items;
            m_ItemSpacing = itemSpacing;
            m_PopupButtonData = popupButtonData;
            m_SelectionChangedCallback = selectionChangedCallback;
            m_MinWidthOfPopup = minWidthOfPopup;
            CalcWidths();
        }

        public float OnGUI(Rect rect)
        {
            if (rect.width >= m_WidthOfButtons && rect.width > m_MinWidthOfPopup)
            {
                Rect buttonRect = rect;

                // Show as buttons
                foreach (var item in m_Items)
                {
                    buttonRect.width = item.m_Width;

                    EditorGUI.BeginChangeCheck();

                    using (new EditorGUI.DisabledScope(!item.m_Enabled))
                    {
                        GUI.Toggle(buttonRect, item.m_On, item.m_GUIContent, item.m_Style);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        SelectionChanged(item);
                        GUIUtility.ExitGUI(); // To make sure we can survive if m_Buttons are reallocated in the callback we exit gui
                    }

                    buttonRect.x += item.m_Width + m_ItemSpacing;
                }

                return m_WidthOfButtons;
            }
            else
            {
                // Show as popup
                if (m_WidthOfPopup < rect.width)
                    rect.width = m_WidthOfPopup;

                //if (GUI.Button (rect, m_PopupButtonData.m_GUIContent, m_PopupButtonData.m_Style))
                if (EditorGUI.DropdownButton(rect, m_PopupButtonData.m_GUIContent, FocusType.Passive, m_PopupButtonData.m_Style))
                    PopUpMenu.Show(rect, m_Items, this);

                return m_WidthOfPopup;
            }
        }

        void CalcWidths()
        {
            // Buttons
            m_WidthOfButtons = 0f;
            foreach (var item in m_Items)
            {
                item.m_Width = item.m_Style.CalcSize(item.m_GUIContent).x;
                m_WidthOfButtons += item.m_Width;
            }
            m_WidthOfButtons += (m_Items.Count - 1) * m_ItemSpacing;

            // Popup
            Vector2 size = m_PopupButtonData.m_Style.CalcSize(m_PopupButtonData.m_GUIContent);
            size.x += 3f; // more space between text and arrow
            m_WidthOfPopup = size.x;
        }

        void SelectionChanged(ItemData item)
        {
            if (m_SelectionChangedCallback != null)
                m_SelectionChangedCallback(item);
            else
                Debug.LogError("Callback is null");
        }

        internal class PopUpMenu
        {
            static List<ItemData> m_Data;
            static ExposablePopupMenu m_Caller;

            static internal void Show(Rect activatorRect, List<ItemData> buttonData, ExposablePopupMenu caller)
            {
                m_Data = buttonData;
                m_Caller = caller;

                GenericMenu menu = new GenericMenu();
                foreach (ItemData item in m_Data)
                    if (item.m_Enabled)
                        menu.AddItem(item.m_GUIContent, item.m_On, SelectionCallback, item);
                    else
                        menu.AddDisabledItem(item.m_GUIContent);

                menu.DropDown(activatorRect);
            }

            static void SelectionCallback(object userData)
            {
                ItemData item = (ItemData)userData;
                m_Caller.SelectionChanged(item);

                // Cleanup
                m_Caller = null;
                m_Data = null;
            }
        }
    }
} // end namespace UnityEditor
