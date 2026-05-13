// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement(visibility = LibraryVisibility.Hidden)]
sealed partial class CanvasSettingsInspector : VisualElement
{
    const string k_VisualTreeAsset = "UIToolkitAuthoring/UIViewportWindow/CanvasSettingsInspector.uxml";
    const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";

    public const string UssClass = "unity-ui-canvas-settings";
    public const string HeaderUssClass = UssClass + "__header";
    public const string CanvasSizeUssClass = UssClass + "__canvas-size";
    public const string BackgroundToggleUssClass = UssClass + "__canvas-background";
    public const string CheckerboardMessageUssClass = BackgroundToggleUssClass + "__checkerboard-message";
    public const string HiddenBackgroundUssClass = BackgroundToggleUssClass + "--hidden";
    public const string BackgroundColorUssClass = BackgroundToggleUssClass + "__color-field";
    public const string BackgroundColorOpacityUssClass = BackgroundToggleUssClass + "__color-opacity-field";
    public const string BackgroundImageOpacityUssClass = BackgroundToggleUssClass + "__image-opacity-field";
    public const string BackgroundImageUssClass = BackgroundToggleUssClass + "__image-field";
    public const string ImageStretchModeUssClass = BackgroundToggleUssClass + "__image-stretch-mode";
    public const string FitCanvasToButtonUssClass = BackgroundToggleUssClass + "__fit-canvas-to-image-button";

    CanvasSettings m_Settings;
    CanvasSettingsHeader m_Header;
    ToggleButtonGroup m_CanvasBackgroundToggle;
    HelpBox m_CheckerboardMessage;
    ColorField m_ColorField;
    Slider m_ColorOpacityField;
    ObjectField m_ImageField;
    Slider m_ImageOpacityField;
    ToggleButtonGroup m_ImageStrechMode;
    Button m_FitCanvasToImageButton;

    public CanvasSettings Settings
    {
        get => m_Settings;
        set
        {
            if (m_Settings == value)
                return;
            m_Settings = value;
            m_CanvasBackgroundToggle.value = ConvertToState(m_Settings.BackgroundType);
            m_ImageStrechMode.value = ConvertToState(m_Settings.BackgroundImageScaleMode);
            SetFieldsVisibility(m_Settings.BackgroundType);
        }
    }

    public CanvasSettingsInspector()
    {
        AddToClassList(UssClass);
        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        styleSheets.Add(styleSheet);

        m_Header = this.Q<CanvasSettingsHeader>(className:HeaderUssClass);
        m_Header.TypeIcon = EditorGUIUtility.Load("VisualTreeAsset Icon") as Texture2D;

        var sizeField = this.Q(className: CanvasSizeUssClass);
        var xInput = sizeField.Q<FloatField>("unity-x-input");
        xInput.label = "Width";
        xInput.isDelayed = true;
        var yInput = sizeField.Q<FloatField>("unity-y-input");
        yInput.label = "Height";
        yInput.isDelayed = true;

        m_CanvasBackgroundToggle = this.Q<ToggleButtonGroup>(className: BackgroundToggleUssClass);
        m_CanvasBackgroundToggle.GetButton(0).iconImage = EditorGUIUtility.Load("UIToolkitAuthoring/UIViewportWindow/checkerboard_16x16.png") as Texture2D;
        m_CanvasBackgroundToggle.GetButton(1).iconImage = EditorGUIUtility.Load("UIToolkitAuthoring/UIViewportWindow/ColorPicker.png") as Texture2D;
        m_CanvasBackgroundToggle.GetButton(2).iconImage = EditorGUIUtility.Load("UIToolkitAuthoring/UIViewportWindow/RawImage.png") as Texture2D;

        m_CheckerboardMessage = this.Q<HelpBox>(className:CheckerboardMessageUssClass);

        m_ColorField = this.Q<ColorField>(className:BackgroundColorUssClass);
        m_ColorOpacityField = this.Q<Slider>(className:BackgroundColorOpacityUssClass);
        m_ImageField = this.Q<ObjectField>(className:BackgroundImageUssClass);
        m_ImageField.objectType = typeof(Texture2D);
        m_ImageOpacityField = this.Q<Slider>(className:BackgroundImageOpacityUssClass);
        m_ImageStrechMode = this.Q<ToggleButtonGroup>(className: ImageStretchModeUssClass);
        m_ImageStrechMode.GetButton(0).iconImage = EditorGUIUtility.Load("UIToolkitAuthoring/UIViewportWindow/Stretch To Fill.png") as Texture2D;
        m_ImageStrechMode.GetButton(1).iconImage = EditorGUIUtility.Load("UIToolkitAuthoring/UIViewportWindow/Scale And Crop.png") as Texture2D;
        m_ImageStrechMode.GetButton(2).iconImage = EditorGUIUtility.Load("UIToolkitAuthoring/UIViewportWindow/Scale To Fit.png") as Texture2D;
        m_FitCanvasToImageButton = this.Q<Button>(className:FitCanvasToButtonUssClass);

    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent:
                m_CanvasBackgroundToggle.RegisterValueChangedCallback(OnCanvasBackgroundChanged);
                m_ImageStrechMode.RegisterValueChangedCallback(OnImageStretchModeChanged);
                m_FitCanvasToImageButton.clicked += OnFitCanvasToImage;
                break;
            case DetachFromPanelEvent:
                m_CanvasBackgroundToggle.UnregisterValueChangedCallback(OnCanvasBackgroundChanged);
                m_ImageStrechMode.UnregisterValueChangedCallback(OnImageStretchModeChanged);
                m_FitCanvasToImageButton.clicked -= OnFitCanvasToImage;
                break;
        }
        base.HandleEventBubbleUp(evt);
    }

    void OnCanvasBackgroundChanged(ChangeEvent<ToggleButtonGroupState> evt)
    {
        var type  = ConvertBackgroundType(evt.newValue);
        var so = new SerializedObject(m_Settings);
        so.FindProperty("m_" + nameof(CanvasSettings.BackgroundType)).enumValueIndex = (int)type;
        so.ApplyModifiedProperties();
        so.Update();
        SetFieldsVisibility(type);
    }

    void OnImageStretchModeChanged(ChangeEvent<ToggleButtonGroupState> evt)
    {
        var type  = ConvertToScaleMode(evt.newValue);
        var so = new SerializedObject(m_Settings);
        so.FindProperty("m_" + nameof(CanvasSettings.BackgroundImageScaleMode)).enumValueIndex = (int)type;
        so.ApplyModifiedProperties();
        so.Update();
    }

    void OnFitCanvasToImage()
    {
        if (m_Settings.BackgroundImage == null)
            return;
        Undo.RegisterCompleteObjectUndo(m_Settings, "Canvas Settings Changed");
        m_Settings.CanvasSize = new(m_Settings.BackgroundImage.width, m_Settings.BackgroundImage.height);
        EditorUtility.SetDirty(m_Settings);
    }

    void SetFieldsVisibility(CanvasBackgroundType type)
    {
        m_CheckerboardMessage.EnableInClassList(HiddenBackgroundUssClass, type is not CanvasBackgroundType.Checkerboard);
        m_ColorField.EnableInClassList(HiddenBackgroundUssClass, type is not CanvasBackgroundType.Color);
        m_ColorOpacityField.EnableInClassList(HiddenBackgroundUssClass, type is not (CanvasBackgroundType.Color));
        m_ImageField.EnableInClassList(HiddenBackgroundUssClass, type is not CanvasBackgroundType.Image);
        m_ImageStrechMode.EnableInClassList(HiddenBackgroundUssClass, type is not CanvasBackgroundType.Image);
        m_ImageOpacityField.EnableInClassList(HiddenBackgroundUssClass, type is not CanvasBackgroundType.Image);
        m_FitCanvasToImageButton.EnableInClassList(HiddenBackgroundUssClass, type is not CanvasBackgroundType.Image);
    }

    static CanvasBackgroundType ConvertBackgroundType(ToggleButtonGroupState state)
    {
        return state.data switch
        {
            0b001 => CanvasBackgroundType.Checkerboard,
            0b010 => CanvasBackgroundType.Color,
            0b100 => CanvasBackgroundType.Image,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }

    static ToggleButtonGroupState ConvertToState(CanvasBackgroundType type)
    {
        return type switch
        {
            CanvasBackgroundType.Checkerboard => new (0b001, 3),
            CanvasBackgroundType.Color => new (0b010, 3),
            CanvasBackgroundType.Image => new (0b100, 3),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    static ScaleMode ConvertToScaleMode(ToggleButtonGroupState state)
    {
        return state.data switch
        {
            0b001 => ScaleMode.StretchToFill,
            0b010 => ScaleMode.ScaleAndCrop,
            0b100 => ScaleMode.ScaleToFit,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }

    static ToggleButtonGroupState ConvertToState(ScaleMode mode)
    {
        return mode switch
        {
            ScaleMode.StretchToFill => new (0b001, 3),
            ScaleMode.ScaleAndCrop => new (0b010, 3),
            ScaleMode.ScaleToFit => new (0b100, 3),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}
