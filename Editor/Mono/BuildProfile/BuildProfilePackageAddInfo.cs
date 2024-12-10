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

    public record struct ProgressEntry(ProgressState state, string name);

    [SerializeField]
    public string profileGuid = string.Empty;
    [SerializeField]
    public string[] packagesToAdd { get; set; } = Array.Empty<string>();
    [SerializeField]
    public int preconfiguredSettingsVariant { get; set; } = preconfiguredSettingsVariantNotSet;

    public Action OnPackageAddProgress;
    public Action OnPackageAddComplete;

    PackageManager.Requests.AddAndRemoveRequest m_PackageAddRequest = null;
    List<ProgressEntry> m_PackageAddProgressInfo = new();

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

    public IReadOnlyList<ProgressEntry> GetPackageAddProgressInfo() => m_PackageAddProgressInfo;

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
        bool done = true;
        var info = new List<ProgressEntry>();
        foreach (var entry in progress.entries)
        {
            if (ContainsPackage(entry.name))
            {
                switch (entry.state)
                {
                    case PackageManager.ProgressState.Ready:
                        info.Add(new ProgressEntry(ProgressState.PackageReady, entry.name));
                        break;
                    case PackageManager.ProgressState.Error:
                        info.Add(new ProgressEntry(ProgressState.PackageError, entry.name));
                        break;
                    case PackageManager.ProgressState.Pending:
                        info.Add(new ProgressEntry(ProgressState.PackagePending, entry.name));
                        done = false;
                        break;
                    case PackageManager.ProgressState.Downloading:
                        info.Add(new ProgressEntry(ProgressState.PackageDownloading, entry.name));
                        done = false;
                        break;
                    case PackageManager.ProgressState.Installing:
                        info.Add(new ProgressEntry(ProgressState.PackageInstalling, entry.name));
                        done = false;
                        break;
                    default:
                        info.Add(new ProgressEntry(ProgressState.PackageStateUnknown, entry.name));
                        done = false;
                        break;
                }
            }
        }
        info.Add(new ProgressEntry(done ? ProgressState.ConfigurationRunning : ProgressState.ConfigurationPending, string.Empty));
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
