// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
