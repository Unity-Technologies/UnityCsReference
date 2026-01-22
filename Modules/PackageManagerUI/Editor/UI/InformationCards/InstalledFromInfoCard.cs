// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

internal class InstalledFromInfoCard : PackageInformationCard
{
    private CopyIconButton m_CopyIcon;

    protected override string titleText => L10n.Tr("Installed From");
    protected override InformationCardSize cardSize => InformationCardSize.Large;

    public InstalledFromInfoCard()
    {
        m_CopyIcon = new CopyIconButton();
        m_Content.Add(m_CopyIcon);
    }

    public override void Refresh(IPackageVersion version)
    {
        var sourcePath = (version as UpmPackageVersion)?.sourcePath?.EscapeBackslashes();
        var isVisible = !string.IsNullOrEmpty(sourcePath);
        UIUtils.SetElementDisplay(this, isVisible);

        if (!isVisible)
            return;

        contentText = sourcePath;
        m_CopyIcon.SetTextToCopy(sourcePath);
    }
}
