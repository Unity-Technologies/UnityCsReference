// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    // Foldout for the CSS gap shorthand: a header field that edits both gaps at once,
    // expandable into row-gap / column-gap sub-fields. Mirrors MarginFoldoutField /
    // PaddingFoldoutField (see LengthFoldoutField) but with two sub-fields instead of
    // four edges. The shorthand order matches CSS: `gap: <row-gap> <column-gap>`.
    [UxmlElement]
    internal partial class GapFoldoutField : StyleFoldoutField<TextField>
    {
        const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/Controls/GapFoldoutField.uxml";
        static readonly string TextUssClassName = FoldoutFieldPropertyName + "__textfield";

        const string k_RowPropertyName = "rowGap";
        const string k_ColumnPropertyName = "columnGap";

        public StyleLengthField rowField { get; private set; }
        public StyleLengthField columnField { get; private set; }

        // Ordered to match the `gap` shorthand: row-gap first, column-gap second.
        List<StyleLengthField> fields => new() { rowField, columnField };

        IntegerField m_DraggerField;
        public IntegerField draggerIntegerField => m_DraggerField;

        public GapFoldoutField() : base("Gap")
        {
            var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
            vta.CloneTree(contentContainer);

            rowField = this.Q<StyleLengthField>("row");
            columnField = this.Q<StyleLengthField>("column");

            SetupCallbacks();
        }

        protected override TextField CreateHeaderInputElement()
        {
            return new TextField();
        }

        void SetupCallbacks()
        {
            foreach (var field in fields)
            {
                field.RegisterCallback<PropertyChangedEvent>(e =>
                {
                    if (e.property == BaseField<StyleLength>.valueProperty ||
                        e.property == enabledSelfProperty)
                        UpdateFromChildFields();
                });
            }

            // Used for its dragger, mirroring LengthFoldoutField.
            m_DraggerField = new IntegerField(" ");
            m_DraggerField.name = "dragger-integer-field";
            m_DraggerField.visualInput.focusable = false;
            m_DraggerField.tabIndex = -1;
            m_DraggerField.AddToClassList(DraggerFieldUssClassName);
            m_DraggerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            m_Toggle.labelElement.Add(m_DraggerField);

            headerInputField.isDelayed = true; // only updates on Enter or lost focus
            headerInputField.AddToClassList(TextUssClassName);
            headerInputField.AddToClassList(TextField.alignedFieldUssClassName);
            headerInputField.RegisterValueChangedCallback(OnHeaderValueChange);

            UpdateFromChildFields();
        }

        public override void UpdateFromChildFields()
        {
            var allTheSame = true;
            var singleValue = "none";
            var cumulativeValue = string.Empty;
            var shouldBeEnabled = false;

            for (var i = 0; i < fields.Count; ++i)
            {
                shouldBeEnabled |= fields[i].enabledSelf;
                var childValue = fields[i].value.ToString().ToLower();
                if (childValue.Equals("0"))
                    childValue = "0px";

                if (i == 0)
                    singleValue = childValue;
                else if (singleValue != childValue)
                    allTheSame = false;

                if (i != 0)
                    cumulativeValue += FieldStringSeparator;

                cumulativeValue += childValue;
            }

            headerInputField.SetValueWithoutNotify(allTheSame ? singleValue : cumulativeValue);
            if (fields.Count > 0)
                draggerIntegerField.SetValueWithoutNotify((int)fields[0].value.value.value);
            enabledSelf = shouldBeEnabled;
        }

        void OnHeaderValueChange(ChangeEvent<string> evt)
        {
            var inputArray = evt.newValue.Split(' ');

            if (inputArray.Length == 1 && fields.Count > 0)
            {
                var newCommonValue = evt.newValue;
                for (var i = 0; i < fields.Count; ++i)
                {
                    fields[i].value = Length.ParseString(newCommonValue);

                    if (i == 0 && !newCommonValue.StartsWith(UssVariablePrefix))
                        newCommonValue = fields[i].value.ToString();
                }
            }
            else
            {
                // gap: <row-gap> <column-gap>
                for (var i = 0; i < Mathf.Min(inputArray.Length, fields.Count); ++i)
                    fields[i].value = Length.ParseString(inputArray[i]);
            }

            UpdateFromChildFields();
            evt.StopPropagation();
        }

        void OnDraggerFieldUpdate(ChangeEvent<int> evt)
        {
            headerInputField.value = evt.newValue.ToString();
        }

        protected override void ForwardDependentPropertiesTracking(TrackPropertyEvent evt)
        {
            base.ForwardDependentPropertiesTracking(evt);

            var target = default(VisualElement);
            if (evt.propertyName == k_RowPropertyName)
                target = rowField;
            else if (evt.propertyName == k_ColumnPropertyName)
                target = columnField;

            if (target == null)
                return;

            var subEvent = TrackPropertyEvent.GetPooled(evt.provider, evt.propertyName);
            subEvent.target = target;
            target.SendEvent(subEvent);
            evt.StopImmediatePropagation();
        }
    }
}
