// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Experimental.EditorMode
{
    /// <summary>
    /// Indicates how windows that are not-overridden should be treated.
    /// </summary>
    internal enum EditorOverrideMode
    {
        /// <summary>
        /// Every window is treated as supported
        /// </summary>
        PassthroughByDefault,
        /// <summary>
        /// Every window is treated as unsupported, unless overridden manually
        /// </summary>
        UnsupportedByDefault,
    }
}
