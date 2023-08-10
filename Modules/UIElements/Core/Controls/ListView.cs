// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A ListView is a vertically scrollable area that links to, and displays, a list of items.
    /// </summary>
    /// <remarks>
    /// A <see cref="ListView"/> is a <see cref="ScrollView"/> with additional logic to display a list of vertically-arranged
    /// VisualElements. Each VisualElement in the list is bound to a corresponding element in a data-source list. The
    /// data-source list can contain elements of any type.
    ///
    /// The logic required to create VisualElements, and to bind them to or unbind them from the data source, varies depending
    /// on the intended result. For the ListView to function correctly, you must supply at least a value for
    /// <see cref="BaseVerticalCollectionView.itemsSource">itemsSource</see>.
    ///
    /// It's also recommended to supply the following properties for more complex items:\\
    /// - <see cref="ListView.makeItem">makeItem</see> \\
    /// - <see cref="ListView.bindItem">bindItem</see> \\
    /// - <see cref="ListView.unbindItem">unbindItem</see> \\
    /// - <see cref="ListView.destroyItem">destroyItem</see> \\
    /// - <see cref="BaseVerticalCollectionView.fixedItemHeight">fixedItemHeight</see> when using <see cref="CollectionVirtualizationMethod.FixedHeight"/>\\
    ///
    /// The <c>ListView</c> creates multiple <see cref="VisualElement"/> objects for the visible items. As the user scrolls, the ListView
    /// recycles these objects and re-binds them to new data items.
    ///
    /// To set the height of a single item in pixels, set the <c>item-height</c> property in UXML or the
    ///     <see cref="ListView.itemHeight"/> property in C# to the desired value. \\
    /// \\
    /// To display a border around the scrollable area, set the <c>show-border</c> property in UXML or the
    ///     <see cref="ListView.showBorder"/> property in C# to <c>true</c>.\\
    ///     \\
    /// By default, the user can select one element in the list at a time. To change the default selection
    /// use the <c>selection-type</c> property in UXML or the<see cref="ListView.selectionType"/> property in C#.
    ///     To allow the user to select more than one element simultaneously, set the property to <c>Selection.Multiple</c>.
    ///     To prevent the user from selecting items, set the property to <c>Selection.None</c>.\\
    /// \\
    ///     By default, all rows in the ListView have same background color. To make the row background colors
    ///     alternate, set the <c>show-alternating-row-backgrounds</c> property in UXML or the
    ///     <see cref="ListView.showAlternatingRowBackgrounds"/> property in C# to
    ///     <see cref="AlternatingRowBackground.ContentOnly"/> or
    ///     <see cref="AlternatingRowBackground.All"/>. For details, see <see cref="AlternatingRowBackground"/>. \\
    /// \\
    ///     By default, the user can't reorder the list's elements. To allow the user to drag the elements
    ///     to reorder them, set the <c>reorderable</c> property in UXML or the <see cref="ListView.reorderable"/>
    ///     property in C# to <c>true</c>.\\
    /// \\
    /// To make the first item in the ListView display the number of items in the list, set the
    ///     <c>show-bound-collection-size</c> property in UXML or the <see cref="ListView.showBoundCollectionSize"/>
    ///     to true. This is useful for debugging. By default, the ListView's scroller element only scrolls vertically.\\
    /// \\
    /// To enable horizontal scrolling when the displayed element is wider than the visible area, set the
    ///     <c>horizontal-scrolling-enabled</c> property in UXML or the <see cref="ListView.horizontalScrollingEnabled"/>
    ///     to <c>true</c>.
    /// </remarks>
    /// <example>
    /// The following example creates an editor window with a list view of a thousand items.
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/ListView_Example.cs"/>
    /// </example>
    public class ListView : BaseListView
    {
        internal static readonly BindingId itemTemplateProperty = nameof(itemTemplate);
        internal static readonly BindingId makeItemProperty = nameof(makeItem);
        internal static readonly BindingId bindItemProperty = nameof(bindItem);
        internal static readonly BindingId unbindItemProperty = nameof(unbindItem);
        internal static readonly BindingId destroyItemProperty = nameof(destroyItem);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseListView.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] private VisualTreeAsset itemTemplate;
            #pragma warning restore 649

            public override object CreateInstance() => new ListView();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (ListView) obj;
                e.itemTemplate = itemTemplate;
            }
        }

        /// <summary>
        /// Instantiates a <see cref="ListView"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        public new class UxmlFactory : UxmlFactory<ListView, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ListView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the ListView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        public new class UxmlTraits : BaseListView.UxmlTraits
        {
            UxmlAssetAttributeDescription<VisualTreeAsset> m_ItemTemplate = new UxmlAssetAttributeDescription<VisualTreeAsset> { name = "item-template" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var view = ve as ListView;

                if (m_ItemTemplate.TryGetValueFromBag(bag, cc, out var itemTemplate))
                {
                    view.itemTemplate = itemTemplate;
                }
            }
        }

        Func<VisualElement> m_MakeItem;

        /// <summary>
        /// Callback for constructing the VisualElement that is the template for each recycled and re-bound element in the list.
        /// </summary>
        /// <remarks>
        /// This callback needs to call a function that constructs a blank <see cref="VisualElement"/> that is
        /// bound to an element from the list.
        ///
        /// The BaseVerticalCollectionView automatically creates enough elements to fill the visible area, and adds more if the area
        /// is expanded. As the user scrolls, the BaseVerticalCollectionView cycles elements in and out as they appear or disappear.
        ///
        /// If this property and <see cref="bindItem"/> are not set, Unity will either create a PropertyField if bound
        /// to a SerializedProperty, or create an empty label for any other case.
        /// </remarks>
        [CreateProperty]
        public new Func<VisualElement> makeItem
        {
            get => m_MakeItem;
            set
            {
                if (value != m_MakeItem)
                {
                    m_MakeItem = value;
                    Rebuild();
                    NotifyPropertyChanged(makeItemProperty);
                }
            }
        }

        internal void SetMakeItemWithoutNotify(Func<VisualElement> func)
        {
            m_MakeItem = func;
        }

        Func<VisualElement> m_TemplateMakeItem;
        VisualTreeAsset m_ItemTemplate;

        /// <summary>
        /// A UXML template that constructs each recycled and rebound element within the list.
        /// This template is designed to replace the <see cref="makeItem"/> definition.
        /// </summary>
        /// <remarks>
        /// You can use it along with <see cref="BaseListView.bindingSourceSelectionMode"/> and bindings to have a completely codeless workflow.
        /// </remarks>
        [CreateProperty]
        public VisualTreeAsset itemTemplate
        {
            get => m_ItemTemplate;
            set
            {
                if (m_ItemTemplate == value)
                    return;

                m_ItemTemplate = value;

                if (m_TemplateMakeItem != makeItem)
                    makeItem = m_TemplateMakeItem;
                else
                    Rebuild();
                NotifyPropertyChanged(itemTemplateProperty);
            }
        }

        VisualElement TemplateMakeItem()
        {
            if (m_ItemTemplate != null)
            {
                return m_ItemTemplate.Instantiate();
            }

            return new Label(k_InvalidTemplateError);
        }

        private Action<VisualElement, int> m_BindItem;

        /// <summary>
        /// Callback for binding a data item to the visual element.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement to bind, and the index of the
        /// element to bind it to.
        ///
        /// If this property and <see cref="makeItem"/> are not set, Unity will try to bind to a SerializedProperty if
        /// bound, or simply set text in the created Label.
        /// </remarks>
        [CreateProperty]
        public new Action<VisualElement, int> bindItem
        {
            get => m_BindItem;
            set
            {
                if (value != m_BindItem)
                {
                    m_BindItem = value;
                    RefreshItems();
                    NotifyPropertyChanged(bindItemProperty);
                }
            }
        }

        internal void SetBindItemWithoutNotify(Action<VisualElement, int> callback)
        {
            m_BindItem = callback;
        }

        private Action<VisualElement, int> m_UnbindItem;
        /// <summary>
        /// Callback for unbinding a data item from the VisualElement.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement to unbind, and the index of the
        /// element to unbind it from.
        /// </remarks>
        [CreateProperty]
        public new Action<VisualElement, int> unbindItem
        {
            get => m_UnbindItem;
            set
            {
                if (value == m_UnbindItem)
                    return;
                m_UnbindItem = value;
                NotifyPropertyChanged(unbindItemProperty);
            }
        }

        private Action<VisualElement> m_DestroyItem;

        /// <summary>
        /// Callback invoked when a <see cref="VisualElement"/> created via <see cref="makeItem"/> is no longer needed and will be destroyed.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement that will be destroyed from the pool.
        /// </remarks>
        [CreateProperty]
        public new Action<VisualElement> destroyItem
        {
            get => m_DestroyItem;
            set
            {
                if (value == m_DestroyItem)
                    return;
                m_DestroyItem = value;
                NotifyPropertyChanged(destroyItemProperty);
            }
        }

        internal override bool HasValidDataAndBindings()
        {
            return base.HasValidDataAndBindings() && (autoAssignSource && makeItem != null || !(makeItem != null ^ bindItem != null));
        }

        protected override CollectionViewController CreateViewController() => new ListViewController();

        /// <summary>
        /// Creates a <see cref="ListView"/> with all default properties. The <see cref="ListView.itemsSource"/>
        /// must all be set for the ListView to function properly.
        /// </summary>
        public ListView()
        {
            AddToClassList(ussClassName);
            m_TemplateMakeItem = TemplateMakeItem;
        }

        /// <summary>
        /// Constructs a <see cref="ListView"/>, with all important properties provided.
        /// </summary>
        /// <param name="itemsSource">The list of items to use as a data source.</param>
        /// <param name="itemHeight">The height of each item, in pixels.</param>
        /// <param name="makeItem">The factory method to call to create a display item. The method should return a
        /// VisualElement that can be bound to a data item.</param>
        /// <param name="bindItem">The method to call to bind a data item to a display item. The method
        /// receives as parameters the display item to bind, and the index of the data item to bind it to.</param>
        public ListView(IList itemsSource, float itemHeight = ItemHeightUnset, Func<VisualElement> makeItem = null, Action<VisualElement, int> bindItem = null)
            : base(itemsSource, itemHeight)
        {
            AddToClassList(ussClassName);
            m_TemplateMakeItem = TemplateMakeItem;

            this.makeItem = makeItem;
            this.bindItem = bindItem;
        }
    }
}
