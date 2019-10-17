// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    internal class DebuggerSelection
    {
        private VisualElement m_Element;
        private IPanelDebug m_PanelDebug;

        public VisualElement element
        {
            get { return m_Element; }
            set
            {
                if (m_Element != value)
                {
                    m_Element = value;
                    onSelectedElementChanged.Invoke(m_Element);
                }
            }
        }

        public IPanelDebug panelDebug
        {
            get { return m_PanelDebug; }
            set
            {
                if (m_PanelDebug != value)
                {
                    m_PanelDebug = value;
                    m_Element = null;
                    onSelectedElementChanged.Invoke(null);
                    onPanelDebugChanged.Invoke(m_PanelDebug);
                }
            }
        }

        public IPanel panel { get { return panelDebug?.panel; } }
        public VisualElement visualTree { get { return panel?.visualTree; } }

        public Action<IPanelDebug> onPanelDebugChanged;
        public Action<VisualElement> onSelectedElementChanged;
    }

    internal class UIElementsDebugger : PanelDebugger, IGlobalPanelDebugger
    {
        const string k_DefaultStyleSheetPath = "StyleSheets/UIElementsDebugger/UIElementsDebugger.uss";
        const string k_DefaultDarkStyleSheetPath = "StyleSheets/UIElementsDebugger/UIElementsDebuggerDark.uss";
        const string k_DefaultLightStyleSheetPath = "StyleSheets/UIElementsDebugger/UIElementsDebuggerLight.uss";
        public const string k_WindowPath = "Window/Analysis/UIElements Debugger";
        public static readonly string WindowName = L10n.Tr("UIElements Debugger");

        private ToolbarToggle m_PickToggle;
        private ToolbarToggle m_ShowLayoutToggle;
        private ToolbarToggle m_RepaintOverlayToggle;
        private ToolbarToggle m_UXMLLiveReloadToggle;
        private ToolbarToggle m_ShowDrawStatsToggle;

        private DebuggerSelection m_DebuggerSelection;
        private RepaintOverlayPainter m_RepaintOverlay;
        private HighlightOverlayPainter m_PickOverlay;
        private LayoutOverlayPainter m_LayoutOverlay;

        private DebuggerTreeView m_TreeViewContainer;
        private StylesDebugger m_StylesDebuggerContainer;

        [SerializeField]
        private int m_SelectedElementIndex = -1;
        private bool m_PickElement = false;
        private bool m_ShowLayoutBound = false;
        private bool m_ShowRepaintOverlay = false;

        [MenuItem(k_WindowPath, false, 101, false)]
        private static void Open()
        {
            var window = CreateDebuggerWindow();
            window.Show();
        }

        [Shortcut(k_WindowPath, KeyCode.F5, ShortcutModifiers.Action)]
        private static void DebugWindowShortcut()
        {
            OpenAndInspectWindow(EditorWindow.focusedWindow);
        }

        public static void OpenAndInspectWindow(EditorWindow window)
        {
            var debuggerWindow = CreateDebuggerWindow();
            debuggerWindow.Show();
            debuggerWindow.ScheduleWindowToDebug(window);
        }

        private static UIElementsDebugger CreateDebuggerWindow()
        {
            var window = CreateInstance<UIElementsDebugger>();
            window.titleContent = EditorGUIUtility.TextContent(WindowName);
            return window;
        }

        public new void OnEnable()
        {
            base.OnEnable();

            DebuggerEventDispatchingStrategy.s_GlobalPanelDebug = this;

            m_DebuggerSelection = new DebuggerSelection();
            m_RepaintOverlay = new RepaintOverlayPainter();
            m_PickOverlay = new HighlightOverlayPainter();
            m_LayoutOverlay = new LayoutOverlayPainter();

            var root = this.rootVisualElement;
            var sheet = EditorGUIUtility.Load(k_DefaultStyleSheetPath) as StyleSheet;
            root.styleSheets.Add(sheet);

            StyleSheet colorSheet;
            if (EditorGUIUtility.isProSkin)
                colorSheet = EditorGUIUtility.Load(k_DefaultDarkStyleSheetPath) as StyleSheet;
            else
                colorSheet = EditorGUIUtility.Load(k_DefaultLightStyleSheetPath) as StyleSheet;

            root.styleSheets.Add(colorSheet);
            root.Add(m_Toolbar);

            m_PickToggle = new ToolbarToggle() { name = "pickToggle" };
            m_PickToggle.text = "Pick Element";
            m_PickToggle.RegisterValueChangedCallback((e) =>
            {
                m_PickElement = e.newValue;
                // On OSX, as focus-follow-mouse is not supported,
                // we explicitly focus the EditorWindow when enabling picking
                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    Panel p = m_DebuggerSelection.panel as Panel;
                    if (p != null)
                    {
                        TryFocusCorrespondingWindow(p);
                    }
                }
            });

            m_Toolbar.Add(m_PickToggle);

            m_ShowLayoutToggle = new ToolbarToggle() { name = "layoutToggle" };
            m_ShowLayoutToggle.SetValueWithoutNotify(m_ShowLayoutBound);
            m_ShowLayoutToggle.text = "Show Layout";
            m_ShowLayoutToggle.RegisterValueChangedCallback((e) =>
            {
                m_ShowLayoutBound = e.newValue;
                panelDebug?.MarkDirtyRepaint();
                panelDebug?.MarkDebugContainerDirtyRepaint();
            });

            m_Toolbar.Add(m_ShowLayoutToggle);

            if (Unsupported.IsDeveloperBuild())
            {
                m_RepaintOverlayToggle = new ToolbarToggle() { name = "repaintOverlayToggle" };
                m_RepaintOverlayToggle.text = "Repaint Overlay";
                m_RepaintOverlayToggle.RegisterValueChangedCallback((e) => m_ShowRepaintOverlay = e.newValue);
                m_Toolbar.Add(m_RepaintOverlayToggle);

                m_UXMLLiveReloadToggle = new ToolbarToggle() { name = "UXMLReloadToggle" };
                m_UXMLLiveReloadToggle.SetValueWithoutNotify(RetainedMode.UxmlLiveReloadIsEnabled);
                m_UXMLLiveReloadToggle.text = "UXML Live Reload";
                m_UXMLLiveReloadToggle.RegisterValueChangedCallback((e) => RetainedMode.UxmlLiveReloadIsEnabled = e.newValue);
                m_Toolbar.Add(m_UXMLLiveReloadToggle);

                m_ShowDrawStatsToggle = new ToolbarToggle() { name = "drawStatsToggle" };
                m_ShowDrawStatsToggle.text = "Draw Stats";
                m_ShowDrawStatsToggle.RegisterValueChangedCallback((e) =>
                {
                    var updater = (panel as BaseVisualElementPanel)?.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater;
                    if (updater != null)
                        updater.DebugGetRenderChain().drawStats = e.newValue;
                    panelDebug?.MarkDirtyRepaint();
                });
                m_Toolbar.Add(m_ShowDrawStatsToggle);
            }

            var splitter = new DebuggerSplitter();
            root.Add(splitter);

            m_TreeViewContainer = new DebuggerTreeView(m_DebuggerSelection, SelectElement);
            m_TreeViewContainer.style.flexGrow = 1f;
            splitter.leftPane.Add(m_TreeViewContainer);

            m_StylesDebuggerContainer = new StylesDebugger(m_DebuggerSelection);
            splitter.rightPane.Add(m_StylesDebuggerContainer);
        }

        public new void OnDisable()
        {
            base.OnDisable();

            if (DebuggerEventDispatchingStrategy.s_GlobalPanelDebug == (IGlobalPanelDebugger)this)
                DebuggerEventDispatchingStrategy.s_GlobalPanelDebug = null;
        }

        public void OnFocus()
        {
            // Avoid taking focus in case of another debugger picking on this one
            var globalPanelDebugger = DebuggerEventDispatchingStrategy.s_GlobalPanelDebug as UIElementsDebugger;
            if (globalPanelDebugger == null || !globalPanelDebugger.m_PickElement)
                DebuggerEventDispatchingStrategy.s_GlobalPanelDebug = this;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (m_PickElement)
                m_PickOverlay.Draw(mgc);

            if (!m_PickElement)
            {
                var selectedElement = m_DebuggerSelection.element;
                m_TreeViewContainer.RebuildTree(panelDebug);
                m_TreeViewContainer.DrawOverlay(mgc);

                //we should not lose the selection when the tree has changed.
                if (selectedElement != m_DebuggerSelection.element)
                {
                    if (m_DebuggerSelection.element == null && selectedElement.panel == panelDebug.panel)
                        SelectElement(selectedElement);
                }

                m_StylesDebuggerContainer.Refresh(mgc);

                Repaint();
            }

            if (m_ShowLayoutBound)
                DrawLayoutBounds(mgc);

            if (!m_PickElement && m_ShowRepaintOverlay)
                m_RepaintOverlay.Draw(mgc);
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType changeTypeFlag)
        {
            if (ve == panelDebug?.debugContainer)
                return;
            if ((changeTypeFlag & VersionChangeType.Repaint) == VersionChangeType.Repaint && m_ShowRepaintOverlay)
            {
                var visible = ve.resolvedStyle.visibility == Visibility.Visible &&
                    ve.resolvedStyle.opacity > Mathf.Epsilon;
                if (panel != null && ve != panel.visualTree && visible)
                    m_RepaintOverlay.AddOverlay(ve, panelDebug?.debugContainer);
            }

            if ((changeTypeFlag & VersionChangeType.Hierarchy) == VersionChangeType.Hierarchy)
                m_TreeViewContainer.hierarchyHasChanged = true;

            if (panelDebug?.debuggerOverlayPanel != null)
            {
                panelDebug.debuggerOverlayPanel.visualTree.layout = panel.visualTree.layout;
                panelDebug.MarkDebugContainerDirtyRepaint();
            }
        }

        protected override void OnSelectPanelDebug(IPanelDebug pdbg)
        {
            m_RepaintOverlay.ClearOverlay();

            m_TreeViewContainer.hierarchyHasChanged = true;
            if (m_DebuggerSelection.panelDebug?.debugContainer != null)
                m_DebuggerSelection.panelDebug.debugContainer.generateVisualContent -= OnGenerateVisualContent;

            m_DebuggerSelection.panelDebug = pdbg;

            if (panelDebug?.debugContainer != null)
                panelDebug.debugContainer.generateVisualContent += OnGenerateVisualContent;
        }

        protected override void OnRestorePanelSelection()
        {
            var restoredElement = panel.FindVisualElementByIndex(m_SelectedElementIndex);
            SelectElement(restoredElement);
        }

        protected override bool ValidateDebuggerConnection(IPanel panelConnection)
        {
            var p = rootVisualElement.panel as Panel;
            var debuggers = p.panelDebug.GetAttachedDebuggers();

            foreach (var dbg in debuggers)
            {
                var uielementsDbg = dbg as UIElementsDebugger;
                if (uielementsDbg != null && uielementsDbg.panel == p && uielementsDbg.rootVisualElement.panel == panelConnection)
                {
                    // Avoid spamming the console if picking
                    if (!m_PickElement)
                        Debug.LogWarning("Cross UIElements debugger debugging is not supported");
                    return false;
                }
            }

            return true;
        }

        public override bool InterceptEvent(EventBase ev)
        {
            return false;
        }

        public override void PostProcessEvent(EventBase ev)
        {
        }

        public bool InterceptMouseEvent(IPanel panel, IMouseEvent ev)
        {
            if (!m_PickElement)
                return false;

            var evtBase = ev as EventBase;
            var evtType = evtBase.eventTypeId;
            var target = evtBase.target as VisualElement;

            // Ignore events on detached elements
            if (panel == null)
                return false;

            // Only intercept mouse clicks, MouseOverEvent and MouseEnterWindow
            if (evtType != MouseDownEvent.TypeId() && evtType != MouseOverEvent.TypeId() && evtType != MouseEnterWindowEvent.TypeId())
                return false;

            if (evtType == MouseDownEvent.TypeId())
            {
                if ((ev as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
                    StopPicking();

                return true;
            }

            // Ignore these events if on this debugger
            if (panel != rootVisualElement.panel)
            {
                if (evtType == MouseOverEvent.TypeId())
                {
                    OnPickMouseOver(target, panel);
                }
                else if (evtType == MouseEnterWindowEvent.TypeId())
                {
                    // Focus window while picking an element
                    var mouseOverView = GUIView.mouseOverView;
                    if (mouseOverView != null)
                        mouseOverView.Focus();
                }
            }

            return false;
        }

        public void OnPostMouseEvent(IPanel panel, IMouseEvent ev)
        {
            var isRightClick = (ev as MouseUpEvent)?.button == (int)MouseButton.RightMouse;
            if (!isRightClick || m_PickElement)
                return;

            // Ignore events on detached elements and on this debugger
            if (panel == null || panel == rootVisualElement.panel)
                return;

            var evtBase = ev as EventBase;
            var target = evtBase.target as VisualElement;
            var targetIsImguiContainer = target is IMGUIContainer;

            if (target != null)
            {
                // If right clicking on the root IMGUIContainer try to select the root container instead
                if (targetIsImguiContainer && target == panel.visualTree[0])
                {
                    // Pick the root container
                    var root = panel.GetRootVisualElement();
                    if (root != null && root.childCount > 0 && root.worldBound.Contains(ev.mousePosition))
                    {
                        target = root;
                        targetIsImguiContainer = false;
                    }
                }
                if (!targetIsImguiContainer)
                {
                    ShowInspectMenu(target);
                }
            }
        }

        private void ShowInspectMenu(VisualElement ve)
        {
            var menu = new GenericMenu();
            menu.AddItem(EditorGUIUtility.TrTextContent("Inspect Element"), false, InspectElement, ve);
            menu.ShowAsContext();
        }

        private void InspectElement(object inspectElement)
        {
            VisualElement ve = inspectElement as VisualElement;
            SelectPanelToDebug(ve.panel);

            // Rebuild tree view on new panel or the selection will fail
            m_TreeViewContainer.RebuildTree(panelDebug);
            SelectElement(ve);
        }

        private void OnPickMouseOver(VisualElement ve, IPanel panel)
        {
            m_PickOverlay.ClearOverlay();
            m_PickOverlay.AddOverlay(ve);

            SelectPanelToDebug(panel);

            if (panelDebug != null)
            {
                panelDebug?.MarkDirtyRepaint();
                this.rootVisualElement.MarkDirtyRepaint();
                panelDebug?.MarkDebugContainerDirtyRepaint();

                m_TreeViewContainer.RebuildTree(panelDebug);
                SelectElement(ve);
            }
        }

        private void StopPicking()
        {
            m_PickElement = false;
            m_PickToggle.SetValueWithoutNotify(false);
            m_PickOverlay.ClearOverlay();

            panelDebug?.MarkDebugContainerDirtyRepaint();
            panelDebug?.MarkDirtyRepaint();

            Focus();
        }

        private void SelectElement(VisualElement ve)
        {
            if (m_DebuggerSelection.element != ve)
            {
                if (ve != null)
                    SelectPanelToDebug(ve.panel);

                m_DebuggerSelection.element = ve;
                m_SelectedElementIndex = panel.FindVisualElementIndex(ve);
            }
        }

        private void DrawLayoutBounds(MeshGenerationContext mgc)
        {
            m_LayoutOverlay.ClearOverlay();
            m_LayoutOverlay.selectedElement = m_DebuggerSelection.element;
            AddLayoutBoundOverlayRecursive(visualTree);

            m_LayoutOverlay.Draw(mgc);
        }

        private void AddLayoutBoundOverlayRecursive(VisualElement ve)
        {
            m_LayoutOverlay.AddOverlay(ve);

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];
                AddLayoutBoundOverlayRecursive(child);
            }
        }
    }
}
