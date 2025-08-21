// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        readonly BuildProfile m_BuildProfile;

        /// <summary>
        /// Create data provider for a given build profile.
        /// </summary>
        /// <param name="profile">Editor build profile.</param>
        public AddSettingsDataProvider(BuildProfile profile)
        {
            m_BuildProfile = profile;
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
        }

        /// <summary>
        /// Given a unique key, returns the corresponding settings provider.
        /// </summary>
        public IBuildProfileSettingsProvider Get(int key)
        {
            return s_InternalSettings[key];
        }
    }
}
