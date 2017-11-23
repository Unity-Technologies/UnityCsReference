// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Event = UnityEngine.Event;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [InitializeOnLoad]
    [Serializable]
    internal abstract class AdvancedDropdownWindow : EditorWindow
    {
        protected internal static Styles s_Styles;
        internal class Styles
        {
            public GUIStyle header = new GUIStyle(EditorStyles.inspectorBig);
            public GUIStyle componentButton = new GUIStyle("PR Label");
            public GUIStyle groupButton;
            public GUIStyle background = "grey_border";
            public GUIStyle previewHeader = new GUIStyle(EditorStyles.label);
            public GUIStyle previewText = new GUIStyle(EditorStyles.wordWrappedLabel);
            public GUIStyle rightArrow = "AC RightArrow";
            public GUIStyle leftArrow = "AC LeftArrow";

            public Styles()
            {
                header.font = EditorStyles.boldLabel.font;

                componentButton.alignment = TextAnchor.MiddleLeft;
                componentButton.padding.left -= 15;
                componentButton.fixedHeight = 20;

                groupButton = new GUIStyle(componentButton);
                groupButton.padding.left += 17;

                previewText.padding.left += 3;
                previewText.padding.right += 3;
                previewHeader.padding.left += 3 - 2;
                previewHeader.padding.right += 3;
                previewHeader.padding.top += 3;
                previewHeader.padding.bottom += 2;
            }
        }

        private const int kHeaderHeight = 30;
        private const int kWindowHeight = 400 - 80;
        private const int kHelpHeight = 80 * 0;
        private const string kSearchHeader = "Search";

        private DropdownElement m_MainTree;
        private DropdownElement m_SearchTree;
        protected DropdownElement CurrentlyRenderedTree
        {
            get
            {
                return hasSearch ? m_SearchTree : m_MainTree;
            }
            set
            {
                if (hasSearch)
                {
                    m_SearchTree = value;
                }
                else
                {
                    m_MainTree = value;
                }
            }
        }

        private DropdownElement m_AnimationTree;
        private float m_NewAnimTarget = 0;
        private long m_LastTime = 0;
        private bool m_ScrollToSelected = true;
        private bool m_DirtyList = true;

        protected string m_Search = "";
        private bool hasSearch { get { return !string.IsNullOrEmpty(m_Search); } }

        public event Action<AdvancedDropdownWindow> onSelected;

        protected abstract DropdownElement RebuildTree();
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

            ShowAsDropDown(buttonRect, new Vector2(buttonRect.width, kWindowHeight), null, ShowMode.PopupMenuWithKeyboardFocus);

            Focus();

            // Add after unfreezing display because AuxWindowManager.cpp assumes that aux windows are added after we got/lost- focus calls.
            m_Parent.AddToAuxWindowList();

            wantsMouseMove = true;
        }

        internal void OnGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, s_Styles.background);

            if (m_DirtyList)
            {
                OnDirtyList();
            }

            HandleKeyboard();

            GUILayout.Space(7);

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
                DrawDropdown(0, CurrentlyRenderedTree);
            }
            else if (anim < 0)
            {
                // Go to parent
                // m_NewAnimTarget goes -1 -> 0
                DrawDropdown(anim, CurrentlyRenderedTree);
                DrawDropdown(anim + 1, m_AnimationTree);
            }
            else // > 0
            {
                // Go to child
                // m_NewAnimTarget 1 -> 0
                DrawDropdown(anim - 1 , m_AnimationTree);
                DrawDropdown(anim, CurrentlyRenderedTree);
            }
        }

        private void OnDirtyList()
        {
            m_DirtyList = false;
            m_MainTree = RebuildTree();
            if (hasSearch)
                m_SearchTree = RebuildSearch();
        }

        private void OnGUISearch()
        {
            if (!isSearchFieldDisabled)
            {
                EditorGUI.FocusTextInControl("ComponentSearch");
            }

            var searchRect = GUILayoutUtility.GetRect(10, 20);
            searchRect.x += 8;
            searchRect.width -= 16;

            GUI.SetNextControlName("ComponentSearch");

            using (new EditorGUI.DisabledScope(isSearchFieldDisabled))
            {
                var newSearch = EditorGUI.SearchField(searchRect, m_Search);

                if (newSearch != m_Search)
                {
                    m_Search = newSearch;
                    m_SearchTree = RebuildSearch();
                }
            }
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
                    CurrentlyRenderedTree.selectedItem++;
                    m_ScrollToSelected = true;
                    evt.Use();
                }
                if (evt.keyCode == KeyCode.UpArrow)
                {
                    CurrentlyRenderedTree.selectedItem--;
                    m_ScrollToSelected = true;
                    evt.Use();
                }
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    if (CurrentlyRenderedTree.GetSelectedChild().children.Any())
                    {
                        GoToChild(CurrentlyRenderedTree);
                    }
                    else
                    {
                        if (CurrentlyRenderedTree.GetSelectedChild().OnAction())
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
                        if (CurrentlyRenderedTree.GetSelectedChild().children.Any())
                            GoToChild(CurrentlyRenderedTree);
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
            return CurrentlyRenderedTree.GetSelectedChild().id;
        }

        protected virtual bool SpecialKeyboardHandling(Event evt)
        {
            return false;
        }

        private void DrawDropdown(float anim, DropdownElement group)
        {
            // Calculate rect for animated area
            var animRect = new Rect(position);
            animRect.x = 1 + (position.width * anim);
            animRect.y = kHeaderHeight;
            animRect.height -= kHeaderHeight + kHelpHeight;
            animRect.width -= 2;

            // Start of animated area (the part that moves left and right)
            GUILayout.BeginArea(animRect);

            // Header
            var headerRect = GUILayoutUtility.GetRect(10, 25);
            var name = group.name;
            GUI.Label(headerRect, name, s_Styles.header);

            // Back button
            if (group.parent != null)
            {
                var arrowRect = new Rect(headerRect.x + 4, headerRect.y + 7, 13, 13);
                if (Event.current.type == EventType.Repaint)
                    s_Styles.leftArrow.Draw(arrowRect, false, false, false, false);
                if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
                {
                    GoToParent();
                    Event.current.Use();
                }
            }

            DrawList(group);

            GUILayout.EndArea();
        }

        private void DrawList(DropdownElement element)
        {
            // Start of scroll view list
            element.m_Scroll = GUILayout.BeginScrollView(element.m_Scroll);
            EditorGUIUtility.SetIconSize(new Vector2(16, 16));

            Rect selectedRect = new Rect();
            for (var i = 0; i < element.children.Count; i++)
            {
                var child = element.children[i];
                bool selected = i == element.m_SelectedItem;
                child.Draw(selected, hasSearch);
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
                        element.m_SelectedItem = i;
                        Event.current.Use();
                    }
                }
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    element.m_SelectedItem = i;
                    if (CurrentlyRenderedTree.GetSelectedChild().children.Any())
                    {
                        GoToChild(CurrentlyRenderedTree);
                    }
                    else
                    {
                        if (CurrentlyRenderedTree.GetSelectedChild().OnAction())
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
                if (selectedRect.yMax - scrollRect.height > element.m_Scroll.y)
                {
                    element.m_Scroll.y = selectedRect.yMax - scrollRect.height;
                    Repaint();
                }
                if (selectedRect.y < element.m_Scroll.y)
                {
                    element.m_Scroll.y = selectedRect.y;
                    Repaint();
                }
            }
        }

        protected virtual DropdownElement RebuildSearch()
        {
            if (string.IsNullOrEmpty(m_Search))
                return null;

            // Support multiple search words separated by spaces.
            var searchWords = m_Search.ToLower().Split(' ');

            // We keep two lists. Matches that matches the start of an item always get first priority.
            var matchesStart = new List<DropdownElement>();
            var matchesWithin = new List<DropdownElement>();

            foreach (var e in m_MainTree.GetSearchableElements())
            {
                var name = e.name.ToLower().Replace(" ", "");

                var didMatchAll = true;
                var didMatchStart = false;

                // See if we match ALL the seaarch words.
                for (var w = 0; w < searchWords.Length; w++)
                {
                    var search = searchWords[w];
                    if (name.Contains(search))
                    {
                        // If the start of the item matches the first search word, make a note of that.
                        if (w == 0 && name.StartsWith(search))
                            didMatchStart = true;
                    }
                    else
                    {
                        // As soon as any word is not matched, we disregard this item.
                        didMatchAll = false;
                        break;
                    }
                }
                // We always need to match all search words.
                // If we ALSO matched the start, this item gets priority.
                if (didMatchAll)
                {
                    if (didMatchStart)
                        matchesStart.Add(e);
                    else
                        matchesWithin.Add(e);
                }
            }

            matchesStart.Sort();
            matchesWithin.Sort();

            var searchTree = new GroupDropdownElement(kSearchHeader);
            foreach (var element in matchesStart)
            {
                searchTree.AddChild(element);
            }
            foreach (var element in matchesWithin)
            {
                searchTree.AddChild(element);
            }
            return searchTree;
        }

        protected void GoToParent()
        {
            if (CurrentlyRenderedTree.parent == null)
                return;
            m_LastTime = System.DateTime.Now.Ticks;
            if (m_NewAnimTarget > 0)
                m_NewAnimTarget = -1 + m_NewAnimTarget;
            else
                m_NewAnimTarget = -1;
            m_AnimationTree = CurrentlyRenderedTree;
            CurrentlyRenderedTree = CurrentlyRenderedTree.parent;
        }

        private void GoToChild(DropdownElement parent)
        {
            m_LastTime = System.DateTime.Now.Ticks;
            if (m_NewAnimTarget < 0)
                m_NewAnimTarget = 1 + m_NewAnimTarget;
            else
                m_NewAnimTarget = 1;
            CurrentlyRenderedTree = parent.GetSelectedChild();
            m_AnimationTree = parent;
        }

        public int GetSelectedIndex()
        {
            return CurrentlyRenderedTree.GetSelectedChildIndex();
        }
    }
}
