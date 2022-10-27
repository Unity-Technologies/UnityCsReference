// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Inspector for <see cref="WireModel"/>.
    /// </summary>
    class WireFieldsInspector : SerializedFieldsInspector
    {
        /// <summary>
        /// Creates a new instance of the <see cref="WireFieldsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="rootView">The root view.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="SerializedFieldsInspector.CanBeInspected"/>.</param>
        /// <returns>A new instance of <see cref="WireFieldsInspector"/>.</returns>
        public static WireFieldsInspector Create(string name, IEnumerable<WireModel> models, RootView rootView, string parentClassName, Func<FieldInfo, bool> filter = null)
        {
            return models.Any() ? new WireFieldsInspector(name, models, rootView, parentClassName, filter) : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WireFieldsInspector"/> class.
        /// </summary>
        protected WireFieldsInspector(string name, IEnumerable<WireModel> models, RootView rootView, string parentClassName, Func<FieldInfo, bool> filter)
            : base(name, models, rootView, parentClassName, filter)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            foreach (var field in base.GetFields())
            {
                yield return field;
            }
        }
    }
}
