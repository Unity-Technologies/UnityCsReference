// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using System.Text;

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

            // Current drop-down reference
            public MaskFieldDropDown m_DropDown;

            // validation flag for masks that are changed externally
            private bool m_Validate = false;

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

                        if (changedFlags != 0 || EditorGUI.showMixedValue)
                        {
                            mask = m_Instance.m_NewMask;
                            GUI.changed = true;
                        }
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

            public void UpdateFlagChanges(int controlID, int mask, int[] optionMaskValues)
            {
                var evt = Event.current;

                if (evt.type == EventType.ExecuteCommand)
                {
                    m_Validate = true;
                }
                // This code  is responsible for verifying whether the incoming mask value differs from the one that is currently selected in the dropdown menu.
                // When these values do not match, it serves as confirmation that the incoming value has been modified.
                // Subsequently, we proceed to update the dropdown menu to reflect these changes.
                else if (evt.type == EventType.Repaint && m_Validate)
                {
                    if (m_DropDown == null)
                    {
                        return;
                    }

                    if (mask != m_NewMask && m_ControlID == controlID)
                        m_DropDown.UpdateMaskValues(mask, optionMaskValues);

                    m_Validate = false;
                }
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

        internal static void DestroyMaskCallBackInfo()
        {
            MaskCallbackInfo.m_Instance = null;
        }

        /// Make a field for a generic mask.
        /// This version also gives you back which flags were changed and what they were changed to.
        /// This is useful if you want to make the same change to multiple objects.
        internal static int DoMaskField(Rect position, int controlID, int mask, string[] flagNames, int[] flagValues, GUIStyle style, out int changedFlags, out bool changedToValue, Type enumType = null)
        {
            mask = MaskCallbackInfo.GetSelectedValueForControl(controlID, mask, out changedFlags, out changedToValue);

            GetMenuOptions(mask, flagNames, flagValues, out var buttonText, out var buttonTextMixed, out var optionNames, out var optionMaskValues, out _, enumType);

            // This checks and update flags changes that are modified out of dropdown menu
            if (MaskCallbackInfo.m_Instance != null)
                MaskCallbackInfo.m_Instance.UpdateFlagChanges(controlID, mask, optionMaskValues);

            Event evt = Event.current;
            if (evt.type == EventType.Repaint)
            {
                var buttonContent = EditorGUI.showMixedValue ? EditorGUI.mixedValueContent : DoMixedLabel(buttonText, buttonTextMixed, position, style);
                style.Draw(position, buttonContent, controlID, false, position.Contains(evt.mousePosition));
            }
            else if ((evt.type == EventType.MouseDown && position.Contains(evt.mousePosition)) || evt.MainActionKeyForControl(controlID))
            {
                MaskCallbackInfo.m_Instance = new MaskCallbackInfo(controlID);
                MaskCallbackInfo.m_Instance.m_DropDown = new MaskFieldDropDown(optionNames, flagValues, optionMaskValues, mask, MaskCallbackInfo.m_Instance.SetMaskValueDelegate);
                PopupWindowWithoutFocus.Show(position, MaskCallbackInfo.m_Instance.m_DropDown);
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

        internal static void GetMaskButtonValue(int mask, string[] flagNames, int[] flagValues, out string buttonText, out string buttonMixedValuesText)
        {
            const int everythingValue = ~0;
            bool hasNothingName = flagValues[0] == 0;
            bool hasEverythingName = flagValues[flagValues.Length - 1] == everythingValue;

            var nothingName = (hasNothingName ? flagNames[0] : "Nothing");
            var everythingName = (hasEverythingName ? flagNames[flagValues.Length - 1] : "Everything");

            var flagCount = flagNames.Length - (hasNothingName ? 1 : 0) - (hasEverythingName ? 1 : 0);

            var flagStartIndex = (hasNothingName ? 1 : 0);
            var flagEndIndex = flagStartIndex + flagCount;

            var flagMask = 0; // Disjunction of all flags (except 0 and everythingValue)
            var intermediateMask = 0; // Mask used to compute new mask value for each options
            var usedFlags = ListPool<int>.Get();

            buttonText = null;
            buttonMixedValuesText = null;

            for (var flagIndex = flagStartIndex; flagIndex < flagEndIndex; flagIndex++)
            {
                var flagValue = flagValues[flagIndex];
                flagMask |= flagValue;

                if (mask == flagValues[flagIndex])
                    buttonText = flagNames[flagIndex];

                if ((mask & flagValue) == flagValue)
                {

                    intermediateMask |= flagValue;
                    usedFlags.Add(flagIndex);
                }
            }

            if (buttonText == null)
            {
                if (flagMask == intermediateMask)
                {
                    // If all of the available flags are set then show the Everything name.
                    buttonText = everythingName;
                }
                else if (intermediateMask == 0)
                {
                    // If the mask is 0 or none of the actual flags are set we use the nothing name.
                    buttonText = nothingName;
                }
                else
                {
                    buttonText = "Mixed...";

                    // Extract mixed labels
                    var sb = GenericPool<StringBuilder>.Get();
                    sb.Append(flagNames[usedFlags[0]]);
                    for (int i = 1; i < usedFlags.Count; ++i)
                    {
                        sb.Append(", ");
                        sb.Append(flagNames[usedFlags[i]]);
                    }
                    buttonMixedValuesText = sb.ToString();

                    sb.Clear();
                    GenericPool<StringBuilder>.Release(sb);
                }
            }

            ListPool<int>.Release(usedFlags);
        }

        internal static GUIContent DoMixedLabel(string label, string mixedLabel, Rect rect, GUIStyle style)
        {
            if (mixedLabel == null)
                return EditorGUIUtility.TempContent(label);

            var content = EditorGUIUtility.TempContent(mixedLabel);
            var size = style.CalcSize(content);

            // If the label is too large to fit then revert back to the shorter label
            if (size.x > rect.width)
                content = EditorGUIUtility.TempContent(label);

            return content;
        }
        internal static void CalculateMaskValues(int mask, int[] flagValues, ref int[] optionMaskValues)
        {
            uint selectedValue = (uint)mask;

            var flagStartIndex = 0;
            if (flagValues[0] == 0)
                flagStartIndex++;
            if (flagValues.Length > 1 && flagValues[1] == -1)
                flagStartIndex++;

            if (mask == ~0)
            {
                uint allLayersMask = 0;
                for (var flagIndex = flagStartIndex; flagIndex < flagValues.Length; flagIndex++)
                {
                    allLayersMask |= (uint)flagValues[flagIndex];
                }

                selectedValue = allLayersMask;
            }

            var flagEndIndex = flagStartIndex + optionMaskValues.Length - 2;

            for (var flagIndex = flagStartIndex; flagIndex < flagEndIndex; flagIndex++)
            {
                uint flagValue = (uint)flagValues[flagIndex];

                bool flagSet = ((selectedValue & flagValue) == flagValue);

                optionMaskValues[flagIndex-flagStartIndex+2] = (int)(flagSet ? selectedValue & ~flagValue : selectedValue | flagValue);
            }
        }

        internal static void GetMenuOptions(int mask, string[] flagNames, int[] flagValues,
            out string buttonText, out string buttonMixedValuesText, out string[] optionNames, out int[] optionMaskValues, out int[] selectedOptions, Type enumType = null)
        {
            const int everythingValue = ~0;
            bool hasNothingName = flagValues[0] == 0;
            bool hasEverythingName = flagValues[flagValues.Length - 1] == everythingValue;

            var nothingName = (hasNothingName ? flagNames[0] : "Nothing");
            var everythingName = (hasEverythingName ? flagNames[flagValues.Length - 1] : "Everything");

            var optionCount = flagNames.Length + (hasNothingName ? 0 : 1) + (hasEverythingName ? 0 : 1);
            var flagCount = flagNames.Length - (hasNothingName ? 1 : 0) - (hasEverythingName ? 1 : 0);

            // These indices refer to flags that are not 0 and everythingValue
            var flagStartIndex = (hasNothingName ? 1 : 0);
            var flagEndIndex = flagStartIndex + flagCount;

            // Options names
            optionNames = GetBuffer(s_OptionNames, optionCount);
            optionNames[0] = nothingName;
            optionNames[1] = everythingName;
            for (var flagIndex = flagStartIndex; flagIndex < flagEndIndex; flagIndex++)
            {
                var optionIndex = flagIndex - flagStartIndex + 2;
                optionNames[optionIndex] = flagNames[flagIndex];
            }

            var flagMask = 0; // Disjunction of all flags (except 0 and everythingValue)
            var intermediateMask = 0; // Mask used to compute new mask value for each options

            // Selected options
            s_SelectedOptionsSet.Clear();
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

            // Button text
            buttonText = null;
            buttonMixedValuesText = null;
            for (var flagIndex = flagStartIndex; flagIndex < flagEndIndex; flagIndex++)
            {
                // Check if a specific value is set.
                if (mask == flagValues[flagIndex])
                {
                    buttonText = flagNames[flagIndex];
                }
            }

            if (buttonText == null)
            {
                if (flagMask == intermediateMask)
                {
                    // If all of the available flags are set then show the Everything name.
                    s_SelectedOptionsSet.Add(1);
                    buttonText = everythingName;
                }
                else if (mask == 0 || s_SelectedOptionsSet.Count == 0)
                {
                    // If the mask is 0 or none of the actual flags are set we use the nothing name.
                    s_SelectedOptionsSet.Add(0);
                    buttonText = nothingName;
                }
                else
                {
                    buttonText = "Mixed...";

                    // Extract mixed labels
                    var sb = GenericPool<StringBuilder>.Get();

                    var iterator = s_SelectedOptionsSet.GetEnumerator();
                    iterator.MoveNext();

                    sb.Append(flagNames[iterator.Current + flagStartIndex - 2]);
                    while(iterator.MoveNext())
                    {
                        sb.Append(", ");
                        sb.Append(flagNames[iterator.Current + flagStartIndex - 2]);
                    }
                    buttonMixedValuesText = sb.ToString();

                    sb.Clear();
                    GenericPool<StringBuilder>.Release(sb);
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
            optionMaskValues[1] = everythingValue;
            if (EditorGUI.showMixedValue)
                intermediateMask = 0;

            CalculateMaskValues(intermediateMask, flagValues, ref optionMaskValues);
        }
    }
}
