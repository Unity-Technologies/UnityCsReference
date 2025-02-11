// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Creates a tab view that groups a one or more <see cref="Tab"/> elements.
    /// </summary>
    /// <remarks>
    /// A tab view provides additional functionality to the Tab. It allows a group of tabs to interact between each
    /// other by hiding the inactive tabs and displaying the active one. Additionally, it adds the ability to re-order
    /// the group of tabs.
    ///
    /// For reorderable tabs, it is important to keep the view-data-key unique for the TabView and Tab elements inside
    /// of it. The view-data-key helps store the state of the tabs such as the order and state. By having an empty
    /// view-data-key, which is the default value, the state of the tabs will not persist when reloading the document.
    ///
    /// For more information, refer to [[wiki:UIE-uxml-element-TabView|UXML element TabView]].
    /// </remarks>
    public class TabView : VisualElement
    {
        internal static readonly BindingId reorderableProperty = nameof(reorderable);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(reorderable), "reorderable")
                });
            }

            #pragma warning disable 649
            [SerializeField] bool reorderable;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags reorderable_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new TabView();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(reorderable_UxmlAttributeFlags))
                {
                    var e = (TabView)obj;
                    e.reorderable = reorderable;
                }
            }
        }

        /// <summary>
        /// Instantiates a <see cref="TabView"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<TabView, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TabView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a TabView element that you can use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription m_Reorderable = new UxmlBoolAttributeDescription { name = "reorderable", defaultValue = false };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var tabView = (TabView)ve;
                tabView.reorderable = m_Reorderable.GetValueFromBag(bag, cc);
            }
        }

        [Serializable]
        class ViewState : ISerializationCallbackReceiver
        {
            bool m_HasPersistedData;

            [SerializeField]
            List<string> m_TabOrder = new();

            [SerializeField]
            string m_ActiveTabKey;

            /// <summary>
            /// Saves the state of the specified tab view control.
            /// </summary>
            /// <param name="tabView">The tab view control of which state to save.</param>
            internal void Save(TabView tabView)
            {
                m_HasPersistedData = true;

                if (tabView.m_ActiveTab != null)
                    m_ActiveTabKey = tabView.m_ActiveTab.viewDataKey;

                m_TabOrder.Clear();

                if (!tabView.reorderable)
                    return;

                foreach (var tab in tabView.tabs)
                {
                    m_TabOrder.Add(tab.viewDataKey);
                }
            }

            /// <summary>
            /// Applies the state of the specified tab view control.
            /// </summary>
            internal void Apply(TabView tabView)
            {
                if (!m_HasPersistedData)
                    return;

                var minCount = Math.Min(m_TabOrder.Count, tabView.tabs.Count);
                var nextValidOrderedIndex = 0;

                var tabToActivate = tabView.FindTabByKey(m_ActiveTabKey);
                if (tabToActivate != null)
                    tabView.activeTab = tabToActivate;

                if (!tabView.reorderable)
                    return;

                for (var orderedIndex = 0; (orderedIndex < m_TabOrder.Count) && (nextValidOrderedIndex < minCount); orderedIndex++)
                {
                    var tabKey = m_TabOrder[orderedIndex];
                    var tab = tabView.FindTabByKey(tabKey);
                    if (tab != null)
                    {
                        var displayIndex = tabView.tabs.IndexOf(tab);
                        tabView.ReorderTab(displayIndex, nextValidOrderedIndex++);
                    }
                }
            }

            public void OnBeforeSerialize()
            {
                m_HasPersistedData = true;
            }

            public void OnAfterDeserialize()
            {
                m_HasPersistedData = true;
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-tab-view";

        /// <summary>
        /// USS class name for the header container of this type.
        /// </summary>
        public static readonly string headerContainerClassName = ussClassName + "__header-container";

        /// <summary>
        /// USS class name for the content container of this type.
        /// </summary>
        public static readonly string contentContainerUssClassName = ussClassName + "__content-container";

        /// <summary>
        /// The USS class name for reorderable tab view.
        /// </summary>
        public static readonly string reorderableUssClassName = ussClassName + "__reorderable";

        /// <summary>
        /// The USS class name for vertical tab view.
        /// </summary>
        public static readonly string verticalUssClassName = ussClassName + "__vertical";

        /// <summary>
        /// The container for the content of the <see cref="TabView"/>.
        /// </summary>
        public override VisualElement contentContainer => m_ContentContainer;

        VisualElement m_HeaderContainer;
        VisualElement m_ContentContainer;
        List<Tab> m_Tabs = new();
        List<VisualElement> m_TabHeaders = new();
        Tab m_ActiveTab;

        ViewState m_ViewState;
        bool m_ApplyingViewState;
        bool m_Reordering;
        internal VisualElement header => m_HeaderContainer;
        internal List<Tab> tabs => m_Tabs;
        internal List<VisualElement> tabHeaders => m_TabHeaders;

        /// <summary>
        /// Called when the active tab is reassigned.
        /// </summary>
        /// <remarks>
        /// This callback receives two tabs, the first being the previously selected tab and the second
        /// being the newly selected tab.
        /// </remarks>
        public event Action<Tab, Tab> activeTabChanged;

        /// <summary>
        /// Raised when a tab is reordered.
        /// The first argument is source index, second is destination index.
        /// </summary>
        public event Action<int, int> tabReordered;

        /// <summary>
        /// Raised when a tab is closed.
        /// The first argument is the tab that was closed, the second is the index of the tab that was closed.
        /// </summary>
        public event Action<Tab, int> tabClosed;

        /// <summary>
        /// Property that holds the current active tab.
        /// </summary>
        /// <remarks>
        /// Setting a valid tab will also deselect the previous active tab before setting the newly assigned tab to
        /// active.
        /// </remarks>
        /// <exception cref="NullReferenceException">If the Tab being passed is null and there are Tabs available to set as active.</exception>
        /// <exception cref="Exception">If the Tab being passed does not exists in the current TabView.</exception>
        public Tab activeTab
        {
            get => m_ActiveTab;
            set
            {
                if (value == null && m_Tabs.Count > 0)
                    throw new NullReferenceException("Active tab cannot be null when there are available tabs.");

                if (m_Tabs.IndexOf(value) == -1)
                    throw new Exception("The tab to be set as active does not exist in this TabView.");

                if (value == m_ActiveTab)
                    return;

                var previous = m_ActiveTab;
                m_ActiveTab?.SetInactive();
                m_ActiveTab = value;
                m_ActiveTab?.SetActive();

                if (!m_ApplyingViewState)
                    SaveViewState();

                activeTabChanged?.Invoke(previous, value);
            }
        }

        // This serves as an utility for users to leverage in their script.
        /// <summary>
        /// A property that returns the index of current active tab inside a list of available tabs.
        /// </summary>
        /// <remarks>
        /// Setting a valid index will set the Tab at the given index to active.
        /// </remarks>
        public int selectedTabIndex
        {
            get
            {
                if (activeTab == null || m_Tabs.Count == 0)
                    return -1;

                return m_Tabs.IndexOf(activeTab);
            }
            set
            {
                if (value >= 0 && m_Tabs.Count > value)
                    activeTab = m_Tabs[value];
            }
        }

        private bool m_Reorderable;

        /// <summary>
        /// A property that adds dragging support to tabs.
        /// </summary>
        /// <remarks>
        /// The default value is <c>false</c>.
        /// Set this value to <c>true</c> to allow the user to reorder tabs in the tab view.
        /// </remarks>
        [CreateProperty]
        public bool reorderable
        {
            get => m_Reorderable;
            set
            {
                if (m_Reorderable == value)
                    return;
                m_Reorderable = value;
                EnableInClassList(reorderableUssClassName, value);
                foreach (var tab in m_Tabs)
                {
                    tab.EnableTabDragHandles(value);
                }
                NotifyPropertyChanged(reorderableProperty);
            }
        }

        /// <summary>
        /// Constructs a TabView.
        /// </summary>
        public TabView()
        {
            AddToClassList(ussClassName);

            m_HeaderContainer = new VisualElement() { name = headerContainerClassName, classList = { headerContainerClassName }};
            hierarchy.Add(m_HeaderContainer);

            m_ContentContainer = new VisualElement() { name = contentContainerUssClassName, classList = { contentContainerUssClassName }};
            hierarchy.Add(m_ContentContainer);

            m_ContentContainer.elementAdded += OnElementAdded;
            m_ContentContainer.elementRemoved += OnElementRemoved;
        }

        internal override void OnViewDataReady()
        {
            try
            {
                m_ApplyingViewState = true;

                base.OnViewDataReady();

                var key = GetFullHierarchicalViewDataKey();

                m_ViewState = GetOrCreateViewData<ViewState>(m_ViewState, key);
                m_ViewState.Apply(this);
            }
            finally
            {
                m_ApplyingViewState = false;
            }
        }

        void SaveViewState()
        {
            if (m_ApplyingViewState)
                return;

            m_ViewState?.Save(this);
            SaveViewData();
        }

        void OnElementAdded(VisualElement ve, int index)
        {
            if (ve is not Tab tab || m_Reordering)
                return;

            var tabHeader = tab.tabHeader;
            if (tabHeader != null)
            {
                // Insert at specified index
                m_HeaderContainer.Insert(index, tabHeader);
                m_TabHeaders.Insert(index, tabHeader);
                m_Tabs.Insert(index, tab);
                tab.EnableTabDragHandles(m_Reorderable);

                tab.closed += (t) => OnTabClosed(t, index);
            }

            tab.selected += OnTabSelected;

            // Set the first tab to be active
            if (activeTab == null)
                activeTab = tab;
        }

        void OnElementRemoved(VisualElement ve)
        {
            if (ve is not Tab tab || m_Reordering)
                return;

            var tabHeaderVisualElement = tab.tabHeader;
            m_HeaderContainer.Remove(tabHeaderVisualElement);
            m_TabHeaders.Remove(tabHeaderVisualElement);
            m_Tabs.Remove(tab);

            // in case of tab being removed from TabView
            tab.EnableTabDragHandles(false);
            tab.hierarchy.Insert(0, tabHeaderVisualElement);
            tab.SetInactive();

            // If we delete an active tab and there are more available, default back to the first one.
            if (activeTab == tab && m_Tabs.Count > 0)
                activeTab = m_Tabs[0];
            else if (m_Tabs.Count == 0)
                m_ActiveTab = null;
        }

        void OnTabSelected(Tab tab)
        {
            activeTab = tab;
        }

        void OnTabClosed(Tab tab, int index)
        {
            tabClosed?.Invoke(tab, index);
        }

        /// <summary>
        /// Moves the tab from the specified index to a new index.
        /// </summary>
        /// <remarks>
        /// If the TabView contains a view-data-key, reorder the tab only after the view data is applied. Otherwise, the view data will override the order.
        /// </remarks>
        /// <param name="from">The current index of the tab to move.</param>
        /// <param name="to">The target index to move the tab to.</param>
        public void ReorderTab(int from, int to)
        {
            var tabHeader = m_TabHeaders[from];
            var tab = m_Tabs[from];

            if (!tabHeader.visible || !reorderable || from == to)
                return;

            m_Reordering = true;

            m_TabHeaders.RemoveAt(from);
            m_TabHeaders.Insert(to, tabHeader);

            m_Tabs.RemoveAt(from);
            m_Tabs.Insert(to, tab);

            m_HeaderContainer.Insert(to, tabHeader);
            Insert(to, tab);

            m_Reordering = false;

            tabReordered?.Invoke(from, to);

            if (!m_ApplyingViewState)
                SaveViewState();
        }

        /// <summary>
        /// Returns the tab at the specified index.
        /// </summary>
        public Tab GetTab(int index)
        {
            if (index < 0 || index >= m_Tabs.Count)
                return null;

            return m_Tabs[index];
        }

        /// <summary>
        /// Returns the tab header at the specified index.
        /// </summary>
        public VisualElement GetTabHeader(int index)
        {
            if (index < 0 || index >= m_Tabs.Count)
                return null;

            return m_TabHeaders[index];
        }

        internal Tab FindTabByKey(string key)
        {
            return m_Tabs.Find(tab => tab.viewDataKey == key);
        }
    }
}
