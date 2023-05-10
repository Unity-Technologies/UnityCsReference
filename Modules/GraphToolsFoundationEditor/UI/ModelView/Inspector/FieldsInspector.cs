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
        bool m_SetupLabelWidth = true;
        float m_MaxLabelWidth = float.PositiveInfinity;
        bool m_LayoutDirty;
        bool m_RelayoutScheduled;

        /// <summary>
        /// The number of fields displayed by the inspector.
        /// </summary>
        public int FieldCount => m_Fields.Count;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        public bool SetupLabelWidth
        {
            get => m_SetupLabelWidth;
            set
            {
                if (m_SetupLabelWidth != value)
                {
                    m_SetupLabelWidth = value;
                    m_LayoutDirty = true;
                }
            }
        }

        public float MaxLabelWidth
        {
            get => m_MaxLabelWidth;
            set
            {
                if (m_MaxLabelWidth != value)
                {
                    m_MaxLabelWidth = value;
                    m_LayoutDirty = true;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldsInspector"/> class.
        /// </summary>
        protected FieldsInspector(string name, IReadOnlyList<Model> models, ChildView ownerElement, string parentClassName)
            : base(name, models, ownerElement, parentClassName) { }

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_Fields = new List<CustomizableModelPropertyField>();
            BuildFields();

            parent.Add(m_Root);

            m_Root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            if (!m_RelayoutScheduled)
            {
                m_RelayoutScheduled = true;
                m_Root.schedule.Execute(UpdateLayout).ExecuteLater(0);
            }

            m_Root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_LayoutDirty)
            {
                m_LayoutDirty = false;
                UpdateLayout();
            }
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

            if (!m_RelayoutScheduled)
            {
                m_RelayoutScheduled = true;
                m_Root.schedule.Execute(UpdateLayout).ExecuteLater(0);
            }


            m_Root.EnableInClassList(emptyModifierClassName, m_Fields.Count == 0);
        }

        /// <summary>
        /// Update the layout of the inspector so that every label is as large as the largest label.
        /// </summary>
        public void UpdateLayout()
        {
            m_RelayoutScheduled = false;
            if (SetupLabelWidth)
            {
                float maxLabelWidth = 0;
                foreach (var element in m_Root.Children())
                {
                    if (element is not BaseModelPropertyField field)
                        continue;
                    var label = field.LabelElement;
                    if (label != null && ! label.computedStyle.fontSize.IsNone())
                    {
                        float width = PortContainer.GetLabelTextWidth(label);
                        if (width > maxLabelWidth)
                            maxLabelWidth = width;
                    }
                }

                if (float.IsFinite(m_MaxLabelWidth))
                    maxLabelWidth = Mathf.Min(m_MaxLabelWidth, maxLabelWidth);

                foreach (var element in m_Root.Children())
                {
                    if (element is not BaseModelPropertyField field)
                        continue;
                    var label = field.LabelElement;
                    if (label != null)
                    {
                        label.style.minWidth = maxLabelWidth + label.resolvedStyle.paddingLeft + label.resolvedStyle.paddingRight + label.resolvedStyle.marginLeft + label.resolvedStyle.marginRight;
                        if (float.IsFinite(m_MaxLabelWidth))
                            label.style.maxWidth = m_MaxLabelWidth;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the field to display.
        /// </summary>
        /// <returns>The fields to display.</returns>
        protected abstract IEnumerable<BaseModelPropertyField> GetFields();
    }
}
