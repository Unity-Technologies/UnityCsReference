// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.AdaptivePerformance.Editor.Metadata
{
    /// <summary>
    /// Implement this interface to provide package-level information and actions.
    ///
    /// Adaptive Performance Provider Management will reflect on all types in the project to find implementers
    /// of this interface. These instances are used to get information required to integrate
    /// your package with the Adaptive Performance Provider Management system.
    /// </summary>
    public interface IAdaptivePerformancePackage
    {
        /// <summary>
        /// Returns an instance of <see cref="IAdaptivePerformancePackageMetadata"/>. Information will be used
        /// to allow the Adaptive Performance Provider Management to provide settings and loaders through the settings UI.
        /// </summary>
        IAdaptivePerformancePackageMetadata metadata { get; }

        /// <summary>
        /// Allows the package to configure new settings passed in.
        /// </summary>
        /// <param name="obj">ScriptableObject instance that represents an instance of the settings
        /// type provided by <see cref="IAdaptivePerformancePackageMetadata.settingsType"/>.</param>
        /// <returns>True if the operation succeeded, false if not. If implementation is empty, just return true.</returns>
        bool PopulateNewSettingsInstance(ScriptableObject obj);
    }
}
