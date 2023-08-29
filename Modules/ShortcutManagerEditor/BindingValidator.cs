// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    interface IBindingValidator
    {
        bool IsBindingValid(IEnumerable<KeyCombination> keyCombinations, out string invalidBindingMessage);
    }

    class BindingValidator : IBindingValidator
    {
        static readonly HashSet<KeyCode> s_InvalidKeyCodes = new HashSet<KeyCode>
        {
            KeyCode.None,
            KeyCode.Escape,
            KeyCode.Return,
            KeyCode.CapsLock,
            KeyCode.RightShift,
            KeyCode.LeftShift,
            KeyCode.RightAlt,
            KeyCode.LeftAlt,
            KeyCode.RightControl,
            KeyCode.LeftControl,
            KeyCode.RightCommand,
            KeyCode.LeftCommand,
        };

        // The following method is internal because it's also used in tests.
        internal string GetInvalidKeyCodes(KeyCode key)
        {
            var allKeyCodeNames = Enum.GetNames(typeof(KeyCode));
            var invalidKeyCodeNames = new List<string>();
            foreach (var name in allKeyCodeNames)
            {
                // This is needed because LeftMeta, LeftCommand and LeftApple have the same enum values.
                // The same goes for RightMeta, RightCommand and RightApple.
                // There is no way of differentiating them. For this reason, we print all of them in the
                // error message if one of them is pressed.
                var currentKeyCode = (KeyCode)Enum.Parse(typeof(KeyCode), name);
                if (currentKeyCode.Equals(key))
                    invalidKeyCodeNames.Add(name);
            }

            return string.Join("/", invalidKeyCodeNames);
        }

        public bool IsBindingValid(IEnumerable<KeyCombination> keyCombinations, out string invalidBindingMessage)
        {
            foreach (var keyCombination in keyCombinations)
            {
                if (s_InvalidKeyCodes.Contains(keyCombination.keyCode) || (int)keyCombination.keyCode >= Directory.MaxIndexedEntries)
                {
                    invalidBindingMessage = $"Binding uses invalid key code {GetInvalidKeyCodes(keyCombination.keyCode)}";
                    return false;
                }

                for (int i = 0; i < 32; i++)
                {
                    int flag = (int)keyCombination.modifiers & (1 << i);
                    if (flag != 0 && !Enum.IsDefined(typeof(ShortcutModifiers), flag))
                    {
                        invalidBindingMessage = $"Binding uses invalid modifier {flag}";
                        return false;
                    }
                }
            }

            invalidBindingMessage = null;
            return true;
        }
    }

    static class BindingValidatorHelper
    {
        public static bool IsBindingValid(this IBindingValidator bindingValidator, KeyCode keyCode)
        {
            string invalidBindingMessage;
            return bindingValidator.IsBindingValid(new[] { new KeyCombination(keyCode) }, out invalidBindingMessage);
        }
    }
}
