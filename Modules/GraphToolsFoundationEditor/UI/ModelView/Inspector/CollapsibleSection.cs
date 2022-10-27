// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// An inspector section that can be collapsed and expanded.
    /// </summary>
    class CollapsibleSection : ModelView
    {
        public static readonly string ussClassName = "ge-collapsible-section";
        public static readonly string headerUssClassName = ussClassName.WithUssElement("header");
        public static readonly string contentContainerUssClassName = ussClassName.WithUssElement("content-container");
        public static readonly string titleContainerUssClassName = ussClassName.WithUssElement("title-container");
        public static readonly string expandedModifierUssClassName = ussClassName.WithUssModifier("collapsed");

        protected CollapsibleSectionHeader m_TitleContainer;
        protected VisualElement m_ContentContainer;

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer ?? base.contentContainer;

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            var header = new VisualElement { name = "header" };
            header.AddToClassList(headerUssClassName);

            m_TitleContainer = new CollapsibleSectionHeader();
            m_TitleContainer.AddToClassList(titleContainerUssClassName);
            header.Add(m_TitleContainer);

            Add(header);

            m_ContentContainer = new VisualElement { name = "content-container" };
            m_ContentContainer.AddToClassList(contentContainerUssClassName);
            hierarchy.Add(m_ContentContainer);

            m_TitleContainer.RegisterCallback<ChangeEvent<bool>>(OnCollapseChange);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            this.AddStylesheet_Internal("CollapsibleSection.uss");
            AddToClassList(ussClassName);
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            var value = (Model as IHasTitle)?.DisplayTitle ?? string.Empty;
            m_TitleContainer.text = value;

            var isCollapsed = (Model as InspectorSectionModel)?.Collapsed ?? false;
            EnableInClassList(expandedModifierUssClassName, isCollapsed);
            m_TitleContainer.SetValueWithoutNotify(isCollapsed);
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
