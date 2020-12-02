namespace UnityEngine.UIElements
{
    internal class TextEditorEventHandler
    {
        protected TextEditorEngine editorEngine { get; private set; }

        protected ITextInputField textInputField { get; private set; }

        protected TextEditorEventHandler(TextEditorEngine editorEngine, ITextInputField textInputField)
        {
            this.editorEngine = editorEngine;
            this.textInputField = textInputField;
            this.textInputField.SyncTextEngine();
        }

        public virtual void ExecuteDefaultActionAtTarget(EventBase evt) {}

        public virtual void ExecuteDefaultAction(EventBase evt)
        {
            if (evt.eventTypeId == FocusEvent.TypeId())
            {
                editorEngine.OnFocus();

                // Make sure to select all text, OnFocus() does not call SelectAll for multiline text field.
                // If focused from the mouse, it will be called later by the OnMouseUp event.
                // This is needed when focus is obtained through a keyboard event (i.e. Tab).
                editorEngine.SelectAll();
            }
            else if (evt.eventTypeId == BlurEvent.TypeId())
            {
                editorEngine.OnLostFocus();
                editorEngine.SelectNone();
            }
        }
    }
}
