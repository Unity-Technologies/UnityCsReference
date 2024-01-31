// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Serialization;

namespace UnityEditor.Search
{
    /// <summary>
    /// Defines at set of options that indicates to the search provider how the preview should be fetched.
    /// </summary>
    [Flags]
    public enum FetchPreviewOptions
    {
        /// <summary>
        /// No options are defined.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the provider should generate a 2D preview.
        /// </summary>
        Preview2D = 1,

        /// <summary>
        /// Indicates that the provider should generate a 3D preview.
        /// </summary>
        Preview3D = 1 << 1,

        /// <summary>
        /// Indicate that the preview size should be around 128x128.
        /// </summary>
        Normal    = 1 << 10,

        /// <summary>
        /// Indicates that the preview resolution should be higher than 256x256.
        /// </summary>
        Large     = 1 << 11
    }

    /// <summary>
    /// Defines what details are shown in the details panel for the search view.
    /// </summary>
    [Flags]
    public enum ShowDetailsOptions
    {
        /// <summary>
        /// No options are defined.
        /// </summary>
        None = 0,

        /// <summary>
        /// Show a large preview.
        /// </summary>
        Preview = 1 << 0,

        /// <summary>
        /// Show an embedded inspector for the selected object.
        /// </summary>
        Inspector = 1 << 1,

        /// <summary>
        /// Show selected item possible actions
        /// </summary>
        Actions = 1 << 2,

        /// <summary>
        /// Show an extended item description
        /// </summary>
        Description = 1 << 3,

        /// <summary>
        /// If possible, default the list view to list view.
        /// </summary>
        ListView = 1 << 4,

        /// <summary>
        /// Show this provider as a default group (always visible even if no result)
        /// </summary>
        DefaultGroup = 1 << 5,

        /// <summary>
        /// Show inspector of this provider without a header.
        /// </summary>
        InspectorWithoutHeader = 1 << 6,

        /// <summary>
        /// Default set of options used when [[showDetails]] is set to true.
        /// </summary>
        Default = Preview | Actions | Description
    }

    /// <summary>
    /// SearchProvider manages search for specific type of items and manages thumbnails, description and sub filters, etc.
    /// </summary>
    [Serializable]
    public class SearchProvider : ISerializationCallbackReceiver
    {
        internal static int sessionCounter;

        [SerializeField] private string m_Id;
        [SerializeField] private string m_Type;
        [SerializeField] private string m_Name;

        // INTERNAL
        [NonSerialized] internal double fetchTime;
        [NonSerialized] internal double loadTime;
        [NonSerialized] internal double enableTime;
        [NonSerialized] internal int m_EnableLockCounter;
        [NonSerialized] internal SearchContext defaultContext;

        /// <summary>
        /// Hint to sort the provider. Affect the order of search results.
        /// </summary>
        public int priority;

        /// <summary>
        /// Indicates if the provider is active or not. Inactive providers are completely ignored by the search service. The active state can be toggled in the search settings.
        /// </summary>
        public bool active;

        /// <summary> Text token use to "filter" a provider (ex:  "me:", "p:", "s:")</summary>
        public string filterId;

        /// <summary> This provider is only active when specified explicitly using his filterId</summary>
        public bool isExplicitProvider;

        /// <summary> Does this provider supports synchronized view searches (ex: Scene provider can be synchronized with Hierarchy view).</summary>
        [SerializeField] internal bool supportsSyncViewSearch;

        /// <summary> Indicates if the provider can show additional details or not.</summary>
        public bool showDetails;

        /// <summary> Explicitly define details options to be shown</summary>
        public ShowDetailsOptions showDetailsOptions = ShowDetailsOptions.Default;

        /// <summary> MANDATORY: Handler to get items for a given search context.
        /// The return value is an object that can be of type IEnumerable or IEnumerator.
        /// The enumeration of those objects should return SearchItems.
        /// </summary>
        public Func<SearchContext, List<SearchItem>, SearchProvider, object> fetchItems;
        [SerializeField] private SearchFunctor<Func<SearchContext, List<SearchItem>, SearchProvider, object>> m_Serialized_fetchItems;

        /// <summary> Handler used to fetch and format the label of a search item.</summary>
        public Func<SearchItem, SearchContext, string> fetchLabel;
        [SerializeField] private SearchFunctor<Func<SearchItem, SearchContext, string>> m_Serialized_fetchLabel;

        /// <summary>
        /// Handler to provider an async description for an item. Will be called when the item is about to be displayed.
        /// Allows a plugin provider to only fetch long description when they are needed.
        /// </summary>
        public Func<SearchItem, SearchContext, string> fetchDescription;
        [SerializeField] private SearchFunctor<Func<SearchItem, SearchContext, string>> m_Serialized_fetchDescription;

        /// <summary>
        /// Handler to provider an async thumbnail for an item. Will be called when the item is about to be displayed.
        /// Compared to preview a thumbnail should be small and returned as fast as possible. Use fetchPreview if you want to generate a preview that is bigger and slower to return.
        /// Allows a plugin provider to only fetch/generate preview when they are needed.
        /// </summary>
        public Func<SearchItem, SearchContext, Texture2D> fetchThumbnail;
        [SerializeField] private SearchFunctor<Func<SearchItem, SearchContext, Texture2D>> m_Serialized_fetchThumbnail;

        /// <summary>
        /// Similar to fetchThumbnail, fetchPreview usually returns a bigger preview. The QuickSearch UI will progressively show one preview each frame,
        /// preventing the UI to block if many preview needs to be generated at the same time.
        /// </summary>
        public Func<SearchItem, SearchContext, Vector2, FetchPreviewOptions, Texture2D> fetchPreview;
        [SerializeField] private SearchFunctor<Func<SearchItem, SearchContext, Vector2, FetchPreviewOptions, Texture2D>> m_Serialized_fetchPreview;

        /// <summary>
        /// Provider can return a list of words that will help the user complete his search query.
        /// </summary>
        public Func<SearchContext, SearchPropositionOptions, IEnumerable<SearchProposition>> fetchPropositions;
        [SerializeField] private SearchFunctor<Func<SearchContext, SearchPropositionOptions, IEnumerable<SearchProposition>>> m_Serialized_fetchPropositions;

        /// <summary>
        /// Fetch search tables that are used to display search result using a table view.
        /// </summary>
        public Func<SearchContext, IEnumerable<SearchItem>, IEnumerable<SearchColumn>> fetchColumns;
        [SerializeField] private SearchFunctor<Func<SearchContext, IEnumerable<SearchItem>, IEnumerable<SearchColumn>>> m_Serialized_fetchColumns;

        /// <summary> If implemented, it means the item supports drag. It is up to the SearchProvider to properly setup the DragAndDrop manager.</summary>
        public Action<SearchItem, SearchContext> startDrag;
        [SerializeField] private SearchFunctor<Action<SearchItem, SearchContext>> m_Serialized_startDrag;

        /// <summary> Called when the selection changed and can be tracked.</summary>
        public Action<SearchItem, SearchContext> trackSelection;
        [SerializeField] private SearchFunctor<Action<SearchItem, SearchContext>> m_Serialized_trackSelection;

        /// <summary>
        /// Returns a default table configuration for the current provider if any.
        /// TODO: Make public when 22.2 Search API lands
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal Func<SearchContext, SearchTable> tableConfig
        {
            get => m_Serialized_tableConfig?.handler;
            set
            {
                if (m_Serialized_tableConfig == null)
                    m_Serialized_tableConfig = new SearchFunctor<Func<SearchContext, SearchTable>>(value);
                else
                    m_Serialized_tableConfig.handler = value;
            }
        }
        [SerializeField] private SearchFunctor<Func<SearchContext, SearchTable>> m_Serialized_tableConfig;

        /// <summary> Returns any valid Unity object held by the search item.</summary>
        public Func<SearchItem, Type, UnityEngine.Object> toObject;
        [SerializeField] private SearchFunctor<Func<SearchItem, Type, UnityEngine.Object>> m_Serialized_toObject;

        /// <summary> Returns any valid item type.</summary>
        internal Func<SearchItem, Type, Type> toType
        {
            get => m_Serialized_toType?.handler;
            set
            {
                if (m_Serialized_toType == null)
                    m_Serialized_toType = new SearchFunctor<Func<SearchItem, Type, Type>>(value);
                else
                    m_Serialized_toType.handler = value;
            }
        }
        [SerializeField] private SearchFunctor<Func<SearchItem, Type, Type>> m_Serialized_toType;

        /// <summary> Returns a document key in which the item is contained.</summary>
        internal Func<SearchItem, ulong> toKey
        {
            get => m_Serialized_toKey?.handler;
            set
            {
                if (m_Serialized_toKey == null)
                    m_Serialized_toKey = new SearchFunctor<Func<SearchItem, ulong>>(value);
                else
                    m_Serialized_toKey.handler = value;
            }
        }
        [SerializeField] private SearchFunctor<Func<SearchItem, ulong>> m_Serialized_toKey;

        /// <summary> Returns an item hashed key (usually the contained object instance id).</summary>
        internal Func<SearchItem, int> toInstanceId
        {
            get => m_Serialized_toInstanceId?.handler;
            set
            {
                if (m_Serialized_toInstanceId == null)
                    m_Serialized_toInstanceId = new SearchFunctor<Func<SearchItem, int>>(value);
                else
                    m_Serialized_toInstanceId.handler = value;
            }
        }
        [SerializeField] private SearchFunctor<Func<SearchItem, int>> m_Serialized_toInstanceId;

        /// <summary>
        /// Called when the QuickSearchWindow is opened. Allow the Provider to perform some caching.
        /// </summary>
        public Action onEnable;
        [SerializeField] private SearchFunctor<Action> m_Serialized_onEnable;

        /// <summary>
        /// Called when the QuickSearchWindow is closed. Allow the Provider to release cached resources.
        /// </summary>
        public Action onDisable;
        [SerializeField] private SearchFunctor<Action> m_Serialized_onDisable;

        /// <summary>
        /// Called when quick search is invoked in "contextual mode". If you return true it means the provider is enabled for this search context.
        /// </summary>
        public Func<bool> isEnabledForContextualSearch;
        [SerializeField] private SearchFunctor<Func<bool>> m_Serialized_isEnabledForContextualSearch;

        /// <summary>Unique id of the provider.</summary>
        public string id { get => m_Id; private set => m_Id = value; }

        /// <summary>Non-unique id of the provider. This is used for specialized or grouped providers.</summary>
        public string type { get => m_Type; set => m_Type = value; }

        /// <summary>Display name of the provider.</summary>
        public string name { get => m_Name; private set => m_Name = value; }

        /// <summary>
        /// List of actions the search provider supports.
        /// </summary>
        public List<SearchAction> actions { get; internal set; } // TODO: Add SearchAction serialization

        /// <summary>
        /// Create a new SearchProvider
        /// </summary>
        /// <param name="id">Search Provider unique id.</param>
        public SearchProvider(string id)
            : this(id, null, (Func<SearchContext, List<SearchItem>, SearchProvider, object>)null)
        {
        }

        /// <summary>
        /// Create a new SearchProvider
        /// </summary>
        /// <param name="id">Search Provider unique id.</param>
        /// <param name="displayName">Provider pretty name, use to display in UI.</param>
        public SearchProvider(string id, string displayName)
            : this(id, displayName, (Func<SearchContext, List<SearchItem>, SearchProvider, object>)null)
        {
        }

        /// <summary>
        /// Create a new SearchProvider
        /// </summary>
        /// <param name="id">Search Provider unique id.</param>
        /// <param name="fetchItemsHandler">Handler responsible to populate a list of SearchItems according to a query.</param>
        public SearchProvider(string id, Func<SearchContext, List<SearchItem>, SearchProvider, object> fetchItemsHandler)
            : this(id, null, fetchItemsHandler)
        {
        }

        /// <summary>
        /// Create a new SearchProvider
        /// </summary>
        /// <param name="id">Search Provider unique id.</param>
        /// <param name="fetchItemsHandler">Handler responsible to populate a list of SearchItems according to a query.</param>
        public SearchProvider(string id, Func<SearchContext, SearchProvider, object> fetchItemsHandler)
            : this(id, null, (context, items, provider) => fetchItemsHandler(context, provider))
        {
        }

        /// <summary>
        /// Create a new SearchProvider
        /// </summary>
        /// <param name="id">Search Provider unique id.</param>
        /// <param name="displayName">Provider pretty name, use to display in UI.</param>
        /// <param name="fetchItemsHandler">Handler responsible to populate a list of SearchItems according to a query.</param>
        public SearchProvider(string id, string displayName, Func<SearchContext, SearchProvider, object> fetchItemsHandler)
            : this(id, displayName, (context, items, provider) => fetchItemsHandler(context, provider))
        {
        }

        /// <summary>
        /// Create a new SearchProvider
        /// </summary>
        /// <param name="id">Search Provider unique id.</param>
        /// <param name="displayName">Provider pretty name, use to display in UI.</param>
        /// <param name="fetchItemsHandler">Handler responsible to populate a list of SearchItems according to a query.</param>
        public SearchProvider(string id, string displayName, Func<SearchContext, List<SearchItem>, SearchProvider, object> fetchItemsHandler)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("provider id must be non-empty", nameof(id));

            this.id = id;
            type = id;
            active = true;
            name = displayName ?? SearchUtils.ToPascalWithSpaces(id);
            actions = new List<SearchAction>();
            fetchItems = fetchItemsHandler ?? DefaultFetchItems;
            fetchThumbnail = DefaultFetchThumbnail;
            fetchPreview = null;
            fetchLabel = DefaultFetchLabel;
            fetchDescription = null;
            priority = 100;
            showDetails = false;
            showDetailsOptions = ShowDetailsOptions.Default;
            filterId = $"{id}:";

            defaultContext = new SearchContext(this);
        }

        internal SearchProvider(string id, string displayName, SearchProvider templateProvider, int groupPriority)
            : this(id, displayName, (Func<SearchContext, List<SearchItem>, SearchProvider, object>)null)
        {
            type = templateProvider.id;
            priority = groupPriority;
            isExplicitProvider = true;
            actions = templateProvider.actions;
            showDetails = templateProvider.showDetails;
            showDetailsOptions = templateProvider.showDetailsOptions;
            CopyFrom(templateProvider);
        }

        private void CopyFrom(SearchProvider templateProvider)
        {
            fetchItems = templateProvider.fetchItems;
            fetchLabel = templateProvider.fetchLabel;
            fetchDescription = templateProvider.fetchDescription;
            fetchPreview = templateProvider.fetchPreview;
            fetchThumbnail = templateProvider.fetchThumbnail;
            fetchColumns = templateProvider.fetchColumns;
            fetchPropositions = templateProvider.fetchPropositions;
            startDrag = templateProvider.startDrag;
            trackSelection = templateProvider.trackSelection;
            tableConfig = templateProvider.tableConfig;
            isEnabledForContextualSearch = templateProvider.isEnabledForContextualSearch;
            toObject = templateProvider.toObject;
            toKey = templateProvider.toKey;
            toType = templateProvider.toType;
            toInstanceId = templateProvider.toInstanceId;
            actions = templateProvider.actions;
        }

        private static object DefaultFetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider) => null;
        private string DefaultFetchLabel(SearchItem item, SearchContext context) => item.label ?? item.id ?? string.Empty;
        private static Texture2D DefaultFetchThumbnail(SearchItem item, SearchContext context) => Icons.quicksearch;

        /// <summary>
        /// Helper function to create a new search item for the current provider.
        /// </summary>
        /// <param name="id">Unique id of the search item. This is used to remove duplicates to the user view.</param>
        /// <param name="context">Search context from the query that generates this item.</param>
        /// <param name="score">Score of the search item. The score is used to sort all the result per provider. Lower score are shown first.</param>
        /// <param name="label">The search item label is displayed on the first line of the search item UI widget.</param>
        /// <param name="description">The search item description is displayed on the second line of the search item UI widget.</param>
        /// <param name="thumbnail">The search item thumbnail is displayed left to the item label and description as a preview.</param>
        /// <param name="data">User data used to recover more information about a search item. Generally used in fetchLabel, fetchDescription, etc.</param>
        /// <returns>The newly created search item attached to the current search provider.</returns>
        public SearchItem CreateItem(SearchContext context, string id, int score, string label, string description, Texture2D thumbnail, object data)
        {
            if (context?.options.HasAny(SearchFlags.Debug) ?? false)
            {
                // Debug sorting
                description = $"DEBUG: id={id} - label={label} - description={description} - thumbnail={thumbnail} - data={data}";
                label = $"{label ?? id} ({score})";
            }

            return new SearchItem(id)
            {
                score = score,
                label = label,
                description = description,
                options = SearchItemOptions.Highlight | SearchItemOptions.Ellipsis,
                thumbnail = thumbnail,
                data = data,
                provider = this,
                context = context
            };
        }

        /// <summary>
        /// Helper function to create a new search item for the current provider.
        /// </summary>
        /// <param name="id">Unique id of the search item. This is used to remove duplicates to the user view.</param>
        /// <param name="score">Score of the search item. The score is used to sort all the result per provider. Lower score are shown first.</param>
        /// <param name="label">The search item label is displayed on the first line of the search item UI widget.</param>
        /// <param name="description">The search item description is displayed on the second line of the search item UI widget.</param>
        /// <param name="thumbnail">The search item thumbnail is displayed left to the item label and description as a preview.</param>
        /// <param name="data">User data used to recover more information about a search item. Generally used in fetchLabel, fetchDescription, etc.</param>
        /// <returns>The newly created search item attached to the current search provider.</returns>
        [Obsolete("Use CreateItem which take a SearchContext", error: false)] // 2023.1
        public SearchItem CreateItem(string id, int score, string label, string description, Texture2D thumbnail, object data)
        {
            return CreateItem(defaultContext, id, score, label, description, thumbnail, data);
        }

        /// <summary>
        /// Helper function to create a new search item for the current provider.
        /// </summary>
        /// <param name="context">Search context from the query that generates this item.</param>
        /// <param name="id">Unique id of the search item. This is used to remove duplicates to the user view.</param>
        /// <returns>The newly created search item attached to the current search provider.</returns>
        public SearchItem CreateItem(SearchContext context, string id)
        {
            return CreateItem(context, id, 0, null, null, null, null);
        }

        /// <summary>
        /// Helper function to create a new search item for the current provider.
        /// </summary>
        /// <param name="id">Unique id of the search item. This is used to remove duplicates to the user view.</param>
        /// <returns>The newly created search item attached to the current search provider.</returns>
        [Obsolete("Use CreateItem which take a SearchContext", error: false)] // 2023.1
        public SearchItem CreateItem(string id)
        {
            return CreateItem(defaultContext, id, 0, null, null, null, null);
        }

        /// <summary>
        /// Helper function to create a new search item for the current provider.
        /// </summary>
        /// <param name="id">Unique id of the search item. This is used to remove duplicates to the user view.</param>
        /// <param name="label">The search item label is displayed on the first line of the search item UI widget.</param>
        /// <returns>The newly created search item attached to the current search provider.</returns>
        [Obsolete("Use CreateItem which take a SearchContext", error: false)] // 2023.1
        public SearchItem CreateItem(string id, string label)
        {
            return CreateItem(defaultContext, id, 0, label, null, null, null);
        }

        /// <summary>
        /// Helper function to create a new search item for the current provider.
        /// </summary>
        /// <param name="id">Unique id of the search item. This is used to remove duplicates to the user view.</param>
        /// <param name="label">The search item label is displayed on the first line of the search item UI widget.</param>
        /// <param name="description">The search item description is displayed on the second line of the search item UI widget.</param>
        /// <param name="thumbnail">The search item thumbnail is displayed left to the item label and description as a preview.</param>
        /// <param name="data">User data used to recover more information about a search item. Generally used in fetchLabel, fetchDescription, etc.</param>
        /// <returns>The newly created search item attached to the current search provider.</returns>
        public SearchItem CreateItem(string id, string label, string description, Texture2D thumbnail, object data)
        {
            return CreateItem(defaultContext, id, 0, label, description, thumbnail, data);
        }

        /// <summary>
        /// Create a Search item that will be bound to the SeaechProvider.
        /// </summary>
        /// <param name="context">Search context from the query that generates this item.</param>
        /// <param name="id">Unique id of the search item. This is used to remove duplicates to the user view.</param>
        /// <param name="label">The search item label is displayed on the first line of the search item UI widget.</param>
        /// <param name="description">The search item description is displayed on the second line of the search item UI widget.</param>
        /// <param name="thumbnail">The search item thumbnail is displayed left to the item label and description as a preview.</param>
        /// <param name="data">User data used to recover more information about a search item. Generally used in fetchLabel, fetchDescription, etc.</param>
        /// <returns>New SearchItem</returns>
        public SearchItem CreateItem(SearchContext context, string id, string label, string description, Texture2D thumbnail, object data)
        {
            return CreateItem(context, id, 0, label, description, thumbnail, data);
        }

        internal void RecordFetchTime(double t)
        {
            fetchTime  = t;
        }

        internal void OnEnable(double enableTimeMs)
        {
            try
            {
                if (m_EnableLockCounter == 0)
                    onEnable?.Invoke();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
            finally
            {
                enableTime = enableTimeMs;
                ++m_EnableLockCounter;
                ++sessionCounter;
            }
        }

        internal void OnDisable()
        {
            --m_EnableLockCounter;
            --sessionCounter;
            if (m_EnableLockCounter == 0)
            {
                try
                {
                    onDisable?.Invoke();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// Returns a short description of the SearchProvider.
        /// </summary>
        /// <returns>Returns a short description of the SearchProvider.</returns>
        public override string ToString()
        {
            return $"[{id}] {name} active={active}";
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (fetchItems != null) m_Serialized_fetchItems = new SearchFunctor<Func<SearchContext, List<SearchItem>, SearchProvider, object>>(fetchItems);
            if (fetchLabel != null) m_Serialized_fetchLabel = new SearchFunctor<Func<SearchItem, SearchContext, string>>(fetchLabel);
            if (fetchDescription != null) m_Serialized_fetchDescription = new SearchFunctor<Func<SearchItem, SearchContext, string>>(fetchDescription);
            if (fetchPreview != null) m_Serialized_fetchPreview = new SearchFunctor<Func<SearchItem, SearchContext, Vector2, FetchPreviewOptions, Texture2D>>(fetchPreview);
            if (fetchThumbnail != null) m_Serialized_fetchThumbnail = new SearchFunctor<Func<SearchItem, SearchContext, Texture2D>>(fetchThumbnail);
            if (fetchColumns != null) m_Serialized_fetchColumns = new SearchFunctor<Func<SearchContext, IEnumerable<SearchItem>, IEnumerable<SearchColumn>>>(fetchColumns);
            if (fetchPropositions != null) m_Serialized_fetchPropositions = new SearchFunctor<Func<SearchContext, SearchPropositionOptions, IEnumerable<SearchProposition>>>(fetchPropositions);
            if (startDrag != null) m_Serialized_startDrag = new SearchFunctor<Action<SearchItem, SearchContext>>(startDrag);
            if (trackSelection != null) m_Serialized_trackSelection = new SearchFunctor<Action<SearchItem, SearchContext>>(trackSelection);
            if (isEnabledForContextualSearch != null) m_Serialized_isEnabledForContextualSearch = new SearchFunctor<Func<bool>>(isEnabledForContextualSearch);
            if (m_Serialized_toObject != null) m_Serialized_toObject = new SearchFunctor<Func<SearchItem, Type, UnityEngine.Object>>(toObject);
            if (m_Serialized_onEnable != null) m_Serialized_onEnable = new SearchFunctor<Action>(onEnable);
            if (m_Serialized_onDisable != null) m_Serialized_onDisable = new SearchFunctor<Action>(onDisable);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            defaultContext = new SearchContext(this);
            fetchItems ??= m_Serialized_fetchItems.handler;
            fetchLabel ??= m_Serialized_fetchLabel.handler;
            fetchDescription ??= m_Serialized_fetchDescription.handler;
            fetchPreview ??= m_Serialized_fetchPreview.handler;
            fetchThumbnail ??= m_Serialized_fetchThumbnail.handler;
            fetchColumns ??= m_Serialized_fetchColumns.handler;
            fetchPropositions ??= m_Serialized_fetchPropositions.handler;
            startDrag ??= m_Serialized_startDrag.handler;
            trackSelection ??= m_Serialized_trackSelection.handler;
            tableConfig ??= m_Serialized_tableConfig.handler;
            isEnabledForContextualSearch ??= m_Serialized_isEnabledForContextualSearch.handler;
            toObject ??= m_Serialized_toObject.handler;
            toKey ??= m_Serialized_toKey.handler;
            toType ??= m_Serialized_toType.handler;
            toInstanceId ??= m_Serialized_toInstanceId.handler;
            onEnable ??= m_Serialized_onEnable.handler;
            onDisable ??= m_Serialized_onDisable.handler;
        }
    }
}
