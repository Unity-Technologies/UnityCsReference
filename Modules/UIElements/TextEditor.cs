// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    internal class TextEditorEventHandler
    {
        protected TextEditorEngine editorEngine { get; private set; }

        protected TextInputFieldBase textInputField { get; private set; }

        protected TextEditorEventHandler(TextEditorEngine editorEngine, TextInputFieldBase textInputField)
        {
            this.editorEngine = editorEngine;
            this.textInputField = textInputField;
            this.textInputField.SyncTextEngine();
        }

        public virtual void ExecuteDefaultActionAtTarget(EventBase evt) {}

        public virtual void ExecuteDefaultAction(EventBase evt)
        {
            if (evt.GetEventTypeId() == FocusEvent.TypeId())
            {
                editorEngine.OnFocus();
            }
            else if (evt.GetEventTypeId() == BlurEvent.TypeId())
            {
                editorEngine.OnLostFocus();
                editorEngine.SelectNone();
            }
        }
    }
}
