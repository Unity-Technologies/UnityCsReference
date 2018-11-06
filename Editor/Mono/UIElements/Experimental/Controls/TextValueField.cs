// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public enum DeltaSpeed
    {
        Fast,
        Normal,
        Slow
    }

    public interface IValueField<T>
    {
        T value { get; set; }

        void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, T startValue);
    }

    // Implements a control with a value of type T backed by a text.
    public abstract class TextValueField<T> : TextInputFieldBase<T>, IValueField<T>
    {
        public new class UxmlTraits : TextInputFieldBase<T>.UxmlTraits {}

        protected TextValueField(int maxLength)
            : base(maxLength, Char.MinValue)
        {
            m_UpdateTextFromValue = true;
            SetValueWithoutNotify(default(T));
        }

        private bool m_UpdateTextFromValue;

        public override T value
        {
            get { return base.value; }
            set
            {
                base.value = value;
                if (m_UpdateTextFromValue)
                    text = ValueToString(m_Value);
            }
        }

        private void UpdateValueFromText()
        {
            T newValue = StringToValue(text);
            value = newValue;
        }

        public override void SetValueWithoutNotify(T newValue)
        {
            base.SetValueWithoutNotify(newValue);
            if (!isDelayed && m_UpdateTextFromValue)
            {
                // Value is the same but the text might not be in sync
                // In the case of an expression like 2+2, the text might not be equal to the result
                text = ValueToString(m_Value);
            }
        }

        [Obsolete("This method is replaced by simply using this.value. The default behaviour has been changed to notify when changed. If the behaviour is not to be notified, SetValueWithoutNotify() must be used.", false)]
        public override void SetValueAndNotify(T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(value, newValue))
            {
                value = newValue;
            }
        }

        internal override bool AcceptCharacter(char c)
        {
            return c != 0 && allowedCharacters.IndexOf(c) != -1;
        }

        protected abstract string allowedCharacters { get; }

        public string formatString { get; set; }

        public abstract void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, T startValue);

        protected abstract string ValueToString(T value);

        protected abstract T StringToValue(string str);

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            bool hasChanged = false;
            if (evt.GetEventTypeId() == KeyDownEvent.TypeId())
            {
                KeyDownEvent kde = evt as KeyDownEvent;
                if (kde.character == '\n')
                {
                    UpdateValueFromText();
                }
                else
                {
                    hasChanged = true;
                }
            }
            else if (evt.GetEventTypeId() == ExecuteCommandEvent.TypeId())
            {
                ExecuteCommandEvent commandEvt = evt as ExecuteCommandEvent;
                string cmdName = commandEvt.commandName;
                if (cmdName == EventCommandNames.Paste || cmdName == EventCommandNames.Cut)
                {
                    hasChanged = true;
                }
            }

            if (!isDelayed && hasChanged)
            {
                // Prevent text from changing when the value change
                // This allow expression (2+2) or string like 00123 to remain as typed in the TextField until enter is pressed
                m_UpdateTextFromValue = false;
                try
                {
                    UpdateValueFromText();
                }
                finally
                {
                    m_UpdateTextFromValue = true;
                }
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == BlurEvent.TypeId())
            {
                if (string.IsNullOrEmpty(text))
                {
                    // Make sure that empty field gets the default value
                    value = default(T);
                }
                else
                {
                    UpdateValueFromText();
                }
            }
        }
    }
}
