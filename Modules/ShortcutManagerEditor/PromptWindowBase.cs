// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    internal abstract class PromptWindowBase : EditorWindow
    {
        internal void ShowWindow()
        {
            CenterWindow();
            ShowAuxWindow();
            RepaintImmediately();
        }

        void CenterWindow()
        {
            var parentWindow = GetWindow<ShortcutManagerWindow>();
            if (!parentWindow)
                return;

            var parent = parentWindow.position;
            var winCenter = new Vector2(parent.x + parent.width / 2, parent.y + parent.height / 2);
            position = new Rect(winCenter - (minSize / 2), minSize);
        }
    }
}
