// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// The number of textures that UI Toolkit can bind simultaneously to the shader to reduce draw calls.
    /// </summary>
    /// <remarks>
    /// SA: [[PanelSettings.textureSlotCount]]
    /// </remarks>
    public enum TextureSlotCount
    {
        /// <summary>
        /// UI Toolkit can bind only one texture to the shader at once. No dynamic branching is required to select the texture.
        /// </summary>
        One = 1,

        /// <summary>
        /// UI Toolkit can bind up to two textures to the shader at once. It uses one dynamic branch to select the appropriate texture.
        /// </summary>
        Two = 2,

        /// <summary>
        /// UI Toolkit can bind up to four textures to the shader at once. It uses two nested dynamic branches to select the appropriate texture.
        /// </summary>
        Four = 4,

        /// <summary>
        /// UI Toolkit can bind up to eight textures to the shader at once. It uses three nested dynamic branches to select the appropriate texture.
        /// /// </summary>
        Eight = 8,
    }
}
