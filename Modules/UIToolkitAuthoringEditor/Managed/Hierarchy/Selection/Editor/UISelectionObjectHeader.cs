// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
internal abstract partial class UISelectionObjectHeader : VisualElement
{
    public static readonly BindingId TypeIconProperty = nameof(TypeIcon);
    public static readonly BindingId TypeNameProperty = nameof(TypeName);

    public const string UssClass = "unity-ui-selection-object-header";
    public const string ContainerUssClass = UssClass + "__container";
    public const string ObjectIdentifierRowUssClass = UssClass + "__object-identifier-row";
    public const string ObjectIdentifierUssClass = UssClass + "__object-identifier";
    public const string ObjectIdentifierDetailsUssClass = ObjectIdentifierUssClass + "__details";
    public const string ObjectTypeIconUssClass = UssClass + "__object-type-icon";
    public const string ObjectTypeNameUssClass = UssClass + "__object-type-name";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/UISelectionObjectHeader.uxml";

    private VisualElement m_TypeIcon;
    private Label m_TypeName;

    [UxmlAttribute, CreateProperty]
    public Background TypeIcon
    {
        get => m_TypeIcon.style.backgroundImage.value;
        set
        {
            if (m_TypeIcon.style.backgroundImage == value)
                return;
            m_TypeIcon.style.backgroundImage = value;
            NotifyPropertyChanged(TypeIconProperty);
        }
    }

    [UxmlAttribute]
    public string TypeName
    {
        get => m_TypeName.text;
        set
        {
            if (string.CompareOrdinal(m_TypeName.text, value) == 0)
                return;
            m_TypeName.text = value;
            NotifyPropertyChanged(TypeNameProperty);
        }
    }

    protected abstract VisualTreeAsset IdentifierDetails { get; }

    protected UISelectionObjectHeader()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        m_TypeIcon = this.Q(className: ObjectTypeIconUssClass);
        m_TypeName = this.Q<Label>(className: ObjectTypeNameUssClass);

        var identifierVta = IdentifierDetails;
        if (identifierVta)
        {
            identifierVta.CloneTree(this.Q(className: ObjectIdentifierDetailsUssClass));
        }
    }
}
