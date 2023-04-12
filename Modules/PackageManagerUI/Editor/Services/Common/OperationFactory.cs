// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal;

internal class OperationFactory
{
    [NonSerialized]
    private UnityConnectProxy m_UnityConnect;
    [NonSerialized]
    private AssetStoreRestAPI m_AssetStoreRestAPI;
    [NonSerialized]
    private AssetStoreCache m_AssetStoreCache;
    [ExcludeFromCodeCoverage]
    public void ResolveDependencies(UnityConnectProxy unityConnect, AssetStoreRestAPI assetStoreRestAPI, AssetStoreCache assetStoreCache)
    {
        m_UnityConnect = unityConnect;
        m_AssetStoreRestAPI = assetStoreRestAPI;
        m_AssetStoreCache = assetStoreCache;
    }

    [ExcludeFromCodeCoverage]
    public void ResolveDependencies(AssetStoreListOperation operation)
    {
        operation?.ResolveDependencies(m_UnityConnect, m_AssetStoreRestAPI, m_AssetStoreCache);
    }

    public virtual AssetStoreListOperation CreateAssetStoreListOperation()
    {
        return new AssetStoreListOperation(m_UnityConnect, m_AssetStoreRestAPI, m_AssetStoreCache);
    }
}
