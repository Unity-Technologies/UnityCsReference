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
    public abstract class TextValueField<T> : TextInputFieldBase, INotifyValueChanged<T>, IValueField<T>
    {
        protected TextValueField(int maxLength)
            : base(maxLength, Char.MinValue)
        {
        }

        public void OnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
        }

        protected T m_Value;

        public T value
        {
            get { return m_Value; }
            set
            {
                m_Value = value;
                text = ValueToString(m_Value);
            }
        }

        public void UpdateValueFromText()
        {
            T newValue = StringToValue(text);
            SetValueAndNotify(newValue);
        }

        public void SetValueAndNotify(T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(value, newValue))
            {
                using (ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    value = newValue;
                    UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
                }
            }
        }

        public string formatString { get; set; }

        public abstract void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, T startValue);

        protected abstract string ValueToString(T value);

        protected abstract T StringToValue(string str);

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt.GetEventTypeId() == KeyDownEvent.TypeId())
            {
                KeyDownEvent kde = evt as KeyDownEvent;
                if (kde.character == '\n')
                {
                    UpdateValueFromText();
                }
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == BlurEvent.TypeId())
            {
                UpdateValueFromText();
            }
        }
    }
}
