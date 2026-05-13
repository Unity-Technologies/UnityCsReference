// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ReSharper disable once RedundantUsingDirective : needed by 2020.3

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to build the UI for the editable title of an <see cref="AbstractNodeModel"/> along with an icon and a progress bar.
    /// </summary>
    [UnityRestricted]
    internal class NodeTitlePart : EditableTitlePart
    {
        /// <summary>
        /// The USS class name added to this element part.
        /// </summary>
        public new static readonly string ussClassName = "ge-node-title-part";

        /// <summary>
        /// The name for the subtitle label.
        /// </summary>
        public static readonly string subtitleName = "subtitle";

        /// <summary>
        /// The USS class name added to the subtitle label.
        /// </summary>
        public static readonly string subtitleUssClassName = ussClassName.WithUssElement(subtitleName);

        /// <summary>
        /// The USS class name added to the <see cref="NodeToolbarButton"/> container.
        /// </summary>
        public static readonly string buttonContainerUssClassName = ussClassName.WithUssElement("button-container");

        /// <summary>
        /// The USS class name for an empty subtitle.
        /// </summary>
        public static readonly string emptyUssClassName = ussClassName.WithUssModifier(GraphElementHelper.emptyUssModifier);

        /// <summary>
        /// The name of the icon to show when the graph is missing.
        /// </summary>
        public static readonly string missingWarningIconName = "missing-graph-icon";

        /// <summary>
        /// The USS modifier for an element without icon.
        /// </summary>
        public static readonly string noIconModifier = "no-icon";

        /// <summary>
        /// The USS class name used when the node of this node title part is hovered on (and its title is non-empty).
        /// </summary>
        static readonly string k_NodeTitlePartHoverStateUssClassName = ussClassName.WithUssModifier("hovered");

        /// <summary>
        /// The USS class name used when the node of this node title part is being hovered on (and its title is empty).
        /// </summary>
        static readonly string k_EmptyTitleHoverStateUssClassName = ussClassName.WithUssModifier("empty-title-hovered");

        /// <summary>
        /// The USS class name used when textfield of this node title part is focused on
        /// (this only happens if its TitleLabel is an EditableLabel).
        /// </summary>
        static readonly string k_TitleInFocusUssClassName = ussClassName.WithUssModifier("focused");

        /// <summary>
        /// Options for configuring the title of a node.
        /// </summary>
        /// <remarks>
        /// The 'Options' class defines configuration options specifically for customizing the title of a node, extending from <see cref="EditableTitlePart.Options"/>.
        /// These options allow further control over the visual presentation and behavior of node titles, including the use of custom colors and icons. The additional options are:
        /// <see cref="Options.ShouldDisplayColor"/>, <see cref="Options.HasIcon"/>, and <see cref="Options.Default"/>.
        /// </remarks>
        [UnityRestricted]
        internal new class Options : EditableTitlePart.Options
        {
            /// <summary>
            /// Whether the title must display a custom color, if one is set.
            /// </summary>
            public const int ShouldDisplayColor = 1 << k_OptionCount;

            /// <summary>
            /// Whether the title must display an icon.
            /// </summary>
            public const int HasIcon = 1 << (k_OptionCount + 1);

            /// <summary>
            /// The default configuration of a node title.
            /// </summary>
            public new const int Default = EditableTitlePart.Options.Default | ShouldDisplayColor | HasIcon;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="nodeModel">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="options">The options see <see cref="Options"/>.</param>
        /// <param name="defaultMinWidth">The default min width of the title label container</param>
        /// <returns>A new instance of <see cref="NodeTitlePart"/>.</returns>
        public static NodeTitlePart Create(string name, GraphElementModel nodeModel, ChildView ownerElement, string parentClassName, int options = Options.Default, float defaultMinWidth = 0)
        {
            return new NodeTitlePart(name, nodeModel, ownerElement, parentClassName, options, defaultMinWidth);
        }

        protected VisualElement m_Root;
        protected Image m_Icon;
        protected VisualElement m_ColorLine;
        protected VisualElement m_NodeToolbarButtonsContainer;
        protected Label m_SubTitle;

        /// <summary>
        /// The visual element for the mode dropdown arrow
        /// </summary>
        protected DropdownField m_ModeDropdownButton;

        internal const float k_ButtonWidth = 20;
        internal const int k_MinTextFieldCharLength = 8; // The max number of characters in the textfield before the textfield takes on a size bigger than a min width of 80px

        bool m_Attached;
        bool m_IsTitleAtFullWidth; // False when the title is squashed to make space for hover buttons, true otherwise.
        bool m_NeedsTitleTextfieldResize; // True when the textfield needs to be squashed, but has to wait until a resize of the node (geometry change) has happened.
        bool m_NeedsTitleTextWidthRecalculation; // True when the cached estimated title text width needs to be recalculated.

        float m_EstimatedTitleTextWidth; // Cached value of the minimum pixel width required for displaying the title without truncating it.

        /// <summary>
        /// The icon on the node.
        /// </summary>
        public Image Icon => m_Icon;

        /// <summary>
        /// The subtitle on the node.
        /// </summary>
        public Label SubTitle => m_SubTitle;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        public ProgressBar CoroutineProgressBar { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="options">The options see <see cref="Options"/>.</param>
        /// <param name="defaultMinWidth">The default min width of the title label container</param>
        protected NodeTitlePart(string name, GraphElementModel model, ChildView ownerElement, string parentClassName, int options, float defaultMinWidth = 0)
            : base(name, model, ownerElement, parentClassName, options, defaultMinWidth)
        {
            Assert.IsTrue((options & Unity.GraphToolkit.Editor.EditableTitlePart.Options.Multiline) == 0);
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            if (!m_Attached)
            {
                m_IsTitleAtFullWidth = true;
                (m_OwnerElement.RootView as GraphView)?.RegisterElementZoomLevelClass(m_Root, GraphViewZoomMode.Medium, ussClassName.WithUssModifier(GraphElementHelper.mediumUssModifier));
                m_Attached = true;
            }
        }

        void OnDetachFromPanel(DetachFromPanelEvent e)
        {
            if (m_Attached)
            {
                (m_OwnerElement.RootView as GraphView)?.UnregisterElementZoomLevelClass(m_Root, GraphViewZoomMode.Medium);
                m_Attached = false;
            }
        }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            if (m_Model is not AbstractNodeModel nodeModel)
                return;

            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_Root.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_Root.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            TitleContainer = new VisualElement();
            TitleContainer.AddToClassList(ussClassName.WithUssElement(GraphElementHelper.titleContainerName));
            TitleContainer.AddToClassList(m_ParentClassName.WithUssElement(GraphElementHelper.titleContainerName));
            m_Root.Add(TitleContainer);

            if ((m_Options & Options.HasIcon) != 0)
            {
                m_Icon = new Image();
                m_Icon.AddToClassList(ussClassName.WithUssElement(GraphElementHelper.iconName));
                m_Icon.AddToClassList(m_ParentClassName.WithUssElement(GraphElementHelper.iconName));
                TitleContainer.Add(m_Icon);
            }

            if (IsNodeDefinitionValid())
            {
                TitleContainer.Add(CreateMissingWarningIcon());
            }

            CreateTitleLabel();

            if (nodeModel is IHasProgress)
            {
                CoroutineProgressBar = new ProgressBar();
                CoroutineProgressBar.AddToClassList(ussClassName.WithUssElement("progress-bar"));
                CoroutineProgressBar.AddToClassList(m_ParentClassName.WithUssElement("progress-bar"));
                TitleContainer.Add(CoroutineProgressBar);
            }

            m_NodeToolbarButtonsContainer = new VisualElement();
            m_NodeToolbarButtonsContainer.AddToClassList(buttonContainerUssClassName);
            TitleContainer.Add(m_NodeToolbarButtonsContainer);

            TitleContainer.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            // Exclude the EditableLabel from tab navigation as users should navigate from property fields to property fields.
            TitleLabel.tabIndex = -1;
            container.Add(m_Root);
        }

        /// <summary>
        /// Adds a <see cref="NodeToolbarButton"/> to the node title part.
        /// </summary>
        /// <param name="button">The button to add.</param>
        public void AddNodeToolbarButton(NodeToolbarButton button)
        {
            m_NodeToolbarButtonsContainer.Add(button);

            // The following code is used for handling the display of buttons on the node title part and should not run
            // if the node title part has no buttons. It's an initialization for the node title part (and not a per button init)
            // so it should only run once.
            if (m_NodeToolbarButtonsContainer.childCount != 1)
                return;

            TitleLabel.RegisterCallback<GeometryChangedEvent>(OnTitleLabelGeometryChanged);

            if (TitleLabel is EditableLabel editableLabel)
            {
                var textField = editableLabel.Q<TextField>();

                if (textField != null)
                {
                    textField.RegisterCallback<FocusInEvent>(OnFocusInTitleTextField);
                    textField.RegisterCallback<FocusOutEvent>(OnFocusOutTitleTextField);
                }
            }

            m_NeedsTitleTextWidthRecalculation = true;
        }

        /// <summary>
        /// Whether the node is valid.
        /// </summary>
        /// <returns><c>true</c> if the node is valid.</returns>
        /// <remarks>Use this method to validate node definitions before performing operations or rendering them in the UI. It ensures that problematic nodes are displayed with visual feedback (for example, an error icon).
        /// The visual indication provided by the error icon makes it easy to identify nodes that require attention.
        /// </remarks>
        protected virtual bool IsNodeDefinitionValid()
        {
            return m_Model is AbstractNodeModel nodeModel &&
                (nodeModel is IPlaceholder || nodeModel is IHasDeclarationModel { DeclarationModel: IPlaceholder });
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            m_Root.AddPackageStylesheet("NodeTitlePart.uss");
        }

        List<string> m_PreviousIconClasses = new List<string>();
        string m_PreviousIconString = null;
        string m_PreviousIconPath;

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            UpdateTitleSizeFromModel(visitor);

            base.UpdateUIFromModel(visitor);

            var nodeModel = m_Model as AbstractNodeModel;
            if (nodeModel == null)
                return;

            string iconTypeString = nodeModel.IconTypeString;
            string iconPath = nodeModel.IconPath;

            if (m_Icon != null && (m_PreviousIconClasses.Count == 0 || m_PreviousIconString != iconTypeString || m_PreviousIconPath != iconPath))
            {
                foreach (var iconClass in m_PreviousIconClasses)
                {
                    m_Icon.RemoveFromClassList(iconClass);
                }
                m_PreviousIconClasses.Clear();

                // If an icon path is specified, it takes precedence over the icon type string.
                if (!string.IsNullOrEmpty(iconPath))
                {
                    var iconTexture = EditorGUIUtility.IconContent(nodeModel.IconPath).image as Texture2D;
                    if (iconTexture == null)
                    {
                        Debug.LogWarning($"Could not load icon at path '{nodeModel.IconPath}' for node {nodeModel.Title}");
                        m_Icon.image = null;
                    }
                    else
                    {
                        m_Icon.image = iconTexture;
                    }
                }
                else if (!string.IsNullOrEmpty(iconTypeString))
                {
                    m_PreviousIconClasses.Add(ussClassName.WithUssElement(GraphElementHelper.iconName).WithUssModifier(iconTypeString));
                    m_PreviousIconClasses.Add(m_ParentClassName.WithUssElement(GraphElementHelper.iconName).WithUssModifier(iconTypeString));
                    m_PreviousIconClasses.Add(GraphElementHelper.iconUssClassName.WithUssModifier(iconTypeString));
                }

                if ((m_Options & Options.HasIcon) == 0)
                {
                    m_PreviousIconClasses.Add(ussClassName.WithUssElement(GraphElementHelper.iconName).WithUssModifier(noIconModifier));
                    m_PreviousIconClasses.Add(m_ParentClassName.WithUssElement(GraphElementHelper.iconName).WithUssModifier(noIconModifier));
                }

                foreach (var iconClass in m_PreviousIconClasses)
                {
                    m_Icon.AddToClassList(iconClass);
                }

                m_PreviousIconString = iconTypeString;
                m_PreviousIconPath = iconPath;
            }

            var hasProgressNode = nodeModel as IHasProgress;
            var showProgress = hasProgressNode is { Progress: >= 0 };
            CoroutineProgressBar?.EnableInClassList(GraphElementHelper.hiddenUssModifier, !showProgress);
            if (CoroutineProgressBar != null && showProgress)
            {
                CoroutineProgressBar.value = hasProgressNode.Progress;
            }

            if (visitor.ChangeHints.HasChange(ChangeHint.Data))
            {
                var subTitle = (m_Model as AbstractNodeModel)?.Subtitle;

                m_SubTitle.text = subTitle;
                m_Root.EnableInClassList(emptyUssClassName, string.IsNullOrEmpty(subTitle));
            }

            if (visitor.ChangeHints.HasChange(ChangeHint.Style))
            {
                m_Root.tooltip = (m_Model as AbstractNodeModel)?.Tooltip ?? string.Empty;
            }

            if (m_Model is not NodeModel nodeModeModel || nodeModeModel.Modes.Count <= 0)
                return;

            var currentMode = nodeModeModel.Modes[nodeModeModel.CurrentModeIndex];
            if (currentMode != null && m_ModeDropdownButton.value != currentMode)
                m_ModeDropdownButton.SetValueWithoutNotify(currentMode);
        }

        void UpdateTitleSizeFromModel(UpdateFromModelVisitor visitor)
        {
            if (m_NodeToolbarButtonsContainer.childCount == 0)
                return;

            if (!visitor.ChangeHints.HasChange(ChangeHint.Data))
                return;

            var title = (m_Model as IHasTitle)?.Title ?? string.Empty;
            if (title == CurrentTitle)
                return;

            var currentTitleLength = CurrentTitle?.Length ?? 0;
            if (title.Length == 0)
            {
                // If the title is being hovered on, switch to hover styling adapted to empty titles.
                // The main difference is that now only the subtitle is being displayed, and the hover buttons are
                // hence aligned with the subtitle instead of the title.
                if (!m_Root.ClassListContains(k_NodeTitlePartHoverStateUssClassName))
                    return;

                m_Root.RemoveFromClassList(k_NodeTitlePartHoverStateUssClassName);
                m_Root.AddToClassList(k_EmptyTitleHoverStateUssClassName);

                if (title.Length <= k_MinTextFieldCharLength && currentTitleLength <= title.Length)
                {
                    // Squash the subtitle in order to make space for the buttons that appear on hover
                    SquashSubtitle(false);
                }
                else
                {
                    // This scenario happens when the title of the node just got erased, except the length of
                    // that title was longer than the min size of a node. This means that the node will likely need to be downsized.
                    // The subtitle needs to be squashed in order to make room for the hover buttons. However, it cannot be done yet
                    // because the resolved width of the subtitle has not been computed yet (the model gets updated before the size of the node is).
                    // The squashing of the subtitle needs to wait until a change in geometry has been triggered.
                    m_NeedsTitleTextfieldResize = true;
                }
            }
            else
            {
                // If the title used to be empty and was being hovered on, switch to hover styling adapted to non-empty titles.
                // This means that now both the title and the subtitle are being displayed, and the hover buttons are aligned with the title.
                if (currentTitleLength == 0 && m_Root.ClassListContains(k_EmptyTitleHoverStateUssClassName))
                {
                    m_Root.RemoveFromClassList(k_EmptyTitleHoverStateUssClassName);
                    m_Root.AddToClassList(k_NodeTitlePartHoverStateUssClassName);

                    StretchSubtitle();
                }

                if (!m_Root.ClassListContains(k_NodeTitlePartHoverStateUssClassName))
                    return;

                if (title.Length == currentTitleLength || (title.Length <= k_MinTextFieldCharLength && currentTitleLength <= k_MinTextFieldCharLength))
                {
                    // This scenario happens when no resize of the node is required. This is either because
                    // 1. The title length has not changed;
                    // 2. The node is already at its min width size and does not need to be resized to a greater width.

                    // Squash the title in order to make space for the buttons that appear on hover.
                    SquashTitleLabel(true);
                }
                else
                {
                    // This scenario happens when the title got renamed to a new title that requires a resize of the node.
                    // The title needs to be squashed in order to make room for the hover buttons. However, it cannot be done yet
                    // because the resolved width of the title has not been computed yet (the model gets updated before the size of the node is).
                    // The squashing of the title needs to wait until a change in geometry has been triggered.
                    m_NeedsTitleTextfieldResize = true;
                }
            }
        }

        public static Image CreateMissingWarningIcon()
        {
            var warningIcon = new Image { name = missingWarningIconName };
            warningIcon.AddToClassList(ussClassName.WithUssElement(GraphElementHelper.iconName));
            warningIcon.AddToClassList(ussClassName.WithUssElement(missingWarningIconName));
            return warningIcon;
        }

        public void SetHoverState(bool isHovered)
        {
            // Hover buttons are hidden when the title textfield is being focused on so the title/subtitle
            // only needs to be squashed/stretched if the node is not being hovered on.
            if (m_NodeToolbarButtonsContainer.childCount == 0)
                return;

            if (isHovered)
            {
                var currentTitleLength = CurrentTitle?.Length ?? 0;
                m_Root.AddToClassList(currentTitleLength > 0 ? k_NodeTitlePartHoverStateUssClassName : k_EmptyTitleHoverStateUssClassName);

                if (!m_Root.ClassListContains(k_TitleInFocusUssClassName))
                {
                    if (currentTitleLength > 0)
                        SquashTitleLabel(true);
                    else
                        SquashSubtitle(true);
                }

                m_NodeToolbarButtonsContainer.style.marginLeft = m_NodeToolbarButtonsContainer.childCount * k_ButtonWidth * -1;
            }
            else
            {
                if (m_Root.ClassListContains(k_NodeTitlePartHoverStateUssClassName))
                {
                    m_Root.RemoveFromClassList(k_NodeTitlePartHoverStateUssClassName);

                    if (!m_Root.ClassListContains(k_TitleInFocusUssClassName))
                        StretchTitleLabel();
                }
                else if (m_Root.ClassListContains(k_EmptyTitleHoverStateUssClassName))
                {
                    m_Root.RemoveFromClassList(k_EmptyTitleHoverStateUssClassName);

                    if (!m_Root.ClassListContains(k_TitleInFocusUssClassName))
                        StretchSubtitle();
                }

                m_NodeToolbarButtonsContainer.style.marginLeft = StyleKeyword.Auto;
            }
        }

        public void UpdateUIOnExpand(float oldNodeWidth, float newNodeWidth)
        {
            var buttonContainerWidth = m_NodeToolbarButtonsContainer.childCount * k_ButtonWidth;

            if (newNodeWidth - oldNodeWidth <= buttonContainerWidth)
                return;

            // If the node's width increases when expanded (for example because one of its ports has a title that is much longer than the node's title),
            // then the node title part's width needs to be updated to scale accordingly. The increase needs to be bigger than the width taken by the buttons.
            // This guarantees that the buttons won't overlap with the stretched title.
            if (m_Root.ClassListContains(k_NodeTitlePartHoverStateUssClassName))
            {
                StretchTitleLabel();
            }
            else if (m_Root.ClassListContains(k_EmptyTitleHoverStateUssClassName))
            {
                StretchSubtitle();
            }
        }

        public void UpdateUIOnCollapse()
        {
            // The node might reduce in width after a collapse. This happens for example if the node has an option with a name that is much longer than the node's title.
            // This means that the squashed size of the title needs to be recalculated to accomodate a possible change in width of the node.
            // (Reminder: squashing is used when the node is being hovered.)
            if (m_Root.ClassListContains(k_NodeTitlePartHoverStateUssClassName))
            {
                SquashTitleLabel(false);
            }
            else if (m_Root.ClassListContains(k_EmptyTitleHoverStateUssClassName))
            {
                SquashSubtitle(false);
            }
        }

        /// <inheritdoc />
        protected override void CreateTitleLabel()
        {
            base.CreateTitleLabel();

            AddModeDropdown();

            m_SubTitle = new Label { name = subtitleName, text = "subtitle" };
            m_SubTitle.AddToClassList(subtitleUssClassName);

            if (m_Model is GraphElementModel model && model.IsRenamable())
                m_SubTitle.AddToClassList(subtitleUssClassName.WithUssModifier(renamableUSSModifier));

            if (LabelContainer != null)
                LabelContainer.Add(m_SubTitle);
            else
                TitleContainer.Add(m_SubTitle);
        }

        protected override void OnRename(ChangeEvent<string> e)
        {
            base.OnRename(e);

            if (m_NodeToolbarButtonsContainer.childCount > 0)
            {
                StretchTitleLabel();

                // Since the title text has changed, the cached value that estimates the pixel width required for displaying said text needs to be recalculated.
                m_NeedsTitleTextWidthRecalculation = true;
            }
        }

        void OnFocusInTitleTextField(FocusInEvent evt)
        {
            if (m_Root.ClassListContains(k_TitleInFocusUssClassName))
                return;

            m_Root.AddToClassList(k_TitleInFocusUssClassName);
            StretchTitleLabel();
        }

        void OnFocusOutTitleTextField(FocusOutEvent evt)
        {
            if (!m_Root.ClassListContains(k_TitleInFocusUssClassName))
                return;

            m_Root.RemoveFromClassList(k_TitleInFocusUssClassName);

            if (m_Root.ClassListContains(k_NodeTitlePartHoverStateUssClassName))
            {
                SquashTitleLabel(true);
            }
            else if (m_Root.ClassListContains(k_EmptyTitleHoverStateUssClassName))
            {
                SquashSubtitle(true);
            }
        }

        void OnTitleLabelGeometryChanged(GeometryChangedEvent evt)
        {
            if (!m_NeedsTitleTextfieldResize || m_Root.ClassListContains(k_TitleInFocusUssClassName))
                return;

            if (m_Root.ClassListContains(k_NodeTitlePartHoverStateUssClassName))
            {
                SquashTitleLabel(true);
                m_NeedsTitleTextfieldResize = false;
            }
            else if (m_Root.ClassListContains(k_EmptyTitleHoverStateUssClassName))
            {
                SquashSubtitle(true);
                m_NeedsTitleTextfieldResize = false;
            }
        }

        void StretchTitleLabel()
        {
            TitleLabel.style.width = StyleKeyword.Auto;
            m_IsTitleAtFullWidth = true;
        }

        void StretchSubtitle()
        {
            if (m_SubTitle == null)
                return;

            m_SubTitle.style.width = StyleKeyword.Auto;
            m_IsTitleAtFullWidth = true;
        }

        /// <summary>
        /// Have the title make space for the hover buttons of this node.
        /// <param name="relativeToCurrentWidth">
        /// Indicates if the squashed size is calculated based on the title's current resolved width (true) or based on the estimated width required to display the title's text (false).
        /// </param>
        /// <remarks>
        /// In most cases, the resolved width should be used because a node's width could be much larger than the width of its title text (for example, if it has a long node option name).
        /// So squashing should be calculated relative to the node's width (the title's width is expected to be set at auto, meaning the resolved width is one that has been scaled to the
        /// node's width.) However, there are cases where the resolved width is unreliable. If the node becomes smaller in width (for example because the node got collapsed),
        /// then the title needs to be squashed however the current resolved width still matches the bigger, expanded width of the node. In a case like this, squashing should
        /// be calculated based on the width of the title's text itself rather than the node's width.
        /// </remarks>
        /// </summary>
        void SquashTitleLabel(bool relativeToCurrentWidth)
        {
            if (!m_IsTitleAtFullWidth)
                return;

            var width = (relativeToCurrentWidth ? TitleLabel.resolvedStyle.width : GetEstimatedTitleTextWidth()) - m_NodeToolbarButtonsContainer.childCount * k_ButtonWidth;
            width = Mathf.Max(0.0f, width);

            TitleLabel.style.width = new StyleLength(width);

            m_IsTitleAtFullWidth = false;
        }

        /// <summary>
        /// Have the subtitle make space for the hover buttons of this node.
        /// <param name="relativeToCurrentWidth">
        /// Indicates if the squashed size is calculated based on the subtitle's current resolved width (true) or based on the estimated width required to display the subtitle's text (false).
        /// </param>
        /// <seealso cref="SquashTitleLabel"/>
        /// </summary>
        void SquashSubtitle(bool relativeToCurrentWidth)
        {
            if (!m_IsTitleAtFullWidth || m_SubTitle == null)
                return;

            var width = (relativeToCurrentWidth ? m_SubTitle.resolvedStyle.width : GetEstimatedSubtitleTextWidth()) - m_NodeToolbarButtonsContainer.childCount * k_ButtonWidth;
            width = Mathf.Max(0.0f, width);

            m_SubTitle.style.width = new StyleLength(new Length(width, LengthUnit.Pixel));

            m_IsTitleAtFullWidth = false;
        }

        float GetEstimatedTitleTextWidth()
        {
            if (!m_NeedsTitleTextWidthRecalculation)
                return m_EstimatedTitleTextWidth;

            if (TitleLabel is TextElement textElement)
            {
                // Caching the value since MeasureToTextSize() is an expensive operation
                m_EstimatedTitleTextWidth = GetEstimatedTextWidth(textElement);
            }
            else if (TitleLabel is EditableLabel editableLabel)
            {
                var label = editableLabel.Q<Label>(name: EditableLabel.labelName);

                // Caching the value since MeasureToTextSize() is an expensive operation
                m_EstimatedTitleTextWidth = GetEstimatedTextWidth(label);
            }

            m_NeedsTitleTextWidthRecalculation = false;

            return m_EstimatedTitleTextWidth;
        }

        float GetEstimatedSubtitleTextWidth()
        {
            if (!m_NeedsTitleTextWidthRecalculation)
                return m_EstimatedTitleTextWidth;

            m_NeedsTitleTextWidthRecalculation = false;

            // Caching the value since MeasureToTextSize() is an expensive operation
            m_EstimatedTitleTextWidth = GetEstimatedTextWidth(m_SubTitle);

            return m_EstimatedTitleTextWidth;
        }

        static float GetEstimatedTextWidth(TextElement element)
        {
            return element == null ?
                throw new ArgumentNullException(nameof(element)) :
                element.MeasureTextSize(element.text, 0, VisualElement.MeasureMode.Undefined, element.resolvedStyle.height, VisualElement.MeasureMode.Exactly).x;
        }

        void AddModeDropdown()
        {
            if (m_Model is NodeModel nodeModel && nodeModel.Modes.Count > 0)
            {
                m_ModeDropdownButton = new NodeModeDropdown(ussClassName)
                {
                    choices = nodeModel.Modes,
                    index = nodeModel.CurrentModeIndex
                };

                m_ModeDropdownButton.RegisterValueChangedCallback(e =>
                {
                    if (e.previousValue != e.newValue)
                    {
                        var newIndex = IEnumerableExtensions.IndexOf(nodeModel.Modes, e.newValue);
                        m_OwnerElement.RootView.Dispatch(new ChangeNodeModeCommand(nodeModel, newIndex));
                    }
                });

                if ((m_Options & Options.UseEllipsis) != 0)
                {
                    LabelContainer.Remove(TitleLabel);

                    var titlegroup = new VisualElement();
                    titlegroup.Add(TitleLabel);
                    titlegroup.AddToClassList(ussClassName.WithUssElement("title-group"));
                    titlegroup.Add(m_ModeDropdownButton);
                    LabelContainer.Add(titlegroup);

                    TitleContainer.Add(LabelContainer);
                }
                else
                {
                    TitleContainer.Remove(TitleLabel);
                    var titlegroup = new VisualElement();
                    titlegroup.Add(TitleLabel);

                    if (m_ModeDropdownButton is not null)
                        titlegroup.Add(m_ModeDropdownButton);
                    TitleContainer.Add(titlegroup);
                }
            }
        }

        public class TestAccess
        {
            public readonly NodeTitlePart nodeTitlePart;

            public TestAccess(NodeTitlePart nodeTitlePart)
            {
                this.nodeTitlePart = nodeTitlePart;
            }

            public void BuildUI(VisualElement container) => nodeTitlePart.BuildUI(container);
        }
    }
}
