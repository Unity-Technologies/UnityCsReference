// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityEditor.InsightsEditor;

static class ServicesDiagnosticsSection
{
    const string k_UxmlPath = "Insights/UXML/ServicesWindow/ServicesDiagnosticsSection.uxml";
    const string k_UssPath = "Insights/StyleSheets/ServicesWindow/ServicesDiagnosticsSection.uss";

    const string k_TitleNodeName = "services-diagnostics-title";
    const string k_DescriptionNodeName = "services-diagnostics-description";
    const string k_BuildProfilesNodeName = "services-diagnostics-build-profiles";
    const string k_DeveloperDataFrameworkNodeName = "services-diagnostics-developer-data-framework";

    internal static void AddTo(VisualElement parent)
    {
        var visualTree = (VisualTreeAsset)EditorGUIUtility.LoadRequired(k_UxmlPath);
        var section = visualTree.CloneTree();
        section.styleSheets.Add((StyleSheet)EditorGUIUtility.LoadRequired(k_UssPath));

        section.Q<Label>(k_TitleNodeName).text = TrText.k_ServicesDiagnosticsTitle;
        SetLinkText(section.Q<Label>(k_DescriptionNodeName), TrText.k_ServicesDiagnosticsDescription);
        SetLinkText(section.Q<Label>(k_BuildProfilesNodeName), TrText.k_ServicesDiagnosticsBuildProfiles);

        var developerDataFrameworkInfoBox = section.Q<HelpBox>(k_DeveloperDataFrameworkNodeName);
        developerDataFrameworkInfoBox.text = TrText.k_ServicesDeveloperDataFramework;
        RegisterLinkCallbacks(developerDataFrameworkInfoBox);

        parent.Add(section);
    }

    static void SetLinkText(Label label, string text)
    {
        label.text = text;
        RegisterLinkCallbacks(label);
    }

    static void RegisterLinkCallbacks(VisualElement element) =>
        InsightsEditorUtils.RegisterLinkTagEventCallbacks(
            element, OnLinkClicked, InsightsEditorUtils.OnLabelLinkPointerOver, InsightsEditorUtils.OnLabelLinkPointerOut);

    static void OnLinkClicked(PointerDownLinkTagEvent evt)
    {
        switch (int.Parse(evt.linkID))
        {
            case TrText.k_InsightsLinkTagServicesDiagnosticsDescriptionLinkId:
                Application.OpenURL(TrText.k_ServicesDiagnosticsDescriptionLink);
                break;
            case TrText.k_InsightsLinkTagServicesBuildProfilesLinkId:
                BuildPipeline.ShowBuildProfileWindow();
                break;
            case TrText.k_InsightsLinkTagServicesDeveloperDataFrameworkLinkId:
                Application.OpenURL(TrText.k_ServicesDeveloperDataFrameworkLink);
                break;
        }
    }
}
