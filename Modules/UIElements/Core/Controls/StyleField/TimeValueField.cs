// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a text field for entering TimeValue.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal class TimeValueField : TextValueField<TimeValue>
    {
        public static readonly BindingId showUnitAsDropdownProperty = nameof(showUnitAsDropdown);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : TextValueField<TimeValue>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] bool showUnitAsDropdown;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags showUnitAsDropdown_UxmlAttributeFlags;
            #pragma warning restore 649

            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                TextValueField<TimeValue>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(showUnitAsDropdown), "show-unit-as-dropdown"),
                }, false);
            }

            public override object CreateInstance() => new TimeValueField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (TimeValueField)obj;
                if (ShouldWriteAttributeValue(showUnitAsDropdown_UxmlAttributeFlags))
                    e.showUnitAsDropdown = showUnitAsDropdown;
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-style-field";
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string timeValueFieldUssClassName = "unity-time-value-field";
        /// <summary>
        /// USS class name of the visual input in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__visual-input";
        /// <summary>
        /// USS class name of options popup container in elements of this type.
        /// </summary>
        public static readonly string unitDropdownContainerUssClass = ussClassName + "__options-popup-container";
        /// <summary>
        /// USS class name of options popup in elements of this type.
        /// </summary>
        public static readonly string unitDropdownUssClass = ussClassName + "__options-popup";
        /// <summary>
        /// USS class name of the options dropdown element of this type when invisible.
        /// </summary>
        public static readonly string invisibleUnitDropdownUssClass = unitDropdownUssClass + "--invisible";

        // Keywords
        public static readonly string KeywordInitial = "initial";

        // Units
        public const string UnitSecond = "s";
        public const string UnitMillisecond = "ms";

        static readonly string[] KLDefaultUnits = new[] { UnitSecond, UnitMillisecond} ;
        static readonly string[] AllKeywords = new[] { KeywordInitial };

        internal static readonly string s_NoOptionString = "-";

        TimeValueInput timeValueInput => (TimeValueInput)textInputBase;

        bool m_ShowUnitAsDropdown;

        readonly PopupField<string> m_OptionsPopup;
        readonly List<string> m_AllOptionsList = new();

        [UxmlAttribute]
        [CreateProperty]
        public bool showUnitAsDropdown
        {
            get => m_ShowUnitAsDropdown;
            set
            {
                if (m_ShowUnitAsDropdown == value)
                    return;

                m_ShowUnitAsDropdown = value;
                UpdateFields();
                NotifyPropertyChanged(showUnitAsDropdownProperty);
            }
        }

        protected internal PopupField<string> optionsPopup => m_OptionsPopup;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TimeValueField() : this(null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxTimeValue">Maximum number of characters the field can take.</param>
        public TimeValueField(int maxTimeValue)
            : this(null, maxTimeValue) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The Label text.</param>
        /// <param name="maxTimeValue">Maximum number of characters the field can take.</param>
        public TimeValueField(string label, int maxTimeValue = kMaxValueFieldLength)
            : base(label, maxTimeValue, new TimeValueInput())
        {
            AddToClassList(ussClassName);
            AddToClassList(timeValueFieldUssClassName);

            AddLabelDragger<TimeValue>();

            var popupContainer = new VisualElement();
            popupContainer.name = unitDropdownContainerUssClass;
            popupContainer.AddToClassList(unitDropdownContainerUssClass);

            m_AllOptionsList.AddRange(KLDefaultUnits);
            m_AllOptionsList.AddRange(AllKeywords);
            m_OptionsPopup = new PopupField<string>(m_AllOptionsList, 0, OnFormatSelectedValue);
            m_OptionsPopup.AddToClassList(unitDropdownUssClass);
            popupContainer.Add(m_OptionsPopup);

            timeValueInput.parentField = this;

            timeValueInput.AddToClassList(inputUssClassName);
            timeValueInput.delegatesFocus = true;
            Add(popupContainer);

            m_OptionsPopup.RegisterValueChangedCallback(OnPopupFieldValueChange);
            UpdateFields();
            showUnitAsDropdown = true;
        }

        public override void SetValueWithoutNotify(TimeValue newValue)
        {
            base.SetValueWithoutNotify(newValue);

            SetOptionsPopupFromValue();
        }

        /// <summary>
        /// Applies the values of a 3D delta and a speed from an input device.
        /// </summary>
        /// <param name="delta">A vector used to compute the value change.</param>
        /// <param name="speed">A multiplier for the value change.</param>
        /// <param name="startValue">The start value.</param>
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, TimeValue startValue)
        {
            timeValueInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        /// <summary>
        /// Converts the given double to a string.
        /// </summary>
        /// <param name="v">The TimeValue to be converted to string.</param>
        /// <returns>The TimeValue as string.</returns>
        protected override string ValueToString(TimeValue v)
        {
            return showUnitAsDropdown ? v.value.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat) : v.ToString();
        }

        /// <summary>
        /// Converts a string to a TimeValue.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The TimeValue parsed from the string.</returns>
        protected override TimeValue StringToValue(string str)
        {
            var trimmedInput = str.AsSpan().Trim();

            var unit = value.unit;
            if (unit != TimeUnit.Second && unit != TimeUnit.Millisecond)
                unit = TimeUnit.Second;

            if (trimmedInput.EndsWith(UnitMillisecond, StringComparison.Ordinal))
            {
                trimmedInput = trimmedInput[..^UnitMillisecond.Length];
                unit = TimeUnit.Millisecond;
            }
            else if (trimmedInput.EndsWith(UnitSecond, StringComparison.Ordinal))
            {
                trimmedInput = trimmedInput[..^UnitSecond.Length];
                unit = TimeUnit.Second;
            }

            return UINumericFieldsUtils.TryConvertStringToFloat(trimmedInput.ToString(), textInputBase.originalText, out var result, out _) ? new TimeValue(result, unit) : value;
        }

        internal override bool CanTryParse(string textString) => double.TryParse(textString, out _);

        void UpdateFields()
        {
            text = ValueToString(value);
            m_OptionsPopup.EnableInClassList(invisibleUnitDropdownUssClass, !showUnitAsDropdown);
        }

        static string OnFormatSelectedValue(string value)
        {
            return Array.IndexOf(AllKeywords, value) < 0 ? value : s_NoOptionString;
        }

        void SetOptionsPopupFromValue()
        {
            var unitStr = value.unit.ToDisplayString();
            var isValid = string.Compare(unitStr, s_NoOptionString, StringComparison.OrdinalIgnoreCase) != 0;
            if (isValid)
                m_OptionsPopup.SetValueWithoutNotify(unitStr);
        }

        void OnPopupFieldValueChange(ChangeEvent<string> evt)
        {
            // There's a bug in UIE that makes the PopupField send a ChangeEvent<string> even
            // if you called SetValueWithoutNotify(). It's the PopupTextElement.text that
            // sends it. Hence, this check.
            if (evt.target != optionsPopup)
            {
                evt.StopImmediatePropagation();
                return;
            }

            value = evt.newValue switch
            {
                UnitSecond => new TimeValue(value.value, TimeUnit.Second),
                UnitMillisecond => new TimeValue(value.value, TimeUnit.Millisecond),
                _ => value
            };

            evt.StopImmediatePropagation();
        }

        class TimeValueInput : TextValueInput
        {
            internal TimeValueField parentField { get; set; }

            internal TimeValueInput()
            {
                formatString = UINumericFieldsUtils.k_DoubleFieldFormatString;
            }

            protected override string allowedCharacters => UINumericFieldsUtils.k_AllowedCharactersForFloat;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, TimeValue startValue)
            {
                double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity(startValue.value);
                float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                var v = StringToValue(text).value;
                v += (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);
                if (parentField.isDelayed)
                {
                    text = ValueToString(new TimeValue(Mathf.ClampToInt((long)v), startValue.unit));
                }
                else
                {
                    parentField.value = new TimeValue(Mathf.ClampToInt((long)v), startValue.unit);
                }
            }

            protected override string ValueToString(TimeValue v)
            {
                return parentField.showUnitAsDropdown
                    ? v.value.ToString(CultureInfo.InvariantCulture)
                    : v.ToString();
            }

            protected override TimeValue StringToValue(string str)
            {
                return TimeValue.TryParseString(str, out var v) ? v : parentField.value;
            }
        }
    }
}
