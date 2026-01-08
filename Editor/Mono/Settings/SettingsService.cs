// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [InitializeOnLoad]
    public static class SettingsService
    {
        internal static event Action repaintAllSettingsWindow;

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

        public static void RepaintAllSettingsWindow()
        {
            repaintAllSettingsWindow?.Invoke();
        }

        internal static IEnumerable<SettingsProvider> FilterAndWarnAgainstDuplicates(IEnumerable<SettingsProvider> settingsProviders, SettingsScope scope)
        {
            Dictionary<string, int> settingPaths = new Dictionary<string, int>();
            foreach (var provider in settingsProviders)
            {
                if (provider.scope != scope)
                {
                    yield return provider;
                    continue;
                }

                if (settingPaths.ContainsKey(provider.settingsPath))
                {
                    settingPaths[provider.settingsPath] += 1;
                    continue;
                }
                else
                {
                    settingPaths.Add(provider.settingsPath, 1);
                    yield return provider;
                }
            }

            foreach (var settingPath in settingPaths)
            {
                if (settingPath.Value > 1)
                    Debug.LogWarning($"There are {settingPath.Value} settings providers with the same name {settingPath.Key} in {scope} scope.");
            }
        }

        internal static event Action settingsProviderChanged;
        internal static SettingsProvider[] FetchSettingsProviders()
        {
            var settingsProviders =
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                FetchSettingProviderFromAttribute()
#pragma warning restore RS0030
                    .Concat(FetchSettingProvidersFromAttribute())
                    .Concat(FetchPreferenceItems())
                    .Where(provider => provider != null);

            settingsProviders = FilterAndWarnAgainstDuplicates(settingsProviders, SettingsScope.Project);
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return FilterAndWarnAgainstDuplicates(settingsProviders, SettingsScope.User).ToArray();
#pragma warning restore RS0030
        }

        internal static SettingsProvider[] FetchSettingsProviders(SettingsScope scope)
        {
            var settingsProviders =
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                FetchSettingProviderFromAttribute()
#pragma warning restore RS0030
                    .Concat(FetchSettingProvidersFromAttribute())
                    .Concat(FetchPreferenceItems())
                    .Where(provider => provider != null && provider.scope == scope);

#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return FilterAndWarnAgainstDuplicates(settingsProviders, scope).ToArray();
#pragma warning restore RS0030
        }

        public static bool Exists(string settingsPath)
        {
            var settingsProviders = FetchSettingsProviders();

            foreach (var provider in settingsProviders)
            {
                if (settingsPath.Equals(provider.settingsPath, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        [RequiredByNativeCode]
        internal static EditorWindow OpenUserPreferenceWindow()
        {
            return SettingsWindow.Show(SettingsScope.User);
        }

        private static IEnumerable<SettingsProvider> FetchPreferenceItems()
        {
#pragma warning disable CS0618
            var methods = AttributeHelper.GetMethodsWithAttribute<PreferenceItem>(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
#pragma warning restore CS0618
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return methods.methodsWithAttributes.Select(method =>
#pragma warning restore RS0030
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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return methods.methodsWithAttributes.Select(method =>
#pragma warning restore RS0030
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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return methods.methodsWithAttributes.SelectMany(method =>
#pragma warning restore RS0030
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
