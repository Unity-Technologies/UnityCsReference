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
    [NativeHeader("Modules/PackageManager/Editor/Public/PackageManagerApi.h")]
    partial class NativeClient
    {
        [StaticAccessor("PackageManager::Api::GetInstance()", StaticAccessorType.Arrow)]
        extern public static NativeStatusCode List([Out] out long operationId);

        [StaticAccessor("PackageManager::Api::GetInstance()", StaticAccessorType.Arrow)]
        extern public static NativeStatusCode Add([Out] out long operationId, string packageId);

        [StaticAccessor("PackageManager::Api::GetInstance()", StaticAccessorType.Arrow)]
        extern public static NativeStatusCode Remove([Out] out long operationId, string packageId);

        [StaticAccessor("PackageManager::Api::GetInstance()", StaticAccessorType.Arrow)]
        extern public static NativeStatusCode Search([Out] out long operationId, string packageId);

        [StaticAccessor("PackageManager::Api::GetInstance()", StaticAccessorType.Arrow)]
        extern public static NativeStatusCode SearchAll([Out] out long operationId);

        [StaticAccessor("PackageManager::Api::GetInstance()", StaticAccessorType.Arrow)]
        extern public static NativeStatusCode ResetToEditorDefaults([Out] out long operationId);

        [ThreadAndSerializationSafe]
        [StaticAccessor("PackageManager::Api::GetInstance()", StaticAccessorType.Arrow)]
        extern public static NativeStatusCode GetOperationStatus(long operationId);

        [ThreadAndSerializationSafe]
        extern public static Error GetOperationError(long operationId);

        [ThreadAndSerializationSafe]
        extern public static OperationStatus GetListOperationData(long operationId);

        [ThreadAndSerializationSafe]
        extern public static PackageInfo GetAddOperationData(long operationId);

        [ThreadAndSerializationSafe]
        extern public static string GetRemoveOperationData(long operationId);

        [ThreadAndSerializationSafe]
        extern public static PackageInfo[] GetSearchOperationData(long operationId);
    }

    [NativeHeader("Modules/PackageManager/Editor/PackageManagerFolders.h")]
    [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
    partial class Folders
    {
        extern public static string GetPackagesMountPoint();
        extern public static bool IsPackagedAssetPath(string path);
        extern public static string[] GetPackagesPaths();
        extern internal static bool RegisterPackageFolder(string name, string assetPath, bool readOnly);
        extern internal static bool UnregisterPackageFolder(string name, string assetPath);
    }
}
