using System;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
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

    [Serializable]
    internal class DebuggerContext
    {
        [SerializeField]
        private int m_SelectedElementIndex = -1;
        private bool m_PickElement = false;

        [SerializeField]
        private bool m_ShowLayoutBound = false;

        [SerializeField]
        private bool m_ShowRepaintOverlay = false;

        [SerializeField]
        private bool m_ShowDrawStats = false;

        public DebuggerSelection selection { get; } = new DebuggerSelection();
        public VisualElement selectedElement => selection.element;
        public IPanelDebug panelDebug => selection.panelDebug;

        public event Action onStateChange;

        public int selectedElementIndex
        {
            get { return m_SelectedElementIndex; }
            set
            {
                if (m_SelectedElementIndex == value)
                    return;
                m_SelectedElementIndex = value;
                onStateChange?.Invoke();
            }
        }

        public bool pickElement
        {
            get { return m_PickElement; }
            set
            {
                if (m_PickElement == value)
                    return;
                m_PickElement = value;
                onStateChange?.Invoke();
            }
        }

        public bool showLayoutBound
        {
            get { return m_ShowLayoutBound; }
            set
            {
                if (m_ShowLayoutBound == value)
                    return;
                m_ShowLayoutBound = value;
                onStateChange?.Invoke();
            }
        }

        public bool showRepaintOverlay
        {
            get { return m_ShowRepaintOverlay; }
            set
            {
                if (m_ShowRepaintOverlay == value)
                    return;
                m_ShowRepaintOverlay = value;
                onStateChange?.Invoke();
            }
        }

        public bool showDrawStats
        {
            get { return m_ShowDrawStats; }
            set
            {
                if (m_ShowDrawStats == value)
                    return;
                m_ShowDrawStats = value;
                onStateChange?.Invoke();
            }
        }

        public bool uxmlLiveReload
        {
            get { return RetainedMode.UxmlLiveReloadIsEnabled; }
            set
            {
                if (RetainedMode.UxmlLiveReloadIsEnabled == value)
                    return;
                RetainedMode.UxmlLiveReloadIsEnabled = value;
                onStateChange?.Invoke();
            }
        }
    }

    internal class UIElementsDebugger : EditorWindow
    {
        public const string k_WindowPath = "Window/UI Toolkit/Debugger";
        public static readonly string WindowName = L10n.Tr("UI Toolkit Debugger");
        public static readonly string OpenWindowCommand = nameof(OpenUIElementsDebugger);

        [SerializeField]
        private UIElementsDebuggerImpl m_DebuggerImpl;

        [SerializeField]
        private DebuggerContext m_DebuggerContext;

        [MenuItem(k_WindowPath, false, 3010, false)]
        private static void OpenUIElementsDebugger()
        {
            if (CommandService.Exists(OpenWindowCommand))
                CommandService.Execute(OpenWindowCommand, CommandHint.Menu);
            else
            {
                OpenAndInspectWindow(null);
            }
        }

        [Shortcut(k_WindowPath, KeyCode.F5, ShortcutModifiers.Action)]
        private static void DebugWindowShortcut()
        {
            if (CommandService.Exists(OpenWindowCommand))
                CommandService.Execute(OpenWindowCommand, CommandHint.Shortcut, EditorWindow.focusedWindow);
            else
            {
                OpenAndInspectWindow(EditorWindow.focusedWindow);
            }
        }


        public static void OpenAndInspectWindow(EditorWindow window)
        {
            var debuggerWindow = CreateDebuggerWindow();
            debuggerWindow.Show();

            if (window != null)
                debuggerWindow.m_DebuggerImpl.ScheduleWindowToDebug(window);
        }

        private static UIElementsDebugger CreateDebuggerWindow()
        {
            var window = CreateInstance<UIElementsDebugger>();
            window.titleContent = EditorGUIUtility.TextContent(WindowName);
            return window;
        }

        public void OnEnable()
        {
            if (m_DebuggerContext == null)
                m_DebuggerContext = new DebuggerContext();

            if (m_DebuggerImpl == null)
                m_DebuggerImpl = new UIElementsDebuggerImpl();

            m_DebuggerImpl.Initialize(this, rootVisualElement, m_DebuggerContext);
        }

        public void OnDisable()
        {
            m_DebuggerImpl.OnDisable();
        }

        public void OnFocus()
        {
            m_DebuggerImpl.OnFocus();
        }
    }

    [Serializable]
    internal class UIElementsDebuggerImpl : PanelDebugger, IGlobalPanelDebugger
    {
        const string k_DefaultStyleSheetPath = "UIPackageResources/StyleSheets/UIElementsDebugger/UIElementsDebugger.uss";
        const string k_DefaultDarkStyleSheetPath = "UIPackageResources/StyleSheets/UIElementsDebugger/UIElementsDebuggerDark.uss";
        const string k_DefaultLightStyleSheetPath = "UIPackageResources/StyleSheets/UIElementsDebugger/UIElementsDebuggerLight.uss";

        private VisualElement m_Root;
        private ToolbarToggle m_PickToggle;
        private ToolbarToggle m_ShowLayoutToggle;
        private ToolbarToggle m_RepaintOverlayToggle;
        private ToolbarToggle m_UXMLLiveReloadToggle;
        private ToolbarToggle m_ShowDrawStatsToggle;

        private DebuggerTreeView m_TreeViewContainer;
        private StylesDebugger m_StylesDebuggerContainer;

        private DebuggerContext m_Context;
        private RepaintOverlayPainter m_RepaintOverlay;
        private HighlightOverlayPainter m_PickOverlay;
        private LayoutOverlayPainter m_LayoutOverlay;

        public void Initialize(EditorWindow debuggerWindow, VisualElement root, DebuggerContext context)
        {
            base.Initialize(debuggerWindow);

            m_Root = root;
            m_Context = context;
            m_Context.onStateChange += OnContextChange;

            var sheet = EditorGUIUtility.Load(k_DefaultStyleSheetPath) as StyleSheet;
            m_Root.styleSheets.Add(sheet);

            StyleSheet colorSheet;
            if (EditorGUIUtility.isProSkin)
                colorSheet = EditorGUIUtility.Load(k_DefaultDarkStyleSheetPath) as StyleSheet;
            else
                colorSheet = EditorGUIUtility.Load(k_DefaultLightStyleSheetPath) as StyleSheet;

            m_Root.styleSheets.Add(colorSheet);

            m_Root.Add(m_Toolbar);

            m_PickToggle = new ToolbarToggle() { name = "pickToggle" };
            m_PickToggle.text = "Pick Element";
            m_PickToggle.RegisterValueChangedCallback((e) =>
            {
                m_Context.pickElement = e.newValue;
                // On OSX, as focus-follow-mouse is not supported,
                // we explicitly focus the EditorWindow when enabling picking
                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    Panel p = m_Context.selection.panel as Panel;
                    if (p != null)
                    {
                        TryFocusCorrespondingWindow(p.ownerObject);
                    }
                }
            });

            m_Toolbar.Add(m_PickToggle);

            m_ShowLayoutToggle = new ToolbarToggle() { name = "layoutToggle" };
            m_ShowLayoutToggle.text = "Show Layout";
            m_ShowLayoutToggle.RegisterValueChangedCallback((e) => { m_Context.showLayoutBound = e.newValue; });

            m_Toolbar.Add(m_ShowLayoutToggle);

            if (Unsupported.IsDeveloperBuild())
            {
                m_RepaintOverlayToggle = new ToolbarToggle() { name = "repaintOverlayToggle" };
                m_RepaintOverlayToggle.text = "Repaint Overlay";
                m_RepaintOverlayToggle.RegisterValueChangedCallback((e) => m_Context.showRepaintOverlay = e.newValue);
                m_Toolbar.Add(m_RepaintOverlayToggle);

                m_UXMLLiveReloadToggle = new ToolbarToggle() { name = "UXMLReloadToggle" };
                m_UXMLLiveReloadToggle.text = "UXML Live Reload";
                m_UXMLLiveReloadToggle.RegisterValueChangedCallback((e) => m_Context.uxmlLiveReload = e.newValue);
                m_Toolbar.Add(m_UXMLLiveReloadToggle);

                m_ShowDrawStatsToggle = new ToolbarToggle() { name = "drawStatsToggle" };
                m_ShowDrawStatsToggle.text = "Draw Stats";
                m_ShowDrawStatsToggle.RegisterValueChangedCallback((e) => { m_Context.showDrawStats = e.newValue; });
                m_Toolbar.Add(m_ShowDrawStatsToggle);
            }

            var splitter = new DebuggerSplitter();
            m_Root.Add(splitter);

            m_TreeViewContainer = new DebuggerTreeView(m_Context.selection, SelectElement);
            m_TreeViewContainer.style.flexGrow = 1f;
            splitter.leftPane.Add(m_TreeViewContainer);

            m_StylesDebuggerContainer = new StylesDebugger(m_Context.selection);
            splitter.rightPane.Add(m_StylesDebuggerContainer);

            DebuggerEventDispatchingStrategy.s_GlobalPanelDebug = this;

            m_RepaintOverlay = new RepaintOverlayPainter();
            m_PickOverlay = new HighlightOverlayPainter();
            m_LayoutOverlay = new LayoutOverlayPainter();

            OnContextChange();
        }

        public new void OnDisable()
        {
            base.OnDisable();

            if (DebuggerEventDispatchingStrategy.s_GlobalPanelDebug == this)
                DebuggerEventDispatchingStrategy.s_GlobalPanelDebug = null;
        }

        public void OnFocus()
        {
            // Avoid taking focus in case of another debugger picking on this one
            var globalPanelDebugger = DebuggerEventDispatchingStrategy.s_GlobalPanelDebug as UIElementsDebuggerImpl;
            if (globalPanelDebugger == null || !globalPanelDebugger.m_Context.pickElement)
                DebuggerEventDispatchingStrategy.s_GlobalPanelDebug = this;
        }

        public override void Refresh()
        {
            if (!m_Context.pickElement)
            {
                var selectedElement = m_Context.selectedElement;
                m_TreeViewContainer.RebuildTree(panelDebug);

                //we should not lose the selection when the tree has changed.
                if (selectedElement != m_Context.selectedElement)
                {
                    if (m_Context.selectedElement == null && selectedElement.panel == panelDebug.panel)
                        SelectElement(selectedElement);
                }

                m_StylesDebuggerContainer.RefreshStylePropertyDebugger();
                m_DebuggerWindow.Repaint();
            }
            panelDebug?.MarkDebugContainerDirtyRepaint();
        }

        void OnContextChange()
        {
            // Sync the toolbar
            m_PickToggle.SetValueWithoutNotify(m_Context.pickElement);
            m_ShowLayoutToggle.SetValueWithoutNotify(m_Context.showLayoutBound);

            if (Unsupported.IsDeveloperBuild())
            {
                m_RepaintOverlayToggle.SetValueWithoutNotify(m_Context.showRepaintOverlay);
                m_UXMLLiveReloadToggle.SetValueWithoutNotify(m_Context.uxmlLiveReload);
                m_ShowDrawStatsToggle.SetValueWithoutNotify(m_Context.showDrawStats);

                var updater = (panel as BaseVisualElementPanel)?.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater;
                if (updater != null)
                    updater.DebugGetRenderChain().drawStats = m_Context.showDrawStats;
            }

            panelDebug?.MarkDirtyRepaint();
            panelDebug?.MarkDebugContainerDirtyRepaint();
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (m_Context.pickElement)
                m_PickOverlay.Draw(mgc);
            else
            {
                m_TreeViewContainer.DrawOverlay(mgc);
                m_StylesDebuggerContainer.RefreshBoxModelView(mgc);

                if (m_Context.showRepaintOverlay)
                    m_RepaintOverlay.Draw(mgc);
            }

            if (m_Context.showLayoutBound)
                DrawLayoutBounds(mgc);
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType changeTypeFlag)
        {
            if ((changeTypeFlag & VersionChangeType.Repaint) == VersionChangeType.Repaint && m_Context.showRepaintOverlay)
            {
                var visible = ve.resolvedStyle.visibility == Visibility.Visible &&
                    ve.resolvedStyle.opacity > Mathf.Epsilon;
                if (panel != null && ve != panel.visualTree && visible)
                    m_RepaintOverlay.AddOverlay(ve, panelDebug?.debugContainer);
            }

            if ((changeTypeFlag & VersionChangeType.Hierarchy) == VersionChangeType.Hierarchy)
                m_TreeViewContainer.hierarchyHasChanged = true;

            if (panelDebug?.debuggerOverlayPanel != null)
                panelDebug.debuggerOverlayPanel.visualTree.layout = panel.visualTree.layout;
        }

        protected override void OnSelectPanelDebug(IPanelDebug pdbg)
        {
            m_RepaintOverlay.ClearOverlay();

            m_TreeViewContainer.hierarchyHasChanged = true;
            if (m_Context.panelDebug?.debugContainer != null)
                m_Context.panelDebug.debugContainer.generateVisualContent -= OnGenerateVisualContent;

            m_Context.selection.panelDebug = pdbg;

            if (panelDebug?.debugContainer != null)
                panelDebug.debugContainer.generateVisualContent += OnGenerateVisualContent;

            Refresh();
        }

        protected override void OnRestorePanelSelection()
        {
            var restoredElement = panel.FindVisualElementByIndex(m_Context.selectedElementIndex);
            SelectElement(restoredElement);
        }

        protected override bool ValidateDebuggerConnection(IPanel panelConnection)
        {
            var p = m_Root.panel as Panel;
            var debuggers = p.panelDebug.GetAttachedDebuggers();

            foreach (var dbg in debuggers)
            {
                var uielementsDbg = dbg as UIElementsDebuggerImpl;
                if (uielementsDbg != null && uielementsDbg.panel == p && uielementsDbg.m_Root.panel == panelConnection)
                {
                    // Avoid spamming the console if picking
                    if (!m_Context.pickElement)
                        Debug.LogWarning("Cross UI Toolkit debugger debugging is not supported");
                    return false;
                }
            }

            return true;
        }

        public bool InterceptMouseEvent(IPanel panel, IMouseEvent ev)
        {
            if (!m_Context.pickElement)
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
            if (panel != m_Root.panel)
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
            if (!isRightClick || m_Context.pickElement)
                return;

            // Ignore events on detached elements and on this debugger
            if (panel == null || panel == m_Root.panel)
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
                this.m_Root.MarkDirtyRepaint();
                panelDebug?.MarkDebugContainerDirtyRepaint();

                m_TreeViewContainer.RebuildTree(panelDebug);
                SelectElement(ve);
            }
        }

        private void StopPicking()
        {
            m_Context.pickElement = false;
            m_PickOverlay.ClearOverlay();

            m_DebuggerWindow.Focus();
        }

        private void SelectElement(VisualElement ve)
        {
            if (m_Context.selectedElement != ve)
            {
                if (ve != null)
                    SelectPanelToDebug(ve.panel);

                m_Context.selection.element = ve;
                m_Context.selectedElementIndex = panel.FindVisualElementIndex(ve);
            }
        }

        private void DrawLayoutBounds(MeshGenerationContext mgc)
        {
            m_LayoutOverlay.ClearOverlay();
            m_LayoutOverlay.selectedElement = m_Context.selectedElement;
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
