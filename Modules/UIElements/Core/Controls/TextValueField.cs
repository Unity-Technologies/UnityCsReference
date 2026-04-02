// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Speed at which the value changes for a given input device delta.
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
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
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public interface IValueField<T>
    {
        /// <summary>
        /// The value of the field.
        /// </summary>
        T value { get; set; }

        /// <summary>
        /// Applies the values of a 3D delta and a speed from an input device.
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
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public abstract class TextValueField<TValueType> : TextInputBaseField<TValueType>, IValueField<TValueType>
    {
        internal static readonly BindingId formatStringProperty = nameof(formatString);
        internal static readonly BindingId supportExpressionsProperty = nameof(supportExpressions);

        [ExcludeFromDocs, Serializable]
        public new abstract class UxmlSerializedData : TextInputBaseField<TValueType>.UxmlSerializedData
        {
            public new static void Register()
            {
                TextInputBaseField<TValueType>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(supportExpressions), "support-expressions", null),
                }, false);
            }

            #pragma warning disable 649
            [Tooltip("Indicates whether the field supports expressions that can be evaluated into a value.")]
            [SerializeField] bool supportExpressions;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags supportExpressions_UxmlAttributeFlags;
            #pragma warning restore 649

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (TextValueField<TValueType>)obj;
                if (ShouldWriteAttributeValue(supportExpressions_UxmlAttributeFlags))
                    e.supportExpressions = supportExpressions;
            }
        }

        // This property to alleviate the fact we have to cast all the time
        TextValueInput textValueInput => (TextValueInput)textInputBase;

        private BaseFieldMouseDragger m_Dragger;
        private bool m_ForceUpdateDisplay;
        private bool m_SupportExpressions = true;
        internal const int kMaxValueFieldLength = 1000;

        internal bool forceUpdateDisplay
        {
            set => m_ForceUpdateDisplay = value;
        }

        /// <summary>
        /// Indicates whether the field supports expressions that can be evaluated into a value.
        /// </summary>
        /// <remarks>
        /// Expressions are mathematical or logical constructs, such as "1+1" or "5*2", which can be processed
        /// to produce a result. When this property is enabled, the field accepts additional characters
        /// required for expressions (e.g., '+', '-', '*', '/', etc.), expanding its functionality beyond the typical restrictions
        /// of a text field that might otherwise only allow digits.
        /// </remarks>
        [CreateProperty]
        public bool supportExpressions
        {
            get => m_SupportExpressions;
            set
            {
                if (m_SupportExpressions != value)
                {
                    m_SupportExpressions = value;
                    NotifyPropertyChanged(supportExpressionsProperty);
                }
            }
        }

        /// <summary>
        /// The format string used to define how numeric values are displayed.
        /// The string follows standard .NET formatting conventions.
        /// </summary>
        /// <remarks>
        /// The supported numeric formats string (using @@0@@ as an example) are:
        ///\\
        ///- <c>"0.#"</c>: Displays the numeric value with up to one decimal place,
        ///     omitting trailing zeros. For example, <c>3.5</c> becomes <c>3.5</c> and
        ///     <c>3.0</c> becomes <c>3</c>.
        ///\\
        ///-<c>"0.00"</c>: Ensures the numeric value is displayed with exactly two decimal
        ///     places. For example, <c>3.5</c> is displayed as <c>3.50</c> and <c>3</c> as
        ///     <c>3.00</c>.
        ///\\
        ///-<c>"0"</c>: Displays only the integer part of a numeric value, rounding if
        ///     necessary. For example, <c>3.5</c> becomes <c>4</c> and <c>3.0</c> becomes <c>3</c>.
        /// </remarks>
        [CreateProperty]
        public string formatString
        {
            get => textValueInput.formatString;
            set
            {
                if (textValueInput.formatString != value)
                {
                    textValueInput.formatString = value;
                    textEdition.UpdateText(ValueToString(rawValue));
                    NotifyPropertyChanged(formatStringProperty);
                }
            }
        }

        protected TextValueField(int maxLength, TextValueInput textValueInput)
            : this(null, maxLength, textValueInput) {}

        protected TextValueField(string label, int maxLength, TextValueInput textValueInput)
            : base(label, maxLength, Char.MinValue, textValueInput)
        {
            textEdition.UpdateText(ValueToString(rawValue));
            onIsReadOnlyChanged += OnIsReadOnlyChanged;
        }

        /// <summary>
        /// Applies the values of a 3D delta and a speed from an input device.
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

        internal override void UpdateValueFromText()
        {
            // Prevent text from changing when the value change
            // This allow expression (2+2) or string like 00123 to remain as typed in the TextField until enter is pressed
            UpdatePlaceholderClassList();
            m_UpdateTextFromValue = false;
            try
            {
                value = StringToValue(text);
            }
            finally
            {
                m_UpdateTextFromValue = true;
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal override void UpdateTextFromValue()
        {
            if (m_UpdateTextFromValue)
            {
                text = ValueToString(rawValue);
            }
        }

        private void OnIsReadOnlyChanged(bool newValue)
        {
            EnableLabelDragger(!newValue);
        }

        internal virtual bool CanTryParse(string textString) => false;

        /// <summary>
        /// Method used to add a mouse dragger on the label for specific numeric fields.
        /// </summary>
        protected void AddLabelDragger<TDraggerType>()
        {
            m_Dragger = new FieldMouseDragger<TDraggerType>((IValueField<TDraggerType>) this);
            EnableLabelDragger(!isReadOnly);
        }

        private void EnableLabelDragger(bool enable)
        {
            if (m_Dragger != null)
            {
                m_Dragger.SetDragZone(enable ? labelElement : null);

                labelElement.EnableInClassList(labelDraggerVariantUssClassNameUnique, enable);
            }
        }

        /// <summary>
        /// Allow to set the value without being notified.
        /// </summary>
        /// <param name="newValue">The new value to set.</param>
        public override void SetValueWithoutNotify(TValueType newValue)
        {
            var displayNeedsUpdate = m_ForceUpdateDisplay || (m_UpdateTextFromValue && !EqualityComparer<TValueType>.Default.Equals(rawValue, newValue));
            base.SetValueWithoutNotify(newValue);

            if (displayNeedsUpdate)
            {
                // Value is the same but the text might not be in sync
                // In the case of an expression like 2+2, the text might not be equal to the result
                textEdition.UpdateText(ValueToString(rawValue));
            }

            m_ForceUpdateDisplay = false;
        }

        /// <summary>
        /// Clears the input field value, reverting to the default state. Placeholder text will be shown if it is set.
        /// </summary>
        public void ClearValue()
        {
            text = string.Empty;
            UpdateValueFromText();
        }

        [EventInterest(typeof(BlurEvent), typeof(FocusEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            //if we want to show placeholder text then we must early out before UpdateValueFromText()
            bool showPlaceholderText = string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(textEdition.placeholder);
            if (showPlaceholderText)
            {
                return;
            }

            if (evt.eventTypeId == BlurEvent.TypeId())
            {
                if (showMixedValue)
                {
                    UpdateMixedValueContent();
                }
                else if (string.IsNullOrEmpty(text))
                {
                    textInputBase.UpdateTextFromValue();
                }
                else
                {
                    textInputBase.UpdateValueFromText();
                    textInputBase.UpdateTextFromValue();
                }
            }
            else if (evt.eventTypeId == FocusEvent.TypeId())
            {
                if (showMixedValue && textInputBase.textElement.hasFocus)
                {
                    textInputBase.text = "";
                }
            }
        }

        internal override void OnViewDataReady()
        {
            // Should the field be reloaded, ensure that the value saved in memory is actually displayed when a data key is used.
            m_ForceUpdateDisplay = true;

            base.OnViewDataReady();
        }

        internal override void RegisterEditingCallbacks()
        {
            base.RegisterEditingCallbacks();
            labelElement.RegisterCallback<PointerDownEvent>(StartEditing, TrickleDown.TrickleDown);
            labelElement.RegisterCallback<PointerUpEvent>(EndEditing);
        }

        internal override void UnregisterEditingCallbacks()
        {
            base.UnregisterEditingCallbacks();
            labelElement.UnregisterCallback<PointerDownEvent>(StartEditing, TrickleDown.TrickleDown);
            labelElement.UnregisterCallback<PointerUpEvent>(EndEditing);
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
                textEdition.AcceptCharacter = AcceptCharacter;
            }

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
                textSelection.SelectNone();
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
                textSelection.SelectAll();
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
        }
    }
}
