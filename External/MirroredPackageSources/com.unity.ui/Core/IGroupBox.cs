namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for options that need to be handled together in a <see cref="IGroupBox"/>.
    /// Visual elements should inherit from this interface in order to be treated by the <see cref="IGroupManager"/>.
    /// Make sure to call <see cref="GroupBoxUtility.RegisterGroupBoxOptionCallbacks"/> to register your option
    /// to panel events, and <see cref="GroupBoxUtility.OnOptionSelected"/> when it changes.
    /// </summary>
    internal interface IGroupBoxOption
    {
        /// <summary>
        /// Implements the selected state for this element.
        /// </summary>
        /// <param name="selected">If the option should be displayed as selected.</param>
        void SetSelected(bool selected);
    }

    /// <summary>
    /// Add this interface to any <see cref="VisualElement"/> that should be considered as an enclosing container
    /// for a group of <see cref="IGroupBoxOption"/>. All group options within this container will interact together
    /// using the default implementation, <see cref="DefaultGroupManager"/>.
    /// If no <see cref="IGroupBox"/> is found in the hierarchy, the default container will be the panel.
    /// <seealso cref="IGroupBox{T}"/> if you want to override the default group manager for this group box.
    /// </summary>
    internal interface IGroupBox
    {
        // This interface was left empty on purpose. This type is only used as a marker on VisualElements.
    }

    /// <summary>
    /// Add this interface to any <see cref="VisualElement"/> that should be considered as an enclosing container
    /// for a group of <see cref="IGroupBoxOption"/>. All group options within this container will interact together
    /// using the provided group manager type.
    /// If no <see cref="IGroupBox"/> is found in the hierarchy, the default container will be the panel.
    /// </summary>
    /// <typeparam name="T">The type of the group manager to instantiate for this <see cref="IGroupBox"/></typeparam>
    internal interface IGroupBox<T> : IGroupBox where T : IGroupManager
    {
        // This interface was left empty on purpose. This type is only used as a marker on VisualElements.
    }
}
