// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    internal interface ITextInputField : IEventHandler, ITextElement
    {
        bool hasFocus { get; }

        bool doubleClickSelectsWord { get; }
        bool tripleClickSelectsLine { get; }

        bool isReadOnly { get; }

        bool isDelayed { get; }

        bool isPasswordField { get; }

        TextEditorEngine editorEngine { get; }

        void SyncTextEngine();
        bool AcceptCharacter(char c);
        string CullString(string s);
        void UpdateText(string value);
        void UpdateValueFromText();
    }

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
                field.maxLength = m_MaxLength.GetValueFromBag(bag, cc);
                field.isPasswordField = m_Password.GetValueFromBag(bag, cc);
                field.isReadOnly = m_IsReadOnly.GetValueFromBag(bag, cc);
                field.isDelayed = m_IsDelayed.GetValueFromBag(bag, cc);
                string maskCharacter = m_MaskCharacter.GetValueFromBag(bag, cc);
                if (!string.IsNullOrEmpty(maskCharacter))
                {
                    field.maskChar = maskCharacter[0];
                }
                field.text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        TextInputBase m_TextInputBase;
        /// <summary>
        /// This is the text input visual element which presents the value in the field.
        /// </summary>
        protected TextInputBase textInputBase => m_TextInputBase;

        internal const int kMaxLengthNone = -1;
        internal const char kMaskCharDefault = '*';

        /// <summary>
        /// DO NOT USE textHandle. This field is only there for backward compatibility reason and will soon be stripped.
        /// </summary>
        internal TextHandle textHandle
        {
            get
            {
                return new TextHandle() {textHandle = iTextHandle};
            }
        }

        internal ITextHandle iTextHandle { get; private set; }

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
            get { return m_TextInputBase.text; }
            protected set
            {
                m_TextInputBase.text = value;
            }
        }

        /// <summary>
        /// Returns true if the field is read only.
        /// </summary>
        public bool isReadOnly
        {
            get { return m_TextInputBase.isReadOnly; }
            set { m_TextInputBase.isReadOnly = value; }
        }

        // Password field (indirectly lossy behaviour when activated via multiline)
        /// <summary>
        /// Returns true if the field is used to edit a password.
        /// </summary>
        public bool isPasswordField
        {
            get { return m_TextInputBase.isPasswordField; }
            set
            {
                if (m_TextInputBase.isPasswordField == value)
                    return;

                m_TextInputBase.isPasswordField = value;
                m_TextInputBase.IncrementVersion(VersionChangeType.Repaint);
            }
        }

        /// <summary>
        /// Background color of selected text.
        /// </summary>
        public Color selectionColor => m_TextInputBase.selectionColor;
        /// <summary>
        /// Color of the cursor.
        /// </summary>
        public Color cursorColor => m_TextInputBase.cursorColor;


        /// <summary>
        /// The current cursor position index in the text input field.
        /// </summary>
        public int cursorIndex => m_TextInputBase.cursorIndex;
        /// <summary>
        /// The current selection position index in the text input field.
        /// </summary>
        public int selectIndex => m_TextInputBase.selectIndex;
        /// <summary>
        /// Maximum number of characters for the field.
        /// </summary>
        public int maxLength
        {
            get { return m_TextInputBase.maxLength; }
            set { m_TextInputBase.maxLength = value; }
        }

        /// <summary>
        /// Controls whether double clicking selects the word under the mouse pointer or not.
        /// </summary>
        public bool doubleClickSelectsWord
        {
            get { return m_TextInputBase.doubleClickSelectsWord; }
            set { m_TextInputBase.doubleClickSelectsWord = value; }
        }
        /// <summary>
        /// Controls whether triple clicking selects the entire line under the mouse pointer or not.
        /// </summary>
        public bool tripleClickSelectsLine
        {
            get { return m_TextInputBase.tripleClickSelectsLine; }
            set { m_TextInputBase.tripleClickSelectsLine = value; }
        }

        /// <summary>
        /// If set to true, the value property isn't updated until either the user presses Enter or the text field loses focus.
        /// </summary>
        public bool isDelayed
        {
            get { return m_TextInputBase.isDelayed; }
            set { m_TextInputBase.isDelayed = value; }
        }

        /// <summary>
        /// The character used for masking in a password field.
        /// </summary>
        public char maskChar
        {
            get { return m_TextInputBase.maskChar; }
            set { m_TextInputBase.maskChar = value; }
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
            return TextUtilities.MeasureVisualElementTextSize(this, textToMeasure, width, widthMode, height, heightMode, iTextHandle);
        }

        /* internal for VisualTree tests */
        internal TextEditorEventHandler editorEventHandler => m_TextInputBase.editorEventHandler;

        /* internal for VisualTree tests */
        internal TextEditorEngine editorEngine  => m_TextInputBase.editorEngine;

        internal bool hasFocus => m_TextInputBase.hasFocus;

        /// <summary>
        /// Converts a value of the specified generic type from the subclass to a string representation.
        /// </summary>
        /// <remarks>Subclasses must implement this method.</remarks>
        /// <param name="value">The value to convert.</param>
        /// <returns>A string representing the value.</returns>
        protected virtual string ValueToString(TValueType value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts a string to the value of the specified generic type from the subclass.
        /// </summary>
        /// <remarks>Subclasses must implement this method.</remarks>
        /// <param name="str">The string to convert.</param>
        /// <returns>A value converted from the string.</returns>
        protected virtual TValueType StringToValue(string str)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Selects all the text.
        /// </summary>
        public void SelectAll()
        {
            m_TextInputBase.SelectAll();
        }

        internal void SyncTextEngine()
        {
            m_TextInputBase.SyncTextEngine();
        }

        internal void DrawWithTextSelectionAndCursor(MeshGenerationContext mgc, string newText)
        {
            m_TextInputBase.DrawWithTextSelectionAndCursor(mgc, newText, scaledPixelsPerPoint);
        }

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

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<CustomStyleResolvedEvent>(OnFieldCustomStyleResolved);
        }

        private void OnAttachToPanel(AttachToPanelEvent e)
        {
            iTextHandle = e.destinationPanel.contextType == ContextType.Editor
                ? TextNativeHandle.New()
                : TextCoreHandle.New();
        }

        private void OnFieldCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            m_TextInputBase.OnInputCustomStyleResolved(e);
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt == null)
            {
                return;
            }

            if (evt.eventTypeId == KeyDownEvent.TypeId())
            {
                KeyDownEvent keyDownEvt = evt as KeyDownEvent;

                // We must handle the ETX (char 3) or the \n instead of the KeypadEnter or Return because the focus will
                //     have the drawback of having the second event to be handled by the focused field.
                if ((keyDownEvt?.character == 3) ||     // KeyCode.KeypadEnter
                    (keyDownEvt?.character == '\n'))    // KeyCode.Return
                {
                    visualInput?.Focus();
                }
            }
            // The following code is to help achieve the following behaviour:
            // On IMGUI, on any text input field in focused-non-edit-mode, doing a TAB will allow the user to get to the next control...
            // To mimic that behaviour in UIE, when in focused-non-edit-mode, we have to make sure the input is not "tabbable".
            //     So, each time, either the main TextField or the Label is receiving the focus, we remove the tabIndex on
            //     the input, and we put it back when the BlurEvent is received.
            else if (evt.eventTypeId == FocusInEvent.TypeId())
            {
                if (evt.leafTarget == this || evt.leafTarget == labelElement)
                {
                    m_VisualInputTabIndex = visualInput.tabIndex;
                    visualInput.tabIndex = -1;
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
                    visualInput.tabIndex = m_VisualInputTabIndex;
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
                    m_TextInputBase.ResetValueAndText();

                if (evt.leafTarget == this || evt.leafTarget == labelElement)
                {
                    m_VisualInputTabIndex = visualInput.tabIndex;
                    visualInput.tabIndex = -1;
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
                    visualInput.tabIndex = m_VisualInputTabIndex;
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
        protected abstract class TextInputBase : VisualElement, ITextInputField
        {
            string m_OriginalText;

            /// <summary>
            /// Resets the text contained in the field.
            /// </summary>
            public void ResetValueAndText()
            {
                m_OriginalText = text = default(string);
            }

            void SaveValueAndText()
            {
                // When getting the FocusIn, we must keep the value in case of Escape...
                m_OriginalText = text;
            }

            void RestoreValueAndText()
            {
                text = m_OriginalText;
            }

            /// <summary>
            /// Selects all the text contained in the field.
            /// </summary>
            public void SelectAll()
            {
                editorEngine?.SelectAll();
            }

            internal void SelectNone()
            {
                editorEngine?.SelectNone();
            }

            private void UpdateText(string value)
            {
                if (text != value)
                {
                    // Setting the VisualElement text here cause a repaint since it dirty the layout flag.
                    using (InputEvent evt = InputEvent.GetPooled(text, value))
                    {
                        evt.target = parent;
                        text = value;
                        parent?.SendEvent(evt);
                    }
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
                var newValue = StringToValue(text);
                TextInputBaseField<TValueType> parentTextField = (TextInputBaseField<TValueType>)parent;
                parentTextField.value = newValue;
            }

            /// <summary>
            /// This is the cursor index in the text presented.
            /// </summary>
            public int cursorIndex
            {
                get { return editorEngine.cursorIndex; }
            }

            /// <summary>
            /// This is the selection index in the text presented.
            /// </summary>
            public int selectIndex
            {
                get { return editorEngine.selectIndex; }
            }

            // For input purposes, the field does not accept modification whether it's set as read only or is not enabled in the hierarchy.
            bool ITextInputField.isReadOnly => isReadOnly || !enabledInHierarchy;

            /// <summary>
            /// Returns true if the field is read only.
            /// </summary>
            public bool isReadOnly { get; set; }
            /// <summary>
            /// Maximum number of characters for the field.
            /// </summary>
            public int maxLength { get; set; }
            /// <summary>
            /// The character used for masking in a password field.
            /// </summary>
            public char maskChar { get; set; }

            /// <summary>
            /// Returns true if the field is used to edit a password.
            /// </summary>
            public virtual bool isPasswordField { get; set; }

            /// <summary>
            /// Indicates if a double click selects or not a word.
            /// </summary>
            public bool doubleClickSelectsWord { get; set; }
            /// <summary>
            /// Indicates if a double click selects or not a line.
            /// </summary>
            public bool tripleClickSelectsLine { get; set; }
            internal bool isDelayed { get; set; }

            internal bool isDragging { get; set; }

            bool touchScreenTextField
            {
                get { return TouchScreenKeyboard.isSupported && !TouchScreenKeyboard.isInPlaceEditingAllowed; }
            }


            Color m_SelectionColor = Color.clear;
            Color m_CursorColor = Color.grey;

            /// <summary>
            /// Background color of selected text.
            /// </summary>
            public Color selectionColor => m_SelectionColor;
            /// <summary>
            /// Color of the cursor.
            /// </summary>
            public Color cursorColor => m_CursorColor;


            internal bool hasFocus
            {
                get { return elementPanel != null && elementPanel.focusController.GetLeafFocusedElement() == this; }
            }

            /* internal for VisualTree tests */
            internal TextEditorEventHandler editorEventHandler { get; private set; }

            /* internal for VisualTree tests */
            internal TextEditorEngine editorEngine { get; private set; }

            private ITextHandle m_TextHandle;

            private string m_Text;

            /// <summary>
            /// The value of the input field.
            /// </summary>
            public string text
            {
                get { return m_Text; }
                set
                {
                    if (m_Text == value)
                        return;

                    m_Text = value;
                    editorEngine.text = value;
                    IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }

            internal TextInputBase()
            {
                isReadOnly = false;
                focusable = true;

                AddToClassList(inputUssClassName);
                AddToClassList(singleLineInputUssClassName);
                m_Text = string.Empty;
                name = TextField.textInputUssName;

                requireMeasureFunction = true;

                editorEngine = new TextEditorEngine(OnDetectFocusChange, OnCursorIndexChange);
                editorEngine.style.richText = false;

                if (touchScreenTextField)
                {
                    editorEventHandler = new TouchScreenTextEditorEventHandler(editorEngine, this);
                }
                else
                {
                    // TODO: Default values should come from GUI.skin.settings
                    doubleClickSelectsWord = true;
                    tripleClickSelectsLine = true;

                    editorEventHandler = new KeyboardTextEditorEventHandler(editorEngine, this);
                }

                // Make the editor style unique across all textfields
                editorEngine.style = new GUIStyle(editorEngine.style);

                RegisterCallback<CustomStyleResolvedEvent>(OnInputCustomStyleResolved);
                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                generateVisualContent += OnGenerateVisualContent;
            }

            DropdownMenuAction.Status CutCopyActionStatus(DropdownMenuAction a)
            {
                return (editorEngine.hasSelection && !isPasswordField) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
            }

            DropdownMenuAction.Status PasteActionStatus(DropdownMenuAction a)
            {
                return (editorEngine.CanPaste() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            void ProcessMenuCommand(string command)
            {
                using (ExecuteCommandEvent evt = ExecuteCommandEvent.GetPooled(command))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
            }

            void Cut(DropdownMenuAction a)
            {
                ProcessMenuCommand(EventCommandNames.Cut);
            }

            void Copy(DropdownMenuAction a)
            {
                ProcessMenuCommand(EventCommandNames.Copy);
            }

            void Paste(DropdownMenuAction a)
            {
                ProcessMenuCommand(EventCommandNames.Paste);
            }

            internal void OnInputCustomStyleResolved(CustomStyleResolvedEvent e)
            {
                Color selectionValue = Color.clear;
                Color cursorValue = Color.clear;

                ICustomStyle customStyle = e.customStyle;
                if (customStyle.TryGetValue(s_SelectionColorProperty, out selectionValue))
                    m_SelectionColor = selectionValue;

                if (customStyle.TryGetValue(s_CursorColorProperty, out cursorValue))
                    m_CursorColor = cursorValue;

                SyncGUIStyle(this, editorEngine.style);
            }

            private void OnAttachToPanel(AttachToPanelEvent e)
            {
                m_TextHandle = e.destinationPanel.contextType == ContextType.Editor
                    ? TextNativeHandle.New()
                    : TextCoreHandle.New();
            }

            internal virtual void SyncTextEngine()
            {
                editorEngine.text = CullString(text);

                editorEngine.SaveBackup();

                editorEngine.position = layout;

                editorEngine.DetectFocusChange();
            }

            internal string CullString(string s)
            {
                if (maxLength >= 0 && s != null && s.Length > maxLength)
                    return s.Substring(0, maxLength);
                return s;
            }

            internal void OnGenerateVisualContent(MeshGenerationContext mgc)
            {
                string drawText = text;
                if (isPasswordField)
                {
                    drawText = "".PadRight(text.Length, maskChar);
                }

                if (touchScreenTextField)
                {
                    var touchScreenEditor = editorEventHandler as TouchScreenTextEditorEventHandler;
                    if (touchScreenEditor != null)
                    {
                        mgc.Text(MeshGenerationContextUtils.TextParams.MakeStyleBased(this, drawText), m_TextHandle, scaledPixelsPerPoint);
                    }
                }
                else
                {
                    if (!hasFocus)
                    {
                        mgc.Text(MeshGenerationContextUtils.TextParams.MakeStyleBased(this, drawText), m_TextHandle, scaledPixelsPerPoint);
                    }
                    else
                    {
                        DrawWithTextSelectionAndCursor(mgc, drawText, scaledPixelsPerPoint);
                    }
                }
            }

            internal void DrawWithTextSelectionAndCursor(MeshGenerationContext mgc, string newText, float pixelsPerPoint)
            {
                var playmodeTintColor = panel.contextType == ContextType.Editor
                    ? UIElementsUtility.editorPlayModeTintColor
                    : Color.white;

                var keyboardTextEditor = editorEventHandler as KeyboardTextEditorEventHandler;
                if (keyboardTextEditor == null)
                    return;

                keyboardTextEditor.PreDrawCursor(newText);

                int cursorIndex = editorEngine.cursorIndex;
                int selectIndex = editorEngine.selectIndex;
                var scrollOffset = editorEngine.scrollOffset;

                float textScaling = TextUtilities.ComputeTextScaling(worldTransform, pixelsPerPoint);

                var textParams = MeshGenerationContextUtils.TextParams.MakeStyleBased(this, " ");
                float lineHeight = m_TextHandle.GetLineHeight(0, textParams, textScaling, pixelsPerPoint);

                float wordWrapWidth = 0.0f;

                // Make sure to take into account the word wrap style...
                if (editorEngine.multiline && (resolvedStyle.whiteSpace == WhiteSpace.Normal))
                {
                    wordWrapWidth = contentRect.width;
                }

                Vector2 pos = editorEngine.graphicalCursorPos - scrollOffset;
                pos.y += lineHeight;
                GUIUtility.compositionCursorPos = this.LocalToWorld(pos);

                int selectionEndIndex = string.IsNullOrEmpty(GUIUtility.compositionString)
                    ? selectIndex
                    : cursorIndex + GUIUtility.compositionString.Length;

                CursorPositionStylePainterParameters cursorParams;

                // Draw highlighted section, if any
                if ((cursorIndex != selectionEndIndex) && !isDragging)
                {
                    int min = cursorIndex < selectionEndIndex ? cursorIndex : selectionEndIndex;
                    int max = cursorIndex > selectionEndIndex ? cursorIndex : selectionEndIndex;

                    cursorParams = CursorPositionStylePainterParameters.GetDefault(this, text);
                    cursorParams.text = editorEngine.text;
                    cursorParams.wordWrapWidth = wordWrapWidth;
                    cursorParams.cursorIndex = min;

                    Vector2 minPos = m_TextHandle.GetCursorPosition(cursorParams, textScaling);

                    cursorParams.cursorIndex = max;
                    Vector2 maxPos = m_TextHandle.GetCursorPosition(cursorParams, textScaling);

                    minPos -= scrollOffset;
                    maxPos -= scrollOffset;

                    lineHeight = m_TextHandle.GetLineHeight(cursorIndex, textParams, textScaling, pixelsPerPoint);

                    if (Mathf.Approximately(minPos.y, maxPos.y))
                    {
                        mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams()
                        {
                            rect = new Rect(minPos.x, minPos.y, maxPos.x - minPos.x, lineHeight),
                            color = selectionColor,
                            playmodeTintColor = playmodeTintColor
                        });
                    }
                    else
                    {
                        // Draw first line
                        mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams()
                        {
                            rect = new Rect(minPos.x, minPos.y, contentRect.xMax - minPos.x, lineHeight),
                            color = selectionColor,
                            playmodeTintColor = playmodeTintColor
                        });

                        var inbetweenHeight = (maxPos.y - minPos.y) - lineHeight;
                        if (inbetweenHeight > 0f)
                        {
                            // Draw all lines in-between
                            mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams()
                            {
                                rect = new Rect(contentRect.xMin, minPos.y + lineHeight, contentRect.width, inbetweenHeight),
                                color = selectionColor,
                                playmodeTintColor = playmodeTintColor
                            });
                        }

                        // Draw last line if not empty
                        if (maxPos.x != contentRect.x)
                        {
                            mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams()
                            {
                                rect = new Rect(contentRect.xMin, maxPos.y, maxPos.x, lineHeight),
                                color = selectionColor,
                                playmodeTintColor = playmodeTintColor
                            });
                        }
                    }
                }

                // Draw the text with the scroll offset
                if (!string.IsNullOrEmpty(editorEngine.text) && contentRect.width > 0.0f && contentRect.height > 0.0f)
                {
                    textParams.rect = new Rect(contentRect.x - scrollOffset.x, contentRect.y - scrollOffset.y, contentRect.width + scrollOffset.x, contentRect.height + scrollOffset.y);
                    textParams.text = editorEngine.text;

                    mgc.Text(textParams, m_TextHandle, scaledPixelsPerPoint);
                }

                // Draw the cursor
                if (!isReadOnly && !isDragging)
                {
                    if (cursorIndex == selectionEndIndex && TextUtilities.IsFontAssigned(this))
                    {
                        cursorParams = CursorPositionStylePainterParameters.GetDefault(this, text);
                        cursorParams.text = editorEngine.text;
                        cursorParams.wordWrapWidth = wordWrapWidth;
                        cursorParams.cursorIndex = cursorIndex;

                        Vector2 cursorPosition = m_TextHandle.GetCursorPosition(cursorParams, textScaling);
                        cursorPosition -= scrollOffset;
                        mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams
                        {
                            rect = new Rect(cursorPosition.x, cursorPosition.y, 1f, lineHeight),
                            color = cursorColor,
                            playmodeTintColor = playmodeTintColor
                        });
                    }

                    // Draw alternate cursor, if any
                    if (editorEngine.altCursorPosition != -1)
                    {
                        cursorParams = CursorPositionStylePainterParameters.GetDefault(this, text);
                        cursorParams.text = editorEngine.text.Substring(0, editorEngine.altCursorPosition);
                        cursorParams.wordWrapWidth = wordWrapWidth;
                        cursorParams.cursorIndex = editorEngine.altCursorPosition;

                        Vector2 altCursorPosition = m_TextHandle.GetCursorPosition(cursorParams, textScaling);
                        altCursorPosition -= scrollOffset;
                        mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams
                        {
                            rect = new Rect(altCursorPosition.x, altCursorPosition.y, 1f, lineHeight),
                            color = cursorColor,
                            playmodeTintColor = playmodeTintColor
                        });
                    }
                }

                keyboardTextEditor.PostDrawCursor();
            }

            internal virtual bool AcceptCharacter(char c)
            {
                // When readonly or not enabled in the hierarchy, we do not accept any character.
                return !isReadOnly && enabledInHierarchy;
            }

            /// <summary>
            /// Called to construct a menu to show different options.
            /// </summary>
            protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
            {
                if (evt?.target is TextInputBase)
                {
                    if (!isReadOnly)
                    {
                        evt.menu.AppendAction("Cut", Cut, CutCopyActionStatus);
                    }
                    evt.menu.AppendAction("Copy", Copy, CutCopyActionStatus);
                    if (!isReadOnly)
                    {
                        evt.menu.AppendAction("Paste", Paste, PasteActionStatus);
                    }
                }
            }

            private void OnDetectFocusChange()
            {
                if (editorEngine.m_HasFocus && !hasFocus)
                {
                    editorEngine.OnFocus();
                }

                if (!editorEngine.m_HasFocus && hasFocus)
                {
                    editorEngine.OnLostFocus();
                }
            }

            private void OnCursorIndexChange()
            {
                IncrementVersion(VersionChangeType.Repaint);
            }

            protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
            {
                // If the text is empty, we should make sure it returns at least the height/width of 1 character...
                var textToUse = m_Text;
                if (string.IsNullOrEmpty(textToUse))
                {
                    textToUse = " ";
                }

                return TextUtilities.MeasureVisualElementTextSize(this, textToUse, desiredWidth, widthMode, desiredHeight, heightMode, m_TextHandle);
            }

            protected override void ExecuteDefaultActionAtTarget(EventBase evt)
            {
                base.ExecuteDefaultActionAtTarget(evt);

                elementPanel?.contextualMenuManager?.DisplayMenuIfEventMatches(evt, this);

                if (evt?.eventTypeId == ContextualMenuPopulateEvent.TypeId())
                {
                    ContextualMenuPopulateEvent e = evt as ContextualMenuPopulateEvent;
                    int count = e.menu.MenuItems().Count;
                    BuildContextualMenu(e);

                    if (count > 0 && e.menu.MenuItems().Count > count)
                    {
                        e.menu.InsertSeparator(null, count);
                    }
                }
                else if (evt.eventTypeId == FocusInEvent.TypeId())
                {
                    SaveValueAndText();
                }
                else if (evt.eventTypeId == KeyDownEvent.TypeId())
                {
                    KeyDownEvent keyDownEvt = evt as KeyDownEvent;

                    if (keyDownEvt?.keyCode == KeyCode.Escape)
                    {
                        RestoreValueAndText();
                        parent.Focus();
                    }
                }

                editorEventHandler.ExecuteDefaultActionAtTarget(evt);
            }

            protected override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                editorEventHandler.ExecuteDefaultAction(evt);
            }

            bool ITextInputField.hasFocus => hasFocus;

            void ITextInputField.SyncTextEngine()
            {
                SyncTextEngine();
            }

            bool ITextInputField.AcceptCharacter(char c)
            {
                return AcceptCharacter(c);
            }

            string ITextInputField.CullString(string s)
            {
                return CullString(s);
            }

            void ITextInputField.UpdateText(string value)
            {
                UpdateText(value);
            }

            TextEditorEngine ITextInputField.editorEngine => editorEngine;

            bool ITextInputField.isDelayed => isDelayed;

            void ITextInputField.UpdateValueFromText()
            {
                UpdateValueFromText();
            }

            private void DeferGUIStyleRectSync()
            {
                RegisterCallback<GeometryChangedEvent>(OnPercentResolved);
            }

            private void OnPercentResolved(GeometryChangedEvent evt)
            {
                UnregisterCallback<GeometryChangedEvent>(OnPercentResolved);

                var guiStyle = editorEngine.style;
                int left = (int)resolvedStyle.marginLeft;
                int top = (int)resolvedStyle.marginTop;
                int right = (int)resolvedStyle.marginRight;
                int bottom = (int)resolvedStyle.marginBottom;
                AssignRect(guiStyle.margin, left, top, right, bottom);

                left = (int)resolvedStyle.paddingLeft;
                top = (int)resolvedStyle.paddingTop;
                right = (int)resolvedStyle.paddingRight;
                bottom = (int)resolvedStyle.paddingBottom;
                AssignRect(guiStyle.padding, left, top, right, bottom);
            }

            private static void SyncGUIStyle(TextInputBase textInput, GUIStyle style)
            {
                var computedStyle = textInput.computedStyle;
                style.alignment = computedStyle.unityTextAlign;
                style.wordWrap = computedStyle.whiteSpace == WhiteSpace.Normal;
                bool overflowVisible = computedStyle.overflow == OverflowInternal.Visible;
                style.clipping = overflowVisible ? TextClipping.Overflow : TextClipping.Clip;

                style.font = TextUtilities.GetFont(textInput);

                style.fontSize = (int)computedStyle.fontSize.value;
                style.fontStyle = computedStyle.unityFontStyleAndWeight;

                int left = computedStyle.unitySliceLeft;
                int top = computedStyle.unitySliceTop;
                int right = computedStyle.unitySliceRight;
                int bottom = computedStyle.unitySliceBottom;
                AssignRect(style.border, left, top, right, bottom);

                if (IsLayoutUsingPercent(textInput))
                {
                    textInput.DeferGUIStyleRectSync();
                }
                else
                {
                    left = (int)computedStyle.marginLeft.value;
                    top = (int)computedStyle.marginTop.value;
                    right = (int)computedStyle.marginRight.value;
                    bottom = (int)computedStyle.marginBottom.value;
                    AssignRect(style.margin, left, top, right, bottom);

                    left = (int)computedStyle.paddingLeft.value;
                    top = (int)computedStyle.paddingTop.value;
                    right = (int)computedStyle.paddingRight.value;
                    bottom = (int)computedStyle.paddingBottom.value;
                    AssignRect(style.padding, left, top, right, bottom);
                }
            }

            private static bool IsLayoutUsingPercent(VisualElement ve)
            {
                var computedStyle = ve.computedStyle;

                // Margin
                if (computedStyle.marginLeft.unit == LengthUnit.Percent ||
                    computedStyle.marginTop.unit == LengthUnit.Percent ||
                    computedStyle.marginRight.unit == LengthUnit.Percent ||
                    computedStyle.marginBottom.unit == LengthUnit.Percent)
                    return true;

                // Padding
                if (computedStyle.paddingLeft.unit == LengthUnit.Percent ||
                    computedStyle.paddingTop.unit == LengthUnit.Percent ||
                    computedStyle.paddingRight.unit == LengthUnit.Percent ||
                    computedStyle.paddingBottom.unit == LengthUnit.Percent)
                    return true;

                return false;
            }

            private static void AssignRect(RectOffset rect, int left, int top, int right, int bottom)
            {
                rect.left = left;
                rect.top = top;
                rect.right = right;
                rect.bottom = bottom;
            }
        }
    }
}
