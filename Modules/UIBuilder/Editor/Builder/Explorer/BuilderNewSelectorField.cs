// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class NewSelectorSubmitEvent : EventBase<NewSelectorSubmitEvent>
    {
        public NewSelectorSubmitEvent()
        {
            LocalInit();
        }

        public string selectorStr { get; set; }

        void LocalInit()
        {
            bubbles = false;
            tricklesDown = false;
        }

        public static NewSelectorSubmitEvent GetPooled(string newSelectorString)
        {
            var t = GetPooled();
            t.LocalInit();
            t.selectorStr = newSelectorString;
            return t;
        }
    }

    internal class BuilderNewSelectorField : VisualElement, INotifyValueChanged<string>
    {
        static internal BindingId valueProperty = nameof(value);

        enum FieldFocusStep
        {
            Idle,
            FocusedFromStandby,
            NeedsSelectionOverride
        }

        static readonly List<string> kNewSelectorPseudoStatesNames = new List<string>()
        {
            ":hover", ":active", ":selected", ":checked", ":focus", ":disabled"
        };

        static readonly string s_UssPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderNewSelectorField.uss";
        static readonly string s_UxmlPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderNewSelectorField.uxml";

        static readonly string s_UssClassName = "unity-new-selector-field";
        static readonly string s_OptionsPopupUssClassName = "unity-new-selector-field__options-popup";
        static readonly string s_TextFieldName = "unity-text-field";
        static readonly string s_OptionsPopupContainerName = "unity-options-popup-container";
        internal static readonly string s_TextFieldUssClassName = "unity-new-selector-field__text-field";

        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new BuilderNewSelectorField();
        }

        TextField m_TextField;
        ToolbarMenu m_OptionsPopup;

        FieldFocusStep m_FieldFocusStep;
        bool m_RefocusOnNextBlur;

        public BuilderNewSelectorField()
        {
            AddToClassList(s_UssClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);
            template.CloneTree(this);

            m_TextField = this.Q<TextField>(s_TextFieldName);

            var popupContainer = this.Q(s_OptionsPopupContainerName);
            m_OptionsPopup = new ToolbarMenu();
            m_OptionsPopup.AddToClassList(s_OptionsPopupUssClassName);
            popupContainer.Add(m_OptionsPopup);

            SetUpPseudoStatesMenu();
            m_OptionsPopup.text = ":";
            m_OptionsPopup.SetEnabled(false);

            m_TextField.RegisterValueChangedCallback<string>(OnTextFieldValueChange);
            var input = m_TextField.Q<TextField.TextInputBase>("unity-text-input");
            input.delegatesFocus = true;
            input.focusable = true;
            input.tabIndex = -1;

            delegatesFocus = true;
            focusable = true;

            var textEdition = (TextElement) input.textEdition;
            textEdition.RegisterCallback<FocusEvent>(evt =>
            {
                m_FieldFocusStep = FieldFocusStep.FocusedFromStandby;
                if (string.IsNullOrEmpty(value) || m_RefocusOnNextBlur)
                {
                    m_TextField.textSelection.selectAllOnMouseUp = false;
                    value = BuilderConstants.UssSelectorClassNameSymbol;
                    m_TextField.textSelection.SelectRange(value.Length, value.Length);
                    m_FieldFocusStep = FieldFocusStep.NeedsSelectionOverride;
                }
                m_RefocusOnNextBlur = false;
            });

            m_TextField.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                if (m_FieldFocusStep != FieldFocusStep.FocusedFromStandby)
                    return;

                m_FieldFocusStep = value == BuilderConstants.UssSelectorClassNameSymbol ? FieldFocusStep.NeedsSelectionOverride : FieldFocusStep.Idle;

                // We don't want the '.' we just inserted in the FocusEvent to be highlighted,
                // which is the default behavior. Same goes for when we add pseudo states with options menu.
                m_TextField.textSelection.SelectRange(value.Length, value.Length);

                var pooled = ChangeEvent<string>.GetPooled(evt.previousValue, evt.newValue);
                pooled.target = this;
                SendEvent(pooled);
            });

            m_TextField.RegisterCallback<KeyDownEvent>(OnEnter, TrickleDown.TrickleDown);

            // Since MouseDown captures the mouse, we need to RegisterCallback directly on the target in order to intercept the event.
            // This could be replaced by setting selectAllOnMouseUp to false.
            ((TextElement)input.textEdition).RegisterCallback<MouseUpEvent>((evt) =>
            {
                // We want to prevent the default action on mouse up in KeyboardTextEditor, but only when
                // the field selection behaviour was changed by us.
                if (m_FieldFocusStep != FieldFocusStep.NeedsSelectionOverride)
                    return;

                m_FieldFocusStep = FieldFocusStep.Idle;

                // Reselect on the next execution, after the KeyboardTextEditor selects all.
                input.schedule.Execute(() =>
                {
                    m_TextField.textSelection.SelectRange(value.Length, value.Length);
                });

            }, TrickleDown.TrickleDown);

            textEdition.RegisterCallback<BlurEvent>((evt) =>
            {
                m_TextField.textSelection.selectAllOnMouseUp = true;

                if (m_RefocusOnNextBlur)
                {
                    Focus();
                    m_RefocusOnNextBlur = false;
                }
                else if (string.IsNullOrEmpty(value) || value == BuilderConstants.UssSelectorClassNameSymbol)
                {
                    value = string.Empty;
                    m_OptionsPopup.SetEnabled(false);
                }
            });
        }

        protected void OnTextFieldValueChange(ChangeEvent<string> evt)
        {
            if (!string.IsNullOrEmpty(evt.newValue) && evt.newValue != BuilderConstants.UssSelectorClassNameSymbol)
            {
                m_OptionsPopup.SetEnabled(true);
            }
            else
            {
                m_OptionsPopup.SetEnabled(false);
            }
        }

        void SetUpPseudoStatesMenu()
        {
            foreach (var state in kNewSelectorPseudoStatesNames)
                m_OptionsPopup.menu.AppendAction(state, a =>
                {
                    value += a.name;
                });
        }

        void OnEnter(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                return;

            var submitEvent = NewSelectorSubmitEvent.GetPooled(value);
            submitEvent.target = this;
            SendEvent(submitEvent);

            evt.StopImmediatePropagation();

            // Whenever we create a selector from a submit event, we want to give back
            // the focus to the selector field to be able to input another selector right away.
            // Of course, this needs to only happen in this situation, so that the selector field
            // won't continually steal the focus.
            m_RefocusOnNextBlur = true;

            value = string.Empty;
        }

        public string value
        {
            get => m_TextField.value;
            set
            {
                if (string.CompareOrdinal(this.value, value) == 0)
                    return;

                if (panel != null)
                {
                    var previousValue = this.value;
                    SetValueWithoutNotify(value);

                    using (var evt = ChangeEvent<string>.GetPooled(previousValue, this.value))
                    {
                        evt.elementTarget = this;
                        SendEvent(evt);
                    }
                    NotifyPropertyChanged(valueProperty);
                }
                else
                {
                    SetValueWithoutNotify(value);
                }
            }
        }
        public void SetValueWithoutNotify(string newValue)
        {
            m_TextField.SetValueWithoutNotify(newValue);
        }

        public void SelectAll()
        {
            m_TextField.textSelection.SelectAll();
        }
    }
}

