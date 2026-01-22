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
    /// Makes a text field for entering Angle.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal class AngleField : TextValueField<Angle>
    {
        public static readonly BindingId showUnitAsDropdownProperty = nameof(showUnitAsDropdown);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : TextValueField<Angle>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] bool showUnitAsDropdown;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags showUnitAsDropdown_UxmlAttributeFlags;
            #pragma warning restore 649

            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                TextValueField<Angle>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(showUnitAsDropdown), "show-unit-as-dropdown"),
                }, false);
            }

            public override object CreateInstance() => new AngleField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (AngleField)obj;
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
        public static readonly string angleFieldUssClassName = "unity-angle-field";
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
        public static readonly string KeywordNone = "none";

        // Units
        public const string UnitDegree = "deg";
        public const string UnitGrad = "grad";
        public const string UnitRad = "rad";
        public const string UnitTurn = "turn";

        static readonly string[] KLDefaultUnits = new[] { UnitDegree, UnitGrad, UnitRad, UnitTurn };
        static readonly string[] AllKeywords = new[] { KeywordNone, KeywordInitial };

        internal static readonly string s_NoOptionString = "-";

        AngleInput angleInput => (AngleInput)textInputBase;

        bool m_ShowUnitAsDropdown;

        readonly PopupField<string> m_OptionsPopup;
        readonly List<string> m_AllOptionsList = new();

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
        public AngleField() : this(null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxAngle">Maximum number of characters the field can take.</param>
        public AngleField(int maxAngle)
            : this(null, maxAngle) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The Label text.</param>
        /// <param name="maxAngle">Maximum number of characters the field can take.</param>
        [SuppressMessage("CodeReloadSafety", "UAL0015:Auto cleaned up symbol assigned by constructor", Justification = "Visual Elements are re-created on Code Reload")]
        public AngleField(string label, int maxAngle = kMaxValueFieldLength)
            : base(label, maxAngle, new AngleInput())
        {
            AddToClassList(ussClassName);
            AddToClassList(angleFieldUssClassName);

            AddLabelDragger<Angle>();

            var popupContainer = new VisualElement();
            popupContainer.name = unitDropdownContainerUssClass;
            popupContainer.AddToClassList(unitDropdownContainerUssClass);

            m_AllOptionsList.AddRange(KLDefaultUnits);
            m_AllOptionsList.AddRange(AllKeywords);
            m_OptionsPopup = new PopupField<string>(m_AllOptionsList, 0, OnFormatSelectedValue);
            m_OptionsPopup.AddToClassList(unitDropdownUssClass);
            popupContainer.Add(m_OptionsPopup);

            angleInput.parentField = this;

            angleInput.AddToClassList(inputUssClassName);
            angleInput.delegatesFocus = true;
            Add(popupContainer);

            m_OptionsPopup.RegisterValueChangedCallback(OnPopupFieldValueChange);
            UpdateFields();
            showUnitAsDropdown = true;
        }

        public override void SetValueWithoutNotify(Angle newValue)
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
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, Angle startValue)
        {
            angleInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        /// <summary>
        /// Converts the given double to a string.
        /// </summary>
        /// <param name="v">The Angle to be converted to string.</param>
        /// <returns>The Angle as string.</returns>
        protected override string ValueToString(Angle v)
        {
            if (showUnitAsDropdown && !v.IsNone())
                return v.value.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

            return v.ToString();
        }

        /// <summary>
        /// Converts a string to a Angle.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The Angle parsed from the string.</returns>
        protected override Angle StringToValue(string str)
        {
            var trimmedInput = str.AsSpan().Trim();

            if (trimmedInput.Equals(KeywordNone, StringComparison.OrdinalIgnoreCase))
                return Angle.None();

            var unit = value.unit;
            if (unit != AngleUnit.Degree && unit != AngleUnit.Gradian && unit != AngleUnit.Radian && unit != AngleUnit.Turn)
                unit = AngleUnit.Degree;

            if (trimmedInput.EndsWith(UnitDegree, StringComparison.Ordinal))
            {
                trimmedInput = trimmedInput[..^UnitDegree.Length];
                unit = AngleUnit.Degree;
            }
            else if (trimmedInput.EndsWith(UnitGrad, StringComparison.Ordinal))
            {
                trimmedInput = trimmedInput[..^UnitGrad.Length];
                unit = AngleUnit.Gradian;
            }
            else if (trimmedInput.EndsWith(UnitRad, StringComparison.Ordinal))
            {
                trimmedInput = trimmedInput[..^UnitRad.Length];
                unit = AngleUnit.Radian;
            }
            else if (trimmedInput.EndsWith(UnitTurn, StringComparison.Ordinal))
            {
                trimmedInput = trimmedInput[..^UnitTurn.Length];
                unit = AngleUnit.Turn;
            }

            return UINumericFieldsUtils.TryConvertStringToFloat(trimmedInput.ToString(), textInputBase.originalText, out var result, out _) ? new Angle(result, unit) : value;
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
            if (value.IsNone())
                m_OptionsPopup.SetValueWithoutNotify(KeywordNone);

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
                UnitDegree => new Angle(value.value, AngleUnit.Degree),
                UnitGrad => new Angle(value.value, AngleUnit.Gradian),
                UnitRad => new Angle(value.value, AngleUnit.Radian),
                UnitTurn => new Angle(value.value, AngleUnit.Turn),
                _ => value
            };

            evt.StopImmediatePropagation();
        }

        class AngleInput : TextValueInput
        {
            internal AngleField parentField { get; set; }

            internal AngleInput()
            {
                formatString = UINumericFieldsUtils.k_DoubleFieldFormatString;
            }

            protected override string allowedCharacters => UINumericFieldsUtils.k_AllowedCharactersForFloat;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, Angle startValue)
            {
                double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity(startValue.value);
                float acceleration =
                    NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                var v = StringToValue(text).value;
                v += (long)Math.Round(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity);
                if (parentField.isDelayed)
                {
                    text = ValueToString(Mathf.ClampToInt((long)v));
                }
                else
                {
                    parentField.value = Mathf.ClampToInt((long)v);
                }
            }

            protected override string ValueToString(Angle v)
            {
                return parentField.showUnitAsDropdown
                    ? v.value.ToString(CultureInfo.InvariantCulture)
                    : v.ToString();
            }

            protected override Angle StringToValue(string str)
            {
                return Angle.TryParseString(str, out var v) ? v : parentField.value;
            }
        }
    }
}
