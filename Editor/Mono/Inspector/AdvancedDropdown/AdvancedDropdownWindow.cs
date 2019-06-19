// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Callbacks;
using UnityEngine;
using Event = UnityEngine.Event;

namespace UnityEditor.IMGUI.Controls
{
    internal class AdvancedDropdownWindow : EditorWindow
    {
        private static class Styles
        {
            public static GUIStyle background = "box";
            public static GUIStyle previewHeader = new GUIStyle(EditorStyles.label);
            public static GUIStyle previewText = new GUIStyle(EditorStyles.wordWrappedLabel);

            static Styles()
            {
                previewText.padding.left += 3;
                previewText.padding.right += 3;
                previewHeader.padding.left += 3 - 2;
                previewHeader.padding.right += 3;
                previewHeader.padding.top += 3;
                previewHeader.padding.bottom += 2;
            }
        }
        private static readonly float kBorderThickness = 1f;
        private static readonly float kRightMargin = 13f;

        private AdvancedDropdownGUI m_Gui = null;
        private AdvancedDropdownDataSource m_DataSource = null;
        private AdvancedDropdownState m_State = null;

        private AdvancedDropdownItem m_CurrentlyRenderedTree;
        protected AdvancedDropdownItem renderedTreeItem => m_CurrentlyRenderedTree;

        private AdvancedDropdownItem m_AnimationTree;
        private float m_NewAnimTarget = 0;
        private long m_LastTime = 0;
        private bool m_ScrollToSelected = true;
        private float m_InitialSelectionPosition = 0f;
        private Rect m_ButtonRectScreenPos;
        private Stack<AdvancedDropdownItem> m_ViewsStack = new Stack<AdvancedDropdownItem>();
        private bool m_DirtyList = true;

        private string m_Search = "";
        private bool hasSearch { get { return !string.IsNullOrEmpty(m_Search); } }
        protected internal string searchString
        {
            get { return m_Search; }
            set
            {
                m_Search = value;
                m_DataSource.RebuildSearch(m_Search);
                m_CurrentlyRenderedTree = m_DataSource.mainTree;
                if (hasSearch)
                {
                    m_CurrentlyRenderedTree = m_DataSource.searchTree;
                    if (state.GetSelectedIndex(m_CurrentlyRenderedTree) < 0)
                    {
                        state.SetSelectedIndex(m_CurrentlyRenderedTree, 0);
                    }
                }
            }
        }

        internal bool showHeader { get; set; } = true;
        internal bool searchable { get; set; } = true;
        internal bool closeOnSelection { get; set; } = true;

        protected virtual bool isSearchFieldDisabled { get; set; }
        protected virtual bool setInitialSelectionPosition { get; } = true;

        protected internal AdvancedDropdownState state
        {
            get { return m_State; }
            set { m_State = value; }
        }

        protected internal AdvancedDropdownGUI gui
        {
            get { return m_Gui; }
            set { m_Gui = value; }
        }

        protected internal AdvancedDropdownDataSource dataSource
        {
            get { return m_DataSource; }
            set { m_DataSource = value; }
        }

        public event Action<AdvancedDropdownWindow> windowClosed;
        public event Action<AdvancedDropdownItem> selectionChanged;

        protected virtual void OnEnable()
        {
            m_DirtyList = true;
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void OnDestroy()
        {
            // This window sets 'editingTextField = true' continuously, through EditorGUI.FocusTextInControl(),
            // for the searchfield in its AdvancedDropdownGUI so here we ensure to clean up. This fixes the issue that
            // EditorGUI.IsEditingTextField() was returning true after e.g the Add Component Menu closes
            EditorGUIUtility.editingTextField = false;
        }

        public static T CreateAndInit<T>(Rect rect, AdvancedDropdownState state) where T : AdvancedDropdownWindow
        {
            var instance = CreateInstance<T>();
            instance.m_State = state;
            instance.Init(rect);
            return instance;
        }

        public void Init(Rect buttonRect)
        {
            m_ButtonRectScreenPos = EditorGUIUtility.GUIToScreenRect(buttonRect);
            if (m_State == null)
                m_State = new AdvancedDropdownState();
            if (m_DataSource == null)
                m_DataSource = new MultiLevelDataSource();
            if (m_Gui == null)
                m_Gui = new AdvancedDropdownGUI(m_DataSource);
            m_Gui.state = m_State;

            // Has to be done before calling Show / ShowWithMode
            buttonRect = GUIUtility.GUIToScreenRect(buttonRect);
            OnDirtyList();
            m_CurrentlyRenderedTree = hasSearch ? m_DataSource.searchTree : m_DataSource.mainTree;
            ShowAsDropDown(buttonRect, CalculateWindowSize(buttonRect), GetLocationPriority());
            if (setInitialSelectionPosition)
            {
                m_InitialSelectionPosition = m_Gui.GetSelectionHeight(m_DataSource, buttonRect);
            }
            wantsMouseMove = true;
            SetSelectionFromState();
        }

        void SetSelectionFromState()
        {
            var selectedIndex = m_State.GetSelectedIndex(m_CurrentlyRenderedTree);
            while (selectedIndex >= 0)
            {
                var child = m_State.GetSelectedChild(m_CurrentlyRenderedTree);
                if (child == null)
                    break;
                selectedIndex = m_State.GetSelectedIndex(child);
                if (selectedIndex < 0)
                    break;
                m_ViewsStack.Push(m_CurrentlyRenderedTree);
                m_CurrentlyRenderedTree = child;
            }
        }

        PopupLocation[] GetLocationPriority()
        {
            return new[]
            {
                PopupLocation.Below,
                PopupLocation.Overlay,
            };
        }

        protected virtual Vector2 CalculateWindowSize(Rect buttonRect)
        {
            var size = m_Gui.CalculateContentSize(m_DataSource);
            // Add 1 pixel for each border
            size.x += kBorderThickness * 2;
            size.y += kBorderThickness * 2;
            size.x += kRightMargin;

            size.y += m_Gui.searchHeight;

            if (showHeader)
            {
                size.y += m_Gui.headerHeight;
            }

            size.y = Mathf.Clamp(size.y, minSize.y, maxSize.y);

            var fitRect = ContainerWindow.FitRectToScreen(new Rect(buttonRect.x, buttonRect.y, size.x, size.y), true, true);
            // If the scrollbar is visible, we want to add extra space to compensate it
            if (fitRect.height < size.y)
                size.x += GUI.skin.verticalScrollbar.fixedWidth;

            // Stretch to the width of the button
            if (size.x < buttonRect.width)
            {
                size.x = buttonRect.width;
            }
            if (size.x < minSize.x)
            {
                size.x = minSize.x;
            }
            if (size.y < minSize.y)
            {
                size.y = minSize.y;
            }

            return new Vector2(size.x, size.y);
        }

        internal void OnGUI()
        {
            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, Styles.background);

            if (m_DirtyList)
            {
                OnDirtyList();
            }

            HandleKeyboard();
            if (searchable)
                OnGUISearch();

            if (m_NewAnimTarget != 0 && Event.current.type == EventType.Layout)
            {
                long now = DateTime.Now.Ticks;
                float deltaTime = (now - m_LastTime) / (float)TimeSpan.TicksPerSecond;
                m_LastTime = now;

                m_NewAnimTarget = Mathf.MoveTowards(m_NewAnimTarget, 0, deltaTime * 4);

                if (m_NewAnimTarget == 0)
                {
                    m_AnimationTree = null;
                }
                Repaint();
            }

            var anim = m_NewAnimTarget;
            // Smooth the animation
            anim = Mathf.Floor(anim) + Mathf.SmoothStep(0, 1, Mathf.Repeat(anim, 1));

            if (anim == 0)
            {
                DrawDropdown(0, m_CurrentlyRenderedTree);
            }
            else if (anim < 0)
            {
                // Go to parent
                // m_NewAnimTarget goes -1 -> 0
                DrawDropdown(anim, m_CurrentlyRenderedTree);
                DrawDropdown(anim + 1, m_AnimationTree);
            }
            else // > 0
            {
                // Go to child
                // m_NewAnimTarget 1 -> 0
                DrawDropdown(anim - 1, m_AnimationTree);
                DrawDropdown(anim, m_CurrentlyRenderedTree);
            }
        }

        private void OnDirtyList()
        {
            m_DirtyList = false;
            m_DataSource.ReloadData();
            if (hasSearch)
            {
                m_DataSource.RebuildSearch(searchString);
                if (state.GetSelectedIndex(m_CurrentlyRenderedTree) < 0)
                {
                    state.SetSelectedIndex(m_CurrentlyRenderedTree, 0);
                }
            }
        }

        private void OnGUISearch()
        {
            m_Gui.DrawSearchField(isSearchFieldDisabled, m_Search, (newSearch) =>
            {
                searchString = newSearch;
            });
        }

        private void HandleKeyboard()
        {
            var evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                // Special handling when in new script panel
                if (SpecialKeyboardHandling(evt))
                {
                    return;
                }

                // Always do these
                if (evt.keyCode == KeyCode.DownArrow)
                {
                    m_State.MoveDownSelection(m_CurrentlyRenderedTree);
                    m_ScrollToSelected = true;
                    evt.Use();
                }
                if (evt.keyCode == KeyCode.UpArrow)
                {
                    m_State.MoveUpSelection(m_CurrentlyRenderedTree);
                    m_ScrollToSelected = true;
                    evt.Use();
                }
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    var selected = m_State.GetSelectedChild(m_CurrentlyRenderedTree);
                    if (selected != null)
                    {
                        if (selected.children.Any())
                        {
                            GoToChild();
                        }
                        else
                        {
                            if (selectionChanged != null)
                            {
                                selectionChanged(m_State.GetSelectedChild(m_CurrentlyRenderedTree));
                            }
                            if (closeOnSelection)
                            {
                                CloseWindow();
                            }
                        }
                    }
                    evt.Use();
                }

                // Do these if we're not in search mode
                if (!hasSearch)
                {
                    if (evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.Backspace)
                    {
                        GoToParent();
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.RightArrow)
                    {
                        var idx = m_State.GetSelectedIndex(m_CurrentlyRenderedTree);
                        if (idx > -1 && m_CurrentlyRenderedTree.children.ElementAt(idx).children.Any())
                        {
                            GoToChild();
                        }
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.Escape)
                    {
                        Close();
                        evt.Use();
                    }
                }
            }
        }

        private void CloseWindow()
        {
            if (windowClosed != null)
                windowClosed(this);
            Close();
        }

        internal AdvancedDropdownItem GetSelectedItem()
        {
            return m_State.GetSelectedChild(m_CurrentlyRenderedTree);
        }

        protected virtual bool SpecialKeyboardHandling(Event evt)
        {
            return false;
        }

        private void DrawDropdown(float anim, AdvancedDropdownItem group)
        {
            // Start of animated area (the part that moves left and right)
            var areaPosition = new Rect(0, 0, position.width, position.height);
            // Adjust to the frame
            areaPosition.x += kBorderThickness;
            areaPosition.y += kBorderThickness;
            areaPosition.height -= kBorderThickness * 2;
            areaPosition.width -= kBorderThickness * 2;

            GUILayout.BeginArea(m_Gui.GetAnimRect(areaPosition, anim));
            // Header
            if (showHeader)
                m_Gui.DrawHeader(group, GoToParent, m_ViewsStack.Count > 0);

            DrawList(group);
            GUILayout.EndArea();
        }

        private void DrawList(AdvancedDropdownItem item)
        {
            // Start of scroll view list
            m_State.SetScrollState(item, GUILayout.BeginScrollView(m_State.GetScrollState(item), GUIStyle.none, GUI.skin.verticalScrollbar));
            EditorGUIUtility.SetIconSize(m_Gui.iconSize);
            Rect selectedRect = new Rect();
            for (var i = 0; i < item.children.Count(); i++)
            {
                var child = item.children.ElementAt(i);
                bool selected = m_State.GetSelectedIndex(item) == i;

                if (child.IsSeparator())
                {
                    m_Gui.DrawLineSeparator();
                }
                else
                {
                    m_Gui.DrawItem(child, child.displayName, child.icon, child.enabled, child.children.Any(), selected, hasSearch);
                }

                var r = GUILayoutUtility.GetLastRect();
                if (selected)
                    selectedRect = r;

                // Skip input handling for the tree used for animation
                if (item != m_CurrentlyRenderedTree)
                    continue;

                // Select the element the mouse cursor is over.
                // Only do it on mouse move - keyboard controls are allowed to overwrite this until the next time the mouse moves.
                if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag)
                {
                    if (!selected && r.Contains(Event.current.mousePosition))
                    {
                        m_State.SetSelectedIndex(item, i);
                        Event.current.Use();
                    }
                }
                if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition))
                {
                    m_State.SetSelectedIndex(item, i);
                    var selectedChild = m_State.GetSelectedChild(item);
                    if (selectedChild.children.Any())
                    {
                        GoToChild();
                    }
                    else
                    {
                        if (!selectedChild.IsSeparator() && selectionChanged != null)
                        {
                            selectionChanged(selectedChild);
                        }
                        if (closeOnSelection)
                        {
                            CloseWindow();
                            GUIUtility.ExitGUI();
                        }
                    }
                    Event.current.Use();
                }
            }
            EditorGUIUtility.SetIconSize(Vector2.zero);
            GUILayout.EndScrollView();

            // Scroll to selected on windows creation
            if (m_ScrollToSelected && m_InitialSelectionPosition != 0)
            {
                float diffOfPopupAboveTheButton = m_ButtonRectScreenPos.y - position.y;
                diffOfPopupAboveTheButton -= m_Gui.searchHeight + m_Gui.headerHeight;
                m_State.SetScrollState(item, new Vector2(0, m_InitialSelectionPosition - diffOfPopupAboveTheButton));
                m_ScrollToSelected = false;
                m_InitialSelectionPosition = 0;
            }
            // Scroll to show selected
            else if (m_ScrollToSelected && Event.current.type == EventType.Repaint)
            {
                m_ScrollToSelected = false;
                Rect scrollRect = GUILayoutUtility.GetLastRect();
                if (selectedRect.yMax - scrollRect.height > m_State.GetScrollState(item).y)
                {
                    m_State.SetScrollState(item, new Vector2(0, selectedRect.yMax - scrollRect.height));
                    Repaint();
                }
                if (selectedRect.y < m_State.GetScrollState(item).y)
                {
                    m_State.SetScrollState(item, new Vector2(0, selectedRect.y));
                    Repaint();
                }
            }
        }

        protected void GoToParent()
        {
            if (m_ViewsStack.Count == 0)
                return;
            m_LastTime = DateTime.Now.Ticks;
            if (m_NewAnimTarget > 0)
                m_NewAnimTarget = -1 + m_NewAnimTarget;
            else
                m_NewAnimTarget = -1;
            m_AnimationTree = m_CurrentlyRenderedTree;
            m_CurrentlyRenderedTree = m_ViewsStack.Pop();
        }

        private void GoToChild()
        {
            m_ViewsStack.Push(m_CurrentlyRenderedTree);
            m_LastTime = DateTime.Now.Ticks;
            if (m_NewAnimTarget < 0)
                m_NewAnimTarget = 1 + m_NewAnimTarget;
            else
                m_NewAnimTarget = 1;
            m_AnimationTree = m_CurrentlyRenderedTree;
            m_CurrentlyRenderedTree = m_State.GetSelectedChild(m_CurrentlyRenderedTree);
        }

        [DidReloadScripts]
        private static void OnScriptReload()
        {
            CloseAllOpenWindows<AdvancedDropdownWindow>();
        }

        protected static void CloseAllOpenWindows<T>()
        {
            var windows = Resources.FindObjectsOfTypeAll(typeof(T));
            foreach (var window in windows)
            {
                try
                {
                    ((EditorWindow)window).Close();
                }
                catch
                {
                    DestroyImmediate(window);
                }
            }
        }
    }
}
