// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    internal class TouchScreenTextEditorEventHandler : TextEditorEventHandler
    {
        private IVisualElementScheduledItem m_TouchKeyboardPoller = null;
        private VisualElement m_LastPointerDownTarget;

        internal TouchScreenKeyboard keyboardOnScreen = null;

        public TouchScreenTextEditorEventHandler(TextElement textElement, TextEditingUtilities editingUtilities)
            : base(textElement, editingUtilities) {}

        void PollTouchScreenKeyboard()
        {
            if (TouchScreenKeyboard.isSupported && !TouchScreenKeyboard.isInPlaceEditingAllowed)
            {
                if (m_TouchKeyboardPoller == null)
                {
                    m_TouchKeyboardPoller = textElement?.schedule.Execute(DoPollTouchScreenKeyboard).Every(100);
                }
                else
                {
                    m_TouchKeyboardPoller.Resume();
                }
            }
        }

        void DoPollTouchScreenKeyboard()
        {
            if (TouchScreenKeyboard.isSupported && !TouchScreenKeyboard.isInPlaceEditingAllowed)
            {
                if (keyboardOnScreen != null)
                {
                    var edition = textElement.edition;
                    edition.UpdateText(keyboardOnScreen.text);

                    if (!edition.isDelayed)
                        edition.UpdateValueFromText?.Invoke();

                    if (keyboardOnScreen.status != TouchScreenKeyboard.Status.Visible)
                    {
                        CloseTouchScreenKeyboard();
                        m_TouchKeyboardPoller.Pause();

                        if (edition.isDelayed)
                            edition.UpdateValueFromText?.Invoke();
                        edition.UpdateTextFromValue?.Invoke();

                        textElement.edition.MoveFocusToCompositeRoot?.Invoke();
                    }
                }
            }
            else
            {
                // TouchScreenKeyboard should no longer be used, presumably because a hardware keyboard is now available.
                CloseTouchScreenKeyboard();

                m_TouchKeyboardPoller.Pause();
            }
        }

        private void CloseTouchScreenKeyboard()
        {
            if (keyboardOnScreen != null)
            {
                keyboardOnScreen.active = false;
                keyboardOnScreen = null;
            }
        }

        private void OpenTouchScreenKeyboard()
        {
            keyboardOnScreen = TouchScreenKeyboard.Open(textElement.text,
                TouchScreenKeyboardType.Default,
                true, // autocorrection
                textElement.edition.multiline,
                textElement.edition.isPassword);

        }

        public override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (keyboardOnScreen != null)
                return;

            var edition = textElement.edition;
            if (!edition.isReadOnly && evt.eventTypeId == PointerDownEvent.TypeId())
            {
                // CaptureMouse is preventing WebGL from processing pointerDown event in
                // TextInputFieldBase during the Javascript event handler, preventing the
                // keyboard from being displayed. Disable the capture behavior for WebGL.
                textElement.CaptureMouse();
                m_LastPointerDownTarget = evt.target as VisualElement;
            }
            else if (!edition.isReadOnly && evt.eventTypeId == PointerUpEvent.TypeId())
            {
                textElement.ReleaseMouse();
                if (m_LastPointerDownTarget == null || !m_LastPointerDownTarget.worldBound.Contains(((PointerUpEvent)evt).position))
                    return;

                m_LastPointerDownTarget = null;

                edition.UpdateText(editingUtilities.text);

                OpenTouchScreenKeyboard();

                if (keyboardOnScreen != null)
                {
                    PollTouchScreenKeyboard();
                }

                // Scroll offset might need to be updated
                edition.UpdateScrollOffset?.Invoke(false);
                evt.StopPropagation();
            }
        }
    }
}
