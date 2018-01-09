// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.Callbacks;
using UnityEngine;
using Event = UnityEngine.Event;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class AdvancedDropdownWindow : EditorWindow
    {
        private static class Styles
        {
            public static GUIStyle background = "grey_border";
            public static GUIStyle previewHeader = new GUIStyle(EditorStyles.label);
            public static GUIStyle previewText = new GUIStyle(EditorStyles.wordWrappedLabel);

            static  Styles()
            {
                previewText.padding.left += 3;
                previewText.padding.right += 3;
                previewHeader.padding.left += 3 - 2;
                previewHeader.padding.right += 3;
                previewHeader.padding.top += 3;
                previewHeader.padding.bottom += 2;
            }
        }

        protected AdvancedDropdownGUI gui = new AdvancedDropdownGUI();
        public AdvancedDropdownDataSource dataSource = new AdvancedDropdownSimpleDataSource();

        protected AdvancedDropdownItem m_CurrentlyRenderedTree;

        private AdvancedDropdownItem m_AnimationTree;
        private float m_NewAnimTarget = 0;
        private long m_LastTime = 0;
        private bool m_ScrollToSelected = true;

        [NonSerialized]
        private bool m_DirtyList = true;

        protected string m_Search = "";
        private bool hasSearch { get { return !string.IsNullOrEmpty(m_Search); } }

        public event Action<AdvancedDropdownWindow> onSelected;
        public bool showHeader { get; set; } = true;

        protected virtual bool isSearchFieldDisabled { get; }

        protected virtual void OnEnable()
        {
            m_DirtyList = true;
        }

        protected virtual void OnDisable()
        {
        }

        public static T CreateAndInit<T>(Rect rect) where T : AdvancedDropdownWindow
        {
            var instance = CreateInstance<T>();
            instance.Init(rect);
            return instance;
        }

        public void Init(Rect buttonRect)
        {
            // Has to be done before calling Show / ShowWithMode
            buttonRect = GUIUtility.GUIToScreenRect(buttonRect);

            OnDirtyList();
            m_CurrentlyRenderedTree = hasSearch ? dataSource.searchTree : dataSource.mainTree;


            ShowAsDropDown(buttonRect, new Vector2(buttonRect.width, gui.WindowHeight), null, ShowMode.PopupMenuWithKeyboardFocus);

            Focus();

            // Add after unfreezing display because AuxWindowManager.cpp assumes that aux windows are added after we got/lost- focus calls.
            m_Parent.AddToAuxWindowList();

            wantsMouseMove = true;
        }

        internal void OnGUI()
        {
            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, Styles.background);

            if (m_DirtyList)
            {
                OnDirtyList();
            }

            HandleKeyboard();
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
                DrawDropdown(anim - 1 , m_AnimationTree);
                DrawDropdown(anim, m_CurrentlyRenderedTree);
            }
        }

        private void OnDirtyList()
        {
            m_DirtyList = false;
            dataSource.ReloadData();
            if (hasSearch)
                dataSource.RebuildSearch(m_Search);
        }

        private void OnGUISearch()
        {
            gui.DrawSearchField(isSearchFieldDisabled, m_Search, (newSearch) =>
                {
                    dataSource.RebuildSearch(newSearch);
                    m_CurrentlyRenderedTree =
                        string.IsNullOrEmpty(newSearch) ? dataSource.mainTree : dataSource.searchTree;
                    m_Search = newSearch;
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
                    m_CurrentlyRenderedTree.MoveDownSelection();
                    m_ScrollToSelected = true;
                    evt.Use();
                }
                if (evt.keyCode == KeyCode.UpArrow)
                {
                    m_CurrentlyRenderedTree.MoveUpSelection();
                    m_ScrollToSelected = true;
                    evt.Use();
                }
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    if (m_CurrentlyRenderedTree.GetSelectedChild().children.Any())
                    {
                        GoToChild(m_CurrentlyRenderedTree);
                    }
                    else
                    {
                        if (m_CurrentlyRenderedTree.GetSelectedChild().OnAction())
                        {
                            CloseWindow();
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
                        if (m_CurrentlyRenderedTree.GetSelectedChild().children.Any())
                            GoToChild(m_CurrentlyRenderedTree);
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
            if (onSelected != null)
                onSelected(this);
            Close();
        }

        internal string GetIdOfSelectedItem()
        {
            return m_CurrentlyRenderedTree.GetSelectedChild().id;
        }

        protected virtual bool SpecialKeyboardHandling(Event evt)
        {
            return false;
        }

        private void DrawDropdown(float anim, AdvancedDropdownItem group)
        {
            // Start of animated area (the part that moves left and right)
            var areaPosition = position;
            // Adjust to the frame
            areaPosition.height -= 1;
            GUILayout.BeginArea(gui.GetAnimRect(areaPosition, anim));
            // Header
            if (showHeader)
                gui.DrawHeader(group, GoToParent);

            DrawList(group);
            GUILayout.EndArea();
        }

        private void DrawList(AdvancedDropdownItem item)
        {
            // Start of scroll view list
            item.m_Scroll = GUILayout.BeginScrollView(item.m_Scroll);
            EditorGUIUtility.SetIconSize(gui.iconSize);
            Rect selectedRect = new Rect();
            for (var i = 0; i < item.children.Count; i++)
            {
                var child = item.children[i];
                bool selected = i == item.m_SelectedItem;
                gui.DrawItem(child, selected, hasSearch);
                var r = GUILayoutUtility.GetLastRect();
                if (selected)
                    selectedRect = r;

                // Select the element the mouse cursor is over.
                // Only do it on mouse move - keyboard controls are allowed to overwrite this until the next time the mouse moves.
                if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDown)
                {
                    //if (parent.selectedIndex != i && r.Contains(Event.current.mousePosition))
                    if (!selected && r.Contains(Event.current.mousePosition))
                    {
                        item.m_SelectedItem = i;
                        Event.current.Use();
                    }
                }
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    item.m_SelectedItem = i;
                    if (m_CurrentlyRenderedTree.GetSelectedChild().children.Any())
                    {
                        GoToChild(m_CurrentlyRenderedTree);
                    }
                    else
                    {
                        if (m_CurrentlyRenderedTree.GetSelectedChild().OnAction())
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

            // Scroll to show selected
            if (m_ScrollToSelected && Event.current.type == EventType.Repaint)
            {
                m_ScrollToSelected = false;
                Rect scrollRect = GUILayoutUtility.GetLastRect();
                if (selectedRect.yMax - scrollRect.height > item.m_Scroll.y)
                {
                    item.m_Scroll.y = selectedRect.yMax - scrollRect.height;
                    Repaint();
                }
                if (selectedRect.y < item.m_Scroll.y)
                {
                    item.m_Scroll.y = selectedRect.y;
                    Repaint();
                }
            }
        }

        protected void GoToParent()
        {
            if (m_CurrentlyRenderedTree.parent == null)
                return;
            m_LastTime = System.DateTime.Now.Ticks;
            if (m_NewAnimTarget > 0)
                m_NewAnimTarget = -1 + m_NewAnimTarget;
            else
                m_NewAnimTarget = -1;
            m_AnimationTree = m_CurrentlyRenderedTree;
            m_CurrentlyRenderedTree = m_CurrentlyRenderedTree.parent;
        }

        private void GoToChild(AdvancedDropdownItem parent)
        {
            m_LastTime = System.DateTime.Now.Ticks;
            if (m_NewAnimTarget < 0)
                m_NewAnimTarget = 1 + m_NewAnimTarget;
            else
                m_NewAnimTarget = 1;
            m_CurrentlyRenderedTree = parent.GetSelectedChild();
            m_AnimationTree = parent;
        }

        public int GetSelectedIndex()
        {
            return m_CurrentlyRenderedTree.GetSelectedChildIndex();
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
