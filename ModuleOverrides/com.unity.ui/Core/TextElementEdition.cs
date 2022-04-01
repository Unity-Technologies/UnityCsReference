// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.UIR;

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
        internal Action UpdateScrollOffset{ get; set; }
        internal Action UpdateValueFromText{ get; set; }

        internal void UpdateText(string value);

        internal string CullString(string s);

        internal bool hasFocus { get; }

        /// <summary>
        /// The character used for masking when in password mode
        /// </summary>
        public char maskChar { get; set; }

        /// <summary>
        /// Returns true if the field is used to edit a password.
        /// </summary>
        public bool isPassword { get; set; }
    }

    // Text editing and selection management implementation
    public partial class TextElement : ITextEdition
    {
        /// <summary>
        /// Retrieves this TextElement's ITextEdition
        /// </summary>
        internal ITextEdition edition => this;

        internal TextEditingManipulator editingManipulator;

        bool m_Multiline;

        bool ITextEdition.multiline
        {
            get => m_Multiline;
            set
            {
                if (value != m_Multiline)
                {
                    if (selection.isSelectable)
                        selectingManipulator.m_SelectingUtilities.multiline = value;
                    m_Multiline = value;
                }
            }
        }

        bool m_IsReadOnly = true;

        bool ITextEdition.isReadOnly
        {
            get => (m_IsReadOnly || !enabledInHierarchy);
            set
            {
                if (value == m_IsReadOnly)
                    return;

                editingManipulator = value ? null : new TextEditingManipulator(this);
                m_IsReadOnly = value;
            }
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

        // From SelectingManipulator.ExecuteDefaultActionAtTarget, EditingManipulator.ExecuteDefaultActionAtTarget
        [EventInterest(typeof(ContextualMenuPopulateEvent), typeof(FocusInEvent), typeof(FocusOutEvent),
            typeof(KeyDownEvent), typeof(KeyUpEvent), typeof(FocusEvent), typeof(BlurEvent),
            typeof(MouseUpEvent), typeof(MouseDownEvent), typeof(MouseMoveEvent), typeof(ValidateCommandEvent),
            typeof(ExecuteCommandEvent), typeof(PointerDownEvent), typeof(PointerUpEvent),
            typeof(NavigationCancelEvent), typeof(NavigationSubmitEvent), typeof(NavigationMoveEvent))]
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            if (selection.isSelectable)
            {
                // Selecting is currently not supported with softKeyboard.
                if (!editingManipulator?.touchScreenTextField ?? true)
                    selectingManipulator.ExecuteDefaultActionAtTarget(evt);
                if (!edition.isReadOnly)
                    editingManipulator?.ExecuteDefaultActionAtTarget(evt);
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
                m_MaxLength = value;
                text = edition.CullString(text);
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
        Action ITextEdition.UpdateScrollOffset{ get; set; }
        Action ITextEdition.UpdateValueFromText{ get; set; }

        void ITextEdition.UpdateText(string value)
        {
            if (text != value)
            {
                // Setting the VisualElement text here cause a repaint since it dirty the layout flag.
                using (InputEvent evt = InputEvent.GetPooled(text, value))
                {
                    evt.target = parent;
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

        bool ITextEdition.hasFocus => elementPanel != null && elementPanel.focusController.GetLeafFocusedElement() == this;

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
                }
            }
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
                }
            }
        }

        string m_RenderedText;
        internal string renderedText
        {
            get
            {
                var mskChar = effectiveMaskChar;
                // Handles password fields.
                if (mskChar != Char.MinValue)
                    return "".PadLeft(text.Length, mskChar) + "\u200B";
                return string.IsNullOrEmpty(m_RenderedText) ? "\u200B" : m_RenderedText;
            }
            set =>

                //The NoWidthSpace unicode is added at the end of the string to make sure LineFeeds update the layout of the text.
                m_RenderedText = value + "\u200B";
        }

        string m_OriginalText;
        private char m_MaskChar;
        private bool m_IsPassword;
    }
}
