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

        readonly OverrideBarManipulator m_OverrideBar;

        /// <summary>
        /// Creates a variable row. <paramref name="overrideBarTarget"/> is the element that paints the row's
        /// override bar, and is normally the list view holding the rows: the bar is drawn in the inspector's
        /// left gutter, which a row cannot reach because the list clips whatever its rows paint out there.
        /// This is how <see cref="FoldoutLonghandField{TData}"/> draws the bars of the transition rows.
        /// </summary>
        public VariablesListItem(VisualElement overrideBarTarget = null)
        {
            AddToClassList(k_ListViewItemClassName);

            if (overrideBarTarget != null)
            {
                m_OverrideBar = new OverrideBarManipulator { target = overrideBarTarget, OverrideContainer = this };
                RegisterCallback<DetachFromPanelEvent>(_ => showOverrideBar = false);
            }

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
                { name = k_ColorFieldName, setAlphaIfTransparentWhenPicked = true }.WithClassList(k_HiddenFieldClassName));
            fieldsGroup.Add(itemLengthField = new LengthField()
                { name = k_LengthFieldName }.WithClassList(k_HiddenFieldClassName));
            fieldsGroup.Add(itemAngleField = new AngleField()
                { name = k_AngleFieldName }.WithClassList(k_HiddenFieldClassName));
            fieldsGroup.Add(itemTimeValueField = new TimeValueField()
                { name = k_TimeValueFieldName }.WithClassList(k_HiddenFieldClassName));
            fieldsGroup.Add(itemKeywordField = new DropdownField()
                { name = k_KeywordFieldName }.WithClassList(k_HiddenFieldClassName));
            fieldsGroup.Add(itemAssetField = CreateAssetField());

            fieldsGroup.Add(itemTypeField = new DropdownField()
                { name = k_TypeFieldName }.WithClassList(k_VisibleFieldClassName));

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
            return new ObjectField() { name = k_AssetFieldName, allowBuiltinResources = false }.WithClassList(k_HiddenFieldClassName);
        }

        internal VisualElement overrideBarTarget => m_OverrideBar?.target;

        // Only a bound row stands for a variable, so a recycled one must not leave its bar behind.
        internal bool showOverrideBar
        {
            get => m_OverrideBar is { IsOverridden: true };
            set
            {
                if (m_OverrideBar != null)
                    m_OverrideBar.IsOverridden = value;
            }
        }

        internal void DetachOverrideBar()
        {
            if (m_OverrideBar == null)
                return;

            showOverrideBar = false;
            m_OverrideBar.target = null;
        }
    }
}
