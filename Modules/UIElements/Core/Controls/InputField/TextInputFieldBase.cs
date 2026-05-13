// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Abstract base class used for all text-based fields.
    /// </summary>
    [UxmlElement]
    public abstract partial class TextInputBaseField<TValueType> : BaseField<TValueType>, IDelayedField
    {
        internal static readonly BindingId autoCorrectionProperty = nameof(autoCorrection);
        internal static readonly BindingId hideMobileInputProperty = nameof(hideMobileInput);
        internal static readonly BindingId hideSoftKeyboardProperty = nameof(hideSoftKeyboard);
        internal static readonly BindingId hidePlaceholderOnFocusProperty = nameof(hidePlaceholderOnFocus);
        internal static readonly BindingId keyboardTypeProperty = nameof(keyboardType);
        internal static readonly BindingId isReadOnlyProperty = nameof(isReadOnly);
        internal static readonly BindingId isPasswordFieldProperty = nameof(isPasswordField);
        internal static readonly BindingId textSelectionProperty = nameof(textSelection);
        internal static readonly BindingId textEditionProperty = nameof(textEdition);
        internal static readonly BindingId placeholderTextProperty = nameof(placeholderText);
        internal static readonly BindingId cursorIndexProperty = nameof(cursorIndex);
        internal static readonly BindingId cursorPositionProperty = nameof(cursorPosition);
        internal static readonly BindingId selectIndexProperty = nameof(selectIndex);
        internal static readonly BindingId selectAllOnFocusProperty = nameof(selectAllOnFocus);
        internal static readonly BindingId selectAllOnMouseUpProperty = nameof(selectAllOnMouseUp);
        internal static readonly BindingId maxLengthProperty = nameof(maxLength);
        internal static readonly BindingId doubleClickSelectsWordProperty = nameof(doubleClickSelectsWord);
        internal static readonly BindingId tripleClickSelectsLineProperty = nameof(tripleClickSelectsLine);
        internal static readonly BindingId emojiFallbackSupportProperty = nameof(emojiFallbackSupport);
        internal static readonly BindingId isDelayedProperty = nameof(isDelayed);
        internal static readonly BindingId maskCharProperty = nameof(maskChar);
        internal static readonly BindingId verticalScrollerVisibilityProperty = nameof(verticalScrollerVisibility);

        static CustomStyleProperty<Color> s_SelectionColorProperty = new CustomStyleProperty<Color>("--unity-selection-color");
        static CustomStyleProperty<Color> s_CursorColorProperty = new CustomStyleProperty<Color>("--unity-cursor-color");
        internal const int kMaxLengthNone = -1;
        internal const char kMaskCharDefault = '*';

        #region Properties for UXML Attributes

        /// <summary>
        /// Maximum number of characters for the field.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute(obsoleteNames = new[] { "maxLength" }), Delayed]
        public int maxLength
        {
            get => textEdition.maxLength;
            set
            {
                if (textEdition.maxLength == value)
                    return;
                textEdition.maxLength = value;
                textEdition.UpdateText(ValueToString(this.value));

                NotifyPropertyChanged(maxLengthProperty);
            }
        }

        // Password field (indirectly lossy behaviour when activated via multiline)
        /// <summary>
        /// Returns true if the field is used to edit a password.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute("password")]
        public bool isPasswordField
        {
            get => textEdition.isPassword;
            set
            {
                if (textEdition.isPassword == value)
                    return;

                textEdition.isPassword = value;
                m_TextInputBase.IncrementVersion(VersionChangeType.Repaint);
                NotifyPropertyChanged(isPasswordFieldProperty);
            }
        }

        /// <summary>
        /// The character used for masking in a password field.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute("mask-character", obsoleteNames = new[] { "maskCharacter" })]
        public char maskChar
        {
            get => textEdition.maskChar;
            set
            {
                if (textEdition.maskChar == value)
                    return;
                textEdition.maskChar = value;
                NotifyPropertyChanged(maskCharProperty);
            }
        }

        /// <summary>
        /// DO NOT USE placeholderText, use textEdition.placeholder instead. This property was added so it can be picked up properly by the UI Builder.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        internal string placeholderText
        {
            get => textEdition.placeholder;
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            set
            {
                if (textEdition.placeholder == value) return;
                textEdition.placeholder = value;
                NotifyPropertyChanged(placeholderTextProperty);
            }
        }

        /// <summary>
        /// DO NOT USE hidePlaceholderOnFocus, use textEdition.hidePlaceholderOnFocus instead. This property was added so it can be picked up properly by the UI Builder.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        internal bool hidePlaceholderOnFocus
        {
            get => textEdition.hidePlaceholderOnFocus;
            set
            {
                if (textEdition.hidePlaceholderOnFocus == value) return;
                textEdition.hidePlaceholderOnFocus = value;
                NotifyPropertyChanged(hidePlaceholderOnFocusProperty);
            }
        }

        /// <summary>
        /// Returns true if the field is read only.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute("readonly")]
        public bool isReadOnly
        {
            get => textEdition.isReadOnly;
            set
            {
                if (textEdition.isReadOnly == value)
                    return;
                textEdition.isReadOnly = value;
                NotifyPropertyChanged(isReadOnlyProperty);
            }
        }

        /// <summary>
        /// If set to true, the value property isn't updated until either the user presses Enter or the text field loses focus.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public bool isDelayed
        {
            get => textEdition.isDelayed;
            set
            {
                if (textEdition.isDelayed == value)
                    return;
                textEdition.isDelayed = value;
                NotifyPropertyChanged(isDelayedProperty);
            }
        }

        /// <summary>
        /// Option for controlling the visibility of the vertical scroll bar in the <see cref="TextInputBaseField{TValueType}"/>.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public ScrollerVisibility verticalScrollerVisibility
        {
            get => textInputBase.verticalScrollerVisibility;
            set
            {
                if (textInputBase.verticalScrollerVisibility == value)
                    return;
                textInputBase.SetVerticalScrollerVisibility(value);
                NotifyPropertyChanged(verticalScrollerVisibilityProperty);
            }
        }

        /// <summary>
        /// Controls whether the element's content is selected when you mouse up for the first time.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public bool selectAllOnMouseUp
        {
            get => textSelection.selectAllOnMouseUp;
            set
            {
                if (textSelection.selectAllOnMouseUp == value)
                    return;
                textSelection.selectAllOnMouseUp = value;
                NotifyPropertyChanged(selectAllOnMouseUpProperty);
            }
        }

        /// <summary>
        /// Controls whether the element's content is selected upon receiving focus.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public bool selectAllOnFocus
        {
            get => textSelection.selectAllOnFocus;
            set
            {
                if (textSelection.selectAllOnFocus == value)
                    return;
                textSelection.selectAllOnFocus = value;
                NotifyPropertyChanged(selectAllOnFocusProperty);
            }
        }

        /// <summary>
        /// Controls whether double-clicking selects the word under the mouse pointer.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute("select-word-by-double-click")]
        public bool doubleClickSelectsWord
        {
            get => textSelection.doubleClickSelectsWord;
            set
            {
                if (textSelection.doubleClickSelectsWord == value)
                    return;
                textSelection.doubleClickSelectsWord = value;
                NotifyPropertyChanged(doubleClickSelectsWordProperty);
            }
        }

        /// <summary>
        /// Controls whether triple clicking selects the entire line under the mouse pointer or not.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute("select-line-by-triple-click")]
        public bool tripleClickSelectsLine
        {
            get => textSelection.tripleClickSelectsLine;
            set
            {
                if (textSelection.tripleClickSelectsLine == value)
                    return;
                textSelection.tripleClickSelectsLine = value;
                NotifyPropertyChanged(tripleClickSelectsLineProperty);
            }
        }

        /// <summary>
        /// Specifies the order in which the system should look for Emoji characters when rendering text.
        /// If this setting is enabled, the global Emoji Fallback list will be searched first for characters defined as
        /// Emoji in the Unicode 14.0 standard.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public bool emojiFallbackSupport
        {
            get => m_TextInputBase.textElement.emojiFallbackSupport;
            set
            {
                if (m_TextInputBase.textElement.emojiFallbackSupport == value)
                    return;

                labelElement.emojiFallbackSupport = value;
                m_TextInputBase.textElement.emojiFallbackSupport = value;
                NotifyPropertyChanged(emojiFallbackSupportProperty);
            }
        }

        /// <summary>
        /// Prevents the OS soft keyboard from opening
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public bool hideSoftKeyboard
        {
            get => textEdition.hideSoftKeyboard;
            set
            {
                if (textEdition.hideSoftKeyboard == value)
                    return;
                textEdition.hideSoftKeyboard = value;
                NotifyPropertyChanged(hideSoftKeyboardProperty);
            }
        }

        /// <summary>
        /// Hides the mobile input field shown in the OS soft keyboard.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public bool hideMobileInput
        {
            get => textEdition.hideMobileInput;
            set
            {
                if (textEdition.hideMobileInput == value)
                    return;
                textEdition.hideMobileInput = value;
                NotifyPropertyChanged(hideMobileInputProperty);
            }
        }

        /// <summary>
        /// The type of mobile keyboard that will be used.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public TouchScreenKeyboardType keyboardType
        {
            get => textEdition.keyboardType;
            set
            {
                if (textEdition.keyboardType == value)
                    return;
                textEdition.keyboardType = value;
                NotifyPropertyChanged(keyboardTypeProperty);
            }
        }

        /// <summary>
        /// Determines if the touch screen keyboard auto correction is turned on or off.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public bool autoCorrection
        {
            get => textEdition.autoCorrection;
            set
            {
                if (textEdition.autoCorrection == value)
                    return;
                textEdition.autoCorrection = value;
                NotifyPropertyChanged(autoCorrectionProperty);
            }
        }

        #endregion

        #region USS ClassNames
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-base-text-field";
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
        /// USS class name of the multiline container.
        /// </summary>
        internal static readonly string multilineContainerClassName = ussClassName + "__multiline-container";
        internal static readonly UniqueStyleString multilineContainerClassNameUnique = new(multilineContainerClassName);

        /// <summary>
        /// USS class name of single line input elements in elements of this type.
        /// </summary>
        public static readonly string singleLineInputUssClassName = inputUssClassName + "--single-line";
        internal static readonly UniqueStyleString singleLineInputUssClassNameUnique = new(singleLineInputUssClassName);

        /// <summary>
        /// USS class name of multiline input elements in elements of this type.
        /// </summary>
        public static readonly string multilineInputUssClassName = inputUssClassName + "--multiline";
        internal static readonly UniqueStyleString multilineInputUssClassNameUnique = new(multilineInputUssClassName);

        /// <summary>
        /// USS class name of input elements when placeholder text is shown
        /// </summary>
        public static readonly string placeholderUssClassName = inputUssClassName + "--placeholder";
        internal static readonly UniqueStyleString placeholderUssClassNameUnique = new(placeholderUssClassName);

        /// <summary>
        /// USS class name of multiline input elements with no scroll view.
        /// </summary>
        internal static readonly string multilineInputWithScrollViewUssClassName = multilineInputUssClassName + "--scroll-view";
        internal static readonly UniqueStyleString multilineInputWithScrollViewUssClassNameUnique = new(multilineInputWithScrollViewUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public static readonly string textInputUssName = "unity-text-input";
        internal static readonly UniqueStyleString k_TextInputUssName = new(textInputUssName);

        #endregion

        protected TextInputBaseField(int maxLength, char maskChar, TextInputBase textInputBase)
            : this(null, maxLength, maskChar, textInputBase) {}

        protected TextInputBaseField(string label, int maxLength, char maskChar, TextInputBase textInputBase)
            : base(label, textInputBase)
        {
            tabIndex = 0;
            delegatesFocus = true;
            labelElement.tabIndex = -1; // To delegate directly to text-input field

            AddToClassList(ussClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);
            visualInput.AddToClassList(singleLineInputUssClassNameUnique);

            m_TextInputBase = textInputBase;
            m_TextInputBase.textEdition.maxLength = maxLength;
            m_TextInputBase.textEdition.maskChar = maskChar;

            Callbacks.OnFieldCustomStyleResolved.Register(this);
            textInputBase.textElement.OnPlaceholderChanged += OnPlaceholderChanged;

            m_UpdateTextFromValue = true;
        }

        TextInputBase m_TextInputBase;
        internal bool m_UpdateTextFromValue;

        /// <undoc/>
        /// <summary>
        /// This is the text input visual element which presents the value in the field.
        /// </summary>
        protected internal TextInputBase textInputBase
        {
            [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
            get => m_TextInputBase;
        }

        /// <summary>
        /// Retrieves this Field's TextElement ITextSelection
        /// </summary>
        [CreateProperty(ReadOnly = true)]
        public ITextSelection textSelection => m_TextInputBase.textElement.selection;

        /// <summary>
        /// Retrieves this Field's TextElement ITextEdition
        /// </summary>
        [CreateProperty(ReadOnly = true)]
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
        /// The active touch keyboard being displayed.
        /// </summary>
        public TouchScreenKeyboard touchScreenKeyboard
        {
            get => textEdition.touchScreenKeyboard;
        }

        #endregion

        #region TextSelection
        /// <summary>
        /// Background color of selected text.
        /// </summary>
        [Obsolete("cursorColor is deprecated. Please use the corresponding USS property (--unity-cursor-color) instead.")]
        public Color selectionColor => textSelection.selectionColor;
        /// <summary>
        /// Color of the cursor.
        /// </summary>
        [Obsolete("cursorColor is deprecated. Please use the corresponding USS property (--unity-cursor-color) instead.")]
        public Color cursorColor => textSelection.cursorColor;

        /// <summary>
        /// This is the cursor index in the text presented.
        /// </summary>
        [CreateProperty]
        public int cursorIndex
        {
            get => textSelection.cursorIndex;
            set
            {
                if (textSelection.cursorIndex == value)
                    return;
                textSelection.cursorIndex = value;
                NotifyPropertyChanged(cursorIndexProperty);
            }
        }

        /// <summary>
        /// The position of the text cursor inside the element.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
        public Vector2 cursorPosition
        {
            get => textSelection.cursorPosition;
        }

        /// <summary>
        /// This is the selection index in the text presented.
        /// </summary>
        [CreateProperty]
        public int selectIndex
        {
            get => textSelection.selectIndex;
            set
            {
                if (textSelection.selectIndex == value)
                    return;
                textSelection.selectIndex = value;
                NotifyPropertyChanged(selectIndexProperty);
            }
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
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            protected internal set => m_TextInputBase.text = value;
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

        [EventInterest(typeof(NavigationSubmitEvent), typeof(FocusInEvent), typeof(FocusEvent), typeof(FocusOutEvent),
            typeof(BlurEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (textEdition.isReadOnly)
                return;

            if (evt.eventTypeId == NavigationSubmitEvent.TypeId() && evt.target != textInputBase.textElement)
            {
                textInputBase.textElement.Focus();
            }
            else if (evt.eventTypeId == NavigationMoveEvent.TypeId() && evt.target != textInputBase.textElement)
            {
                // Move focus as though the textElement was focused to begin with.
                // This allows TextField to have a similar behavior than IMGUI where if you select a TextField,
                // press Enter, then Tab to the next element, it jumps over the text content and into the next control.
                focusController.SwitchFocusOnEvent(textInputBase.textElement, evt);
            }
            else if (evt.eventTypeId == FocusInEvent.TypeId())
            {
                if (showMixedValue)
                    ((INotifyValueChanged<string>)textInputBase.textElement).SetValueWithoutNotify(default);
            }
            else if (evt.eventTypeId == FocusEvent.TypeId())
            {
                UpdatePlaceholderClassList();
            }
            else if (evt.eventTypeId == BlurEvent.TypeId())
            {
                if (showMixedValue)
                    UpdateMixedValueContent();

                UpdatePlaceholderClassList();

                // Scroll to the beginning when focus is lost (UUM-73005)
                textInputBase.UpdateScrollOffset();
            }
        }

        /// <summary>
        /// Allow to set the value without being notified.
        /// </summary>
        /// <param name="newValue">The new value to set.</param>
        public override void SetValueWithoutNotify(TValueType newValue)
        {
            base.SetValueWithoutNotify(newValue);

            // Preemptively set back the placeholder state if we're about to set to an empty string.
            if (textInputBase.textElement.needsPlaceholderIfTextIsEmpty && string.IsNullOrEmpty(ValueToString(newValue)))
            {
                visualInput.AddToClassList(placeholderUssClassNameUnique);
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

        private protected override bool canSwitchToMixedValue => !textInputBase.textElement.hasFocus;

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                if (m_UpdateTextFromValue)
                {
                    ((INotifyValueChanged<string>)textInputBase.textElement).SetValueWithoutNotify(mixedValueString);
                }

                AddToClassList(mixedValueLabelUssClassNameUnique);
                visualInput?.AddToClassList(mixedValueLabelUssClassNameUnique);
            }
            else
            {
                UpdateTextFromValue();
                visualInput?.RemoveFromClassList(mixedValueLabelUssClassNameUnique);
                RemoveFromClassList(mixedValueLabelUssClassNameUnique);
            }
        }

        internal bool hasFocus
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.GraphToolkitModule", "UnityEditor.UIToolkitAuthoringModule")]
            get => textInputBase.textElement.hasFocus;
        }

        internal void OnPlaceholderChanged()
        {
            Callbacks.OnChangeEventUpdatePlaceholderClassList.Unregister(this);
            if (!string.IsNullOrEmpty(textEdition.placeholder))
                Callbacks.OnChangeEventUpdatePlaceholderClassList.Register(this);

            UpdatePlaceholderClassList();
        }

        internal void UpdatePlaceholderClassList(ChangeEvent<TValueType> evt = null)
        {
            if (textInputBase.textElement.showPlaceholderText)
                visualInput.AddToClassList(placeholderUssClassNameUnique);
            else
                visualInput.RemoveFromClassList(placeholderUssClassNameUnique);
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

        private static class Callbacks
        {
            public static readonly EventCallbackDefinition<TextInputBaseField<TValueType>> OnFieldCustomStyleResolved =
                EventCallback.Create<CustomStyleResolvedEvent, TextInputBaseField<TValueType>>(static (e, self) =>
                    self.OnFieldCustomStyleResolved(e));
            public static readonly EventCallbackDefinition<TextInputBaseField<TValueType>> OnChangeEventUpdatePlaceholderClassList =
                EventCallback.Create<ChangeEvent<TValueType>, TextInputBaseField<TValueType>>(static (e, self) =>
                    self.UpdatePlaceholderClassList(e));
        }

        /// <undoc/>
        /// <summary>
        /// This is the input text base class visual representation.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        protected internal abstract class TextInputBase : VisualElement
        {
            internal TextElement textElement
            {
                [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
                get;
                private set;
            }
            internal ScrollView scrollView;
            internal VisualElement multilineContainer;

            #region USS ClassNames
            /// <summary>
            /// Modifier name of the inner components
            /// </summary>
            public static readonly string innerComponentsModifierName = "--inner-input-field-component";
            internal static readonly UniqueStyleString k_InnerComponentsModifierName = new(innerComponentsModifierName);

            /// <summary>
            /// USS class name of the inner TextElement
            /// </summary>
            public static readonly string innerTextElementUssClassName = TextElement.ussClassName + innerComponentsModifierName;
            internal static readonly UniqueStyleString innerTextElementUssClassNameUnique = new(innerTextElementUssClassName);

            /// <summary>
            /// USS class name of the inner TextElement
            /// </summary>
            internal static readonly string innerTextElementWithScrollViewUssClassName = TextElement.ussClassName + innerComponentsModifierName + "--scroll-view";
            internal static readonly UniqueStyleString innerTextElementWithScrollViewUssClassNameUnique = new(innerTextElementWithScrollViewUssClassName);

            /// <summary>
            /// USS class name that's added when the inner TextElement if in horizontal.
            /// </summary>
            public static readonly string horizontalVariantInnerTextElementUssClassName = TextElement.ussClassName + innerComponentsModifierName + "--horizontal";
            internal static readonly UniqueStyleString horizontalVariantInnerTextElementUssClassNameUnique = new(horizontalVariantInnerTextElementUssClassName);

            /// <summary>
            /// USS class name that's added when the inner TextElement if in vertical mode.
            /// </summary>
            public static readonly string verticalVariantInnerTextElementUssClassName = TextElement.ussClassName + innerComponentsModifierName + "--vertical";
            internal static readonly UniqueStyleString verticalVariantInnerTextElementUssClassNameUnique = new(verticalVariantInnerTextElementUssClassName);

            /// <summary>
            /// USS class name that's added when the inner TextElement if in both horizontal and vertical mode.
            /// </summary>
            public static readonly string verticalHorizontalVariantInnerTextElementUssClassName = TextElement.ussClassName + innerComponentsModifierName + "--vertical-horizontal";
            internal static readonly UniqueStyleString verticalHorizontalVariantInnerTextElementUssClassNameUnique = new(verticalHorizontalVariantInnerTextElementUssClassName);

            /// <summary>
            /// USS class name of the inner ScrollView
            /// </summary>
            public static readonly string innerScrollviewUssClassName = ScrollView.ussClassName + innerComponentsModifierName;
            internal static readonly UniqueStyleString innerScrollviewUssClassNameUnique = new(innerScrollviewUssClassName);

            /// <summary>
            /// USS class name of the inner ContentContainer
            /// </summary>
            public static readonly string innerViewportUssClassName = ScrollView.viewportUssClassName + innerComponentsModifierName;
            internal static readonly UniqueStyleString innerViewportUssClassNameUnique = new(innerViewportUssClassName);

            /// <summary>
            /// USS class name of the inner ContentContainer
            /// </summary>
            public static readonly string innerContentContainerUssClassName = ScrollView.contentUssClassName + innerComponentsModifierName;
            internal static readonly UniqueStyleString innerContentContainerUssClassNameUnique = new(innerContentContainerUssClassName);

            #endregion

            internal TextInputBase()
            {
                delegatesFocus = true;

                textElement = new TextElement();
                textElement.isInputField = true;
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

                AddToClassList(inputUssClassNameUnique);
                name = TextField.textInputUssName;

                SetSingleLine();

                Callbacks.OnInputCustomStyleResolved.Register(this);

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
            /// The initial value of the input field before being edited.
            /// </summary>
            internal string originalText => textElement.originalText;

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
                focusController.SwitchFocus(parentTextField);
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
                AddToClassList(singleLineInputUssClassNameUnique);
                textElement.AddToClassList(innerTextElementUssClassNameUnique);

                Callbacks.OnTextElementGeometryChangedEvent.Register(textElement);

                // Make sure we reinitialize the vertical scrollOffset but keep the horizontal scrollOffset.
                if (scrollOffset != Vector2.zero)
                {
                   scrollOffset.y = 0;
                   UpdateScrollOffset();
                }
                if (textElement.hasFocus)
                {
                    textElement.uitkTextHandle.AddToPermanentCacheAndGenerateMesh();
                    textElement.editingManipulator?.editingUtilities.SyncStateToNative();
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

                    scrollView.AddToClassList(innerScrollviewUssClassNameUnique);
                    scrollView.contentViewport.AddToClassList(innerViewportUssClassNameUnique);
                    scrollView.contentContainer.AddToClassList(innerContentContainerUssClassNameUnique);
                    Callbacks.OnScrollViewGeometryChangedEvent.Register(scrollView.contentContainer);

                    // The ScrollView's slider can send ChangeEvent<float>. This makes sure these do not leak.
                    Callbacks.OnScrollViewSliderValueChangedMakeSureScrollViewDoesNotLeakEvents.Register(scrollView.verticalScroller.slider);
                    // We want to make sure the TextElement doesn't loose focus when users are using the slider.
                    scrollView.verticalScroller.slider.focusable = false;

                    Callbacks.OnScrollViewSliderValueChangedMakeSureScrollViewDoesNotLeakEvents.Register(scrollView.horizontalScroller.slider);
                    scrollView.horizontalScroller.slider.focusable = false;

                    AddToClassList(multilineInputWithScrollViewUssClassNameUnique);
                    textElement.AddToClassList(innerTextElementWithScrollViewUssClassNameUnique);
                }
                else if (multilineContainer == null)
                {
                    Callbacks.OnTextElementGeometryChangedEvent.Register(textElement);
                    multilineContainer = new VisualElement().WithClassList(multilineContainerClassName);

                    multilineContainer.Add(textElement);
                    Add(multilineContainer);
                    SetMultilineContainerStyle();

                    AddToClassList(multilineInputUssClassNameUnique);
                    textElement.AddToClassList(innerTextElementUssClassNameUnique);
                }
                if (textElement.hasFocus)
                {
                    textElement.uitkTextHandle.AddToPermanentCacheAndGenerateMesh();
                    textElement.editingManipulator?.editingUtilities.SyncStateToNative();
                }
            }

            void ScrollViewOnGeometryChangedEvent(GeometryChangedEvent e)
            {
                if (e.oldRect.size == e.newRect.size)
                    return;
                UpdateScrollOffset();
            }

            void TextElementOnGeometryChangedEvent(GeometryChangedEvent e)
            {
                if (e.oldRect.size == e.newRect.size)
                    return;

                var widthChanged = Math.Abs(e.oldRect.size.x - e.newRect.size.x) > UIRUtility.k_Epsilon;

                UpdateScrollOffset(isBackspace: false, widthChanged);
            }

            internal void OnInputCustomStyleResolved(CustomStyleResolvedEvent e)
            {
                // These don't quite follow the inline style behavior
                // (aka setting the value via code should always overrides the one from styleSheets)
                ICustomStyle customStyle = e.customStyle;
                if (customStyle.TryGetValue(s_SelectionColorProperty, out Color selectionValue))
                    textElement.selectionColor = selectionValue;

                if (customStyle.TryGetValue(s_CursorColorProperty, out Color cursorValue))
                    textElement.cursorColor = cursorValue;

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

            internal void UpdateScrollOffset(bool isBackspace = false)
            {
                UpdateScrollOffset(isBackspace, widthChanged: false);
            }

            // scrollOffset is used in automated tests
            internal Vector2 scrollOffset = Vector2.zero;
            bool m_ScrollViewWasClamped;
            internal void UpdateScrollOffset(bool isBackspace, bool widthChanged)
            {
                var selection = textSelection;
                if (selection.cursorIndex < 0 || (selection.cursorIndex <= 0 && selection.selectIndex <= 0 && scrollOffset == Vector2.zero))
                    return;

                if (scrollView != null)
                {
                    scrollOffset = GetScrollOffset(scrollView.scrollOffset.x, scrollView.scrollOffset.y, scrollView.contentViewport.layout.width, isBackspace, widthChanged);
                    scrollView.scrollOffset = scrollOffset;

                    m_ScrollViewWasClamped = scrollOffset.x > scrollView.scrollOffset.x || scrollOffset.y > scrollView.scrollOffset.y;
                }
                else
                {
                    var t = textElement.resolvedStyle.translate;

                    scrollOffset = GetScrollOffset(scrollOffset.x, scrollOffset.y, contentRect.width, isBackspace, widthChanged);

                    t.y = -Mathf.Min(scrollOffset.y, Math.Abs(textElement.contentRect.height - contentRect.height));
                    t.x = -scrollOffset.x;

                    if (!t.Equals(textElement.resolvedStyle.translate))
                        textElement.style.translate = t;
                }
            }

            Vector2 lastCursorPos = Vector2.zero;
            Vector2 GetScrollOffset(float xOffset, float yOffset, float contentViewportWidth, bool isBackspace, bool widthChanged)
            {
                // Scroll to the beginning when focus is lost (UUM-73005)
                if (!textElement.hasFocus)
                    return Vector2.zero;

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

                if (Math.Abs(lastCursorPos.x - cursorPos.x) > epsilon || m_ScrollViewWasClamped || widthChanged)
                {
                    // Update scrollOffset when cursor moves right or when the offset is not needed anymore.
                    if (cursorPos.x > xOffset + contentViewportWidth - cursorWidth
                        || xOffset > 0 && widthChanged)
                    {
                        var roundedValue = Mathf.Ceil(cursorPos.x + cursorWidth - contentViewportWidth);
                        newXOffset = Mathf.Max(roundedValue, 0);
                    }
                    // Update scrollOffset when cursor moves left.
                    else if (cursorPos.x < xOffset + leftScrollOffsetPadding)
                    {
                        newXOffset = Mathf.Max(cursorPos.x - leftScrollOffsetPadding, 0);
                    }
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

                if (Math.Abs(xOffset - newXOffset) > epsilon || Math.Abs(yOffset - newYOffset) > epsilon)
                {
                    return new Vector2(newXOffset, newYOffset);
                }

                return scrollView != null ? scrollView.scrollOffset : scrollOffset;
            }

            internal void SetScrollViewMode()
            {
                if (scrollView == null)
                    return;

                textElement.RemoveFromClassList(verticalVariantInnerTextElementUssClassNameUnique);
                textElement.RemoveFromClassList(verticalHorizontalVariantInnerTextElementUssClassNameUnique);
                textElement.RemoveFromClassList(horizontalVariantInnerTextElementUssClassNameUnique);

                if (textEdition.multiline && (computedStyle.whiteSpace == WhiteSpace.Normal || computedStyle.whiteSpace == WhiteSpace.PreWrap))
                {
                    textElement.AddToClassList(verticalVariantInnerTextElementUssClassNameUnique);
                    scrollView.mode = ScrollViewMode.Vertical;
                }
                else if (textEdition.multiline)
                {
                    textElement.AddToClassList(verticalHorizontalVariantInnerTextElementUssClassNameUnique);
                    scrollView.mode = ScrollViewMode.VerticalAndHorizontal;
                }
                else
                {
                    textElement.AddToClassList(horizontalVariantInnerTextElementUssClassNameUnique);
                    scrollView.mode = ScrollViewMode.Horizontal;
                }
            }

            void SetMultilineContainerStyle()
            {
                if (multilineContainer != null)
                {
                    if (computedStyle.whiteSpace == WhiteSpace.Normal || computedStyle.whiteSpace == WhiteSpace.PreWrap)
                    {
                        style.overflow = Overflow.Hidden;
                        multilineContainer.style.alignSelf = Align.Auto;
                    }
                    else
                        style.overflow = (Overflow)OverflowInternal.Scroll;
                }
            }

            void RemoveSingleLineComponents()
            {
                RemoveFromClassList(singleLineInputUssClassNameUnique);
                textElement.RemoveFromClassList(innerTextElementUssClassNameUnique);
                textElement.RemoveFromHierarchy();
                Callbacks.OnTextElementGeometryChangedEvent.Unregister(textElement);
            }

            void RemoveMultilineComponents()
            {
                if (scrollView != null)
                {
                    scrollView.RemoveFromHierarchy();
                    Callbacks.OnScrollViewSliderValueChangedMakeSureScrollViewDoesNotLeakEvents.Unregister(scrollView.horizontalScroller.slider);
                    Callbacks.OnScrollViewSliderValueChangedMakeSureScrollViewDoesNotLeakEvents.Unregister(scrollView.verticalScroller.slider);
                    Callbacks.OnScrollViewGeometryChangedEvent.Unregister(scrollView.contentContainer);
                    scrollView = null;

                    textElement.RemoveFromClassList(verticalVariantInnerTextElementUssClassNameUnique);
                    textElement.RemoveFromClassList(verticalHorizontalVariantInnerTextElementUssClassNameUnique);
                    textElement.RemoveFromClassList(horizontalVariantInnerTextElementUssClassNameUnique);

                    RemoveFromClassList(multilineInputWithScrollViewUssClassNameUnique);
                    textElement.RemoveFromClassList(innerTextElementWithScrollViewUssClassNameUnique);
                }

                if (multilineContainer != null)
                {
                    // Make sure we reset the transform
                    textElement.style.translate = Vector3.zero;

                    multilineContainer.RemoveFromHierarchy();
                    Callbacks.OnTextElementGeometryChangedEvent.Unregister(textElement);
                    multilineContainer = null;

                    RemoveFromClassList(multilineInputUssClassNameUnique);
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

            private static class Callbacks
            {
                // Use with ?. syntax to avoid possible exceptions on events during DetachFromPanel
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static TextInputBase GetTextInputBase(VisualElement child) =>
                    child.GetFirstAncestorOfType<TextInputBase>();

                public static readonly EventCallbackDefinition<TextInputBase> OnInputCustomStyleResolved =
                    EventCallback.Create<CustomStyleResolvedEvent, TextInputBase>(static (e, self) =>
                        self.OnInputCustomStyleResolved(e));

                public static readonly EventCallbackDefinition<TextElement> OnTextElementGeometryChangedEvent =
                    EventCallback.Create<GeometryChangedEvent, TextElement>(static (e, textElement) =>
                        GetTextInputBase(textElement)?.TextElementOnGeometryChangedEvent(e));

                public static readonly EventCallbackDefinition<VisualElement> OnScrollViewGeometryChangedEvent =
                        EventCallback.Create<GeometryChangedEvent, VisualElement>(static (e, scrollView) =>
                            GetTextInputBase(scrollView)?.ScrollViewOnGeometryChangedEvent(e));

                public static readonly EventCallbackDefinition<Slider>
                    OnScrollViewSliderValueChangedMakeSureScrollViewDoesNotLeakEvents =
                        EventCallback.Create<ChangeEvent<float>, Slider>(static (e, slider) =>
                            GetTextInputBase(slider)?.MakeSureScrollViewDoesNotLeakEvents(e));
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
            /// Controls whether double-clicking selects the word under the mouse pointer.
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
