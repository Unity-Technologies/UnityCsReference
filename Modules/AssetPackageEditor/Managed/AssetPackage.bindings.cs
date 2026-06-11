// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;
using System.Runtime.InteropServices;

namespace UnityEditor.AssetPackage
{
#pragma warning disable 649
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [NativeHeader("Modules/AssetPackageEditor/AssetPackage.h")]
    [NativeAsStruct]
    internal class ImportPackageItem
    {
        public string existingAssetPath;
        public string exportedAssetPath;
        public string destinationAssetPath;
        public string sourceFolder;
        public string previewPath;
        public string guid;
        [NativeName("enabled")]
        public int enabledStatus;
        public bool isFolder;
        public bool exists;
        public bool assetChanged;
        public bool pathConflict;
        public bool projectAsset;
        public bool isRestricted;
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [NativeAsStruct]
    [NativeHeader("Modules/AssetPackageEditor/AssetPackage.h")]
    internal class ExportPackageItem
    {
        public string assetPath;
        public string guid;
        public bool isFolder;
        public int enabledStatus;
    }

    public static class Package
    {
        public static void Export(ExportPackageParameters parameters)
        {
            Utility.ExportPackage(parameters.AssetPathNames, parameters.FileName, parameters.OwnerOrgId, parameters.Flags);
        }

        public static void Import(string packagePath, bool interactive)
        {
            Utility.ImportPackage(packagePath, ImportPackageOptions.ImportDelayed | (interactive ? ImportPackageOptions.Default : ImportPackageOptions.NoGUI));
        }
    }

    [NativeHeader("Modules/AssetPackageEditor/AssetPackage.bindings.h")]
    internal class Utility
    {
        [FreeFunction("::ImportPackageWithOrigin")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern private static bool ImportPackageWithOrigin(string packagePath, AssetOrigin origin, ImportPackageOptions options);

        internal static void ImportPackage(string packagePath, AssetOrigin origin, bool interactive)
        {
            ImportPackageWithOrigin(packagePath, origin, ImportPackageOptions.ImportDelayed | (interactive ? ImportPackageOptions.Default : ImportPackageOptions.NoGUI));
        }

        internal static bool ImportPackageImmediately(string packagePath)
        {
            return ImportPackage(packagePath, ImportPackageOptions.NoGUI);
        }

        [FreeFunction("AssetPackage::ImportPackage")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static bool ImportPackage(string packagePath, ImportPackageOptions options);

        [NativeMethod(ThrowsException = true)]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static void ExportPackage(string[] assetPathNames, string fileName, string ownerOrgId, ExportPackageOptions flags);

        [NativeMethod(ThrowsException = true)]
        public static extern ExportPackageItem[] BuildExportPackageItemsList(string[] guids, bool dependencies);
        [NativeMethod(ThrowsException = true)]
        public static extern ExportPackageItem[] BuildExportPackageItemsListWithPackageManagerWarning(string[] guids, bool dependencies, bool warnPackageManagerDependencies);
        [NativeMethod(ThrowsException = true)]
        public static extern void ExportPackageWithGUIDs(string[] guids, string fileName, string ownerOrgId);
        [NativeMethod(ThrowsException = true)]
        public static extern void ExportPackageAndPackageManagerManifest(string[] guids, string fileName, string ownerOrgId);

        [FreeFunction("DelayedImportPackageAssets")]
        public static extern void ImportPackageAssets(string packageName, ImportPackageItem[] items, string packageExtractedPath, bool interactive);

        [FreeFunction("DelayedImportPackageAssetsWithOrigin")]
        public static extern void ImportPackageAssetsWithOrigin(AssetOrigin origin, ImportPackageItem[] items, string packageExtractedPath, bool interactive);

        [FreeFunction("ImportPackageAssets")]
        public static extern void ImportPackageAssetsImmediately(string packageName, ImportPackageItem[] items, string packageExtractedPath, bool interactive);

        [FreeFunction("ImportPackageCancelledGUI")]
        public static extern void ImportPackageAssetsCancelledFromGUI(string packageName, ImportPackageItem[] items);

        [FreeFunction("TickPackageImport")]
        public static extern void TickPackageImport();
    }
}

// We need to keep the PackageInfo struct in the UnityEditor namespace as it's a public API used by our users
namespace UnityEditor
{
    //*undocumented*
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions.Custom)]
    [NativeHeader("Modules/AssetPackageEditor/AssetPackage.h")]
    public struct PackageInfo
    {
        public string packagePath;
        public string jsonInfo;
        public string iconURL;

        [FreeFunction]
        internal static extern PackageInfo[] GetPackageList();

        [FreeFunction]
        internal static extern PackageInfo GetPackageInfo(string packagePath);
    }
}

