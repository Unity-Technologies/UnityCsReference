// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEditor;

namespace UnityEditor.Web
{
    [InitializeOnLoad]
    internal class ClipboardAccess
    {
        private ClipboardAccess()
        {
            // Nothing to do
        }

        public void CopyToClipboard(string value)
        {
            TextEditor te = new TextEditor();
            te.text = value;
            te.SelectAll();
            te.Copy();
        }

        public string PasteFromClipboard()
        {
            TextEditor te = new TextEditor();
            te.Paste();
            return te.text;
        }

        static ClipboardAccess()
        {
            JSProxyMgr.GetInstance().AddGlobalObject("unity/ClipboardAccess", new ClipboardAccess());
        }
    }
}

