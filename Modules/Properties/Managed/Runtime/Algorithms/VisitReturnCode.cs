// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Properties
{
    /// <summary>
    /// Internal return code used during path visitation.
    /// </summary>
    public enum VisitReturnCode
    {
        /// <summary>
        /// The visitation resolved successfully.
        /// </summary>
        Ok,

        /// <summary>
        /// The container being visited was null.
        /// </summary>
        NullContainer,

        /// <summary>
        /// The given container type is not valid for visitation.
        /// </summary>
        InvalidContainerType,

        /// <summary>
        /// No property bag was found for the given container type.
        /// </summary>
        MissingPropertyBag,

        /// <summary>
        /// Failed to resolve some part of the path (e.g. Name, Index or Key).
        /// </summary>
        InvalidPath,

        /// <summary>
        /// Failed to reinterpret the target value as the requested type.
        /// </summary>
        InvalidCast,

        /// <summary>
        /// Failed to set value at path because it is read-only.
        /// </summary>
        AccessViolation
    }
}
