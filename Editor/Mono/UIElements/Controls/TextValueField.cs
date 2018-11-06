// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
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
        void StartDragging();
        void StopDragging();
    }

    public abstract class TextValueField<TValueType> : TextInputBaseField<TValueType>, IValueField<TValueType>
    {
        // This property to alleviate the fact we have to cast all the time
        TextValueInput textValueInput => (TextValueInput)textInputBase;

        protected abstract string ValueToString(TValueType value);
        protected abstract TValueType StringToValue(string str);

        public string formatString
        {
            get { return textValueInput.formatString; }
            set { textValueInput.formatString = value; }
        }

        protected TextValueField(int maxLength, TextValueInput textValueInput)
            : this(null, maxLength, textValueInput) {}

        protected TextValueField(string label, int maxLength, TextValueInput textValueInput)
            : base(label, maxLength, Char.MinValue, textValueInput)
        {
            SetValueWithoutNotify(default(TValueType));
        }

        public abstract void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, TValueType startValue);
        public void StartDragging()
        {
            textValueInput.StartDragging();
        }

        public void StopDragging()
        {
            textValueInput.StopDragging();
        }

        public override TValueType value
        {
            get { return base.value; }
            set
            {
                base.value = value;
                if (textValueInput.m_UpdateTextFromValue)
                    text = ValueToString(rawValue);
            }
        }


        protected void AddLabelDragger<TDraggerType>()
        {
            var dragger = new FieldMouseDragger<TDraggerType>((IValueField<TDraggerType>) this);
            dragger.SetDragZone(labelElement);
            labelElement.AddToClassList(labelDraggerVariantUssClassName);
        }

        public override void SetValueWithoutNotify(TValueType newValue)
        {
            base.SetValueWithoutNotify(newValue);
            if (!isDelayed && textValueInput.m_UpdateTextFromValue)
            {
                // Value is the same but the text might not be in sync
                // In the case of an expression like 2+2, the text might not be equal to the result
                text = ValueToString(rawValue);
            }
        }

        // Implements a control with a value of type T backed by a text.
        protected abstract class TextValueInput : TextInputBase
        {
            TextValueField<TValueType> textValueFieldParent => (TextValueField<TValueType>)parentField;

            protected TextValueInput()
            {
                m_UpdateTextFromValue = true;
            }

            internal bool m_UpdateTextFromValue;

            void UpdateValueFromText()
            {
                var newValue = StringToValue(textValueFieldParent.text);
                textValueFieldParent.value = newValue;
            }

            internal override bool AcceptCharacter(char c)
            {
                return c != 0 && allowedCharacters.IndexOf(c) != -1;
            }

            protected abstract string allowedCharacters { get; }

            public string formatString { get; set; }

            public abstract void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, TValueType startValue);

            public void StartDragging()
            {
                isDragging = true;
                SelectNone();
                MarkDirtyRepaint();
            }

            public void StopDragging()
            {
                isDragging = false;
                SelectAll();
                MarkDirtyRepaint();
            }

            protected abstract string ValueToString(TValueType value);

            protected abstract TValueType StringToValue(string str);

            protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
            {
                base.ExecuteDefaultActionAtTarget(evt);

                bool hasChanged = false;
                if (evt.eventTypeId == KeyDownEvent.TypeId())
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
                else if (evt.eventTypeId == ExecuteCommandEvent.TypeId())
                {
                    ExecuteCommandEvent commandEvt = evt as ExecuteCommandEvent;
                    string cmdName = commandEvt.commandName;
                    if (cmdName == EventCommandNames.Paste || cmdName == EventCommandNames.Cut)
                    {
                        hasChanged = true;
                    }
                }

                if (!textValueFieldParent.isDelayed && hasChanged)
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

                if (evt == null)
                {
                    return;
                }

                if (evt.eventTypeId == BlurEvent.TypeId())
                {
                    if (string.IsNullOrEmpty(textValueFieldParent.text))
                    {
                        // Make sure that empty field gets the default value
                        textValueFieldParent.value = default(TValueType);
                    }
                    else
                    {
                        UpdateValueFromText();
                    }
                }
            }
        }
    }
}
