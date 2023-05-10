// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ReSharper disable once RedundantUsingDirective : needed by 2020.3

using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for the editable title of an <see cref="AbstractNodeModel"/> along with an icon and a progress bar.
    /// </summary>
    class NodeTitlePart : EditableTitlePart
    {
        /// <summary>
        /// The uss class name for this element part.
        /// </summary>
        public new static readonly string ussClassName = "ge-node-title-part";

        /// <summary>
        /// The name for the subtitle label.
        /// </summary>
        public static readonly string subtitleName = "subtitle";

        /// <summary>
        /// The modifier name for an empty subtitle.
        /// </summary>
        public static readonly string subTitleEmptyModifier = "empty";

        /// <summary>
        /// The uss class name for the subtitle label.
        /// </summary>
        public static readonly string subtitleUssClassName = ussClassName.WithUssElement(subtitleName);

        /// <summary>
        /// The uss class modifier name for an empty subtitle.
        /// </summary>
        public static readonly string emptySubtitleModifierUssClassName = ussClassName.WithUssModifier(subTitleEmptyModifier);

        public static readonly string collapseButtonPartName = "collapse-button";
        public static readonly string previewButtonPartName = "preview-button";
        public static readonly string missingWarningIconName = "missing-graph-icon";
        public static readonly string noIconModifier = "no-icon";

        public new class Options : EditableTitlePart.Options
        {
            /// <summary>
            /// Whether the ui should show any custom color if any.
            /// </summary>
            public const int Colorable = 1 << optionCount;

            /// <summary>
            /// Whether the ui should show an icon if any.
            /// </summary>
            public const int HasIcon = 1 << (optionCount + 1);

            public new const int Default = EditableTitlePart.Options.Default | Colorable | HasIcon;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="nodeModel">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="options">The options see <see cref="Options"/>.</param>
        /// <returns>A new instance of <see cref="NodeTitlePart"/>.</returns>
        public static NodeTitlePart Create(string name, AbstractNodeModel nodeModel, ModelView ownerElement, string parentClassName, int options = Options.Default)
        {
            return new NodeTitlePart(name, nodeModel, ownerElement, parentClassName, options);
        }

        protected VisualElement m_Root;
        protected VisualElement m_Icon;
        protected VisualElement m_ColorLine;
        protected Label m_SubTitle;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        public ProgressBar CoroutineProgressBar;


        /// <summary>
        /// Initializes a new instance of the <see cref="NodeTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="options">The options see <see cref="Options"/>.</param>
        protected NodeTitlePart(string name, GraphElementModel model, ModelView ownerElement, string parentClassName, int options)
            : base(name, model, ownerElement, parentClassName, options)
        {
            Assert.IsTrue((options & Options.Multiline) == 0);

            if (model is AbstractNodeModel abstractNodeModel && abstractNodeModel.HasNodePreview)
            {
                var togglePreviewButtonPart = NodePreviewButtonPart.Create(previewButtonPartName, model, ownerElement, ussClassName);
                PartList.AppendPart(togglePreviewButtonPart);
            }

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

            if ((m_Options & Options.Colorable) != 0)
            {
                m_ColorLine = new VisualElement();
                m_ColorLine.AddToClassList(m_ParentClassName.WithUssElement("color-line"));
                m_Root.Add(m_ColorLine);
            }

            TitleContainer = new VisualElement();
            TitleContainer.AddToClassList(ussClassName.WithUssElement("title-container"));
            TitleContainer.AddToClassList(m_ParentClassName.WithUssElement("title-container"));
            m_Root.Add(TitleContainer);

            if ((m_Options & Options.HasIcon) != 0)
            {
                m_Icon = new VisualElement();
                m_Icon.AddToClassList(ussClassName.WithUssElement("icon"));
                m_Icon.AddToClassList(m_ParentClassName.WithUssElement("icon"));
                if (!string.IsNullOrEmpty(nodeModel.IconTypeString))
                {
                    m_Icon.AddToClassList(ussClassName.WithUssElement("icon").WithUssModifier(nodeModel.IconTypeString));
                    m_Icon.AddToClassList(m_ParentClassName.WithUssElement("icon").WithUssModifier(nodeModel.IconTypeString));
                }
                else
                {
                    m_Icon.AddToClassList(ussClassName.WithUssElement("icon").WithUssModifier(noIconModifier));
                    m_Icon.AddToClassList(m_ParentClassName.WithUssElement("icon").WithUssModifier(noIconModifier));
                }
                TitleContainer.Add(m_Icon);
            }

            if (nodeModel is IPlaceholder || nodeModel is IHasDeclarationModel hasDeclarationModel && hasDeclarationModel.DeclarationModel is IPlaceholder)
            {
                TitleContainer.Add(CreateMissingWarningIcon());
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
            m_Root.AddStylesheet_Internal("NodeTitlePart.uss");
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

            if (nodeModel.IsColorable() && m_ColorLine != null)
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

            var subTitle =(m_Model as AbstractNodeModel)?.Subtitle;

            m_SubTitle.text = subTitle;
            m_OwnerElement.EnableInClassList(emptySubtitleModifierUssClassName,string.IsNullOrEmpty(subTitle));
        }

        public static Image CreateMissingWarningIcon()
        {
            var warningIcon = new Image { name = missingWarningIconName };
            warningIcon.AddToClassList(ussClassName.WithUssElement("icon"));
            warningIcon.AddToClassList(ussClassName.WithUssElement(missingWarningIconName));
            return warningIcon;
        }

        /// <inheritdoc />
        protected override void CreateTitleLabel()
        {
            base.CreateTitleLabel();
            m_SubTitle = new Label(){name = subtitleName,text = "subtitle"};
            m_SubTitle.AddToClassList(subtitleUssClassName);
            LabelContainer.Add(m_SubTitle);
        }
    }
}
