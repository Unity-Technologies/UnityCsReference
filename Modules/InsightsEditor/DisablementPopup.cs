// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.InsightsEditor;

[VisibleToOtherModules]
internal class DisablementPopup : EditorWindow
{
    const string k_TemplatePath = "Insights/UXML/ServicesWindow/InsightsDisablementPopup.uxml";
    const string k_StylePath = "Insights/StyleSheets/ServicesWindow/InsightsDisablementPopup.uss";

    const string k_ButtonAgreeName = "ButtonAgree";
    const string k_ButtonCancelName = "ButtonCancel";
    const string k_LabelCtaNodeName = "DisablementCta";
    const string k_HelpBoxNodeName = "DisablementHelpBox";

    static readonly Vector2 k_DefaultWindowSize = new (412, 150);

    DisablementPopupData m_Data;

    public static void ShowDisabledConfirmationDialog(Action onAcceptCallback, Action onCancelCallback = null)
    {
        var windowData = new DisablementPopup.DisablementPopupData
        {
            titleText = TrText.k_DisabledWarningWindowTitle,
            helpBoxText = TrText.k_DisabledWarningHelpBoxText,
            acceptButtonText = TrText.k_DisabledWarningWindowAcceptButton,
            cancelButtonText = TrText.k_DisabledWarningWindowCancelButton,
            ctaText = TrText.k_DisabledWarningCtaText,
            onAccept = onAcceptCallback,
            onCancel = onCancelCallback
        };
        ShowWindow(windowData);
    }

    private static void ShowWindow(DisablementPopupData configData)
    {
        var parentWindow = focusedWindow;
        if (parentWindow != null)
        {
            ShowWindow(configData, k_DefaultWindowSize, parentWindow.position.center);
            return;
        }

        ShowWindow(configData, k_DefaultWindowSize);
    }

    private static void ShowWindow(DisablementPopupData configData, Vector2 windowSize, Vector2? windowPosition = null)
    {
        var window = CreateInstance<DisablementPopup>();

        window.titleContent = new GUIContent(configData.titleText);
        window.m_Data = configData;

        if (windowPosition != null)
        {
            window.position = new Rect((Vector2)windowPosition, k_DefaultWindowSize);
        }

        window.minSize = windowSize;
        window.maxSize = windowSize;

        window.ConfigureWindow();
        window.ShowUtility();
    }

    private void ConfigureWindow()
    {
        var dialogTemplate = EditorGUIUtility.Load(k_TemplatePath) as VisualTreeAsset;
        if (dialogTemplate == null)
        {
            throw new Exception($"Can't find UXML template for {typeof(DisablementPopup)}");
        }

        var styleSheet = EditorGUIUtility.Load(k_StylePath) as StyleSheet;
        if(styleSheet == null)
        {
            throw new Exception($"Can't find USS for {typeof(DisablementPopup)}");
        }

        var dialogContentContainer = dialogTemplate.Instantiate();
        dialogContentContainer.styleSheets.Add(styleSheet);

        rootVisualElement.Add(dialogContentContainer);

        var ctaLabel = rootVisualElement.Q<Label>(k_LabelCtaNodeName);
        ctaLabel.text = m_Data.ctaText;

        var helpBox = rootVisualElement.Q<HelpBox>(k_HelpBoxNodeName);
        helpBox.text = m_Data.helpBoxText;

        var agreeButton = rootVisualElement.Q<Button>(k_ButtonAgreeName);
        agreeButton.text = m_Data.acceptButtonText;
        agreeButton.clicked += OnAcceptClicked;

        var cancelButton = rootVisualElement.Q<Button>(k_ButtonCancelName);
        cancelButton.text = m_Data.cancelButtonText;
        cancelButton.clicked += OnCancelClicked;
    }

    private void OnAcceptClicked()
    {
        m_Data.onAccept?.Invoke();
        Close();
    }

    private void OnCancelClicked()
    {
        m_Data.onCancel?.Invoke();
        Close();
    }

    public struct DisablementPopupData
    {
        public Action onAccept;
        public Action onCancel;
        public string titleText;
        public string helpBoxText;
        public string ctaText;
        public string acceptButtonText;
        public string cancelButtonText;
    }
}
