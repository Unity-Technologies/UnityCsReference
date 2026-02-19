// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Profile.Handlers;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Build profile provider for required components. Adds any required components
    /// to a build profile upon creation.
    /// </summary>
    [InitializeOnLoad]
    internal class RequiredComponentsProvider
    {
        static RequiredComponentsProvider()
        {
            BuildProfile.AddOnBuildProfileEnable(SyncComponents);
        }

        internal static void SyncComponents(BuildProfile profile)
        {
            // We cannot add components unless build profile exists in asset db
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(profile)))
                return;

            AddSettingsDataProvider m_AddSettingsDataSource = new AddSettingsDataProvider(profile);
            foreach (var settings in m_AddSettingsDataSource.FetchRequiredPackageSettings())
            {
                if(!settings.HasSettings(profile))
                {
                    settings.OnAdd(profile);
                }
            }
        }
    }
}
