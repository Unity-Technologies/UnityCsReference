// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement(visibility = LibraryVisibility.Hidden)]
sealed partial class UIViewport : VisualElement
{
    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        public new static void Register()
            => UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), [], true);

        public override object CreateInstance() => new UIViewport();
    }

    const string k_VisualTreeAsset = "UIToolkitAuthoring/UIViewportWindow/UIViewport.uxml";
    const string k_StyleSheetDark = "UIToolkitAuthoring/UIViewportWindow/UIViewportDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/UIViewportWindow/UIViewportLight.uss";

    public const string UssClass = "unity-ui-viewport";
    public const string ToolbarUssClass = UssClass + "__toolbar";
    public const string ToolbarZoomMenuUssClass = ToolbarUssClass + "-zoom-menu";
    public const string ToolbarPreviewToggleUssClass = ToolbarUssClass + "-preview-button";

    public const string ViewportSurfaceUssClass = UssClass + "__viewport-surface";

    public const string ViewportContainerUssClass = UssClass + "__viewport-container";
    public const string PreviewModeUssClass = ViewportContainerUssClass + "--preview";

    readonly VisualElement m_ViewportContainer;
    readonly VisualElement m_Surface;
    readonly UICanvas m_Canvas;
    readonly ToolbarMenu m_ZoomMenu;
    readonly ToolbarToggle m_PreviewToggle;

    readonly UICanvasPanManipulator m_PanManipulator;
    readonly UICanvasZoomManipulator m_ZoomManipulator;
    readonly List<UICanvasResizeManipulator> m_ResizeManipulators = new();

    public UICanvas Canvas => m_Canvas;
    public VisualElement Surface => m_Surface;

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

        foreach (var resizer in this.Query<UICanvasResizerHandle>().Build())
        {
            m_ResizeManipulators.Add(new UICanvasResizeManipulator(m_Canvas, resizer));
        }

        m_ZoomMenu = this.Q<ToolbarMenu>(className: ToolbarZoomMenuUssClass);
        SetupZoomMenu();

        m_PreviewToggle = this.Q<ToolbarToggle>(className:ToolbarPreviewToggleUssClass);
        SetupPreviewToggle();
        EnableInClassList(PreviewModeUssClass, m_Canvas.PreviewMode);
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
                break;
            case DetachFromPanelEvent:
                PrefSettings.settingChanged -= OnPrefsChanged;
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

    static string GetTextForZoomScale(float scale)
    {
        return $"{(int)(scale*100f)}%";
    }
}
