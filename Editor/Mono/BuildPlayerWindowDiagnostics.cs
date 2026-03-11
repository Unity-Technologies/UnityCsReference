// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.EngineDiagnostics;
using UnityEditor.InsightsEditor;
using UnityEditor.InsightsEditor.EditorAnalytics;
using UnityEditor.Modules;
using UnityEditor.Connect;

namespace UnityEditor
{
    public partial class BuildPlayerWindow : EditorWindow
    {
        class DiagnosticStyles
        {
            public GUIContent diagnosticTitle = EditorGUIUtility.TrTextContent(TrText.k_BuildProfileSectionLabel);
            public GUIContent diagnosticLabel = EditorGUIUtility.TrTextContent(TrText.k_DataReportingLevelDropdownLabel, "");

            public DiagnosticSetting[] diagnosticSettings =
            {
                DiagnosticSetting.ProjectSettings,
                DiagnosticSetting.Disabled,
                DiagnosticSetting.Enabled
            };

            public GUIContent diagnosticSettingDefaultDisabled = EditorGUIUtility.TrTextContent(TrText.k_EngineDiagnosticsStateDropdownDisabled + " " + TrText.k_DataReportingLevelDropdownUseProjectSettingsGeneric);
            public GUIContent diagnosticSettingDefaultEnabled = EditorGUIUtility.TrTextContent(TrText.k_EngineDiagnosticsDropdownEnabled + " " + TrText.k_DataReportingLevelDropdownUseProjectSettingsGeneric);

            public GUIContent[] diagnosticSettingsStrings =
            {
                EditorGUIUtility.TrTextContent(""), // Updated at runtime based on 'Project Setting'
                EditorGUIUtility.TrTextContent(TrText.k_EngineDiagnosticsStateDropdownDisabled),
                EditorGUIUtility.TrTextContent(TrText.k_EngineDiagnosticsDropdownEnabled)
            };
        }

        private static DiagnosticStyles diagnosticStyles;
        private static bool projectSettingValue;

        private static void OnProjectSettingsEngineDiagnosticsEnabledChanged(bool projectSettingsValue)
        {
            projectSettingValue = projectSettingsValue;
        }

        private static void OnCloudProjectStateChanged(ProjectInfo _)
        {
            var windows = Resources.FindObjectsOfTypeAll<BuildPlayerWindow>();
            if (windows.Length > 0)
                windows[0].Repaint();
        }

        private static void GUIDiagnosticData(IBuildWindowExtension buildWindowExtension, NamedBuildTarget namedBuildTarget)
        {
            if (diagnosticStyles == null)
            {
                diagnosticStyles = new DiagnosticStyles();
                projectSettingValue = EngineDiagnostics.EngineDiagnosticsSettings.GetEngineDiagnosticsEnabledDefaultBuildValue();
                InsightsEditorUtils.OnEngineDiagnosticsEnabledChanged += OnProjectSettingsEngineDiagnosticsEnabledChanged;
            }

            if (buildWindowExtension.ShouldShowDiagnosticsDataOption())
            {
                UnityConnect.instance.ProjectStateChanged -= OnCloudProjectStateChanged;
                UnityConnect.instance.ProjectStateChanged += OnCloudProjectStateChanged;

                GUIDrawDiagnosticsSection(namedBuildTarget);
            }
        }

        private static void GUIDrawDiagnosticsSection(NamedBuildTarget namedBuildTarget)
        {
            if(CanDrawInsightsSettings())
            {
                GUIEngineDiagnosticsSettings(namedBuildTarget);
                return;
            }

            GUIConnectToCloudLink();
        }

        private static void GUIConnectToCloudLink()
        {
            GUILayout.Space(20);
            GUILayout.Label(diagnosticStyles.diagnosticTitle, styles.title);

            RenderTextWithLinks(TrText.k_BuildProfileNoCloudLabel, (linkId) =>
            {
                if (linkId == TrText.k_InsightsLinkTagNoCloudLinkId)
                {
                    SettingsService.OpenProjectSettings("Project/Services");
                }
            });
        }

        private static void RenderTextWithLinks(string markedUpText, System.Action<int> onLinkClicked)
        {
            var segments = ParseMarkupText(markedUpText);

            GUILayout.BeginHorizontal();

            GUILayout.Space(5);

            foreach (var segment in segments)
            {
                if (segment.isLink)
                {
                    var linkStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = segment.color },
                        hover = { textColor = segment.color * 1.2f },
                        richText = true,
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0)
                    };

                    var content = new GUIContent(segment.text);
                    var rect = GUILayoutUtility.GetRect(content, linkStyle, GUILayout.ExpandWidth(false));
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

                    if (GUI.Button(rect, content, linkStyle))
                    {
                        onLinkClicked?.Invoke(segment.linkId);
                    }
                }
                else
                {
                    var labelStyle = new GUIStyle(EditorStyles.label)
                    {
                        richText = true,
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0)
                    };
                    GUILayout.Label(segment.text, labelStyle, GUILayout.ExpandWidth(false));
                }
            }

            GUILayout.EndHorizontal();
        }

        private struct TextSegment
        {
            public string text;
            public bool isLink;
            public int linkId;
            public Color color;
        }

        private static System.Collections.Generic.List<TextSegment> ParseMarkupText(string markedUpText)
        {
            var segments = new System.Collections.Generic.List<TextSegment>();

            // Simple parser to extract text and link information
            var linkPattern = @"<link=(\d+)><color=[^>]+><u>([^<]+)</u></color></link>";
            var matches = System.Text.RegularExpressions.Regex.Matches(markedUpText, linkPattern);

            int lastIndex = 0;
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                // Add text before the link
                if (match.Index > lastIndex)
                {
                    segments.Add(new TextSegment
                    {
                        text = markedUpText.Substring(lastIndex, match.Index - lastIndex),
                        isLink = false
                    });
                }

                // Add the link segment
                segments.Add(new TextSegment
                {
                    text = match.Groups[2].Value,
                    isLink = true,
                    linkId = int.Parse(match.Groups[1].Value),
                    color = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.6f, 1f) : new Color(0f, 0f, 0.8f)
                });

                lastIndex = match.Index + match.Length;
            }

            // Add remaining text
            if (lastIndex < markedUpText.Length)
            {
                segments.Add(new TextSegment
                {
                    text = markedUpText.Substring(lastIndex),
                    isLink = false
                });
            }

            return segments;
        }

        private static void GUIEngineDiagnosticsSettings(NamedBuildTarget namedBuildTarget)
        {
            var prevDiagnosticIdx = Array.IndexOf(diagnosticStyles.diagnosticSettings, EditorUserBuildSettings.GetDiagnosticSetting(namedBuildTarget.ToBuildTargetGroup()));

            if (prevDiagnosticIdx == -1)
                prevDiagnosticIdx = 0;

            GUILayout.Space(20);
            GUILayout.Label(diagnosticStyles.diagnosticTitle, styles.title);

            if (projectSettingValue)
                diagnosticStyles.diagnosticSettingsStrings[0] = diagnosticStyles.diagnosticSettingDefaultEnabled;
            else
                diagnosticStyles.diagnosticSettingsStrings[0] = diagnosticStyles.diagnosticSettingDefaultDisabled;

            int newDiagnosticIdx = EditorGUILayout.Popup(diagnosticStyles.diagnosticLabel, prevDiagnosticIdx, diagnosticStyles.diagnosticSettingsStrings);

            if (ShouldShowDiagnosticsPopup(diagnosticStyles.diagnosticSettings[prevDiagnosticIdx], diagnosticStyles.diagnosticSettings[newDiagnosticIdx]))
            {
                DisablementPopup.ShowDisabledConfirmationDialog(
                    () => OnAcceptDisabledConfirmationDialog(diagnosticStyles.diagnosticSettings[newDiagnosticIdx]), OnCancelDisabledConfirmationDialog);
            }
            else
            {
                EditorUserBuildSettings.SetDiagnosticSetting(namedBuildTarget.ToBuildTargetGroup(), diagnosticStyles.diagnosticSettings[newDiagnosticIdx]);
            }
        }

        private static bool ShouldShowDiagnosticsPopup(DiagnosticSetting previousState, DiagnosticSetting newState)
        {
            var enabledToDisabledFlag =
                previousState == DiagnosticSetting.Enabled &&
                newState == DiagnosticSetting.Disabled;
            if (enabledToDisabledFlag)
            {
                return true;
            }

            var projectSettingsEnabled = projectSettingValue;

            var enabledToProjectSettingsDisabledFlag =
                previousState == DiagnosticSetting.Enabled &&
                newState == DiagnosticSetting.ProjectSettings &&
                !projectSettingsEnabled;
            if (enabledToProjectSettingsDisabledFlag)
            {
                return true;
            }

            var projectSettingsEnabledToDisabledFlag =
                previousState == DiagnosticSetting.ProjectSettings &&
                newState == DiagnosticSetting.Disabled &&
                projectSettingsEnabled;
            return projectSettingsEnabledToDisabledFlag;
        }

        private static void OnAcceptDisabledConfirmationDialog(DiagnosticSetting targetState)
        {
            NamedBuildTarget namedBuildTarget = EditorUserBuildSettingsUtils.CalculateSelectedNamedBuildTarget();
            EditorUserBuildSettings.SetDiagnosticSetting(namedBuildTarget.ToBuildTargetGroup(), targetState);
            LogDisablementDialogInteraction(InsightsEditorAnalytic.PopupInteraction.Accept);
        }

        private static void OnCancelDisabledConfirmationDialog()
        {
            LogDisablementDialogInteraction(InsightsEditorAnalytic.PopupInteraction.Cancel);
        }

        private static void LogDisablementDialogInteraction(InsightsEditorAnalytic.PopupInteraction interaction)
        {
            InsightsEditorAnalytic.LogAppInsights(new InsightsEditorAnalytic.InsightsEditorAnalyticsEvent
            {
                ActionType = InsightsEditorAnalytic.ActionType.DisablementPopupInteraction,
                disablementPopupInteraction = new InsightsEditorAnalytic.DisablementPopupInteraction
                {
                    PopupInteraction = interaction
                },
                interactionContext = new InsightsEditorAnalytic.InteractionContext
                {
                }
            });
        }

        private static bool CanDrawInsightsSettings()
        {
            return !string.IsNullOrEmpty(CloudProjectSettings.projectId);
        }
    }
}
