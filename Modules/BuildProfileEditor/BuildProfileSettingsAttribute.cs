// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Configuration class specifying if a settings object applies for a build profile and
    /// how it should be displayed in the editor. When defined, the given settings object can be
    /// added to a selected build profile through the build profile editor UI.
    ///
    /// Currently only implementations from specific Unity packages are supported.
    /// </summary>
    /// <remarks>
    /// Expects a static method that returns a <see cref="BuildProfileSettingsProvider"/> instance for a given
    /// settings type, which must be a subclass of <see cref="ScriptableObject"/>.
    /// </remarks>
    /// <code>
    /// class SampleSettings : ScriptableObject
    /// {
    ///     public bool myBoolSetting;
    /// 
    ///     [BuildProfileSettings(typeof(SampleSettings))]
    ///     static BuildProfileSettingsProvider createProvider() => new BuildProfileSettingsProvider("MySampleSettings")
    ///     {
    ///         tooltip = "Sample settings provider for a ScriptableObject."
    ///     };
    /// }
    /// </code>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class BuildProfileSettingsProviderAttribute : Attribute
    {
        internal Type settingsType { get; set; }

        /// <summary>
        /// Specifies a new settings provider for a build profile.
        /// </summary>
        /// <param name="type">Settings type managed by this settings provider.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="type"/> is not a <see cref="ScriptableObject"/>.</exception>
        public BuildProfileSettingsProviderAttribute(Type type)
        {
            var soType = typeof(ScriptableObject);
            if (!type.IsClass || !soType.IsAssignableFrom(type))
            {
                throw new ArgumentException($"Type {type.FullName} must be a subclass of ScriptableObject.");
            }

            settingsType = type;
        }
    }
}
