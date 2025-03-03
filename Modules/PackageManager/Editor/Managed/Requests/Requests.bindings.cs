// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.PackageManager.Requests
{
    [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
    [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
    public partial class Request
    {
        private static extern NativeStatusCode GetOperationStatus(long operationId);

        private static extern Error GetOperationError(long operationId);

        [ThreadAndSerializationSafe]
        private static extern void ReleaseCompletedOperation(long operationId);
    }

    public partial class AddRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::Add", StaticAccessorType.DoubleColon)]
        private static extern PackageInfo GetOperationData(long operationId);
    }

    public partial class AddAndRemoveRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::AddAndRemove", StaticAccessorType.DoubleColon)]
        private static extern OperationStatus GetOperationData(long operationId);
    }

    internal partial class AddScopedRegistryRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::AddScopedRegistry", StaticAccessorType.DoubleColon)]
        private static extern RegistryInfo GetOperationData(long operationId);
    }

    internal partial class ClearCacheRootRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::ClearCacheRoot", StaticAccessorType.DoubleColon)]
        private static extern CacheRootConfig GetOperationData(long operationId);
    }

    public partial class EmbedRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::Embed", StaticAccessorType.DoubleColon)]
        private static extern PackageInfo GetOperationData(long operationId);
    }

    internal partial class GetCacheRootRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::GetCacheRoot", StaticAccessorType.DoubleColon)]
        private static extern CacheRootConfig GetOperationData(long operationId);
    }

    internal partial class GetRegistriesRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::GetRegistries", StaticAccessorType.DoubleColon)]
        private static extern RegistryInfo[] GetOperationData(long operationId);
    }

    public partial class ListRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::List", StaticAccessorType.DoubleColon)]
        private static extern OperationStatus GetOperationData(long operationId);
    }

    internal partial class ListBuiltInPackagesRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::ListBuiltInPackages", StaticAccessorType.DoubleColon)]
        private static extern PackageInfo[] GetOperationData(long operationId);
    }

    public partial class PackRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::Pack", StaticAccessorType.DoubleColon)]
        private static extern PackOperationResult GetOperationData(long operationId);
    }

    public sealed partial class SearchRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::GetPackageInfo", StaticAccessorType.DoubleColon)]
        private static extern PackageInfo[] GetOperationData(long operationId);
    }

    internal partial class SetCacheRootRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::SetCacheRoot", StaticAccessorType.DoubleColon)]
        private static extern CacheRootConfig GetOperationData(long operationId);
    }

    internal partial class UpdateScopedRegistryRequest
    {
        [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
        [StaticAccessor("PackageManager::UpdateScopedRegistry", StaticAccessorType.DoubleColon)]
        private static extern RegistryInfo GetOperationData(long operationId);
    }
}
