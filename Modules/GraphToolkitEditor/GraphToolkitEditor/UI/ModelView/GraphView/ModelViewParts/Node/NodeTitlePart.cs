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
        public static readonly string emptySubtitleUssClassName = ussClassName.WithUssModifier(GraphElementHelper.emptyUssModifier);

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
        /// <returns>A new instance of <see cref="NodeTitlePart"/>.</returns>
        public static NodeTitlePart Create(string name, GraphElementModel nodeModel, ChildView ownerElement, string parentClassName, int options = Options.Default)
        {
            return new NodeTitlePart(name, nodeModel, ownerElement, parentClassName, options);
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

        bool m_Attached;

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
        protected NodeTitlePart(string name, GraphElementModel model, ChildView ownerElement, string parentClassName, int options)
            : base(name, model, ownerElement, parentClassName, options)
        {
            Assert.IsTrue((options & Unity.GraphToolkit.Editor.EditableTitlePart.Options.Multiline) == 0);
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            if (!m_Attached)
            {
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
        }

        List<string> m_PreviousIconClasses = new List<string>();
        string m_PreviousIconString = null;
        string m_PreviousIconPath;

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
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

                if (m_Icon.image == null && !string.IsNullOrEmpty(nodeModel.IconTypeString))
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

            UpdateLineColorFromModel(visitor, nodeModel);

            if (visitor.ChangeHints.HasChange(ChangeHint.Data))
            {
                var subTitle = (m_Model as AbstractNodeModel)?.Subtitle;

                m_SubTitle.text = subTitle;
                m_OwnerElement.EnableInClassList(emptySubtitleUssClassName, string.IsNullOrEmpty(subTitle));
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

        private void AddModeDropdown()
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
