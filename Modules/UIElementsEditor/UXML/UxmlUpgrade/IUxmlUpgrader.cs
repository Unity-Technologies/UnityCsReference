// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Interface for UXML upgrade operations that can be applied to <see cref="VisualTreeAsset"/>.
    /// Inherit from this interface to create a new UXML upgrader.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    public interface IUxmlUpgrader
    {
        /// <summary>
        /// The name of this upgrader, displayed to users.
        /// </summary>
        string name { get; }

        /// <summary>
        /// Description of what this upgrader does.
        /// </summary>
        string description { get; }

        /// <summary>
        /// Apply the upgrade to the asset.
        /// </summary>
        /// <param name="asset">The <see cref="VisualTreeAsset"/> to upgrade.</param>
        /// <returns>True if changes were made to the asset.</returns>
        bool Upgrade(VisualTreeAsset asset);
    }
}
