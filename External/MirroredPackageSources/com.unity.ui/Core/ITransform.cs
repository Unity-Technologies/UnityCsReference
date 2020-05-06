namespace UnityEngine.UIElements
{
    /// <summary>
    /// This interface provides access to a VisualElement transform data.
    /// </summary>
    /// <remarks>
    ///
    ///             Reading properties of this object gives information about the tranform of its VisualElement.
    ///             It is possible to write the position, scale and rotation of the transform.
    ///
    /// </remarks>
    public interface ITransform
    {
        /// <summary>
        /// The position of the VisualElement's transform.
        /// </summary>
        Vector3 position { get; set; }
        /// <summary>
        /// The rotation of the VisualElement's transform stored as a Quaternion.
        /// </summary>
        Quaternion rotation { get; set; }
        /// <summary>
        /// The scale of the VisualElement's transform.
        /// </summary>
        Vector3 scale { get; set; }
        /// <summary>
        /// Transformation matrix calculated from the position, rotation and scale of the transform (Read Only).
        /// </summary>
        Matrix4x4 matrix { get; }
    }
}
