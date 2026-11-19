// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Inspector for <see cref="PlacematModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class PlacematFieldsInspector : GraphElementFieldInspector
    {
        /// <summary>
        /// Creates a new instance of the <see cref="VariableFieldsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="SerializedFieldsInspector.CanBeInspected"/>.</param>
        /// <returns>A new instance of <see cref="VariableFieldsInspector"/>.</returns>
        public static PlacematFieldsInspector Create(string name, IReadOnlyList<PlacematModel> models, ChildView ownerElement, string parentClassName, Func<FieldInfo, bool> filter = null)
        {
            if (models.Count > 0)
                return new PlacematFieldsInspector(name, models, ownerElement, parentClassName, filter);
            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableFieldsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="SerializedFieldsInspector.CanBeInspected"/>.</param>
        protected PlacematFieldsInspector(string name, IReadOnlyList<PlacematModel> models, ChildView ownerElement, string parentClassName, Func<FieldInfo, bool> filter)
            : base(name, models, ownerElement, parentClassName, filter) { }

        /// <inheritdoc />
        protected override void GetCustomFields(List<BaseModelPropertyField> outFieldList)
        {
            // Color field
            var colorPropertyField = new ModelPropertyField<Color>(OwnerRootView, m_Models, nameof(PlacematModel.Color), nameof(PlacematModel.Color), "", typeof(ChangeElementColorCommand));
            var colorField = colorPropertyField.Query<ColorField>().First();
            if (colorField != null)
                colorField.showAlpha = false;
            outFieldList.Add(colorPropertyField);
        }
    }
}
