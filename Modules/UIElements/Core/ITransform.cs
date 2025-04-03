// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This interface provides access to a VisualElement transform data.
    /// </summary>
    /// <remarks>
    /// Reading properties of this object gives information about the transform of its VisualElement.
    /// It is possible to write the position, scale and rotation of the transform.
    ///
    /// </remarks>
    public interface ITransform
    {
        /// <summary>
        /// The position of the VisualElement transform.
        /// </summary>
        [Obsolete("When reading the value, use VisualElement.resolvedStyle.translate. When writing the value, use VisualElement.style.translate instead.")]
        Vector3 position { get; set; }
        /// <summary>
        /// The rotation of the VisualElement transform stored as a Quaternion.
        /// </summary>
        [Obsolete("When reading the value, use VisualElement.resolvedStyle.rotate. When writing the value, use VisualElement.style.rotate instead.")]
        Quaternion rotation { get; set; }
        /// <summary>
        /// The scale of the VisualElement transform.
        /// </summary>
        [Obsolete("When reading the value, use VisualElement.resolvedStyle.scale. When writing the value, use VisualElement.style.scale instead.")]
        Vector3 scale { get; set; }
        /// <summary>
        /// Transformation matrix calculated from the position, rotation and scale of the transform (Read Only).
        /// </summary>
        /// <remarks>
        /// This matrix does not include the layout offset of the VisualElement, just the resolvedStyle.translate value. You will have to take the layout offset into account when using this matrix for useful results
        /// </remarks>
        Matrix4x4 matrix { get; }
    }
}
