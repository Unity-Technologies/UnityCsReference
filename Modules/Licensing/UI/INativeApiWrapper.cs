// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace UnityEditor.Licensing.UI
{
interface INativeApiWrapper
{
    T CreateObjectFromJson<T>(string jsonString);
    void ExitEditor();
    bool HasUiEntitlement();
    bool SaveUnsavedChanges();
    Scene[] GetAllScenes();
    bool HasUnsavedScenes();
    void InvokeLicenseUpdateCallbacks();
    void OpenHubLicenseManagementWindow();
    bool UpdateLicense();
    bool IsHumanControllingUs();
}
}
