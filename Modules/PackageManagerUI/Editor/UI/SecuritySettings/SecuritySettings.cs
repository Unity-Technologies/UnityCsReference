// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class SecuritySettings : VisualElement
    {
        private static readonly TrustPolicyLevel[] k_AdvancedTrustPolicyLevels = { TrustPolicyLevel.Signed, TrustPolicyLevel.AnyPackage };
        private const TrustPolicyLevel k_DefaultTrustPolicyLevel = TrustPolicyLevel.TrustedOnly;

        private readonly IProjectSettingsProxy m_SettingsProxy;
        private readonly IApplicationProxy m_ApplicationProxy;

        private TrustPolicyLevel m_LastSelectedSignatureOption = TrustPolicyLevel.Signed;

        private static bool IsDefaultPolicy(TrustPolicyLevel trustPolicyLevel) => trustPolicyLevel == k_DefaultTrustPolicyLevel;

        public SecuritySettings() : this(
            ServicesContainer.instance.Resolve<IResourceLoader>(),
            ServicesContainer.instance.Resolve<IProjectSettingsProxy>(),
            ServicesContainer.instance.Resolve<IApplicationProxy>())
        {
        }

        public SecuritySettings(IResourceLoader resourceLoader, IProjectSettingsProxy settingsProxy, IApplicationProxy applicationProxy)
        {
            m_SettingsProxy = settingsProxy;
            m_ApplicationProxy = applicationProxy;

            var root = resourceLoader.GetTemplate("SecuritySettings.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            foreach (var trustPolicyLevel in k_AdvancedTrustPolicyLevels)
            {
                var radioButton = new RadioButton(GetSignatureOptionLabel(trustPolicyLevel)) { name = trustPolicyLevel.ToString(), userData = trustPolicyLevel };
                radioButton.RegisterValueChangedCallback(changeEvent =>
                {
                    if (!changeEvent.newValue)
                        return;
                    m_SettingsProxy.trustPolicyLevelDraft = trustPolicyLevel;
                    Refresh();
                });
                signatureOptions.Add(radioButton);
            }

            advancedToggle.RegisterValueChangedCallback(OnAdvancedToggleChanged);
            cancelButton.clickable.clicked += OnCancelClicked;
            applyButton.clickable.clicked += OnApplyClicked;
            learnMoreButton.clickable.clicked += OnLearnMoreClicked;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_SettingsProxy.onTrustPolicyLevelChanged += OnTrustPolicyLevelChanged;
            Refresh();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_SettingsProxy.onTrustPolicyLevelChanged -= OnTrustPolicyLevelChanged;
        }

        private void OnTrustPolicyLevelChanged(TrustPolicyLevel trustPolicyLevel)
        {
            ResetDraft();
        }

        private void OnAdvancedToggleChanged(ChangeEvent<bool> changeEvent)
        {
            if (changeEvent.newValue)
            {
                if (IsDefaultPolicy(m_SettingsProxy.trustPolicyLevelDraft))
                    m_SettingsProxy.trustPolicyLevelDraft = m_LastSelectedSignatureOption;
            }
            else
            {
                m_LastSelectedSignatureOption = m_SettingsProxy.trustPolicyLevelDraft;
                m_SettingsProxy.trustPolicyLevelDraft = k_DefaultTrustPolicyLevel;
            }

            Refresh();
        }

        private void OnCancelClicked()
        {
            ResetDraft();
        }

        private void OnApplyClicked()
        {
            m_SettingsProxy.trustPolicyLevel = m_SettingsProxy.trustPolicyLevelDraft;
            m_SettingsProxy.ClearTrustPolicyLevelDraft();
            PackageManagerWindowAnalytics.SendEvent(GetAnalyticsAction(m_SettingsProxy.trustPolicyLevel));
        }

        private void OnLearnMoreClicked()
        {
            // Tech writer confirmed to reuse the signature page until a dedicated security-settings page is created
            var url = $"https://docs.unity3d.com/{m_ApplicationProxy.shortUnityVersion}/Documentation/Manual/upm-signature.html";
            m_ApplicationProxy.OpenURL(url);
            PackageManagerReadMoreClickedAnalytics.SendEvent("security-settings-read-more", url);
        }

        private static string GetAnalyticsAction(TrustPolicyLevel trustPolicyLevel)
        {
            return trustPolicyLevel switch
            {
                TrustPolicyLevel.Signed => "securitySettingSigned",
                TrustPolicyLevel.AnyPackage => "securitySettingAnyPackage",
                TrustPolicyLevel.TrustedOnly => "securitySettingTrustedOnly",
                _ => string.Empty,
            };
        }

        private static string GetSignatureOptionLabel(TrustPolicyLevel trustPolicyLevel)
        {
            return trustPolicyLevel switch
            {
                TrustPolicyLevel.Signed => L10n.Tr("Valid Signatures", null),
                TrustPolicyLevel.AnyPackage => L10n.Tr("Any Signature Status", null),
                _ => string.Empty
            };
        }

        private void ResetDraft()
        {
            m_SettingsProxy.ClearTrustPolicyLevelDraft();
            Refresh();
        }

        private void Refresh()
        {
            var isAdvancedEnabled = !IsDefaultPolicy(m_SettingsProxy.trustPolicyLevelDraft);
            advancedToggle.SetValueWithoutNotify(isAdvancedEnabled);

            var hasUnsavedChanges = m_SettingsProxy.trustPolicyLevelDraft != m_SettingsProxy.trustPolicyLevel;
            var showAdvancedContent = isAdvancedEnabled || hasUnsavedChanges;
            UIUtils.SetElementDisplay(advancedOptionsContainer, showAdvancedContent);
            UIUtils.SetElementDisplay(buttonsRow, showAdvancedContent);

            signatureOptions.SetEnabled(isAdvancedEnabled);
            foreach (var child in signatureOptions.Children())
                if (child is RadioButton radioButton)
                    radioButton.SetValueWithoutNotify((TrustPolicyLevel)radioButton.userData == m_SettingsProxy.trustPolicyLevelDraft);

            UIUtils.SetElementDisplay(anySignatureWarning, m_SettingsProxy.trustPolicyLevelDraft == TrustPolicyLevel.AnyPackage);

            applyButton.SetEnabled(hasUnsavedChanges);
            cancelButton.SetEnabled(hasUnsavedChanges);
        }

        private VisualElementCache cache { get; }

        private Toggle advancedToggle => cache.Get<Toggle>("advancedToggle");
        private VisualElement advancedOptionsContainer => cache.Get<VisualElement>("advancedOptionsContainer");
        private GroupBox signatureOptions => cache.Get<GroupBox>("signatureOptions");
        private HelpBox anySignatureWarning => cache.Get<HelpBox>("anySignatureWarning");
        private VisualElement buttonsRow => cache.Get<VisualElement>("buttonsRow");
        private Button learnMoreButton => cache.Get<Button>("learnMoreButton");
        private Button cancelButton => cache.Get<Button>("cancelButton");
        private Button applyButton => cache.Get<Button>("applyButton");
    }
}
