// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.AssetPackage;

internal interface IUtilityAdapter
{
    public ExportPackageItem[] BuildExportPackageItemsListWithPackageManagerWarning(string[] guids, bool includeDependencies, bool warnPackageManagerDependencies);
    public void ExportPackageWithGUIDs(string[] guids, string fileName, string ownerOrgId);
}

internal class UtilityAdapter : IUtilityAdapter
{
    public ExportPackageItem[] BuildExportPackageItemsListWithPackageManagerWarning(string[] guids, bool includeDependencies, bool warnPackageManagerDependencies) =>
        Utility.BuildExportPackageItemsListWithPackageManagerWarning(guids, includeDependencies, warnPackageManagerDependencies);

    public void ExportPackageWithGUIDs(string[] guids, string fileName, string ownerOrgId) => Utility.ExportPackageWithGUIDs(guids, fileName, ownerOrgId);
}
