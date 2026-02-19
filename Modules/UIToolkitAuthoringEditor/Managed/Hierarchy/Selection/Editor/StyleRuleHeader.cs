// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
partial class StyleRuleHeader : UISelectionObjectHeader
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
                    new(nameof(RuleName), "rule-name"),
                }
                , true);
        }

#pragma warning disable 649
        [SerializeField] private string RuleName;

        [SerializeField, UxmlIgnore, HideInInspector] private UxmlAttributeFlags RuleName_UxmlAttributeFlags;
#pragma warning restore 649

        public override object CreateInstance()
        {
            return new StyleRuleHeader();
        }

        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);
            var ve = (StyleRuleHeader)obj;
            if (ShouldWriteAttributeValue(RuleName_UxmlAttributeFlags))
                ve.RuleName = RuleName;
        }
    }

    public static readonly BindingId ElementProperty = nameof(Rule);
    public static readonly BindingId RuleNamProperty = nameof(RuleName);

    public new const string UssClass = "unity-style-rule-header";
    public const string RuleNameUssClass = UssClass + "__rule-name";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/StyleRuleHeader.uxml";

    static readonly StyleSheetNodeTypeHandler.StyleSheetEditorExporter s_Exporter = new();

    private TextField m_RuleName;
    private StyleRule m_Rule;

    protected override VisualTreeAsset IdentifierDetails => EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;

    [UxmlAttribute, CreateProperty]
    public string RuleName
    {
        get => m_RuleName.value;
        set
        {
            if (string.CompareOrdinal(m_RuleName.value, value) == 0)
                return;
            m_RuleName.value = value;
            NotifyPropertyChanged(RuleNamProperty);
        }
    }

    [CreateProperty]
    public StyleRule Rule
    {
        get => m_Rule;
        set
        {
            if (m_Rule == value)
                return;
            m_Rule = value;

            m_RuleName.dataSource = Rule;

            if (m_Rule == null)
            {
                TypeIcon = UIResources.GetIconForType(typeof(StyleSheet), UIResources.RequestSize.Px32);
                RuleName = null;
                TypeName = nameof(StyleSheet);

                m_RuleName.ClearBinding(TextField.valueProperty);
                m_RuleName.value = null;
            }
            else
            {
                TypeIcon = UIResources.GetIconForType(typeof(StyleSheet), UIResources.RequestSize.Px32);
                TypeName = TypeUtility.GetTypeDisplayName(m_Rule.GetType());
                RuleName = s_Exporter.ToUssString(m_Rule.styleSheet, m_Rule.complexSelectors);
            }
            NotifyPropertyChanged(ElementProperty);
        }
    }

    public StyleRuleHeader()
    {
        AddToClassList(UssClass);

        TypeIcon = UIResources.GetIconForType(typeof(StyleSheet), UIResources.RequestSize.Px32);
        TypeName = "Rule";

        m_RuleName = this.Q<TextField>(className: RuleNameUssClass);
        m_RuleName.SetEnabled(false);
        m_RuleName.dataSource = this;
    }
}
