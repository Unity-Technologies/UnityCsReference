// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// This class represents a single item used in the path completer. It displays the name and type of a property at a
/// property path.
/// </summary>
class PropertyPathInfoViewItem : VisualElement
{
    public const string UssClassName = "property-path-info-view-item";
    private Label m_NameLabel;
    private Label m_TypeLabel;

    const string k_VisualTreeAssetPath = "UIToolkitAuthoring/Inspector/Binding/PropertyPathInfoViewItem.uxml";

    /// <summary>
    /// The property path represented by this item.
    /// </summary>
    public string PropertyPath
    {
        get => m_NameLabel.text;
        set => m_NameLabel.text = value;
    }

    /// <summary>
    /// The type of the property at the property path represented by this item.
    /// </summary>
    public Type PropertyType
    {
        set
        {
            m_TypeLabel.text = TypeUtility.GetTypeDisplayName(value);
            m_TypeLabel.tooltip = value.GetDisplayFullName();
        }
    }

    /// <summary>
    /// Constructs a PropertyPathInfoViewItem.
    /// </summary>
    public PropertyPathInfoViewItem()
    {
        AddToClassList(UssClassName);

        var template = EditorGUIUtility.Load(k_VisualTreeAssetPath) as VisualTreeAsset;

        template.CloneTree(this);

        m_NameLabel = this.Q<Label>("nameLabel");
        m_TypeLabel = this.Q<Label>("typeLabel");
    }
}
