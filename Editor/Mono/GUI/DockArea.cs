// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.StyleSheets;
using UnityEditor.Experimental;
using UnityEditorInternal;
using UnityEngine.UIElements;
using UnityEngine.XR;

namespace UnityEditor
{
    internal class DockArea : HostView, IDropArea
    {
        private static class Styles
        {
            private static readonly StyleBlock tab = EditorResources.GetStyle("tab");
            public static readonly float tabMinWidth = tab.GetFloat(StyleCatalogKeyword.minWidth, 50.0f);
            public static readonly float tabMaxWidth = tab.GetFloat(StyleCatalogKeyword.maxWidth, 150.0f);
            public static readonly float tabWidthPadding = tab.GetFloat(StyleCatalogKeyword.paddingRight);

            public static readonly float tabDragWidth = EditorResources.GetStyle("tab-drag").GetFloat(StyleCatalogKeyword.width, 100.0f);

            public static readonly GUIStyle tabScrollerPrevButton = new GUIStyle("dragtab scroller prev");
            public static readonly GUIStyle tabScrollerNextButton = new GUIStyle("dragtab scroller next");

            public static SVC<float> genericMenuLeftOffset = new SVC<float>("--window-generic-menu-left-offset", 20f);
            public static SVC<float> genericMenuTopOffset = new SVC<float>("--window-generic-menu-top-offset", 20f);
            public static SVC<float> genericMenuFloatingLeftOffset = new SVC<float>("--window-floating-generic-menu-left-offset", 20f);
            public static SVC<float> genericMenuFloatingTopOffset = new SVC<float>("--window-floating-generic-menu-top-offset", 20f);

            public static readonly GUIStyle tabLabel = new GUIStyle("dragtab") { name = "dragtab-label" };
        }

        internal const int kFloatingWindowTopBorderWidth = 2;
        internal const float kTabHeight = 19;
        internal const float kDockHeight = 39;
        internal const float kSideBorders = 1.0f;
        internal const float kBottomBorders = 2.0f;

        // Which pane window would we drop the currently dragged pane over
        static int s_PlaceholderPos;
        // Which pane is currently being dragged around
        static EditorWindow s_DragPane;
        // Where did it come from
        internal static DockArea s_OriginalDragSource;

        // Mouse coords when we started the drag (used to figure out when we should trigger a drag)
        static Vector2 s_StartDragPosition;
        // Are we dragging yet?
        static int s_DragMode;
        // A view that shouldn't be docked to (to make sure we don't attach to a single view with only the tab that we're dragging)
        static internal View s_IgnoreDockingForView = null;

        private static DropInfo s_DropInfo = null;
        private static readonly Hashtable s_GUIContents = new Hashtable();

        [SerializeField] internal List<EditorWindow> m_Panes = new List<EditorWindow>();
        [SerializeField] internal int m_Selected;
        [SerializeField] internal int m_LastSelected;

        [NonSerialized] internal GUIStyle tabStyle = null;
        [NonSerialized] internal GUIStyle firstTabStyle = null;
        [NonSerialized] internal GUIStyle dockTitleBarStyle = null;

        private bool m_IsBeingDestroyed;
        private Rect m_ScrollLeftRect;
        private Rect m_ScrollRightRect;
        private float m_TotalTabWidth;
        private float m_ScrollOffset;
        private float m_HoldScrollOffset;
        private double m_HoldScrollTimestamp;
        private Rect m_TabAreaRect = Rect.zero;

        public int selected
        {
            get { return m_Selected; }
            set { SetSelectedPrivate(value, sendEvents: true); }
        }

        private void SetSelectedPrivate(int value, bool sendEvents)
        {
            if (m_Selected != value)
                m_LastSelected = m_Selected;
            m_Selected = value;
            if (m_Selected >= 0 && m_Selected < m_Panes.Count)
                SetActualViewInternal(m_Panes[m_Selected], sendEvents);
        }

        public DockArea()
        {
            if (m_Panes != null && m_Panes.Count != 0)
                Debug.LogError("m_Panes is filled in DockArea constructor.");
        }

        private void RemoveNullWindows()
        {
            List<EditorWindow> result = new List<EditorWindow>();
            foreach (EditorWindow i in m_Panes)
            {
                if (i != null)
                    result.Add(i);
            }
            m_Panes = result;
            s_GUIContents.Clear();
        }

        internal override void DoWindowDecorationStart()
        {
            // On windows, we want both close window and side resizes.
            // Titlebar dragging is done at the end, so we can drag next to tabs.
            if (window != null)
                window.HandleWindowDecorationStart(windowPosition);
        }

        internal override void DoWindowDecorationEnd()
        {
            if (window != null)
                window.HandleWindowDecorationEnd(windowPosition);
        }

        protected override void OnDestroy()
        {
            // Prevents double-destroy that may be indirectly caused if Close() is called by OnLostFocus()
            m_IsBeingDestroyed = true;

            if (hasFocus)
                Invoke("OnLostFocus");

            actualView = null;

            // Since m_Panes can me modified indirectly by OnDestroy() callbacks, make a copy of it for safe iteration
            var windows = new List<EditorWindow>(m_Panes);

            foreach (EditorWindow w in windows)
            {
                // Avoid destroying a window that has already being destroyed (case 967778)
                if (w == null)
                    continue;

                UnityEngine.Object.DestroyImmediate(w, true);
            }

            m_Panes.Clear();
            s_GUIContents.Clear();

            base.OnDestroy();
        }

        protected override void OnEnable()
        {
            if (m_Panes != null)
            {
                if (m_Panes.Count == 0)
                    m_Selected = 0;
                else
                {
                    // before fix for case 840151, it is possible that out of range selected index was serialized (e.g., case 846859)
                    m_Selected = Math.Min(m_Selected, m_Panes.Count - 1);
                    actualView = m_Panes[m_Selected];
                }
            }

            base.OnEnable();

            if (imguiContainer != null)
            {
                imguiContainer.name = VisualElementUtils.GetUniqueName("Dockarea");
                imguiContainer.tabIndex = -1;
                imguiContainer.focusOnlyIfHasFocusableControls = false;
            }
        }

        public void AddTab(EditorWindow pane, bool sendPaneEvents = true)
        {
            AddTab(m_Panes.Count, pane, sendPaneEvents);
        }

        public void AddTab(int idx, EditorWindow pane, bool sendPaneEvents = true)
        {
            DeregisterSelectedPane(clearActualView: true, sendEvents: true);
            m_Panes.Insert(idx, pane);
            SetSelectedPrivate(idx, sendPaneEvents);
            s_GUIContents.Clear();

            var sp = parent as SplitView;
            if (sp)
                sp.Reflow();

            Invoke("OnAddedAsTab", pane);
            Repaint();
        }

        public void RemoveTab(EditorWindow pane) { RemoveTab(pane, killIfEmpty: true); }
        public void RemoveTab(EditorWindow pane, bool killIfEmpty, bool sendEvents = true)
        {
            Invoke("OnBeforeRemovedAsTab", pane);

            if (actualView == pane)
                DeregisterSelectedPane(clearActualView: true, sendEvents: sendEvents);

            int idx = m_Panes.IndexOf(pane);
            if (idx == -1)
                return; // Pane is not in the window

            m_Panes.Remove(pane);
            s_GUIContents.Clear();

            // Fixup last selected index
            if (idx == m_LastSelected)
                m_LastSelected = m_Panes.Count - 1;
            else if (idx < m_LastSelected || m_LastSelected == m_Panes.Count)
                --m_LastSelected;

            m_LastSelected = Mathf.Clamp(m_LastSelected, 0, m_Panes.Count - 1);

            // Fixup selected index
            m_Selected = idx == m_Selected ? m_LastSelected : m_Panes.IndexOf(actualView);

            if (m_Selected >= 0 && m_Selected < m_Panes.Count)
                actualView = m_Panes[m_Selected];

            Repaint();
            pane.m_Parent = null;
            if (killIfEmpty)
                KillIfEmpty();
            RegisterSelectedPane(sendEvents: true);
        }

        private void KillIfEmpty()
        {
            // if we're empty, remove ourselves
            if (m_Panes.Count != 0)
                return;

            if (parent == null)
            {
                window.InternalCloseWindow();
                return;
            }

            SplitView sw = (SplitView)parent;
            sw.RemoveChildNice(this);

            if (!m_IsBeingDestroyed)
                DestroyImmediate(this, true);

            sw.Cleanup();
        }

        public DropInfo DragOver(EditorWindow window, Vector2 mouseScreenPosition)
        {
            Rect r = screenPosition;
            r.height = kDockHeight;
            if (r.Contains(mouseScreenPosition))
            {
                if (background == null)
                    background = "hostview";
                Rect scr = background.margin.Remove(screenPosition);
                Vector2 pos = mouseScreenPosition - new Vector2(scr.x, scr.y);

                Rect tr = new Rect(0, 0, m_TotalTabWidth, kTabHeight);
                int mPos = GetTabAtMousePos(tabStyle, pos, tr);

                if (s_PlaceholderPos != mPos)
                {
                    Repaint();
                    s_PlaceholderPos = mPos;
                }

                var dragTopOffset = this.window.showMode != ShowMode.MainWindow ? 3 : -1;
                DropInfo di = new DropInfo(this)
                {
                    type = DropInfo.Type.Tab,
                    rect = new Rect(pos.x - Styles.tabDragWidth * .25f + scr.x, tr.y + scr.y + dragTopOffset, Styles.tabDragWidth, tr.height - 2f)
                };

                return di;
            }
            return null;
        }

        public bool PerformDrop(EditorWindow w, DropInfo info, Vector2 screenPos)
        {
            // Don't send focus events to the tab being moved
            s_OriginalDragSource.RemoveTab(w, killIfEmpty: s_OriginalDragSource != this, sendEvents: false);
            int tabInsertIndex = s_PlaceholderPos == -1 || s_PlaceholderPos > m_Panes.Count ? m_Panes.Count : s_PlaceholderPos;
            AddTab(tabInsertIndex, w, sendPaneEvents: false);
            selected = tabInsertIndex;
            return true;
        }

        protected bool floatingWindow
        {
            get
            {
                if (window == null || window.rootView == null)
                    return false;
                return window.showMode != ShowMode.MainWindow;
            }
        }

        bool isTop => windowPosition.yMin <= 0;
        protected bool isTopRightPane => windowPosition.xMax >= Mathf.FloorToInt(window.position.width) && isTop;

        protected override void OldOnGUI()
        {
            EditorGUIUtility.ResetGUIState();

            // Exit if the window was destroyed after entering play mode or on domain-reload.
            if (window == null)
                return;

            var borderSize = GetBorderSize();

            background = "dockarea";

            Rect dockAreaRect = new Rect(0, 0, position.width, position.height);
            Rect wPos = windowPosition;
            Rect containerWindowPosition = window.position;
            containerWindowPosition.width = Mathf.Ceil(containerWindowPosition.width);
            containerWindowPosition.height = Mathf.Ceil(containerWindowPosition.height);

            DrawDockAreaBackground(dockAreaRect);

            var viewRect = UpdateViewRect(dockAreaRect);
            var titleBarRect = new Rect(viewRect.x, dockAreaRect.y, viewRect.width, borderSize.top);
            m_TabAreaRect = new Rect(titleBarRect.x, viewRect.y - kTabHeight, titleBarRect.width - GetExtraButtonsWidth(), kTabHeight);

            DrawDockTitleBarBackground(titleBarRect);
            HandleTabScrolling(m_TabAreaRect);


            float genericMenuLeftOffset = Styles.genericMenuLeftOffset;
            float genericMenuTopOffset = Styles.genericMenuTopOffset;
            if (floatingWindow && isTopRightPane)
            {
                genericMenuLeftOffset = ContainerWindow.buttonStackWidth + Styles.genericMenuFloatingLeftOffset;
                genericMenuTopOffset = Styles.genericMenuFloatingTopOffset;
            }
            ShowGenericMenu(position.width - genericMenuLeftOffset, m_TabAreaRect.y + genericMenuTopOffset);

            DrawTabs(m_TabAreaRect);
            HandleSplitView(); //fogbugz 1169963: in order to easily use the splitter in the gameView, it must be prioritized over DrawView(). Side effect for touch is that splitter picking zones might overlap other controls but the tabs still have higher priority so the user can undock the window in that case
            DrawView(viewRect, dockAreaRect);

            DrawTabScrollers(m_TabAreaRect);

            EditorGUI.ShowRepaints();
            Highlighter.ControlHighlightGUI(this);
        }

        private void DrawView(Rect viewRect, Rect dockAreaRect)
        {
            InvokeOnGUI(dockAreaRect, viewRect);
            RenderToHMDIfNecessary();
        }

        private void DrawTabs(Rect tabAreaRect)
        {
            Rect clipRect = tabAreaRect;

            if (floatingWindow && isTop)
                clipRect.yMin = 0;

            // If the left scroll button is visible then clip the tabs a bit more because of the top left border radius of the scroll left button
            if (m_ScrollOffset > 0f)
                clipRect.xMin += 3;

            using (new GUI.ClipScope(clipRect, new Vector2(-m_ScrollOffset - 1f, 0)))
            {
                if (tabStyle == null)
                    tabStyle = "dragtab";

                if (firstTabStyle == null)
                    firstTabStyle = EditorStyles.s_Current.GetStyle("dragtab first");

                var totalTabWidth = DragTab(tabAreaRect, m_ScrollOffset, tabStyle, firstTabStyle);
                if (totalTabWidth > 0f)
                    m_TotalTabWidth = totalTabWidth;
                tabStyle = "dragtab";
            }
        }

        private Rect UpdateViewRect(Rect dockAreaRect)
        {
            var border = GetBorderSize();

            var viewRect = new Rect(
                dockAreaRect.x + border.left,
                dockAreaRect.y + border.top,
                dockAreaRect.width - (border.left + border.right),
                dockAreaRect.height - (border.top + border.bottom));

            if (selected >= 0 && selected < m_Panes.Count)
                m_Panes[selected].m_Pos = new Rect(GUIUtility.GUIToScreenPoint(Vector2.zero), viewRect.size);

            return viewRect;
        }

        private void DrawDockAreaBackground(Rect dockAreaRect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                var backgroundRect = dockAreaRect;
                backgroundRect.y = 0;
                background.Draw(backgroundRect, GUIContent.none, 0);
            }
        }

        private void DrawDockTitleBarBackground(Rect titleBarRect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (dockTitleBarStyle == null)
                    dockTitleBarStyle = "dockHeader";
                dockTitleBarStyle.Draw(titleBarRect, GUIContent.none, 0);
            }
        }

        private void SetupHoldScrollerUpdate(float clickOffset)
        {
            m_HoldScrollOffset = clickOffset;
            m_HoldScrollTimestamp = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnMouseDownHoldScroller;
        }

        private void HandleTabScrolling(Rect tabAreaRect)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                const float scrollOffsetShift = 20f;
                if (m_ScrollLeftRect.Contains(Event.current.mousePosition))
                {
                    SetupHoldScrollerUpdate(-2f);
                    m_ScrollOffset = Mathf.Max(0f, m_ScrollOffset - scrollOffsetShift);
                    Event.current.Use();
                }
                else if (m_ScrollRightRect.Contains(Event.current.mousePosition))
                {
                    SetupHoldScrollerUpdate(2f);
                    m_ScrollOffset = Mathf.Min(m_ScrollOffset + scrollOffsetShift, m_TotalTabWidth - tabAreaRect.width);
                    Event.current.Use();
                }
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                m_HoldScrollOffset = 0f;
                EditorApplication.update -= OnMouseDownHoldScroller;
            }
        }

        private void DrawTabScroller(Rect scrollRect, GUIStyle tabScroller)
        {
            tabScroller.Draw(scrollRect, scrollRect.Contains(Event.current.mousePosition), false, false, false);
        }

        private float GetExtraButtonsWidth()
        {
            return (HasExtraDockAreaButton() ? 42f : 20f) + (floatingWindow && isTopRightPane ? ContainerWindow.buttonStackWidth : 0f);
        }

        private void DrawTabScrollers(Rect tabAreaRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (m_TotalTabWidth <= tabAreaRect.xMax)
                m_ScrollOffset = 0;

            float scrollerButtonHeight = tabAreaRect.height;
            if (m_ScrollOffset > 0f)
            {
                m_ScrollLeftRect = new Rect(tabAreaRect.xMin, tabAreaRect.yMin , 16f, scrollerButtonHeight);
                DrawTabScroller(m_ScrollLeftRect, Styles.tabScrollerPrevButton);
            }
            else
                m_ScrollLeftRect.width = 0;

            if (m_TotalTabWidth > tabAreaRect.width && m_ScrollOffset < (m_TotalTabWidth - tabAreaRect.width))
            {
                m_ScrollRightRect = new Rect(tabAreaRect.xMax - 16f, tabAreaRect.yMin, 16f, scrollerButtonHeight);
                DrawTabScroller(m_ScrollRightRect, Styles.tabScrollerNextButton);
            }
            else
                m_ScrollRightRect.width = 0;
        }

        private void HandleSplitView()
        {
            // Add CursorRects
            SplitView sp = parent as SplitView;

            if (Event.current.type == EventType.Repaint && sp)
            {
                View view = this;
                while (sp)
                {
                    int id = sp.controlID;

                    if (id == GUIUtility.hotControl || GUIUtility.hotControl == 0)
                    {
                        int idx = sp.IndexOfChild(view);
                        if (sp.vertical)
                        {
                            if (idx != 0)
                                EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, SplitView.kGrabDist), MouseCursor.SplitResizeUpDown, id);
                            if (idx != sp.children.Length - 1)
                                EditorGUIUtility.AddCursorRect(
                                    new Rect(0, position.height - SplitView.kGrabDist, position.width, SplitView.kGrabDist),
                                    MouseCursor.SplitResizeUpDown, id);
                        }
                        else // horizontal
                        {
                            if (idx != 0)
                                EditorGUIUtility.AddCursorRect(new Rect(0, 0, SplitView.kGrabDist, position.height), MouseCursor.SplitResizeLeftRight,
                                    id);
                            if (idx != sp.children.Length - 1)
                                EditorGUIUtility.AddCursorRect(
                                    new Rect(position.width - SplitView.kGrabDist, 0, SplitView.kGrabDist, position.height),
                                    MouseCursor.SplitResizeLeftRight, id);
                        }
                    }

                    view = sp;
                    sp = sp.parent as SplitView;
                }

                // reset
                sp = (SplitView)parent;
            }

            if (sp)
            {
                Event e = new Event(Event.current);
                e.mousePosition += new Vector2(position.x, position.y);
                sp.SplitGUI(e);
                if (e.type == EventType.Used)
                    Event.current.Use();
            }
        }

        private void OnMouseDownHoldScroller()
        {
            if (m_HoldScrollOffset == 0f)
                return;

            double timeSinceStartup = EditorApplication.timeSinceStartup;
            float dt = (float)(timeSinceStartup - m_HoldScrollTimestamp);
            float maxScrollOffset = m_TotalTabWidth - m_TabAreaRect.width;
            m_HoldScrollTimestamp = timeSinceStartup;
            m_ScrollOffset = Mathf.Max(0f, Mathf.Min(m_ScrollOffset + m_HoldScrollOffset * dt * 250.0f, maxScrollOffset));
            Repaint();
        }

        private void RenderToHMDIfNecessary()
        {
            if (Event.current.type != EventType.Repaint ||
                !XRSettings.isDeviceActive ||
                !EditorApplication.isPlaying ||
                EditorApplication.isPaused ||
                (actualView is GameView))
                return;

            foreach (var pane in m_Panes)
            {
                if (pane is GameView)
                {
                    var gameView = pane as GameView;
                    gameView.RenderToHMDOnly();
                }
            }
        }

        protected override void SetActualViewPosition(Rect newPos)
        {
            //Sit down and let me tell you the intent of this.
            //the position property should return the "usable" dimension of the window
            //In the case of DockArea i not the whole window, because we reserve some space on the top for the
            //window tabs. Because of that we want to report a DockArea.position that does not contain the height of that bar.
            //This is exactly what we try to do here, remove the tab area from the rect passed in.
            //Note that this code path so far is only used when the HostView, SplitView, or any "parent" view changes its dimensions and
            //tries to propagate that to its "children"
            //There are other code paths that also set the Dockarea.position, that won't go through here.
            //But its important that they all return the same values.
            var adjustedDockAreaClientRect = borderSize.Remove(newPos);
            base.SetActualViewPosition(adjustedDockAreaClientRect);
        }

        private void Maximize(object userData)
        {
            EditorWindow editorWindow = userData as EditorWindow;
            if (editorWindow != null)
            {
                WindowLayout.Maximize(editorWindow);
            }
        }

        internal void Close(object userData)
        {
            EditorWindow editorWindow = userData as EditorWindow;
            if (editorWindow != null)
            {
                editorWindow.Close();
            }
            else
            {
                RemoveTab(null, false);
                KillIfEmpty();
            }
        }

        private bool AllowTabAction()
        {
            int mainWindowPaneCount = 0;

            ContainerWindow w = ContainerWindow.windows.FirstOrDefault(e => e.showMode == ShowMode.MainWindow);
            if (w != null)
            {
                foreach (View view in w.rootView.allChildren)
                {
                    DockArea da = view as DockArea;
                    if (da != null)
                    {
                        mainWindowPaneCount += da.m_Panes.Count;
                        if (mainWindowPaneCount > 1) return true;
                    }
                }
            }

            return false;
        }

        protected override void AddDefaultItemsToMenu(GenericMenu menu, EditorWindow view)
        {
            base.AddDefaultItemsToMenu(menu, view);

            if (parent.window.showMode == ShowMode.MainWindow)
                menu.AddItem(EditorGUIUtility.TrTextContent("Maximize"), !(parent is SplitView), Maximize, view);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Maximize"));

            bool closeAllowed = (window.showMode != ShowMode.MainWindow || AllowTabAction());
            if (closeAllowed)
                menu.AddItem(EditorGUIUtility.TrTextContent("Close Tab"), false, Close, view);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Close Tab"));
            menu.AddSeparator("");

            IEnumerable<Type> types = GetPaneTypes();
            GUIContent baseContent = EditorGUIUtility.TrTextContent("Add Tab");
            foreach (Type t in types)
            {
                if (t == null)
                {
                    menu.AddSeparator(baseContent.text + "/");
                    continue;
                }

                GUIContent entry = new GUIContent(EditorWindow.GetLocalizedTitleContentFromType(t)); // make a copy since we modify the text below
                entry.text = baseContent.text + "/" + entry.text;
                menu.AddItem(entry, false, AddTabToHere, t);
            }

            menu.AddSeparator("");
            AddUIElementsDebuggerToMenu(menu);
        }

        void AddTabToHere(object userData)
        {
            EditorWindow win = (EditorWindow)CreateInstance((System.Type)userData);
            AddTab(win);
        }

        private float GetTabWidth(GUIStyle tabStyle, int tabIndex)
        {
            Debug.Assert(0 <= tabIndex && tabIndex < m_Panes.Count);

            float minWidth, expectedWidth;
            tabStyle.CalcMinMaxWidth(m_Panes[tabIndex].titleContent, out minWidth, out expectedWidth);
            return Mathf.Max(Mathf.Min(expectedWidth, Styles.tabMaxWidth), Styles.tabMinWidth) + Styles.tabWidthPadding;
        }

        private int GetTabAtMousePos(GUIStyle tabStyle, Vector2 mousePos, Rect tabAreaRect)
        {
            Rect tabRect = Rect.zero;
            return GetTabAtMousePos(tabStyle, mousePos + new Vector2(m_ScrollOffset, 0), tabAreaRect, 0f, ref tabRect);
        }

        private int GetTabAtMousePos(GUIStyle tabStyle, Vector2 mousePos, float scrollOffset, Rect tabAreaRect)
        {
            Rect tabRect = Rect.zero;
            return GetTabAtMousePos(tabStyle, mousePos, tabAreaRect, scrollOffset, ref tabRect);
        }

        private int GetTabAtMousePos(GUIStyle tabStyle, Vector2 mousePos, Rect tabAreaRect, float scrollOffset, ref Rect tabRect)
        {
            if (!tabAreaRect.Contains(mousePos - new Vector2(scrollOffset, 0)))
                return -1;

            float xPos = tabAreaRect.xMin;
            for (int i = 0; i < m_Panes.Count; i++)
            {
                if (s_DragPane == m_Panes[i])
                    continue;

                float tabWidth = GetTabWidth(tabStyle, i);
                if (xPos <= mousePos.x && mousePos.x < xPos + tabWidth)
                {
                    tabRect = new Rect(xPos, tabAreaRect.yMin + tabStyle.margin.top, tabWidth, tabAreaRect.height);
                    return i;
                }

                xPos += tabWidth;
            }

            return -1;
        }

        // Hack to get around Unity crashing when we have circular references in saved stuff
        internal override void Initialize(ContainerWindow win)
        {
            base.Initialize(win);
            RemoveNullWindows();
            foreach (EditorWindow i in m_Panes)
                i.m_Parent = this;
        }

        private static void CheckDragWindowExists()
        {
            if (s_DragMode == 1 && !PaneDragTab.get.m_Window)
            {
                s_OriginalDragSource.RemoveTab(s_DragPane);
                DestroyImmediate(s_DragPane);
                PaneDragTab.get.Close();
                GUIUtility.hotControl = 0;
                ResetDragVars();
            }
        }

        private float DragTab(Rect tabAreaRect, float scrollOffset, GUIStyle tabStyle, GUIStyle firstTabStyle)
        {
            Event evt = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);

            // Detect if hotcontrol was cleared while dragging (happens when pressing Esc).
            // We do not listen for the Escape keydown event because it is sent to the dragged window (not this dockarea)
            if (s_DragMode != 0 && GUIUtility.hotControl == 0)
            {
                PaneDragTab.get.Close();
                ResetDragVars();
            }

            float xPos = 0f;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (GUIUtility.hotControl == 0)
                    {
                        int sel = GetTabAtMousePos(tabStyle, evt.mousePosition, scrollOffset, tabAreaRect);
                        if (sel != -1 && sel < m_Panes.Count)
                        {
                            switch (evt.button)
                            {
                                case 0:
                                    if (selected != sel)
                                        selected = sel;

                                    GUIUtility.hotControl = id;
                                    s_StartDragPosition = evt.mousePosition;
                                    s_DragMode = 0;
                                    evt.Use();
                                    break;
                                case 2:
                                    m_Panes[sel].Close();
                                    evt.Use();
                                    break;
                            }
                        }
                    }
                    break;
                case EventType.ContextClick:
                    if (GUIUtility.hotControl == 0)
                    {
                        int sel = GetTabAtMousePos(tabStyle, evt.mousePosition, scrollOffset, tabAreaRect);
                        if (sel != -1 && sel < m_Panes.Count)
                            PopupGenericMenu(m_Panes[sel], new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0));
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        Vector2 delta = evt.mousePosition - s_StartDragPosition;
                        evt.Use();
                        Rect screenRect = screenPosition;

                        // if we're not tab dragging yet, check to see if we should start

                        // check if we're allowed to drag tab
                        bool dragAllowed = (window.showMode != ShowMode.MainWindow || AllowTabAction());

                        if (s_DragMode == 0 && delta.sqrMagnitude > 99 && dragAllowed)
                        {
                            s_DragMode = 1;
                            s_PlaceholderPos = selected;
                            s_DragPane = m_Panes[selected];

                            // If we're moving the only editorwindow in this dockarea, we'll be destroyed - so it looks silly if we can attach as children of ourselves
                            s_IgnoreDockingForView = m_Panes.Count == 1 ? this : null;

                            s_OriginalDragSource = this;
                            float tabWidth = GetTabWidth(tabStyle, selected);
                            PaneDragTab.get.Show(
                                new Rect(tabAreaRect.x + screenRect.x + tabWidth * selected, tabAreaRect.y + screenRect.y, tabWidth, tabAreaRect.height - 1f),
                                s_DragPane.titleContent,
                                position.size,
                                GUIUtility.GUIToScreenPoint(evt.mousePosition)
                            );
                            EditorApplication.update += CheckDragWindowExists;
                            // We just showed a window. Exit the GUI because the window might be
                            // repainting already (esp. on Windows)
                            GUIUtility.ExitGUI();
                        }
                        if (s_DragMode == 1)
                        {
                            // Go over all container windows, ask them to dock the window.
                            DropInfo di = null;
                            ContainerWindow[] windows = ContainerWindow.windows;
                            Vector2 screenMousePos = GUIUtility.GUIToScreenPoint(evt.mousePosition);
                            ContainerWindow win = null;
                            foreach (ContainerWindow w in windows)
                            {
                                var rootSplitView = w.rootSplitView;
                                if (rootSplitView == null)
                                    continue;

                                di = rootSplitView.DragOverRootView(screenMousePos);

                                if (di == null)
                                {
                                    foreach (View view in w.rootView.allChildren)
                                    {
                                        IDropArea ida = view as IDropArea;
                                        if (ida != null)
                                            di = ida.DragOver(s_DragPane, screenMousePos);

                                        if (di != null)
                                            break;
                                    }
                                }

                                if (di != null)
                                {
                                    win = w;
                                    break;
                                }
                            }
                            // Ok, we couldn't find anything, let's create a simplified DropIn
                            if (di == null)
                            {
                                di = new DropInfo(null);
                            }

                            if (di.type != DropInfo.Type.Tab)
                                s_PlaceholderPos = -1;

                            s_DropInfo = di;

                            // Handle the window getting closed mid-drag
                            if (PaneDragTab.get.m_Window)
                                PaneDragTab.get.SetDropInfo(di, screenMousePos, win);
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        Vector2 screenMousePos = GUIUtility.GUIToScreenPoint(evt.mousePosition);
                        if (s_DragMode != 0)
                        {
                            // This is where we want to insert it.
                            s_DragMode = 0;
                            PaneDragTab.get.Close();
                            EditorApplication.update -= CheckDragWindowExists;

                            // Try to tell the current DPZ
                            if (s_DropInfo?.dropArea != null)
                            {
                                Invoke("OnTabDetached", s_DragPane);
                                s_DropInfo.dropArea.PerformDrop(s_DragPane, s_DropInfo, screenMousePos);
                            }
                            else
                            {
                                EditorWindow w = s_DragPane;

                                ResetDragVars();

                                // The active tab that we're moving to the new window stays focused at all times.
                                // Do not remove focus from the tab being detached.
                                RemoveTab(w, killIfEmpty: true, sendEvents: false);
                                Rect wPos = w.position;
                                wPos.x = screenMousePos.x - wPos.width * .5f;
                                wPos.y = screenMousePos.y - wPos.height * .5f;

                                // don't put windows top outside of the screen, on mac OS handles this
                                if (Application.platform == RuntimePlatform.WindowsEditor)
                                    wPos.y = Mathf.Max(InternalEditorUtility.GetBoundsOfDesktopAtPoint(screenMousePos).y, wPos.y);

                                // Don't call OnFocus on the tab when it is moved to the new window
                                EditorWindow.CreateNewWindowForEditorWindow(w, loadPosition: false, showImmediately: false, setFocus: false);

                                w.position = w.m_Parent.window.FitWindowRectToScreen(wPos, true, true);

                                GUIUtility.hotControl = 0;
                                GUIUtility.ExitGUI();
                            }
                            ResetDragVars();
                        }

                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }

                    break;

                case EventType.Repaint:
                    xPos = tabAreaRect.xMin;
                    if (actualView)
                    {
                        for (int i = 0, drawNum = 0; i < m_Panes.Count; i++)
                        {
                            // If we're dragging the tab we're about to draw, don't do that (handled by some window)
                            if (s_DragPane == m_Panes[i])
                                continue;

                            var style = tabStyle;

                            if (i == 0)
                                style = firstTabStyle;

                            // If we need space for inserting a tab here, skip some horizontal
                            if (s_DropInfo != null && ReferenceEquals(s_DropInfo.dropArea, this) && s_PlaceholderPos == drawNum)
                                xPos += Styles.tabDragWidth;

                            xPos += DrawTab(tabAreaRect, style, i, xPos);
                            drawNum++;
                        }
                    }
                    else
                    {
                        Rect r = new Rect(xPos, tabAreaRect.yMin, Styles.tabDragWidth, tabAreaRect.height);
                        float roundR = Mathf.Round(r.x);
                        Rect r2 = new Rect(roundR, r.y, Mathf.Round(r.x + r.width) - roundR, r.height);
                        tabStyle.Draw(r2, "Failed to load", false, true, true, false);
                    }
                    break;
            }
            selected = Mathf.Clamp(selected, 0, m_Panes.Count - 1);

            return xPos;
        }

        private GUIContent GetTruncatedTabContent(int tabIndex)
        {
            var tabContent = m_Panes[tabIndex].titleContent;
            if (tabContent.text.Length > 10)
            {
                var text = tabContent.text;
                GUIContent gc = (GUIContent)s_GUIContents[text];
                if (gc != null)
                    return gc;

                int cappedMaxChars = tabStyle.GetNumCharactersThatFitWithinWidth(text, Styles.tabMaxWidth);
                if (text.Length > cappedMaxChars)
                {
                    gc = new GUIContent(text.Substring(0, Mathf.Max(3, Mathf.Min(cappedMaxChars - 2, text.Length))) + "\u2026", tabContent.image,
                        String.IsNullOrEmpty(tabContent.tooltip) ? text : tabContent.tooltip);
                    s_GUIContents[text] = gc;
                    return gc;
                }
            }
            return tabContent;
        }

        private float DrawTab(Rect tabRegionRect, GUIStyle tabStyle, int tabIndex, float xPos)
        {
            float tabWidth = GetTabWidth(tabStyle, tabIndex);
            Rect tabPositionRect = new Rect(xPos, tabRegionRect.yMin + tabStyle.margin.top, tabWidth, tabRegionRect.height);
            float roundedPosX = Mathf.Round(tabPositionRect.x);
            float roundedWidth = Mathf.Round(tabPositionRect.x + tabPositionRect.width) - roundedPosX;

            bool isActive = m_Panes[tabIndex] == EditorWindow.focusedWindow;
            Rect tabContentRect = new Rect(roundedPosX, tabPositionRect.y, roundedWidth, tabPositionRect.height);
            tabStyle.Draw(tabContentRect, tabContentRect.Contains(Event.current.mousePosition), isActive, tabIndex == selected, false);
            GUI.Label(tabPositionRect, GetTruncatedTabContent(tabIndex), Styles.tabLabel);

            var unclippedContentRect = GUIClip.UnclipToWindow(tabContentRect);

            MarkHotRegion(unclippedContentRect);

            return tabWidth;
        }

        internal RectOffset GetBorderSizeInternal()
        {
            if (!window)
                return m_BorderSize;

            Rect containerWindowPosition = window.position;
            containerWindowPosition.width = Mathf.FloorToInt(containerWindowPosition.width);
            containerWindowPosition.height = Mathf.FloorToInt(containerWindowPosition.height);

            bool customBorder = floatingWindow && windowPosition.y == 0;
            bool isBottomTab = windowPosition.yMax == containerWindowPosition.height;

            // Reset
            m_BorderSize.left = m_BorderSize.right = m_BorderSize.top = m_BorderSize.bottom = 0;

            Rect r = windowPosition;
            if (r.xMin != 0)
                m_BorderSize.left += (int)kSideBorders;
            if (r.xMax != Mathf.FloorToInt(window.position.width))
                m_BorderSize.right += (int)kSideBorders;

            m_BorderSize.top = (int)kTabHeight + (customBorder ? kFloatingWindowTopBorderWidth : 0);
            m_BorderSize.bottom = isBottomTab ? 0 : (int)kBottomBorders;

            return m_BorderSize;
        }

        protected override RectOffset GetBorderSize()
        {
            return GetBorderSizeInternal();
        }

        private static void ResetDragVars()
        {
            s_DragPane = null;
            s_DropInfo = null;
            s_PlaceholderPos = -1;
            s_DragMode = 0;
            s_OriginalDragSource = null;
        }
    }

    internal class MaximizedHostView : HostView
    {
        static class Styles
        {
            public static readonly GUIStyle titleBackground = "dockHeader";
            public static readonly GUIStyle titleLabel = new GUIStyle("dragtab") { name = "dragtab-label" };
            public static readonly GUIStyle background = "dockarea";
            public static SVC<float> genericMenuLeftOffset = new SVC<float>("--window-generic-menu-left-offset", 20f);
            public static SVC<float> genericMenuTopOffset = new SVC<float>("--window-generic-menu-top-offset", 20f);
        }

        protected override void OldOnGUI()
        {
            // Call reset GUI state as first thing so GUI.color is correct when drawing window decoration.
            EditorGUIUtility.ResetGUIState();

            Rect maximizedViewRect = Rect.zero;

            maximizedViewRect.size = position.size;
            maximizedViewRect = Styles.background.margin.Remove(maximizedViewRect);

            Rect backRect = new Rect(maximizedViewRect.x + 1, maximizedViewRect.y, maximizedViewRect.width - 2, DockArea.kTabHeight);
            if (Event.current.type == EventType.Repaint)
            {
                Styles.background.Draw(maximizedViewRect, GUIContent.none, false, false, false, false);
                Styles.titleBackground.Draw(backRect, false, false, true, hasFocus);
                GUI.Label(backRect, actualView.titleContent, Styles.titleLabel);
            }

            if (Event.current.type == EventType.ContextClick && backRect.Contains(Event.current.mousePosition))
                PopupGenericMenu(actualView, new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0));

            ShowGenericMenu(position.width - Styles.genericMenuLeftOffset, backRect.yMin + Styles.genericMenuTopOffset);

            const float topBottomPadding = 0f;
            Rect viewRect = maximizedViewRect;
            viewRect.y = backRect.yMax - topBottomPadding;
            viewRect.height = position.height - backRect.yMax + topBottomPadding;

            if (actualView)
                actualView.m_Pos = new Rect(GUIUtility.GUIToScreenPoint(Vector2.zero), viewRect.size);

            InvokeOnGUI(maximizedViewRect, viewRect);
        }

        protected override RectOffset GetBorderSize()
        {
            m_BorderSize.left = 0;
            m_BorderSize.right = 0;
            m_BorderSize.top = (int)DockArea.kTabHeight;
            // Aras: I don't really know why, but this makes GUI be actually correct.
            m_BorderSize.bottom = 4;

            return m_BorderSize;
        }

        void Unmaximize(object userData)
        {
            EditorWindow ew = ((EditorWindow)userData);
            WindowLayout.Unmaximize(ew);
        }

        protected override void AddDefaultItemsToMenu(GenericMenu menu, EditorWindow window)
        {
            base.AddDefaultItemsToMenu(menu, window);

            menu.AddItem(EditorGUIUtility.TrTextContent("Maximize"), !(parent is SplitView), Unmaximize, window);
            menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Close Tab"));
            menu.AddSeparator("");
            IEnumerable<Type> types = GetPaneTypes();

            GUIContent baseContent = EditorGUIUtility.TrTextContent("Add Tab");
            foreach (Type t in types)
            {
                if (t == null)
                {
                    menu.AddSeparator(baseContent.text + "/");
                    continue;
                }

                GUIContent entry = new GUIContent(EditorWindow.GetLocalizedTitleContentFromType(t)); // make a copy since we modify the text below
                entry.text = baseContent.text + "/" + entry.text;
                menu.AddDisabledItem(entry);
            }

            menu.AddSeparator("");
            AddUIElementsDebuggerToMenu(menu);
        }
    }
}
