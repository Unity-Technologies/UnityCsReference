using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    class VariableField : VisualElement, INotifyValueChanged<string>
    {
        static readonly string s_UssClassName = "unity-builder-variable-field";
        static readonly string s_PrefixClassName = s_UssClassName + "--prefix";
        static readonly string s_PlaceholderLabelClassName = s_UssClassName + "__placeholder-label";

        TextField m_Field;
        Label m_PlaceholderLabel;

        string m_Value;

        public TextField textField => m_Field;
        public bool isReadOnly
        {
            get { return m_Field.isReadOnly; }
            set
            {
                m_Field.isReadOnly = value;
            }
        }

        public string value
        {
            get { return m_Value; }
            set
            {
                SetValue(value, true);
            }
        }

        public void SetValueWithoutNotify(string value)
        {
            SetValue(value, false);
        }

        void SetValue(string value, bool notify)
        {
            string cleanValue = StyleSheetUtilities.GetCleanVariableName(value);

            if (m_Value == cleanValue)
                return;

            var oldValue = m_Value;
            m_Value = cleanValue;

            if (panel != null && notify)
            {
                using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(oldValue, cleanValue))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
            }
            m_Field.SetValueWithoutNotify(m_Value);
            UpdatePlaceholderLabelVisibility();
        }

        public string placeholderText
        {
            get { return m_PlaceholderLabel.text; }
            set
            {
                if (placeholderText == value)
                    return;
                m_PlaceholderLabel.text = value;
                UpdatePlaceholderLabelVisibility();
            }
        }

        public VariableField()
        {
            m_Field = new TextField();

            AddToClassList(s_UssClassName);

            m_PlaceholderLabel = new Label();
            m_PlaceholderLabel.pickingMode = PickingMode.Ignore;
            m_PlaceholderLabel.AddToClassList(s_PlaceholderLabelClassName);
            m_PlaceholderLabel.pickingMode = PickingMode.Ignore;

            Add(m_Field);
            Add(m_PlaceholderLabel);

            m_Field.Q(TextField.textInputUssName).RegisterCallback<BlurEvent>(e =>
            {
                value = m_Field?.value?.Trim();
            });

            m_Field.RegisterValueChangedCallback<string>(e =>
            {
                UpdatePlaceholderLabelVisibility();
                e.StopImmediatePropagation();
            });
            m_PlaceholderLabel.RegisterValueChangedCallback<string>(e =>
            {
                e.StopImmediatePropagation();
            });
        }

        void UpdatePlaceholderLabelVisibility()
        {
            m_PlaceholderLabel.visible = string.IsNullOrEmpty(m_Field.value);
        }
    }
}
