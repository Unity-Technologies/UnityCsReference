// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile;

/// <summary>
/// Package installation progress tracker for a newly created build profile. Serialized as part of the
/// <see cref="BuildProfileContext"/>, so a pending package registration survives domain reloads.
/// </summary>
[Serializable]
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
    [field: SerializeField]
    public string[] packagesToAdd { get; set; } = Array.Empty<string>();
    [field: SerializeField]
    public int preconfiguredSettingsVariant { get; set; } = preconfiguredSettingsVariantNotSet;

    /// <summary>
    /// True when the installed packages were not registered yet as the request completed.
    /// <see cref="packagesToAdd"/> is then kept to tell when they are.
    /// </summary>
    [SerializeField]
    public bool isRegistrationPending;

    public Action OnPackageAddProgress;
    public Action OnPackageAddComplete;

    // A request cannot be resumed after a domain reload, so RequestPackageInstallation issues a new one.
    [NonSerialized]
    PackageManager.Requests.AddAndRemoveRequest m_PackageAddRequest = null;
    ProgressEntry m_PackageAddProgressInfo = new();

    public void RequestPackageInstallation()
    {
        if (isRegistrationPending)
        {
            // Resumes the wait after a domain reload or an editor restart.
            AddPackageRegistrationCallbacks();
            HandlePackageRegistration();
        }
        else if (packagesToAdd.Length > 0)
        {
            m_PackageAddRequest = PackageManager.Client.AddAndRemove(packagesToAdd);
            m_PackageAddRequest.progressUpdated += HandlePackageAddProgress;
            EditorApplication.update -= HandlePackageRequestCompleted;
            EditorApplication.update += HandlePackageRequestCompleted;
        }
        else if (preconfiguredSettingsVariant != preconfiguredSettingsVariantNotSet)
        {
            OnPackageAddComplete?.Invoke();
        }
    }

    /// <summary>
    /// Check if package installation is completed, considers that multiple
    /// package add requests with similar packages could be made.
    /// </summary>
    public bool IsPackageRequestDone()
    {
        foreach (var package in packagesToAdd)
        {
            if (!PackageManager.PackageInfo.IsPackageRegistered(package))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Packages count as registered as soon as the editor resolves them, before it imports and
    /// compiles them, so the registration is only done once the editor is idle again.
    /// </summary>
    public bool IsPackageRegistrationDone(bool isEditorBusy) => !isEditorBusy && IsPackageRequestDone();

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
    }

    void HandlePackageRequestCompleted()
    {
        if (m_PackageAddRequest != null && m_PackageAddRequest.IsCompleted)
        {
            EditorApplication.update -= HandlePackageRequestCompleted;
            var succeeded = m_PackageAddRequest.Status < PackageManager.StatusCode.Failure;
            if (!succeeded)
                Debug.LogError(m_PackageAddRequest.Error.message);

            m_PackageAddRequest = null;
            HandlePackageAddComplete(succeeded);
        }
    }

    void HandlePackageAddComplete(bool succeeded)
    {
        // The request reports done before the editor resolves and registers the packages.
        isRegistrationPending = succeeded && !IsPackageRequestDone();
        if (isRegistrationPending)
            AddPackageRegistrationCallbacks();
        else
            CompletePackageAdd();
    }

    /// <summary>
    /// Re-checks the package registration on editor updates, which resume once the editor has finished
    /// the refresh that registers, imports and compiles the packages. Extensions may read package assets
    /// on creation, so the package add only completes then. Editor update callbacks do not survive a
    /// domain reload, so <see cref="RequestPackageInstallation"/> re-attaches them.
    /// </summary>
    void AddPackageRegistrationCallbacks()
    {
        EditorApplication.update -= HandlePackageRegistration;
        EditorApplication.update += HandlePackageRegistration;
    }

    void RemovePackageRegistrationCallbacks()
    {
        EditorApplication.update -= HandlePackageRegistration;
    }

    void HandlePackageRegistration()
    {
        // The profile asset was deleted while waiting.
        if (IsProfileAssetDeleted())
            RemovePackageRegistrationCallbacks();
        else if (IsPackageRegistrationDone(IsEditorBusy()))
        {
            RemovePackageRegistrationCallbacks();
            isRegistrationPending = false;
            CompletePackageAdd();
        }
    }

    void CompletePackageAdd()
    {
        packagesToAdd = Array.Empty<string>();
        OnPackageAddComplete?.Invoke();
    }

    bool IsProfileAssetDeleted()
    {
        // The asset database may still map the GUID of a deleted asset to its former path.
        var profilePath = AssetDatabase.GUIDToAssetPath(profileGuid);
        return string.IsNullOrEmpty(profilePath) || !AssetDatabase.AssetPathExists(profilePath);
    }

    static bool IsEditorBusy() =>
        EditorApplication.isCompiling || EditorApplication.isUpdating || InternalEditorUtility.IsScriptReloadRequested();
}
