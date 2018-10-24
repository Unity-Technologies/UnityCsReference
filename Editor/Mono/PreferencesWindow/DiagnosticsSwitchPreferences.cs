// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    internal static class DiagnosticSwitchPreferences
    {
        private const uint kMaxRangeForSlider = 10;

        private static class Resources
        {
            public static GUIStyle title = "OL Title";
            public static GUIStyle scrollArea = "OL Box";
            public static Texture2D smallWarningIcon;
            public static GUIContent restartNeededWarning = EditorGUIUtility.TrTextContent("Some settings will not take effect until you restart Unity.");

            static Resources()
            {
                smallWarningIcon = EditorGUIUtility.LoadIconRequired("console.warnicon.sml");
            }
        }

        private static bool PassesFilter(DiagnosticSwitch diagnosticSwitch, string filterString)
        {
            return string.IsNullOrEmpty(filterString)
                || SearchUtils.MatchSearchGroups(filterString, diagnosticSwitch.name);
        }

        private static bool DisplaySwitch(DiagnosticSwitch diagnosticSwitch)
        {
            var labelText = new GUIContent(diagnosticSwitch.name, diagnosticSwitch.name + "\n\n" + diagnosticSwitch.description);
            bool hasUnappliedValue = !System.Object.Equals(diagnosticSwitch.value, diagnosticSwitch.persistentValue);

            EditorGUI.BeginChangeCheck();

            var rowRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label, GUILayout.ExpandWidth(true));
            var warningRect = new Rect(rowRect.x, rowRect.y, rowRect.height, rowRect.height);

            if (hasUnappliedValue && Event.current.type == EventType.Repaint)
                GUI.DrawTexture(warningRect, Resources.smallWarningIcon);

            if (diagnosticSwitch.value is bool)
            {
                diagnosticSwitch.persistentValue = EditorGUI.Toggle(rowRect, labelText, (bool)diagnosticSwitch.persistentValue);
            }
            else if (diagnosticSwitch.enumInfo != null)
            {
                if (diagnosticSwitch.enumInfo.isFlags)
                {
                    // MaskField's "Everything" entry will set the value to 0xffffffff, but that might mean that it has set bits that
                    // are not actually valid in the enum. Correct for it by masking the value against the bit patterns that are actually valid.
                    int validMask = 0;
                    foreach (int value in diagnosticSwitch.enumInfo.values)
                        validMask |= value;

                    // Also, many enums will provide a 'Nothing' entry at the bottom. If so we need to chop it off, because MaskField can't cope with there being two 'Nothing' entries.
                    string[] names = diagnosticSwitch.enumInfo.names;
                    int[] values = diagnosticSwitch.enumInfo.values;
                    if (diagnosticSwitch.enumInfo.values[0] == 0)
                    {
                        names = new string[names.Length - 1];
                        values = new int[values.Length - 1];
                        Array.Copy(diagnosticSwitch.enumInfo.names, 1, names, 0, names.Length);
                        Array.Copy(diagnosticSwitch.enumInfo.values, 1, values, 0, values.Length);
                    }

                    diagnosticSwitch.persistentValue = EditorGUI.MaskFieldInternal(rowRect, labelText, (int)diagnosticSwitch.persistentValue, names, values, EditorStyles.popup) & validMask;
                }
                else
                {
                    var guiNames = new GUIContent[diagnosticSwitch.enumInfo.names.Length];
                    for (int i = 0; i < diagnosticSwitch.enumInfo.names.Length; ++i)
                        guiNames[i] = new GUIContent(diagnosticSwitch.enumInfo.names[i], diagnosticSwitch.enumInfo.annotations[i]);
                    diagnosticSwitch.persistentValue = EditorGUI.IntPopup(rowRect, labelText, (int)diagnosticSwitch.persistentValue, guiNames, diagnosticSwitch.enumInfo.values);
                }
            }
            else if (diagnosticSwitch.value is UInt32)
            {
                UInt32 minValue = (UInt32)diagnosticSwitch.minValue;
                UInt32 maxValue = (UInt32)diagnosticSwitch.maxValue;
                if ((maxValue - minValue <= kMaxRangeForSlider) &&
                    (maxValue - minValue > 0) &&
                    (minValue < int.MaxValue && maxValue < int.MaxValue))
                {
                    diagnosticSwitch.persistentValue = (UInt32)EditorGUI.IntSlider(rowRect, labelText, (int)(UInt32)diagnosticSwitch.persistentValue, (int)minValue, (int)maxValue);
                }
                else
                {
                    diagnosticSwitch.persistentValue = (UInt32)EditorGUI.IntField(rowRect, labelText, (int)(UInt32)diagnosticSwitch.persistentValue);
                }
            }
            else if (diagnosticSwitch.value is int)
            {
                int minValue = (int)diagnosticSwitch.minValue;
                int maxValue = (int)diagnosticSwitch.maxValue;
                if ((maxValue - minValue <= kMaxRangeForSlider) &&
                    (maxValue - minValue > 0) &&
                    (minValue < int.MaxValue && maxValue < int.MaxValue))
                {
                    diagnosticSwitch.persistentValue = EditorGUI.IntSlider(rowRect, labelText, (int)diagnosticSwitch.persistentValue, minValue, maxValue);
                }
                else
                {
                    diagnosticSwitch.persistentValue = EditorGUI.IntField(rowRect, labelText, (int)diagnosticSwitch.persistentValue);
                }
            }
            else if (diagnosticSwitch.value is string)
            {
                diagnosticSwitch.persistentValue = EditorGUI.TextField(rowRect, labelText, (string)diagnosticSwitch.persistentValue);
            }
            else
            {
                var redStyle = new GUIStyle();
                redStyle.normal.textColor = Color.red;
                EditorGUI.LabelField(rowRect, labelText, EditorGUIUtility.TrTextContent("Unsupported type: " + diagnosticSwitch.value.GetType().Name), redStyle);
            }

            if (EditorGUI.EndChangeCheck())
                Debug.SetDiagnosticSwitch(diagnosticSwitch.name, diagnosticSwitch.persistentValue, true);

            return hasUnappliedValue;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateDiagnosticProvider()
        {
            return new SettingsProvider("Preferences/Diagnostics", SettingsScope.User)
            {
                guiHandler = searchContext =>
                {
                    using (new SettingsWindow.GUIScope())
                        OnGUI(searchContext);
                },
                hasSearchInterestHandler = searchContext =>
                {
                    var switches = new List<DiagnosticSwitch>();
                    Debug.GetDiagnosticSwitches(switches);
                    foreach (var diagSwitch in switches)
                    {
                        if (PassesFilter(diagSwitch, searchContext))
                            return true;
                    }
                    return false;
                }
            };
        }

        private static void OnGUI(string searchContext)
        {
            List<DiagnosticSwitch> switches = new List<DiagnosticSwitch>();
            Debug.GetDiagnosticSwitches(switches);
            switches.Sort((a, b) => Comparer<string>.Default.Compare(a.name, b.name));

            bool hasAnyUnappliedSwitches = false;
            for (int i = 0; i < switches.Count; ++i)
            {
                hasAnyUnappliedSwitches |= DisplaySwitch(switches[i]);
            }

            var helpBox = GUILayoutUtility.GetRect(Resources.restartNeededWarning, EditorStyles.helpBox, GUILayout.MinHeight(40));
            if (hasAnyUnappliedSwitches)
                EditorGUI.HelpBox(helpBox, Resources.restartNeededWarning.text, MessageType.Warning);
        }
    }
}
