// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class DiagnosticSwitchPreferences : SettingsProvider
    {
        private static class Styles
        {
            public static Texture2D smallWarningIcon;
            public static GUIContent restartNeededWarning = EditorGUIUtility.TrTextContent("Some settings will not take effect until you restart Unity.");
            public static GUIStyle boldFoldout;

            static Styles()
            {
                smallWarningIcon = EditorGUIUtility.LoadIconRequired("console.warnicon.sml");
                boldFoldout = new GUIStyle(EditorStyles.foldout) {fontStyle = FontStyle.Bold};
            }
        }

        class SwitchGroup
        {
            public string name;
            public DiagnosticSwitch[] switches;
            public bool foldout;
            public bool HasAnyChangedValues => switches.Any(s => !s.isSetToDefault);
            public bool HasAnyUnappliedValues => switches.Any(s => s.needsRestart);
        }

        private const uint kMaxRangeForSlider = 10;

        private List<SwitchGroup> m_Switches;

        private bool m_HasAcceptedWarning;

        public DiagnosticSwitchPreferences()
            : base("Preferences/Diagnostics", SettingsScope.User)
        {
        }

        public override bool HasSearchInterest(string searchContext)
        {
            foreach (var diagSwitch in Debug.diagnosticSwitches)
            {
                if (PassesFilter(diagSwitch, searchContext))
                    return true;
            }
            return false;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var switches = Debug.diagnosticSwitches;

            // If any switch has been configured, assume that the user already previously saw the warning, and don't get
            // in their way if they are looking to reset it
            m_HasAcceptedWarning = switches.Any(s => !s.isSetToDefault);

            m_Switches = switches
                .GroupBy(s => s.owningModule)
                .Select(group => new SwitchGroup
                {
                    name = group.Key,
                    switches = group.OrderBy(s => s.name).ToArray(),
                    foldout = group.Any(s => !s.isSetToDefault)
                })
                .OrderBy(group => group.name)
                .ToList();

            if (!m_HasAcceptedWarning)
            {
                VisualTreeAsset warningPanel = (VisualTreeAsset)EditorGUIUtility.LoadRequired("UXML/DiagnosticsPreferences/WarningPanel.uxml");
                warningPanel.CloneTree(rootElement);
                rootElement.Q<Button>("ShowSettings").clicked += () =>
                {
                    m_HasAcceptedWarning = true;
                    rootElement.Clear();

                    EditorWindow.GetWindow<PreferenceSettingsWindow>().SetupIMGUIForCurrentProviderIfNeeded();
                };
            }
        }

        public override void OnTitleBarGUI()
        {
            using (new EditorGUI.DisabledGroupScope(m_Switches.All(group => !group.HasAnyChangedValues)))
            {
                if (GUILayout.Button("Reset all"))
                {
                    foreach (var diagnosticSwitch in m_Switches.SelectMany(group => group.switches))
                        diagnosticSwitch.persistentValue = diagnosticSwitch.defaultValue;
                    DiagnosticSwitchesConsoleMessage.Instance.Update();
                }
            }
        }

        public override void OnGUI(string searchContext)
        {
            using (new SettingsWindow.GUIScope())
            {
                foreach (var group in m_Switches)
                {
                    group.foldout = EditorGUILayout.Foldout(group.foldout, String.IsNullOrEmpty(group.name) ? "General" : group.name,
                        group.HasAnyChangedValues ? Styles.boldFoldout : EditorStyles.foldout);

                    if (group.foldout)
                    {
                        foreach (var diagnosticSwitch in group.switches)
                        {
                            DisplaySwitch(diagnosticSwitch);
                            EditorGUILayout.Space(EditorGUI.kControlVerticalSpacing);
                            EditorGUIUtility.SetBoldDefaultFont(false);
                        }
                    }

                    EditorGUILayout.Space(EditorGUI.kControlVerticalSpacing.value * 1.2f);
                }
            }
        }

        public override void OnFooterBarGUI()
        {
            var helpBox = GUILayoutUtility.GetRect(Styles.restartNeededWarning, EditorStyles.helpBox, GUILayout.MinHeight(40));
            if (m_Switches.Any(group => group.HasAnyUnappliedValues))
                EditorGUI.HelpBox(helpBox, Styles.restartNeededWarning.text, MessageType.Warning);
        }

        private static bool PassesFilter(DiagnosticSwitch diagnosticSwitch, string filterString)
        {
            return string.IsNullOrEmpty(filterString)
                || SearchUtils.MatchSearchGroups(filterString, diagnosticSwitch.name);
        }

        private void DisplaySwitch(DiagnosticSwitch diagnosticSwitch)
        {
            var labelText = new GUIContent(diagnosticSwitch.name, diagnosticSwitch.description);

            var rowRect = GUILayoutUtility.GetRect(0, EditorGUI.kSingleLineHeight, GUILayout.ExpandWidth(true));
            rowRect.xMax -= GUISkin.current.verticalScrollbar.fixedWidth + 5;
            var iconScaleFactor = EditorGUI.kSingleLineHeight / Styles.smallWarningIcon.height;
            var warningRect = new Rect(rowRect.x, rowRect.y, Styles.smallWarningIcon.width * iconScaleFactor, Styles.smallWarningIcon.height * iconScaleFactor);
            rowRect.x += warningRect.width + 2;
            if (diagnosticSwitch.needsRestart && Event.current.type == EventType.Repaint)
                GUI.DrawTexture(warningRect, Styles.smallWarningIcon);

            var resetButtonSize = EditorStyles.miniButton.CalcSize(GUIContent.Temp("Reset"));
            var resetButtonRect = new Rect(rowRect.xMax - resetButtonSize.x, rowRect.y, resetButtonSize.x,
                resetButtonSize.y);
            rowRect.xMax -= resetButtonSize.x + 2;
            if (!diagnosticSwitch.isSetToDefault)
            {
                if (GUI.Button(resetButtonRect, "Reset", EditorStyles.miniButton))
                {
                    diagnosticSwitch.persistentValue = diagnosticSwitch.defaultValue;
                    DiagnosticSwitchesConsoleMessage.Instance.Update();
                }
            }
            else
            {
                // Reserve an ID for the 'reset' button so that if the user begins typing in a text-valued switch, the
                // button showing up doesn't cause the focus to change
                GUIUtility.GetControlID("Button".GetHashCode(), FocusType.Passive, resetButtonRect);
            }

            EditorGUIUtility.SetBoldDefaultFont(!diagnosticSwitch.persistentValue.Equals(diagnosticSwitch.defaultValue));

            EditorGUI.BeginChangeCheck();

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
            else if (diagnosticSwitch.value is float)
            {
                diagnosticSwitch.persistentValue = EditorGUI.FloatField(rowRect, labelText, (float)diagnosticSwitch.persistentValue);
            }
            else
            {
                var redStyle = new GUIStyle();
                redStyle.normal.textColor = Color.red;
                EditorGUI.LabelField(rowRect, labelText, EditorGUIUtility.TrTextContent("Unsupported type: " + diagnosticSwitch.value.GetType().Name), redStyle);
            }

            if (EditorGUI.EndChangeCheck())
                DiagnosticSwitchesConsoleMessage.Instance.Update();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateDiagnosticProvider()
        {
            // Diagnostic switches might be turned off in the build,
            // in which case there will be none of them -- don't
            // create the preference pane then.
            return Debug.diagnosticSwitches.Length != 0 ? new DiagnosticSwitchPreferences() : null;
        }
    }
}
