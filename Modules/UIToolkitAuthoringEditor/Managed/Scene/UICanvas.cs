// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

enum CanvasBackgroundType
{
    Checkerboard,
    Color,
    Image
}

[UxmlElement(visibility = LibraryVisibility.Hidden)]
partial class UICanvas : VisualElement, IVisualElementChangeProcessor
{
    public readonly struct CanvasManipulationScope : IDisposable
    {
        readonly UICanvas m_Canvas;
        readonly Vector2 m_BaseSize;
        readonly Vector2 m_Offset;
        readonly float m_ZoomScale;

        internal CanvasManipulationScope(UICanvas canvas)
        {
            m_Canvas = canvas;
            m_Canvas.m_ScopeLevel++;
            m_BaseSize = m_Canvas.BaseSize;
            m_Offset = m_Canvas.Offset;
            m_ZoomScale = m_Canvas.ZoomScale;
            Undo.RegisterCompleteObjectUndo(m_Canvas.m_Settings, "Change Canvas Settings");
        }

        public void Dispose()
        {
            m_Canvas.m_ScopeLevel--;
            if (m_Canvas.m_ScopeLevel > 0)
                return;

            if (m_Canvas.BaseSize != m_BaseSize ||
                m_Canvas.Offset != m_Offset ||
                !Mathf.Approximately(m_Canvas.ZoomScale, m_ZoomScale))
            {
                m_Canvas.CommitToPanelElement();
            }

            EditorUtility.SetDirty(m_Canvas.m_Settings);
            m_Canvas.m_Settings.SerializeToStorage(m_Canvas.m_StorageKey);
        }
    }

    public class CanvasChangedEvent : EventBase<CanvasChangedEvent>
    {
        static CanvasChangedEvent() => SetCreateFunction(Create);

        static CanvasChangedEvent Create() => new CanvasChangedEvent();

        public Vector2 ViewSize { get; protected set; }
        public Vector2 CanvasSize { get; protected set; }
        public Vector2 Offset { get; protected set; }
        public float ZoomFactor { get; protected set; }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
            ViewSize = Vector2.zero;
            CanvasSize = Vector2.zero;
            Offset = Vector2.zero;
            ZoomFactor = 1.0f;
        }

        public static CanvasChangedEvent GetPooled(Vector2 viewSize, Vector2 canvasSize, Vector2 offset,
            float zoomFactor)
        {
            var e = GetPooled();
            e.ViewSize = viewSize;
            e.CanvasSize = canvasSize;
            e.Offset = offset;
            e.ZoomFactor = zoomFactor;
            return e;
        }

        public CanvasChangedEvent() => LocalInit();
    }

    const string k_VisualTreeAsset = "UIToolkitAuthoring/UIViewportWindow/UICanvas.uxml";
    const string k_StyleSheetDark = "UIToolkitAuthoring/UIViewportWindow/UICanvasDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/UIViewportWindow/UICanvasLight.uss";

    public const string UssClass = "unity-ui-viewport__canvas";
    const string SelectedCanvasUssClass = UssClass + "--selected";
    public const string HeaderUssClass = UssClass + "__header";
    public const string HeaderTitleUssClass = HeaderUssClass + "-title";
    public const string HeaderTagUssClass = HeaderUssClass + "-tag";
    public const string DocumentRootUssClass = UssClass + "-document-root";
    const string CanvasDocumentUssClass = UssClass + "-document";
    const string BackgroundColorOverlayUssClass = UssClass + "-background--color-overlay";
    const string BackgroundImageOverlayUssClass = UssClass + "-background--image-overlay";

    readonly VisualElement m_Header;
    readonly Label m_TitleLabel;
    readonly Label m_TitleTagLabel;
    readonly VisualElement m_CanvasDocument;
    readonly UICanvasDocumentRoot m_DocumentRoot;
    readonly HighlightOverlayPainter2D m_HighlightOverlay;
    readonly CheckerboardBackground m_CheckerboardBackground;
    readonly VisualElement m_BackgroundColorOverlay;
    readonly VisualElement m_BackgroundImageOverlay;

    CanvasSettings m_Settings;
    EntityId m_SettingsKey;
    bool m_PreviewMode;
    int m_ScopeLevel = 0;

    string m_StorageKey;

    PanelElement m_PanelElement;

    public PanelElement PanelElement
    {
        get => m_PanelElement;
        set
        {
            if (m_PanelElement == value)
                return;

            Release(m_PanelElement);
            m_PanelElement = value;
            Acquire(m_PanelElement);
        }
    }

    public VisualElement Header => m_Header;

    public bool PreviewMode
    {
        get => m_PreviewMode;
        set
        {
            if (m_PreviewMode == value)
                return;

            m_PreviewMode = value;
            SetupPreviewMode(m_PreviewMode);
        }
    }

    public UICanvasDocumentRoot DocumentRoot => m_DocumentRoot;

    public string HeaderTitle
    {
        get => m_TitleLabel.text;
        set => m_TitleLabel.text = value;
    }

    public string HeaderTag
    {
        get => m_TitleTagLabel.text;
        set => m_TitleTagLabel.text = value;
    }

    public Vector2 BaseSize
    {
        get => m_Settings?.CanvasSize ?? CanvasSettings.DefaultSize;
        set
        {
            using var _ = new CanvasManipulationScope(this);
            m_Settings.CanvasSize = value;
        }
    }

    public Vector2 Size
    {
        get => m_Settings?.ScaledSize ?? CanvasSettings.DefaultSize * CanvasSettings.DefaultZoomFactor;
        set
        {
            using var _ = new CanvasManipulationScope(this);
            // Update the base size accounting for current zoom
            // Since visual size = base size * zoom, we have: base size = visual size / zoom
            if (m_Settings.ZoomFactor > 0)
            {
                m_Settings.CanvasSize = value / m_Settings.ZoomFactor;
            }
        }
    }

    public Vector2 Offset
    {
        get => m_Settings?.Offset ?? CanvasSettings.DefaultOffset;
        set
        {
            using var _ = new CanvasManipulationScope(this);
            m_Settings.Offset = value;
        }
    }

    public float ZoomScale
    {
        get => m_Settings?.ZoomFactor ?? CanvasSettings.DefaultZoomFactor;
        set
        {
            using var _ = new CanvasManipulationScope(this);
            m_Settings.ZoomFactor = value;
        }
    }

    Vector2 ViewSize => new(m_DocumentRoot.style.width.value.value, m_DocumentRoot.style.height.value.value);

    public UICanvas()
    {
        AddToClassList(UssClass);
        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        if (vta)
            vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        if (styleSheet)
            styleSheets.Add(styleSheet);

        m_Header = this.Q(className: HeaderUssClass);
        m_Header.focusable = true;

        m_TitleLabel = this.Q<Label>(className: HeaderTitleUssClass);
        m_TitleTagLabel = this.Q<Label>(className: HeaderTagUssClass);

        m_CanvasDocument = this.Q(className: CanvasDocumentUssClass);

        m_CheckerboardBackground = this.Q<CheckerboardBackground>();
        m_BackgroundColorOverlay = this.Q(className: BackgroundColorOverlayUssClass);
        m_BackgroundImageOverlay = this.Q(className: BackgroundImageOverlayUssClass);

        style.width = CanvasSettings.DefaultSize.x;
        style.height = CanvasSettings.DefaultSize.y;

        m_DocumentRoot = this.Q<UICanvasDocumentRoot>(className: DocumentRootUssClass);
        m_HighlightOverlay = new HighlightOverlayPainter2D(m_DocumentRoot);
        usageHints = UsageHints.DynamicTransform | UsageHints.LargePixelCoverage;

        m_PreviewMode = false;
        SetupBackground();
        SetupOverlays();
    }

    public void SetContext(PanelElement panelElement, string settingStorageKey)
    {
        m_StorageKey = settingStorageKey;

        // Try to retrieve existing settings from registry if we have a valid key
        if (!m_SettingsKey.IsValid() ||
            !CanvasSettingsRegistry.instance.TryGetSettings(m_SettingsKey, out m_Settings))
        {
            // Create new settings if not found in registry
            m_Settings = ScriptableObject.CreateInstance<CanvasSettings>();
            m_Settings.hideFlags = HideFlags.DontSave | HideFlags.DontUnloadUnusedAsset;

            m_Settings.LoadStorage(m_StorageKey);

            // Get the EntityId of the newly created settings and register it
            m_SettingsKey = m_Settings.GetEntityId();
            CanvasSettingsRegistry.instance.Register(m_SettingsKey, m_Settings);
        }

        m_Settings.CanvasSettingsChanged += OnCanvasChanged;
        m_Settings.CanvasBackgroundChanged += OnCanvasBackgroundTypeChanged;

        PanelElement = panelElement;
    }

    public void ClearContext()
    {
        m_DocumentRoot.style.backgroundImage = default;
        PanelElement = null;

        if (!m_Settings)
            return;

        m_Settings.CanvasSettingsChanged -= OnCanvasChanged;
        m_Settings.CanvasBackgroundChanged -= OnCanvasBackgroundTypeChanged;
        m_Settings.SerializeToStorage(m_StorageKey);
        Undo.ClearUndo(m_Settings);

        // Keep the settings registered so they survive domain reload and playmode transitions
        // Do NOT destroy them: UnityEngine.Object.DestroyImmediate(m_Settings);

        m_Settings = null;
    }

    /// <summary>
    /// Permanently destroys the canvas settings and removes them from the registry.
    /// Use this when the canvas is being permanently destroyed and should not be restored.
    /// </summary>
    public void DestroySettingsPermanently()
    {
        if (m_Settings != null)
        {
            m_Settings.CanvasSettingsChanged -= OnCanvasChanged;
            m_Settings.CanvasBackgroundChanged -= OnCanvasBackgroundTypeChanged;
            m_Settings.SerializeToStorage(m_StorageKey);
            Undo.ClearUndo(m_Settings);
            UnityEngine.Object.DestroyImmediate(m_Settings);
        }

        if (m_SettingsKey.IsValid())
        {
            CanvasSettingsRegistry.instance.Unregister(m_SettingsKey);
        }

        m_DocumentRoot.style.backgroundImage = default;
        PanelElement = null;
        m_Settings = null;
        m_SettingsKey = EntityId.None;
    }

    void OnCanvasChanged(CanvasSettings settings, Vector2 canvasSize, Vector2 offset, float zoomFactor)
    {
        CommitToPanelElement();
    }

    void OnCanvasBackgroundTypeChanged(CanvasSettings settings)
    {
        SetupBackground();
        SetupOverlays();
    }

    public CanvasManipulationScope ManipulationScope()
    {
        return new CanvasManipulationScope(this);
    }

    public void Select()
    {
        if (Selection.activeObject != m_Settings)
            Selection.activeObject = m_Settings;
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent:
                Selection.selectionChanged += OnSelectionChanged;
                OnSelectionChanged();
                PrefSettings.settingChanged += OnPrefsChanged;
                UICommandQueue.RegisterHandlerForCategory(CommandCategory.Highlight, OnHighlight);
                break;
            case DetachFromPanelEvent:
                Selection.selectionChanged -= OnSelectionChanged;
                PrefSettings.settingChanged -= OnPrefsChanged;
                UICommandQueue.UnregisterHandlerForCategory(CommandCategory.Highlight, OnHighlight);
                break;
        }

        base.HandleEventBubbleUp(evt);
    }

    void CommitToPanelElement()
    {
        style.width = Length.Pixels(Size.x);
        style.height = Length.Pixels(Size.y);
        style.translate = new StyleTranslate(Offset);
        m_DocumentRoot.style.translate = -Offset;
        m_HighlightOverlay.ZoomScale = ZoomScale;

        if (panel == null)
            return;

        if (m_ScopeLevel != 0)
            return;

        if (m_PanelElement == null)
            return;

        var evt = CanvasChangedEvent.GetPooled(ViewSize, BaseSize, Offset, ZoomScale);
        evt.elementTarget = this;
        SendEvent(evt);

        m_PanelElement.SubPanelPixelsPerPoint = ((Panel)panel).pixelsPerPoint;
        m_PanelElement.ResizeRenderTexture(ViewSize);
        m_PanelElement.Offset = Offset;
        m_PanelElement.ScaleFactor = ZoomScale;
        m_PanelElement.Size = BaseSize;

        if (ViewSize.x == 0 || ViewSize.y == 0)
            return;

        // Here, we force an update to ensure that the panel is repainted immediately, ensuring that changes in size
        // does not get a warped render due to a one-frame delay.
        m_PanelElement.FrameUpdate();
        m_CheckerboardBackground.MarkDirtyRepaint();
        m_DocumentRoot.MarkDirtyRepaint();
    }

    void SetupPreviewMode(bool enabled)
    {
        if (StageUtility.GetCurrentStage() is VisualElementEditingStage stage)
        {
            stage.ContentOverflowMode(enabled ? Overflow.Hidden : Overflow.Visible);
            stage.RequestRefresh();
        }

        m_DocumentRoot.EventMode = enabled ? CanvasEventMode.Forward : CanvasEventMode.Pick;
    }

    void Release(PanelElement panelElement)
    {
        if (panelElement == null)
            return;

        m_DocumentRoot.PanelElement = null;
        m_DocumentRoot.style.backgroundImage = StyleKeyword.Null;
        panelElement.OnAfterRepaint -= OnPanelElementWasRepainted;
        panelElement.SubPanel?.UnregisterChangeProcessor(this);
        Panel.beforeTickingAnyScheduledPanel -= ForceRuntimePanelUpdate;
    }

    void Acquire(PanelElement panelElement)
    {
        if (panelElement == null)
            return;

        panelElement.ContentOverflowMode = m_PreviewMode ? Overflow.Hidden : Overflow.Visible;
        panelElement.OnAfterRepaint += OnPanelElementWasRepainted;
        panelElement.SubPanel?.RegisterChangeProcessor(this);
        m_DocumentRoot.style.backgroundImage = Background.FromRenderTexture(panelElement.RenderTexture);
        m_DocumentRoot.PanelElement = panelElement;
        m_DocumentRoot.EventMode = m_PreviewMode ? CanvasEventMode.Forward : CanvasEventMode.Pick;
        ApplySettings();
        Panel.beforeTickingAnyScheduledPanel += ForceRuntimePanelUpdate;
    }

    void OnPanelElementWasRepainted(PanelElement panelElement)
    {
        m_DocumentRoot.style.backgroundImage = Background.FromRenderTexture(panelElement.RenderTexture);
    }

    internal void OnViewportChanged(Vector2 viewportSize)
    {
        m_DocumentRoot.style.width = viewportSize.x;
        m_DocumentRoot.style.height = viewportSize.y;

        // First time opening this document.
        if (viewportSize.x != 0.0f && viewportSize.y != 0.0f & string.IsNullOrEmpty(EditorUserSettings.GetConfigValue(m_StorageKey)))
            Offset = (viewportSize - CanvasSettings.DefaultSize * CanvasSettings.DefaultZoomFactor) / 2.0f;

        m_DocumentRoot.MarkDirtyRepaint();
        CommitToPanelElement();
    }

    void IVisualElementChangeProcessor.BeginProcessing(BaseVisualElementPanel panelElementPanel)
    {
    }

    void IVisualElementChangeProcessor.ProcessChanges(BaseVisualElementPanel panelElementPanel,
        AuthoringChanges changes)
    {
        // If only the `visualTree` changed, we don't need to repaint the render texture. This happens because we apply
        // the panel settings on the panel, but then we need to override some settings such as the 'visualTree`'s size.
        if (changes.styleChanged.Count == 1 && changes.styleChanged.Contains(panelElementPanel.visualTree))
            return;

        if (m_PanelElement == null || PanelElement.SubPanel == null)
            return;

        PanelElement.SubPanel.visualTree.MarkDirtyRepaint();
        m_DocumentRoot.IncrementVersion(VersionChangeType.Repaint);
    }

    void IVisualElementChangeProcessor.EndProcessing(BaseVisualElementPanel panelElementPanel)
    {
    }

    void OnPrefsChanged(string prefName, Type prefType)
    {
        if (string.CompareOrdinal(ColorPreferences.SelectionOutlineColor, prefName) == 0)
            UpdateSelectionColors();
    }

    void OnSelectionChanged()
    {
        var partOfSelection = false;
        foreach (var selectedId in Selection.entityIds)
        {
            if (EditorUtility.EntityIdToObject(selectedId) is CanvasSettings)
            {
                partOfSelection = true;
                break;
            }
        }

        EnableInClassList(SelectedCanvasUssClass, partOfSelection);
        UpdateSelectionColors();
    }

    void UpdateSelectionColors()
    {
        if (ClassListContains(SelectedCanvasUssClass))
        {
            m_Header.style.backgroundColor = ColorPreferences.SelectionOutline;
            m_CanvasDocument.style.backgroundColor = ColorPreferences.SelectionOutline;
        }
        else
        {
            m_Header.style.backgroundColor = StyleKeyword.Null;
            m_CanvasDocument.style.backgroundColor = StyleKeyword.Null;
        }
    }

    void SetupBackground()
    {
        switch (m_Settings?.BackgroundType ?? CanvasSettings.DefaultBackgroundType)
        {
            case CanvasBackgroundType.Checkerboard:
                m_BackgroundColorOverlay.style.display = DisplayStyle.None;
                m_BackgroundImageOverlay.style.display = DisplayStyle.None;
                break;
            case CanvasBackgroundType.Color:
                m_BackgroundColorOverlay.style.display = DisplayStyle.Flex;
                m_BackgroundImageOverlay.style.display = DisplayStyle.None;
                break;
            case CanvasBackgroundType.Image:
                m_BackgroundColorOverlay.style.display = DisplayStyle.None;
                m_BackgroundImageOverlay.style.display = DisplayStyle.Flex;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(m_Settings.BackgroundType), m_Settings.BackgroundType,
                    null);
        }
    }

    void ApplySettings()
    {
        if (m_Settings == null || m_PanelElement == null)
            return;

        SetupBackground();
        SetupOverlays();
        CommitToPanelElement();
    }

    void SetupOverlays()
    {
        m_BackgroundColorOverlay.style.backgroundColor =
            m_Settings?.BackgroundColor ?? CanvasSettings.DefaultBackgroundColor;
        m_BackgroundColorOverlay.style.opacity =
            m_Settings?.BackgroundColorOpacity ?? CanvasSettings.DefaultBackgroundOpacity;
        m_BackgroundImageOverlay.style.opacity =
            m_Settings?.BackgroundImageOpacity ?? CanvasSettings.DefaultBackgroundOpacity;
        m_BackgroundImageOverlay.style.backgroundImage =
            m_Settings?.BackgroundImage ?? CanvasSettings.DefaultBackgroundImage;
        var scaleMode = m_Settings?.BackgroundImageScaleMode ?? CanvasSettings.DefaultScaleMode;
        m_BackgroundImageOverlay.style.backgroundRepeat =
            BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(scaleMode);
        m_BackgroundImageOverlay.style.backgroundSize =
            BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(scaleMode);
        m_BackgroundImageOverlay.style.backgroundPositionX =
            BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(scaleMode);
        m_BackgroundImageOverlay.style.backgroundPositionY =
            BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(scaleMode);
    }

    void ForceRuntimePanelUpdate(Panel p)
    {
        if (p != panel)
            return;

        m_PanelElement?.FrameUpdate();
    }

    void OnHighlight(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        switch (context.Command)
        {
            case HighlightCommand highlightEvent:
                SetHighlight(highlightEvent.Elements, context.Source == CommandSources.Viewport);
                break;
            default:
                break;
        }
    }

    void SetHighlight(HashSet<VisualElement> elementsToHighlight, bool self)
    {
        if (m_PanelElement == null)
            return;

        m_HighlightOverlay.ClearOverlay();

        if (elementsToHighlight == null)
            return;

        foreach (var element in elementsToHighlight)
        {
            m_HighlightOverlay.AddOverlay(element, self? OverlayContent.Outline : OverlayContent.AllBoxes);
        }
    }
}
