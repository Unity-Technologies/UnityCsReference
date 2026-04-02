// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class VariablesListItem : VisualElement
    {
        public static readonly string k_NameFieldName = "item-name-field";
        protected static readonly string k_HiddenFieldClassName = "variable-field-hidden";
        protected static readonly string k_AssetFieldName = "item-asset-field";
        static readonly string k_VisibleFieldClassName = "variable-field-visible";
        static readonly string k_FieldGroupName = "field-group";
        static readonly string k_ValueFieldName = "item-value-field";
        static readonly string k_FloatFieldName = "item-float-field";
        static readonly string k_LengthFieldName = "item-length-field";
        static readonly string k_AngleFieldName = "item-angle-field";
        static readonly string k_TimeValueFieldName = "item-time-value-field";
        static readonly string k_ColorFieldName = "item-color-field";
        static readonly string k_KeywordFieldName = "item-keyword-field";
        static readonly string k_TypeFieldName = "item-type-field";
        static readonly string k_PrefixClassName = "variable-prefix";
        static readonly string k_ListViewItemClassName = "list-view-item";

        internal Label itemLabel;
        internal VisualElement fieldsGroup;
        internal TextField itemNameField;
        internal TextField itemValueField;
        internal FloatField itemFloatField;
        internal ColorField itemColorField;
        internal DropdownField itemKeywordField;
        internal DropdownField itemTypeField;
        internal AngleField itemAngleField;
        internal LengthField itemLengthField;
        internal TimeValueField itemTimeValueField;
        internal BaseField<UnityEngine.Object> itemAssetField;
        internal ContextualMenuManipulator contextualMenuManipulator;

        public VariablesListItem()
        {
            AddToClassList(k_ListViewItemClassName);
            fieldsGroup = new VisualElement().WithClassList(k_FieldGroupName);
            fieldsGroup.Add(itemLabel = new Label(VariablesInspector.k_VariablePrefix)
                { name = k_PrefixClassName }.WithClassList(k_PrefixClassName));
            fieldsGroup.Add(itemNameField = new TextField()
                { name = k_NameFieldName, isDelayed = true }.WithClassList(k_VisibleFieldClassName));
            fieldsGroup.Add(itemValueField = new TextField()
                { name = k_ValueFieldName, isDelayed = true }.WithClassList(k_HiddenFieldClassName));
            fieldsGroup.Add(itemFloatField = new FloatField()
                { name = k_FloatFieldName, isDelayed = true }.WithClassList(k_HiddenFieldClassName));
            fieldsGroup.Add(itemColorField = new ColorField()
                { name = k_ColorFieldName }.WithClassList(k_HiddenFieldClassName));
            fieldsGroup.Add(itemLengthField = new LengthField()
                { name = k_LengthFieldName }.WithClassList(k_HiddenFieldClassName));
            fieldsGroup.Add(itemAngleField = new AngleField()
                { name = k_AngleFieldName }.WithClassList(k_HiddenFieldClassName));
            fieldsGroup.Add(itemTimeValueField = new TimeValueField()
                { name = k_TimeValueFieldName }.WithClassList(k_HiddenFieldClassName));
            fieldsGroup.Add(itemKeywordField = new DropdownField()
                { name = k_KeywordFieldName }.WithClassList(k_HiddenFieldClassName));
            fieldsGroup.Add(itemTypeField = new DropdownField()
                { name = k_TypeFieldName }.WithClassList(k_VisibleFieldClassName));

            fieldsGroup.Add(itemAssetField = CreateAssetField());
            Add(fieldsGroup);

            foreach (var choice in VariablesInspector.s_KeywordArray)
            {
                itemKeywordField.choices.Add(choice.ToString());
            }

            foreach (var choice in Enum.GetValues(typeof(VariablesInspector.VariableType)))
            {
                itemTypeField.choices.Add(choice.ToString());
            }
        }

        protected virtual BaseField<UnityEngine.Object> CreateAssetField()
        {
            return new ObjectField() { name = k_AssetFieldName }.WithClassList(k_HiddenFieldClassName);
        }
    }
}
