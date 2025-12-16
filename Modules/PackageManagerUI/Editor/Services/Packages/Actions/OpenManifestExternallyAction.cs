// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

internal class OpenManifestExternallyAction : PackageAction
{
    private readonly IIOProxy m_IOProxy;

    public OpenManifestExternallyAction(IIOProxy ioProxy)
    {
        m_IOProxy = ioProxy;
    }

    private bool TryOpenManifest(IPackageVersion version)
    {
        var path = m_IOProxy.PathsCombine(version.localPath, "package.json");
        if (InternalEditorUtility.OpenFileAtLineExternal(path, 1, 0))
            return true;

        Debug.LogError($"[PackageManagerWindow] Could not open package manifest {path} externally. Please check that the file exists and that your script editor is set up correctly.");
        return false;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        if (!TryOpenManifest(version))
            return false;

        PackageManagerWindowAnalytics.SendEvent("openManifestExternally", version);
        return true;
    }

    protected override bool TriggerActionImplementation(IReadOnlyCollection<IPackage> packages)
    {
        bool result = true;
        var openedVersions = new List<IPackageVersion>();
        foreach (var package in packages)
        {
            result = TryOpenManifest(package.versions.primary) && result;
            openedVersions.Add(package.versions.primary);
        }

        if (!result)
            return false;

        PackageManagerWindowAnalytics.SendEvent("openManifestExternally", openedVersions);
        return true;
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    public override bool IsVisible(IPackageVersion version) => version.isInstalled && !version.HasTag(PackageTag.LegacyFormat | PackageTag.BuiltIn | PackageTag.Feature);

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Open package manifest to view or edit in your script editor.");
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return version.HasTag(PackageTag.Custom | PackageTag.Local) ? L10n.Tr("Edit Manifest Externally") : L10n.Tr("Open Manifest Externally");
    }

    public override string GetMultiSelectText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Open Manifest Externally");
    }
}
