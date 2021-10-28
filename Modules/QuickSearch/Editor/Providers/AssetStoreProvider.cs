// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Text;
using System.Linq;
using Debug = UnityEngine.Debug;
using UnityEditor.Connect;

namespace UnityEditor.Search.Providers
{
    static class AssetStoreProvider
    {
        #pragma warning disable CS0649
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
            public string publisher;
            public string[] icon;
            public string url;
            public string category_slug;
            // public string type;
            // public string name_ja-JP;
            // public string name_ko-KR;
            // public string name_zh-CN;
            // public int avg_rating;
            // public int ratings;
            // public int publisher_id;
            // public string on_sale;
            // public string plus_pro;
            public string[] key_images;
            // public float original_price_USD;
            // public float original_price_EUR;
            // public int age;
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
        }

        [Serializable]
        class ProductDetails
        {
            // public string id;
            // public string packageId;
            // public string slug;
            public PurchaseDetailMainImage mainImage;
            public ImageDesc[] images;
        }
        #pragma warning restore CS0649

        class PreviewData
        {
            public Texture2D preview;
            public UnityWebRequest request;
            public UnityWebRequestAsyncOperation requestOp;
        }

        enum QueryParamType
        {
            kString,
            kFloat,
            kBoolean,
            kStringArray,
            kInteger
        }

        class QueryParam
        {
            public QueryParamType type;
            public string name;
            public string keyword;
            public string queryName;

            public QueryParam(QueryParamType type, string name, string queryName = null)
            {
                this.type = type;
                this.name = name;
                keyword = name + ":";
                this.queryName = queryName ?? name;
            }
        }

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

        private static readonly List<QueryParam> k_QueryParams = new List<QueryParam>
        {
            new QueryParam(QueryParamType.kFloat, "min_price"),
            new QueryParam(QueryParamType.kFloat, "max_price"),
            new QueryParam(QueryParamType.kStringArray, "publisher"),
            new QueryParam(QueryParamType.kString, "version", "unity_version"),
            new QueryParam(QueryParamType.kBoolean, "free"),
            new QueryParam(QueryParamType.kBoolean, "on_sale"),
            new QueryParam(QueryParamType.kBoolean, "plus_pro_sale"),
            new QueryParam(QueryParamType.kStringArray, "category"),
            new QueryParam(QueryParamType.kInteger, "min_rating"),
            new QueryParam(QueryParamType.kInteger, "sort"),
            new QueryParam(QueryParamType.kBoolean, "reverse"),
            new QueryParam(QueryParamType.kInteger, "start"),
            new QueryParam(QueryParamType.kInteger, "rows"),
            new QueryParam(QueryParamType.kString, "lang"),
        };

        private static IEnumerable<SearchItem> SearchStore(SearchContext context, SearchProvider provider)
        {
            if (s_RequestCheckPurchases)
                CheckPurchases();

            if (string.IsNullOrEmpty(context.searchQuery))
                yield break;

            var requestQuery = new Dictionary<string, object>()
            {
                { "q", string.Join(" ", context.searchWords).Trim() }
            };
            ProcessFilter(context, requestQuery);

            var requestStr = Utils.JsonSerialize(requestQuery);
            using (var webRequest = Post(kSearchEndPoint, requestStr))
            {
                var rao = webRequest.SendWebRequest();
                while (!rao.isDone)
                    yield return null;

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Asset store request error: {webRequest.error}");
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
                        var scoreIndex = 1;
                        foreach (var doc in response.response.docs)
                        {
                            yield return CreateItem(context, provider, doc, scoreIndex++);
                        }
                    }
                }
            }
        }

        static void ProcessFilter(SearchContext context, Dictionary<string, object> request)
        {
            foreach (var filter in context.textFilters)
            {
                var filterTokens = filter.Split(':');
                var qp = k_QueryParams.Find(p => p.name == filterTokens[0]);
                if (qp == null || (filterTokens.Length != 2 && qp.type != QueryParamType.kBoolean))
                    continue;
                switch (qp.type)
                {
                    case QueryParamType.kBoolean:
                    {
                        var isOn = false;
                        if (filterTokens.Length == 2)
                        {
                            isOn = TryConvert.ToBool(filterTokens[1]);
                        }
                        else
                        {
                            isOn = true;
                        }
                        request.Add(qp.queryName, isOn);
                        break;
                    }
                    case QueryParamType.kInteger:
                    {
                        var value = TryConvert.ToInt(filterTokens[1]);
                        request.Add(qp.queryName, value);
                        break;
                    }
                    case QueryParamType.kFloat:
                    {
                        var value = TryConvert.ToFloat(filterTokens[1]);
                        request.Add(qp.queryName, value);
                        break;
                    }
                    case QueryParamType.kString:
                    {
                        request.Add(qp.queryName, filterTokens[1]);
                        break;
                    }
                    case QueryParamType.kStringArray:
                    {
                        request.Add(qp.queryName, new object[] { filterTokens[1] });
                        break;
                    }
                }
            }
        }

        static SearchItem CreateItem(SearchContext context, SearchProvider provider, AssetDocument doc, int score)
        {
            var label = doc.name_en_US;
            if (purchasePackageIds != null && purchasePackageIds.Contains(doc.id))
            {
                label += " (Owned)";
            }
            else if (doc.price_USD == 0)
            {
                label += " (Free)";
            }

            var description = $"{doc.name_en_US} - {doc.publisher} - {doc.category_slug}";
            var item = provider.CreateItem(context, doc.id, score, label, description, null, doc);
            item.options &= ~SearchItemOptions.FuzzyHighlight;
            item.options &= ~SearchItemOptions.Highlight;

            doc.productDetail = null;
            doc.url = $"https://assetstore.unity.com/packages/{doc.category_slug}/{doc.id}";
            return item;
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
            s_RequestCheckPurchases = true;
        }

        static bool HasAccessToken()
        {
            return !string.IsNullOrEmpty(Utils.GetConnectAccessToken());
        }

        static void CheckPurchases()
        {
            if (!HasAccessToken())
                return;

            if (s_PackagesKey == null)
                s_PackagesKey = Utils.GetPackagesKey();

            s_RequestCheckPurchases = false;
            if (s_StartPurchaseRequest)
                return;

            s_StartPurchaseRequest = true;
            var startRequest = System.Diagnostics.Stopwatch.StartNew();
            GetAllPurchases((purchases, error) =>
            {
                s_StartPurchaseRequest = false;
                if (error != null)
                {
                    Debug.LogError($"Error in fetching user purchases: {error}");
                    return;
                }
                startRequest.Stop();

                purchasePackageIds = new HashSet<string>();
                foreach (var purchaseInfo in purchases)
                {
                    purchasePackageIds.Add(purchaseInfo.packageId.ToString());
                }
            });
        }

        const string k_ProviderId = "store";

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(k_ProviderId, "Asset Store")
            {
                active = true,
                isExplicitProvider = true,
                filterId = "store:",
                onEnable = OnEnable,
                showDetails = true,
                fetchItems = (context, items, provider) => SearchStore(context, provider),
                fetchThumbnail = (item, context) => FetchImage(((AssetDocument)item.data).icon, false, s_Previews) ?? Icons.store,
                fetchPreview = FetchPreview
            };
        }

        private static Texture2D FetchPreview(SearchItem item, SearchContext context, Vector2 size, FetchPreviewOptions options)
        {
            if (!options.HasFlag(FetchPreviewOptions.Large))
                return null;

            var doc = (AssetDocument)item.data;
            if (s_PackagesKey != null)
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
                return (doc.lastPreview = FetchImage(doc.images, true, s_Previews) ?? doc.lastPreview);

            if (doc.key_images.Length > 0)
                return (doc.lastPreview = FetchImage(doc.key_images, true, s_Previews) ?? doc.lastPreview);

            return (doc.lastPreview = FetchImage(doc.icon, true, s_Previews) ?? doc.lastPreview);
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

        static bool IsItemOwned(IReadOnlyCollection<SearchItem> items)
        {
            if (items.Count > 1)
                return false;
            return IsItemOwned(items.First());
        }

        static bool IsItemOwned(SearchItem item)
        {
            var doc = (AssetDocument)item.data;
            return purchasePackageIds != null && purchasePackageIds.Contains(doc.id);
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
                    }
                },
                new SearchAction(k_ProviderId, "open", new GUIContent("Show in Package Manager"))
                {
                    handler = (item) =>
                    {
                        if (IsItemOwned(item))
                        {
                            var doc = (AssetDocument)item.data;
                            Utils.OpenPackageManager(doc.id);
                        }
                        else
                        {
                            BrowseAssetStoreItem(item);
                        }

                    },
                    enabled = IsItemOwned
                }
            };
        }

        static void BrowseAssetStoreItem(SearchItem item)
        {
            var doc = (AssetDocument)item.data;
            Utils.OpenInBrowser(doc.url);
            CheckPurchases();
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

        static void GetAllPurchases(Action<List<PurchaseInfo>, string> done)
        {
            GetUserInfo((userInfo, userInfoError) =>
            {
                if (userInfoError != null)
                {
                    done(null, userInfoError);
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
        internal static void SearchAssetStoreMenu()
        {
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchOpen, "SearchAssetStore");
            var storeContext = SearchService.CreateContext(SearchService.GetProvider(k_ProviderId));
            var qs = QuickSearch.Create(storeContext, topic: "asset store");
            qs.itemIconSize = (int)DisplayMode.Grid;
            qs.SetSearchText(string.Empty);
            qs.ShowWindow();
        }
    }
}
