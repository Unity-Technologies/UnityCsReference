// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEditor
{


[StructLayout(LayoutKind.Sequential)]
[Serializable]
internal sealed partial class ImportPackageItem
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
internal sealed partial class ExportPackageItem
{
    public string assetPath;
    public string guid;
    public bool   isFolder;
    public int    enabledStatus;
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct PackageInfo
{
    public string packagePath;
    public string jsonInfo;
    public string iconURL;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  PackageInfo[] GetPackageList () ;

}

internal sealed partial class PackageUtility
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  ExportPackageItem[] BuildExportPackageItemsList (string[] guids, bool dependencies) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ExportPackage (string[] guids, string fileName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  ImportPackageItem[] ExtractAndPrepareAssetList (string packagePath, out string packageIconPath, out bool canPerformReInstall) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ImportPackageAssets (string packageName, ImportPackageItem[] items, bool performReInstall) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ImportPackageAssetsImmediately (string packageName, ImportPackageItem[] items, bool performReInstall) ;

}

}
