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
    [UxmlElement(libraryPath = "Controls")]
    [Icon("UIToolkit/Icons/TextField.png")]
    public partial class TextField : TextInputBaseField<string>
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
                }, false);
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
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

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
            AddToClassList(ussClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);

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
                var oldDispatchMode = dispatchMode;
                try
                {
                    dispatchMode = DispatchMode.Immediate;
                    value = text;
                }
                finally
                {
                    dispatchMode = oldDispatchMode;

                }
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
