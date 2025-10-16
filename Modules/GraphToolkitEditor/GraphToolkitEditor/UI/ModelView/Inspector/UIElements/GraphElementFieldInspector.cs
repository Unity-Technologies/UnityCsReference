// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Inspector for the serializable fields of a <see cref="GraphElementModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class GraphElementFieldInspector : SerializedFieldsInspector
    {
        /// <summary>
        /// Creates a new instance of the <see cref="GraphElementFieldInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="SerializedFieldsInspector.CanBeInspected"/>.</param>
        /// <returns>A new instance of <see cref="GraphElementFieldInspector"/>.</returns>
        public new static GraphElementFieldInspector Create(string name, IReadOnlyList<Model> models, ChildView ownerElement,
            string parentClassName, Func<FieldInfo, bool> filter = null)
        {
            return new GraphElementFieldInspector(name, models, ownerElement, parentClassName, filter);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedFieldsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="SerializedFieldsInspector.CanBeInspected"/>.</param>
        protected GraphElementFieldInspector(string name, IReadOnlyList<Model> models, ChildView ownerElement,
                                             string parentClassName, Func<FieldInfo, bool> filter)
            : base(name, models, ownerElement, parentClassName, filter)
        {
        }

        /// <inheritdoc />
        public override BaseModelPropertyField GetTitleField(IReadOnlyList<object> targets)
        {
            var type = ModelHelpers.GetCommonBaseType(targets);

            if (type == null)
                return null;

            if (OwnerRootView is ModelInspectorView modelInspectorView && typeof(GraphElementModel).IsAssignableFrom(type) && typeof(IHasTitle).IsAssignableFrom(type))
            {
                return new GraphElementTitlePropertyField(modelInspectorView, targets.OfTypeToList<IHasTitle, object>());
            }
            return null;
        }
    }
}
