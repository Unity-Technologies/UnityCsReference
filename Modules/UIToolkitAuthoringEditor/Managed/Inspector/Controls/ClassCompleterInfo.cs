// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
struct ClassCompleterInfo
{
    public ClassCompleterInfo(StyleSelectorPart selectorPart, StyleSheet sheet)
    {
        StyleSelectorPart = selectorPart;
        StyleSheet = sheet;
    }

    public ClassCompleterInfo(StyleSheet sheet)
    {
        StyleSheet = sheet;
        StyleSelectorPart = default;
    }

    public StyleSelectorPart StyleSelectorPart { get; set; }

    public StyleSheet StyleSheet { get; set; }

    public readonly bool IsValidClassInfo() => StyleSheet && !string.IsNullOrEmpty(StyleSelectorPart.value) && StyleSelectorPart.type == StyleSelectorType.Class;
    public readonly bool IsValidStyleSheetInfo() => StyleSheet && string.IsNullOrEmpty(StyleSelectorPart.value);
    public readonly bool IsCreateNewClassField() => !IsValidClassInfo() && !IsValidStyleSheetInfo();
}
