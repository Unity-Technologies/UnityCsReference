// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Represents an inspector element that displays bindings information of a selected VisualElement.
/// </summary>
/// <remarks>
/// This view only shows data source information.
/// </remarks>
[UxmlElement]
sealed partial class VisualElementBindingsInspectorElement : VisualElement
{
    public const string UssClassName = "unity-bindings-inspector";
    const string k_VisualTreeAssetPath = "UIToolkitAuthoring/Inspector/VisualElementBindingsInspectorElement.uxml";

    readonly UxmlAttributesView m_AttributesView;

    public UxmlAttributesView AttributesView => m_AttributesView;

    /// <summary>
    /// Constructor for the VisualElementBindingsInspectorElement.
    /// </summary>
    public VisualElementBindingsInspectorElement()
    {
        AddToClassList(UssClassName);

        var visualTreeAsset = EditorGUIUtility.LoadRequired(k_VisualTreeAssetPath) as VisualTreeAsset;

        visualTreeAsset.CloneTree(this);
        m_AttributesView = this.Q<UxmlAttributesView>("RootElement");
    }
}
