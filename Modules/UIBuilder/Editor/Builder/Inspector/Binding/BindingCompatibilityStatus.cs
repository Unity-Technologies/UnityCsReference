// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.UI.Builder
{
    /// <summary>
    /// Used to indicate whether the source and target properties of a binding can be converted.
    /// </summary>
    enum BindingCompatibilityStatus
    {
        /// <summary>
        /// Used when a converter group is not registered.
        /// </summary>
        Unknown,
        /// <summary>
        /// Used when a converter group is registered and can convert the source and target properties of a binding.
        /// </summary>
        Compatible,
        /// <summary>
        /// Used when a converter group is registered but cannot convert the source and target properties a binding.
        /// </summary>
        Incompatible
    }
}
