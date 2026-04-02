// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    static class BindingUtility
    {
        /// <summary>
        /// Finds the binding that binds the specified property of the selected VisualElement to a data source.
        /// </summary>
        /// <param name="property">The property for which we seek a related binding.</param>
        /// <param name="binding">The binding instance we seek.</param>
        /// <param name="uxmlBindingAsset">The uxml element related to the binding instance.</param>
        /// <returns>Returns true if a binding on the specified property is found; otherwise returns false.</returns>
        public static bool TryGetBinding(this VisualElement element, string property,
            out Binding binding, out UxmlObjectAsset uxmlBindingAsset)
        {
            return TryGetBinding(element, element.visualElementAsset, property, out binding, out uxmlBindingAsset);
        }

        /// <summary>
        /// Finds the binding that binds the specified property of the selected VisualElement to a data source.
        /// </summary>
        /// <param name="elementAsset">The VisualElementAsset related to the VisualElement for which we seek a related binding.</param>
        /// <param name="property">The property for which we seek a related binding.</param>
        /// <param name="binding">The binding instance we seek.</param>
        /// <param name="uxmlBindingAsset">The uxml element related to the binding instance.</param>
        /// <returns>Returns true if a binding on the specified property is found; otherwise returns false.</returns>
        public static bool TryGetBinding(this VisualElement element, VisualElementAsset elementAsset, string property, out Binding binding, out UxmlObjectAsset uxmlBindingAsset)
        {
            binding = null;
            uxmlBindingAsset = null;
            var currentVe = element;
            var vea = elementAsset;

            binding = null;
            uxmlBindingAsset = null;

            // VisualElementAsset can be null when the inspected element is coming from a template instance.
            if (vea == null)
                return false;

            if (DataBindingUtility.TryGetBinding(currentVe, new PropertyPath(property), out var bindingInfo))
            {
                binding = bindingInfo.binding;
                uxmlBindingAsset = vea.FindUxmlBinding(property);
            }

            return uxmlBindingAsset != null;
        }
    }
}
