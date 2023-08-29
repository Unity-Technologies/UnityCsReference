// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal interface ILocalInfoHandler : IService
{
    IList<AssetStoreLocalInfo> GetParsedLocalInfos();
    AssetStoreLocalInfo GetParsedLocalInfo(string productPath);
    void UpdateExtraInfoInCacheIfNeeded(string productPath, AssetStoreDownloadInfo downloadInfo);
}

internal class LocalInfoHandler : BaseService<ILocalInfoHandler>, ILocalInfoHandler
{
    private readonly IAssetStoreUtils m_AssetStoreUtils;
    private readonly IIOProxy m_IOProxy;
    public LocalInfoHandler(IAssetStoreUtils assetStoreUtils, IIOProxy ioProxy)
    {
        m_AssetStoreUtils = RegisterDependency(assetStoreUtils);
        m_IOProxy = RegisterDependency(ioProxy);
    }

    public IList<AssetStoreLocalInfo> GetParsedLocalInfos()
    {
        var localInfos = new List<AssetStoreLocalInfo>();
        var packageInfos = m_AssetStoreUtils.GetLocalPackageList();
        foreach (var info in packageInfos)
        {
            var parsedInfo = AssetStoreLocalInfo.ParseLocalInfo(info);
            ReadExtraInfoFromCacheIfNeeded(parsedInfo);
            localInfos.Add(parsedInfo);
        }
        return localInfos;
    }

    public AssetStoreLocalInfo GetParsedLocalInfo(string productPath)
    {
        if (string.IsNullOrEmpty(productPath))
            return null;
        var localInfo = AssetStoreLocalInfo.ParseLocalInfo(m_AssetStoreUtils.GetLocalPackageInfo(productPath));
        ReadExtraInfoFromCacheIfNeeded(localInfo);
        return localInfo;
    }

    private void ReadExtraInfoFromCacheIfNeeded(AssetStoreLocalInfo localInfo)
    {
        if (localInfo == null || localInfo.productId == 0 || localInfo.uploadId != 0) return;

        var extraInfoCacheFilePath = GetExtraInfoCacheFilePath(localInfo.packagePath);
        try
        {
            if (!m_IOProxy.FileExists(extraInfoCacheFilePath)) return;

            var json = m_IOProxy.FileReadAllText(extraInfoCacheFilePath);
            var dictionary = Json.Deserialize(json) as Dictionary<string, object>;
            localInfo.uploadId = dictionary?.GetStringAsLong("upload_id") ?? 0;
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public void UpdateExtraInfoInCacheIfNeeded(string productPath, AssetStoreDownloadInfo downloadInfo)
    {
        if (string.IsNullOrEmpty(productPath) || downloadInfo == null || downloadInfo.uploadId == 0) return;

        var localInfo = AssetStoreLocalInfo.ParseLocalInfo(m_AssetStoreUtils.GetLocalPackageInfo(productPath));
        var extraInfoCacheFilePath = GetExtraInfoCacheFilePath(productPath);
        if (localInfo?.uploadId != 0)
        {
            m_IOProxy.DeleteIfExists(extraInfoCacheFilePath);
            return;
        }

        try
        {
            var dictionary = new Dictionary<string, string> { { "upload_id", downloadInfo.uploadId.ToString() } };
            var json = Json.Serialize(dictionary, true);
            m_IOProxy.FileWriteAllText(extraInfoCacheFilePath, json);
        }
        catch (Exception)
        {
            //ignored
        }
    }

    private string GetExtraInfoCacheFilePath(string packagePath)
    {
        return packagePath + ".info.json";
    }
}
