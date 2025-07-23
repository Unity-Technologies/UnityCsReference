// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

internal class OpenManifestAction : PackageAction
{
    private readonly IIOProxy m_IOProxy;
    private readonly ISelectionProxy m_SelectionProxy;
    private readonly IAssetDatabaseProxy m_AssetDatabaseProxy;

    public OpenManifestAction(IIOProxy ioProxy, ISelectionProxy selectionProxy, IAssetDatabaseProxy AssetDatabaseProxy)
    {
        m_IOProxy = ioProxy;
        m_SelectionProxy = selectionProxy;
        m_AssetDatabaseProxy = AssetDatabaseProxy;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        var path = m_IOProxy.PathsCombine("Packages", version.name, "package.json");
        var folderObject = m_AssetDatabaseProxy.LoadAssetAtPath<Object>(path);
        if (folderObject == null)
            return false;
        m_SelectionProxy.activeObject = folderObject;
        var inspectorWindow = EditorWindow.GetWindow<InspectorWindow>();
        if (inspectorWindow.isLocked)
        {
            var newInspectorWindow = EditorWindow.CreateWindow<InspectorWindow>();
            newInspectorWindow.Show(true);
        }
        else
            inspectorWindow.Show(true);
        PackageManagerWindowAnalytics.SendEvent("openManifest", version);
        return true;
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    public override bool IsVisible(IPackageVersion version) => !version.HasTag(PackageTag.LegacyFormat | PackageTag.BuiltIn | PackageTag.Feature) && version.isInstalled;

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Select manifest in project browser and open inspector.");
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return version.HasTag(PackageTag.Custom | PackageTag.Local) ? L10n.Tr("Edit Manifest") : L10n.Tr("Open Manifest");
    }
}
