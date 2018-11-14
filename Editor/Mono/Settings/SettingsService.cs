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
        }

        internal static event Action settingsProviderChanged;
        internal static SettingsProvider[] FetchSettingsProviders()
        {
            return
                FetchSettingProviderFromAttribute()
                    .Concat(FetchSettingProvidersFromAttribute())
                    .Concat(FetchDeprecatedPreferenceItems())
                    .Where(provider => provider != null)
                    .ToArray();
        }

        internal static EditorWindow OpenUserPreferenceWindow()
        {
            return SettingsWindow.Show(SettingsScope.User);
        }

        private static IEnumerable<SettingsProvider> FetchDeprecatedPreferenceItems()
        {
            var methods = AttributeHelper.GetMethodsWithAttribute<PreferenceItem>(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            return methods.methodsWithAttributes.Select(method =>
            {
                var callback = Delegate.CreateDelegate(typeof(Action), method.info) as Action;
                if (callback != null)
                {
                    var attributeName = (method.attribute as PreferenceItem).name;
                    return new SettingsProvider("Preferences/" + attributeName, SettingsScope.User) { guiHandler = searchContext => callback() };
                }

                return null;
            });
        }

        private static IEnumerable<SettingsProvider> FetchSettingProviderFromAttribute()
        {
            var methods = AttributeHelper.GetMethodsWithAttribute<SettingsProviderAttribute>();
            return methods.methodsWithAttributes.Select(method =>
            {
                var callback = Delegate.CreateDelegate(typeof(Func<SettingsProvider>), method.info) as Func<SettingsProvider>;
                return callback?.Invoke();
            });
        }

        private static IEnumerable<SettingsProvider> FetchSettingProvidersFromAttribute()
        {
            var methods = AttributeHelper.GetMethodsWithAttribute<SettingsProviderGroupAttribute>();
            return methods.methodsWithAttributes.SelectMany(method =>
            {
                var callback = Delegate.CreateDelegate(typeof(Func<SettingsProvider[]>), method.info) as Func<SettingsProvider[]>;
                return callback?.Invoke();
            });
        }
    }
}
