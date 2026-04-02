// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// This class represents a view that displays details about property at a property path, including the property path
/// itself, the type of the property at the end of the path, and the current value of the property.
/// It is used in the path completer to show details about a selected property path. The view is designed to handle
/// unresolved property paths and will display appropriate messages when a property path cannot be resolved or when
/// there is no value at the end of the path.
/// </summary>
class PropertyPathInfoDetailsView : VisualElement
{
    public const string UssClassName = "property-path-info-details-view";
    const string k_ValueLabelUssName = UssClassName + "__data-label";
    const string k_UnresolvedValueLabelUssClassName = k_ValueLabelUssName + "--unresolved";

    static readonly string k_UnresolvedValueText = L10n.Tr("Unresolved Value");
    static readonly string k_NoneText = L10n.Tr("None");

    const string k_VisualTreeAssetPath = "UIToolkitAuthoring/Inspector/Binding/PropertyPathInfoDetailsView.uxml";

    Label m_NameLabel;
    Label m_DataTypeLabel;
    Label m_ValueLabel;

    /// <summary>
    /// The property path being displayed by this view. This is the path that was resolved to get the property details
    /// being displayed.
    /// </summary>
    public string PropertyPath
    {
        get => m_NameLabel.text;
        set => m_NameLabel.text = value;
    }

    /// <summary>
    /// The data type of the property at the property path being displayed by this view.
    /// </summary>
    public string DataType
    {
        get => m_DataTypeLabel.text;
        set => m_DataTypeLabel.text = value;
    }

    /// <summary>
    /// A string representation of the current value of the property at the property path being displayed by this view.
    /// </summary>
    public string Value
    {
        get => m_ValueLabel.text;
        set => m_ValueLabel.text = value;
    }

    /// <summary>
    /// Constructs a PropertyPathInfoDetailsView.
    /// </summary>
    public PropertyPathInfoDetailsView()
    {
        AddToClassList(UssClassName);

        var template = EditorGUIUtility.Load(k_VisualTreeAssetPath) as VisualTreeAsset;
        template.CloneTree(this);
        m_NameLabel = this.Q<Label>("name-label");
        m_DataTypeLabel = this.Q<Label>("data-type-label");
        m_ValueLabel = this.Q<Label>("value-label");

        ClearUI();
    }

    void ClearUI()
    {
        PropertyPath = k_NoneText;
        DataType = k_NoneText;
        Value = k_NoneText;
    }

    /// <summary>
    /// Sets the property path, data type, and value information to be displayed by this view based on the provided
    /// target object, property path info, and property. If the property path cannot be resolved or if there is no value
    /// at the path, appropriate messages will be displayed instead.
    /// This method is typically called when a new property path is selected in the path completer to update the details
    /// view with the information about the newly selected property path.
    /// </summary>
    /// <param name="target">The target object that owns the property</param>
    /// <param name="path">The path to the property to display</param>
    /// <param name="property">The actual property</param>
    public void SetInfo(object target, PropertyPathInfo path, IProperty property)
    {
        ClearUI();

        if (string.IsNullOrEmpty(path.propertyPath.ToString()))
            return;

        var name = path.propertyPath.ToString();
        bool inArray = false;

        if (name.Contains("[0]"))
        {
            name = name.Replace("[0]", "[index]");
            inArray = true;
        }

        PropertyPath = name;
        DataType = path.type.GetDisplayFullName();

        if (property != null && !inArray && PropertyContainer.TryGetValue(target, path.propertyPath, out object v))
        {
            if (v != null)
            {
                if (v is string vStr)
                {
                    Value = "\"" + vStr + "\"";
                }
                else
                {
                    Value = v.ToString();
                }
            }
            else
            {
                Value = "null";
            }

            m_ValueLabel.RemoveFromClassList(k_UnresolvedValueLabelUssClassName);
        }
        else
        {
            Value = k_UnresolvedValueText;
            m_ValueLabel.AddToClassList(k_UnresolvedValueLabelUssClassName);
        }
    }
}
