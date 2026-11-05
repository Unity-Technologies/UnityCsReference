// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityEditor.InsightsEditor;

[VisibleToOtherModules]
internal partial class InsightsEditorUtils
{
    // EditorPrefs are shared with older editors, whose "InsightsInfoBoxDismissed" key must not hide this banner
    public const string k_InsightsInfoBoxDismissedFlagKey = "DiagnosticsDataCollectionInfoBoxDismissed";
    public const string k_InsightsStyleSheetPath = "Insights/StyleSheets/ServicesWindow/InsightsSettings.uss";

    public const string k_UssClass_LinkCursor = "link-cursor";

    public static bool IsInsightsInfoBoxDismissed() =>
        EditorPrefs.GetBool(k_InsightsInfoBoxDismissedFlagKey, false);

    public static void SetInsightsInfoBoxDismissed(bool dismissed) =>
        EditorPrefs.SetBool(k_InsightsInfoBoxDismissedFlagKey, dismissed);

    public static void DrawInsightsInfoBox(VisualElement root, BuildProfileInsightsSettingsVisualElement settings)
    {
        var styleSheet = EditorGUIUtility.LoadRequired(k_InsightsStyleSheetPath) as StyleSheet;
        if (styleSheet == null)
        {
            throw new FileNotFoundException($"Could not find USS file at path: {k_InsightsStyleSheetPath}");
        }

        var infoBox = root.Q<HelpBox>("insights-info-box");
        infoBox.styleSheets.Add(styleSheet);

        infoBox.buttonText = TrText.k_InsightsInfoBoxButtonText;
        infoBox.onButtonClicked += () =>
        {
            SetInsightsInfoBoxDismissed(true);
            UpdateVisibility();
        };

        infoBox.RegisterCallback<PointerDownLinkTagEvent>(OnInfoBoxLinkClicked);
        infoBox.RegisterCallback<PointerOverLinkTagEvent>(OnLabelLinkPointerOver);
        infoBox.RegisterCallback<PointerOutLinkTagEvent>(OnLabelLinkPointerOut);
        infoBox.text = TrText.k_InsightsInfoBoxRichText;

        UpdateVisibility();
        settings.dataCollectionChanged += UpdateVisibility;

        return;

        void UpdateVisibility()
        {
            infoBox.EnableInClassList("display-none", IsInsightsInfoBoxDismissed() || !settings.isDataCollectionEnabled);
        }

        void OnInfoBoxLinkClicked(PointerDownLinkTagEvent evt)
        {
            var linkId = int.Parse(evt.linkID);
            switch (linkId)
            {
                case TrText.k_InsightsLinkTagInfoBoxDiagnosticsDocsLinkId:
                    Application.OpenURL(TrText.k_InsightsInfoBoxDiagnosticsDocsUrl);
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
