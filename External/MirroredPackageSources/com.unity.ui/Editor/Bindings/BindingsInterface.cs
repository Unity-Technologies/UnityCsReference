using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.UIElements.Bindings;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal interface ISerializedObjectBindingImplementation
    {
        void Bind(VisualElement element, SerializedObject obj);
        void Unbind(VisualElement element);
        SerializedProperty BindProperty(IBindable field, SerializedObject obj);
        void BindProperty(IBindable field, SerializedProperty property);

        void Bind(VisualElement element, object bindingContext, SerializedProperty parentProperty);

        void TrackPropertyValue(VisualElement element, SerializedProperty property, Action<SerializedProperty> callback);

        void HandleStyleUpdate(VisualElement element);
    }

    /// <summary>
    /// Provides VisualElement extension methods that implement data binding between INotivyValueChanged fields and SerializedObjects.
    /// </summary>
    public static class BindingExtensions
    {
        /// <summary>
        /// USS class added to element when in prefab override mode.
        /// </summary>
        public static readonly string prefabOverrideUssClassName = "unity-binding--prefab-override";
        internal static readonly string prefabOverrideBarName = "unity-binding-prefab-override-bar";
        internal static readonly string prefabOverrideBarContainerName = "unity-prefab-override-bars-container";
        internal static readonly string prefabOverrideBarUssClassName = "unity-binding__prefab-override-bar";
        internal static readonly string animationAnimatedUssClassName = "unity-binding--animation-animated";
        internal static readonly string animationRecordedUssClassName = "unity-binding--animation-recorded";
        internal static readonly string animationCandidateUssClassName = "unity-binding--animation-candidate";

        internal static ISerializedObjectBindingImplementation bindingImpl =
            new DefaultSerializedObjectBindingImplementation();

        /// <summary>
        /// Binds a SerializedObject to fields in the element hierarchy.
        /// </summary>
        /// <param name="element">Root VisualElement containing IBindable fields.</param>
        /// <param name="obj">Data object.</param>
        public static void Bind(this VisualElement element, SerializedObject obj)
        {
            bindingImpl.Bind(element, obj);
        }

        /// <summary>
        /// Disconnects all properties bound to fields in the element's hierarchy.
        /// </summary>
        /// <param name="element">Root VisualElement containing IBindable fields.</param>
        public static void Unbind(this VisualElement element)
        {
            bindingImpl.Unbind(element);
        }

        /// <summary>
        /// Binds a property to a field and synchronizes their values. This method finds the property using the field's binding path.
        /// </summary>
        /// <param name="field">VisualElement field editing a property.</param>
        /// <param name="obj">Root SerializedObject containing the bindable property.</param>
        /// <returns>The serialized object that owns the bound property.</returns>
        public static SerializedProperty BindProperty(this IBindable field, SerializedObject obj)
        {
            return bindingImpl.BindProperty(field, obj);
        }

        /// <summary>
        /// Binds a property to a field and synchronizes their values.
        /// </summary>
        /// <param name="field">VisualElement field editing a property.</param>
        /// <param name="property">The SerializedProperty to bind.</param>
        public static void BindProperty(this IBindable field, SerializedProperty property)
        {
            bindingImpl.BindProperty(field, property);
        }

        internal static void HandleStyleUpdate(VisualElement element)
        {
            bindingImpl.HandleStyleUpdate(element);
        }

        /// <summary>
        /// Checks the property values for changes every frame. Executes the callback when the property value changes.
        /// If no callback is specified, a SerializedPropertyChangeEvent is sent to the target element.
        /// </summary>
        /// <param name="element">VisualElement tracking a property.</param>
        /// <param name="property">The SerializedProperty to track.</param>
        /// <param name="callback">Invoked when the tracked SerializedProperty value changes.</param>
        public static void TrackPropertyValue(this VisualElement element, SerializedProperty property, Action<SerializedProperty> callback = null)
        {
            bindingImpl.TrackPropertyValue(element, property, callback);
        }
    }
}
