// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal static class PackageManagerPrefs
    {
        private const string kSkipRemoveConfirmationPrefs = "PackageManager.SkipRemoveConfirmation";
        private const string kSkipDisableConfirmationPrefs = "PackageManager.SkipDisableConfirmation";
        private const string kShowPackageDependenciesPrefs = "PackageManager.ShowPackageDependencies";
        private const string kShowPreviewPackagesPrefKeyPrefix = "PackageManager.ShowPreviewPackages_";
        private const string kShowPreviewPackagesWarningPrefs = "PackageManager.ShowPreviewPackagesWarning";
        private const string kLastUsedFilterPrefix = "PackageManager.Filter_";

        private static string GetProjectIdentifier
        {
            get
            {
                // PlayerSettings.productGUID is already used as LocalProjectID by Analytics, so we use it too
                return PlayerSettings.productGUID.ToString();
            }
        }

        private static string LastUsedFilterPrefixForProject { get { return kLastUsedFilterPrefix + GetProjectIdentifier; } }
        private static string kShowPreviewPackagesPrefKeyPrefixForProject { get { return kShowPreviewPackagesPrefKeyPrefix + GetProjectIdentifier; } }

        public static bool SkipRemoveConfirmation
        {
            get { return EditorPrefs.GetBool(kSkipRemoveConfirmationPrefs, false); }
            set { EditorPrefs.SetBool(kSkipRemoveConfirmationPrefs, value); }
        }

        public static bool SkipDisableConfirmation
        {
            get { return EditorPrefs.GetBool(kSkipDisableConfirmationPrefs, false); }
            set { EditorPrefs.SetBool(kSkipDisableConfirmationPrefs, value); }
        }

        public static bool ShowPackageDependencies
        {
            get { return EditorPrefs.GetBool(kShowPackageDependenciesPrefs, false); }
            set { EditorPrefs.SetBool(kShowPackageDependenciesPrefs, value); }
        }

        public static bool HasShowPreviewPackagesKey
        {
            get
            {
                // If user manually choose to show or not preview packages, use this value
                return EditorPrefs.HasKey(kShowPreviewPackagesPrefKeyPrefixForProject);
            }
        }

        public static bool ShowPreviewPackagesFromInstalled { get; set; }

        public static bool ShowPreviewPackages
        {
            get
            {
                var key = kShowPreviewPackagesPrefKeyPrefixForProject;

                // If user manually choose to show or not preview packages, use this value
                if (EditorPrefs.HasKey(key))
                    return EditorPrefs.GetBool(key);

                // Returns true if at least one preview package is installed, false otherwise
                return ShowPreviewPackagesFromInstalled;
            }
            set
            {
                EditorPrefs.SetBool(kShowPreviewPackagesPrefKeyPrefixForProject, value);
            }
        }

        public static bool ShowPreviewPackagesWarning
        {
            get { return EditorPrefs.GetBool(kShowPreviewPackagesWarningPrefs, true); }
            set { EditorPrefs.SetBool(kShowPreviewPackagesWarningPrefs, value); }
        }

        public static PackageFilter LastUsedPackageFilter
        {
            get
            {
                PackageFilter result;
                try
                {
                    result = (PackageFilter)(Enum.Parse(typeof(PackageFilter), EditorPrefs.GetString(LastUsedFilterPrefixForProject, PackageFilter.All.ToString())));
                }
                catch (Exception)
                {
                    result = PackageFilter.All;
                }

                return result;
            }
            set
            {
                EditorPrefs.SetString(LastUsedFilterPrefixForProject, value.ToString());
            }
        }
    }
}
