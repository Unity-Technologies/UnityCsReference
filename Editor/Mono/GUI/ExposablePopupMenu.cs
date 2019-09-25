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
            public float m_Height;
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

        float m_ItemSpacing;
        PopupButtonData m_PopupButtonData;
        GUIContent m_Label;

        int[] m_ItemControlIDs;
        int m_DropDownButtonControlID;
        static readonly int s_ItemHash = "ItemButton".GetHashCode();
        static readonly int m_DropDownButtonHash = "DropDownButton".GetHashCode();

        float m_SpacingLabelToButton = 5f;
        float m_WidthOfLabel;
        float m_WidthOfButtons;
        float m_MinWidthOfPopup;
        Vector2 m_PopupButtonSize = Vector2.zero;
        System.Action<ItemData> m_SelectionChangedCallback = null; // <userData>

        public float widthOfButtonsAndLabel {  get { return m_WidthOfButtons + labelAndSpacingWidth; } }
        public float widthOfPopupAndLabel { get { return m_PopupButtonSize.x + labelAndSpacingWidth; } }
        public bool rightAligned { get; set; }
        float labelAndSpacingWidth { get { return m_WidthOfLabel > 0 ? m_WidthOfLabel + m_SpacingLabelToButton : 0f; } }
        bool hasLabel { get { return m_Label != null && m_Label != GUIContent.none;  } }

        public void Init(List<ItemData> items, float itemSpacing, float minWidthOfPopup, PopupButtonData popupButtonData, System.Action<ItemData> selectionChangedCallback)
        {
            Init(GUIContent.none, items, itemSpacing, minWidthOfPopup, popupButtonData, selectionChangedCallback);
        }

        public void Init(GUIContent label, List<ItemData> items, float itemSpacing, float minWidthOfPopup, PopupButtonData popupButtonData,  System.Action<ItemData> selectionChangedCallback)
        {
            m_Label = label;
            m_Items = items;
            m_ItemSpacing = itemSpacing;
            m_PopupButtonData = popupButtonData;
            m_SelectionChangedCallback = selectionChangedCallback;
            m_MinWidthOfPopup = minWidthOfPopup;
            CalcWidths();
        }

        public float OnGUI(Rect rect)
        {
            // To ensure we allocate a consistent amount of controlIDs on every OnGUI we preallocate before any logic
            if (m_Items.Count > 0 && (m_ItemControlIDs == null || m_ItemControlIDs.Length != m_Items.Count))
                m_ItemControlIDs = new int[m_Items.Count];
            for (int i = 0; i < m_Items.Count; ++i)
                m_ItemControlIDs[i] = GUIUtility.GetControlID(s_ItemHash, FocusType.Passive);
            m_DropDownButtonControlID = GUIUtility.GetControlID(m_DropDownButtonHash, FocusType.Passive);

            if (rect.width >= widthOfButtonsAndLabel && rect.width > m_MinWidthOfPopup)
            {
                // Show as buttons

                if (hasLabel)
                {
                    Rect labelRect = rect;
                    labelRect.width = m_WidthOfLabel;
                    if (rightAligned)
                        labelRect.x = rect.xMax - widthOfButtonsAndLabel;

                    GUI.Label(labelRect, m_Label, EditorStyles.boldLabel);
                    rect.xMin += (m_WidthOfLabel + m_SpacingLabelToButton);
                }

                Rect buttonRect = rect;
                buttonRect.width = widthOfButtonsAndLabel;
                if (rightAligned)
                    buttonRect.x = rect.xMax - m_WidthOfButtons;

                for (int i = 0; i < m_Items.Count; ++i)
                {
                    var item = m_Items[i];
                    buttonRect.width = item.m_Width;
                    buttonRect.y = rect.y + (rect.height - item.m_Height) / 2;
                    buttonRect.height = item.m_Height;

                    EditorGUI.BeginChangeCheck();

                    using (new EditorGUI.DisabledScope(!item.m_Enabled))
                    {
                        GUI.Toggle(buttonRect, m_ItemControlIDs[i], item.m_On, item.m_GUIContent, item.m_Style);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        SelectionChanged(item);
                        GUIUtility.ExitGUI(); // To make sure we can survive if m_Buttons are reallocated in the callback we exit gui
                    }

                    buttonRect.x += item.m_Width + m_ItemSpacing;
                }

                return widthOfButtonsAndLabel;
            }
            else
            {
                // Show as popup

                var dropDownRect = rect;
                if (hasLabel)
                {
                    Rect labelRect = dropDownRect;
                    labelRect.width = m_WidthOfLabel;
                    if (rightAligned)
                        labelRect.x = rect.xMax - widthOfPopupAndLabel;

                    GUI.Label(labelRect, m_Label, EditorStyles.boldLabel);
                    dropDownRect.x = labelRect.x + (m_WidthOfLabel + m_SpacingLabelToButton);
                }
                else
                {
                    if (rightAligned)
                        dropDownRect.x = rect.xMax - dropDownRect.width;
                }

                dropDownRect.width = Mathf.Clamp(dropDownRect.width, 0, m_PopupButtonSize.x);
                dropDownRect.height = m_PopupButtonSize.y;
                dropDownRect.y = rect.y + (rect.height - dropDownRect.height) / 2;

                if (EditorGUI.DropdownButton(m_DropDownButtonControlID, dropDownRect, m_PopupButtonData.m_GUIContent, m_PopupButtonData.m_Style))
                    PopUpMenu.Show(dropDownRect, m_Items, this);

                return widthOfPopupAndLabel;
            }
        }

        void CalcWidths()
        {
            // Buttons
            m_WidthOfButtons = 0f;
            foreach (var item in m_Items)
            {
                var itemSize = item.m_Style.CalcSize(item.m_GUIContent);
                item.m_Width = itemSize.x;
                item.m_Height = itemSize.y;

                m_WidthOfButtons += item.m_Width;
            }
            m_WidthOfButtons += (m_Items.Count - 1) * m_ItemSpacing;

            // Popup
            m_PopupButtonSize = m_PopupButtonData.m_Style.CalcSize(m_PopupButtonData.m_GUIContent);

            // Label
            m_WidthOfLabel = hasLabel ? EditorStyles.boldLabel.CalcSize(m_Label).x : 0;
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
