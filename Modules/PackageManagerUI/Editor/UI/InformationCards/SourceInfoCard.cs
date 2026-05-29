// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class SourceInfoCard : PackageInformationCard
{
    protected override string titleText => L10n.Tr("Source");
    protected override InformationCardSize cardSize => InformationCardSize.Small;

    private readonly IUpmCache m_UpmCache;
    public SourceInfoCard(IUpmCache upmCache)
    {
        m_UpmCache = upmCache;
    }

    public override void Refresh(IPackageVersion version)
    {
        icon = Icon.None;
        iconTooltip = string.Empty;
        string source;
        string registryLink;
        var verifiedIconTooltip = L10n.Tr("This package comes from a verified source.");
        var packageInfo = m_UpmCache.GetBestMatchPackageInfo(version.name, version.isInstalled, version.versionString);

        // Package info may be null if UI is refreshed mid-package generation.
        // Safe to ignore—the UI refreshes again after generation completes.
        if (version.isFromUnity)
        {
            source = L10n.Tr("Unity Technologies");
            registryLink = packageInfo?.registry?.url;
            icon = Icon.Verified;
            iconTooltip = verifiedIconTooltip;
        }
        else if (version.isFromAssetStore)
        {
            source = L10n.Tr("Asset Store");
            registryLink = "https://assetstore.unity.com/";
            icon = Icon.Verified;
            iconTooltip = verifiedIconTooltip;
        }
        else if (packageInfo?.source == PackageSource.Registry)
        {
            source = packageInfo.registry?.name;
            registryLink = packageInfo.registry?.url;
        }
        else
        {
            source = packageInfo?.source.GetDisplayName();
            registryLink = string.Empty;
        }

        var isVisible = !string.IsNullOrEmpty(source);
        UIUtils.SetElementDisplay(this, isVisible);
        if (!isVisible)
            return;

        contentText = source;
        contentTooltip = registryLink;
    }
}
