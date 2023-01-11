// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for UI parts that display a list of <see cref="CustomizableModelPropertyField"/>.
    /// </summary>
    abstract class FieldsInspector : BaseMultipleModelViewsPart
    {
        public static readonly string ussClassName = "ge-inspector-fields";
        public static readonly string emptyModifierClassName = ussClassName.WithUssModifier("empty");

        protected VisualElement m_Root;
        protected List<CustomizableModelPropertyField> m_Fields;

        /// <summary>
        /// The number of fields displayed by the inspector.
        /// </summary>
        public int FieldCount => m_Fields.Count;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldsInspector"/> class.
        /// </summary>
        protected FieldsInspector(string name, IEnumerable<Model> models, RootView rootView, string parentClassName)
            : base(name, models, rootView, parentClassName) { }

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_Fields = new List<CustomizableModelPropertyField>();
            BuildFields();

            parent.Add(m_Root);
        }

        /// <inheritdoc />
        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_Root.AddStylesheet_Internal("FieldInspector.uss");
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            foreach (var modelField in m_Fields)
            {
                modelField.UpdateDisplayedValue();
            }
        }

        /// <summary>
        /// Fill the inspector with fields.
        /// </summary>
        protected void BuildFields()
        {
            m_Fields.Clear();
            m_Root.Clear();

            foreach (var field in GetFields())
            {
                if (field is CustomizableModelPropertyField modelPropertyField)
                    m_Fields.Add(modelPropertyField);

                m_Root.Add(field);
            }

            m_Root.EnableInClassList(emptyModifierClassName, m_Fields.Count == 0);
        }

        /// <summary>
        /// Gets the field to display.
        /// </summary>
        /// <returns>The fields to display.</returns>
        protected abstract IEnumerable<BaseModelPropertyField> GetFields();
    }
}
