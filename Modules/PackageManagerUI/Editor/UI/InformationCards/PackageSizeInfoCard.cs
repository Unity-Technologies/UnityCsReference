// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class PackageSizeInfoCard : PackageInformationCard
{
    protected override string titleText => L10n.Tr("Package Size");
    protected override InformationCardSize cardSize => InformationCardSize.Small;

    public override void Refresh(IPackageVersion version)
    {
        var isVisible = version.sizes?.Count > 0;
        UIUtils.SetElementDisplay(this, isVisible);
        if (!isVisible)
            return;

        PackageSizeInfo sizeInfo = null;
        foreach (var info in version.sizes)
        {
            if (info.supportedUnityVersion == version.supportedVersion)
            {
                sizeInfo = info;
                break;
            }
        }
        sizeInfo ??= version.sizes[version.sizes.Count - 1];

        contentText = string.Format(L10n.Tr("{0} ({1} files)"), UIUtils.ConvertToHumanReadableSize(sizeInfo.downloadSize), sizeInfo.assetCount);
    }
}
