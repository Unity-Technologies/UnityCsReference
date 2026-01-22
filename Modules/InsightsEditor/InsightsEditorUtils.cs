// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityEditor.InsightsEditor;

[VisibleToOtherModules]
internal class InsightsEditorUtils
{
    public const string k_ProjectSettingsInfoBoxNodeName = "insights-project-settings-info";

    public const string k_InsightsInfoBoxDismissedFlagKey = "InsightsInfoBoxDismissed";
    public const string k_InsightsStyleSheetPath = "Insights/StyleSheets/ServicesWindow/InsightsSettings.uss";

    public const string k_UssClass_LinkCursor = "link-cursor";

    public static event Action<bool> OnEngineDiagnosticsEnabledChanged;

    public static void NotifyEngineDiagnosticsSettingsChanged(bool enabled)
    {
        OnEngineDiagnosticsEnabledChanged?.Invoke(enabled);
    }

    public static bool IsInsightsInfoBoxDismissed() =>
        EditorPrefs.GetBool(k_InsightsInfoBoxDismissedFlagKey, false);

    public static void SetInsightsInfoBoxDismissed(bool dismissed) =>
        EditorPrefs.SetBool(k_InsightsInfoBoxDismissedFlagKey, dismissed);

    public static void DrawInsightsInfoBox(VisualElement root)
    {
        var styleSheet = EditorGUIUtility.LoadRequired(k_InsightsStyleSheetPath) as StyleSheet;
        if (styleSheet == null)
        {
            throw new FileNotFoundException($"Could not find USS file at path: {k_InsightsStyleSheetPath}");
        }

        var infoBox = root.Q<HelpBox>("insights-info-box");
        infoBox.RemoveFromClassList("display-none");
        infoBox.styleSheets.Add(styleSheet);

        infoBox.buttonText = TrText.k_InsightsInfoBoxButtonText;
        infoBox.onButtonClicked += () =>
        {
            infoBox.AddToClassList("display-none");
            SetInsightsInfoBoxDismissed(true);
        };

        infoBox.RegisterCallback<PointerDownLinkTagEvent>(OnInfoBoxLinkClicked);
        infoBox.RegisterCallback<PointerOverLinkTagEvent>(OnLabelLinkPointerOver);
        infoBox.RegisterCallback<PointerOutLinkTagEvent>(OnLabelLinkPointerOut);
        infoBox.text = TrText.k_InsightsInfoBoxRichText;

        return;

        void OnInfoBoxLinkClicked(PointerDownLinkTagEvent evt)
        {
            var linkId = int.Parse(evt.linkID);
            switch (linkId)
            {
                case TrText.k_InsightsLinkTagInfoBoxProjectSettingsLinkId:
                    SettingsService.OpenProjectSettings("Project/Services/Diagnostics");
                    return;
                case TrText.k_InsightsLinkTagInfoBoxLearnMoreLinkId:
                    Application.OpenURL(TrText.k_InsightsInfoBoxLearnMoreUrl);
                    return;
            }
        }
    }

    public static void OnLabelLinkPointerOver(PointerOverLinkTagEvent evt) =>
        ((VisualElement)evt.target).AddToClassList(k_UssClass_LinkCursor);


    public static void OnLabelLinkPointerOut(PointerOutLinkTagEvent evt) =>
        ((VisualElement)evt.target).RemoveFromClassList(k_UssClass_LinkCursor);

    public static void RegisterLinkTagEventCallbacks(VisualElement node,
        EventCallback<PointerDownLinkTagEvent> onLinkClicked,
        EventCallback<PointerOverLinkTagEvent> onLabelLinkPointerOver,
        EventCallback<PointerOutLinkTagEvent> onLabelLinkPointerOut)
    {
        node.RegisterCallback(onLinkClicked);
        node.RegisterCallback(onLabelLinkPointerOver);
        node.RegisterCallback(onLabelLinkPointerOut);
    }
}
