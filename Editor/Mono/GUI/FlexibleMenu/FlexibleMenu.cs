// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;

namespace UnityEditor
{
    internal class FlexibleMenu : PopupWindowContent
    {
        class Styles
        {
            public GUIStyle menuItem = "MenuItem";
            public GUIContent plusButtonText = new GUIContent("", "Add New Item");
        }
        static Styles s_Styles;

        IFlexibleMenuItemProvider m_ItemProvider;
        FlexibleMenuModifyItemUI m_ModifyItemUI;
        readonly Action<int, object> m_ItemClickedCallback;
        Vector2 m_ScrollPosition = Vector2.zero;
        bool m_ShowAddNewPresetItem;
        int m_ShowEditWindowForIndex = -1;
        int m_HoverIndex;
        int[] m_SeperatorIndices;
        float m_CachedWidth = -1f;
        float m_MinTextWidth = 200f;

        const float lineHeight = 18f;
        const float seperatorHeight = 8f;
        const float leftMargin = 25f;
        int maxIndex { get { return m_ShowAddNewPresetItem ? m_ItemProvider.Count() : m_ItemProvider.Count() - 1; } }
        public int selectedIndex { get; set; }
        protected float minTextWidth { get { return m_MinTextWidth; } set { m_MinTextWidth = value; ClearCachedWidth(); } }

        // itemClickedCallback arguments is clicked index, clicked item object
        public FlexibleMenu(IFlexibleMenuItemProvider itemProvider, int selectionIndex, FlexibleMenuModifyItemUI modifyItemUi, Action<int, object> itemClickedCallback)
        {
            m_ItemProvider = itemProvider;
            m_ModifyItemUI = modifyItemUi;
            m_ItemClickedCallback = itemClickedCallback;
            m_SeperatorIndices = m_ItemProvider.GetSeperatorIndices();
            selectedIndex = selectionIndex;
            m_ShowAddNewPresetItem = m_ModifyItemUI != null;
        }

        public override Vector2 GetWindowSize()
        {
            return CalcSize();
        }

        bool IsDeleteModiferPressed()
        {
            return Event.current.alt;
        }

        bool AllowDeleteClick(int index)
        {
            return IsDeleteModiferPressed() && m_ItemProvider.IsModificationAllowed(index) && GUIUtility.hotControl == 0;
        }

        public override void OnGUI(Rect rect)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            Event evt = Event.current;

            Rect contentRect = new Rect(0, 0, 1, CalcSize().y);
            m_ScrollPosition = GUI.BeginScrollView(rect, m_ScrollPosition, contentRect);
            {
                float curY = 0f;
                for (int i = 0; i <= maxIndex; ++i)
                {
                    int itemControlID = i + 1000000;
                    Rect itemRect = new Rect(0, curY, rect.width, lineHeight);
                    bool addSeperator = Array.IndexOf(m_SeperatorIndices, i) >= 0;

                    // Add new preset button
                    if (m_ShowAddNewPresetItem && i == m_ItemProvider.Count())
                    {
                        CreateNewItemButton(itemRect);
                        continue;
                    }

                    if (m_ShowEditWindowForIndex == i)
                    {
                        m_ShowEditWindowForIndex = -1;
                        EditExistingItem(itemRect, i);
                    }

                    // Handle event
                    switch (evt.type)
                    {
                        case EventType.Repaint:
                            bool hover = false;
                            if (m_HoverIndex == i)
                            {
                                if (itemRect.Contains(evt.mousePosition))
                                    hover = true;
                                else
                                    m_HoverIndex = -1;
                            }

                            // Overwrite if
                            if (m_ModifyItemUI != null && m_ModifyItemUI.IsShowing())
                            {
                                hover = m_ItemProvider.GetItem(i) == m_ModifyItemUI.m_Object;
                            }

                            s_Styles.menuItem.Draw(itemRect, GUIContent.Temp(m_ItemProvider.GetName(i)), hover, false, i == selectedIndex, false);

                            if (addSeperator)
                            {
                                const float margin = 4f;
                                Rect seperatorRect = new Rect(itemRect.x + margin, itemRect.y + itemRect.height + seperatorHeight * 0.5f, itemRect.width - 2 * margin, 1);
                                DrawRect(seperatorRect, (EditorGUIUtility.isProSkin) ? new Color(0.32f, 0.32f, 0.32f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f)); // dark : light
                            }

                            // We delete presets on alt-click
                            if (AllowDeleteClick(i))
                                EditorGUIUtility.AddCursorRect(itemRect, MouseCursor.ArrowMinus);
                            break;

                        case EventType.MouseDown:
                            if (evt.button == 0 && itemRect.Contains(evt.mousePosition))
                            {
                                GUIUtility.hotControl = itemControlID;
                                if (!IsDeleteModiferPressed() && evt.clickCount == 1)
                                {
                                    GUIUtility.hotControl = 0;
                                    SelectItem(i);
                                    editorWindow.Close();
                                    evt.Use();
                                }
                            }
                            break;

                        case EventType.MouseUp:
                            if (GUIUtility.hotControl == itemControlID)
                            {
                                GUIUtility.hotControl = 0;
                                if (evt.button == 0 && itemRect.Contains(evt.mousePosition))
                                {
                                    if (AllowDeleteClick(i))
                                    {
                                        DeleteItem(i);
                                        evt.Use();
                                    }
                                }
                            }
                            break;

                        case EventType.ContextClick:
                            if (itemRect.Contains(evt.mousePosition))
                            {
                                evt.Use();
                                if (m_ModifyItemUI != null && m_ItemProvider.IsModificationAllowed(i))
                                    ItemContextMenu.Show(i, this);
                            }
                            break;

                        case EventType.MouseMove:
                            if (itemRect.Contains(evt.mousePosition))
                            {
                                if (m_HoverIndex != i)
                                {
                                    m_HoverIndex = i;
                                    Repaint();
                                }
                            }
                            else if (m_HoverIndex == i)
                            {
                                m_HoverIndex = -1;
                                Repaint();
                            }
                            break;
                    }

                    curY += lineHeight;
                    if (addSeperator)
                        curY += seperatorHeight;
                } // end foreach item
            } GUI.EndScrollView();
        }

        void SelectItem(int index)
        {
            selectedIndex = index;
            if (m_ItemClickedCallback != null && index >= 0)
                m_ItemClickedCallback(index, m_ItemProvider.GetItem(index));
        }

        protected Vector2 CalcSize()
        {
            float height = (maxIndex + 1) * lineHeight + m_SeperatorIndices.Length * seperatorHeight;
            if (m_CachedWidth < 0)
                m_CachedWidth = Math.Max(m_MinTextWidth, CalcWidth());
            return new Vector2(m_CachedWidth, height);
        }

        void ClearCachedWidth()
        {
            m_CachedWidth = -1f;
        }

        float CalcWidth()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            float maxWidth = 0;
            for (int i = 0; i < m_ItemProvider.Count(); ++i)
            {
                float w = s_Styles.menuItem.CalcSize(GUIContent.Temp(m_ItemProvider.GetName(i))).x;
                maxWidth = Mathf.Max(w, maxWidth);
            }

            const float rightMargin = 6f;
            return maxWidth + rightMargin;
        }

        void Repaint()
        {
            HandleUtility.Repaint(); // repaints current guiview (needs rename)
        }

        void CreateNewItemButton(Rect itemRect)
        {
            if (m_ModifyItemUI == null)
                return;

            Rect plusRect = new Rect(itemRect.x + leftMargin, itemRect.y, 15, 15);
            if (GUI.Button(plusRect, s_Styles.plusButtonText, "OL Plus"))
            {
                plusRect.y -= 15f;
                m_ModifyItemUI.Init(FlexibleMenuModifyItemUI.MenuType.Add, m_ItemProvider.Create(),
                    delegate(object obj)
                    {
                        ClearCachedWidth();
                        int newIndex = m_ItemProvider.Add(obj);
                        SelectItem(newIndex);
                        EditorApplication.RequestRepaintAllViews(); // We want to repaint the flexible menu (currently in modifyItemUI)
                    });
                PopupWindow.Show(plusRect, m_ModifyItemUI, null, ShowMode.PopupMenuWithKeyboardFocus);
            }
        }

        void EditExistingItem(Rect itemRect, int index)
        {
            if (m_ModifyItemUI == null)
                return;

            itemRect.y -= itemRect.height;
            itemRect.x += itemRect.width;
            m_ModifyItemUI.Init(FlexibleMenuModifyItemUI.MenuType.Edit, m_ItemProvider.GetItem(index),
                delegate(object obj)
                {
                    ClearCachedWidth();
                    m_ItemProvider.Replace(index, obj);
                    EditorApplication.RequestRepaintAllViews(); // We want to repaint the flexible menu (currently in modifyItemUI)
                });
            PopupWindow.Show(itemRect, m_ModifyItemUI, null, ShowMode.PopupMenuWithKeyboardFocus);
        }

        void DeleteItem(int index)
        {
            ClearCachedWidth();
            m_ItemProvider.Remove(index);
            selectedIndex = Mathf.Clamp(selectedIndex, 0, m_ItemProvider.Count() - 1); // ensure valid
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

        internal static class ItemContextMenu
        {
            static FlexibleMenu s_Caller;

            static public void Show(int itemIndex, FlexibleMenu caller)
            {
                s_Caller = caller;
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Edit..."), false, Edit, itemIndex);
                menu.AddItem(new GUIContent("Delete"), false, Delete, itemIndex);
                menu.ShowAsContext();
                EditorGUIUtility.ExitGUI();
            }

            private static void Delete(object userData)
            {
                int itemIndex = (int)userData;
                s_Caller.DeleteItem(itemIndex);
            }

            private static void Edit(object userData)
            {
                int itemIndex = (int)userData;
                s_Caller.m_ShowEditWindowForIndex = itemIndex;
            }
        }
    }
} // namespace
