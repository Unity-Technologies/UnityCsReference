// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEditor
{
    public sealed partial class AssetDatabase
    {
        // Delegate to be called from [[AssetDatabase.ImportPackage]] callbacks
        public delegate void ImportPackageCallback(string packageName);

        // Delegate to be called from [[AssetDatabase.ImportPackage]] callbacks in the event of failure
        public delegate void ImportPackageFailedCallback(string packageName, string errorMessage);

        // Delegate to be called when package import begins
        public static event ImportPackageCallback importPackageStarted;

        // Delegate to be called when package import completes
        public static event ImportPackageCallback importPackageCompleted;

        // Delegate to be called when package import is cancelled
        public static event ImportPackageCallback importPackageCancelled;

        // Delegate to be called when package import fails
        public static event ImportPackageFailedCallback importPackageFailed;

        [RequiredByNativeCode]
        private static void Internal_CallImportPackageStarted(string packageName)
        {
            if (importPackageStarted != null)
                importPackageStarted(packageName);
        }

        [RequiredByNativeCode]
        private static void Internal_CallImportPackageCompleted(string packageName)
        {
            if (importPackageCompleted != null)
                importPackageCompleted(packageName);
        }

        [RequiredByNativeCode]
        private static void Internal_CallImportPackageCancelled(string packageName)
        {
            if (importPackageCancelled != null)
                importPackageCancelled(packageName);
        }

        [RequiredByNativeCode]
        private static void Internal_CallImportPackageFailed(string packageName, string errorMessage)
        {
            if (importPackageFailed != null)
                importPackageFailed(packageName, errorMessage);
        }
    }
}
