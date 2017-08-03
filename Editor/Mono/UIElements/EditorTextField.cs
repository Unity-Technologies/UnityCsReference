// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    internal class EditorTextField : TextField
    {
        protected EditorTextField(int maxLength, bool multiline, bool isPasswordField, char maskChar)
            : base(maxLength, multiline, isPasswordField, maskChar)
        {
            var cm = new ContextualMenu();
            cm.AddAction("Cut", Cut, CutCopyActionStatus);
            cm.AddAction("Copy", Copy, CutCopyActionStatus);
            cm.AddAction("Paste", Paste, PasteActionStatus);
            this.AddManipulator(cm);
        }

        ContextualMenu.ActionStatus CutCopyActionStatus()
        {
            return (editor.hasSelection && !isPasswordField) ? ContextualMenu.ActionStatus.Enabled : ContextualMenu.ActionStatus.Disabled;
        }

        ContextualMenu.ActionStatus PasteActionStatus()
        {
            return (editor.CanPaste() ? ContextualMenu.ActionStatus.Enabled : ContextualMenu.ActionStatus.Off);
        }

        void Cut()
        {
            editor.Cut();
        }

        void Copy()
        {
            editor.Copy();
        }

        void Paste()
        {
            editor.Paste();
        }
    }
}
