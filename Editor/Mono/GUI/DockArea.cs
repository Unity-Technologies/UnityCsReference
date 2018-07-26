// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.XR;

namespace UnityEditor
{
    internal class DockArea : HostView, IDropArea
    {
        internal const float kTabHeight = 17;
        internal const float kDockHeight = 39;
        const float kSideBorders = 2.0f;
        const float kBottomBorders = 2.0f;
        const float kWindowButtonsWidth = 40.0f; // Context & Lock Inspector Buttons

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

        static DropInfo s_DropInfo = null;

        [SerializeField]
        internal List<EditorWindow> m_Panes = new List<EditorWindow>();
        [SerializeField]
        internal int m_Selected;
        [SerializeField]
        internal int m_LastSelected;

        [NonSerialized]
        internal GUIStyle tabStyle = null;

        bool m_IsBeingDestroyed;

        public int selected
        {
            get { return m_Selected; }
            set
            {
                if (m_Selected != value)
                    m_LastSelected = m_Selected;
                m_Selected = value;
                if (m_Selected >= 0 && m_Selected < m_Panes.Count)
                    actualView = m_Panes[m_Selected];
            }
        }

        public DockArea()
        {
            if (m_Panes != null && m_Panes.Count != 0)
                Debug.LogError("m_Panes is filled in DockArea constructor.");
        }

        void RemoveNullWindows()
        {
            List<EditorWindow> result = new List<EditorWindow>();
            foreach (EditorWindow i in m_Panes)
            {
                if (i != null)
                    result.Add(i);
            }
            m_Panes = result;
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

            imguiContainer.name = VisualElementUtils.GetUniqueName("Dockarea");
        }

        protected override void UpdateViewMargins(EditorWindow view)
        {
            base.UpdateViewMargins(view);

            if (view == null)
                return;

            RectOffset margins = GetBorderSize();

            IStyle style = view.rootVisualContainer.style;
            style.positionTop = margins.top;
            style.positionBottom = margins.bottom;
            style.positionLeft = margins.left;
            style.positionRight = margins.right;
            style.positionType = PositionType.Absolute;
        }


        public void AddTab(EditorWindow pane)
        {
            AddTab(m_Panes.Count, pane);
        }

        public void AddTab(int idx, EditorWindow pane)
        {
            DeregisterSelectedPane(true);
            m_Panes.Insert(idx, pane);
            selected = idx;

            var sp = parent as SplitView;
            if (sp)
                sp.Reflow();

            Repaint();
        }

        public void RemoveTab(EditorWindow pane) { RemoveTab(pane, true); }
        public void RemoveTab(EditorWindow pane, bool killIfEmpty)
        {
            if (actualView == pane)
                DeregisterSelectedPane(true);
            int idx = m_Panes.IndexOf(pane);
            if (idx == -1)
                return; // Pane is not in the window

            m_Panes.Remove(pane);

            // Fixup last selected index
            if (idx == m_LastSelected)
                m_LastSelected = m_Panes.Count - 1;
            else if (idx < m_LastSelected || m_LastSelected == m_Panes.Count)
                --m_LastSelected;

            m_LastSelected = Mathf.Clamp(m_LastSelected, 0, m_Panes.Count - 1);

            // Fixup selected index
            if (idx == m_Selected)
                m_Selected = m_LastSelected;
            else
                m_Selected = m_Panes.IndexOf(actualView);

            if (m_Selected >= 0 && m_Selected < m_Panes.Count)
                actualView = m_Panes[m_Selected];

            Repaint();
            pane.m_Parent = null;
            if (killIfEmpty)
                KillIfEmpty();
            RegisterSelectedPane();
        }

        void KillIfEmpty()
        {
            // if we're empty, remove ourselves
            if (m_Panes.Count != 0)
                return;

            if (parent == null)
            {
                window.InternalCloseWindow();
                return;
            }

            SplitView sw = parent as SplitView;
            ICleanuppable p = parent as ICleanuppable;
            sw.RemoveChildNice(this);

            if (!m_IsBeingDestroyed)
                DestroyImmediate(this, true);

            if (p != null)
                p.Cleanup();
        }

        Rect tabRect { get { return new Rect(0, 0, position.width, kTabHeight); } }

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

                Rect tr = tabRect;
                int mPos = GetTabAtMousePos(pos, tr);
                float w = GetTabWidth(tr.width);

                if (s_PlaceholderPos != mPos)
                {
                    Repaint();
                    s_PlaceholderPos = mPos;
                }

                DropInfo di = new DropInfo(this);
                di.type = DropInfo.Type.Tab;
                di.rect = new Rect(pos.x - w * .25f + scr.x, tr.y + scr.y, w, tr.height);

                return di;
            }
            return null;
        }

        public bool PerformDrop(EditorWindow w, DropInfo info, Vector2 screenPos)
        {
            s_OriginalDragSource.RemoveTab(w, s_OriginalDragSource != this);
            int pos2 = s_PlaceholderPos > m_Panes.Count ? m_Panes.Count : s_PlaceholderPos;
            AddTab(pos2, w);
            selected = pos2;
            return true;
        }

        protected override void OldOnGUI()
        {
            ClearBackground();
            // Call reset GUI state as first thing so GUI.color is correct when drawing window decoration.
            EditorGUIUtility.ResetGUIState();

            // Exit if the window was destroyed after entering play mode or on domain-reload.
            if (window == null)
                return;

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
                sp = parent as SplitView;
            }
            bool customBorder = false;
            if (window.rootView.GetType() != typeof(MainView))
            {
                customBorder = true;
                if (windowPosition.y == 0)
                    background = "dockareaStandalone";
                else
                    background = "dockarea";
            }
            else
                background = "dockarea";

            if (sp)
            {
                Event e = new Event(Event.current);
                e.mousePosition += new Vector2(position.x, position.y);
                sp.SplitGUI(e);
                if (e.type == EventType.Used)
                    Event.current.Use();
            }
            Rect r = background.margin.Remove(new Rect(0, 0, position.width, position.height));
            r.x = background.margin.left;
            r.y = background.margin.top;
            Rect wPos = windowPosition;
            float sideBorder = kSideBorders;
            if (wPos.x == 0)
            {
                r.x -= sideBorder;
                r.width += sideBorder;
            }
            if (wPos.xMax == window.position.width)
            {
                r.width += sideBorder;
            }

            if (wPos.yMax == window.position.height)
            {
                r.height += customBorder ? 2f : kBottomBorders;
            }

            if (Event.current.type == EventType.Repaint)
            {
                background.Draw(r, GUIContent.none, 0);
            }

            if (tabStyle == null)
                tabStyle = "dragtab";

            if (m_Panes.Count > 0)
            {
                // Set up the pane's position, so its GUI can use this

                // Contents:
                // scroll it by -1, -1 so we top & left 1px gets culled (they are drawn already by the us, so we want to
                // not have them here (thing that the default skin relies on)
                BeginOffsetArea(new Rect(r.x + 2, r.y + kTabHeight, r.width - 4, r.height - kTabHeight - 2), GUIContent.none, "TabWindowBackground");

                Vector2 basePos = GUIUtility.GUIToScreenPoint(Vector2.zero);
                Rect p = borderSize.Remove(position);
                p.x = basePos.x;
                p.y = basePos.y;

                if (selected >= 0 && selected < m_Panes.Count)
                {
                    m_Panes[selected].m_Pos = p;
                }

                EndOffsetArea();
            }

            DragTab(new Rect(r.x + 1, r.y, r.width - kWindowButtonsWidth, kTabHeight), tabStyle);

            // TODO: Make this nice - right now this is meant to be overridden by Panes in Layout if they want something else. Inspector does this
            tabStyle = "dragtab";

            ShowGenericMenu();

            if (m_Panes.Count > 0)
            {
                RenderToHMDIfNecessary(r);
                InvokeOnGUI(r);
            }

            EditorGUI.ShowRepaints();
            Highlighter.ControlHighlightGUI(this);
        }

        void RenderToHMDIfNecessary(Rect onGUIPosition)
        {
            if (!XRSettings.isDeviceActive ||
                (actualView is GameView) ||
                !EditorApplication.isPlaying ||
                EditorApplication.isPaused ||
                Event.current.type != EventType.Repaint)
            {
                return;
            }

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
            //Note that this code path so far is only used when the HostView, SplitView, or any "parent" view changes its dimentions and
            //tries to propagate that to its "children"
            //There are other code paths that also set the Dockarea.position, that won't go through here.
            //But its important that they all return the same values.
            var adjustedDockAreaClientRect = borderSize.Remove(newPos);
            base.SetActualViewPosition(adjustedDockAreaClientRect);
        }

        void Maximize(object userData)
        {
            EditorWindow window = userData as EditorWindow;
            if (window != null)
            {
                WindowLayout.Maximize(window);
            }
        }

        internal void Close(object userData)
        {
            EditorWindow window = userData as EditorWindow;
            if (window != null)
            {
                window.Close();
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

            ContainerWindow w = ContainerWindow.windows.First(e => e.showMode == ShowMode.MainWindow);
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
                menu.AddItem(EditorGUIUtility.TextContent("Maximize"), !(parent is SplitView), Maximize, view);
            else
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Maximize"));

            bool closeAllowed = (window.showMode != ShowMode.MainWindow || AllowTabAction());
            if (closeAllowed)
                menu.AddItem(EditorGUIUtility.TextContent("Close Tab"), false, Close, view);
            else
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Close Tab"));
            menu.AddSeparator("");

            System.Type[] types = GetPaneTypes();
            GUIContent baseContent = EditorGUIUtility.TextContent("Add Tab");
            foreach (System.Type t in types)
            {
                if (t == null)
                    continue;

                GUIContent entry = new GUIContent(EditorWindow.GetLocalizedTitleContentFromType(t)); // make a copy since we modify the text below
                entry.text = baseContent.text + "/" + entry.text;
                menu.AddItem(entry, false, AddTabToHere, t);
            }
        }

        void AddTabToHere(object userData)
        {
            EditorWindow win = (EditorWindow)CreateInstance((System.Type)userData);
            AddTab(win);
        }

        float GetTabWidth(float width)
        {
            int count = m_Panes.Count;
            if (s_DropInfo != null && System.Object.ReferenceEquals(s_DropInfo.dropArea, this))
                count++;
            if (m_Panes.IndexOf(s_DragPane) != -1)
                count--;

            return Mathf.Min(width / count, 100);
        }

        int GetTabAtMousePos(Vector2 mousePos, Rect position)
        {
            int sel = (int)Mathf.Min((mousePos.x - position.xMin) / GetTabWidth(position.width), 100);
            return sel;
        }

        // Hack to get around Unity crashing when we have circular references in saved stuff
        internal override void Initialize(ContainerWindow win)
        {
            base.Initialize(win);
            RemoveNullWindows();
            foreach (EditorWindow i in m_Panes)
                i.m_Parent = this;
        }

        static void CheckDragWindowExists()
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

        void DragTab(Rect pos, GUIStyle tabStyle)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            float elemWidth = GetTabWidth(pos.width);

            Event evt = Event.current;

            // Detect if hotcontrol was cleared while dragging (happens when pressing Esc).
            // We do not listen for the Escape keydown event because it is sent to the dragged window (not this dockarea)
            if (s_DragMode != 0 && GUIUtility.hotControl == 0)
            {
                PaneDragTab.get.Close();
                ResetDragVars();
            }

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (pos.Contains(evt.mousePosition) && GUIUtility.hotControl == 0)
                    {
                        int sel = GetTabAtMousePos(evt.mousePosition, pos);
                        if (sel < m_Panes.Count)
                        {
                            switch (evt.button)
                            {
                                case 0:
                                    if (sel != selected)
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
                    if (pos.Contains(evt.mousePosition) && GUIUtility.hotControl == 0)
                    {
                        int sel = GetTabAtMousePos(evt.mousePosition, pos);
                        if (sel < m_Panes.Count)
                            PopupGenericMenu(m_Panes[sel], new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0));
                    }

                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        Vector2 delta = evt.mousePosition - s_StartDragPosition;
                        evt.Use();
                        Rect screenRect = screenPosition;

                        // if we're not tabdragging yet, check to see if we should start

                        // check if we're allowed to drag tab
                        bool dragAllowed = (window.showMode != ShowMode.MainWindow || AllowTabAction());

                        if (s_DragMode == 0 && delta.sqrMagnitude > 99 && dragAllowed)
                        {
                            s_DragMode = 1;
                            s_PlaceholderPos = selected;
                            s_DragPane = m_Panes[selected];

                            // If we're moving the only editorwindow in this dockarea, we'll be destroyed - so it looks silly if we can attach as children of ourselves
                            if (m_Panes.Count == 1)
                                s_IgnoreDockingForView = this;
                            else
                                s_IgnoreDockingForView = null;

                            s_OriginalDragSource = this;
                            PaneDragTab.get.Show(
                                new Rect(pos.x + screenRect.x + elemWidth * selected, pos.y + screenRect.y, elemWidth, pos.height),
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
                            if (s_DropInfo != null && s_DropInfo.dropArea != null)
                            {
                                s_DropInfo.dropArea.PerformDrop(s_DragPane, s_DropInfo, screenMousePos);
                            }
                            else
                            {
                                EditorWindow w = s_DragPane;

                                ResetDragVars();

                                RemoveTab(w);
                                Rect wPos = w.position;
                                wPos.x = screenMousePos.x - wPos.width * .5f;
                                wPos.y = screenMousePos.y - wPos.height * .5f;

                                // don't put windows top outside of the screen, on mac OS handles this
                                if (Application.platform == RuntimePlatform.WindowsEditor)
                                    wPos.y = Mathf.Max(InternalEditorUtility.GetBoundsOfDesktopAtPoint(screenMousePos).y, wPos.y);

                                EditorWindow.CreateNewWindowForEditorWindow(w, false, false);

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
                    float xPos = pos.xMin;
                    int drawNum = 0;
                    if (actualView)
                    {
                        for (int i = 0; i < m_Panes.Count; i++)
                        {
                            // if we're dragging the tab we're about to draw, don't do that (handled by some window)
                            if (s_DragPane == m_Panes[i])
                                continue;

                            // If we need space for inserting a tab here, skip some horizontal
                            if (s_DropInfo != null && System.Object.ReferenceEquals(s_DropInfo.dropArea, this) && s_PlaceholderPos == drawNum)
                                xPos += elemWidth;

                            Rect r = new Rect(xPos, pos.yMin, elemWidth, pos.height);
                            float roundR = Mathf.Round(r.x);
                            Rect r2 = new Rect(roundR, r.y, Mathf.Round(r.x + r.width) - roundR, r.height);
                            tabStyle.Draw(r2, m_Panes[i].titleContent, false, false, i == selected, hasFocus);
                            xPos += elemWidth;
                            drawNum++;
                        }
                    }
                    else
                    {
                        Rect r = new Rect(xPos, pos.yMin, elemWidth, pos.height);
                        float roundR = Mathf.Round(r.x);
                        Rect r2 = new Rect(roundR, r.y, Mathf.Round(r.x + r.width) - roundR, r.height);
                        tabStyle.Draw(r2, "Failed to load", false, false, true, false);
                    }
                    break;
            }
            selected = Mathf.Clamp(selected, 0, m_Panes.Count - 1);
        }

        protected override RectOffset GetBorderSize()
        {
            if (!window)
                return m_BorderSize;

            // Reset
            m_BorderSize.left = m_BorderSize.right = m_BorderSize.top = m_BorderSize.bottom = 0;

            Rect r = windowPosition;
            if (r.xMin != 0)
                m_BorderSize.left += (int)kSideBorders;
            if (r.xMax != window.position.width)
            {
                m_BorderSize.right += (int)kSideBorders;
            }

            const int mainWindowTopAdjustment = 2;
            const int floatingWindowTopAdjustment = 5;
            m_BorderSize.top = (int)kTabHeight + (window != null && window.showMode != ShowMode.MainWindow ? floatingWindowTopAdjustment : mainWindowTopAdjustment);
            m_BorderSize.bottom = (window != null && window.showMode != ShowMode.MainWindow ? 0 : (int)kBottomBorders);

            return m_BorderSize;
        }

        static void ResetDragVars()
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
        protected override void OldOnGUI()
        {
            ClearBackground();
            // Call reset GUI state as first thing so GUI.color is correct when drawing window decoration.
            EditorGUIUtility.ResetGUIState();

            Rect r = new Rect(-2, 0, position.width + 4, position.height);
            background = "dockarea";
            r = background.margin.Remove(r);
            Rect backRect = new Rect(r.x + 1, r.y, r.width - 2, DockArea.kTabHeight);
            if (Event.current.type == EventType.Repaint)
            {
                background.Draw(r, GUIContent.none, false, false, false, false);
                GUIStyle s = "dragTab";
                s.Draw(backRect, actualView.titleContent, false, false, true, hasFocus);
            }

            if (Event.current.type == EventType.ContextClick && backRect.Contains(Event.current.mousePosition))
            {
                PopupGenericMenu(actualView, new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0));
            }
            ShowGenericMenu();
            if (actualView)
                actualView.m_Pos = borderSize.Remove(screenPosition);

            InvokeOnGUI(r);
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

            menu.AddItem(EditorGUIUtility.TextContent("Maximize"), !(parent is SplitView), Unmaximize, window);
            menu.AddDisabledItem(EditorGUIUtility.TextContent("Close Tab"));
            menu.AddSeparator("");
            System.Type[] types = GetPaneTypes();

            GUIContent baseContent = EditorGUIUtility.TextContent("Add Tab");
            foreach (System.Type t in types)
            {
                if (t == null)
                    continue;

                GUIContent entry = new GUIContent(EditorWindow.GetLocalizedTitleContentFromType(t)); // make a copy since we modify the text below
                entry.text = baseContent.text + "/" + entry.text;
                menu.AddDisabledItem(entry);
            }
        }
    }
} // namespace
