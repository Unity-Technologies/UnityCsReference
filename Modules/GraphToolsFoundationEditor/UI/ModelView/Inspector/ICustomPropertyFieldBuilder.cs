// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.CommandStateObserver;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Common interface for classes that provide a custom UI to edit a field or a property on an object.
    /// </summary>
    interface ICustomPropertyFieldBuilder
    {
        /// <summary>
        /// Builds the UI to edit the property.
        /// </summary>
        /// <param name="commandTargetView">The view hosting this field.</param>
        /// <param name="label">The label that should be displayed in the UI.</param>
        /// <param name="tooltip">The tooltip for the field.</param>
        /// <param name="objects">The owners of the property or field.</param>
        /// <param name="propertyName">The name of the property or field on <see cref="objects"/>.</param>
        /// <returns>The UI for the property, or null if it cannot be built. In this case, a default UI will be used.</returns>
        VisualElement Build(ICommandTarget commandTargetView, string label, string tooltip, IEnumerable<object> objects, string propertyName);

        /// <summary>
        /// Set the displayed value as a mixed value.
        /// </summary>
        void SetMixed();
    }

    /// <summary>
    /// Common interface for classes that provide a custom UI to edit a field or a property of type <typeparamref name="T"/> on an object.
    /// </summary>
    /// <typeparam name="T">The type of the value to display.</typeparam>
    interface ICustomPropertyFieldBuilder<in T> : ICustomPropertyFieldBuilder
    {
        /// <summary>
        /// Update the value displayed by the custom UI.
        /// </summary>
        /// <param name="value">The value to display.</param>
        /// <returns>True if the value was updated.</returns>
        bool UpdateDisplayedValue(T value);
    }
}
