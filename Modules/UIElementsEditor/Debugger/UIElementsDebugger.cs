// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements.Text;
using UnityEngine;
using UnityEngine.UIElements;

using TextElement = UnityEngine.UIElements.TextElement;

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

        public IPanel panel
        {
            get { return panelDebug?.panel; }
        }

        public VisualElement visualTree
        {
            get { return panel?.visualTree; }
        }

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

        [SerializeField]
        private bool m_BreakBatches = false;

        [SerializeField]
        private bool m_ShowWireframe = false;

        [SerializeField]
        private bool m_ShowTextureAtlasViewer = false;

        [SerializeField]
        private TextInfoOverlay.DisplayOption m_ShowTextMetrics = TextInfoOverlay.DisplayOption.None;



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

        public bool breakBatches
        {
            get { return m_BreakBatches; }
            set
            {
                if (m_BreakBatches == value)
                    return;
                m_BreakBatches = value;
                onStateChange?.Invoke();
            }
        }

        public bool showWireframe
        {
            get { return m_ShowWireframe; }
            set
            {
                if (m_ShowWireframe == value)
                    return;
                m_ShowWireframe = value;
                onStateChange?.Invoke();
            }
        }

        public bool showTextureAtlasViewer
        {
            get { return m_ShowTextureAtlasViewer; }
            set
            {
                if (m_ShowTextureAtlasViewer == value)
                    return;
                m_ShowTextureAtlasViewer = value;
                onStateChange?.Invoke();
            }
        }

        public TextInfoOverlay.DisplayOption showTextMetrics
        {
            get { return m_ShowTextMetrics; }
            set
            {
                if (m_ShowTextMetrics == value)
                    return;
                m_ShowTextMetrics = value;
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

        // Used in tests.
        internal UIElementsDebuggerImpl debuggerImpl => m_DebuggerImpl;

        [SerializeField]
        private DebuggerContext m_DebuggerContext;

        // Used in tests.
        internal DebuggerContext debuggerContext => m_DebuggerContext;

        [MenuItem(k_WindowPath, false, 3010, false, secondaryPriority = 3)]
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

            if (TextureAtlasViewer.UIElementsDebugger == null)
            {
                TextureAtlasViewer.UIElementsDebugger = debuggerWindow;
            }

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

        protected internal void ScrollToSelection()
        {
            m_DebuggerImpl.ScrollToSelection();
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
        private ToolbarToggle m_ShowDrawStatsToggle;
        private ToolbarToggle m_BreakBatchesToggle;
        private ToolbarToggle m_ShowWireframeToggle;
        private ToolbarButton m_TextureAtlasViewerButton;
        private EnumField m_ShowTextMetrics;

        private DebuggerTreeView m_TreeViewContainer;
        private StylesDebugger m_StylesDebuggerContainer;
        ScrollView m_ScrollView;

        private DebuggerContext m_Context;
        private RepaintOverlayPainter m_RepaintOverlay;
        private HighlightOverlayPainter m_PickOverlay;
        private LayoutOverlayPainter m_LayoutOverlay;
        private WireframeOverlayPainter m_WireframeOverlay;
        private TextInfoOverlay m_TextInfoOverlay;

        public void Initialize(EditorWindow debuggerWindow, VisualElement root, DebuggerContext context)
        {
            base.Initialize(debuggerWindow);

            m_Root = root;
            m_Context = context;
            m_Context.onStateChange += OnContextChange;

            m_Root.disablePlayModeTint = true;
            var sheet = EditorGUIUtility.Load(k_DefaultStyleSheetPath) as StyleSheet;
            m_Root.styleSheets.Add(sheet);

            StyleSheet colorSheet;
            if (EditorGUIUtility.isProSkin)
                colorSheet = EditorGUIUtility.Load(k_DefaultDarkStyleSheetPath) as StyleSheet;
            else
                colorSheet = EditorGUIUtility.Load(k_DefaultLightStyleSheetPath) as StyleSheet;

            m_Root.styleSheets.Add(colorSheet);

            m_Root.Add(m_Toolbar);

            m_PickToggle = new ToolbarToggle { name = "pickToggle" };
            m_PickToggle.text = "Pick Element";
            m_PickToggle.RegisterValueChangedCallback(e =>
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
                    else
                    {
                        List<Panel> panels = GetPanels();

                        if (panels.Count > 0)
                        {
                            TryFocusCorrespondingWindow(panels[0].ownerObject);
                        }
                    }
                }
            });

            m_Toolbar.Add(m_PickToggle);

            m_ShowLayoutToggle = new ToolbarToggle { name = "layoutToggle" };
            m_ShowLayoutToggle.text = "Show Layout";
            m_ShowLayoutToggle.RegisterValueChangedCallback(e => { m_Context.showLayoutBound = e.newValue; });

            m_Toolbar.Add(m_ShowLayoutToggle);

            m_ShowTextMetrics = new EnumField { name = "showTextMetrics" };
            m_ShowTextMetrics.Q<TextElement>().text = "Text Overlays";

            // Update USS classes so it looks like other ToolbarToggles
            m_ShowTextMetrics.AddToClassList(ToolbarToggle.ussClassName);
            m_ShowTextMetrics.Q<VisualElement>(classes: EnumField.inputUssClassName).AddToClassList(Toggle.inputUssClassName);
            m_ShowTextMetrics.Q<VisualElement>(classes: EnumField.inputUssClassName).RemoveFromClassList(EnumField.inputUssClassName);
            m_ShowTextMetrics.Q<TextElement>().RemoveFromClassList(EnumField.textUssClassName);
            m_ShowTextMetrics.Q<TextElement>().AddToClassList(Toggle.textUssClassName);
            m_ShowTextMetrics.Init(TextInfoOverlay.DisplayOption.None);
            m_ShowTextMetrics.Q<TextElement>().text = "Text Overlays";

            m_ShowTextMetrics.RegisterValueChangedCallback(e =>
            {
                m_Context.showTextMetrics = (TextInfoOverlay.DisplayOption)e.newValue;
                m_TextInfoOverlay.displayOption = (TextInfoOverlay.DisplayOption)e.newValue;
                m_ShowTextMetrics.Q<TextElement>().text = "Text Overlays";
            });
            m_Toolbar.Add(m_ShowTextMetrics);

            if (Unsupported.IsDeveloperMode())
            {
                m_RepaintOverlayToggle = new ToolbarToggle { name = "repaintOverlayToggle", text = "Repaint Overlay" };
                m_RepaintOverlayToggle.RegisterValueChangedCallback(e => m_Context.showRepaintOverlay = e.newValue);
                m_Toolbar.Add(m_RepaintOverlayToggle);
            }

            if (Unsupported.IsDeveloperMode())
            {
                m_ShowDrawStatsToggle = new ToolbarToggle { name = "drawStatsToggle", text = "Draw Stats Overlay" };
                m_ShowDrawStatsToggle.RegisterValueChangedCallback(e => { m_Context.showDrawStats = e.newValue; });
                m_Toolbar.Add(m_ShowDrawStatsToggle);
            }

            if (Unsupported.IsDeveloperMode())
            {
                m_BreakBatchesToggle = new ToolbarToggle { name = "breakBatchesToggle", text = "Break Batches", tooltip = "Useful when taking captures with RenderDoc" };
                m_BreakBatchesToggle.RegisterValueChangedCallback(e => { m_Context.breakBatches = e.newValue; });
                m_Toolbar.Add(m_BreakBatchesToggle);
            }

            if (Unsupported.IsDeveloperBuild())
            {
                m_ShowWireframeToggle = new ToolbarToggle { name = "showWireframeToggle", text = "Show Wireframe" };
                m_ShowWireframeToggle.RegisterValueChangedCallback(e => { m_Context.showWireframe = e.newValue; });
                m_Toolbar.Add(m_ShowWireframeToggle);
            }

            m_TextureAtlasViewerButton = new ToolbarButton { name = "textureAtlasViewerButton", text = "Texture Atlas Viewer" };
            m_TextureAtlasViewerButton.clicked += () => { TextureAtlasViewerWindow.ShowWindow(); };
            m_Toolbar.Add(m_TextureAtlasViewerButton);

            var splitter = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
            m_Root.Add(splitter);

            m_TreeViewContainer = new DebuggerTreeView(m_Context.selection, SelectElement);
            splitter.Add(m_TreeViewContainer);

            

            m_ScrollView = new ScrollView();
            m_ScrollView.Add(m_StylesDebuggerContainer = new StylesDebugger(m_Context.selection));

            m_ScrollView.Add(new TextDebugger(m_Context.selection));
            m_ScrollView.Add(new LayoutDebuggerTab(m_Context.selection));
            m_ScrollView.Add(new RenderDataDebuggerTab(m_Context.selection));
            m_ScrollView.Add(new PanelTab(m_Context.selection));
            
            splitter.Add(m_ScrollView);


            DebuggerEventDispatchUtilities.s_GlobalPanelDebug = this;

            m_RepaintOverlay = new RepaintOverlayPainter();
            m_PickOverlay = new HighlightOverlayPainter();
            m_LayoutOverlay = new LayoutOverlayPainter();
            m_WireframeOverlay = new WireframeOverlayPainter();
            m_TextInfoOverlay = new TextInfoOverlay(m_Context.selection);

            OnContextChange();

            EditorApplication.update += EditorUpdate;

            UIToolkitProjectSettings.onEnableLowLevelDebuggerChanged += (_) =>Refresh();
        }

        internal abstract class DebuggerFoldout : Foldout
        {
            DebuggerSelection m_DebuggerSelection;
            protected VisualElement m_SelectedElement;


            public DebuggerFoldout(string name, DebuggerSelection debuggerSelection) :base()
            {
                text = name;
                viewDataKey = name;

                m_DebuggerSelection = debuggerSelection;

                m_DebuggerSelection.onSelectedElementChanged += element => selectedElement = element;
                selectedElement = m_DebuggerSelection.element;

                this.RegisterValueChangedCallback( e=> Refresh());
                this.value = false;
                updateVisiblity();
            }

            protected VisualElement selectedElement
            {
                get
                {
                    return m_SelectedElement;
                }
                set
                {
                    if (m_SelectedElement == value)
                        return;


                    m_SelectedElement = value;
                    this.RefreshIfNeeded();
                }
            }

            public void RefreshIfNeeded()
            {
                if (IsActive())
                    Refresh();
            }

            bool IsActive()
            {
                updateVisiblity();
                return value && style.display == DisplayStyle.Flex;
            }

            void updateVisiblity()
            {
                style.display = UIToolkitProjectSettings.EnableLowLevelDebugger ? DisplayStyle.Flex : DisplayStyle.None;
            }

            protected abstract void Refresh();
        }

        internal class LayoutDebuggerTab : DebuggerFoldout
        {
            TextField m_layout;
            TextField m_isManual;
            public LayoutDebuggerTab(DebuggerSelection debuggerSelection) : base("Layout", debuggerSelection)
            {
                Add(m_layout = new TextField("Layout") { isReadOnly = true, multiline = true });
                Add(m_isManual = new TextField("Is Manual") { isReadOnly = true });
            }

            protected override void Refresh()
            {
                if (selectedElement == null)
                {
                    m_layout.text = "No Element selected";
                    m_isManual.text = "";
                }
                else
                {
                    unsafe
                    {
                        var layout = selectedElement.layoutNode.Layout;
                        m_layout.text = $"Overflow: {layout.HadOverflow}\n" +
                                        $"ComputedFlexBasis: {layout.ComputedFlexBasis}\n" +
                                        $"ComputedFlexBasisGeneration: {layout.ComputedFlexBasisGeneration}\n" +
                                        $"LastPointScaleFactor: {layout.LastPointScaleFactor}\n" +
                                        $"Measured: {layout.MeasuredDimensions[0]}, {layout.MeasuredDimensions[1]}\n";
                    }

                    m_isManual.text = $"{selectedElement.isLayoutManual}";

                }

            }
        }

        internal class RenderDataDebuggerTab : DebuggerFoldout
        {
            TextField m_ClippingRect;
            TextField m_ClippingRectMinusGroup;
            TextField m_ClippingRectIsInfinite;
            TextField m_LocalFlipsWinding;
            TextField m_WorldFlipsWinding;
            TextField m_ClipMethod;
            TextField m_ChildrenStencilRef;
            TextField m_ChildrenMaskDepth;
            public RenderDataDebuggerTab(DebuggerSelection debuggerSelection) : base("RenderData", debuggerSelection)
            {
                Add(m_ClippingRect = new("Clipping Rect") { isReadOnly = true });
                Add(m_ClippingRectMinusGroup = new("Clipping Rect Minus Group") { isReadOnly = true });
                Add(m_ClippingRectIsInfinite = new("Clipping Rect Is Infinite") { isReadOnly = true });
                Add(m_LocalFlipsWinding = new("Local Flips Winding") { isReadOnly = true });
                Add(m_WorldFlipsWinding = new("World Flips Winding") { isReadOnly = true });
                Add(m_ClipMethod = new("Clip Method") { isReadOnly = true });
                Add(m_ChildrenStencilRef = new("Children Stencil Ref") { isReadOnly = true });
                Add(m_ChildrenMaskDepth = new("Children Mask Depth") { isReadOnly = true });
            }

            protected override void Refresh()
            {
                if (selectedElement != null )
                {
                    var data = selectedElement.renderChainData;
                    m_ClippingRect.text = selectedElement.worldClip.ToString();
                    m_ClippingRectMinusGroup.text = selectedElement.worldClipMinusGroup.ToString();
                    m_ClippingRectIsInfinite.text = selectedElement.worldClipIsInfinite.ToString();
                    m_LocalFlipsWinding.text = data.localFlipsWinding.ToString();
                    m_WorldFlipsWinding.text = data.worldFlipsWinding.ToString();
                    m_ClipMethod.text = data.clipMethod.ToString();
                    m_ChildrenStencilRef.text = data.childrenStencilRef.ToString();
                    m_ChildrenMaskDepth.text = data.childrenMaskDepth.ToString();

                }
                else
                {
                    m_ClippingRect.text = "";
                    m_ClippingRectMinusGroup.text = "";
                    m_ClippingRectIsInfinite.text = "";
                    m_LocalFlipsWinding.text = "";
                    m_WorldFlipsWinding.text = "";
                    m_ClipMethod.text = "";
                    m_ChildrenStencilRef.text = "";
                    m_ChildrenMaskDepth.text = "";
                }

            }
        }

        internal class PanelTab : DebuggerFoldout
        {
            readonly TextField nameField;
            readonly TextField scale;
            readonly ObjectField panelSettings;


            public PanelTab(DebuggerSelection debuggerSelection) : base("Panel", debuggerSelection)
            {
                Add(nameField = new TextField("Owner Name") { isReadOnly = true });
                Add(panelSettings = new ObjectField("Owner/Panel Settings") { allowSceneObjects = false});
                Add(scale = new TextField("Scale") { isReadOnly = true });
            }

            protected override void Refresh()
            {
                if (selectedElement != null)
                {
                    var panel = selectedElement.elementPanel;
                    nameField.text = panel.ownerObject.name;
                    scale.text = $" scale: { panel.scale}, pixelPerPoint {panel.pixelsPerPoint}, scaledPixelPerPoint {panel.scaledPixelsPerPoint}";
                    panelSettings.value = panel.ownerObject;
                }
            }
        }


        internal class TextDebugger : DebuggerFoldout
        {

            TextField m_GenerationSettings;
            ObjectField m_fontAsset;
            ObjectField m_textSettings;
            TextField m_CacheInfo;
            TextField m_UnicodeResult;
            TextField m_SizeInfo;
            TextField m_CursorInfo;


            public TextDebugger(DebuggerSelection debuggerSelection):base("Text", debuggerSelection)
            {

                
                Add(m_GenerationSettings = new TextField("Generation Settings") { isReadOnly = true, multiline = true });

                Add(m_fontAsset = new ObjectField("Font Asset") { allowSceneObjects = false });

                Add(m_textSettings = new ObjectField("Text Settings") { allowSceneObjects = false, pseudoStates = PseudoStates.Disabled });
                Add(m_CacheInfo = new TextField("Measurement Info") { isReadOnly = true, multiline = true });
                Add(m_UnicodeResult = new TextField("Unicode Input") { isReadOnly = true, multiline = true, style = { whiteSpace = WhiteSpace.Normal } });
                Add(m_SizeInfo = new TextField("Size Info") { isReadOnly = true, multiline = true, style = { whiteSpace = WhiteSpace.Normal } });
                Add(m_CursorInfo = new TextField("Cursor Info") { isReadOnly = true });
            }


            protected override void Refresh()
            {
                
                var textElement = m_SelectedElement as TextElement;
                if (textElement == null || textElement.uitkTextHandle.IsAdvancedTextEnabledForElement())
                {
                    if (m_SelectedElement == null)
                        m_GenerationSettings.text = "No Element selected";
                    else if(textElement == null)
                        m_GenerationSettings.text = "No Text Element selected";
                    else
                        m_GenerationSettings.text = "Advanced Text is not yet supported by this foldout";

                    m_fontAsset.value = null;
                    m_textSettings.value = null;
                    m_CacheInfo.text = null;
                    m_UnicodeResult.text = null;
                    m_SizeInfo.text = null;
                }
                else
                {
                    var handle = textElement.uitkTextHandle;
                    if (handle.ConvertUssToTextGenerationSettings(true))
                    {
                        var settings = UnityEngine.TextCore.Text.TextHandle.settings;
                        m_GenerationSettings.text = settings.ToString();

                        m_fontAsset.value = settings.fontAsset;
                        m_textSettings.value = settings.textSettings;

                    }
                    else
                    {
                        m_GenerationSettings.text = "Failed to get Text Generation Settings";
                        m_fontAsset.value = null;
                        m_textSettings.value = null;
                    }

                    m_CacheInfo.text = handle.MeasuredWidth.HasValue ? $"Measured:{handle.MeasuredWidth} Rounded:{handle.RoundedWidth} PixelPerPoint:{handle.LastPixelPerPoint}" : "No cache";

                    m_UnicodeResult.text = StringToHex(textElement.text);

                    m_SizeInfo.text = $"input:{textElement.text.Length} glyphs:{handle.GetTextElementCount()}";

                    m_CursorInfo.text = textElement.isSelectable ? $"Cursor:{textElement.selectingManipulator.cursorIndex} Selection:{textElement.selectingManipulator.selectIndex}" : "Not Selectable";

                }

            }
            private string StringToHex(string hexstring)
            {
                if( string.IsNullOrEmpty(hexstring))
                    return string.Empty;

                StringBuilder sb = new StringBuilder();
                for(int i =0; i<hexstring.Length; i++) 
                {
                    var t = hexstring[i];
                    sb.Append("U").Append(Convert.ToInt32(t).ToString("X4"));

                    if (i < hexstring.Length - 1)
                        sb.Append(", ");
                }
                return sb.ToString();
            }
        }

        public new void OnDisable()
        {
            base.OnDisable();

            EditorApplication.update -= EditorUpdate;

            if (DebuggerEventDispatchUtilities.s_GlobalPanelDebug == this)
                DebuggerEventDispatchUtilities.s_GlobalPanelDebug = null;

            UIToolkitProjectSettings.onEnableLowLevelDebuggerChanged -= (_) => Refresh();
        }

        void EditorUpdate()
        {
            (panelDebug?.debuggerOverlayPanel as Panel)?.UpdateAnimations();
        }

        public void OnFocus()
        {
            // Avoid taking focus in case of another debugger picking on this one
            var globalPanelDebugger = DebuggerEventDispatchUtilities.s_GlobalPanelDebug as UIElementsDebuggerImpl;
            if (globalPanelDebugger == null || (globalPanelDebugger.m_Context != null && !globalPanelDebugger.m_Context.pickElement))
                DebuggerEventDispatchUtilities.s_GlobalPanelDebug = this;
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

                m_StylesDebuggerContainer?.RefreshStylePropertyDebugger();
                m_DebuggerWindow.Repaint();

                foreach( var child in m_ScrollView.Children())
                {
                    if (child is DebuggerFoldout foldout)
                    {
                        foldout.RefreshIfNeeded();
                    }
                }
            }

            panelDebug?.MarkDebugContainerDirtyRepaint();
        }

        void OnContextChange()
        {
            // Sync the toolbar
            m_PickToggle.SetValueWithoutNotify(m_Context.pickElement);
            m_ShowLayoutToggle.SetValueWithoutNotify(m_Context.showLayoutBound);
            m_RepaintOverlayToggle?.SetValueWithoutNotify(m_Context.showRepaintOverlay);
            m_ShowDrawStatsToggle?.SetValueWithoutNotify(m_Context.showDrawStats);
            m_BreakBatchesToggle?.SetValueWithoutNotify(m_Context.breakBatches);
            m_ShowWireframeToggle?.SetValueWithoutNotify(m_Context.showWireframe);
            m_ShowTextMetrics?.SetValueWithoutNotify( m_Context.showTextMetrics);

            ApplyToPanel(m_Context);

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
                m_StylesDebuggerContainer?.RefreshBoxModelView(mgc);

                if (m_Context.showRepaintOverlay)
                    m_RepaintOverlay.Draw(mgc);
            }

            if (m_Context.showLayoutBound)
                DrawLayoutBounds(mgc);
            if (m_Context.showWireframe)
                DrawWireframe(mgc);
            if(m_Context.showTextMetrics != TextInfoOverlay.DisplayOption.None)
                m_TextInfoOverlay.Draw(mgc, m_Context.showTextMetrics);
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType changeTypeFlag)
        {
            if ((changeTypeFlag & VersionChangeType.Repaint) == VersionChangeType.Repaint && m_Context.showRepaintOverlay)
            {
                var visible = ve.resolvedStyle.visibility == Visibility.Visible &&
                    ve.resolvedStyle.opacity > UIRUtility.k_Epsilon;
                if (panel != null && ve != panel.visualTree && visible)
                    m_RepaintOverlay.AddOverlay(ve, panelDebug?.debugContainer);
            }

            if ((changeTypeFlag & VersionChangeType.Hierarchy) == VersionChangeType.Hierarchy)
                m_TreeViewContainer.hierarchyHasChanged = true;

            if ((changeTypeFlag & VersionChangeType.StyleSheet) == VersionChangeType.StyleSheet && ve == m_Context.selectedElement)
                m_StylesDebuggerContainer.UpdateMatches();

            if (panelDebug?.debuggerOverlayPanel != null)
                panelDebug.debuggerOverlayPanel.visualTree.layout = panel.visualTree.layout;
        }

        static UIRRepaintUpdater GetRepaintUpdater(IPanel panel)
        {
            return (panel as BaseVisualElementPanel)?.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater;
        }

        static void ResetPanel(DebuggerContext context)
        {
            var updater = GetRepaintUpdater(context.selection.panel);
            if (updater != null)
            {
                updater.drawStats = false;
                updater.breakBatches = false;
            }
        }

        static void ApplyToPanel(DebuggerContext context)
        {
            var updater = GetRepaintUpdater(context.selection.panel);
            if (updater != null)
            {
                updater.drawStats = context.showDrawStats;
                updater.breakBatches = context.breakBatches;
            }
        }

        protected override void OnSelectPanelDebug(IPanelDebug pdbg)
        {
            m_RepaintOverlay.ClearOverlay();

            ResetPanel(m_Context);

            m_TreeViewContainer.hierarchyHasChanged = true;
            if (m_Context.panelDebug?.debugContainer != null)
                m_Context.panelDebug.debugContainer.generateVisualContent -= OnGenerateVisualContent;

            m_Context.selection.panelDebug = pdbg;

            if (panelDebug?.debugContainer != null)
                panelDebug.debugContainer.generateVisualContent += OnGenerateVisualContent;

            ApplyToPanel(m_Context);

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

        public bool InterceptEvent(IPanel p, EventBase ev)
        {
            if (m_Context == null)
                return false;

            if (!m_Context.pickElement)
                return false;

            var evtBase = ev as EventBase;
            var evtType = evtBase.eventTypeId;
            var target = evtBase.elementTarget;

            // Ignore events on detached elements
            if (p == null)
                return false;

            if (evtType == NavigationCancelEvent.TypeId())
            {
                StopPicking();
                return true;
            }

            if (((BaseVisualElementPanel)p).ownerObject is HostView hostView && hostView.actualView is PlayModeView playModeView)
            {
                // RuntimePanels won't receive MouseOverEvent when arriving from GameView editor panel, so listen for
                // MouseMoveEvent instead, but still intercept other events going to the game view window.
                if (evtType != MouseMoveEvent.TypeId() && evtType != MouseDownEvent.TypeId())
                    return false;

                if (evtType == MouseMoveEvent.TypeId())
                {
                    if (SelectTopElementFromRuntimePanel(evtBase.imguiEvent.mousePosition,
                        evtBase.imguiEvent.delta, playModeView.viewPadding, playModeView.viewMouseScale))
                        return true;

                    // If no RuntimePanel catches it, select GameView editor panel and let interception fall through.
                    if (m_Context.selectedElement != target)
                    {
                        OnPickMouseOver(target, p);
                    }
                }
            }

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
            if (p != m_Root.panel)
            {
                if (evtType == MouseOverEvent.TypeId())
                {
                    OnPickMouseOver(target, p);
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

        public void OnContextClick(IPanel p, ContextClickEvent ev)
        {
            if (m_Context.pickElement)
                return;

            // Ignore events on detached elements and on panels that are not the selected one
            if (p == null || p != panel)
                return;

            var evtBase = ev as EventBase;
            var target = evtBase.elementTarget;
            var targetIsImguiContainer = target is IMGUIContainer;

            if (target != null)
            {
                // If right clicking on the root IMGUIContainer try to select the root container instead
                if (targetIsImguiContainer && target == p.visualTree[0])
                {
                    // Pick the root container
                    var root = p.GetRootVisualElement();
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

        protected internal void ScrollToSelection()
        {
            m_TreeViewContainer.ScrollToSelection();
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

        private void OnPickMouseOver(VisualElement ve, IPanel p)
        {
            m_PickOverlay.ClearOverlay();
            m_PickOverlay.AddOverlay(ve);

            SelectPanelToDebug(p);

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
            (m_DebuggerWindow as UIElementsDebugger)?.ScrollToSelection();
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

        private bool SelectTopElementFromRuntimePanel(Vector2 editorMousePosition, Vector2 editorMouseDelta, Vector2 gameViewPadding, float gameMouseScale)
        {
            // Try picking element in runtime panels from closest to deepest
            var panels = UIElementsRuntimeUtility.GetSortedPlayerPanels();
            for (var i = panels.Count - 1; i >= 0; i--)
            {
                var runtimePanel = (BaseRuntimePanel) panels[i];

                if (!runtimePanel.ScreenToPanel(editorMousePosition - gameViewPadding, editorMouseDelta, out var panelPosition, out _))
                    continue;

                var mousePosition = panelPosition * gameMouseScale;

                var pickedElement = runtimePanel.Pick(mousePosition);
                if (pickedElement == null)
                    continue;

                if (m_Context.selectedElement != pickedElement)
                    OnPickMouseOver(pickedElement, runtimePanel);
                return true;
            }

            return false;
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

        private void DrawWireframe(MeshGenerationContext mgc)
        {
            m_WireframeOverlay.ClearOverlay();
            m_WireframeOverlay.selectedElement = m_Context.selectedElement;
            AddWireframeOverlayRecursive(visualTree);

            m_WireframeOverlay.Draw(mgc);
        }

        private void AddWireframeOverlayRecursive(VisualElement ve)
        {
            m_WireframeOverlay.AddOverlay(ve);

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];
                AddWireframeOverlayRecursive(child);
            }
        }
    }
}
