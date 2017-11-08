// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine.Bindings;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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
        extern public static NativeStatusCode Outdated([Out] out long operationId);

        [StaticAccessor("PackageManager::Api::GetInstance()", StaticAccessorType.Arrow)]
        extern public static NativeStatusCode ResetToEditorDefaults([Out] out long operationId);

        [StaticAccessor("PackageManager::Api::GetInstance()", StaticAccessorType.Arrow)]
        extern public static NativeStatusCode GetOperationStatus(long operationId);

        extern public static Error GetOperationError(long operationId);

        extern public static OperationStatus GetListOperationData(long operationId);

        extern public static UpmPackageInfo GetAddOperationData(long operationId);

        extern public static string GetRemoveOperationData(long operationId);

        extern public static UpmPackageInfo[] GetSearchOperationData(long operationId);

        public static Dictionary<string, OutdatedPackage> GetOutdatedOperationData(long operationId)
        {
            OutdatedPackage[] outdatedPackages = GetOutdatedOperationDataNative(operationId);
            return outdatedPackages.ToDictionary(outdated => outdated.current.name, outdated => outdated);
        }

        [NativeMethod("GetOutdatedOperationData")]
        extern private static OutdatedPackage[] GetOutdatedOperationDataNative(long operationId);
    }
}

