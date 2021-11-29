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
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };
            UxmlBoolAttributeDescription m_IsReadOnly = new UxmlBoolAttributeDescription { name = "readonly" };
            UxmlBoolAttributeDescription m_IsDelayed = new UxmlBoolAttributeDescription {name = "is-delayed"};

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
                field.text = m_Text.GetValueFromBag(bag, cc);
                field.maxLength = m_MaxLength.GetValueFromBag(bag, cc);
                field.isPasswordField = m_Password.GetValueFromBag(bag, cc);
                field.isReadOnly = m_IsReadOnly.GetValueFromBag(bag, cc);
                field.isDelayed = m_IsDelayed.GetValueFromBag(bag, cc);
                string maskCharacter = m_MaskCharacter.GetValueFromBag(bag, cc);
                if (!string.IsNullOrEmpty(maskCharacter))
                {
                    field.maskChar = maskCharacter[0];
                }
            }
        }

        TextInputBase m_TextInputBase;
        /// <summary>
        /// This is the text input visual element which presents the value in the field.
        /// </summary>
        protected internal TextInputBase textInputBase => m_TextInputBase;

        internal const int kMaxLengthNone = -1;
        internal const char kMaskCharDefault = '*';

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
        /// USS class name of single line input elements in elements of this type.
        /// </summary>
        public static readonly string singleLineInputUssClassName = inputUssClassName + "--single-line";

        /// <summary>
        /// USS class name of multiline input elements in elements of this type.
        /// </summary>
        public static readonly string multilineInputUssClassName = inputUssClassName + "--multiline";

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public static readonly string textInputUssName = "unity-text-input";

        /// <summary>
        /// The value of the input field.
        /// </summary>
        public string text
        {
            get => m_TextInputBase.text;
            protected set => m_TextInputBase.text = value;
        }

        /// <summary>
        /// Calls the methods in its invocation list when <see cref="isReadOnly"/> changes.
        /// </summary>
        protected event Action<bool> onIsReadOnlyChanged;

        /// <summary>
        /// Returns true if the field is read only.
        /// </summary>
        public bool isReadOnly
        {
            get => m_TextInputBase.isReadOnly;
            set
            {
                m_TextInputBase.isReadOnly = value;
                onIsReadOnlyChanged?.Invoke(value);
            }
        }

        // Password field (indirectly lossy behaviour when activated via multiline)
        /// <summary>
        /// Returns true if the field is used to edit a password.
        /// </summary>
        public bool isPasswordField
        {
            get => m_TextInputBase.isPasswordField;
            set
            {
                if (m_TextInputBase.isPasswordField == value)
                    return;

                m_TextInputBase.isPasswordField = value;
                m_TextInputBase.IncrementVersion(VersionChangeType.Repaint);
            }
        }

        /// <summary>
        /// Retrieves this Field's TextElement ITextSelection
        /// </summary>
        public ITextSelection textSelection => m_TextInputBase.textElement.selection;

        /// <summary>
        /// Retrieves this Field's TextElement ITextEdition
        /// </summary>
        public ITextEdition textEdition => m_TextInputBase.textElement.edition;

        // TODO: Obsolete all of these belolw and use the exposed TextSelection above

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
        /// This is the position of the cursor inside the <see cref="TextInputBaseField{TValueType}"/>.
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
        /// Selects all the text.
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
        /// Selects text in the textfield between cursorIndex and selectionIndex.
        /// </summary>
        /// <param name="selectionIndex">The selection end position.</param>
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
        /// Maximum number of characters for the field.
        /// </summary>
        public int maxLength
        {
            get => textEdition.maxLength;
            set => textEdition.maxLength = value;
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

        /// <summary>
        /// Options for controlling the visibility of the vertical scroll bar in the <see cref="TextInputBaseField{TValueType}"/>.
        /// </summary>
        public bool SetVerticalScrollerVisibility(ScrollerVisibility sv)
        {
            return textInputBase.SetVerticalScrollerVisibility(sv);
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

        internal bool hasFocus => textEdition.hasFocus;

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
            m_TextInputBase.maxLength = maxLength;
            m_TextInputBase.maskChar = maskChar;

            RegisterCallback<CustomStyleResolvedEvent>(OnFieldCustomStyleResolved);
        }

        private void OnFieldCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            m_TextInputBase.OnInputCustomStyleResolved(e);
        }

        [EventInterest(typeof(KeyDownEvent), typeof(FocusInEvent), typeof(FocusEvent), typeof(BlurEvent))]
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt.eventTypeId == KeyDownEvent.TypeId())
            {
                KeyDownEvent keyDownEvt = evt as KeyDownEvent;

                // We must handle the ETX (char 3) or the \n instead of the KeypadEnter or Return because the focus will
                //     have the drawback of having the second event to be handled by the focused field.
                if ((keyDownEvt?.character == 3) ||     // KeyCode.KeypadEnter
                    (keyDownEvt?.character == '\n'))    // KeyCode.Return
                {
                    textInputBase.textElement.Focus();
                }
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
                delegatesFocus = false;
            }
            else if (evt.eventTypeId == BlurEvent.TypeId())
            {
                delegatesFocus = true;

                if (evt.leafTarget == this || evt.leafTarget == labelElement)
                {
                    textInputBase.textElement.tabIndex = m_VisualInputTabIndex;
                }
            }
        }

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

        /// <summary>
        /// This is the input text base class visual representation.
        /// </summary>
        protected internal abstract class TextInputBase : VisualElement
        {
            internal TextElement textElement { get; private set; }
            internal ScrollView scrollView;


            /// <summary>
            /// Modifier name of the inner components
            /// </summary>
            public static readonly string innerComponentsModifierName = "--inner-input-field-component";

            /// <summary>
            /// USS class name of the inner TextElement
            /// </summary>
            public static readonly string innerTextElementUssClassName = TextElement.ussClassName + innerComponentsModifierName;

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

            /// <summary>
            /// Retrieves this TextInput's ITextSelection
            /// </summary>
            public ITextSelection textSelection => textElement.selection;

            /// <summary>
            /// Retrieves this TextInput's ITextEdition
            /// </summary>
            public ITextEdition textEdition => textElement.edition;

            /// <summary>
            /// Selects all the text contained in the field.
            /// </summary>
            public void SelectAll()
            {
                textSelection.SelectAll();
            }

            internal void SelectNone()
            {
                textSelection.SelectNone();
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
                var newValue = StringToValue(text);
                TextInputBaseField<TValueType> parentTextField = (TextInputBaseField<TValueType>)parent;
                parentTextField.value = newValue;
            }

            /// <summary>
            /// Resets the text contained in the field.
            /// </summary>
            public void ResetValueAndText()
            {
                textEdition.ResetValueAndText();
            }

            /// <summary>
            /// Returns true if the field is read only.
            /// </summary>
            public bool isReadOnly
            {
                get => textEdition.isReadOnly;
                set => textEdition.isReadOnly = value;
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
            /// The character used for masking in a password field.
            /// </summary>
            public char maskChar
            {
                get => textEdition.maskChar;
                set => textEdition.maskChar = value;
            }

            /// <summary>
            /// Returns true if the field is used to edit a password.
            /// </summary>
            public virtual bool isPasswordField
            {
                get => textEdition.isPassword;
                set => textEdition.isPassword = value;
            }

            internal bool isDelayed {
                get => textEdition.isDelayed;
                set => textEdition.isDelayed = value;
            }

            internal bool isDragging { get; set; }

            //we need to deprecate these ASAP

            /// <summary>
            /// Background color of selected text.
            /// </summary>
            public Color selectionColor
            {
                get => textSelection.selectionColor;
                set => textSelection.selectionColor = value;
            }

            /// <summary>
            /// Color of the cursor.
            /// </summary>
            public Color cursorColor
            {
                get => textSelection.cursorColor;
                set => textSelection.cursorColor = value;
            }

            /// <summary>
            /// This is the cursor index in the text presented.
            /// </summary>
            public int cursorIndex
            {
                get => textSelection.cursorIndex;
            }

            /// <summary>
            /// This is the selection index in the text presented.
            /// </summary>
            public int selectIndex
            {
                get => textSelection.selectIndex;
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

            internal TextInputBase()
            {
                delegatesFocus = true;

                textElement = new TextElement();
                textElement.focusable = true;
                textElement.enableRichText = false;
                textEdition.isReadOnly = false;
                textSelection.selectAllOnFocus = true;
                textSelection.selectAllOnMouseUp = true;
                textEdition.AcceptCharacter += AcceptCharacter;
                textEdition.UpdateScrollOffset += UpdateScrollOffset;
                textEdition.UpdateValueFromText += UpdateValueFromText;

                scrollView = new ScrollView();
                SetScrollViewMode();
                scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;

                scrollView.Add(textElement);
                Add(scrollView);

                AddToClassList(inputUssClassName);
                AddToClassList(singleLineInputUssClassName);
                name = TextField.textInputUssName;

                textElement.AddToClassList(innerTextElementUssClassName);
                scrollView.AddToClassList(innerScrollviewUssClassName);
                scrollView.contentViewport.AddToClassList(innerViewportUssClassName);
                scrollView.contentContainer.AddToClassList(innerContentContainerUssClassName);

                RegisterCallback<CustomStyleResolvedEvent>(OnInputCustomStyleResolved);
                scrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(ScrollViewOnGeometryChangedEvent);

                tabIndex = -1;
            }

            internal void ScrollViewOnGeometryChangedEvent(GeometryChangedEvent e)
            {
                if (e.oldRect.size == e.newRect.size || !m_DelayedUpdateScrollOffset)
                    return;

                m_DelayedUpdateScrollOffset = false;
                scrollView.scrollOffset = scrollOffset;
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
            }

            internal virtual bool AcceptCharacter(char c)
            {
                // When readonly or not enabled in the hierarchy, we do not accept any character.
                return !isReadOnly && enabledInHierarchy;
            }

            // scrollOffset and m_DelayedUpdateScrollOffset are used in automated tests
            internal Vector2 scrollOffset = Vector2.zero;
            bool m_DelayedUpdateScrollOffset;
            internal void UpdateScrollOffset()
            {
                var selection = textSelection;
                if (selection.cursorIndex < 0)
                    return;

                var cursorPos = selection.cursorPosition;
                var cursorWidth = selection.cursorWidth;
                var xOffset = scrollView.scrollOffset.x;
                var yOffset = scrollView.scrollOffset.y;
                var contentViewportWidth = scrollView.contentViewport.layout.width;

                if ((cursorPos.x + cursorWidth - scrollView.scrollOffset.x) > contentViewportWidth)
                    xOffset = cursorPos.x + cursorWidth - contentViewportWidth;
                else if (cursorPos.x - cursorWidth - scrollView.scrollOffset.x < 0 && scrollView.scrollOffset.x > 0)
                    xOffset = cursorPos.x - cursorWidth;

                if ((cursorPos.y - scrollView.scrollOffset.y) > contentRect.height)
                    yOffset = cursorPos.y - contentRect.height;
                else if (cursorPos.y - selection.cursorLineHeight - scrollView.scrollOffset.y < 0)
                    yOffset = cursorPos.y - selection.cursorLineHeight;

                if (xOffset != scrollView.scrollOffset.x || yOffset != scrollView.scrollOffset.y)
                {
                    scrollOffset = new Vector2(xOffset, yOffset);
                    if (scrollView.verticalScroller.highValue < yOffset || scrollView.horizontalScroller.highValue < xOffset)
                        m_DelayedUpdateScrollOffset = true;
                    scrollView.scrollOffset = new Vector2(xOffset, yOffset);
                }
            }

            void SetScrollViewMode()
            {
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

            internal bool SetVerticalScrollerVisibility(ScrollerVisibility sv)
            {
                if (textEdition.multiline)
                {
                    scrollView.verticalScrollerVisibility = sv;
                    return true;
                }
                return false;
            }
        }
    }
}
