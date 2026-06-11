// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Unity.UIToolkit.Editor;

[UxmlElement(visibility = LibraryVisibility.Hidden)]
sealed partial class UIViewport : VisualElement
{
    const string k_VisualTreeAsset = "UIToolkitAuthoring/UIViewportWindow/UIViewport.uxml";
    const string k_StyleSheetDark = "UIToolkitAuthoring/UIViewportWindow/UIViewportDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/UIViewportWindow/UIViewportLight.uss";

    const string k_OuterSplitViewName = UssClass + "__viewport-preview-split-view";
    const string k_InnerSplitViewUssClass = UssClass + "__code-section";
    const string k_ShowUxmlPreviewPrefKey = "UIToolkit.UIViewportWindow.ShowUxmlPreview";
    const string k_ShowUssPreviewPrefKey = "UIToolkit.UIViewportWindow.ShowUssPreview";

    public const string UssClass = "unity-ui-viewport";
    public const string ToolbarUssClass = UssClass + "__toolbar";
    public const string ToolbarZoomMenuUssClass = ToolbarUssClass + "-zoom-menu";
    public const string ToolbarPreviewToggleUssClass = ToolbarUssClass + "-preview-button";
    public const string ToolbarFitViewportButtonUssClass = ToolbarUssClass + "-fit-button";
    public const string ToolbarViewMenuUssClass = ToolbarUssClass + "-view-menu";

    public const string ViewportSurfaceUssClass = UssClass + "__viewport-surface";

    public const string ViewportContainerUssClass = UssClass + "__viewport-container";
    public const string PreviewModeUssClass = ViewportContainerUssClass + "--preview";

    public const string BreadcrumbsName = UssClass + "__breadcrumbs-view";

    readonly VisualElement m_ViewportContainer;
    readonly VisualElement m_Surface;
    readonly UICanvas m_Canvas;
    readonly ToolbarMenu m_ZoomMenu;
    readonly ToolbarMenu m_ViewMenu;
    readonly UIViewportThemeMenu m_ThemeMenu;
    readonly ToolbarToggle m_PreviewToggle;
    readonly Button m_FitViewportButton;

    readonly ToolbarBreadcrumbs m_Breadcrumbs;

    readonly UICanvasPanManipulator m_PanManipulator;
    readonly UICanvasZoomManipulator m_ZoomManipulator;
    readonly List<UICanvasResizeManipulator> m_ResizeManipulators = new();
    readonly AddElementDropManipulator m_DropManipulator;

    TwoPaneSplitView m_OuterSplitView;
    TwoPaneSplitView m_InnerSplitView;

    const int k_FitAnimationDuration = 250;
    ValueAnimation<float> m_FitAnimation;

    // Used for tests
    internal bool IsAnimating => m_FitAnimation?.isRunning == true;

    public UICanvas Canvas => m_Canvas;
    public VisualElement Surface => m_Surface;
    public AddElementDropManipulator DropManipulator => m_DropManipulator;

    public UIViewport()
    {
        AddToClassList(UssClass);
        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        if (vta)
            vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        if (styleSheet)
            styleSheets.Add(styleSheet);

        m_ViewportContainer = this.Q(className: ViewportContainerUssClass);
        m_Surface = this.Q(className: ViewportSurfaceUssClass);
        m_Surface.RegisterCallback<GeometryChangedEvent>(OnSurfaceGeometryChanged);
        m_Canvas = this.Q<UICanvas>();

        m_PanManipulator = new UICanvasPanManipulator(this);
        m_ZoomManipulator = new UICanvasZoomManipulator(this);
        m_DropManipulator = new AddElementDropManipulator(new UICanvasDropContext(m_Canvas));
        m_Canvas.AddManipulator(m_DropManipulator);

        foreach (var resizer in this.Query<UICanvasResizerHandle>().Build())
        {
            m_ResizeManipulators.Add(new UICanvasResizeManipulator(m_Canvas, resizer));
        }

        m_ZoomMenu = this.Q<ToolbarMenu>(className: ToolbarZoomMenuUssClass);
        SetupZoomMenu();

        m_ViewMenu = this.Q<ToolbarMenu>(className: ToolbarViewMenuUssClass);
        SetupViewMenu();

        m_ThemeMenu = this.Q<UIViewportThemeMenu>();
        m_PreviewToggle = this.Q<ToolbarToggle>(className: ToolbarPreviewToggleUssClass);
        SetupPreviewToggle();
        EnableInClassList(PreviewModeUssClass, m_Canvas.PreviewMode);

        m_Breadcrumbs = this.Q<ToolbarBreadcrumbs>(BreadcrumbsName);

        m_FitViewportButton = this.Q<Button>(className: ToolbarFitViewportButtonUssClass);
        m_FitViewportButton.clicked += FitViewport;
    }

    void OnSurfaceGeometryChanged(GeometryChangedEvent evt)
    {
        m_Canvas.OnViewportChanged(evt.newRect.size);
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case UICanvas.CanvasChangedEvent canvasChangedEvent:
                m_ZoomMenu.text = GetTextForZoomScale(canvasChangedEvent.ZoomFactor);
                break;
            case AttachToPanelEvent:
                PrefSettings.settingChanged += OnPrefsChanged;
                m_OuterSplitView = panel.visualTree.Q<TwoPaneSplitView>(k_OuterSplitViewName);
                m_InnerSplitView = panel.visualTree.Q<TwoPaneSplitView>(className: k_InnerSplitViewUssClass);
                ApplyPreviewVisibility();
                break;
            case DetachFromPanelEvent:
                PrefSettings.settingChanged -= OnPrefsChanged;
                if (m_FitAnimation != null && m_FitAnimation.isRunning)
                    m_FitAnimation?.Stop();
                m_OuterSplitView = null;
                m_InnerSplitView = null;
                break;
            case PointerUpEvent pointerUpEvent:
                if (TrySelectCanvas(pointerUpEvent))
                    evt.StopPropagation();
                break;
        }
        base.HandleEventBubbleUp(evt);
    }

    bool TrySelectCanvas(PointerUpEvent evt)
    {
        if (evt.button != 0)
            return false;
        if (!m_Canvas.Header.ContainsPoint(this.ChangeCoordinatesTo(m_Canvas.Header, evt.localPosition)))
            return false;

        m_Canvas.Select();
        return true;
    }

    void SetupZoomMenu()
    {
        foreach (var zoomScale in UICanvasZoomManipulator.ZoomMenuScaleValues)
        {
            m_ZoomMenu.menu.AppendAction(GetTextForZoomScale(zoomScale),
                a => { m_Canvas.ZoomScale = zoomScale; },
                a => Mathf.Approximately(m_Canvas.ZoomScale, zoomScale) ?
                    DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }
    }

    void SetupPreviewToggle()
    {
        m_PreviewToggle.RegisterValueChangedCallback(OnPreviewToggleChanged);
        m_PreviewToggle.SetValueWithoutNotify(m_Canvas.PreviewMode);
    }

    internal UIViewportThemeMenu ThemeMenu => m_ThemeMenu;

    void OnPreviewToggleChanged(ChangeEvent<bool> evt)
    {
        m_Canvas.PreviewMode = evt.newValue;
        m_ViewportContainer.EnableInClassList(PreviewModeUssClass, m_Canvas.PreviewMode);
        UpdatePreviewBackgroundColor();
    }

    void UpdatePreviewBackgroundColor()
    {
        if (m_Canvas.PreviewMode)
        {
            m_ViewportContainer.style.borderTopColor = ColorPreferences.PreviewBackground;
            m_ViewportContainer.style.borderRightColor = ColorPreferences.PreviewBackground;
            m_ViewportContainer.style.borderBottomColor = ColorPreferences.PreviewBackground;
            m_ViewportContainer.style.borderLeftColor = ColorPreferences.PreviewBackground;
            m_ViewportContainer.style.backgroundColor = ColorPreferences.PreviewBackground;
        }
        else
        {
            m_ViewportContainer.style.backgroundColor = StyleKeyword.Null;
            m_ViewportContainer.style.borderTopColor = StyleKeyword.Null;
            m_ViewportContainer.style.borderRightColor = StyleKeyword.Null;
            m_ViewportContainer.style.borderBottomColor = StyleKeyword.Null;
            m_ViewportContainer.style.borderLeftColor = StyleKeyword.Null;
        }
    }

    void OnPrefsChanged(string prefName, Type prefType)
    {
        if (string.CompareOrdinal(ColorPreferences.PreviewBackgroundColor, prefName) == 0)
            UpdatePreviewBackgroundColor();
    }

    public void FitViewport() => FitViewport(GetFirstSelectedElement());

    public void FitViewport(VisualElement target)
    {
        const float padding = 20f;

        var surfaceWidth = m_Surface.resolvedStyle.width;
        var surfaceHeight = m_Surface.resolvedStyle.height;

        if (float.IsNaN(surfaceWidth) || float.IsNaN(surfaceHeight) || surfaceWidth <= 0 || surfaceHeight <= 0)
            return;

        var currentZoom = m_Canvas.ZoomScale;
        var currentOffset = m_Canvas.Offset;

        float targetLeft, targetTop, targetWidth, targetHeight;

        if (target != null)
        {
            var ppp = m_Canvas.PanelElement?.SubPanelPixelsPerPoint ?? 1f;
            var bounds = target.worldBound;
            targetLeft = bounds.x / ppp;
            targetTop = bounds.y / ppp;
            targetWidth = bounds.width / ppp;
            targetHeight = bounds.height / ppp;
        }
        else
        {
            var canvasBaseSize = m_Canvas.BaseSize;
            if (canvasBaseSize.x <= 0 || canvasBaseSize.y <= 0)
                return;

            // Canvas is visually positioned at Offset with visual size = BaseSize * ZoomScale
            targetLeft = currentOffset.x;
            targetTop = currentOffset.y;
            targetWidth = canvasBaseSize.x * currentZoom;
            targetHeight = canvasBaseSize.y * currentZoom;
        }

        // Convert visual size to canvas-local (unscaled) space; clamp to prevent division-by-zero
        var baseWidth = Mathf.Max(targetWidth / currentZoom, 0.001f);
        var baseHeight = Mathf.Max(targetHeight / currentZoom, 0.001f);
        var canvasLocalX = (targetLeft - currentOffset.x) / currentZoom;
        var canvasLocalY = (targetTop - currentOffset.y) / currentZoom;

        // Zoom to fit the target with padding (padding is screen-space pixels, so subtract from surface)
        var paddedWidth = Mathf.Max(surfaceWidth - padding * 2, 0.001f);
        var paddedHeight = Mathf.Max(surfaceHeight - padding * 2, 0.001f);
        var newZoom = Mathf.Max(Mathf.Min(
            paddedWidth / baseWidth,
            paddedHeight / baseHeight), Mathf.Epsilon);

        // Offset to center the target in the surface at the new zoom
        var newOffset = new Vector2(
            (surfaceWidth - baseWidth * newZoom) / 2f - canvasLocalX * newZoom,
            (surfaceHeight - baseHeight * newZoom) / 2f - canvasLocalY * newZoom);

        // If the current zoom is not in the bounds of the zoom values list, return the closest zoom value.
        if (newZoom < m_ZoomManipulator.zoomScaleValues[0])
            newZoom = m_ZoomManipulator.zoomScaleValues[0];
        if (newZoom > m_ZoomManipulator.zoomScaleValues[^1])
            newZoom = m_ZoomManipulator.zoomScaleValues[^1];

        StartFitAnimation(newZoom, newOffset);
    }

    void StartFitAnimation(float targetZoom, Vector2 targetOffset)
    {
        if (m_FitAnimation?.isRunning == true)
            m_FitAnimation.Stop();

        var startZoom = m_Canvas.ZoomScale;
        var startOffset = m_Canvas.Offset;

        m_FitAnimation = m_Surface.experimental.animation.Start(0f, 1f, k_FitAnimationDuration, (_, t) =>
        {
            using (m_Canvas.ManipulationScope())
            {
                m_Canvas.ZoomScale = Mathf.Lerp(startZoom, targetZoom, t);
                m_Canvas.Offset = Vector2.Lerp(startOffset, targetOffset, t);
            }
        });
    }

    VisualElement GetFirstSelectedElement()
    {
        var subRoot = m_Canvas.PanelElement?.subRootVisualElement;
        foreach (var selectedId in Selection.entityIds)
        {
            if (EditorUtility.EntityIdToObject(selectedId) is VisualElementSelection { Element: { } element })
            {
                if (element.panel != null && element.panel == subRoot?.panel)
                    return element;
            }
        }
        return null;
    }

    static string GetTextForZoomScale(float scale)
    {
        return $"{(int)(scale*100f)}%";
    }

    public void ClearBreadcrumbs()
    {
        m_Breadcrumbs.Clear();
    }

    public void PushBreadcrumb(string label, Texture2D icon = null, Action clickedEvent = null)
    {
        m_Breadcrumbs.PushItem(label, clickedEvent);
        if (icon != null && m_Breadcrumbs.childCount > 0 &&
            m_Breadcrumbs.children[m_Breadcrumbs.childCount - 1] is Button item)
        {
            item.iconImage = Background.FromTexture2D(icon);
        }
    }

    /// <summary>
    /// Adds a checkable toggle item to the View dropdown menu.
    /// </summary>
    /// <param name="label">Display label for the menu item.</param>
    /// <param name="isChecked">Callback that returns the current checked state.</param>
    /// <param name="onToggle">Called with the new value when the item is clicked.</param>
    public void AddViewMenuToggle(string label, Func<bool> isChecked, Action<bool> onToggle)
    {
        if (m_ViewMenu == null)
            return;

        m_ViewMenu.menu.AppendAction(label,
            _ => onToggle(!isChecked()),
            _ => isChecked() ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
    }

    internal bool ShowUxmlPreview
    {
        get => GetUserSettingBool(k_ShowUxmlPreviewPrefKey, true);
        set
        {
            SetUserSettingBool(k_ShowUxmlPreviewPrefKey, value);
            ApplyPreviewVisibility();
        }
    }

    internal bool ShowUssPreview
    {
        get => GetUserSettingBool(k_ShowUssPreviewPrefKey, true);
        set
        {
            SetUserSettingBool(k_ShowUssPreviewPrefKey, value);
            ApplyPreviewVisibility();
        }
    }

    void SetupViewMenu()
    {
        AddViewMenuToggle(
            L10n.Tr("Show UXML preview"),
            () => ShowUxmlPreview,
            value => ShowUxmlPreview = value);

        AddViewMenuToggle(
            L10n.Tr("Show USS preview"),
            () => ShowUssPreview,
            value => ShowUssPreview = value);
    }

    void ApplyPreviewVisibility()
    {
        if (m_InnerSplitView == null || m_OuterSplitView == null)
            return;

        var showUxml = ShowUxmlPreview;
        var showUss = ShowUssPreview;

        if (showUxml || showUss)
        {
            // Ensure the code section and inner split view are fully restored before
            // selectively collapsing one side, to avoid double-collapsed state.
            m_OuterSplitView.UnCollapse();
            m_InnerSplitView.UnCollapse();

            if (!showUxml)
                m_InnerSplitView.CollapseChild(0);
            else if (!showUss)
                m_InnerSplitView.CollapseChild(1);
        }
        else
        {
            // Collapse the code section pane (index 1) from the outer split view.
            // CollapseChild handles hiding the drag line automatically.
            m_OuterSplitView.CollapseChild(1);
        }
    }

    static bool GetUserSettingBool(string key, bool defaultValue)
    {
        var stored = EditorUserSettings.GetConfigValue(key);
        return string.IsNullOrEmpty(stored) ? defaultValue : stored == "true";
    }

    static void SetUserSettingBool(string key, bool value)
    {
        EditorUserSettings.SetConfigValue(key, value ? "true" : "false");
    }
}
