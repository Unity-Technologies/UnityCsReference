namespace UnityEngine.UIElements
{
    /// <summary>
    /// An interface that allows value fields to visually represent a mixed value.
    /// </summary>
    /// <remarks>
    /// A mixed value refers to a situation where a value field is editing more than one value at a time.
    /// For example, when selecting multiple objects and editing the selection in the Inspector window,
    /// selected objects may have different values for the same property.
    /// This situation requires value fields to implement their own logic for supporting this situation.
    /// </remarks>
    public interface IMixedValueSupport
    {
        /// <summary>
        /// Indicates whether to enable the mixed value state on the value field.
        /// </summary>
        bool showMixedValue { get; set; }
    }
}
