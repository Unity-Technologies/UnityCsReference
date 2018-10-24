// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager
{
    static class PackageManagerCommands
    {
        [MenuItem("Help/Reset Packages to defaults", priority = 1000)]
        public static void ResetProjectPackagesToEditorDefaults()
        {
            if (EditorUtility.DisplayDialog(L10n.Tr("Unity Package Manager"),
                L10n.Tr("Resetting packages to defaults will discard any changes you have made and/or remove packages set by the project template.\nThis action may result in compilation errors or a broken project. Are you sure?"),
                L10n.Tr("No"), L10n.Tr("Yes")))
                return;

            ResetToEditorDefaultsRequest request = Client.ResetToEditorDefaults();
            if (request.Status == StatusCode.Failure)
            {
                Debug.LogError("Resetting packages to defaults failed. Reason: " + request.Error.message);
            }
        }
    }
}
