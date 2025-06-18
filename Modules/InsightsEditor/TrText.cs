// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.InsightsEditor;

internal class TrText
{
    internal const int k_InsightsLinkTagMainDescriptionLinkId = 0;
    internal const int k_InsightsLinkTagDefaultSummaryLinkId = 1;
    internal const int k_InsightsLinkTagNoCloudLinkId = 2;
    internal const int k_InsightsLinkTagInfoBoxProjectSettingsLinkId = 3;
    internal const int k_InsightsLinkTagInfoBoxLearnMoreLinkId = 4;
    internal const int k_InsightsLinkTagProjectSettingsInfoBoxEnabledLinkId = 5;
    internal const int k_InsightsLinkTagProjectSettingsInfoBoxDisabledLinkId = 6;

    internal static readonly string k_BuildProfileSectionLabel = L10n.Tr("Diagnostics");
    internal static readonly string k_BuildProfileNoCloudLabel = L10n.Tr(
        $"Disabled (Requires <link={k_InsightsLinkTagNoCloudLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>Unity Cloud</u></color></link>)");

    internal static readonly string k_DataReportingLevelDropdownLabel = L10n.Tr("Diagnostics Data");
    internal static readonly string k_EngineDiagnosticsDropdownEnabled = L10n.Tr("Enabled");
    internal static readonly string k_EngineDiagnosticsStateDropdownDisabled = L10n.Tr("Disabled");
    internal static readonly string k_DataReportingLevelDropdownUseProjectSettingsPart1 = L10n.Tr("Use Project Settings");
    internal static readonly string k_DataReportingLevelDropdownUseProjectSettingsPart2 = L10n.Tr("Diagnostics");

    internal static readonly string k_ProjectSettingsDiagnosticsMainTitle = L10n.Tr("Diagnostics");
    internal static readonly string k_ProjectSettingsDiagnosticsMainDescription = L10n.Tr(
        $"Diagnostics is <link={k_InsightsLinkTagMainDescriptionLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>Developer Data</u></color></link> that Unity collects on your behalf at runtime to improve game performance, stability, and compatibility. This includes crash logs, ANRs, error traces, and other telemetry Unity uses to detect and resolve issues. You can access diagnostics from your project in the Unity Dashboard.");
    internal static readonly string k_ProjectSettingsDiagnosticsMainDescriptionLink = L10n.Tr("https://docs.unity.com/cloud/en-us/developer-data-framework/what-is-developer-data");

    internal static readonly string k_ProjectSettingsDiagnosticsDefaultSettings = L10n.Tr("Build Setting Default");
    internal static readonly string k_ProjectSettingsDiagnosticsDefaultSummary = L10n.Tr(
        $"This setting acts as the default for all build targets and build profiles unless manually overridden. When enabled, Unity collects Diagnostics Data for this project automatically and uses it in accordance with your <link={k_InsightsLinkTagDefaultSummaryLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>Developer Data settings</u></color></link>.");
    internal static readonly string k_ProjectSettingsDiagnosticsDefaultSummaryLink = L10n.Tr("https://docs.unity.com/cloud/en-us/developer-data-framework/how-the-framework-works");

    internal static readonly string k_ProjectSettingsDiagnosticsInfoTextEnabled = L10n.Tr(
        $"The <link={k_InsightsLinkTagProjectSettingsInfoBoxEnabledLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>Developer Data framework</u></color></link> is Unity’s approach to how data is collected, managed, and used across the Unity ecosystem. It gives you control over what data Unity collects on your behalf and how your data is used across products, services, and systems.");
    internal static readonly string k_ProjectSettingsDiagnosticsInfoTextEnabledLink = L10n.Tr("https://docs.unity.com/cloud/en-us/developer-data-framework/overview");
    internal static readonly string k_ProjectSettingsDiagnosticsInfoTextDisabled = L10n.Tr(
        $"<b>Warning: Developer Data collection OFF by default.</b> Consider enabling via your build settings. By disabling diagnostics data all collection of all other Developer Data will be disabled for your project. Any Unity products or services your organization uses that require Developer Data will no longer function correctly or at all.<br><br>The <link={k_InsightsLinkTagProjectSettingsInfoBoxDisabledLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>Developer Data framework</u></color></link> is Unity’s approach to how data is collected, managed, and used across the Unity ecosystem. It gives you control over what data Unity collects on your behalf and how your data is used across products, services, and systems.");
    internal static readonly string k_ProjectSettingsDiagnosticsInfoTextDisabledLink = L10n.Tr("https://docs.unity.com/cloud/en-us/developer-data-framework/overview");

    internal static readonly string k_DisabledWarningWindowTitle = L10n.Tr("Disable Diagnostics?");
    internal static readonly string k_DisabledWarningHelpBoxText = L10n.Tr("Disabling diagnostics can cause problems when other services are linked to Cloud Diagnostics, and prevent Unity from collecting essential data to improve the engine.");
    internal static readonly string k_DisabledWarningCtaText = L10n.Tr("Are you sure you want to disable Diagnostic Data?");
    internal static readonly string k_DisabledWarningWindowAcceptButton = L10n.Tr("Yes, disable Diagnostic Data");
    internal static readonly string k_DisabledWarningWindowCancelButton = L10n.Tr("Cancel");

    // Describes text block with multiple clickable elements. First one proposes expected order, rest is substitutions for tokens.
    internal static readonly string k_InsightsInfoBoxRichText = L10n.Tr($"In Unity 6.2 <link={k_InsightsLinkTagInfoBoxProjectSettingsLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>Diagnostics Data</u></color></link> is on by default for new projects and based on Unity’s Customer Data Framework. <link={k_InsightsLinkTagInfoBoxLearnMoreLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>Learn more</u></color></link>");
    internal static readonly string k_InsightsInfoBoxLearnMoreUrl = L10n.Tr("https://docs.unity.com/cloud/en-us/developer-data-framework/overview");
    internal static readonly string k_InsightsInfoBoxButtonText = L10n.Tr("Dismiss");
}
