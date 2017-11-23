// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public abstract class BaseValueField<T> : VisualElement, INotifyValueChanged<T>
    {
        protected T m_Value;

        public T value
        {
            get { return m_Value; }
            set
            {
                if (!EqualityComparer<T>.Default.Equals(m_Value, value))
                {
                    m_Value = value;
                    UpdateDisplay();
                }
            }
        }

        protected abstract void UpdateDisplay();

        public void SetValueAndNotify(T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(m_Value, newValue))
            {
                using (ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    value = newValue;
                    UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
                }
            }
        }

        public void OnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
        }
    }
}
