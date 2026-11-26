// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.InsightsEditor;

internal class TrText
{
    internal const int k_InsightsLinkTagServicesDiagnosticsDescriptionLinkId = 0;
    internal const int k_InsightsLinkTagServicesBuildProfilesLinkId = 1;
    internal const int k_InsightsLinkTagNoCloudLinkId = 2;
    internal const int k_InsightsLinkTagInfoBoxDiagnosticsDocsLinkId = 3;
    internal const int k_InsightsLinkTagInfoBoxLearnMoreLinkId = 4;
    internal const int k_InsightsLinkTagServicesDeveloperDataFrameworkLinkId = 5;

    internal static readonly string k_BuildProfileNoCloudLabel = L10n.Tr(
        $"Diagnostic Data Collection Disabled (Requires <link={k_InsightsLinkTagNoCloudLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>linking to Cloud Project</u></color></link>)", null);

    internal static readonly string k_DataReportingLevelDropdownLabel = L10n.Tr("Diagnostic Data Collection", null);
    internal static readonly string k_DataReportingLevelDropdownTooltip = L10n.Tr("Diagnostic data is Developer Data that Unity collects on your behalf at runtime to improve game performance, stability, and compatibility. You can access it from your project in the Unity Dashboard.", null);
    internal static readonly string k_EngineDiagnosticsDropdownEnabled = L10n.Tr("Enabled", null);
    internal static readonly string k_EngineDiagnosticsStateDropdownDisabled = L10n.Tr("Disabled", null);

    internal static readonly string k_ServicesDiagnosticsTitle = L10n.Tr("Diagnostics", null);
    internal static readonly string k_ServicesDiagnosticsDescription = L10n.Tr(
        $"Diagnostic data is <link={k_InsightsLinkTagServicesDiagnosticsDescriptionLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>Developer Data</u></color></link> that Unity collects on your behalf at runtime to improve game performance, stability, and compatibility. This includes crash logs, ANRs, error traces, and other telemetry Unity uses to detect and resolve issues. You can access Diagnostics from your project in the Unity Dashboard.", null);
    internal static readonly string k_ServicesDiagnosticsDescriptionLink = L10n.Tr("https://docs.unity.com/cloud/en-us/developer-data-framework/what-is-developer-data", null);
    internal static readonly string k_ServicesDiagnosticsBuildProfiles = L10n.Tr(
        $"To enable or disable diagnostic data collection for each build, use the Diagnostic Data Collection setting in the <link={k_InsightsLinkTagServicesBuildProfilesLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>Build Profiles</u></color></link> window.", null);
    internal static readonly string k_ServicesDeveloperDataFramework = L10n.Tr(
        $"The <link={k_InsightsLinkTagServicesDeveloperDataFrameworkLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>Developer Data framework</u></color></link> is Unity’s approach to how data is collected, managed, and used across the Unity ecosystem. It gives you control over what data Unity collects on your behalf and how your data is used across products, services, and systems.", null);
    internal static readonly string k_ServicesDeveloperDataFrameworkLink = L10n.Tr("https://docs.unity.com/cloud/en-us/developer-data-framework/overview", null);

    // Describes text block with multiple clickable elements. First one proposes expected order, rest is substitutions for tokens.
    internal static readonly string k_InsightsInfoBoxRichText = L10n.Tr($"<link={k_InsightsLinkTagInfoBoxDiagnosticsDocsLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>Diagnostic Data Collection</u></color></link> is enabled for this build profile and collects data (including device information) for <link={k_InsightsLinkTagInfoBoxLearnMoreLinkId}><color={EditorGUIUtility.GetHyperlinkColorForSkin()}><u>Unity’s Developer Data framework</u></color></link>.", null);
    internal static readonly string k_InsightsInfoBoxDiagnosticsDocsUrl = L10n.Tr("https://docs.unity.com/en-us/cloud/developer-data/diagnostics", null);
    internal static readonly string k_InsightsInfoBoxLearnMoreUrl = L10n.Tr("https://docs.unity.com/cloud/en-us/developer-data-framework/overview", null);
    internal static readonly string k_InsightsInfoBoxButtonText = L10n.Tr("Dismiss", null);
}
