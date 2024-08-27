// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements.Internal
{
    /// <summary>
    /// Displays the sort arrow indicator and the sort index when multi-sorting.
    /// </summary>
    class MultiColumnHeaderColumnSortIndicator : VisualElement
    {
        public static readonly string ussClassName = MultiColumnHeaderColumn.ussClassName + "__sort-indicator";
        public static readonly string arrowUssClassName = ussClassName + "__arrow";
        public static readonly string indexLabelUssClassName = ussClassName + "__index-label";
        Label m_IndexLabel;

        public string sortOrderLabel
        {
            get => m_IndexLabel.text;
            set => m_IndexLabel.text = value;
        }

        public MultiColumnHeaderColumnSortIndicator()
        {
            AddToClassList(ussClassName);

            pickingMode = PickingMode.Ignore;
            VisualElement arrowIndicator = new VisualElement() { pickingMode = PickingMode.Ignore };

            arrowIndicator.AddToClassList(arrowUssClassName);
            Add(arrowIndicator);

            m_IndexLabel = new Label() { pickingMode = PickingMode.Ignore };
            m_IndexLabel.AddToClassList(indexLabelUssClassName);
            Add(m_IndexLabel);
        }
    }

    /// <summary>
    /// Displays the icon of a header column control.
    /// </summary>
    class MultiColumnHeaderColumnIcon : Image
    {
        public static new readonly string ussClassName = MultiColumnHeaderColumn.ussClassName + "__icon";

        public bool isImageInline { get; set; }

        public MultiColumnHeaderColumnIcon()
        {
            AddToClassList(ussClassName);
            RegisterCallback<CustomStyleResolvedEvent>((evt) => UpdateClassList());
        }

        public void UpdateClassList()
        {
            parent.RemoveFromClassList(MultiColumnHeaderColumn.hasIconUssClassName);

            if (image != null || sprite != null || vectorImage != null)
            {
                parent.AddToClassList(MultiColumnHeaderColumn.hasIconUssClassName);
            }
        }
    }

    /// <summary>
    /// Visual element representing column in header of a multi-column view. By default, it's basically a container for an image and a label.
    /// </summary>
    class MultiColumnHeaderColumn : VisualElement
    {
        public static readonly string ussClassName = MultiColumnCollectionHeader.ussClassName + "__column";
        public static readonly string sortableUssClassName = ussClassName + "--sortable";
        public static readonly string sortedAscendingUssClassName = ussClassName + "--sorted-ascending";
        public static readonly string sortedDescendingUssClassName = ussClassName + "--sorted-descending";
        public static readonly string movingUssClassName = ussClassName + "--moving";
        public static readonly string contentContainerUssClassName = ussClassName + "__content-container";
        public static readonly string contentUssClassName = ussClassName + "__content";
        public static readonly string defaultContentUssClassName = ussClassName + "__default-content";
        public static readonly string hasIconUssClassName = contentUssClassName + "--has-icon";
        public static readonly string hasTitleUssClassName = contentUssClassName + "--has-title";
        public static readonly string titleUssClassName = ussClassName + "__title";
        public static readonly string iconElementName = "unity-multi-column-header-column-icon";
        public static readonly string titleElementName = "unity-multi-column-header-column-title";
        static readonly string s_BoundVEPropertyName = "__bound";
        static readonly string s_BindingCallbackVEPropertyName = "__binding-callback";
        static readonly string s_UnbindingCallbackVEPropertyName = "__unbinding-callback";
        static readonly string s_DestroyCallbackVEPropertyName = "__destroy-callback";

        VisualElement m_ContentContainer;
        VisualElement m_Content;
        MultiColumnHeaderColumnSortIndicator m_SortIndicatorContainer;

        IVisualElementScheduledItem m_ScheduledHeaderTemplateUpdate;

        /// <summary>
        /// Returns the clickable manipulator.
        /// </summary>
        public Clickable clickable { get; private set; }

        /// <summary>
        /// Returns the reorder manipulator.
        /// </summary>
        public ColumnMover mover { get; private set; }

        /// <summary>
        /// The sort order of this column control when multi-sorting.
        /// </summary>
        public string sortOrderLabel
        {
            get => m_SortIndicatorContainer.sortOrderLabel;
            set => m_SortIndicatorContainer.sortOrderLabel = value;
        }

        /// <summary>
        /// The column related to this column control.
        /// </summary>
        public Column column { get; private set; }

        // For testing
        internal Label title => content?.Q<Label>(titleElementName);

        /// <summary>
        /// The content element of this column control.
        /// </summary>
        public VisualElement content
        {
            get => m_Content;
            set
            {
                if (m_Content != null)
                {
                    if (m_Content.parent == m_ContentContainer)
                    {
                        m_Content.RemoveFromHierarchy();
                    }
                    DestroyHeaderContent();

                    m_Content = null;
                }

                m_Content = value;

                if (m_Content != null)
                {
                    m_Content.AddToClassList(contentUssClassName);
                    m_ContentContainer.Add(m_Content);
                }
            }
        }

        bool isContentBound
        {
            get => m_Content != null && (bool) m_Content.GetProperty(s_BoundVEPropertyName);
            set => m_Content?.SetProperty(s_BoundVEPropertyName, value);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MultiColumnHeaderColumn()
            : this(new Column()) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="column"></param>
        public MultiColumnHeaderColumn(Column column)
        {
            this.column = column;
            this.column.changed += OnColumnChanged;
            this.column.resized += OnColumnResized;
            AddToClassList(ussClassName);
            // Enforce to avoid override from uss.
            style.marginLeft = 0;
            style.marginTop = 0;
            style.marginRight = 0;
            style.marginBottom = 0;
            style.paddingLeft = 0;
            style.paddingTop = 0;
            style.paddingRight = 0;
            style.paddingBottom = 0;

            Add(m_SortIndicatorContainer = new MultiColumnHeaderColumnSortIndicator());

            m_ContentContainer = new VisualElement();
            m_ContentContainer.style.flexGrow = 1;
            m_ContentContainer.style.flexShrink = 1;
            m_ContentContainer.AddToClassList(contentContainerUssClassName);
            Add(m_ContentContainer);

            UpdateHeaderTemplate();
            UpdateGeometryFromColumn();
            InitManipulators();
        }

        private void OnColumnChanged(Column c, ColumnDataType role)
        {
            if (column != c)
                return;

            if (role == ColumnDataType.HeaderTemplate)
            {
                m_ScheduledHeaderTemplateUpdate?.Pause();
                m_ScheduledHeaderTemplateUpdate = schedule.Execute(UpdateHeaderTemplate);
            }
            else
            {
                UpdateDataFromColumn();
            }
        }

        private void OnColumnResized(Column c)
        {
            UpdateGeometryFromColumn();
        }

        void InitManipulators()
        {
            this.AddManipulator(mover = new ColumnMover());
            mover.movingChanged += OnMoverChanged;
            this.AddManipulator(clickable = new Clickable((Action)null));
            clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });

            EventModifiers multiSortingModifier = EventModifiers.Control;

            if (Application.platform is RuntimePlatform.OSXEditor or RuntimePlatform.OSXPlayer)
            {
                multiSortingModifier = EventModifiers.Command;
            }
            clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = multiSortingModifier });
        }

        private void OnMoverChanged(ColumnMover mv)
        {
            if (mover.moving)
                AddToClassList(movingUssClassName);
            else
                RemoveFromClassList(movingUssClassName);
        }

        void UpdateDataFromColumn()
        {
            if (column == null)
                return;

            name = column.name;
            UnbindHeaderContent();
            BindHeaderContent();
        }

        void BindHeaderContent()
        {
            if (!isContentBound)
            {
                var bindCallback = content.GetProperty(s_BindingCallbackVEPropertyName) as Action<VisualElement>;

                bindCallback?.Invoke(content);
                isContentBound = true;
            }
        }

        void UnbindHeaderContent()
        {
            if (isContentBound)
            {
                var unbindCallback = content.GetProperty(s_UnbindingCallbackVEPropertyName) as Action<VisualElement>;

                unbindCallback?.Invoke(content);
                isContentBound = false;
            }
        }

        void DestroyHeaderContent()
        {
            // Unbind if bound before destroying
            UnbindHeaderContent();
            var destroyCallback = content.GetProperty(s_DestroyCallbackVEPropertyName) as Action<VisualElement>;

            content.ClearProperty(s_BindingCallbackVEPropertyName);
            content.ClearProperty(s_UnbindingCallbackVEPropertyName);
            content.ClearProperty(s_DestroyCallbackVEPropertyName);
            content.ClearProperty(s_BoundVEPropertyName);
            destroyCallback?.Invoke(content);
        }

        VisualElement CreateDefaultHeaderContent()
        {
            var defContent = new VisualElement() { pickingMode = PickingMode.Ignore };

            defContent.AddToClassList(defaultContentUssClassName);

            var icon = new MultiColumnHeaderColumnIcon() { name = iconElementName, pickingMode = PickingMode.Ignore };

            var title = new Label() { name = titleElementName, pickingMode = PickingMode.Ignore };
            title.AddToClassList(titleUssClassName);

            defContent.Add(icon);
            defContent.Add(title);

            return defContent;
        }

        void DefaultBindHeaderContent(VisualElement ve)
        {
            var title = ve.Q<Label>(titleElementName);
            var icon = ve.Q<MultiColumnHeaderColumnIcon>();

            ve.RemoveFromClassList(hasTitleUssClassName);

            if (title != null)
                title.text = column.title;

            if (!string.IsNullOrEmpty(column.title))
                ve.AddToClassList(hasTitleUssClassName);

            if (icon != null)
            {
                if (column.icon.texture != null || column.icon.sprite != null || column.icon.vectorImage != null)
                {
                    icon.isImageInline = true;
                    icon.image = column.icon.texture;
                    icon.sprite = column.icon.sprite;
                    icon.vectorImage = column.icon.vectorImage;
                }
                else
                {
                    if (icon.isImageInline)
                    {
                        icon.image = null;
                        icon.sprite = null;
                        icon.vectorImage = null;
                    }
                }
                icon.UpdateClassList();
            }
        }

        void UpdateHeaderTemplate()
        {
            if (column == null)
                return;

            Func<VisualElement> makeContentCallback = column.makeHeader;
            Action<VisualElement> bindContentCallback = column.bindHeader;
            Action<VisualElement> unbindContentCallback = column.unbindHeader;
            Action<VisualElement> destroyCallback = column.destroyHeader;

            if (makeContentCallback == null)
            {
                makeContentCallback = CreateDefaultHeaderContent;
                bindContentCallback = DefaultBindHeaderContent;
                unbindContentCallback = null;
                destroyCallback = null;
            }

            content = makeContentCallback();
            content.SetProperty(s_BindingCallbackVEPropertyName, bindContentCallback);
            content.SetProperty(s_UnbindingCallbackVEPropertyName, unbindContentCallback);
            content.SetProperty(s_DestroyCallbackVEPropertyName, destroyCallback);
            isContentBound = false;
            m_ScheduledHeaderTemplateUpdate = null;
            UpdateDataFromColumn();
        }

        void UpdateGeometryFromColumn()
        {
            if (float.IsNaN(column.desiredWidth))
                return;
            style.width = column.desiredWidth;
        }

        public void Dispose()
        {
            mover.movingChanged -= OnMoverChanged;
            column.changed -= OnColumnChanged;
            column.resized -= OnColumnResized;
            this.RemoveManipulator(mover);
            this.RemoveManipulator(clickable);
            mover = null;
            column = null;
            content = null;
        }
    }
}
