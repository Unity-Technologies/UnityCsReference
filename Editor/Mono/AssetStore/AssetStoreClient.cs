// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

/*
 * Server class handling communication with the server backend
 *
 * Jonas Drewsen - (C) Unity3d.com - 2011
 *
 */
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;


namespace UnityEditor
{
    /**
     * The raw response from the asset store before being parsed into
     * specific result class (see below).
     */
    class AssetStoreResponse
    {
        internal AsyncHTTPClient job;
        public Dictionary<string, JSONValue> dict;
        public bool ok;
        public bool failed { get { return !ok; } }
        public string message
        {
            get
            {
                if (dict == null || !dict.ContainsKey("message")) return null;
                return dict["message"].AsString(true);
            }
        }
        // Encode a string into a json string
        private static string EncodeString(string str)
        {
            str = str.Replace("\"", "\\\"");
            str = str.Replace("\\", "\\\\");
            str = str.Replace("\b", "\\b");
            str = str.Replace("\f", "\\f");
            str = str.Replace("\n", "\\n");
            str = str.Replace("\r", "\\r");
            str = str.Replace("\t", "\\t");
            // We do not use \uXXXX specifier but direct unicode in the string.
            return str;
        }

        public override string ToString()
        {
            string res = "{";
            string delim = "";
            foreach (KeyValuePair<string, JSONValue> kv in dict)
            {
                res += delim + '"' + EncodeString(kv.Key) + "\" : " + kv.Value.ToString();
                delim = ", ";
            }
            return res + "}";
        }
    }

    /**
     * All results derive from this class.
     */
    abstract class AssetStoreResultBase<Derived> where Derived : class
    {
        public delegate void Callback(Derived res);
        private Callback callback;
        public string error;
        public string warnings;

        public AssetStoreResultBase(Callback cb)
        {
            callback = cb;
            warnings = "";
        }

        public void Parse(AssetStoreResponse response)
        {
            if (response.job.IsSuccess())
            {
                if (response.job.responseCode >= 300)
                    error = string.Format("HTTP status code {0}", response.job.responseCode);
                else if (response.dict.ContainsKey("error"))
                    error = response.dict["error"].AsString(true);
                else
                    Parse(response.dict);
            }
            else
            {
                string url = response.job == null ? "nulljob" : (response.job.url == null ? "null" : response.job.url);
                error = "Error receiving response from server on url '" + url + "': " + (response.job.text ?? "n/a");
            }
            callback(this as Derived);
        }

        abstract protected void Parse(Dictionary<string, JSONValue> dict);
    }


    /**
     * Result that contains the list of asset matching a search criteria.
     */
    class AssetStoreSearchResults : AssetStoreResultBase<AssetStoreSearchResults>
    {
        internal struct Group
        {
            public static Group Create()
            {
                Group g = new Group();
                g.assets = new List<AssetStoreAsset>();
                g.label = "";
                g.name = "";
                g.offset = 0;
                g.limit = -1; // no limit
                return g;
            }

            public List<AssetStoreAsset> assets;
            public int totalFound;
            public string label;
            public string name;
            public int offset;
            public int limit;
        };

        internal List<Group> groups = new List<Group>();

        public AssetStoreSearchResults(Callback c) : base(c) {}

        protected override void Parse(Dictionary<string, JSONValue> dict)
        {
            foreach (JSONValue v in dict["groups"].AsList(true))
            {
                Group group = Group.Create();
                ParseList(v, ref group);
                groups.Add(group);
            }
            JSONValue offsets = dict["query"]["offsets"];
            List<JSONValue> limits = dict["query"]["limits"].AsList(true);
            int idx = 0;
            foreach (JSONValue v in offsets.AsList(true))
            {
                Group g = groups[idx];
                g.offset = (int)v.AsFloat(true);
                g.limit = (int)limits[idx].AsFloat(true);
                groups[idx] = g;
                idx++;
            }
        }

        static string StripExtension(string path)
        {
            if (path == null) return null;

            int i = path.LastIndexOf(".");
            return i < 0 ? path : path.Substring(0, i);
        }

        private void ParseList(JSONValue matches, ref Group group)
        {
            List<AssetStoreAsset> assets = group.assets;

            if (matches.ContainsKey("error"))
                error = matches["error"].AsString(true);
            if (matches.ContainsKey("warnings"))
                warnings = matches["warnings"].AsString(true);

            if (matches.ContainsKey("name"))
                group.name = matches["name"].AsString(true);
            if (matches.ContainsKey("label"))
                group.label = matches["label"].AsString(true);
            group.label = group.label ?? group.name; // fallback
            if (matches.ContainsKey("total_found"))
                group.totalFound = (int)matches["total_found"].AsFloat(true);

            if (matches.ContainsKey("matches"))
            {
                foreach (JSONValue asset in matches["matches"].AsList(true))
                {
                    AssetStoreAsset res = new AssetStoreAsset();
                    if (!asset.ContainsKey("id") || !asset.ContainsKey("name") || !asset.ContainsKey("package_id"))
                        continue;

                    res.id = (int)asset["id"].AsFloat();
                    res.name = asset["name"].AsString();
                    res.displayName = StripExtension(res.name);
                    res.packageID = (int)asset["package_id"].AsFloat();

                    if (asset.ContainsKey("static_preview_url"))
                        res.staticPreviewURL = asset["static_preview_url"].AsString();
                    if (asset.ContainsKey("dynamic_preview_url"))
                        res.dynamicPreviewURL = asset["dynamic_preview_url"].AsString();

                    res.className = asset.ContainsKey("class_name") ? asset["class_name"].AsString() : "";
                    if (asset.ContainsKey("price"))
                        res.price = asset["price"].AsString();
                    assets.Add(res);
                }
            }
        }
    }

    /**
     * Result that will flesh out the specified AssetStoreAssets with
     * info to be displayed in an inspector.
     */
    class AssetStoreAssetsInfo : AssetStoreResultBase<AssetStoreAssetsInfo>
    {
        internal enum Status
        {
            BasketNotEmpty,
            ServiceDisabled,
            AnonymousUser,
            Ok
        }
        internal Status status;

        internal Dictionary<int, AssetStoreAsset> assets = new Dictionary<int, AssetStoreAsset>();
        internal bool paymentTokenAvailable;
        internal string paymentMethodCard;
        internal string paymentMethodExpire;
        internal float price;
        internal float vat;
        internal string currency;
        internal string priceText;
        internal string vatText;
        internal string message;

        internal AssetStoreAssetsInfo(Callback c, List<AssetStoreAsset> assets) : base(c)
        {
            foreach (AssetStoreAsset a in assets)
                this.assets[a.id] = a;
        }

        protected override void Parse(Dictionary<string, JSONValue> dict)
        {
            Dictionary<string, JSONValue> purchaseInfo = dict["purchase_info"].AsDict(true);
            string statusStr = purchaseInfo["status"].AsString(true);
            if (statusStr == "basket-not-empty")
                status = Status.BasketNotEmpty;
            else if (statusStr == "service-disabled")
                status = Status.ServiceDisabled;
            else if (statusStr == "user-anonymous")
                status = Status.AnonymousUser;
            else if (statusStr == "ok")
                status = Status.Ok;

            paymentTokenAvailable = purchaseInfo["payment_token_available"].AsBool();
            if (purchaseInfo.ContainsKey("payment_method_card"))
                paymentMethodCard = purchaseInfo["payment_method_card"].AsString(true);
            if (purchaseInfo.ContainsKey("payment_method_expire"))
                paymentMethodExpire = purchaseInfo["payment_method_expire"].AsString(true);
            price = purchaseInfo["price"].AsFloat(true);
            vat = purchaseInfo["vat"].AsFloat(true);
            priceText = purchaseInfo["price_text"].AsString(true);
            vatText = purchaseInfo["vat_text"].AsString(true);
            currency = purchaseInfo["currency"].AsString(true);
            message = purchaseInfo.ContainsKey("message") ? purchaseInfo["message"].AsString(true) : null;

            List<JSONValue> assetsIn = dict["results"].AsList(true);

            foreach (JSONValue val in assetsIn)
            {
                AssetStoreAsset asset;
                int aid = 0;
                if (val["id"].IsString())
                    aid = int.Parse(val["id"].AsString());
                else
                    aid = (int)val["id"].AsFloat();

                if (!assets.TryGetValue(aid, out asset))
                    continue;

                if (asset.previewInfo == null)
                    asset.previewInfo = new AssetStoreAsset.PreviewInfo();

                AssetStoreAsset.PreviewInfo a = asset.previewInfo;
                asset.className = val["class_names"].AsString(true).Trim();
                a.packageName = val["package_name"].AsString(true).Trim();
                a.packageShortUrl = val["short_url"].AsString(true).Trim();
                // a.packagePrice = val.ContainsKey("price_text") ? val["price_text"].AsString(true).Trim() : null;
                asset.price = val.ContainsKey("price_text") ? val["price_text"].AsString(true).Trim() : null;
                a.packageSize = int.Parse(val.Get("package_size").IsNull() ? "-1" : val["package_size"].AsString(true));
                asset.packageID = int.Parse(val["package_id"].AsString());
                a.packageVersion = val["package_version"].AsString();
                a.packageRating = int.Parse(val.Get("rating").IsNull() || val["rating"].AsString(true).Length == 0 ? "-1" : val["rating"].AsString(true));
                a.packageAssetCount = int.Parse(val["package_asset_count"].IsNull() ? "-1" : val["package_asset_count"].AsString(true));
                a.isPurchased = val.ContainsKey("purchased") ? val["purchased"].AsBool(true) : false;
                a.isDownloadable = a.isPurchased || asset.price == null;
                a.publisherName = val["publisher_name"].AsString(true).Trim();
                // a.previewBundle = (!val.ContainsKey("preview_bundle") || val["preview_bundle"].IsNull()) ? "" : val["preview_bundle"].AsString(true);
                a.packageUrl = val.Get("package_url").IsNull() ? "" : val["package_url"].AsString(true);
                a.encryptionKey = val.Get("encryption_key").IsNull() ? "" : val["encryption_key"].AsString(true);
                a.categoryName = val.Get("category_name").IsNull() ? "" : val["category_name"].AsString(true);
                a.buildProgress = -1f;
                a.downloadProgress = -1f;
            }
        }
    }

    /**
     * Success of purchasing a package
     */
    class PurchaseResult : AssetStoreResultBase<PurchaseResult>
    {
        public enum Status
        {
            BasketNotEmpty,
            ServiceDisabled,
            AnonymousUser,
            PasswordMissing,
            PasswordWrong,
            PurchaseDeclined,
            Ok
        }
        public Status status;
        public int packageID;
        public string message;

        public PurchaseResult(Callback c) : base(c)
        {
        }

        protected override void Parse(Dictionary<string, JSONValue> dict)
        {
            packageID = int.Parse(dict["package_id"].AsString());
            message = dict.ContainsKey("message") ? dict["message"].AsString(true) : null;

            string statusStr = dict["status"].AsString(true);
            if (statusStr == "basket-not-empty")
                status = Status.BasketNotEmpty;
            else if (statusStr == "service-disabled")
                status = Status.ServiceDisabled;
            else if (statusStr == "user-anonymous")
                status = Status.AnonymousUser;
            else if (statusStr == "password-missing")
                status = Status.PasswordMissing;
            else if (statusStr == "password-wrong")
                status = Status.PasswordWrong;
            else if (statusStr == "purchase-declined")
                status = Status.PurchaseDeclined;
            else if (statusStr == "ok")
                status = Status.Ok;
        }
    }

    /**
     * Status of building a package on the asset store server.
     */
    class BuildPackageResult : AssetStoreResultBase<BuildPackageResult>
    {
        internal AssetStoreAsset asset;
        internal int packageID;

        internal BuildPackageResult(AssetStoreAsset asset, Callback c) : base(c)
        {
            this.asset = asset;
            packageID = -1;
        }

        protected override void Parse(Dictionary<string, JSONValue> dict)
        {
            dict = dict["download"].AsDict();

            packageID = int.Parse(dict["id"].AsString());
            if (packageID != asset.packageID)
            {
                Debug.LogError("Got asset store server build result from mismatching package");
                return;
            }
            asset.previewInfo.packageUrl = dict.ContainsKey("url") ? dict["url"].AsString(true) : "";
            asset.previewInfo.encryptionKey = dict.ContainsKey("key") ? dict["key"].AsString(true) : "";

            // The asset store is weird. Of pct is 0 it returns a 0.0 float, if it is > 0 it returns a string with the pct ie "23".
            asset.previewInfo.buildProgress = (dict["progress"].IsFloat() ? dict["progress"].AsFloat(true) : float.Parse(dict["progress"].AsString(true), System.Globalization.CultureInfo.InvariantCulture)) / 100.0f;
        }
    }


    /**
     * The main API class for communication with the asset store server.
     * Most interesting methods are the static ones specified in the
     * bottom of this class.
     */
    class AssetStoreClient
    {
        const string kUnauthSessionID = "26c4202eb475d02864b40827dfff11a14657aa41";

        internal enum LoginState
        {
            LOGGED_OUT,
            IN_PROGRESS,
            LOGGED_IN,
            LOGIN_ERROR
        };

        static string s_AssetStoreUrl = null;
        static string s_AssetStoreSearchUrl = null;
        static LoginState sLoginState = AssetStoreClient.LoginState.LOGGED_OUT;
        static string sLoginErrorMessage = null;
        public static string LoginErrorMessage { get { return sLoginErrorMessage; } }
        public delegate void DoneCallback(AssetStoreResponse response);
        public delegate void DoneLoginCallback(string errorMessage);

        // Version parameters expected by the server
        static string VersionParams
        {
            get
            {
                return "unityversion=" + System.Uri.EscapeDataString(Application.unityVersion) + "&skip_terms=1";
            }
        }

        // The base asset store url as parsed from the loader.html file
        static string AssetStoreUrl
        {
            get
            {
                if (s_AssetStoreUrl == null)
                    s_AssetStoreUrl = AssetStoreUtils.GetAssetStoreUrl();
                // s_AssetStoreUrl = "https://kharma-test.unity3d.com";
                return s_AssetStoreUrl;
            }
        }

        // The base asset store url as parsed from the loader.html file
        static string AssetStoreSearchUrl
        {
            get
            {
                if (s_AssetStoreSearchUrl == null)
                    s_AssetStoreSearchUrl = AssetStoreUtils.GetAssetStoreSearchUrl();
                // s_AssetStoreSearchUrl = "http://shawarma-test.unity3d.com";
                return s_AssetStoreSearchUrl;
            }
        }

        static AssetStoreClient()
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        }

        // The base server API url. This is used for all the normal request towards the server API.
        static string APIUrl(string path)
        {
            return String.Format("{0}/api{2}.json?{1}" , AssetStoreUrl, VersionParams, path);
        }

        static string APISearchUrl(string path)
        {
            return String.Format("{0}/public-api{2}.json?{1}" , AssetStoreSearchUrl, VersionParams, path);
        }

        // The saved session ID if the user has allowed remembering it.
        static string SavedSessionID
        {
            get
            {
                if (RememberSession)
                    return EditorPrefs.GetString("kharma.sessionid", "");
                return "";
            }
            set { EditorPrefs.SetString("kharma.sessionid", value); }
        }

        public static bool HasSavedSessionID
        {
            get { return !string.IsNullOrEmpty(SavedSessionID); }
        }

        internal static string ActiveSessionID
        {
            get
            {
                if (AssetStoreContext.SessionHasString("kharma.active_sessionid"))
                    return AssetStoreContext.SessionGetString("kharma.active_sessionid");
                return "";
            }
            set { AssetStoreContext.SessionSetString("kharma.active_sessionid", value); }
        }

        public static bool HasActiveSessionID
        {
            get { return !string.IsNullOrEmpty(ActiveSessionID); }
        }

        static string ActiveOrUnauthSessionID
        {
            get
            {
                string s = ActiveSessionID;
                if (s == "")
                    return kUnauthSessionID;
                return s;
            }
        }

        static public bool RememberSession
        {
            get { return EditorPrefs.GetString("kharma.remember_session") == "1"; }
            set { EditorPrefs.SetString("kharma.remember_session", value ? "1" : "0"); }
        }

        // The authentication token
        static string GetToken()
        {
            return InternalEditorUtility.GetAuthToken();
        }

        // Login status
        public static bool LoggedIn() { return !string.IsNullOrEmpty(ActiveSessionID); }
        public static bool LoggedOut() { return string.IsNullOrEmpty(ActiveSessionID); }
        public static bool LoginError() { return sLoginState == LoginState.LOGIN_ERROR; }
        public static bool LoginInProgress() { return sLoginState == LoginState.IN_PROGRESS; }

        internal static void LoginWithCredentials(string username, string password, bool rememberMe, DoneLoginCallback callback)
        {
            if (sLoginState == LoginState.IN_PROGRESS)
            {
                Debug.LogError("Tried to login with credentials while already in progress of logging in");
                return;
            }
            sLoginState = LoginState.IN_PROGRESS;
            RememberSession = rememberMe;
            string url = AssetStoreUrl + "/login?skip_terms=1";

            AssetStoreClient.sLoginErrorMessage = null;
            AsyncHTTPClient client = new AsyncHTTPClient(url.Replace("http://", "https://"));
            client.postData = "user=" + username + "&pass=" + password;
            client.header["X-Unity-Session"] = kUnauthSessionID + GetToken();
            client.doneCallback = WrapLoginCallback(callback);
            client.Begin();
        }

        /*
         * Tries to login using a remembered session
         */
        internal static void LoginWithRememberedSession(DoneLoginCallback callback)
        {
            if (sLoginState == LoginState.IN_PROGRESS)
            {
                Debug.LogError("Tried to login with remembered session while already in progress of logging in");
                return;
            }
            sLoginState = LoginState.IN_PROGRESS;

            // Make sure the session is not present if we're not allowed to use it
            if (!RememberSession)
                SavedSessionID = "";

            string url = AssetStoreUrl + "/login?skip_terms=1&reuse_session=" + SavedSessionID;
            AssetStoreClient.sLoginErrorMessage = null;
            AsyncHTTPClient client = new AsyncHTTPClient(url);
            client.header["X-Unity-Session"] = kUnauthSessionID + GetToken();
            client.doneCallback = WrapLoginCallback(callback);
            client.Begin();
        }

        // Helper function for login callbacks
        static AsyncHTTPClient.DoneCallback WrapLoginCallback(DoneLoginCallback callback)
        {
            return delegate(AsyncHTTPClient job) {
                    // We're logging in
                    string msg = job.text;
                    if (!job.IsSuccess())
                    {
                        AssetStoreClient.sLoginState = LoginState.LOGIN_ERROR;
                        AssetStoreClient.sLoginErrorMessage = job.responseCode >= 200 && job.responseCode < 300 ? msg : "Failed to login - please retry";
                    }
                    else if (msg.StartsWith("<!DOCTYPE")) // TODO: Expose status line in job
                    {
                        AssetStoreClient.sLoginState = LoginState.LOGIN_ERROR;
                        AssetStoreClient.sLoginErrorMessage = "Failed to login";
                    }
                    else
                    {
                        AssetStoreClient.sLoginState = LoginState.LOGGED_IN;
                        if (msg.Contains("@")) // login with reused session id returns the user email
                            AssetStoreClient.ActiveSessionID = SavedSessionID;
                        else
                            AssetStoreClient.ActiveSessionID = msg;

                        if (RememberSession)
                        {
                            SavedSessionID = ActiveSessionID;
                        }
                    }
                    callback(AssetStoreClient.sLoginErrorMessage);
                };
        }

        public static void Logout()
        {
            ActiveSessionID = "";
            SavedSessionID = "";
            sLoginState = AssetStoreClient.LoginState.LOGGED_OUT;
        }

        // Create a pending HTTP GET request to the server
        static AsyncHTTPClient CreateJSONRequest(string url, DoneCallback callback)
        {
            AsyncHTTPClient client = new AsyncHTTPClient(url);
            client.header["X-Unity-Session"] = ActiveOrUnauthSessionID + GetToken();
            client.doneCallback = WrapJsonCallback(callback);
            client.Begin();
            return client;
        }

        // Create a pending HTTP POST request to the server
        static AsyncHTTPClient CreateJSONRequestPost(string url, Dictionary<string, string> param,
            DoneCallback callback)
        {
            AsyncHTTPClient client = new AsyncHTTPClient(url);

            client.header["X-Unity-Session"] = ActiveOrUnauthSessionID + GetToken();
            client.postDictionary = param;

            client.doneCallback = WrapJsonCallback(callback);
            client.Begin();
            return client;
        }

        // Create a pending HTTP POST request to the server
        static AsyncHTTPClient CreateJSONRequestPost(string url, string postData,
            DoneCallback callback)
        {
            AsyncHTTPClient client = new AsyncHTTPClient(url);

            client.header["X-Unity-Session"] = ActiveOrUnauthSessionID + GetToken();
            client.postData = postData;

            client.doneCallback = WrapJsonCallback(callback);
            client.Begin();
            return client;
        }

        /* TODO: Implement PUT from a filepath in back end curl code
        // Create a pending HTTP PUT request to the server
        static void CreateJSONRequestPut(string url, string filepath,
                                                 DoneCallback callback) {
            AsyncHTTPClient job = new AsyncHTTPClient(url, "PUT");
            client.SetUploadFilePath(filepath);
            client.header["X-Unity-Session"] = ActiveOrUnauthSessionID + GetToken();
            client.doneCallback = WrapJsonCallback(callback);
            client.Begin();
        }
        */

        // Create a pending HTTP DELETE request to the server
        static AsyncHTTPClient CreateJSONRequestDelete(string url, DoneCallback callback)
        {
            AsyncHTTPClient client = new AsyncHTTPClient(url, "DELETE");
            client.header["X-Unity-Session"] = ActiveOrUnauthSessionID + GetToken();
            client.doneCallback = WrapJsonCallback(callback);
            client.Begin();
            return client;
        }

        /* Handle HTTP results and forward them to the original callback.
         *
         * This will callback any handler registered for requests that has finished.
         */
        private static AsyncHTTPClient.DoneCallback WrapJsonCallback(DoneCallback callback)
        {
            return delegate(AsyncHTTPClient job) {
                    if (job.IsDone())
                    {
                        try
                        {
                            AssetStoreResponse c = ParseContent(job);
                            callback(c);
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("Uncaught exception in async net callback: " + ex.Message);
                            Debug.Log(ex.StackTrace);
                        }
                    }
                };
        }

        /*
         * Parse the HTTP response as a JSON string and into a AssetStoreResponse object.
         */
        static AssetStoreResponse ParseContent(AsyncHTTPClient job)
        {
            AssetStoreResponse resp = new AssetStoreResponse();
            resp.job = job;
            resp.dict = null;
            resp.ok = false;
            AsyncHTTPClient.State state = job.state;
            string content = job.text;
            if (!AsyncHTTPClient.IsSuccess(state))
            {
                Console.WriteLine(content);
                // Debug.Log("Request error: " + content);
                return resp; // abort
            }

            string status;
            string message;
            resp.dict = ParseJSON(content, out status, out message);
            if (status == "error")
            {
                Debug.LogError("Request error (" + status + "): " + message);
                return resp;
            }
            resp.ok = true;
            return resp;
        }

        /*
         * Parse the HTTP response as a JSON string into a string/JSONValue dictionary
         */
        static Dictionary<string, JSONValue> ParseJSON(string content, out string status, out string message)
        {
            // extract ids etc. from json message
            message = null;
            status = null;
            JSONValue jval;
            try
            {
                JSONParser parser = new JSONParser(content);
                jval = parser.Parse();
            }
            catch (JSONParseException ex)
            {
                Debug.Log("Error parsing server reply: " + content);
                Debug.Log(ex.Message);
                return null;
            }
            Dictionary<string, JSONValue> dict;
            try
            {
                dict = jval.AsDict(true);
                if (dict == null)
                {
                    Debug.Log("Error parsing server message: " + content);
                    return null;
                }
                // Old backend encapsulated all replies in a "result" dict. Just unwrap that here.
                if (dict.ContainsKey("result") && dict["result"].IsDict())
                {
                    dict = dict["result"].AsDict(true);
                }

                if (dict.ContainsKey("message"))
                    message = dict["message"].AsString(true);
                if (dict.ContainsKey("status"))
                    status = dict["status"].AsString(true);
                else if (dict.ContainsKey("error"))
                {
                    status = dict["error"].AsString(true);
                    if (status == "")
                        status = "ok";
                }
                else
                    status = "ok";
            }
            catch (JSONTypeException ex)
            {
                Debug.Log("Error parsing server reply. " + content);
                Debug.Log(ex.Message);
                return null;
            }
            return dict;
        }

        internal struct SearchCount
        {
            public string name;
            public int offset;
            public int limit;
        }


        /*
         * Searches the asset store for assets and passes the results to DoneCallback
         */
        internal static AsyncHTTPClient SearchAssets(string searchString, string[] requiredClassNames, string[] assetLabels,
            List<SearchCount> counts,
            AssetStoreSearchResults.Callback callback)
        {
            string offsets = "";
            string limits = "";
            string groupNames = "";
            string delim = "";
            foreach (SearchCount v in counts)
            {
                offsets += delim + v.offset;
                limits += delim + v.limit;
                groupNames += delim + v.name;
                delim = ",";
            }

            // If one af the class names is "MonoScript" then also include "Script" since
            // that is what asset store expects
            if (Array.Exists(requiredClassNames, (string a) => { return a.Equals("MonoScript", StringComparison.OrdinalIgnoreCase); }))
            {
                Array.Resize(ref requiredClassNames, requiredClassNames.Length + 1);
                requiredClassNames[requiredClassNames.Length - 1] = "Script";
            }

            string url =  string.Format("{0}&q={1}&c={2}&l={3}&O={4}&N={5}&G={6}",
                    APISearchUrl("/search/assets"),
                    System.Uri.EscapeDataString(searchString),
                    System.Uri.EscapeDataString(string.Join(",", requiredClassNames)),
                    System.Uri.EscapeDataString(string.Join(",", assetLabels)),
                    offsets, limits, groupNames);

            //Debug.Log(url);
            //Debug.Log("session key " + ActiveOrUnauthSessionID + GetToken());
            AssetStoreSearchResults r = new AssetStoreSearchResults(callback);
            return CreateJSONRequest(url, delegate(AssetStoreResponse ar) { r.Parse(ar); });
        }

        /*
         * Looks up assets by ID in the asset store and passes the results to DoneCallback
         */
        internal static AsyncHTTPClient AssetsInfo(List<AssetStoreAsset> assets, AssetStoreAssetsInfo.Callback callback)
        {
            string url = APIUrl("/assets/list");
            foreach (AssetStoreAsset asset in assets)
                url += "&id=" + asset.id.ToString();
            // Debug.Log(url);
            AssetStoreAssetsInfo r = new AssetStoreAssetsInfo(callback, assets);
            return CreateJSONRequest(url, delegate(AssetStoreResponse ar) { r.Parse(ar); });
        }

        /*
         * Returns purchase info for the package that has assetID as part of it.
         */
        internal static AsyncHTTPClient DirectPurchase(int packageID, string password, PurchaseResult.Callback callback)
        {
            string url = APIUrl(string.Format("/purchase/direct/{0}", packageID.ToString()));
            // Debug.Log(url);
            PurchaseResult r = new PurchaseResult(callback);
            Dictionary<string, string> d = new Dictionary<string, string>();
            d["password"] = password;
            return CreateJSONRequestPost(url, d, delegate(AssetStoreResponse ar) { r.Parse(ar); });
        }

        internal static AsyncHTTPClient BuildPackage(AssetStoreAsset asset, BuildPackageResult.Callback callback)
        {
            string url = APIUrl("/content/download/" + asset.packageID.ToString());
            // Debug.Log(url);
            BuildPackageResult r = new BuildPackageResult(asset, callback);
            return CreateJSONRequest(url, delegate(AssetStoreResponse ar) { r.Parse(ar); });
        }
    }
}
