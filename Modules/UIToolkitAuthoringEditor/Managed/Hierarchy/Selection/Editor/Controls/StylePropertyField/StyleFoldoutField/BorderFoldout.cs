// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    internal abstract partial class BorderFoldout : StyleFoldoutField<TextField>
    {
        public static readonly string textUssClassName = FoldoutFieldPropertyName + "__textfield";

        readonly List<StyleLengthField> m_Fields = new();
        public List<StyleLengthField> fields => m_Fields;

        IntegerField m_DraggerField;
        public IntegerField draggerIntegerField => m_DraggerField;

        protected abstract IReadOnlyList<string> propertyNames { get; }
        protected abstract IReadOnlyList<string> fieldLabels { get; }
        protected abstract IReadOnlyList<string> fieldTooltips { get; }

        protected BorderFoldout(string text)
            : base(text)
        {
            for (var i = 0; i < propertyNames.Count; ++i)
            {
                var propertyName = propertyNames[i];
                var row = new OverrideRow() { name = propertyName };
                var field = new StyleLengthField(fieldLabels[i]) { name = propertyName }.WithClassList(TextField.alignedFieldUssClassName);
                field.SetBinding("value", new StylePropertyBinding(propertyName));
                field.tooltip = fieldTooltips[i];

                var validation = GetValidation(i);
                if (validation != null)
                    field.AddValidation(validation);

                field.RegisterCallback<PropertyChangedEvent>(e =>
                {
                    if (e.property == BaseField<StyleLength>.valueProperty ||
                        e.property == enabledSelfProperty)
                        UpdateFromChildFields();
                });

                row.Add(field);
                Add(row);
                m_Fields.Add(field);
            }

            // Used for its dragger.
            var toggleInput = this.Q(className: "unity-toggle__input");
            m_DraggerField = new IntegerField(" ");
            m_DraggerField.name = "dragger-integer-field";
            m_DraggerField.visualInput.focusable = false;
            m_DraggerField.tabIndex = -1;
            // The dragger overlays the whole header; let clicks fall through to the header text field while the label keeps its drag zone.
            m_DraggerField.pickingMode = PickingMode.Ignore;
            m_DraggerField.AddToClassList(DraggerFieldUssClassName);
            m_DraggerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            toggleInput.Add(m_DraggerField);

            headerInputField.AddToClassList(textUssClassName);
            headerInputField.AddToClassList(TextField.alignedFieldUssClassName);
            headerInputField.isDelayed = true;
            headerInputField.RegisterValueChangedCallback(OnHeaderValueChange);

            UpdateFromChildFields();
        }

        protected override TextField CreateHeaderInputElement()
        {
            return new TextField();
        }

        protected virtual StylePropertyValidation GetValidation(int index) => null;

        public override void UpdateFromChildFields()
        {
            var allTheSame = true;
            var singleValue = "none";
            var cumulativeValue = string.Empty;
            var shouldBeEnabled = false;

            for (var i = 0; i < m_Fields.Count; ++i)
            {
                shouldBeEnabled |= m_Fields[i].enabledSelf;
                var childValue = m_Fields[i].value.ToString().ToLower();
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
            if (m_Fields.Count > 0)
                draggerIntegerField.SetValueWithoutNotify((int)m_Fields[0].value.value.value);
            enabledSelf = shouldBeEnabled;
        }

        void OnHeaderValueChange(ChangeEvent<string> evt)
        {
            var newValue = evt.newValue;
            var inputArray = newValue.Split(' ');

            if (inputArray.Length == 1 && m_Fields.Count > 0)
            {
                var newCommonValue = newValue;

                for (var i = 0; i < m_Fields.Count; ++i)
                {
                    m_Fields[i].value = Length.ParseString(newCommonValue);

                    if (i == 0 && !newCommonValue.StartsWith(UssVariablePrefix))
                        newCommonValue = m_Fields[i].value.ToString();
                }
            }
            else if (inputArray.Length == 2)
            {
                m_Fields[0].value = m_Fields[2].value = Length.ParseString(inputArray[0]);
                m_Fields[1].value = m_Fields[3].value = Length.ParseString(inputArray[1]);
            }
            else if (inputArray.Length == 3)
            {
                m_Fields[0].value = Length.ParseString(inputArray[0]);
                m_Fields[1].value = m_Fields[3].value = Length.ParseString(inputArray[1]);
                m_Fields[2].value = Length.ParseString(inputArray[2]);
            }
            else
            {
                for (var i = 0; i < Mathf.Min(inputArray.Length, m_Fields.Count); ++i)
                    m_Fields[i].value = Length.ParseString(inputArray[i]);
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

            VisualElement target = null;
            for (var i = 0; i < m_Fields.Count; ++i)
            {
                if (evt.propertyName == propertyNames[i])
                {
                    target = m_Fields[i];
                    break;
                }
            }

            if (target == null)
                return;

            var subEvent = TrackPropertyEvent.GetPooled(evt.provider, evt.propertyName);
            subEvent.target = target;
            target.SendEvent(subEvent);
            evt.StopImmediatePropagation();
        }
    }
}
