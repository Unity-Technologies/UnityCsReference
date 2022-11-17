// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Abstract base class used for all text-based fields.
    /// </summary>
    public abstract class TextInputBaseField<TValueType> : BaseField<TValueType>
    {
        static CustomStyleProperty<Color> s_SelectionColorProperty = new CustomStyleProperty<Color>("--unity-selection-color");
        static CustomStyleProperty<Color> s_CursorColorProperty = new CustomStyleProperty<Color>("--unity-cursor-color");
        internal const int kMaxLengthNone = -1;
        internal const char kMaskCharDefault = '*';

        // This is to save the value of the tabindex of the visual input to achieve the IMGUI behaviour of tabbing on focused-non-edit-mode TextFields.
        int m_VisualInputTabIndex;

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for <see cref="TextInputFieldBase"/>.
        /// </summary>
        public new class UxmlTraits : BaseFieldTraits<string, UxmlStringAttributeDescription>
        {
            UxmlIntAttributeDescription m_MaxLength = new UxmlIntAttributeDescription { name = "max-length", obsoleteNames = new[] { "maxLength" }, defaultValue = kMaxLengthNone };
            UxmlBoolAttributeDescription m_Password = new UxmlBoolAttributeDescription { name = "password" };
            UxmlStringAttributeDescription m_MaskCharacter = new UxmlStringAttributeDescription { name = "mask-character", obsoleteNames = new[] { "maskCharacter" }, defaultValue = kMaskCharDefault.ToString()};
            UxmlStringAttributeDescription m_PlaceholderText = new UxmlStringAttributeDescription { name = "placeholder-text" };
            UxmlBoolAttributeDescription m_HidePlaceholderOnFocus = new UxmlBoolAttributeDescription { name = "hide-placeholder-on-focus" };
            UxmlBoolAttributeDescription m_IsReadOnly = new UxmlBoolAttributeDescription { name = "readonly" };
            UxmlBoolAttributeDescription m_IsDelayed = new UxmlBoolAttributeDescription {name = "is-delayed"};
            UxmlEnumAttributeDescription<ScrollerVisibility> m_VerticalScrollerVisibility = new UxmlEnumAttributeDescription<ScrollerVisibility> { name = "vertical-scroller-visibility", defaultValue = ScrollerVisibility.Hidden };
            UxmlBoolAttributeDescription m_SelectAllOnMouseUp = new UxmlBoolAttributeDescription { name = "select-all-on-mouse-up", defaultValue = true};
            UxmlBoolAttributeDescription m_SelectAllOnFocus = new UxmlBoolAttributeDescription { name = "select-all-on-focus", defaultValue = true};
            UxmlBoolAttributeDescription m_SelectWordByDoubleClick = new UxmlBoolAttributeDescription { name = "select-word-by-double-click", defaultValue = true };
            UxmlBoolAttributeDescription m_SelectLineByTripleClick = new UxmlBoolAttributeDescription { name = "select-line-by-triple-click", defaultValue = true };
			UxmlBoolAttributeDescription m_HideMobileInput = new UxmlBoolAttributeDescription { name = "hide-mobile-input" };
            UxmlEnumAttributeDescription<TouchScreenKeyboardType> m_KeyboardType = new UxmlEnumAttributeDescription<TouchScreenKeyboardType> { name = "keyboard-type" };
            UxmlBoolAttributeDescription m_AutoCorrection = new UxmlBoolAttributeDescription { name = "auto-correction" };

            /// <summary>
            /// Initialize the traits for this field.
            /// </summary>
            /// <param name="ve">VisualElement to which to apply the attributes.</param>
            /// <param name="bag">Bag of attributes where to get the attributes.</param>
            /// <param name="cc">Creation context.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var field = ((TextInputBaseField<TValueType>)ve);

                field.maxLength = m_MaxLength.GetValueFromBag(bag, cc);
                field.password = m_Password.GetValueFromBag(bag, cc);
                field.readOnly = m_IsReadOnly.GetValueFromBag(bag, cc);
                field.isDelayed = m_IsDelayed.GetValueFromBag(bag, cc);
                field.textSelection.selectAllOnFocus = m_SelectAllOnFocus.GetValueFromBag(bag, cc);
				field.textSelection.selectAllOnMouseUp = m_SelectAllOnMouseUp.GetValueFromBag(bag, cc);
                field.doubleClickSelectsWord = m_SelectWordByDoubleClick.GetValueFromBag(bag, cc);
                field.tripleClickSelectsLine = m_SelectLineByTripleClick.GetValueFromBag(bag, cc);

                var verticalScrollerVisibility = ScrollerVisibility.Hidden;
                m_VerticalScrollerVisibility.TryGetValueFromBag(bag, cc, ref verticalScrollerVisibility);
                field.verticalScrollerVisibility = verticalScrollerVisibility;

				field.hideMobileInput = m_HideMobileInput.GetValueFromBag(bag, cc);
                field.keyboardType = m_KeyboardType.GetValueFromBag(bag, cc);
                field.autoCorrection = m_AutoCorrection.GetValueFromBag(bag, cc);

                string maskCharacter = m_MaskCharacter.GetValueFromBag(bag, cc);
                field.maskChar = (string.IsNullOrEmpty(maskCharacter)) ? kMaskCharDefault : maskCharacter[0];
                field.placeholderText = m_PlaceholderText.GetValueFromBag(bag, cc);
                field.hidePlaceholderOnFocus = m_HidePlaceholderOnFocus.GetValueFromBag(bag, cc);

            }
        }

        #region Properties for UI Builder
        // The UI Builder needs the property to be named exactly the same as the UXML attribute in order for
        // serialization to work properly.

        /// <summary>
        /// DO NOT USE password, use isPassword instead. This property was added to rename the property in the UI Builder.
        /// </summary>
        internal bool password
        {
            get => textEdition.isPassword;
            set => textEdition.isPassword = value;
        }

        /// <summary>
        /// DO NOT USE selectWordByDoubleClick, use textSelection.doubleClickSelectsWord instead. This property was added to rename the property in the UI Builder.
        /// </summary>
        internal bool selectWordByDoubleClick
        {
            get => textSelection.doubleClickSelectsWord;
            set => textSelection.doubleClickSelectsWord = value;
        }

        /// <summary>
        /// DO NOT USE selectLineByTripleClick, use textSelection.tripleClickSelectsLine instead. This property was added to rename the property in the UI Builder.
        /// </summary>
        internal bool selectLineByTripleClick
        {
            get => textSelection.tripleClickSelectsLine;
            set => textSelection.tripleClickSelectsLine = value;
        }

        /// <summary>
        /// DO NOT USE readOnly, use isReadOnly instead. This property was added to rename the property in the UI Builder.
        /// </summary>
        internal bool readOnly
        {
            get => isReadOnly;
            set => isReadOnly = value;
        }

        /// <summary>
        /// DO NOT USE placeholderText, use textEdition.placeholder instead. This property was added so it can be picked up properly by the UI Builder.
        /// </summary>
        internal string placeholderText
        {
            get => textEdition.placeholder;
            set => textEdition.placeholder = value;
        }

        /// <summary>
        /// DO NOT USE hidePlaceholderOnFocus, use textEdition.hidePlaceholderOnFocus instead. This property was added so it can be picked up properly by the UI Builder.
        /// </summary>
        internal bool hidePlaceholderOnFocus
        {
            get => textEdition.hidePlaceholderOnFocus;
            set => textEdition.hidePlaceholderOnFocus = value;
        }

        #endregion

        #region USS ClassNames
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-base-text-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// USS class name of the multiline container.
        /// </summary>
        internal static readonly string multilineContainerClassName = ussClassName + "__multiline-container";
        /// <summary>
        /// USS class name of single line input elements in elements of this type.
        /// </summary>
        public static readonly string singleLineInputUssClassName = inputUssClassName + "--single-line";

        /// <summary>
        /// USS class name of multiline input elements in elements of this type.
        /// </summary>
        public static readonly string multilineInputUssClassName = inputUssClassName + "--multiline";

        /// <summary>
        /// USS class name of input elements when placeholder text is shown
        /// </summary>
        public static readonly string placeholderUssClassName = inputUssClassName + "--placeholder";

        /// <summary>
        /// USS class name of multiline input elements with no scroll view.
        /// </summary>
        internal static readonly string multilineInputWithScrollViewUssClassName = multilineInputUssClassName + "--scroll-view";

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public static readonly string textInputUssName = "unity-text-input";
        #endregion

        protected TextInputBaseField(int maxLength, char maskChar, TextInputBase textInputBase)
            : this(null, maxLength, maskChar, textInputBase) {}

        protected TextInputBaseField(string label, int maxLength, char maskChar, TextInputBase textInputBase)
            : base(label, textInputBase)
        {
            tabIndex = 0;
            delegatesFocus = true;
            labelElement.tabIndex = -1; // To delegate directly to text-input field

            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            visualInput.AddToClassList(singleLineInputUssClassName);

            m_TextInputBase = textInputBase;
            m_TextInputBase.textEdition.maxLength = maxLength;
            m_TextInputBase.textEdition.maskChar = maskChar;

            RegisterCallback<CustomStyleResolvedEvent>(OnFieldCustomStyleResolved);
			textInputBase.textElement.OnPlaceholderChanged += OnPlaceholderChanged;
        }

        TextInputBase m_TextInputBase;
        /// <summary>
        /// This is the text input visual element which presents the value in the field.
        /// </summary>
        protected internal TextInputBase textInputBase => m_TextInputBase;

        /// <summary>
        /// Retrieves this Field's TextElement ITextSelection
        /// </summary>
        public ITextSelection textSelection => m_TextInputBase.textElement.selection;

        /// <summary>
        /// Retrieves this Field's TextElement ITextEdition
        /// </summary>
        public ITextEdition textEdition => m_TextInputBase.textElement.edition;

        #region TextEdition
        /// <summary>
        /// Calls the methods in its invocation list when <see cref="isReadOnly"/> changes.
        /// </summary>
        protected Action<bool> onIsReadOnlyChanged
        {
            get => m_TextInputBase.textElement.onIsReadOnlyChanged;
            set => m_TextInputBase.textElement.onIsReadOnlyChanged = value;
        }

        /// <summary>
        /// Returns true if the field is read only.
        /// </summary>
        public bool isReadOnly
        {
            get => textEdition.isReadOnly;
            set => textEdition.isReadOnly = value;
        }

        // Password field (indirectly lossy behaviour when activated via multiline)
        /// <summary>
        /// Returns true if the field is used to edit a password.
        /// </summary>
        public bool isPasswordField
        {
            get => textEdition.isPassword;
            set
            {
                if (textEdition.isPassword == value)
                    return;

                textEdition.isPassword = value;
                m_TextInputBase.IncrementVersion(VersionChangeType.Repaint);
            }
        }

        /// <summary>
        /// Determines if the touch screen keyboard auto correction is turned on or off.
        /// </summary>
        public bool autoCorrection
        {
            get => textEdition.autoCorrection;
            set => textEdition.autoCorrection = value;
        }

        /// <summary>
        /// Hides or shows the mobile input field.
        /// </summary>
        public bool hideMobileInput
        {
            get => textEdition.hideMobileInput;
            set => textEdition.hideMobileInput = value;
        }

        /// <summary>
        /// The type of mobile keyboard that will be used.
        /// </summary>
        public TouchScreenKeyboardType keyboardType
        {
            get => textEdition.keyboardType;
            set => textEdition.keyboardType = value;
        }

        /// <summary>
        /// The active touch keyboard being displayed.
        /// </summary>
        public TouchScreenKeyboard touchScreenKeyboard
        {
            get => textEdition.touchScreenKeyboard;
        }

        /// <summary>
        /// Maximum number of characters for the field.
        /// </summary>
        public int maxLength
        {
            get => textEdition.maxLength;
            set => textEdition.maxLength = value;
        }

        /// <summary>
        /// If set to true, the value property isn't updated until either the user presses Enter or the text field loses focus.
        /// </summary>
        public bool isDelayed
        {
            get => textEdition.isDelayed;
            set => textEdition.isDelayed = value;
        }

        /// <summary>
        /// The character used for masking in a password field.
        /// </summary>
        public char maskChar
        {
            get => textEdition.maskChar;
            set => textEdition.maskChar = value;
        }

        #endregion

        #region TextSelection
        /// <summary>
        /// Background color of selected text.
        /// </summary>
        public Color selectionColor => textSelection.selectionColor;
        /// <summary>
        /// Color of the cursor.
        /// </summary>
        public Color cursorColor => textSelection.cursorColor;

        /// <summary>
        /// This is the cursor index in the text presented.
        /// </summary>
        public int cursorIndex
        {
            get => textSelection.cursorIndex;
            set => textSelection.cursorIndex = value;
        }

        /// <summary>
        /// The position of the text cursor inside the element.
        /// </summary>
        public Vector2 cursorPosition
        {
            get => textSelection.cursorPosition;
        }

        /// <summary>
        /// This is the selection index in the text presented.
        /// </summary>
        public int selectIndex
        {
            get => textSelection.selectIndex;
            set => textSelection.selectIndex = value;
        }

        /// <summary>
        /// Selects all the text contained in the field.
        /// </summary>
        public void SelectAll()
        {
            textSelection.SelectAll();
        }

        /// <summary>
        /// Remove selection
        /// </summary>
        public void SelectNone()
        {
            textSelection.SelectNone();
        }

        /// <summary>
        /// Select text between cursorIndex and selectIndex.
        /// </summary>
        public void SelectRange(int cursorIndex, int selectionIndex)
        {
            textSelection.SelectRange(cursorIndex, selectionIndex);
        }

        /// <summary>
        /// Controls whether the element's content is selected upon receiving focus.
        /// </summary>
        public bool selectAllOnFocus
        {
            get => textSelection.selectAllOnFocus;
            set => textSelection.selectAllOnFocus = value;
        }

        /// <summary>
        /// Controls whether the element's content is selected when you mouse up for the first time.
        /// </summary>
        public bool selectAllOnMouseUp
        {
            get => textSelection.selectAllOnMouseUp;
            set => textSelection.selectAllOnMouseUp = value;
        }

        /// <summary>
        /// Controls whether double clicking selects the word under the mouse pointer or not.
        /// </summary>
        public bool doubleClickSelectsWord
        {
            get => textSelection.doubleClickSelectsWord;
            set => textSelection.doubleClickSelectsWord = value;
        }
        /// <summary>
        /// Controls whether triple clicking selects the entire line under the mouse pointer or not.
        /// </summary>
        public bool tripleClickSelectsLine
        {
            get => textSelection.tripleClickSelectsLine;
            set => textSelection.tripleClickSelectsLine = value;
        }

        /// <summary>
        /// Options for controlling the visibility of the vertical scroll bar in the <see cref="TextInputBaseField{TValueType}"/>.
        /// </summary>
        [Obsolete("SetVerticalScrollerVisibility is deprecated. Use TextField.verticalScrollerVisibility instead.")]
        public bool SetVerticalScrollerVisibility(ScrollerVisibility sv)
        {
            return textInputBase.SetVerticalScrollerVisibility(sv);
        }
        #endregion
        /// <summary>
        /// The value of the input field.
        /// </summary>
        public string text
        {
            get => m_TextInputBase.text;
            protected set => m_TextInputBase.text = value;
        }

        /// <summary>
        /// Option for controlling the visibility of the vertical scroll bar in the <see cref="TextInputBaseField{TValueType}"/>.
        /// </summary>
        public ScrollerVisibility verticalScrollerVisibility
        {
            get => textInputBase.verticalScrollerVisibility;
            set => textInputBase.SetVerticalScrollerVisibility(value);
        }

        /// <summary>
        /// Computes the size needed to display a text string based on element style values such as font, font-size, and word-wrap.
        /// </summary>
        /// <param name="textToMeasure">The text to measure.</param>
        /// <param name="width">Suggested width. Can be zero.</param>
        /// <param name="widthMode">Width restrictions.</param>
        /// <param name="height">Suggested height.</param>
        /// <param name="heightMode">Height restrictions.</param>
        /// <returns>The horizontal and vertical size needed to display the text string.</returns>
        public Vector2 MeasureTextSize(string textToMeasure, float width, MeasureMode widthMode, float height,
            MeasureMode heightMode)
        {
            return TextUtilities.MeasureVisualElementTextSize(m_TextInputBase.textElement, textToMeasure, width, widthMode, height, heightMode);
        }

        [EventInterest(typeof(NavigationSubmitEvent), typeof(FocusInEvent), typeof(FocusEvent), typeof(BlurEvent))]
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (textEdition.isReadOnly)
                return;

            if (evt.eventTypeId == NavigationSubmitEvent.TypeId() && evt.leafTarget != textInputBase.textElement)
            {
                textInputBase.textElement.Focus();
            }

            // The following code is to help achieve the following behaviour:
            // On IMGUI, on any text input field in focused-non-edit-mode, doing a TAB will allow the user to get to the next control...
            // To mimic that behaviour in UIE, when in focused-non-edit-mode, we have to make sure the input is not "tabbable".
            //     So, each time, either the main TextField or the Label is receiving the focus, we remove the tabIndex on
            //     the input, and we put it back when the BlurEvent is received.
            else if (evt.eventTypeId == FocusInEvent.TypeId())
            {
                if (showMixedValue)
                    textEdition.ResetValueAndText();

                if (evt.leafTarget == this || evt.leafTarget == labelElement)
                {
                    m_VisualInputTabIndex = textInputBase.textElement.tabIndex;
                    textInputBase.textElement.tabIndex = -1;
                }
            }
            // The following code was added to help achieve the following behaviour:
            // On IMGUI, doing a Return, Shift+Return or Escape will get out of the Edit mode, but stay on the control. To allow a
            //     focused-non-edit-mode, we remove the delegateFocus when we start editing to allow focusing on the parent,
            //     and we restore it when we exit the control, to prevent coming in a semi-focused state from outside the control.
            else if (evt.eventTypeId == FocusEvent.TypeId())
            {
                UpdatePlaceholderClassList();
                delegatesFocus = false;
            }
            else if (evt.eventTypeId == BlurEvent.TypeId())
            {
                UpdatePlaceholderClassList();
                delegatesFocus = true;

                if (evt.leafTarget == this || evt.leafTarget == labelElement)
                {
                    textInputBase.textElement.tabIndex = m_VisualInputTabIndex;
                }
            }
        }

        /// <summary>
        /// Converts a value of the specified generic type from the subclass to a string representation.
        /// </summary>
        /// <remarks>Subclasses must implement this method.</remarks>
        /// <param name="value">The value to convert.</param>
        /// <returns>A string representing the value.</returns>
        protected abstract string ValueToString(TValueType value);

        /// <summary>
        /// Converts a string to the value of the specified generic type from the subclass.
        /// </summary>
        /// <remarks>Subclasses must implement this method.</remarks>
        /// <param name="str">The string to convert.</param>
        /// <returns>A value converted from the string.</returns>
        protected abstract TValueType StringToValue(string str);

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                text = mixedValueString;
                AddToClassList(mixedValueLabelUssClassName);
                visualInput?.AddToClassList(mixedValueLabelUssClassName);
            }
            else
            {
                visualInput?.RemoveFromClassList(mixedValueLabelUssClassName);
                RemoveFromClassList(mixedValueLabelUssClassName);
            }
        }

        internal bool hasFocus => textInputBase.textElement.hasFocus;
        internal void OnPlaceholderChanged()
        {
            if (!string.IsNullOrEmpty(textEdition.placeholder))
                RegisterCallback<ChangeEvent<TValueType>>(UpdatePlaceholderClassList);
            else
                UnregisterCallback<ChangeEvent<TValueType>>(UpdatePlaceholderClassList);

            UpdatePlaceholderClassList();
        }

        internal void UpdatePlaceholderClassList(ChangeEvent<TValueType> evt = null)
        {
            if (textInputBase.textElement.showPlaceholderText)
                visualInput.AddToClassList(placeholderUssClassName);
            else
                visualInput.RemoveFromClassList(placeholderUssClassName);
        }

        // UpdateValueFromText and UpdateTextFromValue are overriden by TextValueField and potentially other
        // TextInputFieldBase derived classes when the text content of the TextElement needs to be synchronized with
        // some numerical value. Some TextEditor operations like pressing Enter will update the text to match a
        // canonical form of the underlying value, while other operations force the value to be updated from text.
        internal virtual void UpdateValueFromText()
        {
            value = StringToValue(text);
        }
        internal virtual void UpdateTextFromValue()
        {
            // Do nothing. Value-based fields will override this if appropriate.
        }

        void OnFieldCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            m_TextInputBase.OnInputCustomStyleResolved(e);
        }

        /// <summary>
        /// This is the input text base class visual representation.
        /// </summary>
        protected internal abstract class TextInputBase : VisualElement
        {
            internal TextElement textElement { get; private set; }
            internal ScrollView scrollView;
            internal VisualElement multilineContainer;

            #region USS ClassNames
            /// <summary>
            /// Modifier name of the inner components
            /// </summary>
            public static readonly string innerComponentsModifierName = "--inner-input-field-component";

            /// <summary>
            /// USS class name of the inner TextElement
            /// </summary>
            public static readonly string innerTextElementUssClassName = TextElement.ussClassName + innerComponentsModifierName;

            /// <summary>
            /// USS class name of the inner TextElement
            /// </summary>
            internal static readonly string innerTextElementWithScrollViewUssClassName = TextElement.ussClassName + innerComponentsModifierName + "--scroll-view";

            /// <summary>
            /// USS class name that's added when the inner TextElement if in horizontal.
            /// </summary>
            public static readonly string horizontalVariantInnerTextElementUssClassName = TextElement.ussClassName + innerComponentsModifierName + "--horizontal";
            /// <summary>
            /// USS class name that's added when the inner TextElement if in vertical mode.
            /// </summary>
            public static readonly string verticalVariantInnerTextElementUssClassName = TextElement.ussClassName + innerComponentsModifierName + "--vertical";
            /// <summary>
            /// USS class name that's added when the inner TextElement if in both horizontal and vertical mode.
            /// </summary>
            public static readonly string verticalHorizontalVariantInnerTextElementUssClassName = TextElement.ussClassName + innerComponentsModifierName + "--vertical-horizontal";

            /// <summary>
            /// USS class name of the inner ScrollView
            /// </summary>
            public static readonly string innerScrollviewUssClassName = ScrollView.ussClassName + innerComponentsModifierName;

            /// <summary>
            /// USS class name of the inner ContentContainer
            /// </summary>
            public static readonly string innerViewportUssClassName = ScrollView.viewportUssClassName + innerComponentsModifierName;

            /// <summary>
            /// USS class name of the inner ContentContainer
            /// </summary>
            public static readonly string innerContentContainerUssClassName = ScrollView.contentUssClassName + innerComponentsModifierName;
            #endregion

            internal TextInputBase()
            {
                delegatesFocus = true;

                textElement = new TextElement();
                textElement.selection.isSelectable = true;
                textEdition.isReadOnly = false;
                textSelection.isSelectable = true;
                textSelection.selectAllOnFocus = true;
                textSelection.selectAllOnMouseUp = true;
                textElement.enableRichText = false;
                textElement.tabIndex = 0;

                textEdition.AcceptCharacter += AcceptCharacter;
                textEdition.UpdateScrollOffset += UpdateScrollOffset;
                textEdition.UpdateValueFromText += UpdateValueFromText;
                textEdition.UpdateTextFromValue += UpdateTextFromValue;
                textEdition.MoveFocusToCompositeRoot += MoveFocusToCompositeRoot;
                textEdition.GetDefaultValueType = GetDefaultValueType;


                AddToClassList(inputUssClassName);
                name = TextField.textInputUssName;

                SetSingleLine();

                RegisterCallback<CustomStyleResolvedEvent>(OnInputCustomStyleResolved);

                tabIndex = -1;
            }

            /// <summary>
            /// Retrieves this TextInput's ITextSelection
            /// </summary>
            public ITextSelection textSelection => textElement.selection;

            /// <summary>
            /// Retrieves this TextInput's ITextEdition
            /// </summary>
            public ITextEdition textEdition => textElement.edition;

            internal bool isDragging { get; set; }

            /// <summary>
            /// The value of the input field.
            /// </summary>
            public string text
            {
                get => textElement.text;
                set
                {
                    if (textElement.text == value)
                        return;

                    textElement.text = value;
                }
            }

            /// <summary>
            /// Converts a string to a value type.
            /// </summary>
            /// <param name="str">The string to convert.</param>
            /// <returns>The value parsed from the string.</returns>
            protected virtual TValueType StringToValue(string str)
            {
                throw new NotSupportedException();
            }

            internal void UpdateValueFromText()
            {
                TextInputBaseField<TValueType> parentTextField = (TextInputBaseField<TValueType>)parent;
                parentTextField.UpdateValueFromText();
            }

            internal void UpdateTextFromValue()
            {
                TextInputBaseField<TValueType> parentTextField = (TextInputBaseField<TValueType>)parent;
                parentTextField.UpdateTextFromValue();
            }

            internal void MoveFocusToCompositeRoot()
            {
                TextInputBaseField<TValueType> parentTextField = (TextInputBaseField<TValueType>)parent;
                parentTextField.Focus();
                textEdition.keyboardType = TouchScreenKeyboardType.Default;
                textEdition.autoCorrection = false;
            }

            void MakeSureScrollViewDoesNotLeakEvents(ChangeEvent<float> evt)
            {
                evt.StopPropagation();
            }

            internal void SetSingleLine()
            {
                hierarchy.Clear();
                RemoveMultilineComponents();

                Add(textElement);
                AddToClassList(singleLineInputUssClassName);
                textElement.AddToClassList(innerTextElementUssClassName);

                textElement.RegisterCallback<GeometryChangedEvent>(TextElementOnGeometryChangedEvent);

                // Make sure we reinitialize the vertical scrollOffset but keep the horizontal scrollOffset.
                if (scrollOffset != Vector2.zero)
                {
                   scrollOffset.y = 0;
                   UpdateScrollOffset();
                }
            }

            internal void SetMultiline()
            {
                if (!textEdition.multiline)
                    return;

                RemoveSingleLineComponents();
                RemoveMultilineComponents();

                if (verticalScrollerVisibility != ScrollerVisibility.Hidden && scrollView == null)
                {
                    scrollView = new ScrollView();
                    scrollView.Add(textElement);
                    Add(scrollView);

                    SetScrollViewMode();
                    scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                    scrollView.verticalScrollerVisibility = verticalScrollerVisibility;

                    scrollView.AddToClassList(innerScrollviewUssClassName);
                    scrollView.contentViewport.AddToClassList(innerViewportUssClassName);
                    scrollView.contentContainer.AddToClassList(innerContentContainerUssClassName);
                    scrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(ScrollViewOnGeometryChangedEvent);

                    // The ScrollView's slider can send ChangeEvent<float>. This makes sure these do not leak.
                    scrollView.verticalScroller.slider.RegisterValueChangedCallback(MakeSureScrollViewDoesNotLeakEvents);
                    // We want to make sure the TextElement doesn't loose focus when users are using the slider.
                    scrollView.verticalScroller.slider.focusable = false;

                    scrollView.horizontalScroller.slider.RegisterValueChangedCallback(MakeSureScrollViewDoesNotLeakEvents);
                    scrollView.horizontalScroller.slider.focusable = false;

                    AddToClassList(multilineInputWithScrollViewUssClassName);
                    textElement.AddToClassList(innerTextElementWithScrollViewUssClassName);
                }
                else if (multilineContainer == null)
                {
                    textElement.RegisterCallback<GeometryChangedEvent>(TextElementOnGeometryChangedEvent);
                    multilineContainer = new VisualElement() { classList = { multilineContainerClassName } };

                    multilineContainer.Add(textElement);
                    Add(multilineContainer);
                    SetMultilineContainerStyle();

                    AddToClassList(multilineInputUssClassName);
                    textElement.AddToClassList(innerTextElementUssClassName);
                }
            }

            void ScrollViewOnGeometryChangedEvent(GeometryChangedEvent e)
            {
                if (e.oldRect.size == e.newRect.size)
                    return;

                // The ScrollView is updating its Scroller values the next frame after the GeometryChangedEvent.
                // We need to make sure the new ScrollOffset value is not clamped.
                scrollView.OnUpdateScrollers += UpdateScrollOffset;
            }

            void TextElementOnGeometryChangedEvent(GeometryChangedEvent e)
            {
                if (e.oldRect.size == e.newRect.size)
                    return;

                UpdateScrollOffset();
            }

            internal void OnInputCustomStyleResolved(CustomStyleResolvedEvent e)
            {
                Color selectionValue = Color.clear;
                Color cursorValue = Color.clear;

                // These don't quite follow the inline style behavior
                // (aka setting the value via code should always overrides the one from styleSheets)
                ICustomStyle customStyle = e.customStyle;
                if (customStyle.TryGetValue(s_SelectionColorProperty, out selectionValue))
                    textSelection.selectionColor = selectionValue;

                if (customStyle.TryGetValue(s_CursorColorProperty, out cursorValue))
                    textSelection.cursorColor = cursorValue;

                SetScrollViewMode();
                SetMultilineContainerStyle();
            }

            private string GetDefaultValueType()
            {
                return default(TValueType) == null ? "" : default(TValueType).ToString();
            }

            internal virtual bool AcceptCharacter(char c)
            {
                // When readonly or not enabled in the hierarchy, we do not accept any character.
                return !textEdition.isReadOnly && enabledInHierarchy;
            }

            // scrollOffset is used in automated tests
            internal Vector2 scrollOffset = Vector2.zero;
            bool m_ScrollViewWasClamped;
            internal void UpdateScrollOffset(bool isBackspace = false)
            {
                var selection = textSelection;
                if (selection.cursorIndex < 0)
                    return;

                if (scrollView != null)
                {
                    scrollOffset = GetScrollOffset(scrollView.scrollOffset.x, scrollView.scrollOffset.y, scrollView.contentViewport.layout.width, isBackspace);
                    scrollView.scrollOffset = scrollOffset;

                    m_ScrollViewWasClamped = scrollOffset.x > scrollView.scrollOffset.x || scrollOffset.y > scrollView.scrollOffset.y;
                    scrollView.OnUpdateScrollers -= UpdateScrollOffset;
                }
                else
                {
                    var t = textElement.transform.position;

                    scrollOffset = GetScrollOffset(scrollOffset.x, scrollOffset.y, contentRect.width, isBackspace);

                    t.y = -Mathf.Min(scrollOffset.y, Math.Abs(textElement.contentRect.height - contentRect.height));
                    t.x = -scrollOffset.x;

                    if (!t.Equals(textElement.transform.position))
                        textElement.transform.position = t;
                }
            }

            Vector2 lastCursorPos = Vector2.zero;
            Vector2 GetScrollOffset(float xOffset, float yOffset, float contentViewportWidth, bool isBackspace = false)
            {
                var cursorPos = textSelection.cursorPosition;
                var cursorWidth = textSelection.cursorWidth;

                var newXOffset = xOffset;
                var newYOffset = yOffset;

                const int leftScrollOffsetPadding = 5;
                const float epsilon = 0.05f;

                // Related to: UUM-2057
                // To uncomment once TXT-301 is fixed.
                // if (isBackspace && xOffset > 0.0f)
                // {
                //     newXOffset = xOffset + cursorPos.x - lastCursorPos.x;
                // }
                if (Math.Abs(lastCursorPos.x - cursorPos.x) > epsilon || m_ScrollViewWasClamped)
                {
                    // Update scrollOffset when cursor moves right.
                    if (cursorPos.x > xOffset + contentViewportWidth - cursorWidth)
                        newXOffset = cursorPos.x + cursorWidth - contentViewportWidth;
                    // Update scrollOffset when cursor moves left.
                    else if (cursorPos.x < xOffset + leftScrollOffsetPadding)
                        newXOffset = Mathf.Max(cursorPos.x - leftScrollOffsetPadding, 0);
                }

                if (textEdition.multiline && (Math.Abs(lastCursorPos.y - cursorPos.y) > epsilon || m_ScrollViewWasClamped))
                {
                    // Update scrollOffset when cursor moves down.
                    if (cursorPos.y > contentRect.height + yOffset)
                        newYOffset = cursorPos.y - contentRect.height;
                    // Update scrollOffset when cursor moves up.
                    else if (cursorPos.y < textSelection.lineHeightAtCursorPosition + yOffset + epsilon)
                        newYOffset = cursorPos.y - textSelection.lineHeightAtCursorPosition;
                }

                lastCursorPos = cursorPos;

                if (xOffset != newXOffset || yOffset != newYOffset)
                    return new Vector2(newXOffset, newYOffset);

                return scrollView != null ? scrollView.scrollOffset : scrollOffset;
            }

            internal void SetScrollViewMode()
            {
                if (scrollView == null)
                    return;

                textElement.RemoveFromClassList(verticalVariantInnerTextElementUssClassName);
                textElement.RemoveFromClassList(verticalHorizontalVariantInnerTextElementUssClassName);
                textElement.RemoveFromClassList(horizontalVariantInnerTextElementUssClassName);

                if (textEdition.multiline && computedStyle.whiteSpace == WhiteSpace.Normal)
                {
                    textElement.AddToClassList(verticalVariantInnerTextElementUssClassName);
                    scrollView.mode = ScrollViewMode.Vertical;
                }
                else if (textEdition.multiline)
                {
                    textElement.AddToClassList(verticalHorizontalVariantInnerTextElementUssClassName);
                    scrollView.mode = ScrollViewMode.VerticalAndHorizontal;
                }
                else
                {
                    textElement.AddToClassList(horizontalVariantInnerTextElementUssClassName);
                    scrollView.mode = ScrollViewMode.Horizontal;
                }
            }

            void SetMultilineContainerStyle()
            {
                if (multilineContainer != null)
                {
                    if (computedStyle.whiteSpace == WhiteSpace.Normal)
                        style.overflow = Overflow.Hidden;
                    else
                        style.overflow = (Overflow)OverflowInternal.Scroll;
                }
            }

            void RemoveSingleLineComponents()
            {
                RemoveFromClassList(singleLineInputUssClassName);
                textElement.RemoveFromClassList(innerTextElementUssClassName);
                textElement.RemoveFromHierarchy();
                textElement.UnregisterCallback<GeometryChangedEvent>(TextElementOnGeometryChangedEvent);
            }

            void RemoveMultilineComponents()
            {
                if (scrollView != null)
                {
                    scrollView.RemoveFromHierarchy();
                    scrollView.contentContainer.UnregisterCallback<GeometryChangedEvent>(ScrollViewOnGeometryChangedEvent);
                    scrollView.verticalScroller.slider.UnregisterValueChangedCallback(MakeSureScrollViewDoesNotLeakEvents);
                    scrollView.horizontalScroller.slider.UnregisterValueChangedCallback(MakeSureScrollViewDoesNotLeakEvents);
                    scrollView = null;

                    textElement.RemoveFromClassList(verticalVariantInnerTextElementUssClassName);
                    textElement.RemoveFromClassList(verticalHorizontalVariantInnerTextElementUssClassName);
                    textElement.RemoveFromClassList(horizontalVariantInnerTextElementUssClassName);

                    RemoveFromClassList(multilineInputWithScrollViewUssClassName);
                    textElement.RemoveFromClassList(innerTextElementWithScrollViewUssClassName);
                }

                if (multilineContainer != null)
                {
                    // Make sure we reset the transform
                    textElement.transform.position = Vector3.zero;

                    multilineContainer.RemoveFromHierarchy();
                    textElement.UnregisterCallback<GeometryChangedEvent>(TextElementOnGeometryChangedEvent);
                    multilineContainer = null;

                    RemoveFromClassList(multilineInputUssClassName);
                }
            }

            internal ScrollerVisibility verticalScrollerVisibility = ScrollerVisibility.Hidden;
            internal bool SetVerticalScrollerVisibility(ScrollerVisibility sv)
            {
                if (textEdition.multiline)
                {
                    verticalScrollerVisibility = sv;
                    if (scrollView == null)
                        SetMultiline();
                    else
                        scrollView.verticalScrollerVisibility = verticalScrollerVisibility;

                    return true;
                }
                return false;
            }

            #region Obsolete
            /// <summary>
            /// Selects all the text contained in the field.
            /// </summary>
            [Obsolete("SelectAll() is deprecated. Use textSelection.SelectAll() instead.")]
            public void SelectAll()
            {
                textSelection.SelectAll();
            }

            /// <summary>
            /// Returns true if the field is read only.
            /// </summary>
            [Obsolete("isReadOnly is deprecated. Use textEdition.isReadOnly instead.")]
            public bool isReadOnly
            {
                get => textEdition.isReadOnly;
                set => textEdition.isReadOnly = value;
            }

            /// <summary>
            /// Maximum number of characters for the field.
            /// </summary>
            [Obsolete("maxLength is deprecated. Use textEdition.maxLength instead.")]
            public int maxLength
            {
                get => textEdition.maxLength;
                set => textEdition.maxLength = value;
            }
            /// <summary>
            /// The character used for masking in a password field.
            /// </summary>
            [Obsolete("maskChar is deprecated. Use textEdition.maskChar instead.")]
            public char maskChar
            {
                get => textEdition.maskChar;
                set => textEdition.maskChar = value;
            }

            /// <summary>
            /// Returns true if the field is used to edit a password.
            /// </summary>
            [Obsolete("isPasswordField is deprecated. Use textEdition.isPassword instead.")]
            public virtual bool isPasswordField
            {
                get => textEdition.isPassword;
                set => textEdition.isPassword = value;
            }
            /// <summary>
            /// Background color of selected text.
            /// </summary>
            [Obsolete("selectionColor is deprecated. Use textSelection.selectionColor instead.")]
            public Color selectionColor
            {
                get => textSelection.selectionColor;
                set => textSelection.selectionColor = value;
            }

            /// <summary>
            /// Color of the cursor.
            /// </summary>
            [Obsolete("cursorColor is deprecated. Use textSelection.cursorColor instead.")]
            public Color cursorColor
            {
                get => textSelection.cursorColor;
                set => textSelection.cursorColor = value;
            }

            /// <summary>
            /// This is the cursor index in the text presented.
            /// </summary>
            [Obsolete("cursorIndex is deprecated. Use textSelection.cursorIndex instead.")]
            public int cursorIndex
            {
                get => textSelection.cursorIndex;
            }

            /// <summary>
            /// This is the selection index in the text presented.
            /// </summary>
            [Obsolete("selectIndex is deprecated. Use textSelection.selectIndex instead.")]
            public int selectIndex
            {
                get => textSelection.selectIndex;
            }

            /// <summary>
            /// Controls whether double clicking selects the word under the mouse pointer or not.
            /// </summary>
            [Obsolete("doubleClickSelectsWord is deprecated. Use textSelection.doubleClickSelectsWord instead.")]
            public bool doubleClickSelectsWord
            {
                get => textSelection.doubleClickSelectsWord;
                set => textSelection.doubleClickSelectsWord = value;
            }
            /// <summary>
            /// Controls whether triple clicking selects the entire line under the mouse pointer or not.
            /// </summary>
            [Obsolete("tripleClickSelectsLine is deprecated. Use textSelection.tripleClickSelectsLine instead.")]
            public bool tripleClickSelectsLine
            {
                get => textSelection.tripleClickSelectsLine;
                set => textSelection.tripleClickSelectsLine = value;
            }

            #endregion
        }
    }
}
