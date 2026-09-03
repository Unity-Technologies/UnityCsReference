// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Globalization;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    internal partial class ZIndexStyleIntField : StylePropertyField<StyleInt, ZIndexStyleIntField.AutoIntegerField, int>
    {
        public new static readonly string ussClassName = "unity-integer-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public ZIndexStyleIntField() : this(null) {}

        public ZIndexStyleIntField(string label) : base(label, new AutoIntegerField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override AutoIntegerField CreateValueField() => new AutoIntegerField();

        protected override StyleInt CreateStyleValue(int v) =>
            v == int.MinValue ? new StyleInt(StyleKeyword.Auto) : new StyleInt(v);

        public override void SetValueWithoutNotify(StyleInt newValue)
        {
            base.SetValueWithoutNotify(newValue);
            if (newValue.keyword == StyleKeyword.Auto || newValue.keyword == StyleKeyword.Null)
                valueField.SetValueWithoutNotify(int.MinValue);
        }

        internal override bool EqualsCurrentValue(StyleInt v) => value == v;

        public sealed class AutoIntegerField : IntegerField
        {
            const string k_Auto = "auto";
            const string k_NoOption = "-";

            static readonly string s_OptionsPopupContainerUssClass = "unity-style-field__options-popup-container";
            static readonly string s_OptionsPopupUssClass = "unity-style-field__options-popup";

            [NoAutoStaticsCleanup] // fixed option labels, never mutated
            static readonly List<string> k_Options = new() { k_NoOption, k_Auto };

            readonly PopupField<string> m_OptionsPopup;

            public AutoIntegerField()
            {
                var popupContainer = new VisualElement();
                popupContainer.AddToClassList(s_OptionsPopupContainerUssClass);

                m_OptionsPopup = new PopupField<string>(k_Options, 0, OnFormatSelectedValue);
                m_OptionsPopup.AddToClassList(s_OptionsPopupUssClass);
                popupContainer.Add(m_OptionsPopup);
                hierarchy.Add(popupContainer);

                m_OptionsPopup.RegisterValueChangedCallback(OnPopupFieldValueChange);
            }

            protected override string ValueToString(int v) =>
                v == int.MinValue ? k_Auto : v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

            protected override int StringToValue(string str) =>
                str.Trim() == k_Auto ? int.MinValue : base.StringToValue(str);

            public override void SetValueWithoutNotify(int newValue)
            {
                base.SetValueWithoutNotify(newValue);
                m_OptionsPopup.SetValueWithoutNotify(newValue == int.MinValue ? k_Auto : k_NoOption);
            }

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, int startValue)
            {
                if (startValue == int.MinValue)
                {
                    startValue = 0;
                    if (value == int.MinValue)
                        SetValueWithoutNotify(0);
                }
                base.ApplyInputDeviceDelta(delta, speed, startValue);
            }

            static string OnFormatSelectedValue(string _) => k_NoOption;

            void OnPopupFieldValueChange(ChangeEvent<string> evt)
            {
                if (evt.target != m_OptionsPopup)
                {
                    evt.StopImmediatePropagation();
                    return;
                }

                if (evt.newValue == k_Auto)
                    value = int.MinValue;
                else if (value == int.MinValue)
                    value = 0;

                evt.StopImmediatePropagation();
            }
        }
    }
}
