// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Static GameObject-menu entries for the built-in controls. The set is fixed, so the entries are
/// declared with <see cref="MenuItem"/> and registered by the editor's attribute scan instead of the
/// per-reload dynamic registration <see cref="MenuItemGenerator"/> does for project controls.
/// </summary>
static class StandardElementMenuItems
{
    const string k_Root = "GameObject/UI Toolkit/Standard Elements/";
    const string k_NumericRoot = k_Root + "Numeric Fields/";

    const string k_VisualElementPath = k_Root + "Visual Element";
    const string k_ScrollViewPath = k_Root + "Scroll View";
    const string k_ButtonPath = k_Root + "Button";
    const string k_DropdownFieldPath = k_Root + "Dropdown Field";
    const string k_ImagePath = k_Root + "Image";
    const string k_LabelPath = k_Root + "Label";
    const string k_SliderPath = k_Root + "Slider";
    const string k_TextFieldPath = k_Root + "Text Field";
    const string k_TogglePath = k_Root + "Toggle";
    const string k_DoubleFieldPath = k_NumericRoot + "Double Field";
    const string k_FloatFieldPath = k_NumericRoot + "Float Field";
    const string k_IntegerFieldPath = k_NumericRoot + "Integer Field";
    const string k_LongFieldPath = k_NumericRoot + "Long Field";
    const string k_UILibraryPath = k_Root + "UI Library...";

    [NoAutoStaticsCleanup] // immutable path map, safe to persist
    static readonly Dictionary<string, Type> s_TypeByMenuPath = new()
    {
        [k_VisualElementPath] = typeof(VisualElement),
        [k_ScrollViewPath] = typeof(ScrollView),
        [k_ButtonPath] = typeof(Button),
        [k_DropdownFieldPath] = typeof(DropdownField),
        [k_ImagePath] = typeof(Image),
        [k_LabelPath] = typeof(Label),
        [k_SliderPath] = typeof(Slider),
        [k_TextFieldPath] = typeof(TextField),
        [k_TogglePath] = typeof(Toggle),
        [k_DoubleFieldPath] = typeof(DoubleField),
        [k_FloatFieldPath] = typeof(FloatField),
        [k_IntegerFieldPath] = typeof(IntegerField),
        [k_LongFieldPath] = typeof(LongField),
    };

    public static bool TryGetTypeForMenuPath(string menuPath, out Type type)
    {
        return s_TypeByMenuPath.TryGetValue(menuPath, out type);
    }

    static void AddElement(string menuPath, Type elementType)
    {
        if (Menu.HasContext(menuPath))
            MenuUtility.AddElementAsLastChild(elementType);
        else
            MenuUtility.AddElementAsSibling(elementType);
    }

    // Priorities mirror the order the dynamic registration used to compute: containers first, a
    // separator-sized gap, the flat controls alphabetically, then the grouped numeric fields.
    [MenuItem(k_VisualElementPath, false, 6)]
    static void AddVisualElement() => AddElement(k_VisualElementPath, typeof(VisualElement));

    [MenuItem(k_ScrollViewPath, false, 7)]
    static void AddScrollView() => AddElement(k_ScrollViewPath, typeof(ScrollView));

    [MenuItem(k_ButtonPath, false, 18)]
    static void AddButton() => AddElement(k_ButtonPath, typeof(Button));

    [MenuItem(k_DropdownFieldPath, false, 19)]
    static void AddDropdownField() => AddElement(k_DropdownFieldPath, typeof(DropdownField));

    [MenuItem(k_ImagePath, false, 20)]
    static void AddImage() => AddElement(k_ImagePath, typeof(Image));

    [MenuItem(k_LabelPath, false, 21)]
    static void AddLabel() => AddElement(k_LabelPath, typeof(Label));

    [MenuItem(k_SliderPath, false, 22)]
    static void AddSlider() => AddElement(k_SliderPath, typeof(Slider));

    [MenuItem(k_TextFieldPath, false, 23)]
    static void AddTextField() => AddElement(k_TextFieldPath, typeof(TextField));

    [MenuItem(k_TogglePath, false, 24)]
    static void AddToggle() => AddElement(k_TogglePath, typeof(Toggle));

    [MenuItem(k_DoubleFieldPath, false, 25)]
    static void AddDoubleField() => AddElement(k_DoubleFieldPath, typeof(DoubleField));

    [MenuItem(k_FloatFieldPath, false, 26)]
    static void AddFloatField() => AddElement(k_FloatFieldPath, typeof(FloatField));

    [MenuItem(k_IntegerFieldPath, false, 27)]
    static void AddIntegerField() => AddElement(k_IntegerFieldPath, typeof(IntegerField));

    [MenuItem(k_LongFieldPath, false, 28)]
    static void AddLongField() => AddElement(k_LongFieldPath, typeof(LongField));

    [MenuItem(k_UILibraryPath, false, 49)]
    static void OpenUILibrary() => UIElementsProvider.OpenUIElementsPicker();
}
