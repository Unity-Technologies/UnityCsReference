// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UnityEditor
{
    /**
     *  An asset store asset.
     */
    public sealed class AssetStoreAsset
    {
        /* Search result data
         *
         * This is returned when searching for assets
         */
        public int id;
        public string name;
        public string displayName;      // name without extension, we cache it to prevent string operations when rendering
        public string staticPreviewURL;
        public string dynamicPreviewURL;
        public string className;
        public string price;
        public int packageID;

        /* Preview data
         *
         * This is returned when clicking on an assets for preview
         */
        internal class PreviewInfo
        {
            public string packageName;
            public string packageShortUrl;
            public int packageSize;
            public string packageVersion;
            public int packageRating;
            public int packageAssetCount;
            public bool isPurchased;
            public bool isDownloadable;
            public string publisherName;
            public string encryptionKey;
            public string packageUrl;
            public float buildProgress; // -1 when not building
            public float downloadProgress; // -1 when not downloading
            public string categoryName;
        }

        internal PreviewInfo previewInfo;

        //
        public Texture2D previewImage;
        internal AssetBundleCreateRequest previewBundleRequest;
        internal AssetBundle previewBundle;
        internal Object previewAsset;
        internal bool disposed;

        // public AssetStoreAssetsInfo assetsInfo;
        public AssetStoreAsset()
        {
            disposed = false;
        }

        public void Dispose()
        {
            if (previewImage != null)
            {
                Object.DestroyImmediate(previewImage);
                previewImage = null;
            }
            if (previewBundle != null)
            {
                previewBundle.Unload(true);
                previewBundle = null;
                previewAsset = null;
            }
            disposed = true;
        }

        public Object Preview
        {
            get
            {
                if (previewAsset != null)
                    return previewAsset;

                return previewImage;
            }
        }

        public bool HasLivePreview
        {
            get
            {
                return previewAsset != null;
            }
        }

        internal string DebugString
        {
            get
            {
                string r = string.Format("id: {0}\nname: {1}\nstaticPreviewURL: {2}\ndynamicPreviewURL: {3}\n" +
                    "className: {4}\nprice: {5}\npackageID: {6}",
                    id, name ?? "N/A", staticPreviewURL ?? "N/A", dynamicPreviewURL ?? "N/A", className ?? "N/A", price, packageID);
                if (previewInfo != null)
                {
                    r += string.Format("previewInfo {{\n" +
                        "    packageName: {0}\n" +
                        "    packageShortUrl: {1}\n" +
                        "    packageSize: {2}\n" +
                        "    packageVersion: {3}\n" +
                        "    packageRating: {4}\n" +
                        "    packageAssetCount: {5}\n" +
                        "    isPurchased: {6}\n" +
                        "    isDownloadable: {7}\n" +
                        "    publisherName: {8}\n" +
                        "    encryptionKey: {9}\n" +
                        "    packageUrl: {10}\n" +
                        "    buildProgress: {11}\n" +
                        "    downloadProgress: {12}\n" +
                        "    categoryName: {13}\n" +
                        "}}",
                        previewInfo.packageName ?? "N/A",
                        previewInfo.packageShortUrl ?? "N/A",
                        previewInfo.packageSize,
                        previewInfo.packageVersion ?? "N/A",
                        previewInfo.packageRating,
                        previewInfo.packageAssetCount,
                        previewInfo.isPurchased,
                        previewInfo.isDownloadable,
                        previewInfo.publisherName ?? "N/A",
                        previewInfo.encryptionKey ?? "N/A",
                        previewInfo.packageUrl ?? "N/A",
                        previewInfo.buildProgress,
                        previewInfo.downloadProgress,
                        previewInfo.categoryName ?? "N/A");
                }
                return r;
            }
        }
    }

    /**
     *  An asset store asset selection.
     *
     *  This class works as a singleton for keeping the list of selected
     *  asset store assets. This is not handled by the normal select framework
     *  because an asset store asset does not have an instanceID since it is not
     *  actually an asset in the local project.
     *
     *  Currently there is only support for handling a single selected asset store
     *  asset at a time. This class is somewhat prepared for multiple asset store asset
     *  selections though.
     */
    internal static class AssetStoreAssetSelection
    {
        public delegate void AssetsRefreshed();

        static internal Dictionary<int, AssetStoreAsset> s_SelectedAssets;

        public static void AddAsset(AssetStoreAsset searchResult, Texture2D placeholderPreviewImage)
        {
            if (placeholderPreviewImage != null)
                searchResult.previewImage = ScaleImage(placeholderPreviewImage, 256, 256);

            searchResult.previewInfo = null;
            searchResult.previewBundleRequest = null;

            // Dynamic previews is asset bundles to be displayed in
            // the inspector. Static previews are images.
            if (!string.IsNullOrEmpty(searchResult.dynamicPreviewURL) && searchResult.previewBundle == null)
            {
                // Debug.Log("dyn url " + searchResult.disposed.ToString() + " " + searchResult.dynamicPreviewURL);
                searchResult.disposed = false;
                // searchResult.previewBundle = AssetBundle.CreateFromFile("/users/jonasd/test.unity3d");
                // searchResult.previewAsset = searchResult.previewBundle.mainAsset;

                // Request the asset bundle data from the url and register a callback
                AsyncHTTPClient client = new AsyncHTTPClient(searchResult.dynamicPreviewURL);
                client.doneCallback = delegate(IAsyncHTTPClient c) {
                    if (!client.IsSuccess())
                    {
                        System.Console.WriteLine("Error downloading dynamic preview: " + client.text);
                        // Try the static preview instead
                        searchResult.dynamicPreviewURL = null;
                        DownloadStaticPreview(searchResult);
                        return;
                    }

                    // We only suppport one asset so grab the first one
                    AssetStoreAsset sel = GetFirstAsset();

                    // Make sure that the selection hasn't changed meanwhile
                    if (searchResult.disposed || sel == null || searchResult.id != sel.id)
                    {
                        //Debug.Log("dyn disposed " + searchResult.disposed.ToString() + " " + (sel == null ? "null" : sel.id.ToString()) + " " + searchResult.id.ToString());
                        return;
                    }

                    // Go create the asset bundle in memory from the binary blob asynchronously
                    try
                    {
                        AssetBundleCreateRequest cr = AssetBundle.LoadFromMemoryAsync(c.bytes);

                        // Workaround: Don't subject the bundle to the usual compatibility checks.  We want
                        // to stay compatible with previews created in prior versions of Unity and with the
                        // stuff we put into previews, we should generally be able to still load the content
                        // in the editor.
                        cr.DisableCompatibilityChecks();

                        searchResult.previewBundleRequest = cr;
                        EditorApplication.CallbackFunction callback = null;

                        // The callback will be called each tick and check if the asset bundle is ready
                        double startTime = EditorApplication.timeSinceStartup;
                        callback = () => {
                            AssetStoreUtils.UpdatePreloading();

                            if (!cr.isDone)
                            {
                                double nowTime = EditorApplication.timeSinceStartup;
                                if (nowTime - startTime > 10.0)
                                {
                                    // Timeout. Stop polling
                                    EditorApplication.update -= callback;
                                    System.Console.WriteLine("Timed out fetch live preview bundle " +
                                        (searchResult.dynamicPreviewURL ?? "<n/a>"));
                                    // Debug.Log("Not done Timed out" + cr.progress.ToString() );
                                }
                                else
                                {
                                    // Debug.Log("Not done " + cr.progress.ToString() );
                                }
                                return;
                            }

                            // Done cooking. Stop polling.
                            EditorApplication.update -= callback;

                            // Make sure that the selection hasn't changed meanwhile
                            AssetStoreAsset sel2 = GetFirstAsset();
                            if (searchResult.disposed || sel2 == null || searchResult.id != sel2.id)
                            {
                                // No problem. Just ignore.
                                // Debug.Log("dyn late disposed " + searchResult.disposed.ToString() + " " + (sel2 == null ? "null" : sel2.id.ToString()) + " " + searchResult.id.ToString());
                            }
                            else
                            {
                                searchResult.previewBundle = cr.assetBundle;
#pragma warning disable 618
                                if (cr.assetBundle == null ||  cr.assetBundle.mainAsset == null)
                                {
                                    // Failed downloading live preview. Fallback to static
                                    searchResult.dynamicPreviewURL = null;
                                    DownloadStaticPreview(searchResult);
                                }
                                else
                                    searchResult.previewAsset = searchResult.previewBundle.mainAsset;
#pragma warning restore 618
                            }
                        };

                        EditorApplication.update += callback;
                    }
                    catch (System.Exception e)
                    {
                        System.Console.Write(e.Message);
                        Debug.Log(e.Message);
                    }
                };
                client.Begin();
            }
            else if (!string.IsNullOrEmpty(searchResult.staticPreviewURL))
            {
                DownloadStaticPreview(searchResult);
            }

            // searchResult.previewBundle = null;
            AddAssetInternal(searchResult);

            RefreshFromServer(null);
        }

        // Also used by AssetStoreToolUtils
        internal static void AddAssetInternal(AssetStoreAsset searchResult)
        {
            if (s_SelectedAssets == null)
                s_SelectedAssets = new Dictionary<int, AssetStoreAsset>();
            s_SelectedAssets[searchResult.id] = searchResult;
        }

        static void DownloadStaticPreview(AssetStoreAsset searchResult)
        {
            AsyncHTTPClient client = new AsyncHTTPClient(searchResult.staticPreviewURL);
            client.doneCallback = delegate(IAsyncHTTPClient c) {
                if (!client.IsSuccess())
                {
                    System.Console.WriteLine("Error downloading static preview: " + client.text);
                    // Debug.LogError("Error downloading static preview: " + client.text);
                    return;
                }

                // Need to put the texture through some scaling magic in order for the
                // TextureInspector to be able to show it.
                // TODO: This is a workaround and should be fixed.
                Texture2D srcTex = c.texture;
                Texture2D tex = new Texture2D(srcTex.width, srcTex.height, TextureFormat.RGB24, false, true);
                AssetStorePreviewManager.ScaleImage(tex.width, tex.height, srcTex, tex, null);
                // tex.Compress(true);
                searchResult.previewImage = tex;

                Object.DestroyImmediate(srcTex);
                AssetStoreAssetInspector.Instance.Repaint();
            };
            client.Begin();
        }

        // Refresh information about displayed asset by quering the
        // asset store server. This is typically after the user has
        // logged in because we need to know if he already owns the
        // displayed asset.
        public static void RefreshFromServer(AssetsRefreshed callback)
        {
            if (s_SelectedAssets.Count == 0)
                return;

            // Refetch assetInfo
            // Query the asset store for more info
            List<AssetStoreAsset> queryAssets = new List<AssetStoreAsset>();
            foreach (KeyValuePair<int, AssetStoreAsset> qasset in s_SelectedAssets)
                queryAssets.Add(qasset.Value);

            // This will fill the queryAssets with extra preview data
            AssetStoreClient.AssetsInfo(queryAssets,
                delegate(AssetStoreAssetsInfo results) {
                    AssetStoreAssetInspector.paymentAvailability = AssetStoreAssetInspector.PaymentAvailability.ServiceDisabled;
                    if (!string.IsNullOrEmpty(results.error))
                    {
                        System.Console.WriteLine("Error performing Asset Store Info search: " + results.error);
                        AssetStoreAssetInspector.OfflineNoticeEnabled = true;
                        //Debug.LogError("Error performing Asset Store Info search: " + results.error);
                        if (callback != null) callback();
                        return;
                    }
                    AssetStoreAssetInspector.OfflineNoticeEnabled = false;

                    if (results.status == AssetStoreAssetsInfo.Status.Ok)
                        AssetStoreAssetInspector.paymentAvailability = AssetStoreAssetInspector.PaymentAvailability.Ok;
                    else if (results.status == AssetStoreAssetsInfo.Status.BasketNotEmpty)
                        AssetStoreAssetInspector.paymentAvailability = AssetStoreAssetInspector.PaymentAvailability.BasketNotEmpty;
                    else if (results.status == AssetStoreAssetsInfo.Status.AnonymousUser)
                        AssetStoreAssetInspector.paymentAvailability = AssetStoreAssetInspector.PaymentAvailability.AnonymousUser;

                    AssetStoreAssetInspector.s_PurchaseMessage = results.message;
                    AssetStoreAssetInspector.s_PaymentMethodCard = results.paymentMethodCard;
                    AssetStoreAssetInspector.s_PaymentMethodExpire = results.paymentMethodExpire;
                    AssetStoreAssetInspector.s_PriceText = results.priceText;

                    AssetStoreAssetInspector.Instance.Repaint();
                    if (callback != null) callback();
                });
        }

        private static Texture2D ScaleImage(Texture2D source, int w, int h)
        {
            // Bug: When scaling down things look weird unless the source size is
            //      == 0 when mod 4. Therefore we just return null if that's the case.
            if (source.width % 4 != 0)
                return null;

            Texture2D result = new Texture2D(w, h, TextureFormat.RGB24, false, true);
            Color[] rpixels = result.GetPixels(0);

            double dx = 1.0 / (double)w;
            double dy = 1.0 / (double)h;
            double x = 0;
            double y = 0;
            int idx = 0;
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++, idx++)
                {
                    rpixels[idx] = source.GetPixelBilinear((float)x, (float)y);
                    x += dx;
                }
                x = 0;
                y += dy;
            }
            result.SetPixels(rpixels, 0);
            result.Apply();
            return result;
        }

        public static bool ContainsAsset(int id)
        {
            return s_SelectedAssets != null && s_SelectedAssets.ContainsKey(id);
        }

        public static void Clear()
        {
            if (s_SelectedAssets == null)
                return;
            foreach (var kv in s_SelectedAssets)
                kv.Value.Dispose();

            s_SelectedAssets.Clear();
        }

        public static int Count
        {
            get { return s_SelectedAssets == null ? 0 : s_SelectedAssets.Count; }
        }

        public static bool Empty
        {
            get { return s_SelectedAssets == null ? true : s_SelectedAssets.Count == 0; }
        }

        public static AssetStoreAsset GetFirstAsset()
        {
            if (s_SelectedAssets == null)
                return null;
            var i = s_SelectedAssets.GetEnumerator();
            if (!i.MoveNext())
                return null;
            return i.Current.Value;
        }
    }

    /**
     *  In addition to being an inspector for AssetStoreAssets this
     *  inspector works as the object that is selected when an
     *  asset store asset is selected ie.
     *  Selection.object == AssetStoreAssetInspector.Instance
     *  when asset store assets are selected.
     */
    [CustomEditor(typeof(AssetStoreAssetInspector))]
    internal class AssetStoreAssetInspector : Editor
    {
        static AssetStoreAssetInspector s_SharedAssetStoreAssetInspector;

        public static AssetStoreAssetInspector Instance
        {
            get
            {
                if (s_SharedAssetStoreAssetInspector == null)
                {
                    s_SharedAssetStoreAssetInspector = ScriptableObject.CreateInstance<AssetStoreAssetInspector>();
                    s_SharedAssetStoreAssetInspector.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_SharedAssetStoreAssetInspector;
            }
        }

        class Styles
        {
            public GUIStyle link = new GUIStyle(EditorStyles.label);
            public Styles()
            {
                link.normal.textColor = new Color(.26f, .51f, .75f, 1f);
            }
        }

        static Styles styles;

        bool packageInfoShown = true;

        // Payment info for all selected assets
        internal static string s_PurchaseMessage = "";
        internal static string s_PaymentMethodCard = "";
        internal static string s_PaymentMethodExpire = "";
        internal static string s_PriceText = "";
        static GUIContent[] sStatusWheel;

        public static bool OfflineNoticeEnabled { get; set; }

        // Asset store payment availability
        internal enum PaymentAvailability
        {
            BasketNotEmpty,
            ServiceDisabled,
            AnonymousUser,
            Ok
        }

        internal static PaymentAvailability m_PaymentAvailability;
        internal static PaymentAvailability paymentAvailability
        {
            get
            {
                if (AssetStoreClient.LoggedOut())
                    m_PaymentAvailability = PaymentAvailability.AnonymousUser;
                return m_PaymentAvailability;
            }
            set
            {
                if (AssetStoreClient.LoggedOut())
                    m_PaymentAvailability = PaymentAvailability.AnonymousUser;
                else
                    m_PaymentAvailability = value;
            }
        }

        int lastAssetID;

        // Callback for curl to call
        public void OnDownloadProgress(string id, string message, int bytes, int total)
        {
            AssetStoreAsset activeAsset = AssetStoreAssetSelection.GetFirstAsset();
            if (activeAsset == null) return;
            AssetStoreAsset.PreviewInfo info = activeAsset.previewInfo;
            if (info == null) return;

            if (activeAsset.packageID.ToString() != id)
                return;

            if ((message == "downloading" || message == "connecting") && !OfflineNoticeEnabled)
            {
                info.downloadProgress = (float)bytes / (float)total;
            }
            else
            {
                info.downloadProgress = -1f;
            }
            Repaint();
        }

        public void Update()
        {
            // Repaint if asset has changed.
            // This has to be done here because the .target is always set to
            // this inspector when inspecting asset store assets.
            AssetStoreAsset a = AssetStoreAssetSelection.GetFirstAsset();
            bool hasProgress = a != null && a.previewInfo != null && (a.previewInfo.buildProgress >= 0f || a.previewInfo.downloadProgress >= 0f);
            if ((a == null && lastAssetID != 0) ||
                (a != null && lastAssetID != a.id) ||
                hasProgress)
            {
                lastAssetID = a == null ? 0 : a.id;
                Repaint();
            }

            // Repaint when the main asset of a possibly downloaded bundle is ready for preview
            if (a != null && a.previewBundle != null)
            {
                a.previewBundle.Unload(false);
                a.previewBundle = null;
                Repaint();
            }
        }

        public override void OnInspectorGUI()
        {
            if (styles == null)
            {
                // Set the singleton in case the DrawEditors() has created this window
                s_SharedAssetStoreAssetInspector = this;
                styles = new Styles();
            }

            AssetStoreAsset activeAsset = AssetStoreAssetSelection.GetFirstAsset();
            AssetStoreAsset.PreviewInfo info = null;
            if (activeAsset != null)
                info = activeAsset.previewInfo;

            if (activeAsset != null)
                target.name = string.Format("Asset Store: {0}", activeAsset.name);
            else
                target.name = "Asset Store";

            EditorGUILayout.BeginVertical();

            bool guiEnabled = GUI.enabled;

            GUI.enabled = activeAsset != null && activeAsset.packageID != 0;

            if (OfflineNoticeEnabled)
            {
                Color col = GUI.color;
                GUI.color = Color.yellow;
                GUILayout.Label("Network is offline");
                GUI.color = col;
            }

            if (activeAsset != null)
            {
                string typeName = activeAsset.className == null ? "" : activeAsset.className.Split(new char[] {' '}, 2)[0];
                bool isPackage = activeAsset.id == -activeAsset.packageID;
                if (isPackage)
                    typeName = "Package";
                if (activeAsset.HasLivePreview)
                    typeName = activeAsset.Preview.GetType().Name;
                EditorGUILayout.LabelField("Type", typeName);

                if (isPackage)
                {
                    packageInfoShown = true;
                }
                else
                {
                    EditorGUILayout.Separator();
                    packageInfoShown = EditorGUILayout.Foldout(packageInfoShown , "Part of package", true);
                }
                if (packageInfoShown)
                {
                    EditorGUILayout.LabelField("Name", info == null ? "-" : info.packageName);
                    EditorGUILayout.LabelField("Version", info == null ? "-" : info.packageVersion);
                    string price = info == null ? "-" : (!string.IsNullOrEmpty(activeAsset.price) ? activeAsset.price : "free");
                    EditorGUILayout.LabelField("Price", price);
                    string rating = info != null && info.packageRating >= 0 ? info.packageRating + " of 5" : "-";
                    EditorGUILayout.LabelField("Rating", rating);
                    EditorGUILayout.LabelField("Size", info == null ? "-" : intToSizeString(info.packageSize));
                    string assetCount = info != null && info.packageAssetCount >= 0 ? info.packageAssetCount.ToString() : "-";
                    EditorGUILayout.LabelField("Asset count", assetCount);
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Web page");
                    bool hasPageUrl = info != null && info.packageShortUrl != null && info.packageShortUrl != "";
                    bool guiBefore = GUI.enabled;
                    GUI.enabled = hasPageUrl;

                    if (GUILayout.Button(hasPageUrl ? new GUIContent(info.packageShortUrl, "View in browser") : EditorGUIUtility.TempContent("-"), styles.link))
                    {
                        Application.OpenURL(info.packageShortUrl);
                    }
                    if (GUI.enabled)
                        EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                    GUI.enabled = guiBefore;
                    GUILayout.EndHorizontal();
                    EditorGUILayout.LabelField("Publisher", info == null ? "-" : info.publisherName);
                }

                if (activeAsset.id != 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Open Asset Store", GUILayout.Height(40), GUILayout.Width(120)))
                    {
                        OpenItemInAssetStore(activeAsset);
                        GUIUtility.ExitGUI();
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUILayout.FlexibleSpace();
            }
            EditorWrapper editor = previewEditor;
            if (editor != null && activeAsset != null && activeAsset.HasLivePreview)
                editor.OnAssetStoreInspectorGUI();

            GUI.enabled = guiEnabled;

            EditorGUILayout.EndVertical();
        }

        public static void OpenItemInAssetStore(AssetStoreAsset activeAsset)
        {
            if (activeAsset.id != 0)
            {
                AssetStore.Open(string.Format("content/{0}?assetID={1}", activeAsset.packageID, activeAsset.id));
            }
        }

        private static string intToSizeString(int inValue)
        {
            if (inValue < 0)
                return "unknown";
            float val = (float)inValue;
            string[] scale = new string[] { "TB", "GB", "MB", "KB", "Bytes" };
            int idx = scale.Length - 1;
            while (val > 1000.0f && idx >= 0)
            {
                val /= 1000f;
                idx--;
            }

            if (idx < 0)
                return "<error>";

            return UnityString.Format("{0:#.##} {1}", val, scale[idx]);
        }

        public override bool HasPreviewGUI()
        {
            return (target != null && AssetStoreAssetSelection.Count != 0);
        }

        EditorWrapper m_PreviewEditor;
        Object m_PreviewObject;

        public void OnEnable()
        {
            EditorApplication.update += Update;
            AssetStoreUtils.RegisterDownloadDelegate(this);
        }

        public void OnDisable()
        {
            EditorApplication.update -= Update;
            if (m_PreviewEditor != null)
            {
                m_PreviewEditor.Dispose();
                m_PreviewEditor = null;
            }
            if (m_PreviewObject != null)
                m_PreviewObject = null;
            AssetStoreUtils.UnRegisterDownloadDelegate(this);
        }

        private EditorWrapper previewEditor
        {
            get
            {
                AssetStoreAsset asset = AssetStoreAssetSelection.GetFirstAsset();
                if (asset == null) return null;
                Object preview = asset.Preview;
                if (preview == null) return null;

                if (preview != m_PreviewObject)
                {
                    m_PreviewObject = preview;

                    if (m_PreviewEditor != null)
                        m_PreviewEditor.Dispose();

                    m_PreviewEditor = EditorWrapper.Make(m_PreviewObject, EditorFeatures.PreviewGUI);
                }

                return m_PreviewEditor;
            }
        }

        public override void OnPreviewSettings()
        {
            AssetStoreAsset asset = AssetStoreAssetSelection.GetFirstAsset();
            if (asset == null) return;

            EditorWrapper editor = previewEditor;
            if (editor != null && asset.HasLivePreview)
                editor.OnPreviewSettings();
        }

        public override string GetInfoString()
        {
            EditorWrapper editor = previewEditor;
            AssetStoreAsset a = AssetStoreAssetSelection.GetFirstAsset();
            if (a == null)
                return "No item selected";

            if (editor != null && a.HasLivePreview)
                return editor.GetInfoString();

            return "";
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (m_PreviewObject == null) return;
            EditorWrapper editor = previewEditor;

            // Special handling for animation clips because they only have
            // an interactive preview available which shows play button etc.
            // The OnPreviewGUI is also used for the small icons in the top
            // of the inspectors where buttons should not be rendered.

            if (editor != null && m_PreviewObject is AnimationClip)
                editor.OnPreviewGUI(r, background); // currently renders nothing for animation clips
            else
                OnInteractivePreviewGUI(r, background);
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            EditorWrapper editor = previewEditor;
            if (editor != null)
            {
                editor.OnInteractivePreviewGUI(r, background);
            }

            // If the live preview is not available yes the show a spinner
            AssetStoreAsset a = AssetStoreAssetSelection.GetFirstAsset();
            if (a != null && !a.HasLivePreview && !string.IsNullOrEmpty(a.dynamicPreviewURL))
            {
                GUIContent c = StatusWheel;
                r.y += (r.height - c.image.height) / 2f;
                r.x += (r.width - c.image.width) / 2f;
                GUI.Label(r, StatusWheel);
                Repaint();
            }
        }

        static GUIContent StatusWheel
        {
            get
            {
                if (sStatusWheel == null)
                {
                    sStatusWheel = new GUIContent[12];
                    for (int i = 0; i < 12; i++)
                    {
                        GUIContent gc = new GUIContent();
                        gc.image = EditorGUIUtility.LoadIcon("WaitSpin" + i.ToString("00")) as Texture2D;
                        sStatusWheel[i] = gc;
                    }
                }
                int frame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 10, 11.99f);
                return sStatusWheel[frame];
            }
        }

        public override GUIContent GetPreviewTitle()
        {
            return GUIContent.Temp("Asset Store Preview");
        }
    } // Inspector class
}
