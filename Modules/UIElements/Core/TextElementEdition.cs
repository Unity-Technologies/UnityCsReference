// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using Unity.Properties;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface to access a TextElement edition capabilities
    /// This interface is not meant to be implemented explicitly
    /// as its declaration might change in the future.
    /// </summary>
    public interface ITextEdition
    {
        // A text element can`t really be multiline by itself, this needs to stay internal
        internal bool multiline { get; set; }

        /// <summary>
        /// Returns true if the element is read only.
        /// </summary>
        bool isReadOnly { get; set; }

        /// <summary>
        /// Maximum number of characters for that element
        /// </summary>
        public int maxLength { get; set; }

        /// <summary>
        /// A short hint to help users understand what to enter in the field.
        /// </summary>
        public string placeholder { get; set; }

        /// <summary>
        /// If set to true, the value property isn't updated until either the user presses Enter or the element loses focus.
        /// </summary>
        public bool isDelayed { get; set; }

        /// <summary>
        /// Resets the text contained in the element.
        /// </summary>
        internal void ResetValueAndText();

        internal void SaveValueAndText();

        internal void RestoreValueAndText();

        internal Func<char, bool> AcceptCharacter { get; set; }
        internal Action<bool> UpdateScrollOffset { get; set; }
        internal Action UpdateValueFromText { get; set; }
        internal Action UpdateTextFromValue { get; set; }
        internal Action MoveFocusToCompositeRoot { get; set; }
        internal Func<string> GetDefaultValueType { get; set; }

        internal void UpdateText(string value);

        internal string CullString(string s);

        /// <summary>
        /// The character used for masking when in password mode.
        /// </summary>
        public char maskChar { get; set; }

        /// <summary>
        /// Returns true if the field is used to edit a password.
        /// </summary>
        public bool isPassword { get; set; }

        /// <summary>
        /// Hides the placeholder on focus.
        /// </summary>
        public bool hidePlaceholderOnFocus { get; set; }

        /// <summary>
        /// Determines if the soft keyboard auto correction is turned on or off.
        /// </summary>
        public bool autoCorrection
        {
            get
            {
                Debug.Log($"Type {GetType().Name} implementing interface {nameof(ITextEdition)} is missing the implementation for {nameof(autoCorrection)}. Calling {nameof(ITextEdition)}.{nameof(autoCorrection)} of this type will always return false.");
                return false;
            }
            set => Debug.Log($"Type {GetType().Name} implementing interface {nameof(ITextEdition)} is missing the implementation for {nameof(autoCorrection)}. Assigning a value to {nameof(ITextEdition)}.{nameof(autoCorrection)} will not update its value.");
        }

        /// <summary>
        /// Hides or shows the mobile input field.
        /// </summary>
        public bool hideMobileInput
        {
            get
            {
                Debug.Log($"Type {GetType().Name} implementing interface {nameof(ITextEdition)} is missing the implementation for {nameof(hideMobileInput)}. Calling {nameof(ITextEdition)}.{nameof(hideMobileInput)} of this type will always return false.");
                return false;
            }
            set => Debug.Log($"Type {GetType().Name} implementing interface {nameof(ITextEdition)} is missing the implementation for {nameof(hideMobileInput)}. Assigning a value to {nameof(ITextEdition)}.{nameof(hideMobileInput)} will not update its value.");
        }

        /// <summary>
        /// The TouchScreenKeyboard being used to edit the Input Field.
        /// </summary>
        public TouchScreenKeyboard touchScreenKeyboard
        {
            get
            {
                Debug.Log($"Type {GetType().Name} implementing interface {nameof(ITextEdition)} is missing the implementation for {nameof(touchScreenKeyboard)}. Calling {nameof(ITextEdition)}.{nameof(touchScreenKeyboard)} of this type will always return null.");
                return null;
            }
        }

        /// <summary>
        /// The type of mobile keyboard that will be used.
        /// </summary>
        public TouchScreenKeyboardType keyboardType
        {
            get
            {
                Debug.Log($"Type {GetType().Name} implementing interface {nameof(ITextEdition)} is missing the implementation for {nameof(keyboardType)}. Calling {nameof(ITextEdition)}.{nameof(keyboardType)} of this type will always return {nameof(TouchScreenKeyboardType.Default)}.");
                return TouchScreenKeyboardType.Default;
            }
            set => Debug.Log($"Type {GetType().Name} implementing interface {nameof(ITextEdition)} is missing the implementation for {nameof(keyboardType)}. Assigning a value to {nameof(ITextEdition)}.{nameof(keyboardType)} will not update its value.");
        }
    }

    // Text editing and selection management implementation
    public partial class TextElement : ITextEdition
    {
        internal static readonly BindingId autoCorrectionProperty = nameof(autoCorrection);
        internal static readonly BindingId hideMobileInputProperty = nameof(hideMobileInput);
        internal static readonly BindingId keyboardTypeProperty = nameof(keyboardType);
        internal static readonly BindingId isReadOnlyProperty = nameof(isReadOnly);
        internal static readonly BindingId isPasswordProperty = nameof(isPassword);
        internal static readonly BindingId maxLengthProperty = nameof(maxLength);
        internal static readonly BindingId maskCharProperty = nameof(maskChar);

        /// <summary>
        /// Retrieves this TextElement's ITextEdition
        /// </summary>
        internal ITextEdition edition => this;

        internal TextEditingManipulator editingManipulator { get; private set; }

        bool m_Multiline;

        bool ITextEdition.multiline
        {
            get => m_Multiline;
            set
            {
                if (value != m_Multiline)
                {
                    if (!edition.isReadOnly)
                        editingManipulator.editingUtilities.multiline = value;
                    m_Multiline = value;
                }
            }
        }

        internal TouchScreenKeyboard m_TouchScreenKeyboard;
        internal Action<bool> onIsReadOnlyChanged;
        TouchScreenKeyboard ITextEdition.touchScreenKeyboard
        {
            get => m_TouchScreenKeyboard;
        }

        internal TouchScreenKeyboardType m_KeyboardType = TouchScreenKeyboardType.Default;

        TouchScreenKeyboardType ITextEdition.keyboardType
        {
            get => m_KeyboardType;
            set
            {
                if (m_KeyboardType == value)
                    return;
                m_KeyboardType = value;
                NotifyPropertyChanged(keyboardTypeProperty);
            }
        }

        [CreateProperty]
        private TouchScreenKeyboardType keyboardType
        {
            get => edition.keyboardType;
            set => edition.keyboardType = value;
        }

        bool m_HideMobileInput;

        bool ITextEdition.hideMobileInput
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
                    case RuntimePlatform.WebGLPlayer:
                        return m_HideMobileInput;
                }
                return m_HideMobileInput;
            }
            set
            {
                var current = m_HideMobileInput;
                switch(Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
                    case RuntimePlatform.WebGLPlayer:
                        m_HideMobileInput = value;
                        break;
                    default:
                        m_HideMobileInput = value;
                        break;
                }
                if (current != m_HideMobileInput)
                    NotifyPropertyChanged(hideMobileInputProperty);
            }
        }

        [CreateProperty]
        private bool hideMobileInput
        {
            get => edition.hideMobileInput;
            set => edition.hideMobileInput = value;
        }

        bool m_IsReadOnly = true;

        bool ITextEdition.isReadOnly
        {
            get => (m_IsReadOnly || !enabledInHierarchy);
            set
            {
                if (value == m_IsReadOnly)
                    return;

                editingManipulator?.Reset();
                editingManipulator = value ? null : new TextEditingManipulator(this);
                m_IsReadOnly = value;
                onIsReadOnlyChanged?.Invoke(value);
                NotifyPropertyChanged(isReadOnlyProperty);
            }
        }

        [CreateProperty]
        private bool isReadOnly
        {
            get => edition.isReadOnly;
            set => edition.isReadOnly = value;
        }

        void ProcessMenuCommand(string command)
        {
            Focus();
            using (ExecuteCommandEvent evt = ExecuteCommandEvent.GetPooled(command))
            {
                evt.elementTarget = this;
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

        /// <summary>
        /// Called to construct a menu to show different options.
        /// </summary>
        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt?.target is TextElement)
            {
                if (!edition.isReadOnly)
                {
                    evt.menu.AppendAction("Cut", Cut, CutActionStatus);
                    evt.menu.AppendAction("Copy", Copy, CopyActionStatus);
                    evt.menu.AppendAction("Paste", Paste, PasteActionStatus);
                }
                else
                    evt.menu.AppendAction("Copy", Copy, CopyActionStatus);
            }
        }

        DropdownMenuAction.Status CutActionStatus(DropdownMenuAction a)
        {
            return enabledInHierarchy && selection.HasSelection() && !edition.isPassword
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
        }

        DropdownMenuAction.Status CopyActionStatus(DropdownMenuAction a)
        {
            return (!enabledInHierarchy || selection.HasSelection()) && !edition.isPassword
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
        }

        DropdownMenuAction.Status PasteActionStatus(DropdownMenuAction a)
        {
            var canPaste = editingManipulator.editingUtilities.CanPaste();
            return enabledInHierarchy
                ? canPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
                : DropdownMenuAction.Status.Hidden;
        }

        // From SelectingManipulator.HandleEventBubbleUp, EditingManipulator.HandleEventBubbleUp
        [EventInterest(typeof(ContextualMenuPopulateEvent), typeof(KeyDownEvent), typeof(KeyUpEvent),
            typeof(ValidateCommandEvent), typeof(ExecuteCommandEvent),
            typeof(FocusEvent), typeof(BlurEvent), typeof(FocusInEvent), typeof(FocusOutEvent),
            typeof(PointerDownEvent), typeof(PointerUpEvent), typeof(PointerMoveEvent),
            typeof(NavigationMoveEvent), typeof(NavigationSubmitEvent), typeof(NavigationCancelEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (selection.isSelectable)
            {
                var useTouchScreenKeyboard = editingManipulator?.editingUtilities.TouchScreenKeyboardShouldBeUsed() ?? false;

                if (!useTouchScreenKeyboard || edition.hideMobileInput)
                    selectingManipulator?.HandleEventBubbleUp(evt);
                if (!edition.isReadOnly)
                    editingManipulator?.HandleEventBubbleUp(evt);

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
            }
        }


        int m_MaxLength = -1;
        int ITextEdition.maxLength
        {
            get => m_MaxLength;
            set
            {
                if (m_MaxLength == value)
                    return;
                m_MaxLength = value;
                text = edition.CullString(text);
                NotifyPropertyChanged(maxLengthProperty);
            }
        }

        [CreateProperty]
        private int maxLength
        {
            get => edition.maxLength;
            set => edition.maxLength = value;
        }

        string m_PlaceholderText = "";
        string ITextEdition.placeholder
        {
            get => m_PlaceholderText;
            set
            {
                if (value == m_PlaceholderText)
                    return;

                //this approach results not showing leading 0s when input fields are updated from the UI Builder if there is a placeholder text
                //however since this does not occur at run-time and inputfields are generally empty when placeholder text is set this should be ok
                //if we want to fix this down the line then defaultValue needs to return null so we could differentiate an empty field vs. one with "0"
                if (!string.IsNullOrEmpty(value) && (text == null || text.Equals(edition.GetDefaultValueType())))
                    text = "";

                m_PlaceholderText = value;
                OnPlaceholderChanged?.Invoke();
                MarkDirtyRepaint();
            }
        }

        bool ITextEdition.isDelayed { get; set; }

        /// <summary>
        /// Resets the text contained in the field.
        /// </summary>
        void ITextEdition.ResetValueAndText()
        {
            m_OriginalText = text = default(string);
        }

        void ITextEdition.SaveValueAndText()
        {
            // When getting the FocusIn, we must keep the value in case of Escape...
            m_OriginalText = text;
        }

        void ITextEdition.RestoreValueAndText()
        {
            text = m_OriginalText;
        }

        Func<char, bool> ITextEdition.AcceptCharacter { get; set; }
        Action<bool> ITextEdition.UpdateScrollOffset { get; set; }
        Action ITextEdition.UpdateValueFromText { get; set; }
        Action ITextEdition.UpdateTextFromValue { get; set; }
        Action ITextEdition.MoveFocusToCompositeRoot { get; set; }
        internal Action OnPlaceholderChanged { get; set; }
        Func<string> ITextEdition.GetDefaultValueType { get; set; }

        void ITextEdition.UpdateText(string value)
        {
            if (m_TouchScreenKeyboard != null && m_TouchScreenKeyboard.text != value)
                m_TouchScreenKeyboard.text = value;

            if (text != value)
            {
                // Setting the VisualElement text here cause a repaint since it dirty the layout flag.
                using (InputEvent evt = InputEvent.GetPooled(text, value))
                {
                    evt.elementTarget = parent;
                    ((INotifyValueChanged<string>)this).SetValueWithoutNotify(value);
                    parent?.SendEvent(evt);
                }
            }
        }
        string ITextEdition.CullString(string s)
        {
            var mLength = edition.maxLength;
            if (mLength >= 0 && s != null && s.Length > mLength)
                return s.Substring(0, mLength);
            return s;
        }

        char ITextEdition.maskChar
        {
            get => m_MaskChar;
            set
            {
                if (m_MaskChar != value)
                {
                    m_MaskChar = value;

                    if (edition.isPassword)
                    {
                        IncrementVersion(VersionChangeType.Repaint);
                    }
                    NotifyPropertyChanged(maskCharProperty);
                }
            }
        }

        [CreateProperty]
        private char maskChar
        {
            get => edition.maskChar;
            set => edition.maskChar = value;
        }

        char effectiveMaskChar
        {
            get => edition.isPassword ? m_MaskChar : Char.MinValue;
        }

        bool ITextEdition.isPassword
        {
            get => m_IsPassword;
            set
            {
                if (m_IsPassword != value)
                {
                    m_IsPassword = value;
                    IncrementVersion(VersionChangeType.Repaint);
                    NotifyPropertyChanged(isPasswordProperty);
                }
            }
        }

        [CreateProperty]
        private bool isPassword
        {
            get => edition.isPassword;
            set => edition.isPassword = value;
        }

        bool ITextEdition.hidePlaceholderOnFocus
        {
            get => m_HidePlaceholderTextOnFocus;
            set => m_HidePlaceholderTextOnFocus = value;
        }

        internal bool showPlaceholderText
        {
            get
            {
                var isPlaceholderVisible = m_PlaceholderText.Length > 0;
                var shouldHideOnFocus = edition.hidePlaceholderOnFocus && hasFocus;
                var isTextEmpty = string.IsNullOrEmpty(text);

                if (!isPlaceholderVisible) return false;
                if (shouldHideOnFocus) return false;

                return isTextEmpty;
            }
        }

        bool ITextEdition.autoCorrection
        {
            get => m_AutoCorrection;
            set
            {
                if (m_AutoCorrection == value)
                    return;
                m_AutoCorrection = value;
                NotifyPropertyChanged(autoCorrectionProperty);
            }
        }

        [CreateProperty]
        private bool autoCorrection
        {
            get => edition.autoCorrection;
            set => edition.autoCorrection = value;
        }

        private const string ZeroWidthSpace = "\u200B";
        private string m_RenderedText;

        internal RenderedText renderedText
        {
            get
            {
                if (showPlaceholderText)
                {
                    return new RenderedText(m_PlaceholderText, ZeroWidthSpace);
                }

                if (effectiveMaskChar != char.MinValue)
                {
                    return new RenderedText(effectiveMaskChar, m_RenderedText?.Length ?? 0, ZeroWidthSpace);
                }

                return new RenderedText(m_RenderedText, ZeroWidthSpace);
            }
        }

        private void SetRenderedText(string value)
        {
            m_RenderedText = value;
        }

        string m_OriginalText;
        internal string originalText => m_OriginalText;

        private char m_MaskChar;
        private bool m_IsPassword;
        private bool m_HidePlaceholderTextOnFocus;
        private bool m_AutoCorrection;
    }
}
