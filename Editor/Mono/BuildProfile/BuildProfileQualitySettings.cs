// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile
{
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    sealed class BuildProfileQualitySettings : ScriptableObject
    {
        [SerializeField] string m_DefaultQualityLevel = string.Empty;
        [SerializeField] string[] m_QualityLevels = Array.Empty<string>();

        public string defaultQualityLevel
        {
            get => m_DefaultQualityLevel;
            set => m_DefaultQualityLevel = value;
        }

        public string[] qualityLevels
        {
            get => m_QualityLevels;
            set => m_QualityLevels = value;
        }

        public void Instantiate()
        {
            name = "Quality Settings";
            hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
        }

        public void RemoveQualityLevel(string qualityLevel)
        {
            var index = Array.IndexOf(qualityLevels, qualityLevel);
            if (index == -1)
                return;

            var newQualityLevels = new string[qualityLevels.Length - 1];
            Array.Copy(qualityLevels, 0, newQualityLevels, 0, index);
            Array.Copy(qualityLevels, index + 1, newQualityLevels, index, qualityLevels.Length - index - 1);
            qualityLevels = newQualityLevels;

            if (defaultQualityLevel == qualityLevel)
                defaultQualityLevel = qualityLevels.Length > 0 ? qualityLevels[0] : string.Empty;

            EditorUtility.SetDirty(this);
        }

        public void RenameQualityLevel(string oldName, string newName)
        {
            var index = Array.IndexOf(qualityLevels, oldName);
            if (index == -1)
                return;

            qualityLevels[index] = newName;

            if (defaultQualityLevel == oldName)
                defaultQualityLevel = newName;

            EditorUtility.SetDirty(this);
        }

        public void AddQualityLevel(string qualityLevel)
        {
            var newQualityLevels = new string[qualityLevels.Length + 1];
            Array.Copy(qualityLevels, 0, newQualityLevels, 0, qualityLevels.Length);
            newQualityLevels[qualityLevels.Length] = qualityLevel;
            qualityLevels = newQualityLevels;

            EditorUtility.SetDirty(this);
        }
    }
}
