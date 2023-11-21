// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    partial class CategoryDropdownField : BaseField<string>
    {
        [Serializable]
        public new class UxmlSerializedData : BaseField<string>.UxmlSerializedData
        {
            public override object CreateInstance() => new CategoryDropdownField();
        }

        // Base selectors coming from BasePopupField
        const string k_UssClassNameBasePopupField = "unity-base-popup-field";
        const string k_TextUssClassNameBasePopupField = k_UssClassNameBasePopupField + "__text";
        const string k_ArrowUssClassNameBasePopupField = k_UssClassNameBasePopupField + "__arrow";
        const string k_LabelUssClassNameBasePopupField = k_UssClassNameBasePopupField + "__label";
        const string k_InputUssClassNameBasePopupField = k_UssClassNameBasePopupField + "__input";

        // Base selectors coming from PopupField
        const string k_UssClassNamePopupField = "unity-popup-field";
        const string k_LabelUssClassNamePopupField = k_UssClassNamePopupField + "__label";
        const string k_InputUssClassNamePopupField = k_UssClassNamePopupField + "__input";

        readonly TextElement m_Input;

        internal Func<CategoryDropdownContent> getContent;

        public CategoryDropdownField() : this(null)
        {
        }

        public CategoryDropdownField(string label) : base(label, null)
        {
            AddToClassList(k_UssClassNameBasePopupField);
            labelElement.AddToClassList(k_LabelUssClassNameBasePopupField);

            m_Input = new PopupTextElement
            {
                pickingMode = PickingMode.Ignore
            };
            m_Input.AddToClassList(k_TextUssClassNameBasePopupField);
            visualInput.AddToClassList(k_InputUssClassNameBasePopupField);
            visualInput.Add(m_Input);

            var dropdownArrow = new VisualElement();
            dropdownArrow.AddToClassList(k_ArrowUssClassNameBasePopupField);
            dropdownArrow.pickingMode = PickingMode.Ignore;
            visualInput.Add(dropdownArrow);

            AddToClassList(k_UssClassNamePopupField);
            labelElement.AddToClassList(k_LabelUssClassNamePopupField);
            visualInput.AddToClassList(k_InputUssClassNamePopupField);
        }

        class PopupTextElement : TextElement
        {
            protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode,
                float desiredHeight,
                MeasureMode heightMode)
            {
                var textToMeasure = text;
                if (string.IsNullOrEmpty(textToMeasure))
                {
                    textToMeasure = " ";
                }

                return MeasureTextSize(textToMeasure, desiredWidth, widthMode, desiredHeight, heightMode);
            }
        }

        [EventInterest(typeof(KeyDownEvent), typeof(MouseDownEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (evt == null)
                return;

            var showPopupMenu = false;
            if (evt is KeyDownEvent kde)
            {
                if (kde.keyCode == KeyCode.Space ||
                    kde.keyCode == KeyCode.KeypadEnter ||
                    kde.keyCode == KeyCode.Return)
                    showPopupMenu = true;
            }
            else if (evt is MouseDownEvent {button: (int) MouseButton.LeftMouse} mde)
            {
                if (visualInput.ContainsPoint(visualInput.WorldToLocal(mde.mousePosition)))
                    showPopupMenu = true;
            }

            if (!showPopupMenu)
                return;

            ShowMenu();
            evt.StopPropagation();
        }

        void ShowMenu()
        {
            var windowContent = new WindowContent();
            windowContent.onSelectionChanged += selected => value = selected;

            var content = getContent?.Invoke() ?? default;
            windowContent.Show(visualInput.worldBound, value, content.Items);
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            ((INotifyValueChanged<string>) m_Input).SetValueWithoutNotify(value);
        }
    }
}
