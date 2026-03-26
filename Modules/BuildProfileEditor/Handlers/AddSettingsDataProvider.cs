// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build.Profile.AdaptivePerformance;
using UnityEditor.Build.Profile.Elements;
using UnityEditor.Build.Profile.Internal;

namespace UnityEditor.Build.Profile.Handlers
{
    /// <summary>
    /// Build profile editor setting foldout data provider. Tracks
    /// ordered collection of all possible build profile setting sections.
    /// </summary>
    class AddSettingsDataProvider : IAddSettingsDataProvider
    {
        static readonly IList<IBuildProfileSettingsProvider> s_InternalSettings = new List<IBuildProfileSettingsProvider>
        {
            new SceneListProvider(),
            new ScriptingDefinesSettings(),
            new PlayerSettingsProvider(),
            new QualitySettingsProvider(),
            new Elements.GraphicsSettingsProvider(),
            new BuildAutomationSettingsProvider(),
            new AdaptivePerformanceSettingProvider()
        };

        static IList<IBuildProfileSettingsProvider> s_GenericSettingProviders = null;

        readonly BuildProfile m_BuildProfile;

        /// <summary>
        /// Create data provider for a given build profile.
        /// </summary>
        /// <param name="profile">Editor build profile.</param>
        public AddSettingsDataProvider(BuildProfile profile)
        {
            m_BuildProfile = profile;

            if (s_GenericSettingProviders != null)
            {
                return;
            }

            var foundPackageProviders = PackageSettingsProvider.Get();
            var genericSettingProviders = new List<IBuildProfileSettingsProvider>(foundPackageProviders.Count);
            for (int i = 0; i < foundPackageProviders.Count; i++)
            {
                var found = foundPackageProviders[i];
                var typedInternalProvider = typeof(ScriptableObjectSettingsProvider<>).MakeGenericType(found.settingsType);
                genericSettingProviders.Add((IBuildProfileSettingsProvider)Activator.CreateInstance(typedInternalProvider, found));
            }
            genericSettingProviders.Sort((a, b) => a.GetDisplayOrder().CompareTo(b.GetDisplayOrder()));
            s_GenericSettingProviders = genericSettingProviders;
        }

        /// <summary>
        /// Returns a collection of all available settings that can be added to the
        /// current profile.
        /// </summary>
        /// <returns>
        ///     Tuple<int, string> collection, where the int uniquely identifies a provider.
        ///     <see cref="Get(int)"/>
        /// </returns>
        public IEnumerable<(int key, string displayName)> FetchSettings()
        {
            for (int i = 0; i < s_InternalSettings.Count; ++i)
            {
                var provider = s_InternalSettings[i];
                if (provider.CanAddSettings(m_BuildProfile) && !provider.HasSettings(m_BuildProfile))
                    yield return (i, provider.GetDisplayName());
            }

            for (int i = 0; i < s_GenericSettingProviders.Count; ++i)
            {
                var provider = s_GenericSettingProviders[i];
                if (provider.CanAddSettings(m_BuildProfile) && !provider.HasSettings(m_BuildProfile))
                    yield return (s_InternalSettings.Count + i, provider.GetDisplayName());
            }
        }

        public IEnumerable<IBuildProfileSettingsProvider> FetchRequiredPackageSettings()
        {
            foreach (var provider in s_GenericSettingProviders)
            {
                if (provider.GetIsRequired() && provider.CanAddSettings(m_BuildProfile) && !provider.HasSettings(m_BuildProfile))
                    yield return provider;
            }
        }

        /// <summary>
        /// Returns a collection of all settings in the current profile.
        /// </summary>
        public IEnumerable<IBuildProfileSettingsProvider> GetSettingsInProfile()
        {
            foreach (var provider in s_InternalSettings)
            {
                if (provider.HasSettings(m_BuildProfile))
                    yield return provider;
            }

            foreach (var provider in s_GenericSettingProviders)
            {
                if (provider.HasSettings(m_BuildProfile))
                    yield return provider;
            }
        }

        /// <summary>
        /// Given a unique key, returns the corresponding settings provider.
        /// </summary>
        public IBuildProfileSettingsProvider Get(int key)
        {
            var offset = s_InternalSettings.Count;
            if (key < offset)
                return s_InternalSettings[key];
            else
                return s_GenericSettingProviders[key - offset];
        }

        /// <summary>
        /// Checks if all available profile settings are in use.
        /// </summary>
        /// <returns><see langword="true"/> if all profile settings are in use; otherwise, <see langword="false"/>.</returns>
        public bool AllProfileSettingsInUse()
        {
            foreach (var provider in s_InternalSettings)
            {
                if (!provider.CanAddSettings(m_BuildProfile) || provider.HasSettings(m_BuildProfile))
                    continue;

                return false;
            }

            return true;
        }
    }
}
