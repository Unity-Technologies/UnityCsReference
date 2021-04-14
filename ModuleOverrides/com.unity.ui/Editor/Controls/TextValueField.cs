// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Speed at which the value changes for a given input device delta.
    /// </summary>
    public enum DeltaSpeed
    {
        /// <summary>
        /// The value changes at four times the normal rate.
        /// </summary>
        Fast,
        /// <summary>
        /// The value changes at the normal rate.
        /// </summary>
        Normal,
        /// <summary>
        /// The value changes at one quarter of its normal rate.
        /// </summary>
        Slow
    }

    /// <summary>
    /// Base interface for UIElements text value fields.
    /// </summary>
    public interface IValueField<T>
    {
        /// <summary>
        /// The value of the field.
        /// </summary>
        T value { get; set; }

        /// <summary>
        /// Modify the value using a 3D delta and a speed, typically coming from an input device.
        /// </summary>
        /// <param name="delta">A vector used to compute the value change.</param>
        /// <param name="speed">A multiplier for the value change.</param>
        /// <param name="startValue">The start value.</param>
        void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, T startValue);
        /// <summary>
        /// Indicate when the mouse dragging is starting.
        /// </summary>
        void StartDragging();
        /// <summary>
        /// Indicate when the mouse dragging is ending.
        /// </summary>
        void StopDragging();
    }

    /// <summary>
    /// Base class for text fields.
    /// </summary>
    public abstract class TextValueField<TValueType> : TextInputBaseField<TValueType>, IValueField<TValueType>
    {
        // This property to alleviate the fact we have to cast all the time
        TextValueInput textValueInput => (TextValueInput)textInputBase;


        /// <summary>
        /// The format string for the value.
        /// </summary>
        public string formatString
        {
            get { return textValueInput.formatString; }
            set
            {
                textValueInput.formatString = value;
                SetValueWithoutNotify(this.value);
            }
        }

        protected TextValueField(int maxLength, TextValueInput textValueInput)
            : this(null, maxLength, textValueInput) {}

        protected TextValueField(string label, int maxLength, TextValueInput textValueInput)
            : base(label, maxLength, Char.MinValue, textValueInput)
        {
            SetValueWithoutNotify(default(TValueType));
        }

        /// <summary>
        /// Modify the value using a 3D delta and a speed, typically coming from an input device.
        /// </summary>
        /// <param name="delta">A vector used to compute the value change.</param>
        /// <param name="speed">A multiplier for the value change.</param>
        /// <param name="startValue">The start value.</param>
        public abstract void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, TValueType startValue);
        /// <summary>
        /// Indicates the user started the mouse dragging for text selection.
        /// </summary>
        public void StartDragging()
        {
            if (showMixedValue)
            {
                value = default(TValueType);
            }

            textValueInput.StartDragging();
        }

        /// <summary>
        /// Indicates the user stopped the mouse dragging for text selection.
        /// </summary>
        public void StopDragging()
        {
            textValueInput.StopDragging();
        }

        /// <summary>
        /// This is the value of the field.
        /// </summary>
        public override TValueType value
        {
            get { return base.value; }
            set
            {
                base.value = value;
                if (textValueInput.m_UpdateTextFromValue)
                {
                    text = ValueToString(rawValue);
                }
            }
        }

        internal virtual bool CanTryParse(string textString) => false;

        /// <summary>
        /// Method used to add a mouse dragger on the label for specific numeric fields.
        /// </summary>
        protected void AddLabelDragger<TDraggerType>()
        {
            var dragger = new FieldMouseDragger<TDraggerType>((IValueField<TDraggerType>) this);
            dragger.SetDragZone(labelElement);
            labelElement.AddToClassList(labelDraggerVariantUssClassName);
        }

        /// <summary>
        /// Allow to set the value without being notified.
        /// </summary>
        /// <param name="newValue">The new value to set.</param>
        public override void SetValueWithoutNotify(TValueType newValue)
        {
            base.SetValueWithoutNotify(newValue);
            if (textValueInput.m_UpdateTextFromValue)
            {
                // Value is the same but the text might not be in sync
                // In the case of an expression like 2+2, the text might not be equal to the result
                text = ValueToString(rawValue);
            }
        }

        // Implements a control with a value of type T backed by a text.
        /// <summary>
        /// This is the inner representation of the Text input.
        /// </summary>
        protected abstract class TextValueInput : TextInputBase
        {
            TextValueField<TValueType> textValueFieldParent => (TextValueField<TValueType>)parent;

            protected TextValueInput()
            {
                m_UpdateTextFromValue = true;
            }

            internal bool m_UpdateTextFromValue;

            internal override bool AcceptCharacter(char c)
            {
                return base.AcceptCharacter(c) && c != 0 && allowedCharacters.IndexOf(c) != -1;
            }

            /// <summary>
            /// Method to override to indicate the allowed characters in the actual field.
            /// </summary>
            protected abstract string allowedCharacters { get; }

            /// <summary>
            /// Formats the string.
            /// </summary>
            public string formatString { get; set; }

            /// <summary>
            /// Called when the user is dragging the label to update the value contained in the field.
            /// </summary>
            /// <param name="delta">Delta on the move.</param>
            /// <param name="speed">Speed of the move.</param>
            /// <param name="startValue">Starting value.</param>
            public abstract void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, TValueType startValue);

            /// <summary>
            /// Method called by the application when the label of the field is started to be dragged to change the value of it.
            /// </summary>
            public void StartDragging()
            {
                isDragging = true;
                SelectNone();
                MarkDirtyRepaint();
            }

            /// <summary>
            /// Method called by the application when the label of the field is stopped to be dragged to change the value of it.
            /// </summary>
            public void StopDragging()
            {
                if (textValueFieldParent.isDelayed)
                {
                    UpdateValueFromText();
                }
                isDragging = false;
                SelectAll();
                MarkDirtyRepaint();
            }

            /// <summary>
            /// Convert the value to string for visual representation.
            /// </summary>
            /// <param name="value">Value to convert.</param>
            /// <returns>String representation.</returns>
            protected abstract string ValueToString(TValueType value);

            /// <summary>
            /// Converts a string to a value type.
            /// </summary>
            /// <param name="str">The string to convert.</param>
            /// <returns>The value parsed from the string.</returns>
            protected override TValueType StringToValue(string str)
            {
                return base.StringToValue(str);
            }

            protected override void ExecuteDefaultActionAtTarget(EventBase evt)
            {
                base.ExecuteDefaultActionAtTarget(evt);

                bool hasChanged = false;
                if (evt.eventTypeId == KeyDownEvent.TypeId())
                {
                    KeyDownEvent kde = evt as KeyDownEvent;

                    if ((kde?.character == 3) ||     // KeyCode.KeypadEnter
                        (kde?.character == '\n'))    // KeyCode.Return
                    {
                        // Here we should update the value, but it will be done when the blur event is handled...
                        parent.Focus();
                        evt.StopPropagation();
                        evt.PreventDefault();
                    }
                    else if (!isReadOnly)
                    {
                        hasChanged = true;
                    }
                }
                else if (!isReadOnly && evt.eventTypeId == ExecuteCommandEvent.TypeId())
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

            protected override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                if (evt == null)
                {
                    return;
                }

                if (evt.eventTypeId == BlurEvent.TypeId())
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        // Make sure that empty field gets the default value
                        textValueFieldParent.value = default(TValueType);
                    }
                    else
                    {
                        UpdateValueFromText();
                    }
                }
                else if (evt.eventTypeId == FocusEvent.TypeId())
                {
                    if (textValueFieldParent.showMixedValue)
                    {
                        textValueFieldParent.value = default(TValueType);
                    }
                }
            }
        }
    }

    // Derive from BaseFieldTraits in order to not inherit from TextInputBaseField UXML attributes.
    /// <summary>
    /// Specifies the <see cref="TextValueField"/>'s <see cref="UxmlTraits"/>.
    /// </summary>
    public class TextValueFieldTraits<TValueType, TValueUxmlAttributeType> : BaseFieldTraits<TValueType, TValueUxmlAttributeType>
        where TValueUxmlAttributeType : TypedUxmlAttributeDescription<TValueType>, new()
    {
        UxmlBoolAttributeDescription m_IsReadOnly = new UxmlBoolAttributeDescription { name = "readonly" };
        UxmlBoolAttributeDescription m_IsDelayed = new UxmlBoolAttributeDescription {name = "is-delayed"};

        /// <summary>
        /// Initializes the <see cref="TextValueField"/>'s <see cref="UxmlTraits"/>.
        /// </summary>
        /// <param name="ve">The VisualElement to initialize.</param>
        /// <param name="bag">A bag of UXML attribute name-value pairs used to initialize VisualElement members.</param>
        /// <param name="cc">The creation context associated with these traits.</param>
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var field = (TextInputBaseField<TValueType>)ve;
            if (field != null)
            {
                field.isReadOnly = m_IsReadOnly.GetValueFromBag(bag, cc);
                field.isDelayed = m_IsDelayed.GetValueFromBag(bag, cc);
            }
        }
    }
}
