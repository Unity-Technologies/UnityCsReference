namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for all bindable fields.
    /// </summary>
    public interface IBindable
    {
        /// <summary>
        /// Binding object that will be updated.
        /// </summary>
        IBinding binding { get; set; }
        /// <summary>
        /// Path of the target property to be bound.
        /// </summary>
        string bindingPath { get; set; }
    }

    /// <summary>
    /// Base interface for Binding objects.
    /// </summary>
    public interface IBinding
    {
        /// <summary>
        /// Called at regular intervals to synchronize bound properties to their IBindable counterparts. Called before the Update() method.
        /// </summary>
        void PreUpdate();
        /// <summary>
        /// Called at regular intervals to synchronize bound properties to their IBindable counterparts. Called before the Update() method.
        /// </summary>
        void Update();
        /// <summary>
        /// Disconnects the field from its bound property
        /// </summary>
        void Release();
    }

    /// <summary>
    /// Extensions methods to provide additional IBindable functionality.
    /// </summary>
    public static class IBindingExtensions
    {
        /// <summary>
        /// Checks if a IBindable is bound to a property.
        /// </summary>
        /// <param name="control">This Bindable object.</param>
        /// <returns>True if this IBindable is bound to a property.</returns>
        public static bool IsBound(this IBindable control)
        {
            return control?.binding != null;
        }
    }
}
