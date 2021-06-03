using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class StyleFieldBase : BaseField<string>
    {
        public StyleFieldBase(string label) : base(label) {}
    }

    internal abstract class StyleField<T> : StyleFieldBase
    {
        internal static readonly string s_NoOptionString = "-";

        static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/StyleField/StyleField.uss";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/StyleField.uxml";

        static readonly string s_UssClassName = "unity-style-field";
        static readonly string s_OptionsPopupUssClassName = "unity-style-field__options-popup";
        static readonly string s_VisualInputName = "unity-visual-input";
        static readonly string s_TextFieldName = "unity-text-field";
        static readonly string s_OptionsPopupContainerName = "unity-options-popup-container";

        static readonly string s_DefaultKeyword = StyleFieldConstants.KeywordInitial;

        public new class UxmlTraits : BaseField<string>.UxmlTraits {}

        TextField m_TextField;
        PopupField<string> m_OptionsPopup;
        List<string> m_StyleKeywords;
        List<string> m_CachedRegularOptionsList;
        List<string> m_AllOptionsList;

        protected List<string> styleKeywords => m_StyleKeywords;

        protected TextField textField => m_TextField;
        protected PopupField<string> optionsPopup => m_OptionsPopup;

        public T innerValue { get; protected set; }

        public string option { get; set; } = s_DefaultKeyword;

        public StyleValueKeyword keyword
        {
            get
            {
                var isInMap = StyleFieldConstants.StringToStyleValueKeywordMap.TryGetValue(option, out var optionEnum);

                if (!isInMap)
                    throw new ArgumentException("Call isKeyword first and make sure the current value is a keyword before getting the keyword.");

                return optionEnum;
            }
            set
            {
                var isInMap = StyleFieldConstants.StyleValueKeywordToStringMap.TryGetValue(value, out var option);

                if (isInMap && m_StyleKeywords.Contains(option))
                    this.option = option;
                else
                    this.option = s_DefaultKeyword;

                SetValueWithoutNotify(this.option);
            }
        }

        public bool isKeyword => m_StyleKeywords.Contains(option);

        public bool populatesOptionsMenuFromParentRow { get; set; } = true;

        public StyleField() : this(null) {}

        public StyleField(string label) : base(label)
        {
            AddToClassList(s_UssClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);
            template.CloneTree(this);

            visualInput = this.Q(s_VisualInputName);
            m_TextField = this.Q<TextField>(s_TextFieldName);
            m_TextField.isDelayed = true;

            var popupContainer = this.Q(s_OptionsPopupContainerName);
            m_StyleKeywords = StyleFieldConstants.KLDefault;
            m_CachedRegularOptionsList = GenerateAdditionalOptions(string.Empty);
            m_AllOptionsList = new List<string>();
            m_AllOptionsList.AddRange(m_CachedRegularOptionsList);
            m_AllOptionsList.AddRange(m_StyleKeywords);
            m_OptionsPopup = new PopupField<string>(m_AllOptionsList, 0, OnFormatSelectedValue);
            m_OptionsPopup.AddToClassList(s_OptionsPopupUssClassName);
            popupContainer.Add(m_OptionsPopup);

            m_TextField.RegisterValueChangedCallback(OnTextFieldValueChange);
            m_OptionsPopup.RegisterValueChangedCallback(OnPopupFieldValueChange);
        }

        protected virtual bool SetInnerValueFromValue(string val)
        {
            return false;
        }

        protected virtual bool SetOptionFromValue(string val)
        {
            if (!m_StyleKeywords.Contains(val))
                return false;

            option = val;
            return true;
        }

        protected virtual string ComposeValue()
        {
            if (styleKeywords.Contains(option))
                return option;

            return innerValue.ToString();
        }

        protected virtual void RefreshChildFields()
        {
            m_TextField.SetValueWithoutNotify(GetTextFromValue());
            m_OptionsPopup.SetValueWithoutNotify(GetOptionFromValue());
        }

        protected string GetTextFromValue()
        {
            if (styleKeywords.Contains(option))
                return option;

            return innerValue.ToString();
        }

        protected string GetOptionFromValue()
        {
            return option;
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            SetInnerValueFromValue(newValue);
            SetOptionFromValue(newValue);

            var realValue = ComposeValue();
            base.SetValueWithoutNotify(realValue);

            RefreshChildFields();
        }

        string OnFormatSelectedValue(string value)
        {
            if (m_StyleKeywords.Contains(value))
                return s_NoOptionString;

            return value;
        }

        void OnTextFieldValueChange(ChangeEvent<string> evt)
        {
            value = evt.newValue;

            evt.StopImmediatePropagation();
            evt.PreventDefault();
        }

        void OnPopupFieldValueChange(ChangeEvent<string> evt)
        {
            // There's a bug in UIE that makes the PopupField send a ChangeEvent<string> even
            // if you called SetValueWithoutNotify(). It's the PopupTextElement.text that
            // sends it. Hence, this check.
            if (evt.leafTarget != optionsPopup)
            {
                evt.StopImmediatePropagation();
                evt.PreventDefault();
                return;
            }

            value = evt.newValue;

            evt.StopImmediatePropagation();
            evt.PreventDefault();
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.eventTypeId != AttachToPanelEvent.TypeId())
                return;

            if (!string.IsNullOrEmpty(bindingPath))
            {
                UpdateOptionsMenu(bindingPath);
                return;
            }

            if (populatesOptionsMenuFromParentRow)
            {
                var row = GetFirstAncestorOfType<BuilderStyleRow>();
                if (row != null && !string.IsNullOrEmpty(row.bindingPath))
                {
                    UpdateOptionsMenu(row.bindingPath);
                    return;
                }
            }
        }

        protected virtual List<string> GenerateAdditionalOptions(string binding)
        {
            return new List<string>();
        }

        public void UpdateOptionsMenu()
        {
            UpdateOptionsMenu(bindingPath);
        }

        void UpdateOptionsMenu(string binding)
        {
            m_CachedRegularOptionsList = GenerateAdditionalOptions(binding);
            m_StyleKeywords = StyleFieldConstants.GetStyleKeywords(binding);

            m_AllOptionsList = new List<string>();
            m_AllOptionsList.AddRange(m_CachedRegularOptionsList);
            m_AllOptionsList.AddRange(m_StyleKeywords);

            m_OptionsPopup.choices = m_AllOptionsList;
        }
    }
}
