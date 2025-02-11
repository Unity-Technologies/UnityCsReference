// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Represents a single item in the list view used to represent local variables.
    /// </summary>
    class BuilderInspectorVariablesListItem : VisualElement
    {
        internal static readonly string hiddenFieldClassName = "variable-field-hidden";
        internal static readonly string visibleFieldClassName = "variable-field-visible";

        internal static readonly string fieldGroupName = "field-group";
        internal static readonly string nameFieldName = "item-name-field";
        internal static readonly string valueFieldName = "item-value-field";
        internal static readonly string floatFieldName = "item-float-field";
        internal static readonly string dimensionFieldName = "item-dimension-field";
        internal static readonly string colorFieldName = "item-color-field";
        internal static readonly string assetFieldName = "item-asset-field";
        internal static readonly string keywordFieldName = "item-keyword-field";
        internal static readonly string typeFieldName = "item-type-field";

        internal static readonly string prefixClassName = "variable-prefix";
        internal static readonly string compositeFieldClassName = "unity-builder-composite-field";

        internal static readonly string listViewItemClassName = "list-view-item";

        internal string[] m_TypeChoices;

        internal Label itemLabel;
        internal VisualElement fieldsGroup;
        internal TextField itemNameField, itemValueField;
        internal FloatField itemFloatField;
        internal USSVariablesStyleField itemDimensionField;
        internal ColorField itemColorField;
        internal AssetReferenceStyleField itemAssetField;
        internal DropdownField itemKeywordField;
        internal DropdownField itemTypeField;

        /// <summary>
        /// Constructs a converter group view item
        /// </summary>
        public BuilderInspectorVariablesListItem()
        {
            AddToClassList(listViewItemClassName);
            fieldsGroup = new VisualElement() { classList = { fieldGroupName } };
            fieldsGroup.Add(itemLabel = new Label(BuilderConstants.VariablePrefix)
                { name = prefixClassName, classList = { prefixClassName } });
            fieldsGroup.Add(itemNameField = new TextField()
                { name = nameFieldName, classList = { visibleFieldClassName }, isDelayed = true });
            fieldsGroup.Add(itemValueField = new TextField()
                { name = valueFieldName, classList = { hiddenFieldClassName }, isDelayed = true });
            fieldsGroup.Add(itemFloatField = new FloatField()
                { name = floatFieldName, classList = { hiddenFieldClassName }, isDelayed = true });
            fieldsGroup.Add(itemDimensionField = new USSVariablesStyleField()
                { name = dimensionFieldName, classList = { dimensionFieldName, compositeFieldClassName } });
            fieldsGroup.Add(itemColorField = new ColorField()
                { name = colorFieldName, classList = { hiddenFieldClassName } });
            fieldsGroup.Add(itemAssetField = new AssetReferenceStyleField()
                { name = assetFieldName, classList = { hiddenFieldClassName } });
            fieldsGroup.Add(itemKeywordField = new DropdownField()
                { name = keywordFieldName, classList = { hiddenFieldClassName } });
            fieldsGroup.Add(itemTypeField = new DropdownField()
                { name = typeFieldName, classList = { visibleFieldClassName } });
            Add(fieldsGroup);

            foreach (var choice in BuilderInspectorVariables.keywordArray)
            {
                itemKeywordField.choices.Add(choice.ToString());
            }

            foreach (var choice in BuilderInspectorVariables.typesArray)
            {
                itemTypeField.choices.Add(choice.ToString());
            }
        }
    }
}
