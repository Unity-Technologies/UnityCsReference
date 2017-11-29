// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;
using System.Runtime.InteropServices;

namespace UnityEditor
{
#pragma warning disable 649
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [NativeType(CodegenOptions.Custom, "MonoImportPackageItem", Header = "Editor/Mono/PackageUtility.bindings.h")]
    [NativeAsStruct]
    internal class ImportPackageItem
    {
        public string exportedAssetPath;
        public string destinationAssetPath;
        public string sourceFolder;
        public string previewPath;
        public string guid;
        public int    enabledStatus;
        public bool   isFolder;
        public bool   exists;
        public bool   assetChanged;
        public bool   pathConflict;
        public bool   projectAsset;
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [NativeAsStruct]
    [NativeType(CodegenOptions.Custom, "MonoExportPackageItem", Header = "Editor/Mono/PackageUtility.bindings.h")]
    internal class ExportPackageItem
    {
        public string assetPath;
        public string guid;
        public bool   isFolder;
        public int    enabledStatus;
    }

    //*undocumented*
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions.Custom, "MonoPackageInfo", Header = "Editor/Src/PackageUtility.h")]
    public struct PackageInfo
    {
        public string packagePath;
        public string jsonInfo;
        public string iconURL;

        [FreeFunction]
        internal static extern PackageInfo[] GetPackageList();
    }

    [NativeHeader("Editor/Mono/PackageUtility.bindings.h")]
    internal class PackageUtility
    {
        [NativeThrows]
        public static extern ExportPackageItem[] BuildExportPackageItemsList(string[] guids, bool dependencies);
        [NativeThrows]
        public static extern void ExportPackage(string[] guids, string fileName);
        public static extern ImportPackageItem[] ExtractAndPrepareAssetList(string packagePath, out string packageIconPath, out bool canPerformReInstall);

        [FreeFunction("DelayedImportPackageAssets")]
        public static extern void ImportPackageAssets(string packageName, ImportPackageItem[] items, bool performReInstall);
        [FreeFunction("ImportPackageAssets")]
        public static extern void ImportPackageAssetsImmediately(string packageName, ImportPackageItem[] items, bool performReInstall);
    }
}
