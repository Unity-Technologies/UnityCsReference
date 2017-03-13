// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
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
                    string price = info == null ? "-" : (activeAsset.price != null && activeAsset.price != "" ? activeAsset.price : "free");
                    EditorGUILayout.LabelField("Price", price);
                    string rating = info != null && info.packageRating >= 0 ? info.packageRating.ToString() + " of 5" : "-";
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

                    string actionLabel;
                    if (info != null && info.isDownloadable)
                        actionLabel = "Import package";
                    else
                        actionLabel = "Buy for " + activeAsset.price;

                    bool lastEnabled = GUI.enabled;
                    bool building = info != null && info.buildProgress >= 0;
                    bool downloading = info != null && info.downloadProgress >= 0;
                    if (building || downloading || info == null)
                    {
                        actionLabel = "";
                        GUI.enabled = false;
                    }

                    if (GUILayout.Button(actionLabel, GUILayout.Height(40), GUILayout.Width(120)))
                    {
                        if (info.isDownloadable)
                            ImportPackage(activeAsset);
                        else
                            InitiateBuySelected();
                        GUIUtility.ExitGUI();
                    }

                    Rect r;
                    if (Event.current.type == EventType.Repaint)
                    {
                        r = GUILayoutUtility.GetLastRect();
                        r.height -= 4;
                        float width = r.width;
                        r.width = r.height;
                        r.y += 2;
                        r.x += 2;

                        if (building || downloading)
                        {
                            r.width = width - r.height - 4;
                            r.x += r.height;
                            EditorGUI.ProgressBar(r,
                                downloading ? info.downloadProgress : info.buildProgress,
                                downloading ? "Downloading" : "Building");
                        }
                    }

                    GUI.enabled = lastEnabled;
                    GUILayout.Space(4);

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
                UsabilityAnalytics.Track(string.Format("/AssetStore/ViewInStore/{0}/{1}", activeAsset.packageID, activeAsset.id));
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

            return string.Format("{0:#.##} {1}", val, scale[idx]);
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

        /**
         * Initiate a purchase of the selected package from the asset store.
         * The package will be the one that contains the currently selected asset.
         *
         * Since not all users may have allowed for single click purchases this can result
         * in two scenarios:
         * 1. single click allowed and a native purchase acknowledgement dialog appears to
         *    finalize the purchase.
         * 2. single click is not allowed and the package is put to an asset store basket.
         *    Then the asset store window is displayed with the basket open.
         */
        void InitiateBuySelected(bool firstAttempt)
        {
            //Debug.Log("payavail " + paymentAvailability.ToString());
            // Ask the asset store if the use has allowed single click payments
            AssetStoreAsset asset = AssetStoreAssetSelection.GetFirstAsset();
            if (asset == null)
            {
                EditorUtility.DisplayDialog("No asset selected", "Please select asset before buying a package", "ok");
            }
            else if (paymentAvailability == PaymentAvailability.AnonymousUser)
            {
                // Maybe the asset store window did a login already and we have a session key
                // then we just need to fetch the new info
                if (AssetStoreClient.LoggedIn())
                {
                    AssetStoreAssetSelection.RefreshFromServer(delegate() {
                            InitiateBuySelected(false);
                        });
                }
                else if (firstAttempt) LoginAndInitiateBuySelected();
            }
            else if (paymentAvailability == PaymentAvailability.ServiceDisabled)
            {
                // Use the asset store window to complete the purchase since single click is not possible
                if (asset.previewInfo == null) return;
                AssetStore.Open(string.Format("content/{0}/directpurchase", asset.packageID));
            }
            else if (paymentAvailability == PaymentAvailability.BasketNotEmpty)
            {
                // Use the asset store window to complete the purchase since there is already \
                // something in the users basket
                if (asset.previewInfo == null) return;

                // Maybe the basket has been emptied and this is a retry by the user
                if (firstAttempt)
                    AssetStoreAssetSelection.RefreshFromServer(delegate() {
                            InitiateBuySelected(false);
                        });
                else
                    AssetStore.Open(string.Format("content/{0}/basketpurchase", asset.packageID));
            }
            else
            {
                // Show single click window
                AssetStoreInstaBuyWindow.ShowAssetStoreInstaBuyWindow(asset,
                    AssetStoreAssetInspector.s_PurchaseMessage,
                    AssetStoreAssetInspector.s_PaymentMethodCard,
                    AssetStoreAssetInspector.s_PaymentMethodExpire,
                    AssetStoreAssetInspector.s_PriceText
                    );
            }
        }

        void InitiateBuySelected()
        {
            InitiateBuySelected(true);
        }

        void LoginAndInitiateBuySelected()
        {
            AssetStoreLoginWindow.Login("Please login to the Asset Store in order to get payment information about the package.",
                delegate(string errorMessage) {
                    if (errorMessage != null)
                        return; // aborted

                    AssetStoreAssetSelection.RefreshFromServer(delegate() {
                        InitiateBuySelected(false);
                    });
                });
        }

        void ImportPackage(AssetStoreAsset asset)
        {
            if (paymentAvailability == PaymentAvailability.AnonymousUser)
                LoginAndImport(asset);
            else
                AssetStoreInstaBuyWindow.ShowAssetStoreInstaBuyWindowBuilding(asset);
        }

        void LoginAndImport(AssetStoreAsset asset)
        {
            AssetStoreLoginWindow.Login("Please login to the Asset Store in order to get download information for the package.",
                delegate(string errorMessage) {
                    if (errorMessage != null)
                        return; // aborted

                    AssetStoreAssetSelection.RefreshFromServer(delegate() {
                        AssetStoreInstaBuyWindow.ShowAssetStoreInstaBuyWindowBuilding(asset);
                    });
                });
        }
    } // Inspector class
} // UnityEditor namespace
