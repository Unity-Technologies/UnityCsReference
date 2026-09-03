// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using Unity.Localization.Providers;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Unity.Localization.Editor;

static partial class AddressablesReferenceMode
{
    // Plain id resolves to whatever the registry offers; pin @2.0.0 once the Addressables-capable version ships.
    const string k_PackageId = "com.unity.localization";

    // Proposed contract with the localization 2.0 package; align these names with the package team before release.
    const string k_IntegrationTypeName = "UnityEngine.Localization.Addressables.LocalizationAddressablesIntegration, Unity.Localization";
    const string k_InstallMethodName = "InstallProvider";

    internal const string k_PendingKey = "Unity.Localization.Addressables.PendingSetup";

    const double k_InstallTimeoutSeconds = 120;

    [AutoStaticsCleanup] // an in flight package request cannot outlive the code that started it
    static AddRequest s_AddRequest;

    [AutoStaticsCleanup] // holds subscriber delegates
    internal static event Action installStateChanged;

    internal static bool IsInstalling => SessionState.GetBool(k_PendingKey, false);

    static void SetPending(bool value)
    {
        if (value)
            SessionState.SetBool(k_PendingKey, true);
        else
            SessionState.EraseBool(k_PendingKey);
        installStateChanged?.Invoke();
    }

    public static void InstallAndConfigure(LocalizationSettings active, Action onComplete)
    {
        if (active == null)
            return;

        // Recorded up front so the wizard completes and the choice survives the reload the install triggers.
        Undo.RegisterCompleteObjectUndo(active, "Set Reference Mode");
        EnsureReferencedFallback(active);
        // Interim selection so setup reads as configured; the package's provider replaces it once registered.
        if (active.Database?.AssetProvider != null)
            active.SelectedProvider = active.Database.AssetProvider.GetProvider<ReferencedAssetProvider>();
        EditorUtility.SetDirty(active);
        AssetDatabase.SaveAssets();

        if (IsPackageInstalled())
        {
            TryRegisterProvider(active);
            onComplete?.Invoke();
            return;
        }

        if (s_AddRequest != null)
            return;

        SetPending(true);
        s_AddRequest = Client.Add(k_PackageId);
        EditorApplication.update += PollAdd;

        void PollAdd()
        {
            if (s_AddRequest == null || !s_AddRequest.IsCompleted)
                return;
            EditorApplication.update -= PollAdd;
            var failed = s_AddRequest.Status == StatusCode.Failure;
            var error = s_AddRequest.Error?.message;
            s_AddRequest = null;
            if (failed)
            {
                // Roll the choice back so the wizard step reappears and the user can retry.
                active.SelectedProvider = null;
                EditorUtility.SetDirty(active);
                AssetDatabase.SaveAssets();
                SetPending(false);
                Debug.LogError($"Localization: failed to add {k_PackageId}: {error}");
            }
            // On success the package import triggers a domain reload; CompletePendingSetup finishes registration.
        }
    }

    [InitializeOnLoadMethod]
    static void CompletePendingSetup()
    {
        if (!IsInstalling)
            return;

        var active = LocalizationEditorSettings.ActiveSettings;
        if (active == null)
        {
            SetPending(false);
            return;
        }

        if (IsIntegrationReady())
        {
            TryRegisterProvider(active);
            SetPending(false);
            return;
        }

        var deadline = EditorApplication.timeSinceStartup + k_InstallTimeoutSeconds;
        EditorApplication.update += WaitForIntegration;

        void WaitForIntegration()
        {
            if (IsIntegrationReady())
            {
                EditorApplication.update -= WaitForIntegration;
                TryRegisterProvider(active);
                SetPending(false);
                return;
            }
            if (EditorApplication.timeSinceStartup < deadline)
                return;
            EditorApplication.update -= WaitForIntegration;
            if (active != null)
            {
                active.SelectedProvider = null;
                EditorUtility.SetDirty(active);
                AssetDatabase.SaveAssets();
            }
            SetPending(false);
            Debug.LogError($"Localization: timed out waiting for {k_PackageId}'s Addressables integration; it may be missing or too old.");
        }
    }

    static void TryRegisterProvider(LocalizationSettings active)
    {
        var type = Type.GetType(k_IntegrationTypeName);
        var method = type?.GetMethod(k_InstallMethodName, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            Debug.Log($"Localization: {k_PackageId} is installed but does not expose {k_InstallMethodName}; leaving the reference mode unset until an Addressables-capable version provides it.");
            active.SelectedProvider = null;
            EditorUtility.SetDirty(active);
            AssetDatabase.SaveAssets();
            return;
        }
        try
        {
            method.Invoke(null, new object[] { active });
            EditorUtility.SetDirty(active);
            AssetDatabase.SaveAssets();
        }
        catch (Exception e)
        {
            Debug.LogError($"Localization: the Addressables integration entry point threw: {e.InnerException?.Message ?? e.Message}");
        }
    }

    static void EnsureReferencedFallback(LocalizationSettings active)
    {
        var chain = active.Database != null ? active.Database.AssetProvider : null;
        if (chain != null && chain.GetProvider<ReferencedAssetProvider>() == null)
            chain.AddProvider(new ReferencedAssetProvider());
    }

    static bool IsPackageInstalled()
        => UnityEditor.PackageManager.PackageInfo.FindForPackageName(k_PackageId) != null;

    // Gated on the type, not the files: PackageInfo reports the package present before its assembly loads.
    static bool IsIntegrationReady() => Type.GetType(k_IntegrationTypeName) != null;
}
