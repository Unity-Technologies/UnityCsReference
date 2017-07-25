// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#pragma warning disable 618 // disable obsolete warning

namespace UnityEditor
{
    public partial class EditorGUI
    {
        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskField(Rect position, Enum enumValue)
        {
            return EnumMaskField(position, enumValue, EditorStyles.popup);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskField(Rect position, Enum enumValue, GUIStyle style)
        {
            return EnumMaskFieldInternal(position, enumValue, style);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskField(Rect position, string label, Enum enumValue)
        {
            return EnumMaskField(position, label, enumValue, EditorStyles.popup);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskField(Rect position, string label, Enum enumValue, GUIStyle style)
        {
            return EnumMaskFieldInternal(position, EditorGUIUtility.TempContent(label), enumValue, style);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskField(Rect position, GUIContent label, Enum enumValue)
        {
            return EnumMaskField(position, label, enumValue, EditorStyles.popup);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskField(Rect position, GUIContent label, Enum enumValue, GUIStyle style)
        {
            return EnumMaskFieldInternal(position, label, enumValue, style);
        }

        [Obsolete("EnumMaskPopup has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskPopup(Rect position, string label, Enum selected)
        {
            return EnumMaskPopup(position, label, selected, EditorStyles.popup);
        }

        [Obsolete("EnumMaskPopup has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskPopup(Rect position, string label, Enum selected, GUIStyle style)
        {
            int changedFlags;
            bool changedToValue;
            return EnumMaskPopup(position, label, selected, out changedFlags, out changedToValue, style);
        }

        [Obsolete("EnumMaskPopup has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskPopup(Rect position, GUIContent label, Enum selected)
        {
            return EnumMaskPopup(position, label, selected, EditorStyles.popup);
        }

        [Obsolete("EnumMaskPopup has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskPopup(Rect position, GUIContent label, Enum selected, GUIStyle style)
        {
            int changedFlags;
            bool changedToValue;
            return EnumMaskPopup(position, label, selected, out changedFlags, out changedToValue, style);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        static Enum EnumMaskField(Rect position, GUIContent label, Enum enumValue, GUIStyle style, out int changedFlags, out bool changedToValue)
        {
            return DoEnumMaskField(position, label, enumValue, style, out changedFlags, out changedToValue);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        static Enum EnumMaskFieldInternal(Rect position, Enum enumValue, GUIStyle style)
        {
            Type enumType = enumValue.GetType();
            if (!enumType.IsEnum)
                throw new ArgumentException("Parameter enumValue must be of type System.Enum", "enumValue");

            var names = Enum.GetNames(enumType).Select(ObjectNames.NicifyVariableName).ToArray();
            int flags = MaskFieldGUIDeprecated.DoMaskField(
                    IndentedRect(position),
                    GUIUtility.GetControlID(s_MaskField, FocusType.Keyboard, position),
                    Convert.ToInt32(enumValue),
                    names, style);
            return IntToEnumFlags(enumType, flags);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        static Enum EnumMaskFieldInternal(Rect position, GUIContent label, Enum enumValue, GUIStyle style)
        {
            Type enumType = enumValue.GetType();
            if (!enumType.IsEnum)
                throw new ArgumentException("Parameter enumValue must be of type System.Enum", "enumValue");

            var id = GUIUtility.GetControlID(s_MaskField, FocusType.Keyboard, position);
            var position2 = EditorGUI.PrefixLabel(position, id, label);
            position.xMax = position2.x;

            var names = Enum.GetNames(enumType).Select(ObjectNames.NicifyVariableName).ToArray();
            int flags = MaskFieldGUIDeprecated.DoMaskField(position2, id, Convert.ToInt32(enumValue), names, style);
            return IntToEnumFlags(enumType, flags);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        static Enum DoEnumMaskField(Rect position, GUIContent label, Enum enumValue, GUIStyle style, out int changedFlags, out bool changedToValue)
        {
            var enumType = enumValue.GetType();
            if (!enumType.IsEnum)
                throw new ArgumentException("Parameter enumValue must be of type System.Enum", "enumValue");

            var id = GUIUtility.GetControlID(s_MaskField, FocusType.Keyboard, position);
            var names = Enum.GetNames(enumType).Select(ObjectNames.NicifyVariableName).ToArray();
            int flags = MaskFieldGUIDeprecated.DoMaskField(
                    PrefixLabel(position, id, label),
                    id,
                    Convert.ToInt32(enumValue),
                    names, style, out changedFlags, out changedToValue);
            return IntToEnumFlags(enumType, flags);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        static Enum EnumMaskPopup(Rect position, string label, Enum selected, out int changedFlags, out bool changedToValue, GUIStyle style)
        {
            return EnumMaskPopup(position, EditorGUIUtility.TempContent(label), selected, out changedFlags, out changedToValue, style);
        }

        [Obsolete("EnumMaskPopup has been deprecated. Use EnumFlagsField instead.")]
        internal static Enum EnumMaskPopup(Rect position, GUIContent label, Enum selected, out int changedFlags, out bool changedToValue, GUIStyle style)
        {
            return EnumMaskPopupInternal(position, label, selected, out changedFlags, out changedToValue, style);
        }

        // Make an enum mask popup selection field.
        [Obsolete("EnumMaskPopup has been deprecated. Use EnumFlagsField instead.")]
        static Enum EnumMaskPopupInternal(Rect position, GUIContent label, Enum selected, out int changedFlags, out bool changedToValue, GUIStyle style)
        {
            return EnumMaskField(position, label, selected, style, out changedFlags, out changedToValue);
        }
    }

    public partial class EditorGUILayout
    {
        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskField(Enum enumValue, params GUILayoutOption[] options)
        {
            return EnumMaskField(enumValue, EditorStyles.popup, options);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskField(Enum enumValue, GUIStyle style, params GUILayoutOption[] options)
        {
            var r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.EnumMaskField(r, enumValue, style);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskField(string label, Enum enumValue, params GUILayoutOption[] options)
        {
            return EnumMaskField(label, enumValue, EditorStyles.popup, options);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskField(string label, Enum enumValue, GUIStyle style, params GUILayoutOption[] options)
        {
            var r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.EnumMaskField(r, label, enumValue, style);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskField(GUIContent label, Enum enumValue, params GUILayoutOption[] options)
        {
            return EnumMaskField(label, enumValue, EditorStyles.popup, options);
        }

        [Obsolete("EnumMaskField has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskField(GUIContent label, Enum enumValue, GUIStyle style, params GUILayoutOption[] options)
        {
            var r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.EnumMaskField(r, label, enumValue, style);
        }

        [Obsolete("EnumMaskPopup has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskPopup(string label, Enum selected, params GUILayoutOption[] options)
        {
            return EnumMaskPopup(label, selected, EditorStyles.popup, options);
        }

        [Obsolete("EnumMaskPopup has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskPopup(string label, Enum selected, GUIStyle style, params GUILayoutOption[] options)
        {
            int changedFlags;
            bool changedToValue;
            return EnumMaskPopup(EditorGUIUtility.TempContent(label), selected, out changedFlags, out changedToValue, style, options);
        }

        [Obsolete("EnumMaskPopup has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskPopup(GUIContent label, Enum selected, params GUILayoutOption[] options)
        {
            return EnumMaskPopup(label, selected, EditorStyles.popup, options);
        }

        [Obsolete("EnumMaskPopup has been deprecated. Use EnumFlagsField instead.")]
        public static Enum EnumMaskPopup(GUIContent label, Enum selected, GUIStyle style, params GUILayoutOption[] options)
        {
            int changedFlags;
            bool changedToValue;
            return EnumMaskPopup(label, selected, out changedFlags, out changedToValue, style, options);
        }

        // Make an enum popup selection field for a bitmask.
        [Obsolete("EnumMaskPopup has been deprecated. Use EnumFlagsField instead.")]
        static Enum EnumMaskPopup(GUIContent label, Enum selected, out int changedFlags, out bool changedToValue, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.EnumMaskPopup(r, label, selected, out changedFlags, out changedToValue, style);
        }
    }

    // Class for storing state for mask menus so we can get the info back to OnGUI from the user selection
    [Obsolete("MaskFieldGUIDeprecated is deprecated. Use MaskFieldGUI instead.")]
    static class MaskFieldGUIDeprecated
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

            // Which item was selected
            private int m_Mask;

            //Flags to control all / nothing buttons
            private bool m_SetAll;
            private bool m_ClearAll;
            private bool m_DoNothing;

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
                        if (!m_Instance.m_DoNothing)
                        {
                            if (m_Instance.m_ClearAll)
                            {
                                mask = 0;
                                changedFlags = ~0;
                                changedToValue = false;
                            }
                            else if (m_Instance.m_SetAll)
                            {
                                mask = ~0;
                                changedFlags = ~0;
                                changedToValue = true;
                            }
                            else
                            {
                                mask ^= m_Instance.m_Mask;
                                changedFlags = m_Instance.m_Mask;
                                changedToValue = (mask & m_Instance.m_Mask) != 0;
                            }

                            GUI.changed = true;
                        }
                        m_Instance.m_DoNothing = false;
                        m_Instance.m_ClearAll = false;
                        m_Instance.m_SetAll = false;
                        m_Instance = null;
                        evt.Use();
                    }
                }
                return mask;
            }

            internal void SetMaskValueDelegate(object userData, string[] options, int selected)
            {
                switch (selected)
                {
                    case 0:
                        m_ClearAll = true;
                        break;
                    case 1:
                        m_SetAll = true;
                        break;
                    default:
                        m_Mask = ((int[])userData)[selected - 2];
                        break;
                }

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
            var selectedFlags = new List<int>();
            var fullFlagNames = new List<string> {"Nothing", "Everything"};

            for (var i = 0; i < flagNames.Length; i++)
            {
                if ((mask & flagValues[i]) != 0)
                    selectedFlags.Add(i + 2);
            }

            fullFlagNames.AddRange(flagNames);

            GUIContent buttonContent = EditorGUI.mixedValueContent;
            if (!EditorGUI.showMixedValue)
            {
                switch (selectedFlags.Count)
                {
                    case 0:
                        buttonContent = EditorGUIUtility.TempContent("Nothing");
                        selectedFlags.Add(0);
                        break;
                    case 1:
                        buttonContent = new GUIContent(fullFlagNames[selectedFlags[0]]);
                        break;
                    default:
                        if (selectedFlags.Count >= flagNames.Length)
                        {
                            buttonContent = EditorGUIUtility.TempContent("Everything");
                            selectedFlags.Add(1);
                            // When every available item is selected, we force to ~0 to keep the mask int value consistent
                            // between the cases where all items are individually selected vs. user clicks "everything"
                            mask = ~0;
                        }
                        else
                            buttonContent = EditorGUIUtility.TempContent("Mixed ...");
                        break;
                }
            }
            Event evt = Event.current;
            if (evt.type == EventType.Repaint)
            {
                style.Draw(position, buttonContent, controlID, false);
            }
            else if ((evt.type == EventType.MouseDown && position.Contains(evt.mousePosition)) || evt.MainActionKeyForControl(controlID))
            {
                MaskCallbackInfo.m_Instance = new MaskCallbackInfo(controlID);
                evt.Use();
                EditorUtility.DisplayCustomMenu(position, fullFlagNames.ToArray(),
                    // Only show selections if we are not multi-editing
                    EditorGUI.showMixedValue ? new int[] {} : selectedFlags.ToArray(),
                    MaskCallbackInfo.m_Instance.SetMaskValueDelegate, flagValues);
                EditorGUIUtility.keyboardControl = controlID;
            }
            return mask;
        }
    }
}

#pragma warning restore 618 // restore obsolete warning
