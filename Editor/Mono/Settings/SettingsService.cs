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
        }

        public static event Action settingsProviderChanged;

        public static SettingsProvider[] FetchSettingsProviders()
        {
            return
                FetchSettingProviderFromAttribute()
                    .Concat(FetchSettingProvidersFromAttribute())
                    .Concat(FetchDeprecatedPreferenceItems())
                    .Where(provider => provider != null)
                    .ToArray();
        }

        public static void NotifySettingsProviderChanged()
        {
            settingsProviderChanged?.Invoke();
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
