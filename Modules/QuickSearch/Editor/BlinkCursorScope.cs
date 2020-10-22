// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Search
{
    internal struct BlinkCursorScope : IDisposable
    {
        private bool changed;
        private Color oldCursorColor;

        public BlinkCursorScope(bool blink, Color blinkColor)
        {
            changed = false;
            oldCursorColor = Color.white;
            if (blink)
            {
                oldCursorColor = GUI.skin.settings.cursorColor;
                GUI.skin.settings.cursorColor = blinkColor;
                changed = true;
            }
        }

        public void Dispose()
        {
            if (changed)
            {
                GUI.skin.settings.cursorColor = oldCursorColor;
            }
        }
    }
}
