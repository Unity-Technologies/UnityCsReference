// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.UIAutomation
{
    class KeyInputOverTime
    {
        float intervalBetweenKeyInput;
        float nextEventTime;

        List<KeyCode> keyCodes;
        int caretPosition;

        public void KeyInput(EditorWindow window, List<KeyCode> keyCodeInput, float seconds)
        {
            keyCodes = keyCodeInput;
            caretPosition = 0;
            nextEventTime = 0;
            intervalBetweenKeyInput = seconds / keyCodeInput.Count;
        }

        public bool Update(EditorWindow window)
        {
            float curtime = (float)EditorApplication.timeSinceStartup;
            if (curtime > nextEventTime)
            {
                bool shouldContinue = caretPosition < keyCodes.Count;
                if (caretPosition < keyCodes.Count)
                {
                    EventUtility.KeyDownAndUp(window, keyCodes[caretPosition]);
                    caretPosition++;
                    nextEventTime = curtime + intervalBetweenKeyInput;
                    window.Repaint();
                }
                return shouldContinue;
            }
            return true;
        }
    }
}
