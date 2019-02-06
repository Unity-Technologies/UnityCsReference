// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor.PackageManager
{
    [NativeHeader("Modules/PackageManager/Editor/PackageManagerNativeClientImpl.h")]
    [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
    class NativeClient
    {
        [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
        extern public static NativeStatusCode List([Out] out long operationId, bool offlineMode, bool includeIndirectDependencies);

        [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
        extern public static NativeStatusCode Add([Out] out long operationId, string packageId);

        [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
        extern public static NativeStatusCode Remove([Out] out long operationId, string packageId);

        [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
        extern public static NativeStatusCode Search([Out] out long operationId, string packageId, bool offlineMode);

        [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
        extern public static NativeStatusCode SearchAll([Out] out long operationId, bool offlineMode);

        [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
        extern public static NativeStatusCode ResetToEditorDefaults([Out] out long operationId);

        [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
        extern public static NativeStatusCode GetOperationStatus(long operationId);

        [ThreadAndSerializationSafe]
        extern public static void ReleaseCompletedOperation(long operationId);

        extern public static Error GetOperationError(long operationId);

        extern public static OperationStatus GetListOperationData(long operationId);

        extern public static PackageInfo GetAddOperationData(long operationId);

        extern public static string GetRemoveOperationData(long operationId);

        extern public static PackageInfo[] GetSearchOperationData(long operationId);
    }

    [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
    [NativeHeader("Modules/PackageManager/Editor/PackageManagerFolders.h")]
    [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
    class Folders
    {
        extern public static string GetPackagesPath();
        extern public static bool IsPackagedAssetPath(string path);
        extern public static string[] GetPackagesPaths();
    }

    [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
    [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
    internal partial class Packages
    {
        [NativeName("GetAllPackages")]
        extern public static PackageInfo[] GetAll();

        extern private static bool GetPackageByAssetPath(string assetPath, [Out][NotNull] PackageInfo packageInfo);
    }
}
