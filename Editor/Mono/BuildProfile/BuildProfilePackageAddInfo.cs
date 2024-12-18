// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using System.Collections.Generic;

namespace UnityEditor.Build.Profile;

[VisibleToOtherModules]
internal class BuildProfilePackageAddInfo
{
    public const int preconfiguredSettingsVariantNotSet = -2;

    public enum ProgressState
    {
        PackageStateUnknown,
        PackagePending,
        PackageDownloading,
        PackageInstalling,
        PackageReady,
        PackageError,
        ConfigurationPending,
        ConfigurationRunning
    }

    public record struct ProgressEntry(ProgressState state, string name, int packageCount);

    [SerializeField]
    public string profileGuid = string.Empty;
    [SerializeField]
    public string[] packagesToAdd { get; set; } = Array.Empty<string>();
    [SerializeField]
    public int preconfiguredSettingsVariant { get; set; } = preconfiguredSettingsVariantNotSet;

    public Action OnPackageAddProgress;
    public Action OnPackageAddComplete;

    PackageManager.Requests.AddAndRemoveRequest m_PackageAddRequest = null;
    ProgressEntry m_PackageAddProgressInfo = new();

    public void RequestPackageInstallation()
    {
        if (packagesToAdd.Length > 0)
        {
            m_PackageAddRequest = PackageManager.Client.AddAndRemove(packagesToAdd);
            m_PackageAddRequest.progressUpdated += HandlePackageAddProgress;
        }
        else if (preconfiguredSettingsVariant != preconfiguredSettingsVariantNotSet)
        {
            OnPackageAddComplete?.Invoke();
        }
    }

    public ProgressEntry GetPackageAddProgressInfo() => m_PackageAddProgressInfo;

    bool ContainsPackage(string name)
    {
        foreach (var package in packagesToAdd)
        {
            if (package == name)
                return true;
        }
        return false;
    }

    void HandlePackageAddProgress(PackageManager.ProgressUpdateEventArgs progress)
    {
        int readyPackageNum = 0;
        bool done = false;
        bool packageInstalling = false;
        bool packageDownloading = false;
        bool packageErr = false;
        string packageNames = "";
        int packagesDownloadingCnt = 0;
        int packagesInstallingCnt = 0;
        int packagesErrCnt = 0;


        ProgressEntry info;
        foreach (var entry in progress.entries)
        {
            if (ContainsPackage(entry.name))
            {
                if (packageNames.Length > 0)
                    packageNames += " ";
                switch (entry.state)
                {
                    case PackageManager.ProgressState.Ready:
                        readyPackageNum += 1;
                        break;
                    case PackageManager.ProgressState.Error:
                        packageErr = true;
                        packagesErrCnt++;
                        break;
                    case PackageManager.ProgressState.Downloading:
                        packageDownloading = true;
                        packagesDownloadingCnt++;
                        break;
                    case PackageManager.ProgressState.Installing:
                        packageInstalling = true;
                        packagesInstallingCnt++;
                        break;
                }
                packageNames += entry.name;
            }
        }

        done = (readyPackageNum == packagesToAdd.Length);

        if (packageErr)
        {
            info = new ProgressEntry(ProgressState.PackageError, packageNames, packagesErrCnt);
        }
        else if (packageInstalling)
        {
            info = new ProgressEntry(ProgressState.PackageInstalling, packageNames, packagesInstallingCnt);
        }
        else if (packageDownloading)
        {
            info = new ProgressEntry(ProgressState.PackageDownloading, packageNames, packagesDownloadingCnt);
        }
        else
        {
            info = new ProgressEntry(done ? ProgressState.ConfigurationRunning : ProgressState.ConfigurationPending, packageNames, 0);
        }

        m_PackageAddProgressInfo = info;
        OnPackageAddProgress?.Invoke();
        if (done)
        {
            m_PackageAddRequest = null;
            packagesToAdd = Array.Empty<string>();
            OnPackageAddComplete?.Invoke();
        }
    }
}
