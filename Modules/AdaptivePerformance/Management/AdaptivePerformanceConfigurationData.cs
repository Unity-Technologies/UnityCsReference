// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// This attribute is used to tag classes as providing build settings support for an Adaptive Performance provider. The unified setting system
    /// will present the settings as an inspectable object in the Project Settings window using the built-in inspector UI.
    ///
    /// The implementor of the settings is able to create their own custom UI and the Project Settings system will use that UI in
    /// place of the build-in one in the Inspector. See the <see href="https://docs.unity3d.com/Manual/ExtendingTheEditor.html">Extending the Editor</see>
    /// page in the Unity Manual for more information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AdaptivePerformanceConfigurationDataAttribute : Attribute
    {
        /// <summary>
        /// The display name that the user sees in the Project Settings window.
        /// </summary>
        public string displayName { get; set; }

        /// <summary>
        /// The key used to store the singleton instance of these settings within `EditorBuildSettings`.
        /// </summary>
        /// <remarks>
        /// To access the configuration settings instance, retrieve the object from [[EditorBuildSettings]] using this key.
        /// </remarks>
        public string buildSettingsKey { get; set; }

        private AdaptivePerformanceConfigurationDataAttribute() {}

        /// <summary>Constructor for attribute</summary>
        /// <param name="displayName">The display name to use in the Project Settings window.</param>
        /// <param name="buildSettingsKey">The key to use to get or set build settings with.</param>
        public AdaptivePerformanceConfigurationDataAttribute(string displayName, string buildSettingsKey)
        {
            this.displayName = displayName;
            this.buildSettingsKey = buildSettingsKey;
        }
    }
}
