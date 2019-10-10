// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal static class SelectionCommands
    {
        [Shortcut("Edit/CopyInsert", null, "%INS")]
        internal static void CopyInsertSelection()
        {
            InternalEditorUtility.ExecuteCommandOnKeyWindow(EventCommandNames.Copy);
        }

        [Shortcut("Edit/PasteInsert", null, "#INS")]
        internal static void PasteInsertSelection()
        {
            InternalEditorUtility.ExecuteCommandOnKeyWindow(EventCommandNames.Paste);
        }
    }
}
