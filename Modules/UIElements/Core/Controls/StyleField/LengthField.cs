// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.StyleSheets.Syntax;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a text field for entering Length.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class LengthField : TextValueField<Length>
    {
        public static readonly BindingId showUnitAsDropdownProperty = nameof(showUnitAsDropdown);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : TextValueField<Length>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] bool showUnitAsDropdown;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags showUnitAsDropdown_UxmlAttributeFlags;
            #pragma warning restore 649

            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                TextValueField<Length>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(showUnitAsDropdown), "show-unit-as-dropdown"),
                }, false);
            }

            public override object CreateInstance() => new LengthField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (LengthField)obj;
                if (ShouldWriteAttributeValue(showUnitAsDropdown_UxmlAttributeFlags))
                    e.showUnitAsDropdown = showUnitAsDropdown;
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-style-field";
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
        public static readonly string KeywordAuto = "auto";
        public static readonly string KeywordNone = "none";

        // Units
        public static readonly string UnitPixel = "px";
        public static readonly string UnitPercent = "%";

        static readonly string[] KLInitial = new[] { KeywordInitial };
        static readonly string[] KLDefaultUnits = new[] { UnitPixel, UnitPercent };
        static readonly string[] KLAuto = new[] { KeywordAuto, KeywordInitial };
        static readonly string[] KLNone = new[] { KeywordNone, KeywordInitial };
        static readonly string[] AllKeywords  = new[] { KeywordAuto, KeywordNone, KeywordInitial };

        internal static readonly string s_NoOptionString = "-";

        LengthInput lengthInput => (LengthInput)textInputBase;

        bool m_ShowUnitAsDropdown;

        readonly List<LengthUnit> m_Units = new() { LengthUnit.Pixel };

        readonly PopupField<string> m_OptionsPopup;
        readonly List<string> m_StyleKeywords = new();
        readonly List<string> m_CachedRegularOptionsList = new();
        readonly List<string> m_AllOptionsList = new();
        private readonly StyleMatcher m_StyleMatcher = new();
        Expression m_SyntaxTree;

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

        protected List<string> styleKeywords => m_StyleKeywords;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LengthField() : this(null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public LengthField(int maxLength)
            : this(null, maxLength) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The Label text.</param>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public LengthField(string label, int maxLength = kMaxValueFieldLength)
            : base(label, maxLength, new LengthInput())
        {
            AddToClassList(ussClassName);

            AddLabelDragger<Length>();

            var popupContainer = new VisualElement();
            popupContainer.name = unitDropdownContainerUssClass;
            popupContainer.AddToClassList(unitDropdownContainerUssClass);
            m_StyleKeywords.AddRange(KLAuto);
            PopulateAdditionalOptions(m_CachedRegularOptionsList);

            m_AllOptionsList.AddRange(m_CachedRegularOptionsList);
            m_AllOptionsList.AddRange(m_StyleKeywords);
            m_OptionsPopup = new PopupField<string>(m_AllOptionsList, 0, OnFormatSelectedValue);
            m_OptionsPopup.AddToClassList(unitDropdownUssClass);
            popupContainer.Add(m_OptionsPopup);

            lengthInput.parentLengthField = this;

            lengthInput.AddToClassList(inputUssClassName);
            lengthInput.delegatesFocus = true;
            Add(popupContainer);

            m_OptionsPopup.RegisterValueChangedCallback(OnPopupFieldValueChange);
            UpdateFields();
            showUnitAsDropdown = true;
        }

        /// <summary>
        /// Gets the keywords that are valid for the current length field.
        /// </summary>
        public void PopulateStyleKeywords(List<string> keywordList)
        {
            if (m_SyntaxTree == null)
            {
                keywordList.AddRange(KLAuto);
                return;
            }

            var hasAuto = FindKeywordInExpression(m_SyntaxTree, KeywordAuto);
            var hasNone = FindKeywordInExpression(m_SyntaxTree, KeywordNone);

            if (hasAuto)
            {
                keywordList.AddRange(KLAuto);
                return;
            }

            if (hasNone)
            {
                keywordList.AddRange(KLNone);
                return;
            }

            keywordList.AddRange(KLInitial);
        }

        public override void SetValueWithoutNotify(Length newValue)
        {
            if (!IsValid(newValue))
                return;

            base.SetValueWithoutNotify(newValue);

            SetOptionsPopupFromValue();
        }

        /// <summary>
        /// <para>Set the validation syntax tree for the field.</para>
        /// <para>When the syntax tree is set, the field will validate the input against the syntax tree.</para>
        /// <para>When the input is invalid, the field will revert to the previous valid value.</para>
        /// <para>When the input is invalid and there is no previous valid value, the field will revert to the default value.</para>
        /// </summary>
        /// <param name="validation">The syntax to validate the input against.</param>
        public void SetValidation(StylePropertyValidation validation)
        {
            if (validation is not Syntax syntaxValidation)
                return;

            m_SyntaxTree = Syntax.GetSyntaxTree(syntaxValidation);
            UpdateOptionsMenu();
        }

        /// <summary>
        /// <para>Set the validation syntax tree for the field.</para>
        /// <para>When the syntax tree is set, the field will validate the input against the syntax tree.</para>
        /// <para>When the input is invalid, the field will revert to the previous valid value.</para>
        /// <para>When the input is invalid and there is no previous valid value, the field will revert to the default value.</para>
        /// </summary>
        /// <param name="validation">The list of validation to validate the input against.</param>
        public void SetValidation(in StylePropertyValidationCollection validation)
        {
            using var syntaxHandle = ListPool<Syntax>.Get(out var syntaxes);
            foreach (var propertyValidation in validation)
            {
                if (propertyValidation is Syntax syntax)
                    syntaxes.Add(syntax);
            }
            m_SyntaxTree = Syntax.GetSyntaxTree(syntaxes);
            UpdateOptionsMenu();
        }

        /// <summary>
        /// Removes previous validation applied to the field.
        /// </summary>
        public void ClearValidation()
        {
            m_SyntaxTree = null;
            UpdateOptionsMenu();
        }

        /// <summary>
        /// Applies the values of a 3D delta and a speed from an input device.
        /// </summary>
        /// <param name="delta">A vector used to compute the value change.</param>
        /// <param name="speed">A multiplier for the value change.</param>
        /// <param name="startValue">The start value.</param>
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, Length startValue)
        {
            lengthInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        /// <summary>
        /// Converts the given double to a string.
        /// </summary>
        /// <param name="v">The Length to be converted to string.</param>
        /// <returns>The Length as string.</returns>
        protected override string ValueToString(Length v)
        {
            if (showUnitAsDropdown && !v.IsAuto() && !v.IsNone())
                return v.value.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

            return v.ToString();
        }

        /// <summary>
        /// Converts a string to a Length.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The Length parsed from the string.</returns>
        protected override Length StringToValue(string str)
        {
            var trimmedInput = str.AsSpan().Trim();

            if (trimmedInput.Equals(KeywordAuto, StringComparison.OrdinalIgnoreCase))
                return Length.Auto();

            if (trimmedInput.Equals(KeywordNone, StringComparison.OrdinalIgnoreCase))
                return Length.None();

            var unit = value.unit;
            if (unit != LengthUnit.Percent && unit != LengthUnit.Pixel)
                unit = LengthUnit.Pixel;

            if (trimmedInput.EndsWith(UnitPercent, StringComparison.Ordinal))
            {
                trimmedInput = trimmedInput[..^UnitPercent.Length];
                unit = LengthUnit.Percent;
            }
            else if (trimmedInput.EndsWith(UnitPixel, StringComparison.OrdinalIgnoreCase))
            {
                trimmedInput = trimmedInput[..^UnitPixel.Length];
                unit = LengthUnit.Pixel;
            }

            return UINumericFieldsUtils.TryConvertStringToFloat(trimmedInput.ToString(), textInputBase.originalText, out var result, out _) ? new Length(result, unit) : value;
        }

        internal override bool CanTryParse(string textString) => double.TryParse(textString, out _);

        void UpdateFields()
        {
            text = ValueToString(value);
            m_OptionsPopup.EnableInClassList(invisibleUnitDropdownUssClass, !showUnitAsDropdown);
        }

        bool IsValid(Length newValue)
        {
            if (m_SyntaxTree == null)
                return true;

            var result = m_StyleMatcher.Match(m_SyntaxTree, newValue.ToString());
            return result.success;
        }

        protected internal bool Validate(Length previousValue, Length newValue)
        {
            if (!IsValid(newValue))
            {
                value = previousValue;
                return false;
            }

            return true;
        }

        void UpdateOptionsMenu()
        {
            m_CachedRegularOptionsList.Clear();
            PopulateAdditionalOptions(m_CachedRegularOptionsList);

            m_StyleKeywords.Clear();
            PopulateStyleKeywords(m_StyleKeywords);

            m_AllOptionsList.Clear();
            m_AllOptionsList.AddRange(m_CachedRegularOptionsList);
            m_AllOptionsList.AddRange(m_StyleKeywords);

            m_OptionsPopup.choices = m_AllOptionsList;

            if (!IsValid(value))
                value = GetValidValue();

            SetOptionsPopupFromValue();
        }

        internal void AddOption(string newOption)
        {
            m_OptionsPopup.choices.Add(newOption);

            if (!IsValid(value))
                value = GetValidValue();

            SetOptionsPopupFromValue();
        }

        Length GetValidValue()
        {
            if (m_Units.Count == 0)
                return Length.Auto();
            if (m_Units.Contains(LengthUnit.Pixel))
                return 0;
            if (m_Units.Contains(LengthUnit.Percent))
                return Length.Percent(0);
            return Length.None();
        }

        static bool FindKeywordInExpression(Expression expression, string keyword)
        {
            if (expression.type == ExpressionType.Keyword && expression.keyword == keyword)
                return true;

            if (expression.subExpressions == null)
                return false;

            foreach (var subExp in expression.subExpressions)
                if (FindKeywordInExpression(subExp, keyword))
                    return true;

            return false;
        }

        void PopulateAdditionalOptions(List<string> additionalOptions)
        {
            if (m_SyntaxTree == null)
            {
                additionalOptions.AddRange(KLDefaultUnits);
                return;
            }

            var hasLength = FindUnitInExpression(m_SyntaxTree, DataType.Length);
            var hasPercent = FindUnitInExpression(m_SyntaxTree, DataType.Percentage);

            m_Units.Clear();
            if (hasLength)
                m_Units.Add(LengthUnit.Pixel);
            if (hasPercent)
                m_Units.Add(LengthUnit.Percent);

            foreach (var unit in m_Units)
            {
                additionalOptions.Add(unit.ToDisplayString());
            }
        }

        bool FindUnitInExpression(Expression expression, DataType dataType)
        {
            if (expression.type == ExpressionType.Data && expression.dataType == dataType)
                return true;

            if (expression.subExpressions == null)
                return false;

            foreach (var subExp in expression.subExpressions)
                if (FindUnitInExpression(subExp, dataType))
                    return true;

            return false;
        }

        static string OnFormatSelectedValue(string value)
        {
            return Array.IndexOf(AllKeywords, value) < 0 ? value : s_NoOptionString;
        }

        void SetOptionsPopupFromValue()
        {
            if (value.IsAuto())
                m_OptionsPopup.SetValueWithoutNotify(KeywordAuto);
            else if (value.IsNone())
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
                "auto" => Length.Auto(),
                "none" => Length.None(),
                "px" => new Length(value.value, LengthUnit.Pixel),
                "%" => new Length(value.value, LengthUnit.Percent),
                _ => value
            };

            evt.StopImmediatePropagation();
        }

        class LengthInput : TextValueInput
        {
            internal LengthField parentLengthField { get; set; }

            internal LengthInput()
            {
                formatString = UINumericFieldsUtils.k_DoubleFieldFormatString;
            }

            protected override string allowedCharacters => UINumericFieldsUtils.k_AllowedCharactersForFloat;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, Length startValue)
            {
                var v = StringToValue(text);
                v.unit = startValue.unit;

                if (v.IsAuto() || v.IsNone())
                    v = new Length(0);

                double value = v.value;

                double sensitivity = NumericFieldDraggerUtility.CalculateIntDragSensitivity((long)startValue.value);
                float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                value += NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity;
                value = Mathf.RoundBasedOnMinimumDifference(value, sensitivity);

                v = new Length((float)value, v.unit);

                if (parentLengthField.isDelayed)
                    parentLengthField.text = ValueToString(v);
                else
                    parentLengthField.value = v;
            }

            protected override string ValueToString(Length v)
            {
                return parentLengthField.showUnitAsDropdown
                    ? v.value.ToString(CultureInfo.InvariantCulture)
                    : v.ToString();
            }

            protected override Length StringToValue(string str)
            {
                return Length.ParseString(str, parentLengthField.value);
            }
        }
    }
}
