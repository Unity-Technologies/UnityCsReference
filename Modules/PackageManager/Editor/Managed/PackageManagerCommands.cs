// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager
{
    static class PackageManagerCommands
    {
        [MenuItem("Help/Reset Packages to defaults", priority = 1000)]
        public static void ResetProjectPackagesToEditorDefaults()
        {
            ResetToEditorDefaultsRequest request = Client.ResetToEditorDefaults();
            if (request.Status == StatusCode.Failure)
            {
                Debug.LogError("Resetting your project's packages to the editor's default configuration failed. Reason: " + request.Error.message);
            }
        }
    }
}

