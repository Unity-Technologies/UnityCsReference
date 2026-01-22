// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class OriginalUnityVersionInfoCard : PackageInformationCard
{
    protected override string titleText => L10n.Tr("Original Unity Version");
    protected override InformationCardSize cardSize => InformationCardSize.Small;

    public override void Refresh(IPackageVersion version)
    {
        var hasManySupportedVersions = version.supportedVersions?.Count > 0;
        var supportedVersion = hasManySupportedVersions ? version.supportedVersions[0] : version.supportedVersion;
        var isVisible = supportedVersion != null;
        UIUtils.SetElementDisplay(this, isVisible);
        if (!isVisible)
            return;

        contentText = string.Format(L10n.Tr("Unity {0}"), supportedVersion);

        var tooltipText = supportedVersion.ToString();
        if (hasManySupportedVersions)
        {
            var versions = new string[version.supportedVersions.Count];
            for (var i = 0; i < version.supportedVersions.Count; i++)
                versions[i] = version.supportedVersions[i].ToString();

            tooltipText = versions.Length == 1
                ? versions[0]
                : string.Format(L10n.Tr("{0} and {1} to improve compatibility with the range of these versions of Unity"),
                    string.Join(", ", versions, 0, versions.Length - 1), versions[versions.Length - 1]);
        }
        contentTooltip = string.Format(L10n.Tr("Package has been submitted using Unity {0}"), tooltipText);
    }
}
