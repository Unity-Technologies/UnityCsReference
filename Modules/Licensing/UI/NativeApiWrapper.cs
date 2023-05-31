// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Experimental.Licensing;
using UnityEditor.Licensing.UI.Helper;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.Licensing.UI;

class NativeApiWrapper : INativeApiWrapper
{
    readonly ILicenseLogger m_LicenseLogger;

    public NativeApiWrapper(ILicenseLogger licenseLogger)
    {
        m_LicenseLogger = licenseLogger;
    }

    // Added for support of mocking in Licensing Tests
    // ERROR: System.ArgumentException : Can not instantiate proxy of class: UnityEditor.Licensing.UI.NativeApiWrapper.
    // Could not find a parameterless constructor.
    protected NativeApiWrapper() { }

    public virtual T CreateObjectFromJson<T>(string jsonString)
    {
        return JsonUtility.FromJson<T>(jsonString);
    }

    public virtual void ExitEditor()
    {
        EditorApplication.Exit(0);
    }

    public virtual bool HasUiEntitlement()
    {
        var entitlementInfos = LicensingUtility.HasEntitlementsExtended(new[] { Constants.UiEntitlement }, false);
        return entitlementInfos.Length > 0;
    }

    public virtual Scene[] GetAllScenes()
    {
        var scenes = new Scene[SceneManager.sceneCount];
        for (var index = 0; index < SceneManager.sceneCount; ++index)
        {
            scenes[index] = SceneManager.GetSceneAt(index);
        }

        return scenes;
    }

    public virtual IList<Scene> GetUnsavedScenes()
    {
        var unsavedScenes = new List<Scene>();
        for (var index = 0; index < SceneManager.sceneCount; ++index)
        {
            var scene = SceneManager.GetSceneAt(index);
            if (scene.isDirty)
            {
                unsavedScenes.Add(scene);
            }
        }

        return unsavedScenes;
    }

    public virtual void InvokeLicenseUpdateCallbacks()
    {
        LicensingUtility.InvokeLicenseUpdateCallbacks();
    }

    public virtual void OpenHubLicenseManagementWindow()
    {
        HomeWindow.Show(HomeWindow.HomeMode.ManageLicense);
    }

    public virtual bool SaveUnsavedChanges()
    {
        // Notify to Save and close
        // Editor native code will check if there is a dirty(unsaved scene) scene and saves it
        // no need to filter the scenes in managed code
        return EditorSceneManager.SaveScenes(GetAllScenes());
    }

    public virtual bool UpdateLicense()
    {
        var isSucceed = LicensingUtility.UpdateLicense();

        if (isSucceed)
        {
            m_LicenseLogger.DebugLogNoStackTrace("Successfully updated licenses.");
        }
        else
        {
            m_LicenseLogger.DebugLogNoStackTrace("Failed to update licenses.", LogType.Error);
        }

        return isSucceed;
    }

    public virtual bool IsHumanControllingUs()
    {
        return InternalEditorUtility.isHumanControllingUs;
    }
}
