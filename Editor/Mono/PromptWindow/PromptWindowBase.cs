// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal abstract class PromptWindowBase : EditorWindow
    {
        internal void ShowWindow(EditorWindow owner)
        {
            CenterWindow(owner);
            ShowAuxWindow();
            RepaintImmediately();

            // There's so use cases where the position applied isn't taken into account so we ensure a proper centering 1 frame later
            EditorApplication.delayCall += () => CenterWindow(owner);
        }

        void CenterWindow(EditorWindow owner)
        {
            if (!owner)
                return;

            var rootView = owner.m_Parent.window;
            var parent = rootView.position;
            var winCenter = new Vector2(parent.x + parent.width / 2, parent.y + parent.height / 2);
            position = new Rect(winCenter - (minSize / 2), minSize);
        }
    }
}
