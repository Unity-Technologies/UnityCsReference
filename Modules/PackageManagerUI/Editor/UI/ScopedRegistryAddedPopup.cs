// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    internal static class ScopedRegistryAddedPopup
    {
        [InitializeOnLoadMethod]
        private static void SubscribeToRegistriesAdded()
        {
            if (!ApplicationUtil.instance.isBatchMode && ApplicationUtil.instance.isUpmRunning)
            {
                UpmRegistryClient.instance.onRegistriesAdded += OnRegistriesAdded;
                UpmRegistryClient.instance.CheckRegistriesChanged();
            }
        }

        private static string Pluralize(this int number, string word, string singularPrefix = "", string pluralPrefix = "", string singularSuffix = "", string pluralSuffix = "s")
        {
            return $"{(number==1?singularPrefix:pluralPrefix)}{word}{(number==1?singularSuffix:pluralSuffix)}";
        }

        private static void OnRegistriesAdded(int registriesAddedCount)
        {
            void WaitForPackageManagerProjectSettings()
            {
                var projectSettings = SettingsWindow.FindWindowByScope(SettingsScope.Project);
                if (projectSettings == null || !projectSettings.hasFocus || !(projectSettings.GetCurrentProvider() is PackageManagerProjectSettingsProvider))
                    return;

                EditorApplication.update -= WaitForPackageManagerProjectSettings;

                PackageManagerProjectSettings.instance.scopedRegistriesSettingsExpanded = true;

                var message = registriesAddedCount.Pluralize(
                    " now available in the Package Manager.",
                    "A new scoped registry is", $"{registriesAddedCount} new scoped registries are",
                    string.Empty, string.Empty);

                var title = registriesAddedCount.Pluralize("Importing ",
                    string.Empty, string.Empty,
                    "a scoped registry", $"{registriesAddedCount} scoped registries");

                if (EditorUtility.DisplayDialog(L10n.Tr(title), L10n.Tr(message), L10n.Tr("Read more"), L10n.Tr("Close")))
                {
                    ApplicationUtil.instance.OpenURL($"https://docs.unity3d.com/{ApplicationUtil.instance.shortUnityVersion}/Documentation/Manual/upm-scoped.html");
                }
            }

            EditorApplication.update += WaitForPackageManagerProjectSettings;
            SettingsService.OpenProjectSettings("Project/Package Manager");
        }
    }
}
