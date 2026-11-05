// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// A build profile component that overrides the global quality levels for the build profile's platform.
    /// </summary>
    [ExcludeFromObjectFactory]
    [ExcludeFromPreset]
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    public sealed class BuildProfileQualitySettings : ScriptableObject
    {
        [SerializeField] string m_DefaultQualityLevel = string.Empty;
        [SerializeField] string[] m_QualityLevels = Array.Empty<string>();

        internal BuildProfileQualitySettings()
        {
        }

        /// <summary>
        /// The name of the quality level that the build uses at startup.
        /// </summary>
        public string defaultQualityLevel
        {
            get => m_DefaultQualityLevel;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (value == m_DefaultQualityLevel)
                    return;

                if (Array.IndexOf(m_QualityLevels, value) == -1)
                    throw new ArgumentException($"'{value}' isn't one of this build profile's quality levels.", nameof(value));

                m_DefaultQualityLevel = value;
                EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// The names of the quality levels to include in the build.
        /// </summary>
        public string[] qualityLevels
        {
            get => (string[])m_QualityLevels.Clone();
            set
            {
                ThrowIfInvalidQualityLevels(value);

                m_QualityLevels = (string[])value.Clone();
                if (Array.IndexOf(m_QualityLevels, m_DefaultQualityLevel) == -1)
                    m_DefaultQualityLevel = m_QualityLevels.Length > 0 ? m_QualityLevels[0] : string.Empty;

                EditorUtility.SetDirty(this);
            }
        }

        static void ThrowIfInvalidQualityLevels(string[] qualityLevels)
        {
            if (qualityLevels == null)
                throw new ArgumentNullException(nameof(qualityLevels));

            var projectQualityLevels = QualitySettings.names;
            for (int i = 0; i < qualityLevels.Length; i++)
            {
                var qualityLevel = qualityLevels[i];
                if (string.IsNullOrEmpty(qualityLevel))
                    throw new ArgumentException($"The quality level at index {i} is null or empty.", nameof(qualityLevels));
                if (Array.IndexOf(projectQualityLevels, qualityLevel) == -1)
                    throw new ArgumentException($"'{qualityLevel}' isn't a quality level in the project.", nameof(qualityLevels));
                if (Array.IndexOf(qualityLevels, qualityLevel, 0, i) != -1)
                    throw new ArgumentException($"'{qualityLevel}' appears more than once.", nameof(qualityLevels));
            }
        }

        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal void ResetToGlobalValues(BuildProfile profile)
        {
            var namedBuildTargetName = NamedBuildTarget.FromTargetAndSubtarget(profile.buildTarget, (int)profile.subtarget).TargetName;
            var globalQualityLevels = QualitySettings.GetActiveQualityLevelsForPlatform(namedBuildTargetName);

            var newQualityLevels = new string[globalQualityLevels.Length];

            // populates newQualityLevels array with global quality levels in their existing order
            for (int i = 0; i < globalQualityLevels.Length; i++)
            {
                newQualityLevels[i] = QualitySettings.names[globalQualityLevels[i]];
            }

            var globalDefaultQualityLevelIndex = QualitySettings.GetDefaultQualityForPlatform(namedBuildTargetName);
            if (globalDefaultQualityLevelIndex != -1)
                m_DefaultQualityLevel = QualitySettings.names[globalDefaultQualityLevelIndex];
            else
                m_DefaultQualityLevel = newQualityLevels.Length > 0 ? QualitySettings.names[globalQualityLevels[0]] : string.Empty;

            m_QualityLevels = newQualityLevels;

            EditorUtility.SetDirty(this);
        }

        internal void Instantiate()
        {
            name = "Quality Settings";
            hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
        }

        internal void RemoveQualityLevel(string qualityLevel)
        {
            var index = Array.IndexOf(m_QualityLevels, qualityLevel);
            if (index == -1)
                return;

            var newQualityLevels = new string[m_QualityLevels.Length - 1];
            Array.Copy(m_QualityLevels, 0, newQualityLevels, 0, index);
            Array.Copy(m_QualityLevels, index + 1, newQualityLevels, index, m_QualityLevels.Length - index - 1);
            m_QualityLevels = newQualityLevels;

            if (m_DefaultQualityLevel == qualityLevel)
                m_DefaultQualityLevel = m_QualityLevels.Length > 0 ? m_QualityLevels[0] : string.Empty;

            EditorUtility.SetDirty(this);
        }

        internal void RenameQualityLevel(string oldName, string newName)
        {
            var index = Array.IndexOf(m_QualityLevels, oldName);
            if (index == -1)
                return;

            m_QualityLevels[index] = newName;

            if (m_DefaultQualityLevel == oldName)
                m_DefaultQualityLevel = newName;

            EditorUtility.SetDirty(this);
        }

        internal void AddQualityLevel(string qualityLevel)
        {
            var newQualityLevels = new string[m_QualityLevels.Length + 1];
            Array.Copy(m_QualityLevels, 0, newQualityLevels, 0, m_QualityLevels.Length);
            newQualityLevels[m_QualityLevels.Length] = qualityLevel;
            m_QualityLevels = newQualityLevels;

            EditorUtility.SetDirty(this);
        }
    }
}
