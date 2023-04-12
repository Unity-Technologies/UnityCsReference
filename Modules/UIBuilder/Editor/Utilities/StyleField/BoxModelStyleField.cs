// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BoxModelStyleField : DimensionStyleField
    {
        static readonly string k_LabelClassName = "unity-box-model__style-field__label";

        private Label m_Label;
        private bool m_ShowUnit;

        public bool showUnit
        {
            get => m_ShowUnit;
            set 
            {
                m_ShowUnit = value;
                UpdateLabel();
            }
        }
        
        public BoxModelStyleField()
        {
            m_Label = new Label(ComposeValue());
            m_Label.AddToClassList(k_LabelClassName);
            Insert(0, m_Label);

            // remove focus on enter
            RegisterCallback<KeyUpEvent>(OnKeyUp);
            draggerIntegerField.labelElement.RegisterCallback<PointerUpEvent>(OnDraggerPointerUp, TrickleDown.TrickleDown);
        }

        void OnKeyUp(KeyUpEvent e)
        {
            if (e.keyCode is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Escape)
            {
                Blur();
            }
        }

        void OnDraggerPointerUp(PointerUpEvent e)
        {
            if (!isUsingLabelDragger)
            {
                textField.Focus();
                return;
            }

            Blur();
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateLabel();
        }

        public void UpdateLabel()
        {
            ((INotifyValueChanged<string>)m_Label).SetValueWithoutNotify(showUnit ? ComposeValue() : GetTextFromValue());
        }
    }
}
