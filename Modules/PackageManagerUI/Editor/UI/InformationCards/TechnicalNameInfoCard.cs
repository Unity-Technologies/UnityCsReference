// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

internal class TechnicalNameInfoCard : PackageInformationCard
{
    [SerializeField]
    private CopyIconButton m_CopyIcon;

    protected override string titleText => L10n.Tr("Technical Name");
    protected override InformationCardSize cardSize => InformationCardSize.Medium;

    public TechnicalNameInfoCard(IApplicationProxy applicationProxy)
    {
        m_CopyIcon = new CopyIconButton(applicationProxy);
        m_Content.Add(m_CopyIcon);
    }

    public override void Refresh(IPackageVersion version)
    {
        // We use package.name instead of version.name because `version.name` would be empty for a PlaceholderPackageVersion
        var technicalName = version?.package?.name ?? string.Empty;
        var isVisible = !string.IsNullOrEmpty(technicalName);
        UIUtils.SetElementDisplay(this, isVisible);

        if (!isVisible)
            return;

        contentText = technicalName;
        m_CopyIcon.SetTextToCopy(technicalName);
    }
}
