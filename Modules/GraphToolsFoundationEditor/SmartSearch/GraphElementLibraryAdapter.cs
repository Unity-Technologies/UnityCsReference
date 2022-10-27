// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ItemLibrary.Editor;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// <see cref="ItemLibraryAdapter"/> for <see cref="GraphElementModel"/>.
    /// </summary>
    abstract class GraphElementLibraryAdapter : ItemLibraryAdapter, IGraphElementLibraryAdapter
    {
        float m_InitialSplitterDetailRatio = 1.0f;

        /// <inheritdoc />
        public override float InitialSplitterDetailRatio => m_InitialSplitterDetailRatio;

        protected Label m_DetailsDescriptionTitle;
        protected ScrollView m_DetailsPreviewContainer;

        protected const string k_BaseClassName = "ge-library-details";
        protected static readonly string k_HidePreviewClassName = k_BaseClassName.WithUssModifier("hidden");
        static readonly string k_DetailsDescriptionTitleClassName = DetailsTitleClassName.WithUssModifier("description");
        const string k_DetailsNodeClassName = "ge-library-details-preview-container";
        const string k_DescriptionTitle = "Description";

        protected GraphElementLibraryAdapter(string title, string toolName = null) : base(title, toolName) {}

        /// <inheritdoc />
        public override void InitDetailsPanel(VisualElement detailsPanel)
        {
            base.InitDetailsPanel(detailsPanel);

            m_DetailsPreviewContainer = MakeDetailsPreviewContainer();
            detailsPanel.Insert(1, m_DetailsPreviewContainer);
            m_DetailsDescriptionTitle = MakeDetailsTitleLabel(k_DescriptionTitle);
            m_DetailsDescriptionTitle.AddToClassList(k_DetailsDescriptionTitleClassName);
            detailsPanel.Insert(2, m_DetailsDescriptionTitle);

            detailsPanel.AddStylesheet_Internal("LibraryAdapter.uss");
        }

        /// <inheritdoc />
        public virtual void SetHostGraphView(GraphView graphView)
        {
            var size = graphView.GraphTool.Preferences.GetItemLibrarySize(LibraryName);
            m_InitialSplitterDetailRatio = size.RightLeftRatio;
        }

        /// <summary>
        /// Creates a container for the preview in Details section.
        /// </summary>
        /// <returns>A container with uss class for a preview container in the details panel.</returns>
        protected virtual ScrollView MakeDetailsPreviewContainer()
        {
            var previewContainer = new ScrollView();
            previewContainer.StretchToParentSize();
            previewContainer.AddToClassList(k_DetailsNodeClassName);
            previewContainer.style.position = Position.Relative;

            return previewContainer;
        }

        /// <inheritdoc />
        public override void UpdateDetailsPanel(ItemLibraryItem item)
        {
            base.UpdateDetailsPanel(item);

            var showPreview = ItemHasPreview(item);
            m_DetailsPreviewContainer.EnableInClassList(k_HidePreviewClassName, !showPreview);
            var hasDescriptionTitle = showPreview && !string.IsNullOrEmpty(DetailsTextLabel.text);
            m_DetailsDescriptionTitle.EnableInClassList(k_HidePreviewClassName, !hasDescriptionTitle);
        }

        public virtual bool ItemHasPreview(ItemLibraryItem item)
        {
            return false;
        }
    }
}
