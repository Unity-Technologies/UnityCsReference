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
    /// Inspector for <see cref="VariableDeclarationModel"/>.
    /// </summary>
    class VariableFieldsInspector : SerializedFieldsInspector
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
        public static VariableFieldsInspector Create(string name, IReadOnlyList<VariableDeclarationModel> models, ChildView ownerElement, string parentClassName, Func<FieldInfo, bool> filter = null)
        {
            if(models.Any())
                return new VariableFieldsInspector(name, models, ownerElement, parentClassName, filter);
            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableFieldsInspector"/> class.
        /// </summary>
        protected VariableFieldsInspector(string name, IReadOnlyList<VariableDeclarationModel> models, ChildView ownerElement, string parentClassName, Func<FieldInfo, bool> filter)
            : base(name, models, ownerElement, parentClassName, filter)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            foreach (var field in base.GetFields())
            {
                yield return field;
            }

            // Selected Variables must all have an Initialization model of the same TypeHandle to display their default value.
            if (m_Models.OfType<VariableDeclarationModel>().All(t => t.InitializationModel != null) && !m_Models.OfType<VariableDeclarationModel>().Select(t => t.InitializationModel.GetTypeHandle()).Distinct().Skip(1).Any())
            {
                BaseModelPropertyField valueEditor = InlineValueEditor.CreateEditorForConstants(OwnerRootView, m_Models.OfType<VariableDeclarationModel>(), m_Models.OfType<VariableDeclarationModel>().Select(t => t.InitializationModel), false, "Value");

                yield return valueEditor;
            }
        }
    }
}
