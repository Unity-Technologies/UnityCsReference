// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
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
        const float k_LabelWidthBuffer = 4f;

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
        float m_MaxLabelWidthOverride = float.PositiveInfinity;
        bool m_LayoutDirty;
        bool m_RelayoutScheduled;
        float m_PreviousValidMinLabelWidth;
        bool m_AreFieldsAttached;
        float m_LabelFontSize = 12f;

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
            get => m_MaxLabelWidthOverride;
            set
            {
                if (!Mathf.Approximately(m_MaxLabelWidthOverride, value))
                {
                    m_MaxLabelWidthOverride = value;
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
            m_AreFieldsAttached = false;

            foreach (var field in GetFields())
            {
                 // Register to the AttachToPanelEvent of each field to know when the fields are attached to the panel.
                 // This is needed to know when we can compute the label width, which requires the fields to be attached to the panel.
                 field.RegisterCallback<AttachToPanelEvent>(_ =>
                 {
                     m_AreFieldsAttached = true;
                 });

                 // Get the font size from the field label.
                 field.RegisterCallback<GeometryChangedEvent>(_ =>
                 {
                    if (field.LabelElement != null)
                        m_LabelFontSize = field.LabelElement.resolvedStyle.fontSize;
                 });

                 m_Fields.Add(field);

                 m_Root.Add(field);
            }

            if (!m_RelayoutScheduled)
            {
                m_RelayoutScheduled = true;
                m_Root.schedule.Execute(UpdateLayout).ExecuteLater(0);
            }

            m_Root.EnableInClassList(emptyUssClassName, m_Fields.Count == 0);

            EnsurePropertyViewIsHiddenIfNeeded();
        }

        void EnsurePropertyViewIsHiddenIfNeeded()
        {
            // If the inspector is in a BlackboardField, hide the property view of the blackboard field when there are no fields to display.
            var blackboardField = m_Root.GetFirstAncestorOfType<BlackboardField>();

            if (blackboardField == null)
            {
                // If the blackboard field is not found, it might be because the inspector is not yet added to the blackboard field.
                m_Root.schedule.Execute(EnsurePropertyViewIsHiddenIfNeeded).ExecuteLater(0);
                return;
            }

            blackboardField.HidePropertyView(m_Fields.Count == 0);
        }

        /// <summary>
        /// Update the layout of the inspector so that every label is as large as the largest label.
        /// </summary>
        public void UpdateLayout()
        {
            if (m_Root?.panel == null || !m_AreFieldsAttached)
                return;

            m_RelayoutScheduled = false;
            if (SetupLabelWidth)
            {
                float minLabelWidth = 0;

                // Find the minimum label width required so that each label is as large as the largest label.
                ComputeMinLabelWidth(m_Root, ref minLabelWidth);

                // Set the minimum label width for each label.
                SetMinLabelWidth(m_Root, minLabelWidth + k_LabelWidthBuffer);
            }
        }

        void ComputeMinLabelWidth(VisualElement root, ref float minLabelWidth)
        {
            foreach (var child in root.Children())
            {
                var field = child as BaseModelPropertyField;
                var label = field?.LabelElement ?? child as Label;
                var isPropertyField = label?.ClassListContains(BaseModelPropertyField.labelUssClassName) ?? false;

                if (label is { panel: not null } && isPropertyField)
                {
                    var labelPosition = label.parent.ChangeCoordinatesTo(Root, label.localBound.position); //needed for sub ports label that are offset.
                    if (!float.IsNaN(m_LabelFontSize))
                    {
                        var width = label.MeasureTextSize(label.text, float.NaN, VisualElement.MeasureMode.Undefined, float.NaN, VisualElement.MeasureMode.Undefined, m_LabelFontSize).x + labelPosition.x;
                        if (width > minLabelWidth)
                            minLabelWidth = width;
                    }
                }

                ComputeMinLabelWidth(child, ref minLabelWidth);
            }

            // If the m_MaxLabelWidthOverride is set, use the smallest between it and the computed minLabelWidth.
            if (float.IsFinite(m_MaxLabelWidthOverride))
                minLabelWidth = Mathf.Min(m_MaxLabelWidthOverride, minLabelWidth);

            if (minLabelWidth > 0 && float.IsFinite(minLabelWidth))
            {
                // If valid, store the minLabelWidth as the last valid one.
                m_PreviousValidMinLabelWidth = minLabelWidth;
            }
            else
            {
                // If the current minLabelWidth is not valid, use the previous valid one.
                minLabelWidth = m_PreviousValidMinLabelWidth;
            }
        }

        void SetMinLabelWidth(VisualElement root, float minLabelWidth)
        {
            foreach (var child in root.Children())
            {
                SetMinLabelWidth(child, minLabelWidth);

                var field = child as BaseModelPropertyField;
                var label = field?.LabelElement ?? child as Label;
                var isPropertyField = label?.ClassListContains(BaseModelPropertyField.labelUssClassName) ?? false;

                if (label is not { panel: not null } || !isPropertyField)
                    continue;

                // If the m_MaxLabelWidthOverride is set, use it for the label's max width.
                if (float.IsFinite(m_MaxLabelWidthOverride))
                {
                    label.style.maxWidth = m_MaxLabelWidthOverride;
                }

                var labelPosition = label.parent.ChangeCoordinatesTo(Root, label.localBound.position); //needed for sub ports label that are offset.
                label.style.minWidth = minLabelWidth - labelPosition.x;
            }
        }

        /// <summary>
        /// Gets the field to display.
        /// </summary>
        /// <returns>The fields to display.</returns>
        protected abstract IReadOnlyList<BaseModelPropertyField> GetFields();
    }
}
