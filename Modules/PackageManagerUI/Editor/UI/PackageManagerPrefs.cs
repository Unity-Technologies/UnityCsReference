// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class PackageManagerPrefs
    {
        static IPackageManagerPrefs s_Instance = null;
        public static IPackageManagerPrefs instance { get { return s_Instance ?? PackageManagerPrefsInternal.instance; } }
        private class PackageManagerPrefsInternal : IPackageManagerPrefs
        {
            static PackageManagerPrefsInternal s_Instance = null;
            public static PackageManagerPrefsInternal instance { get { return s_Instance ?? (s_Instance = new PackageManagerPrefsInternal()); } }

            private const string k_SkipRemoveConfirmationPrefs = "PackageManager.SkipRemoveConfirmation";
            private const string k_SkipDisableConfirmationPrefs = "PackageManager.SkipDisableConfirmation";
            private const string k_ShowPackageDependenciesPrefs = "PackageManager.ShowPackageDependencies";
            private const string k_ShowPreviewPackagesWarningPrefs = "PackageManager.ShowPreviewPackagesWarning";
            private const string k_ShowPreviewPackagesPrefsPrefix = "PackageManager.ShowPreviewPackages_";
            private const string k_LastUsedFilterPrefsPrefix = "PackageManager.Filter_";

            public event Action<bool> onShowDependenciesChanged = delegate {};
            public event Action<bool> onShowPreviewPackagesChanged = delegate {};

            private static string projectIdentifier
            {
                get
                {
                    // PlayerSettings.productGUID is already used as LocalProjectID by Analytics, so we use it too
                    return PlayerSettings.productGUID.ToString();
                }
            }
            private static string lastUsedFilterForProjectPerfs { get { return k_LastUsedFilterPrefsPrefix + projectIdentifier; } }
            private static string showPreviewPackagesForProjectPrefs { get { return k_ShowPreviewPackagesPrefsPrefix + projectIdentifier; } }

            public bool skipRemoveConfirmation
            {
                get { return EditorPrefs.GetBool(k_SkipRemoveConfirmationPrefs, false); }
                set { EditorPrefs.SetBool(k_SkipRemoveConfirmationPrefs, value); }
            }

            public bool skipDisableConfirmation
            {
                get { return EditorPrefs.GetBool(k_SkipDisableConfirmationPrefs, false); }
                set { EditorPrefs.SetBool(k_SkipDisableConfirmationPrefs, value); }
            }

            public bool showPackageDependencies
            {
                get { return EditorPrefs.GetBool(k_ShowPackageDependenciesPrefs, false); }
                set
                {
                    var oldValue = showPackageDependencies;
                    EditorPrefs.SetBool(k_ShowPackageDependenciesPrefs, value);
                    if (oldValue != value)
                        onShowDependenciesChanged(value);
                }
            }

            public bool hasShowPreviewPackagesKey
            {
                get
                {
                    // If user manually choose to show or not preview packages, use this value
                    return EditorPrefs.HasKey(showPreviewPackagesForProjectPrefs);
                }
            }

            private bool m_ShowPreviewPackagesFromInstalled = false;
            public bool showPreviewPackagesFromInstalled
            {
                get { return m_ShowPreviewPackagesFromInstalled; }
                set
                {
                    if (m_ShowPreviewPackagesFromInstalled == value)
                        return;
                    var showPreviewPackagesOldValue = showPreviewPackages;
                    m_ShowPreviewPackagesFromInstalled = value;
                    var showPreviewPackagesNewValue = showPreviewPackages;
                    if (showPreviewPackagesNewValue != showPreviewPackagesOldValue)
                        onShowPreviewPackagesChanged(showPreviewPackagesNewValue);
                }
            }

            public bool showPreviewPackages
            {
                get
                {
                    var key = showPreviewPackagesForProjectPrefs;
                    // If user manually choose to show or not preview packages, use this value
                    if (EditorPrefs.HasKey(key))
                        return EditorPrefs.GetBool(key);

                    // Returns true if at least one preview package is installed, false otherwise
                    return showPreviewPackagesFromInstalled;
                }
                set
                {
                    var oldValue = showPreviewPackages;
                    EditorPrefs.SetBool(showPreviewPackagesForProjectPrefs, value);
                    if (oldValue != value)
                        onShowPreviewPackagesChanged(value);
                }
            }

            public bool showPreviewPackagesWarning
            {
                get { return EditorPrefs.GetBool(k_ShowPreviewPackagesWarningPrefs, true); }
                set { EditorPrefs.SetBool(k_ShowPreviewPackagesWarningPrefs, value); }
            }

            public PackageFilterTab? lastUsedPackageFilter
            {
                get
                {
                    try
                    {
                        return (PackageFilterTab)Enum.Parse(typeof(PackageFilterTab), EditorPrefs.GetString(lastUsedFilterForProjectPerfs, PackageFilterTab.Unity.ToString()));
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
                set
                {
                    EditorPrefs.SetString(lastUsedFilterForProjectPerfs, value?.ToString());
                }
            }
        }
    }
}
