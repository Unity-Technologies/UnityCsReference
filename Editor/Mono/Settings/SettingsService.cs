// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Internal;

namespace UnityEditor
{
    [ExcludeFromDocs]
    [InitializeOnLoad]
    public class SettingsService
    {
        const string k_ProjectSettings = "Edit/Project Settings";
        static SettingsService()
        {
            EditorApplication.update -= CheckProjectSettings;
            EditorApplication.update += CheckProjectSettings;
        }

        public static event Action settingsProviderChanged;

        public static SettingsProvider[] FetchSettingsProviders()
        {
            return
                FetchSettingProviderFromAttribute()
                    .Concat(FetchSettingProvidersFromAttribute())
                    .Concat(FetchPreferenceItems())
                    .Where(provider => provider != null)
                    .ToArray();
        }

        public static void NotifySettingsProviderChanged()
        {
            settingsProviderChanged?.Invoke();
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
            var methods = AttributeHelper.GetMethodsWithAttribute<PreferenceItem>(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            return methods.methodsWithAttributes.Select(method =>
            {
                var callback = Delegate.CreateDelegate(typeof(Action), method.info) as Action;
                if (callback != null)
                {
                    var attributeName = (method.attribute as PreferenceItem).name;
                    Debug.LogWarning($"Trying to register preference item: \"{attributeName}\". [PreferenceItem] attribute is deprecated. Use [SettingsProvider] attribute instead.");
                    return new SettingsProvider("Preferences/" + attributeName) { guiHandler = searchContext => callback(), scopes = SettingsScopes.User };
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
