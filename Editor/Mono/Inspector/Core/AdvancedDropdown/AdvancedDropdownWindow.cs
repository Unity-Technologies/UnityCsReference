// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;
using Event = UnityEngine.Event;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.RenderPipelines.Core.Editor")]
namespace UnityEditor.IMGUI.Controls
{
    internal class AdvancedDropdownWindow : EditorWindow
    {
        private static class Styles
        {
            public static GUIStyle background = "DD Background";
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
        // Sentinel used when the window has no explicit size constraint set by the caller. Coming from EditorWindow defaults.
        private static readonly Vector2 k_MinWindowSize = new Vector2(50, 50);
        private static readonly Vector2 k_MaxWindowSize = new Vector2(4000, 4000);

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
        private bool m_NeedsResize = false;

        // Caller-supplied size constraints captured in Init() so the dynamic resize logic can
        // still respect minimumSize/maximumSize even after they're overwritten by ShowAsDropDown.
        private Vector2 m_OriginalMinSize;
        private Vector2 m_OriginalMaxSize;

        // Set by the AdvancedDropdown wrapper before Init runs. null means the caller didn't
        // supply bounds → fall back to design defaults. Bypasses EditorWindow's minSize/maxSize
        // so we can distinguish "unset" from "set to the EditorWindow default of (50, 50)".
        internal Vector2? callerMinSize { get; set; }
        internal Vector2? callerMaxSize { get; set; }

        // Default minimum content height applied when the caller didn't pass minimumSize.
        // ~6 items in the default Editor skin — prevents 1–2 item levels from looking cramped.
        // No default upper bound: tall levels grow to fit; ContainerWindow.FitRectToMouseScreen
        // already prevents the window from running off-screen.
        private const float k_DefaultMinContentHeight = 120f;

        private string m_Search = "";
        private bool hasSearch { get { return !string.IsNullOrEmpty(m_Search); } }

        // Snapshot of where the user was when search started, so clearing the search box
        // restores their position instead of dropping them back at the root.
        private AdvancedDropdownItem m_PreSearchTree;
        private Stack<AdvancedDropdownItem> m_PreSearchViewsStack;

        protected internal string searchString
        {
            get { return m_Search; }
            set
            {
                bool wasSearching = hasSearch;
                m_Search = value;
                bool isSearching = hasSearch;

                // Entering search — remember the user's current level (and views stack) so
                // we can restore them when the search box is cleared.
                if (!wasSearching && isSearching)
                {
                    m_PreSearchTree = m_CurrentlyRenderedTree;
                    m_PreSearchViewsStack = new Stack<AdvancedDropdownItem>(new Stack<AdvancedDropdownItem>(m_ViewsStack));
                }

                // Pass the user's real tree (not searchTree) so contextual search scopes correctly
                // on first entry. RebuildSearch ignores updates where currentTree == searchTree,
                // so subsequent keystrokes don't override the context.
                m_DataSource.RebuildSearch(m_Search, m_CurrentlyRenderedTree);

                if (isSearching)
                {
                    m_CurrentlyRenderedTree = m_DataSource.searchTree;
                    if (state.GetSelectedIndex(m_CurrentlyRenderedTree) < 0)
                    {
                        state.SetSelectedIndex(m_CurrentlyRenderedTree, 0);
                    }
                }
                else if (m_PreSearchTree != null)
                {
                    // Exiting search — restore the level + stack the user was on when search started.
                    m_CurrentlyRenderedTree = m_PreSearchTree;
                    m_ViewsStack = m_PreSearchViewsStack;
                    m_PreSearchTree = null;
                    m_PreSearchViewsStack = null;
                }
                else
                {
                    // searchString set to empty before any search was ever active — default to root.
                    m_CurrentlyRenderedTree = m_DataSource.mainTree;
                }
            }
        }

        internal bool showHeader { get; set; } = true;
        internal bool searchable { get; set; } = true;
        internal bool closeOnSelection { get; set; } = true;

        internal void SetDataSourceDirty()
        {
            m_DirtyList = true;
            Repaint();
        }

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
        public event Action selectionCanceled;
        internal Func<Event, bool> specialKeyboardHandling;

        protected virtual void OnEnable()
        {
            m_DirtyList = true;
        }

        protected virtual void OnDisable()
        {
            selectionCanceled?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            // This window sets 'editingTextField = true' continuously, through EditorGUI.FocusTextInControl(),
            // for the search field in its AdvancedDropdownGUI so here we ensure to clean up. This fixes the issue that
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

            // Apply saved state before calculating size, so if it points to a child level, calculate size for it
            SetSelectionFromState();

            // Force the GUI to initialise its header/search rects so CalculateDefaultExtraHeight()
            // returns the real size on the very first call.
            m_Gui.CalculateContentSize(m_DataSource, m_CurrentlyRenderedTree);

            // If callerMinSize/callerMaxSize weren't set by wrapper, check if window.minSize/maxSize
            // were set by direct callers before Init(). Only use them if they differ from EditorWindow
            // defaults (50, 50) to distinguish "explicitly set" from "unset".
            // Edge case: A direct caller who explicitly wants (50, 50) or (4000, 4000) will be treated as "not set" and get design defaults.
            // Use the wrapper or set callerMinSize/callerMaxSize directly for exact control.
            if (!callerMinSize.HasValue && minSize != k_MinWindowSize)
                callerMinSize = minSize;
            if (!callerMaxSize.HasValue && maxSize != k_MaxWindowSize)
                callerMaxSize = maxSize;

            // Apply caller bounds if supplied, otherwise fall back to defaults. The min default
            // prevents 1-item levels from looking cramped; for max, we use the EditorWindow-level
            // cap (4000 px) so content drives the window height — screen-fit clamping in
            // CalculateWindowSize handles the off-screen case.
            m_OriginalMinSize = callerMinSize ?? new Vector2(0, k_DefaultMinContentHeight + CalculateDefaultExtraHeight());
            m_OriginalMaxSize = callerMaxSize ?? k_MaxWindowSize;

            // Calculate initial size based on the current level (not entire tree)
            var initialSize = CalculateWindowSize(buttonRect);

            // Lock the window to the calculated size; a subsequent navigation will widen
            // the bounds again via ResizeWindowForCurrentLevel.
            minSize = initialSize;
            maxSize = initialSize;

            ShowAsDropDown(buttonRect, initialSize, GetLocationPriority());

            if (setInitialSelectionPosition)
            {
                m_InitialSelectionPosition = m_Gui.GetSelectionHeight(m_DataSource, buttonRect);
            }
            wantsMouseMove = true;
        }

        void SetSelectionFromState()
        {
            var selectedIndex = m_State.GetSelectedIndex(m_CurrentlyRenderedTree);
            while (selectedIndex >= 0)
            {
                var child = m_State.GetSelectedChild(m_CurrentlyRenderedTree);
                if (child == null)
                    break;
                if (child.id == m_CurrentlyRenderedTree.id)
                    Debug.LogWarning($"Same id: {child.id} given to both {child.displayName} and {m_CurrentlyRenderedTree.displayName}. Selection may be wrong.");
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
            return CalculateWindowSize(buttonRect, m_CurrentlyRenderedTree, m_OriginalMinSize, m_OriginalMaxSize);
        }

        protected virtual Vector2 CalculateWindowSize(Rect buttonRect, AdvancedDropdownItem currentLevel, Vector2 minBounds, Vector2 maxBounds)
        {
            var size = m_Gui.CalculateContentSize(m_DataSource, currentLevel);
            // Add 1 pixel for each border
            size.x += kBorderThickness * 2;
            size.x += kRightMargin;
            size.y += CalculateDefaultExtraHeight();

            size.y = Mathf.Clamp(size.y, minBounds.y, maxBounds.y);

            var fitRect = ContainerWindow.FitRectToMouseScreen(new Rect(buttonRect.x, buttonRect.y, size.x, size.y), true, null);
            // If the scrollbar is visible, we want to add extra space to compensate it
            if (fitRect.height < size.y)
                size.x += GUI.skin.verticalScrollbar.fixedWidth;

            // Stretch to the width of the button
            if (size.x < buttonRect.width)
            {
                size.x = buttonRect.width;
            }
            if (size.x < minBounds.x)
            {
                size.x = minBounds.x;
            }
            if (size.y < minBounds.y)
            {
                size.y = minBounds.y;
            }

            return new Vector2(size.x, size.y);
        }

        // Height consumed by the search field, header, and top/bottom borders —
        // i.e. everything around the scrollable content area.
        private float CalculateDefaultExtraHeight()
        {
            return (kBorderThickness * 2) + m_Gui.searchHeight + (showHeader ? m_Gui.headerHeight : 0);
        }

        internal void OnGUI()
        {
            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, Styles.background);

            if (m_DirtyList)
            {
                OnDirtyList();
            }

            // Handle resize request during Layout event (when GUI functions are safe to call)
            if (m_NeedsResize && Event.current.type == EventType.Layout)
            {
                m_NeedsResize = false;
                ResizeWindowForCurrentLevel();
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
                m_DataSource.RebuildSearch(searchString, m_CurrentlyRenderedTree);
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
                        if (selected.hasChildren)
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
                if (!hasSearch &&
                    (string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()) || GUI.GetNameOfFocusedControl() == AdvancedDropdownGUI.k_SearchFieldName))
                {
                    if (evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.Backspace)
                    {
                        GoToParent();
                        evt.Use();
                    }
                    else if (evt.keyCode == KeyCode.RightArrow)
                    {
                        var idx = m_State.GetSelectedIndex(m_CurrentlyRenderedTree);
                        if (idx > -1 && m_CurrentlyRenderedTree.childList[idx].hasChildren)
                        {
                            GoToChild();
                        }
                        evt.Use();
                    }
                    else if (evt.keyCode == KeyCode.Escape)
                    {
                        Close();
                        evt.Use();
                    }
                }
            }
        }

        private void CloseWindow()
        {
            if (GetSelectedItem() != null)
                selectionCanceled = null;
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
            return specialKeyboardHandling?.Invoke(evt) ?? false;
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
            m_Gui.areaRect = areaPosition;

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
            for (var i = 0; i < item.childList.Count; i++)
            {
                var child = item.childList[i];
                bool selected = m_State.GetSelectedIndex(item) == i;

                if (child.IsSeparator())
                {
                    m_Gui.DrawLineSeparator();
                }
                else if (child is AdvancedDropdownItem.HelpBoxDropdownItem helpBox)
                {
                    m_Gui.DrawHelpBox(helpBox);
                }
                else
                {
                    m_Gui.DrawItem(child, child.displayName, child.icon, child.enabled, child.hasChildren, selected, hasSearch);
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
                    if (selectedChild.hasChildren)
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
            GUI.FocusControl("");
            if (m_ViewsStack.Count == 0)
                return;
            m_LastTime = DateTime.Now.Ticks;
            if (m_NewAnimTarget > 0)
                m_NewAnimTarget = -1 + m_NewAnimTarget;
            else
                m_NewAnimTarget = -1;
            m_AnimationTree = m_CurrentlyRenderedTree;
            m_CurrentlyRenderedTree = m_ViewsStack.Pop();

            // Request resize on next Layout event (safe to call GUI functions)
            m_NeedsResize = true;
        }

        private void GoToChild()
        {
            GUI.FocusControl("");
            m_ViewsStack.Push(m_CurrentlyRenderedTree);
            m_LastTime = DateTime.Now.Ticks;
            if (m_NewAnimTarget < 0)
                m_NewAnimTarget = 1 + m_NewAnimTarget;
            else
                m_NewAnimTarget = 1;
            m_AnimationTree = m_CurrentlyRenderedTree;
            m_CurrentlyRenderedTree = m_State.GetSelectedChild(m_CurrentlyRenderedTree);

            // Request resize on next Layout event (safe to call GUI functions)
            m_NeedsResize = true;
        }

        private void ResizeWindowForCurrentLevel()
        {
            // Safety check: don't resize if tree is null
            if (m_CurrentlyRenderedTree == null)
            {
                return;
            }

            var newSize = CalculateWindowSize(m_ButtonRectScreenPos);

            // Skip if computed size matches current window size — common for subclasses that pin the window to a fixed size
            const float kEpsilon = 0.5f;
            if (Mathf.Abs(newSize.x - position.width) < kEpsilon && Mathf.Abs(newSize.y - position.height) < kEpsilon)
                return;

            // Re-run the popup placement so the window stays attached to the button
            // even when ShowAsDropDown originally shifted it up/left to fit the screen.
            var newPos = ShowAsDropDownFitToScreen(m_ButtonRectScreenPos, newSize, GetLocationPriority());
            position = newPos;

            // Lock to the new size, but do not shrink past or grow beyond caller-supplied limits.
            var fittedSize = new Vector2(newPos.width, newPos.height);
            minSize = fittedSize;
            maxSize = fittedSize;
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
