// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AssetStorePackageInfo = UnityEditor.PackageInfo;

namespace UnityEditor.PackageManager.UI.AssetStore
{
    internal sealed class AssetStoreClient
    {
        static IAssetStoreClient s_Instance = null;
        public static IAssetStoreClient instance => s_Instance ?? AssetStoreClientInternal.instance;

        [Serializable]
        internal class AssetStoreClientInternal : ScriptableSingleton<AssetStoreClientInternal>, IAssetStoreClient, ISerializationCallbackReceiver
        {
            private static readonly string k_AssetStoreDownloadPrefix = "content__";

            public event Action<IEnumerable<IPackage>> onPackagesChanged = delegate {};
            public event Action<DownloadProgress> onDownloadProgress = delegate {};

            public event Action onOperationStart = delegate {};
            public event Action onOperationFinish = delegate {};
            public event Action<Error> onOperationError = delegate {};

            private Dictionary<string, DownloadProgress> m_Downloads = new Dictionary<string, DownloadProgress>();

            private Dictionary<string, PackageState> m_UpdateDetails = new Dictionary<string, PackageState>();

            [SerializeField]
            private string[] m_SerializedUpdateDetailKeys = new string[0];

            [SerializeField]
            private PackageState[] m_SerializedUpdateDetailValues = new PackageState[0];

            [SerializeField]
            private DownloadProgress[] m_SerializedDownloads = new DownloadProgress[0];

            public void OnAfterDeserialize()
            {
                m_Downloads.Clear();
                foreach (var p in m_SerializedDownloads)
                {
                    m_Downloads[p.packageId] = p;
                }

                m_UpdateDetails.Clear();
                for (var i = 0; i < m_SerializedUpdateDetailKeys.Length; i++)
                {
                    m_UpdateDetails[m_SerializedUpdateDetailKeys[i]] = m_SerializedUpdateDetailValues[i];
                }
            }

            public void OnBeforeSerialize()
            {
                m_SerializedDownloads = m_Downloads.Values.ToArray();

                m_SerializedUpdateDetailKeys = new string[m_UpdateDetails.Count];
                m_SerializedUpdateDetailValues = new PackageState[m_UpdateDetails.Count];
                var i = 0;
                foreach (var kp in m_UpdateDetails)
                {
                    m_SerializedUpdateDetailKeys[i] = kp.Key;
                    m_SerializedUpdateDetailValues[i] = kp.Value;
                    i++;
                }
            }

            public void List(int offset, int limit)
            {
                if (!ApplicationUtil.instance.isUserLoggedIn)
                    return;

                var localPackages = new Dictionary<string, AssetStorePackageInfo>();
                foreach (var package in AssetStoreUtils.instance.GetLocalPackageList())
                {
                    var item = Json.Deserialize(package.jsonInfo) as Dictionary<string, object>;
                    if (item != null && item.ContainsKey("id") && item["id"] is string)
                    {
                        var packageId = (string)item["id"];
                        localPackages[packageId] = package;
                        if (!m_UpdateDetails.ContainsKey(packageId))
                            m_UpdateDetails[packageId] = PackageState.UpToDate;
                    }
                }

                var needsUpdateDetail = localPackages.Where(kp => m_UpdateDetails[kp.Key] == PackageState.UpToDate);
                if (!needsUpdateDetail.Any())
                {
                    ListInternal(localPackages, offset, limit);
                }
                else
                {
                    var list = needsUpdateDetail.Select(kp => kp.Value).ToList();
                    AssetStoreRestAPI.instance.GetProductUpdateDetail(list, updateDetails =>
                    {
                        object error;
                        if (!updateDetails.TryGetValue("errorMessage", out error))
                        {
                            var results = updateDetails["results"] as List<object>;
                            foreach (var item in results)
                            {
                                var updateDetail = item as IDictionary<string, object>;
                                var canUpdate = (updateDetail["can_update"] is long? (long)updateDetail["can_update"] : 0) != 0;
                                m_UpdateDetails[updateDetail["id"] as string] = canUpdate ? PackageState.Outdated : PackageState.UpToDate;
                            }
                        }

                        ListInternal(localPackages, offset, limit);
                    });
                }
            }

            private void ListInternal(IDictionary<string, AssetStorePackageInfo> localPackages, int offset, int limit)
            {
                onOperationStart?.Invoke();

                AssetStoreRestAPI.instance.GetProductIDList(offset, limit, productList =>
                {
                    if (!productList.isValid)
                    {
                        onOperationFinish?.Invoke();
                        onOperationError?.Invoke(new Error(NativeErrorCode.Unknown, productList.errorMessage));
                        return;
                    }

                    var countProduct = productList.list.Count;
                    if (countProduct == 0 || !ApplicationUtil.instance.isUserLoggedIn)
                    {
                        onOperationFinish?.Invoke();
                        return;
                    }

                    foreach (var product in productList.list)
                    {
                        AssetStoreRestAPI.instance.GetProductDetail(product, productDetail =>
                        {
                            AssetStorePackage package;
                            object error;
                            if (!productDetail.TryGetValue("errorMessage", out error))
                            {
                                AssetStorePackageInfo localPackage;
                                if (localPackages.TryGetValue(product.ToString(), out localPackage))
                                {
                                    productDetail["localPath"] = localPackage.packagePath;
                                }
                                else
                                {
                                    productDetail["localPath"] = string.Empty;
                                }

                                package = new AssetStorePackage(product.ToString(), productDetail);
                                if (m_UpdateDetails.ContainsKey(package.uniqueId))
                                {
                                    package.SetState(m_UpdateDetails[package.uniqueId]);
                                }

                                if (package.state == PackageState.Outdated && !string.IsNullOrEmpty(localPackage.packagePath))
                                {
                                    package.m_FirstVersion.localPath = string.Empty;

                                    try
                                    {
                                        var info = new AssetStorePackageVersion.SpecificVersionInfo();
                                        var item = Json.Deserialize(localPackage.jsonInfo) as Dictionary<string, object>;
                                        info.versionId = item["version_id"] as string;
                                        info.versionString = item["version"] as string;
                                        info.publishedDate = item["pubdate"] as string;
                                        info.supportedVersion = item["unity_version"] as string;

                                        var installedVersion = new AssetStorePackageVersion(product.ToString(), productDetail, info);
                                        installedVersion.localPath = localPackage.packagePath;

                                        package.AddVersion(installedVersion);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                            else
                                package = new AssetStorePackage(product.ToString(), new Error(NativeErrorCode.Unknown, error as string));

                            onPackagesChanged?.Invoke(new[] { package });

                            countProduct--;
                            if (countProduct == 0)
                            {
                                onOperationFinish?.Invoke();
                            }
                        });
                    }
                });
            }

            public void Refresh(IPackage package)
            {
                if (!ApplicationUtil.instance.isUserLoggedIn)
                    return;

                var assetStorePackage = package as AssetStorePackage;
                if (assetStorePackage == null)
                    return;

                var localPackage = AssetStoreUtils.instance.GetLocalPackageList().FirstOrDefault(p =>
                {
                    if (!string.IsNullOrEmpty(p.jsonInfo))
                    {
                        var item = Json.Deserialize(p.jsonInfo) as Dictionary<string, object>;
                        return item != null && item.ContainsKey("id") && item["id"] is string && package.uniqueId == (string)item["id"];
                    }
                    return false;
                });

                if (!string.IsNullOrEmpty(localPackage.packagePath))
                {
                    assetStorePackage.m_FirstVersion.localPath = localPackage.packagePath;
                    if (assetStorePackage.m_FirstVersion != assetStorePackage.m_LastVersion)
                    {
                        assetStorePackage.RemoveVersion(assetStorePackage.m_LastVersion);
                    }
                    assetStorePackage.SetState(PackageState.UpToDate);
                    m_UpdateDetails[package.uniqueId] = PackageState.UpToDate;

                    onPackagesChanged?.Invoke(new[] { package });
                }
            }

            public bool IsAnyDownloadInProgress()
            {
                return m_Downloads.Values.Any(progress => progress.state == DownloadProgress.State.InProgress || progress.state == DownloadProgress.State.Started);
            }

            private static string AssetStoreCompatibleKey(string packageId)
            {
                if (packageId.StartsWith(k_AssetStoreDownloadPrefix))
                    return packageId;

                return k_AssetStoreDownloadPrefix + packageId;
            }

            public bool IsDownloadInProgress(string packageId)
            {
                DownloadProgress progress;
                if (!GetDownloadInProgress(packageId, out progress))
                    return false;

                return progress.state == DownloadProgress.State.InProgress || progress.state == DownloadProgress.State.Started;
            }

            public bool GetDownloadInProgress(string packageId, out DownloadProgress progress)
            {
                progress = null;
                return m_Downloads.TryGetValue(AssetStoreCompatibleKey(packageId), out progress);
            }

            public void Download(string packageId)
            {
                DownloadProgress progress;
                if (GetDownloadInProgress(packageId, out progress))
                {
                    if (progress.state != DownloadProgress.State.Started &&
                        progress.state != DownloadProgress.State.InProgress &&
                        progress.state != DownloadProgress.State.Decrypting)
                    {
                        m_Downloads.Remove(AssetStoreCompatibleKey(packageId));
                    }
                    else
                    {
                        onDownloadProgress?.Invoke(progress);
                        return;
                    }
                }

                progress = new DownloadProgress(packageId);
                m_Downloads[AssetStoreCompatibleKey(packageId)] = progress;
                onDownloadProgress?.Invoke(progress);

                var id = long.Parse(packageId);
                AssetStoreDownloadOperation.instance.DownloadUnityPackageAsync(id, result =>
                {
                    progress.state = result.downloadState;
                    if (result.downloadState == DownloadProgress.State.Error)
                        progress.message = result.errorMessage;

                    onDownloadProgress?.Invoke(progress);
                });
            }

            public void AbortDownload(string packageId)
            {
                DownloadProgress progress;
                if (!GetDownloadInProgress(packageId, out progress))
                    return;

                if (progress.state == DownloadProgress.State.Aborted || progress.state == DownloadProgress.State.Completed || progress.state == DownloadProgress.State.Error)
                    return;

                var id = long.Parse(packageId);
                AssetStoreDownloadOperation.instance.AbortDownloadPackageAsync(id, result =>
                {
                    progress.state = DownloadProgress.State.Aborted;
                    progress.current = progress.total;
                    progress.message = L10n.Tr("Download aborted");

                    onDownloadProgress?.Invoke(progress);

                    m_Downloads.Remove(AssetStoreCompatibleKey(packageId));
                });
            }

            // Used by AssetStoreUtils
            public void OnDownloadProgress(string packageId, string message, ulong bytes, ulong total)
            {
                DownloadProgress progress;
                if (!GetDownloadInProgress(packageId, out progress))
                {
                    if (packageId.StartsWith(k_AssetStoreDownloadPrefix))
                        packageId = packageId.Substring(k_AssetStoreDownloadPrefix.Length);
                    progress = new DownloadProgress(packageId) { state = DownloadProgress.State.InProgress, message = "downloading" };
                    m_Downloads[AssetStoreCompatibleKey(packageId)] = progress;
                }

                progress.current = bytes;
                progress.total = total;
                progress.message = message;

                if (message == "ok")
                    progress.state = DownloadProgress.State.Completed;
                else if (message == "connecting")
                    progress.state = DownloadProgress.State.Started;
                else if (message == "downloading")
                    progress.state = DownloadProgress.State.InProgress;
                else if (message == "decrypt")
                    progress.state = DownloadProgress.State.Decrypting;
                else if (message == "aborted")
                    progress.state = DownloadProgress.State.Aborted;
                else
                    progress.state = DownloadProgress.State.Error;

                onDownloadProgress?.Invoke(progress);
            }

            public void Setup()
            {
                ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;
                if (ApplicationUtil.instance.isUserLoggedIn)
                {
                    AssetStoreUtils.instance.RegisterDownloadDelegate(this);
                }
            }

            public void Clear()
            {
                AssetStoreUtils.instance.UnRegisterDownloadDelegate(this);
                ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;

                m_Downloads.Clear();
                m_UpdateDetails.Clear();

                m_SerializedDownloads = new DownloadProgress[0];
                m_SerializedUpdateDetailKeys = new string[0];
                m_SerializedUpdateDetailValues = new PackageState[0];
            }

            private void OnUserLoginStateChange(bool loggedIn)
            {
                if (!loggedIn)
                {
                    AssetStoreUtils.instance.UnRegisterDownloadDelegate(this);
                    AbortAllDownloads();
                }
                else
                {
                    AssetStoreUtils.instance.RegisterDownloadDelegate(this);
                }
            }

            public void AbortAllDownloads()
            {
                var currentDownloads = m_Downloads.Values.Where(v => v.state == DownloadProgress.State.Started || v.state == DownloadProgress.State.InProgress)
                    .Select(v => long.Parse(v.packageId)).ToArray();
                m_Downloads.Clear();

                foreach (var download in currentDownloads)
                    AssetStoreDownloadOperation.instance.AbortDownloadPackageAsync(download);
            }
        }
    }
}
