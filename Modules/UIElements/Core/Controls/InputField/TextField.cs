// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A TextField accepts and displays text input. For more information, refer to [[wiki:UIE-uxml-element-TextField|UXML element TextField]].
    /// </summary>
    public class TextField : TextInputBaseField<string>
    {
        internal static readonly BindingId multilineProperty = nameof(multiline);

        // This property to alleviate the fact we have to cast all the time
        TextInput textInput => (TextInput)textInputBase;

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : TextInputBaseField<string>.UxmlSerializedData, IUxmlSerializedDataCustomAttributeHandler
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                TextInputBaseField<string>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(multiline), "multiline")
                });
            }

            #pragma warning disable 649
            [SerializeField, MultilineDecorator] bool multiline;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags multiline_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new TextField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                // rely on multiline to set verticalScrollerVisibility, see: https://jira.unity3d.com/browse/UIT-2027
                // TODO: Check https://jira.unity3d.com/browse/UIT-2027
                var e = (TextField)obj;
                if (ShouldWriteAttributeValue(multiline_UxmlAttributeFlags))
                    e.multiline = multiline;
                if (ShouldWriteAttributeValue(verticalScrollerVisibility_UxmlAttributeFlags))
                    e.verticalScrollerVisibility = verticalScrollerVisibility;
            }

            void IUxmlSerializedDataCustomAttributeHandler.SerializeCustomAttributes(IUxmlAttributes bag, HashSet<string> handledAttributes)
            {
                if (bag.TryGetAttributeValue("text", out var text))
                {
                    Value = text;
                    handledAttributes.Add("value");

                    if (bag is UxmlAsset uxmlAsset)
                    {
                        uxmlAsset.RemoveAttribute("text");
                        uxmlAsset.SetAttribute("value", Value);
                    }
                }
            }
        }

        /// <summary>
        /// Instantiates a <see cref="TextField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<TextField, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TextField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : TextInputBaseField<string>.UxmlTraits
        {
            // Using a static attribute here because we want to override the behaviour of an
            // attribute from a base trait class, without the attribute appearing twice in the
            // UI Builder.
            static readonly UxmlStringAttributeDescription k_Value = new UxmlStringAttributeDescription
            {
                name = "value",
                obsoleteNames = new [] { "text" }
            };

            UxmlBoolAttributeDescription m_Multiline = new UxmlBoolAttributeDescription { name = "multiline" };

            /// <summary>
            /// Initialize <see cref="TextField"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                TextField field = ((TextField)ve);
                base.Init(ve, bag, cc);

                // Re-defining the value to account for the "obsolete" text property.
                // We are doing it here because TextField binds the value to the text and this
                // is not the case in the base class.
                var value = string.Empty;
                if (k_Value.TryGetValueFromBag(bag, cc, ref value))
                {
                    field.SetValueWithoutNotify(value);
                }

                field.multiline = m_Multiline.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// Set this to true to allow multiple lines in the textfield and false if otherwise.
        /// </summary>
        [CreateProperty]
        public bool multiline
        {
            get { return textInput.multiline; }
            set
            {
                var previous = multiline;
                textInput.multiline = value;

                if (previous != multiline)
                    NotifyPropertyChanged(multilineProperty);
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-text-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Creates a new textfield.
        /// </summary>
        public TextField()
            : this(null) {}

        /// <summary>
        /// Creates a new textfield.
        /// </summary>
        /// <param name="maxLength">The maximum number of characters this textfield can hold. If -1, there is no limit.</param>
        /// <param name="multiline">Set this to true to allow multiple lines in the textfield and false if otherwise.</param>
        /// <param name="isPasswordField">Set this to true to mask the characters and false if otherwise.</param>
        /// <param name="maskChar">The character used for masking in a password field.</param>
        public TextField(int maxLength, bool multiline, bool isPasswordField, char maskChar)
            : this(null, maxLength, multiline, isPasswordField, maskChar) {}

        /// <summary>
        /// Creates a new textfield.
        /// </summary>
        public TextField(string label)
            : this(label, kMaxLengthNone, false, false, kMaskCharDefault) {}

        /// <summary>
        /// Creates a new textfield.
        /// </summary>
        /// <param name="maxLength">The maximum number of characters this textfield can hold. If -1, there is no limit.</param>
        /// <param name="multiline">Set this to true to allow multiple lines in the textfield and false if otherwise.</param>
        /// <param name="isPassword">Set this to true to mask the characters and false if otherwise.</param>
        /// <param name="maskChar">The character used for masking in a password field.</param>
        public TextField(string label, int maxLength, bool multiline, bool isPasswordField, char maskChar)
            : base(label, maxLength, maskChar, new TextInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            pickingMode = PickingMode.Ignore;
            SetValueWithoutNotify("");
            this.multiline = multiline;
            textEdition.isPassword = isPasswordField;
        }

        /// <summary>
        /// The string currently being exposed by the field.
        /// </summary>
        public override string value
        {
            get { return base.value; }
            set
            {
                base.value = value;
                textEdition.UpdateText(rawValue);
            }
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);

            var textValue = rawValue;

            if (!multiline && rawValue != null)
                textValue = rawValue.Replace("\n", "");

            ((INotifyValueChanged<string>)textInput.textElement).SetValueWithoutNotify(textValue);
        }

        internal override void UpdateTextFromValue()
        {
            SetValueWithoutNotify(rawValue);
        }

        [EventInterest(typeof(FocusOutEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (isDelayed && evt?.eventTypeId == FocusOutEvent.TypeId())
            {
                value = text;
            }
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            string key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);

            // Here we must make sure the value is restored on screen from the saved value !
            text = rawValue;
        }

        protected override string ValueToString(string value) => value;

        protected override string StringToValue(string str) => str;

        class TextInput : TextInputBase
        {
            TextField parentTextField => (TextField)parent;

            public bool multiline
            {
                get { return textEdition.multiline; }
                set
                {
                    var textMatchesMultiline = !(!value && !string.IsNullOrEmpty(text) && text.Contains("\n"));

                    if (textMatchesMultiline && textEdition.multiline == value)
                        return;

                    textEdition.multiline = value;
                    if (value)
                    {
                        // resetting the input's text to raw value to ensure any removed new lines are added back
                        text = parentTextField.rawValue;
                        SetMultiline();
                    }
                    else
                    {
                        text = text.Replace("\n", "");
                        SetSingleLine();
                    }
                }
            }

            // Password field (indirectly lossy behaviour when activated via multiline)
            [Obsolete("isPasswordField is deprecated. Use textEdition.isPassword instead.")]
            public override bool isPasswordField
            {
                set
                {
                    textEdition.isPassword = value;
                    if (value)
                        multiline = false;
                }
            }

            protected override string StringToValue(string str) => str;
        }
    }
}
