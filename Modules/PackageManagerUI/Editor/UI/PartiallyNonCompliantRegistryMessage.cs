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
        public override object CreateInstance() => new PartiallyNonCompliantRegistryMessage();
    }

    private HelpBoxWithOptionalReadMore m_PartiallyNonCompliantHelpBox;
    private IPageManager m_PageManager;

    public PartiallyNonCompliantRegistryMessage()
    {
        ResolveDependencies();
        m_PartiallyNonCompliantHelpBox = new HelpBoxWithOptionalReadMore { messageType = HelpBoxMessageType.Error };
        Add(m_PartiallyNonCompliantHelpBox);
    }

    private void ResolveDependencies()
    {
        var container = ServicesContainer.instance;
        m_PageManager = container.Resolve<IPageManager>();
    }

    public void OnEnable()
    {
        m_PageManager.onSelectionChanged += OnPageSelectionChange;
        Refresh(m_PageManager.activePage);
    }

    public void OnDisable()
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
        var isVisible = page.isActivePage && page.scopedRegistry?.compliance.status == RegistryComplianceStatus.PartiallyNonCompliant;
        if (!isVisible)
            return;

        var violation = page.scopedRegistry.compliance.violations[0];
        m_PartiallyNonCompliantHelpBox.text = string.Format(L10n.Tr("The provider must revise this registry to comply with Unity's Terms of Service. Certain restricted packages may not be visible in the registry. Contact the provider for further assistance. {0}"), violation?.message ?? string.Empty);
        m_PartiallyNonCompliantHelpBox.readMoreUrl = violation?.readMoreLink;
        UIUtils.SetElementDisplay(this, true);
    }
}
