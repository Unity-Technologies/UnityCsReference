// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An inspector section that can be collapsed and expanded.
    /// </summary>
    [UnityRestricted]
    internal class CollapsibleSection : ModelView
    {
        /// <summary>
        /// The USS class name added to a <see cref="CollapsibleSection"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-collapsible-section";

        /// <summary>
        /// The USS class name added to the header of a <see cref="CollapsibleSection"/>.
        /// </summary>
        public static readonly string headerUssClassName = ussClassName.WithUssElement(GraphElementHelper.headerName);

        /// <summary>
        /// The USS class name added to the content container of a <see cref="CollapsibleSection"/>.
        /// </summary>
        public static readonly string contentContainerUssClassName = ussClassName.WithUssElement(GraphElementHelper.contentContainerName);

        /// <summary>
        /// The USS class name added to the title container of a <see cref="CollapsibleSection"/>.
        /// </summary>
        public static readonly string titleContainerUssClassName = ussClassName.WithUssElement(GraphElementHelper.titleContainerName);

        /// <summary>
        /// The USS class name added to a <see cref="CollapsibleSection"/> when it is collapsed.
        /// </summary>
        public static readonly string expandedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.collapsedUssModifier);

        protected CollapsibleSectionHeader m_TitleContainer;
        protected VisualElement m_ContentContainer;

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer ?? base.contentContainer;

        /// <inheritdoc />
        protected override void BuildUI()
        {
            var header = new VisualElement { name = GraphElementHelper.headerName };
            header.AddToClassList(headerUssClassName);

            m_TitleContainer = new CollapsibleSectionHeader();
            m_TitleContainer.AddToClassList(titleContainerUssClassName);
            header.Add(m_TitleContainer);

            Add(header);

            m_ContentContainer = new VisualElement { name = GraphElementHelper.contentContainerName };
            m_ContentContainer.AddToClassList(contentContainerUssClassName);
            hierarchy.Add(m_ContentContainer);

            m_TitleContainer.RegisterCallback<ChangeEvent<bool>>(OnCollapseChange);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            this.AddPackageStylesheet("CollapsibleSection.uss");
            AddToClassList(ussClassName);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (visitor.ChangeHints.HasChange(ChangeHint.Data))
            {
                var value = (Model as IHasTitle)?.Title ?? string.Empty;
                m_TitleContainer.Text = value;
            }

            if (visitor.ChangeHints.HasChange(ChangeHint.Layout))
            {
                var isCollapsed = (Model as InspectorSectionModel)?.Collapsed ?? false;
                EnableInClassList(expandedUssClassName, isCollapsed);
                m_TitleContainer.SetValueWithoutNotify(isCollapsed);
            }
        }

        /// <summary>
        /// Callback for when the collapse state changes.
        /// </summary>
        /// <param name="e">The change event.</param>
        protected void OnCollapseChange(ChangeEvent<bool> e)
        {
            RootView.Dispatch(new CollapseInspectorSectionCommand(Model as InspectorSectionModel, e.newValue));
        }
    }
}
