// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.InternalBridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for UI parts that display a list of <see cref="CustomizableModelPropertyField"/>.
    /// </summary>
    [UnityRestricted]
    internal abstract class FieldsInspector : BaseMultipleModelViewsPart
    {
        /// <summary>
        /// The USS class name added to a <see cref="FieldsInspector"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-inspector-fields";

        /// <summary>
        /// The USS class name added to a <see cref="FieldsInspector"/> when it is empty.
        /// </summary>
        public static readonly string emptyUssClassName = ussClassName.WithUssModifier(GraphElementHelper.emptyUssModifier);

        protected VisualElement m_Root;
        protected List<BaseModelPropertyField> m_Fields;
        bool m_SetupLabelWidth = true;
        float m_MaxLabelWidth = float.PositiveInfinity;
        bool m_LayoutDirty;
        bool m_RelayoutScheduled;

        /// <summary>
        /// The number of fields displayed by the inspector.
        /// </summary>
        public int FieldCount => m_Fields.Count;

        /// <summary>
        /// Whether the inspector is empty.
        /// </summary>
        public virtual bool IsEmpty => FieldCount == 0;

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
        protected override void BuildUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_Fields = new List<BaseModelPropertyField>();
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
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
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
        /// Returns the field that will take the place of the inspector title. Return null to have the default title.
        /// </summary>
        /// <param name="targets"> The inspected elements.</param>
        /// <returns>the field that will take the place of the inspector title.</returns>
        public virtual BaseModelPropertyField GetTitleField(IReadOnlyList<object> targets)
        {
            return null;
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
                m_Fields.Add(field);

                m_Root.Add(field);
            }

            if (!m_RelayoutScheduled)
            {
                m_RelayoutScheduled = true;
                m_Root.schedule.Execute(UpdateLayout).ExecuteLater(0);
            }


            m_Root.EnableInClassList(emptyUssClassName, m_Fields.Count == 0);
        }

        /// <summary>
        /// Update the layout of the inspector so that every label is as large as the largest label.
        /// </summary>
        public void UpdateLayout()
        {
            if (m_Root?.panel == null)
                return;

            m_RelayoutScheduled = false;
            if (SetupLabelWidth)
            {
                float maxLabelWidth = 0;

                UpdateMaxLabelWidth(m_Root, false, ref maxLabelWidth);

                if (float.IsFinite(m_MaxLabelWidth))
                    maxLabelWidth = Mathf.Min(m_MaxLabelWidth, maxLabelWidth);

                UpdateMaxLabelWidth(m_Root, true, ref maxLabelWidth);
            }

            return;

            void UpdateMaxLabelWidth(VisualElement root, bool shouldSetLabelsWidth, ref float maxLabelWidth)
            {
                foreach (var child in root.Children())
                {
                    var field = child as BaseModelPropertyField;
                    var label = field?.LabelElement ?? child as Label;
                    var isPropertyField = label?.ClassListContains(BaseModelPropertyField.labelUssClassName) ?? false;

                    if (label is { panel: not null } && isPropertyField)
                    {
                        var labelPosition = label.parent.ChangeCoordinatesTo(Root, label.localBound.position); //needed for sub ports label that are offset.
                        if (shouldSetLabelsWidth)
                        {
                            label.style.minWidth = maxLabelWidth + label.resolvedStyle.paddingLeft + label.resolvedStyle.paddingRight + label.resolvedStyle.marginLeft + label.resolvedStyle.marginRight - labelPosition.x;
                            if (float.IsFinite(m_MaxLabelWidth))
                                label.style.maxWidth = m_MaxLabelWidth;
                        }
                        else if (!float.IsNaN(label.resolvedStyle.fontSize))
                        {
                            var width = label.GetTextWidth() + labelPosition.x;
                            if (width > maxLabelWidth)
                                maxLabelWidth = width;
                        }
                    }

                    UpdateMaxLabelWidth(child, shouldSetLabelsWidth, ref maxLabelWidth);
                }
            }
        }

        /// <summary>
        /// Gets the field to display.
        /// </summary>
        /// <returns>The fields to display.</returns>
        protected abstract IReadOnlyList<BaseModelPropertyField> GetFields();
    }
}
