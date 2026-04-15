// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Abstracts what the variable editing stack needs from its host inspector.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal interface IVariableEditingContext
{
    VisualElement CurrentVisualElement { get; }
    StyleRule CurrentRule { get; }
    StyleSheet CurrentStyleSheet { get; }
    bool EditorExtensionMode { get; }
    bool IsSelectorElement { get; }
    VisualElement TooltipRoot { get; }

    void SetVariable(string variableName, BindableElement field, string styleName, int index);
    void UnsetVariable(BindableElement field);
    void RefreshUI();

    /// <summary>
    /// Resolves the bound variable name from the current style rule using the style property manipulator.
    /// Returns null if no variable binding is found or the property is not set on the current rule.
    /// </summary>
    string GetBoundVariableNameFromCurrentRule(string styleName, int index);

    /// <summary>
    /// Resolves the bound variable name by walking matched style rules (read-only mode).
    /// Returns null if no variable binding is found.
    /// </summary>
    string GetBoundVariableNameFromMatchedRules(string styleName, int index);
}
