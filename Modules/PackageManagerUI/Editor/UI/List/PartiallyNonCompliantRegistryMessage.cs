// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class PartiallyNonCompliantRegistryMessage : VisualElement
{
    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        public override object CreateInstance()
        {
            var container = ServicesContainer.instance;
            return new PartiallyNonCompliantRegistryMessage(
                container.Resolve<IApplicationProxy>(),
                container.Resolve<IPageManager>());
        }
    }

    private readonly ExtendedHelpBox m_HelpBox;

    private readonly IPageManager m_PageManager;
    public PartiallyNonCompliantRegistryMessage(IApplicationProxy applicationProxy, IPageManager pageManager)
    {
        m_PageManager = pageManager;

        m_HelpBox = new ExtendedHelpBox(applicationProxy)
        {
            customIcon = Icon.RegistryErrorLarge,
            analyticsId = "partially-non-compliant-registry-help-box"
        };
        Add(m_HelpBox);

        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        m_PageManager.onSelectionChanged += OnPageSelectionChange;
        Refresh(m_PageManager.activePage);
    }

    private void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        m_PageManager.onSelectionChanged -= OnPageSelectionChange;
    }

    private void OnPageSelectionChange(PageSelectionChangeArgs args)
    {
        Refresh(args.page);
    }

    private void Refresh(IPage page)
    {
        UIUtils.SetElementDisplay(this, false);
        var isVisible = page.isActive && page.scopedRegistry?.compliance.status == RegistryComplianceStatus.PartiallyNonCompliant;
        if (!isVisible)
            return;

        var violation = page.scopedRegistry.compliance.violations[0];
        m_HelpBox.text = string.Format(L10n.Tr("Certain restricted packages may not be visible in the registry. {0}"), violation?.message ?? string.Empty);
        m_HelpBox.readMoreUrl = violation?.readMoreLink;
        UIUtils.SetElementDisplay(this, true);
    }
}
