// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
partial class VisualElementHeader : UISelectionObjectHeader
{
    [Serializable]
    public new class UxmlSerializedData : UISelectionObjectHeader.UxmlSerializedData
    {
        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
        /// </summary>
        [Conditional("UNITY_EDITOR"), RegisterUxmlCache]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(Enabled), "enabled"),
                    new(nameof(ElementName), "element-name"),

                }
                , true);
        }

#pragma warning disable 649
        [SerializeField] private bool Enabled;
        [SerializeField] private string ElementName;

        [SerializeField, UxmlIgnore, HideInInspector] private UxmlAttributeFlags Enabled_UxmlAttributeFlags;
        [SerializeField, UxmlIgnore, HideInInspector] private UxmlAttributeFlags ElementName_UxmlAttributeFlags;
#pragma warning restore 649

        public override object CreateInstance()
        {
            return new VisualElementHeader();
        }

        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);
            var ve = (VisualElementHeader)obj;
            if (ShouldWriteAttributeValue(Enabled_UxmlAttributeFlags))
                ve.Enabled = Enabled;
            if (ShouldWriteAttributeValue(ElementName_UxmlAttributeFlags))
                ve.ElementName = ElementName;
        }
    }

    public static readonly BindingId ElementProperty = nameof(Element);
    public static readonly BindingId ElementNameProperty = nameof(ElementName);
    public static readonly BindingId EnabledProperty = nameof(Enabled);

    public new const string UssClass = "unity-visual-element-header";
    public const string ElementNameUssClass = UssClass + "__element-name";
    public const string ElementEnabledUssClass = UssClass + "__element-enabled";

    [NoAutoStaticsCleanup]
    private static DataBinding s_NameBinding = null;
    private static DataBinding k_NameBinding
    {
        get
        {
            if(s_NameBinding == null)
                s_NameBinding = new() { dataSourcePath = nameProperty };
            return s_NameBinding;
        }
    }

    [NoAutoStaticsCleanup]
    private static DataBinding s_EnabledBinding = null;

    private static DataBinding k_EnabledBinding
    {
        get
        {
            if (s_EnabledBinding == null)
                s_EnabledBinding = new() { dataSourcePath = enabledSelfProperty };
            return s_EnabledBinding;
        }
    }

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualElementHeader.uxml";

    private Toggle m_Enabled;
    private TextField m_ElementName;
    private VisualElement m_Element;

    protected override VisualTreeAsset IdentifierDetails => EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;

    [UxmlAttribute, CreateProperty]
    public bool Enabled
    {
        get => m_Enabled.value;
        set
        {
            if (m_Enabled.value == value)
                return;
            m_Enabled.value = value;
            NotifyPropertyChanged(EnabledProperty);
        }
    }

    [UxmlAttribute, CreateProperty]
    public string ElementName
    {
        get => m_ElementName.value;
        set
        {
            if (string.CompareOrdinal(m_ElementName.value, value) == 0)
                return;
            m_ElementName.value = value;
            NotifyPropertyChanged(ElementNameProperty);
        }
    }

    [CreateProperty]
    public VisualElement Element
    {
        get => m_Element;
        set
        {
            if (m_Element == value)
                return;
            m_Element = value;

            m_Enabled.dataSource = Element;
            m_ElementName.dataSource = Element;

            if (m_Element == null)
            {
                TypeIcon = UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px32);
                ElementName = null;
                TypeName = nameof(VisualElement);

                m_Enabled.ClearBinding(Toggle.valueProperty);
                m_Enabled.value = false;
                m_ElementName.ClearBinding(TextField.valueProperty);
                m_ElementName.value = null;
            }
            else
            {
                TypeIcon = UIResources.GetIconForElement(m_Element, UIResources.RequestSize.Px32);
                TypeName = TypeUtility.GetTypeDisplayName(m_Element.GetType());
                m_Enabled.SetBinding(Toggle.valueProperty, k_EnabledBinding);
                Enabled = m_Element.enabledSelf;
                m_ElementName.SetBinding(TextField.valueProperty, k_NameBinding);
                ElementName = m_Element.name;
            }
            NotifyPropertyChanged(ElementProperty);
        }
    }

    public VisualElementHeader()
    {
        AddToClassList(UssClass);

        TypeIcon = UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px32);
        TypeName = "VisualElement";

        m_ElementName = this.Q<TextField>(className: ElementNameUssClass);
        m_ElementName.dataSource = this;

        m_Enabled = this.Q<Toggle>(className: ElementEnabledUssClass);
        m_Enabled.dataSource = this;
    }
}
