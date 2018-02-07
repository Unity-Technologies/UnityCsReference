// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class PopupField<T> : BaseTextElement, INotifyValueChanged<T>
    {
        private readonly List<T> m_PossibleValues;

        private T m_Value;
        public T value
        {
            get { return m_Value; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(m_Value, value))
                    return;

                if (!m_PossibleValues.Contains(value))
                    throw new ArgumentException(string.Format("Value {0} is not present in the list of possible values", value));

                m_Value = value;
                m_Index = m_PossibleValues.IndexOf(m_Value);
                text = m_Value.ToString();
                Dirty(ChangeType.Repaint);
            }
        }

        private int m_Index = -1;
        public int index
        {
            get { return m_Index; }
            set
            {
                if (value != m_Index)
                {
                    if (value >= m_PossibleValues.Count || value < 0)
                        throw new ArgumentException(string.Format("Index {0} is beyond the scope of possible value", value));
                    m_Index = value;
                    this.value = m_PossibleValues[m_Index];
                }
            }
        }

        private PopupField(List<T> possibleValues)
        {
            if (possibleValues == null)
                throw new ArgumentNullException("possibleValues can't be null");

            m_PossibleValues = possibleValues;

            AddToClassList("popupField");
        }

        public PopupField(List<T> possibleValues, T defaultValue) :
            this(possibleValues)
        {
            if (defaultValue == null)
                throw new ArgumentNullException("defaultValue can't be null");

            if (!m_PossibleValues.Contains(defaultValue))
                throw new ArgumentException(string.Format("Default value {0} is not present in the list of possible values", defaultValue));

            // note: idx will be set when setting value
            value = defaultValue;
        }

        public PopupField(List<T> possibleValues, int defaultIndex) :
            this(possibleValues)
        {
            if (defaultIndex >= m_PossibleValues.Count || defaultIndex < 0)
                throw new ArgumentException(string.Format("Default Index {0} is beyond the scope of possible value", value));

            // note: value will be set when setting idx
            index = defaultIndex;
        }

        public void SetValueAndNotify(T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(newValue, value))
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

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == MouseDownEvent.TypeId())
                OnMouseDown();
        }

        private void OnMouseDown()
        {
            var menu = new GenericMenu();

            foreach (T item in m_PossibleValues)
            {
                bool isSelected = EqualityComparer<T>.Default.Equals(item, value);
                menu.AddItem(new GUIContent(item.ToString()), isSelected,
                    () => ChangeValueFromMenu(item));
            }

            var menuPosition = new Vector2(0.0f, layout.height);
            menuPosition = this.LocalToWorld(menuPosition);
            var menuRect = new Rect(menuPosition, Vector2.zero);
            menu.DropDown(menuRect);
        }

        private void ChangeValueFromMenu(T menuItem)
        {
            SetValueAndNotify(menuItem);
        }
    }
}
