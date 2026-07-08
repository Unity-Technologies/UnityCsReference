// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

static class BuiltInElementConfigurations
{
    [UILibraryVariantConfiguration(typeof(VisualElement), "With flex-grow")]
    static void ConfigureStyledVisualElement(ElementConfigurationContext context)
    {
        context.SetStyle("flex-grow", 1.0f);
    }

    [UILibraryDefaultConfiguration(typeof(TwoPaneSplitView))]
    static void ConfigureTwoPaneSplitView(ElementConfigurationContext context)
    {
        // A TwoPaneSplitView needs two child panes to be usable out of the box, otherwise it logs an errors in the
        // Console.
        context.AddChild<VisualElement>();
        context.AddChild<VisualElement>();
    }

    [UILibraryDefaultConfiguration(typeof(Label))]
    static void ConfigureLabel(ElementConfigurationContext context)
    {
        context.SetAttribute("text", "Label");
    }

    [UILibraryDefaultConfiguration(typeof(Button))]
    static void ConfigureButton(ElementConfigurationContext context)
    {
        context.SetAttribute("text", "Button");
    }

    [UILibraryDefaultConfiguration(typeof(Toggle))]
    static void ConfigureToggle(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Toggle");
    }

    [UILibraryDefaultConfiguration(typeof(ToggleButtonGroup))]
    static void ConfigureToggleButtonGroup(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Toggle Button Group");
    }

    [UILibraryDefaultConfiguration(typeof(Scroller))]
    static void ConfigureScroller(ElementConfigurationContext context)
    {
        context.SetAttribute("low-value", 0f);
        context.SetAttribute("high-value", 100f);
        context.SetAttribute("direction", SliderDirection.Horizontal);
        context.SetAttribute("value", 42f);
    }

    [UILibraryDefaultConfiguration(typeof(TextField))]
    static void ConfigureTextField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Text Field");
        context.SetAttribute("placeholder-text", "filler text");
    }

    [UILibraryDefaultConfiguration(typeof(Foldout))]
    static void ConfigureFoldout(ElementConfigurationContext context)
    {
        context.SetAttribute("text", "Foldout");
    }

    [UILibraryDefaultConfiguration(typeof(Slider))]
    static void ConfigureSlider(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Slider");
        context.SetAttribute("low-value", 0f);
        context.SetAttribute("high-value", 100f);
        context.SetAttribute("value", 42f);
    }

    [UILibraryDefaultConfiguration(typeof(SliderInt))]
    static void ConfigureSliderInt(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "SliderInt");
        context.SetAttribute("low-value", 0);
        context.SetAttribute("high-value", 100);
        context.SetAttribute("value", 42);
    }

    [UILibraryDefaultConfiguration(typeof(MinMaxSlider))]
    static void ConfigureMinMaxSlider(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Min/Max Slider");
        context.SetAttribute("low-limit", -10f);
        context.SetAttribute("high-limit", 40f);
        context.SetAttribute("value", new Vector2(10, 12));
    }

    [UILibraryDefaultConfiguration(typeof(ProgressBar))]
    static void ConfigureProgressBar(ElementConfigurationContext context)
    {
        context.SetAttribute("title", "my-progress");
        context.SetAttribute("value", 22f);
    }

    [UILibraryDefaultConfiguration(typeof(DropdownField))]
    static void ConfigureDropdownField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Dropdown");
    }

    [UILibraryDefaultConfiguration(typeof(EnumField))]
    static void ConfigureEnumField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Enum");
        context.SetAttribute("type", UxmlUtility.TypeToString(typeof(TextAlignment)));
        context.SetAttribute("value", nameof(TextAlignment.Center));
    }

    [UILibraryDefaultConfiguration(typeof(RadioButton))]
    static void ConfigureRadioButton(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Radio Button");
    }

    [UILibraryDefaultConfiguration(typeof(RadioButtonGroup))]
    static void ConfigureRadioButtonGroup(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Radio Button Group");
    }

    [UILibraryDefaultConfiguration(typeof(HelpBox))]
    static void ConfigureHelpBox(ElementConfigurationContext context)
    {
        context.SetAttribute("text", "Help Box");
    }

    [UILibraryDefaultConfiguration(typeof(MaskField))]
    static void ConfigureMaskField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Mask");
    }

    [UILibraryDefaultConfiguration(typeof(Mask64Field))]
    static void ConfigureMask64Field(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Mask64");
    }

    [UILibraryDefaultConfiguration(typeof(TabView))]
    static void ConfigureTabView(ElementConfigurationContext context)
    {
        var childCtx = context.AddChild<Tab>();
        ConfigureTab(childCtx);
    }

    [UILibraryDefaultConfiguration(typeof(Tab))]
    static void ConfigureTab(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Tab");
    }

    [UILibraryDefaultConfiguration(typeof(IntegerField))]
    static void ConfigureIntegerField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Integer Field");
        context.SetAttribute("value", 42);
    }

    [UILibraryDefaultConfiguration(typeof(FloatField))]
    static void ConfigureFloatField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Float Field");
        context.SetAttribute("value", 42.2f);
    }

    [UILibraryDefaultConfiguration(typeof(LongField))]
    static void ConfigureLongField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Long Field");
        context.SetAttribute("value", 42L);
    }

    [UILibraryDefaultConfiguration(typeof(DoubleField))]
    static void ConfigureDoubleField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Double Field");
        context.SetAttribute("value", 42.2);
    }

    [UILibraryDefaultConfiguration(typeof(Hash128Field))]
    static void ConfigureHash128Field(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Hash128 Field");
        context.SetAttribute("value", Hash128.Compute("42"));
    }

    [UILibraryDefaultConfiguration(typeof(Vector2Field))]
    static void ConfigureVector2Field(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Vec2 Field");
    }

    [UILibraryDefaultConfiguration(typeof(Vector3Field))]
    static void ConfigureVector3Field(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Vec3 Field");
    }

    [UILibraryDefaultConfiguration(typeof(Vector4Field))]
    static void ConfigureVector4Field(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Vec4 Field");
    }

    [UILibraryDefaultConfiguration(typeof(RectField))]
    static void ConfigureRectField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Rect");
    }

    [UILibraryDefaultConfiguration(typeof(BoundsField))]
    static void ConfigureBoundsField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Bounds");
    }

    [UILibraryDefaultConfiguration(typeof(UnsignedIntegerField))]
    static void ConfigureUnsignedIntegerField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Unsigned Integer Field");
        context.SetAttribute("value", 42u);
    }

    [UILibraryDefaultConfiguration(typeof(UnsignedLongField))]
    static void ConfigureUnsignedLongField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Unsigned Long Field");
        context.SetAttribute("value", 42UL);
    }

    [UILibraryDefaultConfiguration(typeof(Vector2IntField))]
    static void ConfigureVector2IntField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Vector2Int");
    }

    [UILibraryDefaultConfiguration(typeof(Vector3IntField))]
    static void ConfigureVector3IntField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Vector3Int");
    }

    [UILibraryDefaultConfiguration(typeof(RectIntField))]
    static void ConfigureRectIntField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "RectInt");
    }

    [UILibraryDefaultConfiguration(typeof(BoundsIntField))]
    static void ConfigureBoundsIntField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "BoundsInt");
    }

    [UILibraryDefaultConfiguration(typeof(ColorField))]
    static void ConfigureColorField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Color");
        context.SetAttribute("value", Color.cyan);
    }

    [UILibraryDefaultConfiguration(typeof(CurveField))]
    static void ConfigureCurveField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Curve");
        context.SetAttribute("value", new AnimationCurve(new Keyframe(0, 0), new Keyframe(5, 8), new Keyframe(10, 4)));
    }

    [UILibraryDefaultConfiguration(typeof(GradientField))]
    static void ConfigureGradientField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Gradient");
        context.SetAttribute("value", new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(Color.red, 0),
                new GradientColorKey(Color.blue, .33f),
                new GradientColorKey(Color.green, .66f)
            }
        });
    }

    [UILibraryDefaultConfiguration(typeof(TagField))]
    static void ConfigureTagField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Tag");
        context.SetAttribute("value", "Player");
    }

    [UILibraryDefaultConfiguration(typeof(LayerField))]
    static void ConfigureLayerField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Layer");
    }

    [UILibraryDefaultConfiguration(typeof(LayerMaskField))]
    static void ConfigureLayerMaskField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "LayerMask");
    }

    [UILibraryDefaultConfiguration(typeof(EnumFlagsField))]
    static void ConfigureEnumFlagsField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "EnumFlags");
        context.SetAttribute("type", UxmlUtility.TypeToString(typeof(UsageHints)));
        context.SetAttribute("value", nameof(UsageHints.DynamicTransform));
    }

    [UILibraryDefaultConfiguration(typeof(ToolbarButton))]
    static void ConfigureToolbarButton(ElementConfigurationContext context)
    {
        context.SetAttribute("text", "Button");
    }

    [UILibraryDefaultConfiguration(typeof(ToolbarToggle))]
    static void ConfigureToolbarToggle(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Toggle");
    }

    [UILibraryDefaultConfiguration(typeof(ObjectField))]
    static void ConfigureObjectField(ElementConfigurationContext context)
    {
        context.SetAttribute("label", "Object Field");
    }
}
