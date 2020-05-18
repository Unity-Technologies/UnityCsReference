// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace UnityEditor
{
    [InitializeOnLoad]
    public static class SettingsService
    {
        public static EditorWindow OpenProjectSettings(string settingsPath = null)
        {
            return SettingsWindow.Show(SettingsScope.Project, settingsPath);
        }

        public static EditorWindow OpenUserPreferences(string settingsPath = null)
        {
            return SettingsWindow.Show(SettingsScope.User, settingsPath);
        }

        public static void NotifySettingsProviderChanged()
        {
            settingsProviderChanged?.Invoke();
        }

        const string k_ProjectSettings = "Edit/Project Settings";
        static SettingsService()
        {
            if (UnityEditor.MPE.ProcessService.level == UnityEditor.MPE.ProcessLevel.Master)
            {
                EditorApplication.update -= CheckProjectSettings;
                EditorApplication.update += CheckProjectSettings;
            }
        }

        internal static event Action settingsProviderChanged;
        internal static SettingsProvider[] FetchSettingsProviders()
        {
            return
                FetchSettingProviderFromAttribute()
                    .Concat(FetchSettingProvidersFromAttribute())
                    .Concat(FetchPreferenceItems())
                    .Where(provider => provider != null)
                    .ToArray();
        }

        internal static EditorWindow OpenUserPreferenceWindow()
        {
            return SettingsWindow.Show(SettingsScope.User);
        }

        private static void CheckProjectSettings()
        {
            EditorApplication.update -= CheckProjectSettings;

            var deprecatedMenuItems = Menu.ExtractSubmenus(k_ProjectSettings);
            if (deprecatedMenuItems.Length > 0)
            {
                var sb = new StringBuilder();
                sb.Append("There are menu items registered under Edit/Project Settings: ");
                sb.Append(string.Join(", ", deprecatedMenuItems.Select(item => item.Replace(k_ProjectSettings + "/", "")).ToArray()));
                sb.Append("\n");
                sb.AppendLine("Consider using [SettingsProvider] attribute to register in the Unified Settings Window.");
                Debug.LogWarning(sb);
            }
        }

        private static IEnumerable<SettingsProvider> FetchPreferenceItems()
        {
#pragma warning disable CS0618
            var methods = AttributeHelper.GetMethodsWithAttribute<PreferenceItem>(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
#pragma warning restore CS0618
            return methods.methodsWithAttributes.Select(method =>
            {
                var callback = Delegate.CreateDelegate(typeof(Action), method.info) as Action;
                if (callback != null)
                {
#pragma warning disable CS0618
                    var attributeName = (method.attribute as PreferenceItem).name;
#pragma warning restore CS0618
                    try
                    {
                        return new SettingsProvider("Preferences/" + attributeName, SettingsScope.User) { guiHandler = searchContext => callback() };
                    }
                    catch (Exception)
                    {
                        Debug.LogError("Cannot create preference wrapper for: " + attributeName);
                    }
                }

                return null;
            });
        }

        private static IEnumerable<SettingsProvider> FetchSettingProviderFromAttribute()
        {
            var methods = AttributeHelper.GetMethodsWithAttribute<SettingsProviderAttribute>();
            return methods.methodsWithAttributes.Select(method =>
            {
                try
                {
                    var callback = Delegate.CreateDelegate(typeof(Func<SettingsProvider>), method.info) as Func<SettingsProvider>;
                    return callback?.Invoke();
                }
                catch (Exception)
                {
                    Debug.LogError("Cannot create Settings Provider for: " + method.info.Name);
                }
                return null;
            });
        }

        private static IEnumerable<SettingsProvider> FetchSettingProvidersFromAttribute()
        {
            var methods = AttributeHelper.GetMethodsWithAttribute<SettingsProviderGroupAttribute>();
            return methods.methodsWithAttributes.SelectMany(method =>
            {
                try
                {
                    var callback = Delegate.CreateDelegate(typeof(Func<SettingsProvider[]>), method.info) as Func<SettingsProvider[]>;
                    var providers = callback?.Invoke();
                    if (providers != null)
                    {
                        return providers;
                    }
                }
                catch (Exception)
                {
                    Debug.LogError("Cannot create Settings Providers for: " + method.info.Name);
                }

                return new SettingsProvider[0];
            });
        }
    }
}
