// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
internal abstract partial class UISelectionObjectHeader : VisualElement
{
    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
        /// </summary>
        [Conditional("UNITY_EDITOR"), RegisterUxmlCache]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(TypeIcon), "type-icon"),
                    new(nameof(TypeName), "type-name"),

                }
                , true);
        }

#pragma warning disable 649
        [SerializeField] private Background TypeIcon;
        [SerializeField] private string TypeName;

        [SerializeField, UxmlIgnore, HideInInspector] private UxmlAttributeFlags TypeIcon_UxmlAttributeFlags;
        [SerializeField, UxmlIgnore, HideInInspector] private UxmlAttributeFlags TypeName_UxmlAttributeFlags;
#pragma warning restore 649

        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);
            var ve = (UISelectionObjectHeader)obj;
            if (ShouldWriteAttributeValue(TypeIcon_UxmlAttributeFlags))
                ve.TypeIcon = TypeIcon;
            if (ShouldWriteAttributeValue(TypeName_UxmlAttributeFlags))
                ve.TypeName = TypeName;
        }
    }

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
