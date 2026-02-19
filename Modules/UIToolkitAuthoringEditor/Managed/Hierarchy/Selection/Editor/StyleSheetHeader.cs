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
partial class StyleSheetHeader : UISelectionObjectHeader
{
    [Serializable]
    public new class UxmlSerializedData : UISelectionObjectHeader.UxmlSerializedData
    {
        [Conditional("UNITY_EDITOR"), RegisterUxmlCache]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(StyleSheetName), "style-sheet-name"),
                }
                , true);
        }

#pragma warning disable 649
        [SerializeField] private string StyleSheetName;

        [SerializeField, UxmlIgnore, HideInInspector] private UxmlAttributeFlags StyleSheetName_UxmlAttributeFlags;
#pragma warning restore 649

        public override object CreateInstance()
        {
            return new StyleSheetHeader();
        }

        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);
            var ve = (StyleSheetHeader)obj;
            if (ShouldWriteAttributeValue(StyleSheetName_UxmlAttributeFlags))
                ve.StyleSheetName = StyleSheetName;
        }
    }

    public static readonly BindingId StyleSheetProperty = nameof(StyleSheet);
    public static readonly BindingId StyleSheetNameProperty = nameof(StyleSheetName);

    public new const string UssClass = "unity-stylesheet-header";
    public const string StyleSheetNameUssClass = UssClass + "__stylesheet-name";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/StyleSheetHeader.uxml";

    private TextField m_StyleSheetName;
    private StyleSheet m_StyleSheet;

    protected override VisualTreeAsset IdentifierDetails => EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;

    [UxmlAttribute, CreateProperty]
    public string StyleSheetName
    {
        get => m_StyleSheetName.value;
        set
        {
            if (string.CompareOrdinal(m_StyleSheetName.value, value) == 0)
                return;
            m_StyleSheetName.value = value;
            NotifyPropertyChanged(StyleSheetNameProperty);
        }
    }

    [CreateProperty]
    public StyleSheet StyleSheet
    {
        get => m_StyleSheet;
        set
        {
            if (m_StyleSheet == value)
                return;
            m_StyleSheet = value;

            m_StyleSheetName.dataSource = StyleSheet;

            if (m_StyleSheet == null)
            {
                TypeIcon = UIResources.GetIconForType(typeof(StyleSheet), UIResources.RequestSize.Px32);
                StyleSheetName = null;
                TypeName = nameof(StyleSheet);

                m_StyleSheetName.value = null;
            }
            else
            {
                TypeIcon = UIResources.GetIconForType(typeof(StyleSheet), UIResources.RequestSize.Px32);
                TypeName = TypeUtility.GetTypeDisplayName(m_StyleSheet.GetType());
                StyleSheetName = m_StyleSheet.name;
            }
            NotifyPropertyChanged(StyleSheetProperty);
        }
    }

    public StyleSheetHeader()
    {
        AddToClassList(UssClass);

        TypeIcon = UIResources.GetIconForType(typeof(StyleSheet), UIResources.RequestSize.Px32);
        TypeName = nameof(StyleSheet);

        m_StyleSheetName = this.Q<TextField>(className: StyleSheetNameUssClass);
        m_StyleSheetName.SetEnabled(false);
        m_StyleSheetName.dataSource = this;
    }
}
