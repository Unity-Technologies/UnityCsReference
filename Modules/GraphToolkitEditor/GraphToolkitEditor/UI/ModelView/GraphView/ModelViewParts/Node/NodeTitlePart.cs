// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ReSharper disable once RedundantUsingDirective : needed by 2020.3

using System;
using System.Collections.Generic;
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
        /// The name of the <see cref="VisualElement"/> of the colored line.
        /// </summary>
        public static readonly string colorLineName = "color-line";

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
        protected VisualElement m_Icon;
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

        /// <summary>
        /// The icon on the node.
        /// </summary>
        public VisualElement Icon => m_Icon;

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

            if ((m_Options & Options.ShouldDisplayColor) != 0)
            {
                m_ColorLine = new VisualElement { name = colorLineName };
                m_ColorLine.AddToClassList(m_ParentClassName.WithUssElement(colorLineName));
                m_Root.Add(m_ColorLine);
            }

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

            if (TitleLabel is EditableLabel editableLabel)
            {
                var textField = editableLabel.Q<TextField>();

                if (textField != null)
                {
                    textField.RegisterCallback<FocusInEvent>(OnFocusInTitleTextField);
                    textField.RegisterCallback<FocusOutEvent>(OnFocusOutTitleTextField);
                }
            }
        }

        List<string> m_PreviousIconClasses = new List<string>();
        string m_PreviousIconString = null;

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            UpdateTitleSizeFromModel(visitor);

            base.UpdateUIFromModel(visitor);

            var nodeModel = m_Model as AbstractNodeModel;
            if (nodeModel == null)
                return;

            string iconTypeString = nodeModel.IconTypeString;
            if (m_Icon != null && (m_PreviousIconClasses.Count == 0 || m_PreviousIconString != iconTypeString))
            {
                foreach (var iconClass in m_PreviousIconClasses)
                {
                    m_Icon.RemoveFromClassList(iconClass);
                }
                m_PreviousIconClasses.Clear();
                if (!string.IsNullOrEmpty(nodeModel.IconTypeString))
                {
                    m_PreviousIconClasses.Add(ussClassName.WithUssElement(GraphElementHelper.iconName).WithUssModifier(iconTypeString));
                    m_PreviousIconClasses.Add(m_ParentClassName.WithUssElement(GraphElementHelper.iconName).WithUssModifier(iconTypeString));
                    m_PreviousIconClasses.Add(GraphElementHelper.iconUssClassName.WithUssModifier(iconTypeString));
                }
                else if ((m_Options & Options.HasIcon) == 0)
                {
                    m_PreviousIconClasses.Add(ussClassName.WithUssElement(GraphElementHelper.iconName).WithUssModifier(noIconModifier));
                    m_PreviousIconClasses.Add(m_ParentClassName.WithUssElement(GraphElementHelper.iconName).WithUssModifier(noIconModifier));
                }
                foreach (var iconClass in m_PreviousIconClasses)
                {
                    m_Icon.AddToClassList(iconClass);
                }

                m_PreviousIconString = iconTypeString;
            }

            var hasProgressNode = nodeModel as IHasProgress;
            var showProgress = hasProgressNode is { Progress: >= 0 };
            CoroutineProgressBar?.EnableInClassList(GraphElementHelper.hiddenUssModifier, !showProgress);
            if (CoroutineProgressBar != null && showProgress)
            {
                CoroutineProgressBar.value = hasProgressNode.Progress;
            }

            UpdateLineColorFromModel(visitor, nodeModel);

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
            if (visitor.ChangeHints.HasChange(ChangeHint.Data))
            {
                var title = (m_Model as IHasTitle)?.Title ?? string.Empty;
                if (title == CurrentTitle)
                    return;

                var currentTitleLength = CurrentTitle?.Length ?? 0;
                if (title.Length == 0)
                {
                    // If the title is being hovered on, switch to hover styling adapted to empty titles.
                    // The main difference is that now only the subtitle is being displayed, and the hover buttons are
                    // hence aligned with the subtitle instead of the title.
                    if (m_Root.ClassListContains(k_NodeTitlePartHoverStateUssClassName))
                    {
                        m_Root.RemoveFromClassList(k_NodeTitlePartHoverStateUssClassName);
                        m_Root.AddToClassList(k_EmptyTitleHoverStateUssClassName);

                        if (title.Length <= k_MinTextFieldCharLength && currentTitleLength <= title.Length)
                        {
                            // Squash the subtitle in order to make space for the buttons that appear on hover
                            SquashSubtitle();
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
                        SquashTitleLabel();
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
        }

        /// <summary>
        /// When handling a style change for a node, this method updates the color line based on the model's color.
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="nodeModel"></param>
        /// <returns>Returns true if the ColorLine's background color is set</returns>
        private protected virtual bool UpdateLineColorFromModel(UpdateFromModelVisitor visitor, AbstractNodeModel nodeModel)
        {
            if (m_ColorLine == null)
                return false;

            if (visitor.ChangeHints.HasChange(ChangeHint.Style))
            {
                if (nodeModel.ElementColor.HasUserColor)
                {
                    m_ColorLine.style.backgroundColor = nodeModel.ElementColor.Color;
                    return true;
                }

                m_ColorLine.style.backgroundColor = nodeModel.DefaultColor;
            }

            return false;
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
            // Note that hover buttons are hidden when the title textfield is being focused on so the title/subtitle
            // only needs to be squashed/stretched if the node is not being hovered on.

            if (isHovered)
            {
                var currentTitleLength = CurrentTitle?.Length ?? 0;
                m_Root.AddToClassList(currentTitleLength > 0 ? k_NodeTitlePartHoverStateUssClassName : k_EmptyTitleHoverStateUssClassName);

                if (!m_Root.ClassListContains(k_TitleInFocusUssClassName))
                {
                    if (currentTitleLength > 0)
                        SquashTitleLabel();
                    else
                        SquashSubtitle();
                }
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
            }
        }

        public void UpdateUIOnCollapseStateChange()
        {
            if (m_Root.ClassListContains(k_NodeTitlePartHoverStateUssClassName))
            {
                StretchTitleLabel();
                m_NeedsTitleTextfieldResize = true;
            }
            else if (m_Root.ClassListContains(k_EmptyTitleHoverStateUssClassName))
            {
                StretchSubtitle();
                m_NeedsTitleTextfieldResize = true;
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

            TitleLabel.RegisterCallback<GeometryChangedEvent>(OnTitleLabelGeometryChanged);
        }

        protected override void OnRename(ChangeEvent<string> e)
        {
            base.OnRename(e);

            StretchTitleLabel();
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
                SquashTitleLabel();
            }
            else if (m_Root.ClassListContains(k_EmptyTitleHoverStateUssClassName))
            {
                SquashSubtitle();
            }
        }

        void OnTitleLabelGeometryChanged(GeometryChangedEvent evt)
        {
            if (!m_NeedsTitleTextfieldResize || m_Root.ClassListContains(k_TitleInFocusUssClassName))
                return;

            if (m_Root.ClassListContains(k_NodeTitlePartHoverStateUssClassName))
            {
                SquashTitleLabel();
                m_NeedsTitleTextfieldResize = false;
            }
            else if (m_Root.ClassListContains(k_EmptyTitleHoverStateUssClassName))
            {
                SquashSubtitle();
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
        /// </summary>
        void SquashTitleLabel()
        {
            if (!m_IsTitleAtFullWidth)
                return;

            var width = TitleLabel.resolvedStyle.width - m_NodeToolbarButtonsContainer.childCount * k_ButtonWidth;
            TitleLabel.style.width = new StyleLength(new Length(width, LengthUnit.Pixel));
            m_IsTitleAtFullWidth = false;
        }

        /// <summary>
        /// Have the subtitle make space for the hover buttons of this node.
        /// </summary>
        void SquashSubtitle()
        {
            if (!m_IsTitleAtFullWidth)
                return;

            if (m_SubTitle == null)
                return;

            var width = m_SubTitle.resolvedStyle.width - m_NodeToolbarButtonsContainer.childCount * k_ButtonWidth;
            m_SubTitle.style.width = new StyleLength(new Length(width, LengthUnit.Pixel));
            m_IsTitleAtFullWidth = false;
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

            public bool UpdateLineColorFromModel(UpdateFromModelVisitor visitor, AbstractNodeModel nodeModel) => nodeTitlePart.UpdateLineColorFromModel(visitor, nodeModel);
            public void BuildUI(VisualElement container) => nodeTitlePart.BuildUI(container);
        }
    }
}
