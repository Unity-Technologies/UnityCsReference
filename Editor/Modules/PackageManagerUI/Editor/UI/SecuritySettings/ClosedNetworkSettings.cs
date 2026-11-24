// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ClosedNetworkSettings : VisualElement
    {
        internal static readonly string k_EnableLabel = L10n.Tr("<b>Closed network environment.</b> Check to stop signature validation for packages in this project.", null);
        internal static readonly string k_DisableLabel = L10n.Tr("<b>Closed network environment.</b> Uncheck to resume signature validation for packages in this project.", null);
        internal static readonly string k_NoEntitlementLabel = L10n.Tr("<b>Closed network environment.</b> Signature validation is enabled because your license doesn't permit disabling it.", null);

        private readonly IProjectSettingsProxy m_SettingsProxy;

        public ClosedNetworkSettings(IResourceLoader resourceLoader, IProjectSettingsProxy settingsProxy)
        {
            m_SettingsProxy = settingsProxy;

            var root = resourceLoader.GetTemplate("ClosedNetworkSettings.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            closedNetworkToggle.RegisterValueChangedCallback(OnClosedNetworkToggleChanged);
            cancelButton.clickable.clicked += OnCancelClicked;
            applyButton.clickable.clicked += OnApplyClicked;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_SettingsProxy.onClosedNetworkEnabledChanged += OnClosedNetworkEnabledChanged;
            Refresh();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_SettingsProxy.onClosedNetworkEnabledChanged -= OnClosedNetworkEnabledChanged;
        }

        private void OnClosedNetworkEnabledChanged(bool closedNetworkEnabled)
        {
            ResetDraft();
        }

        private void OnClosedNetworkToggleChanged(ChangeEvent<bool> changeEvent)
        {
            m_SettingsProxy.closedNetworkEnabledDraft = changeEvent.newValue;
            Refresh();
        }

        private void OnCancelClicked()
        {
            ResetDraft();
        }

        private void OnApplyClicked()
        {
            var newClosedNetworkEnabled = m_SettingsProxy.closedNetworkEnabledDraft;
            if (newClosedNetworkEnabled)
                m_SettingsProxy.ClearTrustPolicyLevelDraft();

            m_SettingsProxy.closedNetworkEnabled = newClosedNetworkEnabled;
        }

        private void ResetDraft()
        {
            m_SettingsProxy.ClearClosedNetworkEnabledDraft();
            Refresh();
        }

        private void Refresh()
        {
            var closedNetworkEnabled = m_SettingsProxy.closedNetworkEnabled;
            var hasBypassPackageTrustEntitlement = m_SettingsProxy.hasBypassPackageTrustEntitlement;
            UIUtils.SetElementDisplay(this, hasBypassPackageTrustEntitlement || closedNetworkEnabled);

            if (!hasBypassPackageTrustEntitlement)
            {
                closedNetworkToggle.label = k_NoEntitlementLabel;
                closedNetworkToggle.SetValueWithoutNotify(closedNetworkEnabled);
                closedNetworkToggle.SetEnabled(false);
                UIUtils.SetElementDisplay(buttonsRow, false);
                return;
            }

            var closedNetworkEnabledDraft = m_SettingsProxy.closedNetworkEnabledDraft;
            closedNetworkToggle.label = closedNetworkEnabled ? k_DisableLabel : k_EnableLabel;
            closedNetworkToggle.SetValueWithoutNotify(closedNetworkEnabledDraft);
            closedNetworkToggle.SetEnabled(true);
            UIUtils.SetElementDisplay(buttonsRow, closedNetworkEnabledDraft != closedNetworkEnabled);
        }

        private VisualElementCache cache { get; }

        private Toggle closedNetworkToggle => cache.Get<Toggle>("closedNetworkToggle");
        private VisualElement buttonsRow => cache.Get<VisualElement>("closedNetworkButtonsRow");
        private Button cancelButton => cache.Get<Button>("closedNetworkCancelButton");
        private Button applyButton => cache.Get<Button>("closedNetworkApplyButton");
    }
}
