// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define QUICK_SEARCH_STORE
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Text;
using System.Linq;
using Debug = UnityEngine.Debug;
using UnityEditor.Connect;
using System.Globalization;

namespace UnityEditor.Search.Providers
{
    static class AssetStoreProvider
    {
#pragma warning disable CS0649
        class AssetDocumentWrapper : UnityEngine.ScriptableObject
        {
            public SearchItem item;
        }

        [CustomEditor(typeof(AssetDocumentWrapper))]
        class AssetDocumentWrapperEditor : UnityEditor.Editor
        {

            static class Styles
            {
                static Styles()
                {
                    SetupMargin(label);
                    SetupMargin(nameLabel);
                    SetupMargin(linkButton);
                    SetupMargin(separator);
                    SetupMargin(button);
                }

                public static GUIStyle label = new GUIStyle("label")
                {
                    richText = true,
                    wordWrap = true,
                };

                public static GUIStyle nameLabel = new GUIStyle(label)
                {
                    fontSize = label.fontSize + 2
                };

                public static GUIStyle separator = new GUIStyle("AnimLeftPaneSeparator");

                public static GUIStyle linkButton = new GUIStyle(EditorStyles.linkLabel);
                public static GUIStyle button = new GUIStyle(GUI.skin.button);

                public static GUIContent viewMyAsset = new GUIContent(L10n.Tr("View in My Assets"));
                public static GUIContent addToMyAsset = new GUIContent(L10n.Tr("Add to My Assets"));
                public static GUIContent viewOnWeb = new GUIContent(L10n.Tr("View on Web"));
                public static GUIContent eula = new GUIContent(L10n.Tr("Standard Unity Asset Store EULA"));

                static void SetupMargin(GUIStyle style)
                {
                    style.margin = new RectOffset(8, 8, style.margin.top, style.margin.bottom);
                }
            }


            private void OnEnable()
            {
            }

            public override bool UseDefaultMargins()
            {
                return false;
            }

            public override void OnInspectorGUI()
            {
                var wrapper = (AssetDocumentWrapper)target;
                var item = wrapper.item;
                var desc = (AssetDocument)item.data;
                if (desc != null)
                {
                    GUILayout.Label($"<b>{desc.name_en_US}</b>", Styles.nameLabel);
                    GUILayout.Label($"by {desc.publisher}", Styles.label);
                    var isOwned = IsItemOwned(item);
                    var price = GetPrice(desc);
                    string priceStr;
                    if (isOwned)
                        priceStr = "Owned";
                    else if (price == 0)
                        priceStr = "Free";
                    else
                        priceStr = $"{currencySymbol}{price} ({currency})";
                    GUILayout.Label(priceStr, Styles.label);
                    var lastRect = GUILayoutUtility.GetLastRect();
                    if (desc.productDetail != null && !string.IsNullOrEmpty(desc.productDetail.elevatorPitch))
                        GUILayout.Label(desc.productDetail.elevatorPitch, Styles.label);

                    GUILayout.Space(10);
                    var sepRect = GUILayoutUtility.GetRect(lastRect.width, lastRect.height);
                    if (Event.current.type == EventType.Repaint)
                    {
                        Styles.separator.Draw(sepRect, false, false, false, false);
                    }
                    GUILayout.Space(15);

                    if (isOwned)
                    {
                        if (GUILayout.Button(Styles.viewMyAsset, Styles.button))
                            OpenPackageManager(item);
                    }
                    if (GUILayout.Button(Styles.viewOnWeb, Styles.button))
                    {
                        BrowseAssetStoreItem(item);
                    }

                    if (GUILayout.Button(Styles.eula, Styles.linkButton))
                    {
                        Application.OpenURL("https://unity.com/legal/as-terms");
                    }
                }
                else
                {
                    EditorGUILayout.LabelField($"Name", item.label);
                }

            }
        }

        [Serializable]
        class StoreSearchRequest
        {
            public string q;
        }

        [Serializable]
        class StoreSearchResponseHeader
        {
            public int status;
            public int QTime;
        }

        [Serializable]
        class ErrorObject
        {
            public string msg;
        }

        [Serializable]
        class StoreSearchResponseObject
        {
            public int numFound;
            public int numInserted;
            public int start;
            public AssetDocument[] docs;
        }

        [Serializable]
        class AssetDocument
        {
            public string id;
            public string name_en_US;
            public float price_USD;
            public float price_EUR;
            public string publisher;
            public string[] icon;
            public string url;
            public string category_slug;
            public string type;
            // public string name_ja-JP;
            // public string name_ko-KR;
            // public string name_zh-CN;
            public int avg_rating;
            // public int ratings;
            // public int publisher_id;
            public string on_sale;
            // public string plus_pro;
            public string[] key_images;
            // public float original_price_USD;
            // public float original_price_EUR;
            public int age;
            // public string new;
            // public string partner;

            // Cache for the PurchaseInfo API
            public ProductDetails productDetail;
            public string[] images;

            public Texture2D lastPreview { get; set; }
        }

        [Serializable]
        class StoreSearchResponse
        {
            public StoreSearchResponseHeader responseHeader;
            public StoreSearchResponseObject response;
            public ErrorObject error;
        }

        [Serializable]
        class AccessToken
        {
            public string access_token;
            public string expires_in;

            public long expiration;
            public double expirationStarts;

            // public string token_type;
            // public string refresh_token;
            // public string user;
            // public string display_name;
        }

        [Serializable]
        class TokenInfo
        {
            public string sub;
            public string access_token;
            public string expires_in;

            public long expiration;
            public double expirationStarts;
            // public string scopes;
            // public string client_id;
            // public string ip_address;
        }

        [Serializable]
        class UserInfoName
        {
            public string fillName;
        }

        [Serializable]
        class UserInfo
        {
            public string id;
            public string username;
            public UserInfoName name;
        }

        [Serializable]
        class PurchaseInfo
        {
            public int packageId;
        }

        [Serializable]
        class PurchaseResponse
        {
            public int total;
            public PurchaseInfo[] results;
        }

        [Serializable]
        class PurchaseDetailCategory
        {
            public string id;
            public string name;
            public string slug;
        }

        [Serializable]
        class PurchaseDetailMainImage
        {
            public string big;
            public string icon;
            public string icon25;
            public string icon75;
            public string small;
            public string url;
            public string facebook;

            public override string ToString()
            {
                return $"{big} - {icon} - {small}";
            }
        }

        [Serializable]
        class PurchaseDetail
        {
            public string packageId;
            public string ownerId;
            public string name;
            public string displayName;
            public string publisherId;
            public PurchaseDetailCategory category;
            public PurchaseDetailMainImage mainImage;
        }

        [Serializable]
        class ProductListResponse
        {
            public ProductDetails[] results;
        }

        [Serializable]
        class ImageDesc
        {
            public int height;
            public int width;
            public string imageUrl;
            public string thumbnailUrl;
            public string type;

            public override string ToString()
            {
                return $"{type} - ({width},{height})";
            }
        }

        [Serializable]
        class ProductDetails
        {
            // public string id;
            // public string packageId;
            // public string slug;
            public string displayName;
            public string description;
            public string elevatorPitch;
            public string keyFeatures;
            public PurchaseDetailMainImage mainImage;
            public ImageDesc[] images;

            public override string ToString()
            {
                return displayName;
            }
        }

        const string k_OpenBrowserFromToolbar = "unity-editor-toolbar";
        const string k_OpenBrowserFromSearch = "unity-editor-search";

        struct QueryValue
        {
            public QueryValue(string displayName, string queryToken = null, object value = null, object value2 = null)
            {
                this.displayName = displayName;
                if (queryToken != null)
                {
                    this.queryToken = queryToken.ToLowerInvariant();
                }
                else
                {
                    this.queryToken = displayName.Replace(" ", "-").ToLowerInvariant();
                }
                this.value = value ?? this.queryToken;
                this.value2 = value2;
            }

            public string displayName;
            public string queryToken;
            public object value;
            public object value2;

            public override string ToString()
            {
                return $"{displayName} - {queryToken}";
            }
        }

        class PreviewData
        {
            public Texture2D preview;
            public UnityWebRequest request;
            public UnityWebRequestAsyncOperation requestOp;
        }

        class AssetsLoadingPage : IComparable<AssetsLoadingPage>
        {
            public AssetsLoadingPage(int startIndex, int endIndex)
            {
                this.startIndex = startIndex;
                this.endIndex = endIndex;
                items = new List<SearchItem>(size);
            }

            public List<SearchItem> items;
            public int startIndex;
            public int endIndex;
            public int size => endIndex - startIndex;
            public UnityWebRequest request;
            public bool isLoaded;

            public int CompareTo(AssetsLoadingPage other)
            {
                return startIndex.CompareTo(other.startIndex);
            }
        }

        class QueryDescriptor
        {
            public QueryDescriptor()
            {
                max_price = min_price = 0;
                publisher = "";
                unity_version = "2022";
                free = false;
                on_sale = false;
                category = "";
                min_rating = 0;
                release = ReleaseDate.OneYearAgo;
                platform = "ios";
                sort = SortOrder.Relevance;
                currency = 0; // 1: Euro
            }
            public float min_price;
            public float max_price;
            public string publisher;
            public string unity_version;
            public bool free;
            public bool on_sale;
            public string category;
            public int min_rating;
            public int currency;
            public ReleaseDate release;
            public string platform;
            public SortOrder sort;
        }
#pragma warning restore CS0649

        private const string kSearchEndPoint = "https://assetstore.unity.com/api/search";
        private const string kProductDetailsEndPoint = "https://api.unity.com/v1/products/list";
        private static readonly Dictionary<string, PreviewData> s_Previews = new Dictionary<string, PreviewData>();
        private static bool s_RequestCheckPurchases;
        private static bool s_StartPurchaseRequest;
        private static readonly List<PurchaseInfo> s_Purchases = new List<PurchaseInfo>();
        private static HashSet<string> purchasePackageIds;
        private static string s_PackagesKey;
        private static string s_AuthCode;
        private static AccessToken s_AccessTokenData;
        private static TokenInfo s_TokenInfo;
        private static UserInfo s_UserInfo;
        const string k_MultiValueQueryParameter = "multivalue";
        private static QueryEngine<QueryDescriptor> s_QueryEngine;
        private static List<AssetsLoadingPage> s_AssetsPage;
        private static Dictionary<string, object> s_QueryParams;
        private static ISearchView s_CurrentSearchView;
        const int kAssetsPerPage = 100;

        private static readonly HashSet<string> k_EuroCountries = new HashSet<string>(new[] { "AT", "BE", "ES", "FI", "FR", "DE", "GR", "IE", "IT", "LV", "LT", "LU", "MT", "NL", "PT", "SK", "SI", "ES", "BG", "HR", "CZ", "HU", "PL", "RO", "SE" });

        static QueryValue[] k_Categories =
        {
            new QueryValue("3D/All 3D", "3d"),
            new QueryValue("3D/Animations"),
            new QueryValue("3D/Characters"),
            new QueryValue("3D/Environments"),
            new QueryValue("3D/GUI"),
            new QueryValue("3D/Props"),
            new QueryValue("3D/Vegetation"),
            new QueryValue("3D/Vehicules"),
            new QueryValue("2D/All 2D", "2d"),
            new QueryValue("2D/Characters"),
            new QueryValue("2D/Environments"),
            new QueryValue("2D/Fonts"),
            new QueryValue("2D/GUI"),
            new QueryValue("2D/Textures & materials", "2d/textures-materials"),
            new QueryValue("Add Ons/All Add Ons", "Add Ons"),
            new QueryValue("Add Ons/Machine Learning", "add-ons/machinelearning"),
            new QueryValue("Add Ons/Services"),
            new QueryValue("Audio/All Audio", "Audio"),
            new QueryValue("Audio/Ambient"),
            new QueryValue("Audio/Music"),
            new QueryValue("Audio/Sound FX"),
            new QueryValue("Essentials/All Essentials", "Essentials"),
            new QueryValue("Essentials/Asset Packs"),
            new QueryValue("Essentials/Tutorial Projects"),
            new QueryValue("Templates"),
            new QueryValue("Tools"),
            new QueryValue("Vfx"),
        };

        [QueryListBlock("Category", "Category", "category", "=", 1)]
        class QueryCategoryBlock : QueryListBlock
        {
            public QueryCategoryBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
                 : base(source, id, value, attr)
            {
                var labelValue = k_Categories.FirstOrDefault(c => (string)c.value == value);
                if (labelValue.displayName != null)
                    label = labelValue.displayName.Split("/").Last();
            }

            public override void Apply(in SearchProposition searchProposition)
            {
                label = searchProposition.label.Split("/").Last();
                base.Apply(searchProposition);
            }

            public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
            {
                foreach (var cat in k_Categories)
                {
                    yield return CreateProposition(flags, cat.displayName, cat.queryToken, $"Assets in category: {cat.displayName}");
                }
            }
        }

        static QueryValue[] k_UnityVersions =
        {
            new QueryValue("2022"),
            new QueryValue("2021"),
            new QueryValue("2020"),
            new QueryValue("2019"),
            new QueryValue("2018"),
            new QueryValue("2017"),
            new QueryValue("Unity 5.x", "5"),
        };

        [QueryListBlock("Unity Version", "Unity Version", "unity_version", "=", 1)]
        class QueryVersionBlock : QueryListBlock
        {
            public QueryVersionBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
                 : base(source, id, value, attr)
            {
            }

            public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
            {
                foreach (var cat in k_UnityVersions)
                {
                    yield return CreateProposition(flags, cat.displayName, cat.queryToken, $"Assets for Unity version: {cat.displayName}");
                }
            }
        }

        static QueryValue[] k_Platforms =
        {
            new QueryValue("Windows", "standalonewindows64"),
            new QueryValue("Mac OS X", "standaloneosxuniversal"),
            new QueryValue("Linux", "standalonelinuxuniversal"),
            new QueryValue("iOS"),
            new QueryValue("Android"),
            new QueryValue("Web GL", "webgl")
        };

        [QueryListBlock("Platform", "Platform", "platform", "=", 1)]
        class QueryPlatformBlock : QueryListBlock
        {
            public QueryPlatformBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
                 : base(source, id, value, attr)
            {
            }

            public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
            {
                foreach (var cat in k_Platforms)
                {
                    yield return CreateProposition(flags, cat.displayName, cat.queryToken, $"Assets for Platform: {cat.displayName}");
                }
            }
        }

        enum ReleaseDate
        {
            OneDayAgo = 1,
            OneWeekAgo = 7,
            OneMonthAgo = 31,
            SixMonthsAgo = 180,
            OneYearAgo = 365
        }

        [QueryListBlock("Release", "Release since (days)", "release", "=", 1)]
        class QueryReleaseBlock : QueryListBlock
        {
            public QueryReleaseBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
                 : base(source, id, value, attr)
            {
            }

            public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
            {
                return GetEnumPropositions<ReleaseDate>(flags, "Find Asset by Release day:");
            }
        }

        enum SortOrder
        {
            Relevance = 0,
            Popularity = 1,
            Name = 2,
            Price = 4,
            Rating = 5
        }

        [QueryListBlock("Sorting order", "Sorting order", "sort", "=", 1)]
        class QuerySortOrderBlock : QueryListBlock
        {
            public QuerySortOrderBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
                 : base(source, id, value, attr)
            {
            }

            public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
            {
                return GetEnumPropositions<SortOrder>(flags, "Set item sorting by:");
            }
        }


        static QueryValue[] k_MinRating =
        {
            new QueryValue("5", null, 5),
            new QueryValue("4", null, 4),
            new QueryValue("3", null, 3),
            new QueryValue("2", null, 2),
            new QueryValue("1", null, 1),
        };

        [QueryListBlock("Star Rating", "Star Rating", "min_rating", "=", 5)]
        class QueryMinRatingBlock : QueryListBlock
        {
            public QueryMinRatingBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
                 : base(source, id, value, attr)
            {
            }

            public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
            {
                foreach (var cat in k_MinRating)
                {
                    yield return CreateProposition(flags, cat.displayName, cat.value, $"Minimum star rating for assets: {cat.displayName}");
                }
            }
        }

        static QueryValue[] k_PriceRange =
        {
            new QueryValue($"Free", null, 0),
            new QueryValue($"1-5 {currencySymbol}", null, 5, 1),
            new QueryValue($"6-10 {currencySymbol}", null, 10, 6),
            new QueryValue($"11-20 {currencySymbol}", null, 20, 11),
            new QueryValue($"21-50 {currencySymbol}", null, 50, 21),
            new QueryValue($"51-100 {currencySymbol}", null, 100, 51),
            new QueryValue($"101-250 {currencySymbol}", null, 250, 101),
            new QueryValue($"251-500 {currencySymbol}", null, 500, 251),
            new QueryValue("No limit", null, 100000, 1),
        };

        [QueryListBlock("Price Range", "Price Range", "price", "=", 1)]
        class QueryPriceBlock : QueryListBlock
        {
            public QueryPriceBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
                 : base(source, id, value, attr)
            {
                int valueInt = Convert.ToInt32(value);
                var labelValue = k_PriceRange.FirstOrDefault(c => (int)c.value == valueInt);
                if (labelValue.displayName != null)
                    label = labelValue.displayName;
            }

            public override void Apply(in SearchProposition searchProposition)
            {
                label = searchProposition.label;
                base.Apply(searchProposition);
            }

            public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
            {
                var category = flags.HasAny(SearchPropositionFlags.NoCategory) ? null : $"Price Range ({currencySymbol})";
                int priority = 0;
                foreach (var p in k_PriceRange)
                {
                    yield return new SearchProposition(category: category, label: p.displayName, help: $"Price range ({currencySymbol}) for assets: {p.displayName}",
                                data: p.value, priority: priority++, icon: icon, type: GetType(), color: GetBackgroundColor());
                }
            }
        }

        enum Currency
        {
            None,
            Euro,
            USD
        }

        static Currency s_Currency;
        private static Currency currency
        {
            get
            {
                if (s_Currency == Currency.None)
                {
                    var c = CultureInfo.CurrentUICulture;
                    s_Currency = Currency.USD;
                    if (k_EuroCountries.Contains(c.TwoLetterISOLanguageName))
                    {
                        s_Currency = Currency.Euro;
                    }
                }
                return s_Currency;
            }
        }

        private static string currencySymbol => currency == Currency.USD ? "$" : "€";

        private static IEnumerable<SearchItem> SearchStore(SearchContext context, SearchProvider provider)
        {
            ClearSearchSession();
            if (s_RequestCheckPurchases)
                CheckPurchases(null);

            if (string.IsNullOrEmpty(context.searchQuery))
                yield break;

            var query = s_QueryEngine.ParseQuery(context.searchQuery);
            if (!query.valid)
            {
                Debug.LogError(string.Join(" ", query.errors.Select(e => e.reason)));
                yield break;
            }

            s_CurrentSearchView = context.searchView;

            var requestStr = FormatQuery(query, context, 0);
            using (var webRequest = Post(kSearchEndPoint, requestStr))
            {
                var rao = webRequest.SendWebRequest();
                while (!rao.isDone)
                    yield return null;

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    // Debug.LogError($"Asset store request error: {webRequest.error}");
                }
                else
                {
                    StoreSearchResponse response;
                    // using (new DebugTimer("Parse response"))
                    {
                        var saneJsonStr = webRequest.downloadHandler.text.Replace("name_en-US\"", "name_en_US\"");
                        response = JsonUtility.FromJson<StoreSearchResponse>(saneJsonStr);
                    }

                    if (response.responseHeader.status != 0)
                    {
                        if (response.error != null)
                            Debug.LogError($"Error: {response.error.msg}");
                    }
                    else
                    {
                        
                        // Use score Index as an unofficial ids for items:
                        var scoreIndex = 0;
                        foreach (var doc in response.response.docs)
                        {
                            yield return CreateItem(context, provider, doc, scoreIndex++);
                        }

                        // Create pages:
                        var totalNbItems = response.response.numFound;
                        var nbPages = (int)(totalNbItems / kAssetsPerPage) + 1;
                        if (s_AssetsPage.Capacity < nbPages)
                            s_AssetsPage.Capacity = nbPages;

                        var startIndex = scoreIndex;
                        for(var pageIndex = 0; pageIndex < nbPages; ++pageIndex)
                        {
                            var endIndex = startIndex + kAssetsPerPage;
                            var page = new AssetsLoadingPage(startIndex, endIndex);
                            s_AssetsPage.Add(page);
                            for(var itemIndex = startIndex; itemIndex < endIndex && itemIndex < totalNbItems; ++itemIndex)
                            {
                                var id = $"store_item_{itemIndex}";
                                var item = provider.CreateItem(context, id, itemIndex, label: null, description: null, Icons.store, null);
                                page.items.Add(item);
                                yield return item;
                            }
                            startIndex = endIndex;
                        }
                    }
                }
            }
        }

        static void ClearSearchSession()
        {
            s_AssetsPage?.Clear();
            s_QueryParams?.Clear();
            s_CurrentSearchView = null;
        }

        static void GetQueryParts(IQueryNode n, List<IFilterNode> filters, List<ISearchNode> searches)
        {
            if (n == null)
                return;
            if (n is IFilterNode filterNode)
            {
                filters.Add(filterNode);
            }
            else if (n is ISearchNode searchNode)
            {
                searches.Add(searchNode);
            }

            if (n.children != null)
            {
                foreach (var child in n.children)
                {
                    GetQueryParts(child, filters, searches);
                }
            }
        }

        static string FormatQuery(ParsedQuery<QueryDescriptor> query, SearchContext context, int startIndex)
        {
            List<IFilterNode> filters = new();
            List<ISearchNode> searches = new();
            GetQueryParts(query.queryGraph.root, filters, searches);

            var searchStr = string.Join(" ", searches.Select(s => s.searchValue)).Trim();
            s_QueryParams = new Dictionary<string, object>()
            {
                { "q", searchStr },
                { "rows", kAssetsPerPage },
                { "currency", currency == Currency.USD ? 0 : 1 }
            };

            SetQueryStartIndex(s_QueryParams, startIndex);

            foreach (var f in filters)
            {
                var filter = s_QueryEngine.GetFilter(f.filterId);
                if (filter == null)
                    continue;

                if (f.filterId == "price")
                {
                    var queryvalue = k_PriceRange.FirstOrDefault(qv => qv.value.ToString() == f.filterValue);
                    if (queryvalue.value == null)
                        continue;
                    s_QueryParams.Add("max_price", queryvalue.value);
                    if (queryvalue.value2 != null)
                        s_QueryParams.Add("min_price", queryvalue.value2);
                }
                if (filter.metaInfo.ContainsKey(k_MultiValueQueryParameter))
                {
                    if (!s_QueryParams.TryGetValue(f.filterId, out var values))
                    {
                        values = new List<string>();
                        s_QueryParams.TryAdd(f.filterId, values);
                    }
                    (values as List<string>).Add(f.filterValue);
                }
                else if (filter.type == typeof(bool))
                {
                    s_QueryParams.TryAdd(f.filterId, Convert.ToBoolean(f.filterValue));
                }
                else if (filter.type == typeof(float))
                {
                    s_QueryParams.TryAdd(f.filterId, Convert.ToSingle(f.filterValue));
                }
                else if (filter.type == typeof(int))
                {
                    s_QueryParams.TryAdd(f.filterId, Convert.ToInt32(f.filterValue));
                }
                else if (filter.type.IsEnum)
                {
                    foreach(var e in Enum.GetValues(filter.type))
                    {
                        if (e.ToString().ToLowerInvariant() == f.filterValue.ToLowerInvariant())
                        {
                            s_QueryParams.TryAdd(f.filterId, (int)e);
                            break;
                        }
                    }
                }
                else
                {
                    s_QueryParams.TryAdd(f.filterId, f.filterValue);
                }
            }

            if (!s_QueryParams.ContainsKey("sort"))
            {
                s_QueryParams.TryAdd("sort", 0);
            }

            var requestStr = Json.Serialize(s_QueryParams, true);
            // Debug.Log($"query: {requestStr}");
            return requestStr;
        }

        static void SetQueryStartIndex(Dictionary<string, object> queryParams, int startIndex)
        {
            queryParams["start"] = startIndex;
        }

        static SearchItem CreateItem(SearchContext context, SearchProvider provider, AssetDocument doc, int score)
        {
            var item = provider.CreateItem(context, doc.id);
            item.score = score;
            UpdateItem(item, doc);
            return item;
        }

        static void UpdateItem(SearchItem item, AssetDocument doc)
        {
            item.data = doc;
            item.options &= ~SearchItemOptions.FuzzyHighlight;
            item.options &= ~SearchItemOptions.Highlight;
            doc.productDetail = null;
            doc.url = $"https://assetstore.unity.com/packages/{doc.category_slug}/{doc.id}";
        }

        static UnityWebRequest Post(string url, string jsonData)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        static void OnEnable()
        {
            s_QueryEngine = new();
            s_QueryEngine.SetSearchDataCallback(data =>
            {
                return new[] { "dummy" };
            });

            s_QueryEngine.SetFilter("free", data => data.free, new[] { "=" })
                .AddOrUpdatePropositionData(category: null, label: "Free", replacement: "free=true", help: "Search for free asset");
            s_QueryEngine.SetFilter("on_sale", data => data.on_sale, new[] { "=" })
                .AddOrUpdatePropositionData(category: null, label: "On Sale", replacement: "on_sale=true", help: "Search for free asset");
            s_QueryEngine.SetFilter("publisher", data => data.publisher, new[] { "=", ":" })
                .AddOrUpdatePropositionData(category: null, label: "Publisher", replacement: "publisher=Unity", help: "Search Assets from Publisher")
                .AddOrUpdateMetaInfo(k_MultiValueQueryParameter, k_MultiValueQueryParameter);

            // ListBlock
            s_QueryEngine.SetFilter("category", data => data.category, new[] { "=" })
                .AddOrUpdateMetaInfo(k_MultiValueQueryParameter, k_MultiValueQueryParameter);
            s_QueryEngine.SetFilter("unity_version", data => data.unity_version, new[] { "=" });
            s_QueryEngine.SetFilter("platform", data => data.platform, new[] { "=" });
            s_QueryEngine.SetFilter("release", data => data.release, new[] { "=" });
            s_QueryEngine.SetFilter("sort", data => data.sort, new[] { "=" });
            s_QueryEngine.SetFilter("price", data => data.max_price, new[] { "=" });
            s_QueryEngine.SetFilter("min_rating", data => data.min_rating, new[] { "=" });

            s_RequestCheckPurchases = true;

            s_AssetsPage = new();

            UnityConnect.instance.UserStateChanged -= OnUserStateChanged;
            UnityConnect.instance.UserStateChanged += OnUserStateChanged;
        }

        static void OnDisable()
        {
            ClearSearchSession();
            UnityConnect.instance.UserStateChanged -= OnUserStateChanged;
        }

        private static void OnUserStateChanged(Connect.UserInfo state)
        {
            ClearUserInfo();
            CheckPurchases(() => Refresh());
        }

        static bool HasAccessToken()
        {
            return !string.IsNullOrEmpty(Utils.GetConnectAccessToken());
        }

        static void CheckPurchases(Action done)
        {
            if (!HasAccessToken())
            {
                done?.Invoke();
                return;
            }

            if (string.IsNullOrEmpty(s_PackagesKey))
                s_PackagesKey = Utils.GetPackagesKey();
            if (string.IsNullOrEmpty(s_PackagesKey))
            {
                done?.Invoke();
                return;
            }

            s_RequestCheckPurchases = false;
            if (s_StartPurchaseRequest)
            {
                done?.Invoke();
                return;
            }

            s_StartPurchaseRequest = true;
            var startRequest = System.Diagnostics.Stopwatch.StartNew();
            GetAllPurchases((purchases, error) =>
            {
                s_StartPurchaseRequest = false;
                if (error != null)
                {
                    Debug.LogError($"Error in fetching user purchases: {error}");
                    done?.Invoke();
                    return;
                }
                startRequest.Stop();

                purchasePackageIds = new HashSet<string>();
                foreach (var purchaseInfo in purchases)
                {
                    purchasePackageIds.Add(purchaseInfo.packageId.ToString());
                }

                s_RequestCheckPurchases = false;
                done?.Invoke();
            }, () =>
            {
                startRequest.Stop();
                s_StartPurchaseRequest = false;
                s_RequestCheckPurchases = true;
            });
        }

        const string k_ProviderId = "store";
        const string k_FilterId = "store:";

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(k_ProviderId, "Asset Store")
            {
                active = true,
                isExplicitProvider = true,
                filterId = k_FilterId,
                onEnable = OnEnable,
                onDisable = OnDisable,
                fetchItems = (context, items, provider) => SearchStore(context, provider),
                fetchThumbnail = (item, context) => FetchIcons(item, animateCarrousel: false, s_Previews) ?? Icons.store,
                fetchPropositions = (context, options) => FetchPropositions(context, options),
                fetchPreview = FetchPreview,
                fetchDescription = FetchDescription,
                fetchLabel = FetchLabel,
                showDetails = true,
                trackSelection = OnTrackSelection,
                showDetailsOptions = ShowDetailsOptions.Preview | ShowDetailsOptions.Inspector | ShowDetailsOptions.InspectorWithoutHeader,
                toObject = (item, type) =>
                {            
                    var wrapper = AssetDocumentWrapper.CreateInstance<AssetDocumentWrapper>();
                    wrapper.item = item;
                    return wrapper;
                },
            
                tableConfig = GetDefaultTableConfig
            };
        }

        [SearchTemplate(description = "Find all free assets", providerId = k_ProviderId)] internal static string ST1() => @"free=true";
        [SearchTemplate(description = "Find on sale assets", providerId = k_ProviderId)] internal static string ST2() => @"on_sale=true";
        [SearchTemplate(description = "Find 3D assets", providerId = k_ProviderId)] internal static string ST3() => @"category=3d";
        [SearchTemplate(description = "Find 2D assets", providerId = k_ProviderId)] internal static string ST4() => @"category=2d";

        private static IEnumerable<SearchProposition> FetchPropositions(SearchContext ctx, SearchPropositionOptions flags)
        {
            foreach (var p in QueryListBlockAttribute.GetPropositions(typeof(QueryCategoryBlock)))
                yield return p;

            foreach (var p in QueryListBlockAttribute.GetPropositions(typeof(QueryVersionBlock)))
                yield return p;

            foreach (var p in QueryListBlockAttribute.GetPropositions(typeof(QueryPlatformBlock)))
                yield return p;

            foreach (var p in QueryListBlockAttribute.GetPropositions(typeof(QueryReleaseBlock)))
                yield return p;

            foreach (var p in QueryListBlockAttribute.GetPropositions(typeof(QuerySortOrderBlock)))
                yield return p;

            foreach (var p in QueryListBlockAttribute.GetPropositions(typeof(QueryMinRatingBlock)))
                yield return p;

            foreach (var p in QueryListBlockAttribute.GetPropositions(typeof(QueryPriceBlock)))
                yield return p;

            foreach (var p in s_QueryEngine.GetPropositions())
                yield return p;
        }

        [SearchColumnProvider(nameof(AssetDocument))]
        internal static void AssetDocumentColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var data = args.item.data as AssetDocument;
                if (data != null)
                {
                    switch (column.selector)
                    {
                        case "id": return data.id;
                        case "publisher": return data.publisher;
                        case "category": return data.category_slug;
                        case "avg_rating": return data.avg_rating;
                        case "on_sale": return data.on_sale;
                        case "price": return GetPrice(data);
                        case "free": return IsFree(data);
                        case "age": return data.age;
                    }
                }
                return null;
            };
        }

        static float GetPrice(AssetDocument doc)
        {
            return currency == Currency.USD ? doc.price_USD : doc.price_EUR;
        }

        static bool IsFree(AssetDocument doc)
        {
            return doc != null && GetPrice(doc) == 0;
        }

        static void OnTrackSelection(SearchItem item, SearchContext context)
        {
            var win = context.searchView as SearchWindow;
            if (win != null && !win.state.flags.HasFlag(UnityEngine.Search.SearchViewFlags.OpenInspectorPreview))
            {
                win.TogglePanelView(UnityEngine.Search.SearchViewFlags.OpenInspectorPreview);
            }
        }

        static IEnumerable<SearchColumn> FetchColumns(SearchContext context, IEnumerable<SearchItem> items)
        {
            yield return new SearchColumn("Asset Store/Publisher", "publisher", nameof(AssetDocument));
            yield return new SearchColumn("Asset Store/Category", "category", nameof(AssetDocument));
            yield return new SearchColumn("Asset Store/Rating", "avg_rating", nameof(AssetDocument));
            yield return new SearchColumn("Asset Store/Price", "price", nameof(AssetDocument));
            yield return new SearchColumn("Asset Store/On Sale", "on_sale", nameof(AssetDocument));
            yield return new SearchColumn("Asset Store/Age (in days)", "age", nameof(AssetDocument));
            yield return new SearchColumn("Asset Store/Id", "id", nameof(AssetDocument));
        }

        static SearchTable GetDefaultTableConfig(SearchContext context)
        {
            return new SearchTable(k_ProviderId, new[] { new SearchColumn("Name", "label") }.Concat(FetchColumns(context, null)));
        }

        static string FetchDescription(SearchItem item, SearchContext context)
        {
            if (item.data == null)
                return null;
            var doc = (AssetDocument)item.data;
            return $"{doc.name_en_US} - {doc.publisher} - {doc.category_slug}";
        }

        static string FetchLabel(SearchItem item, SearchContext context)
        {
            if (item.data == null)
            {
                // Found page for asset:
                var page = s_AssetsPage.FirstOrDefault(p => p.startIndex <= item.score && item.score < p.endIndex);
                if (page == null)
                {
                    // Can this happen?
                    Debug.LogError($"Cannot find page to load for item: {item.score} {item.id}");
                    return null;
                }
                LoadPage(page, item);
                return null;
            }

            var doc = (AssetDocument)item.data;
            var label = doc.name_en_US;
            if (purchasePackageIds != null && purchasePackageIds.Contains(doc.id))
            {
                label += " (Owned)";
            }
            else if (IsFree(doc))
            {
                label += " (Free)";
            }

            return label;
        }

        static Texture2D FetchPreview(SearchItem item, SearchContext context, Vector2 size, FetchPreviewOptions options)
        {
            if (!options.HasFlag(FetchPreviewOptions.Large))
                return null;

            var doc = (AssetDocument)item.data;
            if (doc != null && s_PackagesKey != null)
            {
                if (doc.productDetail == null)
                {
                    var productId = Convert.ToInt32(doc.id);
                    RequestProductDetailsInfo(new[] { productId }, (detail, error) =>
                    {
                        if (error != null || detail.results.Length == 0)
                            return;
                        doc.productDetail = detail.results[0];
                        doc.images = new[] { doc.productDetail.mainImage.big }.Concat(
                            doc.productDetail.images.Where(img => img.type == "screenshot").Select(imgDesc => imgDesc.imageUrl)).ToArray();
                    });
                }
            }

            if (doc.productDetail?.images.Length > 0)
                return (doc.lastPreview = FetchImage(doc.images, false, s_Previews) ?? doc.lastPreview);

            return null;
        }

        static void LoadPage(AssetsLoadingPage page, SearchItem item)
        {
            if (page.isLoaded)
            {
                // Can this happen?
                Debug.LogError($"Page already loaded: {page.startIndex}");
                return;
            }

            if (page.request != null)
            {
                // Request already started
                return;
            }

            // Load the page:
            SetQueryStartIndex(s_QueryParams, page.startIndex);
            var requestStr = Json.Serialize(s_QueryParams, true);
            // Debug.Log($"LoadPage: {page.startIndex} from {item.score} {item.id} {requestStr}");

            var request = Post(kSearchEndPoint, requestStr);
            page.request = request;
            var asyncOp = page.request.SendWebRequest();
            asyncOp.completed += op =>
            {
                if (!s_AssetsPage.Contains(page))
                {
                    // Debug.LogError($"Page does not exists: {page.startIndex}");
                    request.Dispose();
                    return;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    // What to do?
                    // Debug.LogError($"Error while loading page {page.startIndex}. {request.error}");
                }
                else
                {
                    var saneJsonStr = request.downloadHandler.text.Replace("name_en-US\"", "name_en_US\"");
                    var response = JsonUtility.FromJson<StoreSearchResponse>(saneJsonStr);
                    var nbUpdate = Mathf.Min(response.response.docs.Length, page.items.Count);
                    // Go over all items in this page and update them according to the fetched data:
                    for (var i = 0; i < nbUpdate; ++i)
                    {
                        var assetDoc = response.response.docs[i];
                        var item = page.items[i];
                        UpdateItem(item, assetDoc);
                    }

                    // TODO FetchItemProperties(DOTSE - 1994): keep this unless the view can tick all activeitems for all async properties (label, desc, thumbnail)
                    s_CurrentSearchView?.Refresh(RefreshFlags.DisplayModeChanged);
                    s_AssetsPage.Remove(page);
                }
                request.Dispose();
            };
        }

        static Texture2D FetchIcons(SearchItem item, bool animateCarrousel, Dictionary<string, PreviewData> imageDb)
        {
            var doc = item.data as AssetDocument;
            if (doc == null)
                return null;
            return FetchImage(doc.icon, animateCarrousel, imageDb);
        }

        static Texture2D FetchImage(string[] imageUrls, bool animateCarrousel, Dictionary<string, PreviewData> imageDb)
        {
            if (imageUrls == null || imageUrls.Length == 0)
                return null;

            var keyImage = imageUrls[0];
            if (animateCarrousel)
            {
                var imageIndex = Mathf.FloorToInt(Mathf.Repeat((float)EditorApplication.timeSinceStartup, imageUrls.Length));
                keyImage = imageUrls[imageIndex];
            }

            if (keyImage == null)
                return null;

            if (imageDb.TryGetValue(keyImage, out var previewData))
            {
                if (previewData.preview)
                    return previewData.preview;
                return null;
            }

            var newPreview = new PreviewData { request = UnityWebRequestTexture.GetTexture(keyImage) };
            newPreview.requestOp = newPreview.request.SendWebRequest();
            newPreview.requestOp.completed += (aop) =>
            {
                if (newPreview.request.isDone && newPreview.request.result == UnityWebRequest.Result.Success)
                    newPreview.preview = DownloadHandlerTexture.GetContent(newPreview.request);
                newPreview.requestOp = null;
            };
            imageDb[keyImage] = newPreview;
            return newPreview.preview;
        }

        static bool CanShowInPackageManager(SearchItem item)
        {
            var doc = item.data as AssetDocument;
            return IsItemOwned(item);
        }

        static bool CanShowInPackageManager(IReadOnlyCollection<SearchItem> items)
        {
            if (items.Count > 1)
                return false;
            return CanShowInPackageManager(items.First());
        }

        static bool IsItemOwned(SearchItem item)
        {
            var doc = (AssetDocument)item.data;
            return doc != null && purchasePackageIds != null && purchasePackageIds.Contains(doc.id);
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {
                new SearchAction(k_ProviderId, "browse", new GUIContent("Open Unity Asset Store..."))
                {
                    execute = (items) =>
                    {
                        foreach (var item in items)
                            BrowseAssetStoreItem(item);
                    },
                    closeWindowAfterExecution = false
                },
                new SearchAction(k_ProviderId, "open", new GUIContent("Show in Package Manager"))
                {
                    handler = (item) =>
                    {
                        if (CanShowInPackageManager(item))
                        {
                            OpenPackageManager(item);
                        }
                        else
                        {
                            BrowseAssetStoreItem(item);
                        }

                    },
                    closeWindowAfterExecution = false,
                    enabled = CanShowInPackageManager
                }
            };
        }

        static void OpenPackageManager(SearchItem item)
        {
            var doc = (AssetDocument)item.data;
            Utils.OpenPackageManager(doc.id);
        }

        static void BrowseAssetStoreItem(SearchItem item)
        {
            var doc = (AssetDocument)item.data;
            if (doc != null)
            {
                var url = MakeAssetStoreURL(doc.url, k_OpenBrowserFromSearch);
                Utils.OpenInBrowser(url);
                CheckPurchases(null);
            }
        }

        static string MakeAssetStoreURL(string url, string source)
        {
            // These params helps google analytics track when the asset store is opened and which source opened it.
            url += $"?utm_source={source}&utm_medium=desktop-app";
            return url;
        }

        static void GetAuthCode(Action<string, Exception> done)
        {
            if (s_AuthCode != null)
            {
                done(s_AuthCode, null);
                return;
            }

            UnityOAuth.GetAuthorizationCodeAsync("packman", response =>
            {
                if (response.Exception != null)
                {
                    done(null, response.Exception);
                    return;
                }
                s_AuthCode = response.AuthCode;
                done(response.AuthCode, null);
            });
        }

        static double GetEpochSeconds()
        {
            return (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        static bool IsTokenValid(double expirationStart, long expiresIn)
        {
            const long accessTokenBuffer = 15;
            return (GetEpochSeconds() - expirationStart) < (expiresIn - accessTokenBuffer);
        }

        static void GetAccessToken(Action<AccessToken, string> done)
        {
            if (s_AccessTokenData != null && IsTokenValid(s_AccessTokenData.expirationStarts, s_AccessTokenData.expiration))
            {
                done(s_AccessTokenData, null);
                return;
            }

            GetAuthCode((authCode, exception) =>
            {
                if (exception != null)
                {
                    done(null, exception.ToString());
                    return;
                }
                RequestAccessToken(authCode, (accessTokenData, error) =>
                {
                    if (accessTokenData == null)
                    {
                        done(null, "Failed to get access token.");
                        return;
                    }
                    s_AccessTokenData = accessTokenData;
                    s_AccessTokenData.expiration = long.Parse(s_AccessTokenData.expires_in);
                    s_AccessTokenData.expirationStarts = GetEpochSeconds();
                    done(accessTokenData, error);
                });
            });
        }

        static void GetAccessTokenInfo(Action<TokenInfo, string> done)
        {
            if (s_TokenInfo != null && IsTokenValid(s_AccessTokenData.expirationStarts, s_AccessTokenData.expiration))
            {
                done(s_TokenInfo, null);
                return;
            }

            GetAccessToken((accessTokenData, error) =>
            {
                if (error != null)
                {
                    done(null, error);
                    return;
                }
                RequestAccessTokenInfo(accessTokenData.access_token, (tokenInfo, tokenInfoError) =>
                {
                    s_TokenInfo = tokenInfo;
                    s_TokenInfo.expiration = long.Parse(s_TokenInfo.expires_in);
                    s_TokenInfo.expirationStarts = GetEpochSeconds();
                    done(tokenInfo, tokenInfoError);
                });
            });
        }

        static void GetUserInfo(Action<UserInfo, string> done)
        {
            GetAccessTokenInfo((accessTokenInfo, error) =>
            {
                if (error != null)
                {
                    done(null, error);
                    return;
                }

                if (s_UserInfo != null)
                {
                    done(s_UserInfo, null);
                    return;
                }

                RequestUserInfo(accessTokenInfo.access_token, accessTokenInfo.sub, (userInfo, userInfoError) =>
                {
                    s_UserInfo = userInfo;
                    done(userInfo, userInfoError);
                });
            });
        }

        static void GetAllPurchases(Action<List<PurchaseInfo>, string> done, Action cancel)
        {
            GetUserInfo((userInfo, userInfoError) =>
            {
                if (userInfoError != null)
                {
                    done(null, userInfoError);
                    return;
                }

                if (s_TokenInfo == null)
                {
                    cancel();
                    return;
                }

                const int kLimit = 50;
                RequestPurchases(s_TokenInfo.access_token, (purchases, errPurchases) =>
                {
                    if (errPurchases != null)
                    {
                        done(null, errPurchases);
                        return;
                    }

                    if (s_TokenInfo == null)
                    {
                        cancel();
                        return;
                    }

                    if (s_Purchases.Count == purchases.total)
                    {
                        done(s_Purchases, null);
                        return;
                    }

                    s_Purchases.Clear();
                    s_Purchases.AddRange(purchases.results);
                    if (purchases.total <= purchases.results.Length)
                    {
                        done(s_Purchases, null);
                        return;
                    }
                    var restOfPurchases = purchases.total - purchases.results.Length;
                    var nbRequests = (restOfPurchases / kLimit) + ((restOfPurchases % kLimit) > 0 ? 1 : 0);
                    var requestsFulfilled = 0;
                    for (var i = 0; i < nbRequests; ++i)
                    {
                        RequestPurchases(s_TokenInfo.access_token, (purchasesBatch, errPurchasesBatch) =>
                        {
                            if (purchasesBatch != null)
                            {
                                s_Purchases.AddRange(purchasesBatch.results);
                            }
                            requestsFulfilled++;
                            if (requestsFulfilled == nbRequests)
                            {
                                done(s_Purchases, null);
                            }
                        }, purchases.results.Length + (i * kLimit), kLimit);
                    }
                }, 0, kLimit);
            });
        }

#region Requests
        static void RequestUserInfo(string accessToken, string userId, Action<UserInfo, string> done)
        {
            var url = $"https://api.unity.com/v1/users/{userId}";
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            var asyncOp = request.SendWebRequest();
            asyncOp.completed += op =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    done(null, request.error);
                }
                else
                {
                    var text = request.downloadHandler.text;
                    var userInfo = JsonUtility.FromJson<UserInfo>(text);
                    done(userInfo, null);
                }
            };
        }

        static void RequestAccessTokenInfo(string accessToken, Action<TokenInfo, string> done)
        {
            var url = $"https://api.unity.com/v1/oauth2/tokeninfo?access_token={accessToken}";
            var request = UnityWebRequest.Get(url);
            var asyncOp = request.SendWebRequest();
            asyncOp.completed += op =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    done(null, request.error);
                }
                else
                {
                    var text = request.downloadHandler.text;
                    var tokenInfo = JsonUtility.FromJson<TokenInfo>(text);
                    done(tokenInfo, null);
                }
            };
        }

        static void RequestAccessToken(string authCode, Action<AccessToken, string> done)
        {
            var url = $"https://api.unity.com/v1/oauth2/token";
            var form = new WWWForm();
            form.AddField("grant_type", "authorization_code");
            form.AddField("code", authCode);
            form.AddField("client_id", "packman");
            form.AddField("client_secret", s_PackagesKey);
            form.AddField("redirect_uri", "packman://unity");
            var request = UnityWebRequest.Post(url, form);
            var asyncOp = request.SendWebRequest();
            asyncOp.completed += op =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    done(null, request.error);
                }
                else
                {
                    var text = request.downloadHandler.text;
                    s_AccessTokenData = JsonUtility.FromJson<AccessToken>(text);
                    done(s_AccessTokenData, null);
                }
                request.Dispose();
            };
        }

        static void RequestProductDetailsInfo(int[] productIds, Action<ProductListResponse, string> done)
        {
            var requestStr = Utils.JsonSerialize(productIds);
            var request = Post(kProductDetailsEndPoint, requestStr);
            var asyncOp = request.SendWebRequest();
            asyncOp.completed += op =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    done(null, request.error);
                }
                else
                {
                    var text = request.downloadHandler.text;
                    var result = JsonUtility.FromJson<ProductListResponse>(text);
                    done(result, null);
                }
                request.Dispose();
            };
        }

        static void RequestPurchases(string accessToken, Action<PurchaseResponse, string> done, int offset = 0, int limit = 50)
        {
            var url = $"https://packages-v2.unity.com/-/api/purchases?offset={offset}&limit={limit}&query=";
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            var asyncOp = request.SendWebRequest();
            asyncOp.completed += op =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    done(null, request.error);
                }
                else
                {
                    var text = request.downloadHandler.text;
                    var pr = JsonUtility.FromJson<PurchaseResponse>(text);
                    done(pr, null);
                }
            };
        }

#endregion

        [MenuItem("Window/Search/Asset Store", priority = 1270)]
        static void SearchAssetStoreMenu()
        {
            SearchStore();
        }

        [CommandHandler("OpenSearchStore")]
        internal static void OpenStoreCommand(CommandExecuteContext c)
        {
            SearchStore();
        }

        [CommandHandler("OpenAssetStoreInBrowser")]
        private static void OpenAssetStoreInBrowser(CommandExecuteContext c)
        {
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.BrowseAssetStoreWeb);
            string assetStoreUrl = MakeAssetStoreURL(UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudAssetStoreUrl), k_OpenBrowserFromToolbar);
            if (UnityEditor.Connect.UnityConnect.instance.loggedIn)
            {
                UnityEditor.Connect.UnityConnect.instance.OpenAuthorizedURLInWebBrowser(assetStoreUrl);
            }
            else
            {
                Application.OpenURL(assetStoreUrl);
            }
        }

        static void SearchStore()
        {
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchOpen, "SearchAssetStore");
            var storeContext = SearchService.CreateContext(SearchService.GetProvider(k_ProviderId));
            var viewState = SearchViewState.LoadDefaults();
            viewState.flags &= ~UnityEngine.Search.SearchViewFlags.OpenInspectorPreview;
            viewState.flags |= UnityEngine.Search.SearchViewFlags.DisableNoResultTips;
            viewState.context = storeContext;
            viewState.itemSize = (int)DisplayMode.Grid;
            viewState.queryBuilderEnabled = true;
            SearchService.ShowWindow(viewState);
        }

        static void Refresh()
        {
            EditorApplication.delayCall -= SearchService.RefreshWindows;
            EditorApplication.delayCall += SearchService.RefreshWindows;
        }

        static void ClearUserInfo()
        {
            s_UserInfo = null;
            s_TokenInfo = null;
            s_AccessTokenData = null;
            s_AuthCode = null;
            s_Purchases?.Clear();
            purchasePackageIds?.Clear();
            s_PackagesKey = null;
            s_StartPurchaseRequest = false;
            s_RequestCheckPurchases = true;
        }
    }
}
