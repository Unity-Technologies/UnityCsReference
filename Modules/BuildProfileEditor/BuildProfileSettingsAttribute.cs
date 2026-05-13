// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Marks a static method as the entry point for registering a custom settings foldout
    /// in the Build Profile window. The decorated method must return a
    /// <see cref="BuildProfileSettingsProvider"/> instance for the given settings type,
    /// which must be a subclass of <see cref="ScriptableObject"/>.
    /// </summary>
    /// <remarks>
    /// Only methods in assemblies from Unity package registry sources are supported.
    /// </remarks>
    /// <example>
    /// <code>
    /// class SampleSettings : ScriptableObject
    /// {
    ///     public bool myBoolSetting;
    /// 
    ///     [BuildProfileSettingsProvider(typeof(SampleSettings))]
    ///     static BuildProfileSettingsProvider createProvider() => new BuildProfileSettingsProvider("MySampleSettings")
    ///     {
    ///         tooltip = "Sample settings provider for a ScriptableObject.",
    ///         canAddSetting = profile => true
    ///     };
    /// }
    /// </code>
    /// </example>
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
