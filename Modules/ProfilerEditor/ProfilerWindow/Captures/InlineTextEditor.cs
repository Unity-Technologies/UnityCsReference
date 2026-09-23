// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    // In-place editing of one field in a capture row: a TextField that takes the place of the label
    // it edits, commits on Enter or on losing focus, and discards on Escape.
    //
    // "Edit field" throughout means this TextField, never the Unity Editor.
    //
    // The rename field and the target-frame-time field need identical behaviour, and the ways it
    // goes wrong are subtle enough to be worth having in exactly one place:
    //
    //  - The fields are delayed, so the text on screen is an edit buffer separate from the field's
    //    value. Restoring the value does not restore the text, and the commit path reads the text,
    //    so a cancel has to suppress the commit rather than rely on putting the old value back.
    //  - Hiding the field blurs it, which raises FocusOutEvent, which is a commit signal. Every
    //    close therefore passes back through the commit path whether or not that was intended,
    //    which is what m_Closing exists to stop.
    //  - A delayed TextField keeps focus when Enter is pressed, so Enter must commit explicitly or
    //    nothing happens until focus is lost some other way.
    //  - Only the field and its visual input are focusable; the inner TextElement is not, and
    //    Focusable.Focus() on a non-focusable element clears focus instead of granting it. Focus
    //    therefore has to target the field and let delegatesFocus route it inwards.
    //  - One key press arrives as two events, a key code and then a character. A menu item
    //    activated from the keyboard gives the field focus between the two, so the character half
    //    lands in the edit field and reads as Enter - committing it immediately. Hence
    //    m_UserHasTyped.
    //
    // Cancelling an edit when the user scrolls the list is the list's job, not this class's - see
    // CapturesListViewController, which owns the ScrollView and knows which row is editing.
    internal sealed class InlineTextEditor
    {
        // Focus can take several panel updates to arrive; give up rather than retry forever if it
        // never does. Matches the bound ScrollView uses for its own deferred-until-ready work.
        const int k_MaxFocusAttempts = 60;

        readonly TextField m_Field;
        readonly VisualElement m_Display;
        readonly TextElement m_InputArea;

        readonly Func<string> m_ReadValue;
        readonly Action<string> m_Commit;
        readonly Func<string, string> m_Sanitize;
        readonly Func<string, bool> m_Validate;
        readonly Action m_Closed;

        // Set for the duration of a close, and left set until the next Open. Closing hides the
        // field, which blurs it, and that blur is a commit signal - so without this every close
        // would commit, including a cancel. The blur is not always dispatched in the same frame as
        // the hide, so this cannot be scoped to the call itself.
        bool m_Closing;

        int m_FocusAttempts;

        // False from Open until the first key-down carrying a real key code. Gates the
        // character-only swallow in OnKeyDown and the re-selection in TryTakeFocus.
        bool m_UserHasTyped;

        /// <param name="field">The field shown while editing.</param>
        /// <param name="display">The label shown while not editing.</param>
        /// <param name="readValue">The committed value, used to seed the edit field and to restore on cancel.</param>
        /// <param name="commit">Applies the edit.</param>
        /// <param name="sanitize">Optional cleanup applied to the typed text before committing.</param>
        /// <param name="validate">Optional. Returning false rejects the text: Enter is swallowed and
        /// editing continues. Also called as the user types, so validation feedback can track input.</param>
        /// <param name="closed">Optional. Called after editing ends, however it ended.</param>
        public InlineTextEditor(
            TextField field,
            VisualElement display,
            Func<string> readValue,
            Action<string> commit,
            Func<string, string> sanitize = null,
            Func<string, bool> validate = null,
            Action closed = null)
        {
            m_Field = field;
            m_Display = display;
            m_InputArea = field?.Q<TextElement>();
            m_ReadValue = readValue;
            m_Commit = commit;
            m_Sanitize = sanitize;
            m_Validate = validate;
            m_Closed = closed;

            if (m_Field == null)
                return;

            m_Field.isDelayed = true;
            m_Field.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            m_Field.RegisterCallback<KeyUpEvent>(OnKeyUp);
            m_Field.RegisterCallback<MouseUpEvent>(OnMouseUp);
            m_Field.RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        public bool IsOpen => m_Field is { visible: true };

        public void Open()
        {
            if (m_Field == null)
                return;

            m_Closing = false;
            m_UserHasTyped = false;

            UIUtility.SwitchVisibility(m_Field, m_Display);
            m_Field.SetValueWithoutNotify(m_ReadValue?.Invoke() ?? string.Empty);

            // Select the text so typing replaces it. The field has not necessarily been laid out on
            // its first open, so the selection is retried once it has been. This is defensive: with
            // focus landing correctly the framework's own selectAllOnFocus already covers the normal
            // case, and no confirmed fault requires this retry - it is kept because it is cheap,
            // guarded by IsOpen, and unregistered on close, so it cannot fire late.
            m_InputArea?.RegisterCallbackOnce<GeometryChangedEvent>(OnFirstLayout);

            // Take focus, retrying until it actually lands.
            //
            // The caller has just asked the platform to bring the profiler window forward, and that
            // completes asynchronously - focus has to finish being handed over after the menu popup
            // closes. A focus request made before the panel owns focus is dropped, so asking once on
            // the next panel update is a bet: it worked for some invocations and not others, leaving
            // the field open but dead to the keyboard. There is no event for "the window has focus
            // now" to wait on, so check each update instead of assuming. A keystroke arriving is
            // itself proof focus landed, so typing ends the retry too.
            m_FocusAttempts = 0;
            m_Field.schedule.Execute(TryTakeFocus).Until(FocusAttemptsFinished);
        }

        // Abandon the edit without applying it.
        //
        // Do not Cancel and then immediately Open the same instance: closing schedules a
        // FocusOutEvent that may arrive after Open has cleared m_Closing, and that stale blur then
        // commits the reopened field. A single blur cannot be attributed to a particular close, so
        // this is prevented at the call site rather than guarded here - see BeginEdit in
        // CaptureFileTreeItemViewController. Reopening an already-open field is safe.
        public void Cancel()
        {
            if (!IsOpen)
                return;

            m_Field.SetValueWithoutNotify(m_ReadValue?.Invoke() ?? string.Empty);
            Close();
        }

        // Release listeners that outlive a single edit. The row's view can be recycled or disposed
        // while a field is being edited.
        public void Detach()
        {
            m_InputArea?.UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);
        }

        void CommitAndClose()
        {
            var text = m_Sanitize != null ? m_Sanitize(m_Field.text) : m_Field.text;
            Close();
            m_Commit?.Invoke(text);
        }

        void Close()
        {
            if (m_Closing)
                return;

            m_Closing = true;
            Detach();
            UIUtility.SwitchVisibility(m_Field, m_Display, false);
            m_Closed?.Invoke();
        }

        void TryTakeFocus()
        {
            if (FocusAttemptsFinished())
                return;

            m_FocusAttempts++;

            // Focus the field rather than its inner TextElement: only the field and its visual
            // input are given focusable = true by BaseField, and Focusable.Focus() on a
            // non-focusable element takes the canGrabFocus == false branch and calls
            // SwitchFocus(null) - clearing focus rather than doing nothing. Going through the field
            // lets delegatesFocus route inwards, so the caret appears and typing works.
            m_Field.Focus();

            // Re-selecting after the user has typed loses their input on the next keystroke.
            if (!m_UserHasTyped)
                m_Field.SelectAll();
        }

        // Side-effect free: used both as the scheduler's stop condition and as an early-out inside
        // the attempt itself.
        bool FocusAttemptsFinished()
        {
            return !IsOpen || m_UserHasTyped || HasKeyboardFocus() || m_FocusAttempts >= k_MaxFocusAttempts;
        }

        bool HasKeyboardFocus()
        {
            return m_Field.focusController?.focusedElement is VisualElement focused
                && (focused == m_Field || m_Field.Contains(focused));
        }

        void OnFirstLayout(GeometryChangedEvent _)
        {
            if (IsOpen && !m_UserHasTyped)
                m_Field.SelectAll();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (!m_UserHasTyped)
            {
                if (evt.keyCode == KeyCode.None)
                {
                    // The OS sends the physical key as a key code and then, separately, whatever
                    // character that press produces. A menu item activated from the keyboard means
                    // the field takes focus between the two, so the character half lands here - and
                    // IsEnterOrSimilar matches on character alone, so it would commit the rename the
                    // instant it opened. It would also replace the freshly selected text.
                    //
                    // Eat character-only key-downs until a real key code arrives, i.e. until the
                    // user actually types something. Typing any character sends its key code first,
                    // so this closes on the user's first real keystroke.
                    evt.StopPropagation();
                    return;
                }

                m_UserHasTyped = true;
            }

            if (IsEnterOrSimilar(evt))
            {
                if (m_Validate != null && !m_Validate(m_Field.text))
                {
                    // Keep editing rather than letting the field finish with an invalid value.
                    evt.StopImmediatePropagation();
                    m_Field.focusController?.IgnoreEvent(evt);
                    return;
                }

                CommitAndClose();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                Cancel();
                evt.StopImmediatePropagation();
            }
        }

        void OnKeyUp(KeyUpEvent _)
        {
            // Validated on key up rather than key down: on key down the field's text does not yet
            // include the key just pressed.
            m_Validate?.Invoke(m_Field.text);
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            // A click inside the field must not reach the row, which would open the capture.
            evt.StopImmediatePropagation();
        }

        void OnFocusOut(FocusOutEvent _)
        {
            if (m_Closing)
                return;

            // Deliberately not validated: rejecting here can leave the user with an invalid value
            // and no focus, which is awkward to recover from.
            CommitAndClose();
        }

        static bool IsEnterOrSimilar(KeyDownEvent evt)
        {
            return evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter ||
                evt.character == '\n' || evt.character == '\r' || evt.character == 0x10;
        }
    }
}
