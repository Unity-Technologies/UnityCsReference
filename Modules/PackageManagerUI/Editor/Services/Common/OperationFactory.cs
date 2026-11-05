// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal;

internal interface IOperationFactory : IService
{
    void ResolveDependenciesForOperation(AssetStoreListOperation operation);
    AssetStoreListOperation CreateAssetStoreListOperation();
}

internal class OperationFactory : BaseService<IOperationFactory>, IOperationFactory
{
    private readonly IUnityConnectProxy m_UnityConnect;
    private readonly IAssetStoreRestAPI m_AssetStoreRestAPI;
    private readonly IAssetStoreCache m_AssetStoreCache;
    private readonly IDateTimeProxy m_DateTimeProxy;

    public OperationFactory(IUnityConnectProxy unityConnect, IAssetStoreRestAPI assetStoreRestAPI, IAssetStoreCache assetStoreCache, IDateTimeProxy dateTimeProxy)
    {
        m_UnityConnect = RegisterDependency(unityConnect);
        m_AssetStoreRestAPI = RegisterDependency(assetStoreRestAPI);
        m_AssetStoreCache = RegisterDependency(assetStoreCache);
        m_DateTimeProxy = RegisterDependency(dateTimeProxy);
    }

    [ExcludeFromCodeCoverage]
    public void ResolveDependenciesForOperation(AssetStoreListOperation operation)
    {
        operation?.ResolveDependencies(m_UnityConnect, m_AssetStoreRestAPI, m_AssetStoreCache, m_DateTimeProxy);
    }

    public AssetStoreListOperation CreateAssetStoreListOperation()
    {
        return new AssetStoreListOperation(m_UnityConnect, m_AssetStoreRestAPI, m_AssetStoreCache, m_DateTimeProxy);
    }
}
