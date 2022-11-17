// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ReSharper disable once RedundantUsingDirective : needed by 2020.3

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for the editable title of an <see cref="AbstractNodeModel"/> along with an icon and a progress bar.
    /// </summary>
    class IconTitleProgressPart : EditableTitlePart
    {
        public static new readonly string ussClassName = "ge-icon-title-progress";
        public static readonly string collapseButtonPartName = "collapse-button";

        /// <summary>
        /// Initializes a new instance of the <see cref="IconTitleProgressPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="useEllipsis">Whether to use ellipsis when the text is too large.</param>
        /// <returns>A new instance of <see cref="IconTitleProgressPart"/>.</returns>
        public static IconTitleProgressPart Create(string name, Model model, ModelView ownerElement, string parentClassName, bool useEllipsis)
        {
            if (model is AbstractNodeModel nodeModel)
            {
                return new IconTitleProgressPart(name, nodeModel, ownerElement, parentClassName, useEllipsis);
            }

            return null;
        }

        protected VisualElement m_Root;
        protected VisualElement m_Icon;
        protected VisualElement m_ColorLine;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        public ProgressBar CoroutineProgressBar;

        /// <summary>
        /// Initializes a new instance of the <see cref="IconTitleProgressPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="useEllipsis">Whether to use ellipsis when the text is too large.</param>
        protected IconTitleProgressPart(string name, GraphElementModel model, ModelView ownerElement, string parentClassName, bool useEllipsis)
            : base(name, model, ownerElement, parentClassName, false, useEllipsis ,true)
        {

            if (model.IsCollapsible())
            {
                var collapseButtonPart = NodeCollapseButtonPart.Create(collapseButtonPartName, model, ownerElement, ussClassName);
                PartList.AppendPart(collapseButtonPart);
            }
        }

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (!(m_Model is AbstractNodeModel nodeModel))
                return;

            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_ColorLine = new VisualElement();
            m_ColorLine.AddToClassList(m_ParentClassName.WithUssElement("color-line"));
            m_Root.Add(m_ColorLine);

            TitleContainer = new VisualElement();
            TitleContainer.AddToClassList(ussClassName.WithUssElement("title-container"));
            TitleContainer.AddToClassList(m_ParentClassName.WithUssElement("title-container"));
            m_Root.Add(TitleContainer);

            m_Icon = new VisualElement();
            m_Icon.AddToClassList(ussClassName.WithUssElement("icon"));
            m_Icon.AddToClassList(m_ParentClassName.WithUssElement("icon"));
            if (!string.IsNullOrEmpty(nodeModel.IconTypeString))
            {
                m_Icon.AddToClassList(ussClassName.WithUssElement("icon").WithUssModifier(nodeModel.IconTypeString));
                m_Icon.AddToClassList(m_ParentClassName.WithUssElement("icon").WithUssModifier(nodeModel.IconTypeString));
            }
            TitleContainer.Add(m_Icon);

            if (nodeModel is IPlaceholder || nodeModel is IHasDeclarationModel hasDeclarationModel && hasDeclarationModel.DeclarationModel is IPlaceholder)
            {
                var warningIcon = new Image { name = "missing-graph-icon" };
                warningIcon.AddToClassList(ussClassName.WithUssElement("icon"));
                warningIcon.AddToClassList(ussClassName.WithUssElement("missing-graph-icon"));
                TitleContainer.Add(warningIcon);
            }

            CreateTitleLabel();

            if (nodeModel is IHasProgress hasProgress && hasProgress.HasProgress)
            {
                CoroutineProgressBar = new ProgressBar();
                CoroutineProgressBar.AddToClassList(ussClassName.WithUssElement("progress-bar"));
                CoroutineProgressBar.AddToClassList(m_ParentClassName.WithUssElement("progress-bar"));
                TitleContainer.Add(CoroutineProgressBar);
            }

            TitleContainer.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            container.Add(m_Root);
        }

        /// <inheritdoc />
        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_Root.AddStylesheet_Internal("IconTitleProgressPart.uss");
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();

            var nodeModel = m_Model as AbstractNodeModel;
            if (nodeModel == null)
                return;

            bool hasProgess = nodeModel is IHasProgress hasProgress && hasProgress.HasProgress;
            CoroutineProgressBar?.EnableInClassList("hidden", !hasProgess);

            if (nodeModel.IsColorable())
            {
                if (nodeModel.HasUserColor)
                {
                    m_ColorLine.style.backgroundColor = nodeModel.Color;
                }
                else
                {
                    m_ColorLine.style.backgroundColor = StyleKeyword.Null;
                }
            }
        }
    }
}
