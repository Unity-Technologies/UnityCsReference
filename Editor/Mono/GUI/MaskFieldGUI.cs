// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    // Class for storing state for mask menus so we can get the info back to OnGUI from the user selection
    internal static class MaskFieldGUI
    {
        // Class for storing state for mask menus so we can get the info back to OnGUI from the user selection
        private class MaskCallbackInfo
        {
            // The global shared popup state
            public static MaskCallbackInfo m_Instance;

            // Name of the command event sent from the popup menu to OnGUI when user has changed selection
            private const string kMaskMenuChangedMessage = "MaskMenuChanged";

            // The control ID of the popup menu that is currently displayed.
            // Used to pass selection changes back again
            private readonly int m_ControlID;

            // New mask value
            private int m_NewMask;

            // Which view should we send it to.
            private readonly GUIView m_SourceView;

            public MaskCallbackInfo(int controlID)
            {
                m_ControlID = controlID;
                m_SourceView = GUIView.current;
            }

            public static int GetSelectedValueForControl(int controlID, int mask, out int changedFlags, out bool changedToValue)
            {
                var evt = Event.current;

                // No flags are changed by default
                changedFlags = 0;
                changedToValue = false;

                if (evt.type == EventType.ExecuteCommand && evt.commandName == kMaskMenuChangedMessage)
                {
                    if (m_Instance == null)
                    {
                        Debug.LogError("Mask menu has no instance");
                        return mask;
                    }
                    if (m_Instance.m_ControlID == controlID)
                    {
                        changedFlags = mask ^ m_Instance.m_NewMask;
                        changedToValue = (m_Instance.m_NewMask & changedFlags) != 0;

                        if (changedFlags != 0)
                        {
                            mask = m_Instance.m_NewMask;
                            GUI.changed = true;
                        }

                        m_Instance = null;
                        evt.Use();
                    }
                }
                return mask;
            }

            internal void SetMaskValueDelegate(object userData, string[] options, int selected)
            {
                int[] optionMaskValues = (int[])userData;
                m_NewMask = optionMaskValues[selected];

                if (m_SourceView)
                    m_SourceView.SendEvent(EditorGUIUtility.CommandEvent(kMaskMenuChangedMessage));
            }
        }

        /// Make a field for a generic mask.
        internal static int DoMaskField(Rect position, int controlID, int mask, string[] flagNames, GUIStyle style)
        {
            int dummyInt;
            bool dummyBool;
            return DoMaskField(position, controlID, mask, flagNames, style, out dummyInt, out dummyBool);
        }

        internal static int DoMaskField(Rect position, int controlID, int mask, string[] flagNames, int[] flagValues, GUIStyle style)
        {
            int dummyInt;
            bool dummyBool;
            return DoMaskField(position, controlID, mask, flagNames, flagValues, style, out dummyInt, out dummyBool);
        }

        internal static int DoMaskField(Rect position, int controlID, int mask, string[] flagNames, GUIStyle style, out int changedFlags, out bool changedToValue)
        {
            var flagValues = new int[flagNames.Length];
            for (int i = 0; i < flagValues.Length; ++i)
                flagValues[i] = (1 << i);

            return DoMaskField(position, controlID, mask, flagNames, flagValues, style, out changedFlags, out changedToValue);
        }

        /// Make a field for a generic mask.
        /// This version also gives you back which flags were changed and what they were changed to.
        /// This is useful if you want to make the same change to multiple objects.
        internal static int DoMaskField(Rect position, int controlID, int mask, string[] flagNames, int[] flagValues, GUIStyle style, out int changedFlags, out bool changedToValue)
        {
            mask = MaskCallbackInfo.GetSelectedValueForControl(controlID, mask, out changedFlags, out changedToValue);

            string buttonText;
            string[] optionNames;
            int[] optionMaskValues;
            int[] selectedOptions;
            GetMenuOptions(mask, flagNames, flagValues, out buttonText, out optionNames, out optionMaskValues, out selectedOptions);

            Event evt = Event.current;
            if (evt.type == EventType.Repaint)
            {
                GUIContent buttonContent = EditorGUI.showMixedValue ? EditorGUI.mixedValueContent : EditorGUIUtility.TempContent(buttonText);
                style.Draw(position, buttonContent, controlID, false);
            }
            else if ((evt.type == EventType.MouseDown && position.Contains(evt.mousePosition)) || evt.MainActionKeyForControl(controlID))
            {
                MaskCallbackInfo.m_Instance = new MaskCallbackInfo(controlID);
                evt.Use();
                EditorUtility.DisplayCustomMenu(position, optionNames,
                    // Only show selections if we are not multi-editing
                    EditorGUI.showMixedValue ? new int[] {} : selectedOptions,
                    // optionMaskValues is from the pool so use a clone of the values for the current control
                    MaskCallbackInfo.m_Instance.SetMaskValueDelegate, optionMaskValues.Clone());
                EditorGUIUtility.keyboardControl = controlID;
            }

            return mask;
        }

        private static readonly List<string[]> s_OptionNames = new List<string[]>();
        private static readonly List<int[]> s_OptionValues = new List<int[]>();
        private static readonly List<int[]> s_SelectedOptions = new List<int[]>();
        private static readonly HashSet<int> s_SelectedOptionsSet = new HashSet<int>();

        private static T[] GetBuffer<T>(List<T[]> pool, int bufferLength)
        {
            for (int i = pool.Count; i <= bufferLength; ++i)
                pool.Add(null);
            if (pool[bufferLength] == null)
                pool[bufferLength] = new T[bufferLength];
            var buffer = pool[bufferLength];
            for (int i = 0, length = buffer.Length; i < length; ++i)
                buffer[i] = default(T);
            return buffer;
        }

        internal static void GetMenuOptions(int mask, string[] flagNames, int[] flagValues,
            out string buttonText, out string[] optionNames, out int[] optionMaskValues, out int[] selectedOptions)
        {
            bool hasNothingName = (flagValues[0] == 0);
            bool hasEverythingName = (flagValues[flagValues.Length - 1] == ~0);

            var nothingName = (hasNothingName ? flagNames[0] : "Nothing");
            var everythingName = (hasEverythingName ? flagNames[flagValues.Length - 1] : "Everything");

            var optionCount = flagNames.Length + (hasNothingName ? 0 : 1) + (hasEverythingName ? 0 : 1);
            var flagCount = flagNames.Length - (hasNothingName ? 1 : 0) - (hasEverythingName ? 1 : 0);

            // These indices refer to flags that are not 0 and ~0
            var flagStartIndex = (hasNothingName ? 1 : 0);
            var flagEndIndex = flagStartIndex + flagCount;

            // Button text
            buttonText = "Mixed ...";
            if (mask == 0)
                buttonText = nothingName;
            else if (mask == ~0)
                buttonText = everythingName;
            else
            {
                for (var flagIndex = flagStartIndex; flagIndex < flagEndIndex; flagIndex++)
                {
                    if (mask == flagValues[flagIndex])
                        buttonText = flagNames[flagIndex];
                }
            }

            // Options names
            optionNames = GetBuffer(s_OptionNames, optionCount);
            optionNames[0] = nothingName;
            optionNames[1] = everythingName;
            for (var flagIndex = flagStartIndex; flagIndex < flagEndIndex; flagIndex++)
            {
                var optionIndex = flagIndex - flagStartIndex + 2;
                optionNames[optionIndex] = flagNames[flagIndex];
            }

            var flagMask = 0; // Disjunction of all flags (except 0 and ~0)
            var intermediateMask = 0; // Mask used to compute new mask value for each option

            // Selected options
            s_SelectedOptionsSet.Clear();
            if (mask == 0)
                s_SelectedOptionsSet.Add(0);
            if (mask == ~0)
                s_SelectedOptionsSet.Add(1);
            for (var flagIndex = flagStartIndex; flagIndex < flagEndIndex; flagIndex++)
            {
                var flagValue = flagValues[flagIndex];
                flagMask |= flagValue;
                if ((mask & flagValue) == flagValue)
                {
                    var optionIndex = flagIndex - flagStartIndex + 2;
                    s_SelectedOptionsSet.Add(optionIndex);
                    intermediateMask |= flagValue;
                }
            }
            selectedOptions = GetBuffer(s_SelectedOptions, s_SelectedOptionsSet.Count);
            var x = 0;
            foreach (var selected in s_SelectedOptionsSet)
            {
                selectedOptions[x] = selected;
                ++x;
            }

            // Option mask values
            optionMaskValues = GetBuffer(s_OptionValues, optionCount);
            optionMaskValues[0] = 0;
            optionMaskValues[1] = ~0;
            for (var flagIndex = flagStartIndex; flagIndex < flagEndIndex; flagIndex++)
            {
                var optionIndex = flagIndex - flagStartIndex + 2;
                var flagValue = flagValues[flagIndex];
                var flagSet = ((intermediateMask & flagValue) == flagValue);
                var newMask = (flagSet ? intermediateMask & ~flagValue : intermediateMask | flagValue);

                // If all flag options are selected the mask becomes ~0 to be consistent with the "Everything" option
                if (newMask == flagMask)
                    newMask = ~0;

                optionMaskValues[optionIndex] = newMask;
            }
        }
    }
}
