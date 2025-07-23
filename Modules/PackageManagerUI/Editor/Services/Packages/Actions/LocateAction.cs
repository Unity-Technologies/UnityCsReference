// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class LocateAction : PackageAction
{
    private readonly IIOProxy m_IOProxy;
    private readonly IApplicationProxy m_ApplicationProxy;

    public LocateAction(IIOProxy ioProxy, IApplicationProxy applicationProxy)
    {
        m_ApplicationProxy = applicationProxy;
        m_IOProxy = ioProxy;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        var path = m_IOProxy.PathsCombine("Packages", version.name, "package.json");
        if (!m_ApplicationProxy.PingObjectInProjectBrowser(path))
            return false;
        PackageManagerWindowAnalytics.SendEvent("locate", version);
        return true;
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    public override bool IsVisible(IPackageVersion version) => !version.HasTag(PackageTag.LegacyFormat | PackageTag.BuiltIn | PackageTag.Feature) && version.isInstalled;

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Show the package’s manifest file in the Project window.");
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Locate");
    }
}
