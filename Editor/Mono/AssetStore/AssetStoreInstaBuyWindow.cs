// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor
{
    /**
     * A dialog that lists packages to purchase and
     * a password in order to acknowledge purchase.
     * Will should purchase success or failure and
     * the option to import package on successful purchase.
     */
    internal class AssetStoreInstaBuyWindow : EditorWindow
    {
        const int kStandardHeight = 160;

        enum PurchaseStatus
        {
            Init,
            InProgress,
            Declined,
            Complete,
            StartBuild,
            Building,
            Downloading
        };

        // Open the dialog to allow the user to purchase the asset
        static public AssetStoreInstaBuyWindow ShowAssetStoreInstaBuyWindow(AssetStoreAsset asset, string purchaseMessage, string paymentMethodCard, string paymentMethodExpire, string priceText)
        {
            AssetStoreInstaBuyWindow w = EditorWindow.GetWindowWithRect<AssetStoreInstaBuyWindow>(new Rect(100, 100, 400, kStandardHeight), true, "Buy package from Asset Store");
            if (w.m_Purchasing != PurchaseStatus.Init)
            {
                EditorUtility.DisplayDialog("Download in progress", "There is already a package download in progress. You can only have one download running at a time", "Close");
                return w;
            }

            w.position = new Rect(100, 100, 400, kStandardHeight);
            w.m_Parent.window.m_DontSaveToLayout = true;
            w.m_Asset = asset;
            w.m_Password = "";
            w.m_Message = "";
            w.m_Purchasing = PurchaseStatus.Init;
            w.m_NextAllowedBuildRequestTime = 0.0;
            w.m_BuildAttempts = 0;

            w.m_PurchaseMessage = purchaseMessage;
            w.m_PaymentMethodCard = paymentMethodCard;
            w.m_PaymentMethodExpire = paymentMethodExpire;
            w.m_PriceText = priceText;
            UsabilityAnalytics.Track(string.Format("/AssetStore/ShowInstaBuy/{0}/{1}", w.m_Asset.packageID, w.m_Asset.id));
            return w;
        }

        // For free or already purchased packages this will open the dialog
        // and just import the package.
        static public void ShowAssetStoreInstaBuyWindowBuilding(AssetStoreAsset asset)
        {
            AssetStoreInstaBuyWindow w = ShowAssetStoreInstaBuyWindow(asset, "", "", "", "");
            if (w.m_Purchasing != PurchaseStatus.Init)
            {
                EditorUtility.DisplayDialog("Download in progress", "There is already a package download in progress. You can only have one download running at a time", "Close");
                return;
            }
            w.m_Purchasing = PurchaseStatus.StartBuild;
            w.m_BuildAttempts = 1;
            asset.previewInfo.buildProgress = 0f;
            UsabilityAnalytics.Track(string.Format("/AssetStore/ShowInstaFree/{0}/{1}", w.m_Asset.packageID, w.m_Asset.id));
        }

        private static GUIContent s_AssetStoreLogo;

        private static void LoadLogos()
        {
            if (s_AssetStoreLogo != null)
                return;
            s_AssetStoreLogo = EditorGUIUtility.IconContent("WelcomeScreen.AssetStoreLogo");
        }

        private string m_Password = "";
        private AssetStoreAsset m_Asset = null;
        private string m_Message = "";
        PurchaseStatus m_Purchasing = PurchaseStatus.Init;
        private double m_NextAllowedBuildRequestTime = 0.0;
        private const double kBuildPollInterval = 2.0;
        private const int kMaxPolls = 5 * 30;
        private int m_BuildAttempts = 0; //  5 minutes

        private string m_PurchaseMessage = null;
        private string m_PaymentMethodCard = null;
        private string m_PaymentMethodExpire = null;
        private string m_PriceText = null;

        public void OnInspectorUpdate()
        {
            if (m_Purchasing == PurchaseStatus.StartBuild &&
                m_NextAllowedBuildRequestTime <= EditorApplication.timeSinceStartup)
            {
                m_NextAllowedBuildRequestTime = EditorApplication.timeSinceStartup + kBuildPollInterval;
                BuildPackage();
            }
        }

        void OnEnable()
        {
            AssetStoreUtils.RegisterDownloadDelegate(this);
        }

        public void OnDisable()
        {
            AssetStoreAsset.PreviewInfo info = m_Asset == null ? null : m_Asset.previewInfo;
            if (info != null)
            {
                info.downloadProgress = -1f;
                info.buildProgress = -1;
            }
            AssetStoreUtils.UnRegisterDownloadDelegate(this);
            m_Purchasing = PurchaseStatus.Init;
        }

        // Callback handler for internal curl
        public void OnDownloadProgress(string id, string message, int bytes, int total)
        {
            AssetStoreAsset.PreviewInfo info = m_Asset == null ? null : m_Asset.previewInfo;
            if (info == null || m_Asset.packageID.ToString() != id)
                return;

            if (message == "downloading" || message == "connecting")
            {
                info.downloadProgress = (float)bytes / (float)total;
            }
            else
            {
                info.downloadProgress = -1f;
            }
            Repaint();
        }

        public void OnGUI()
        {
            LoadLogos();
            if (m_Asset == null)
                return;

            // Currently we only support selling a single asset at a time.
            GUILayout.BeginVertical();
            GUILayout.Space(10);

            switch (m_Purchasing)
            {
                case PurchaseStatus.Init:
                    PasswordGUI();
                    break;
                case PurchaseStatus.InProgress:
                    if (m_Purchasing == PurchaseStatus.InProgress)
                        GUI.enabled = false;
                    PasswordGUI();
                    break;
                case PurchaseStatus.Declined:
                    PurchaseDeclinedGUI();
                    break;
                case PurchaseStatus.Complete:
                    PurchaseSuccessGUI();
                    break;
                case PurchaseStatus.StartBuild:
                case PurchaseStatus.Building:
                case PurchaseStatus.Downloading:
                    DownloadingGUI();
                    break;
            }

            GUILayout.EndVertical();
        }

        void PasswordGUI()
        {
            AssetStoreAsset.PreviewInfo item = m_Asset.previewInfo;
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label(s_AssetStoreLogo, GUIStyle.none, GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            GUILayout.Label("Complete purchase by entering your AssetStore password", EditorStyles.boldLabel);
            bool hasMessage = m_PurchaseMessage != null && m_PurchaseMessage != "";
            bool hasErrorMessage = m_Message != null && m_Message != "";
            float newHeight = kStandardHeight + (hasMessage ? 20 : 0) + (hasErrorMessage ? 20 : 0);
            if (newHeight != position.height)
                position = new Rect(position.x, position.y, position.width, newHeight);

            if (hasMessage)
                GUILayout.Label(m_PurchaseMessage, EditorStyles.wordWrappedLabel);
            if (hasErrorMessage)
            {
                Color oldColor = GUI.color;
                GUI.color = Color.red;
                GUILayout.Label(m_Message, EditorStyles.wordWrappedLabel);
                GUI.color = oldColor;
            }
            GUILayout.Label("Package: " + item.packageName, EditorStyles.wordWrappedLabel);
            string cardInfo = string.Format("Credit card: {0} (expires {1})", m_PaymentMethodCard, m_PaymentMethodExpire);
            GUILayout.Label(cardInfo, EditorStyles.wordWrappedLabel);
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Amount", m_PriceText);
            m_Password = EditorGUILayout.PasswordField("Password", m_Password);
            GUILayout.EndVertical();
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            if (GUILayout.Button("Just put to basket..."))
            {
                AssetStore.Open(string.Format("content/{0}/basketpurchase", m_Asset.packageID));
                UsabilityAnalytics.Track(string.Format("/AssetStore/PutToBasket/{0}/{1}", m_Asset.packageID, m_Asset.id));
                m_Asset = null;
                this.Close();
                GUIUtility.ExitGUI();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel"))
            {
                UsabilityAnalytics.Track(string.Format("/AssetStore/CancelInstaBuy/{0}/{1}", m_Asset.packageID, m_Asset.id));
                m_Asset = null;
                this.Close();
                GUIUtility.ExitGUI();
            }
            GUILayout.Space(5);
            if (GUILayout.Button("Complete purchase"))
                CompletePurchase();

            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }

        void PurchaseSuccessGUI()
        {
            AssetStoreAsset.PreviewInfo item = m_Asset.previewInfo;
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label(s_AssetStoreLogo, GUIStyle.none, GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            GUILayout.Label("Purchase completed succesfully", EditorStyles.boldLabel);
            GUILayout.Label("You will receive a receipt in your email soon.");

            bool hasMessage = m_Message != null && m_Message != "";
            float newHeight = kStandardHeight + (hasMessage ? 20 : 0);
            if (newHeight != position.height)
                position = new Rect(position.x, position.y, position.width, newHeight);

            if (hasMessage)
                GUILayout.Label(m_Message, EditorStyles.wordWrappedLabel);

            GUILayout.Label("Package: " + item.packageName, EditorStyles.wordWrappedLabel);

            GUILayout.EndVertical();
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.Space(8);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close"))
            {
                UsabilityAnalytics.Track(string.Format("/AssetStore/PurchaseOk/{0}/{1}", m_Asset.packageID, m_Asset.id));
                m_Asset = null;
                this.Close();
            }
            GUILayout.Space(5);
            if (GUILayout.Button("Import package"))
            {
                UsabilityAnalytics.Track(string.Format("/AssetStore/PurchaseOkImport/{0}/{1}", m_Asset.packageID, m_Asset.id));
                m_BuildAttempts = 1;
                m_Asset.previewInfo.buildProgress = 0f;
                m_Purchasing = PurchaseStatus.StartBuild;
            }
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }

        void DownloadingGUI()
        {
            AssetStoreAsset.PreviewInfo item = m_Asset.previewInfo;
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label(s_AssetStoreLogo, GUIStyle.none, GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            string label = "Importing";     // m_Purchasing == PurchaseStatus.Downloading ? "Downloading" : "Building";
            GUILayout.Label(label, EditorStyles.boldLabel);
            GUILayout.Label("Package: " + item.packageName, EditorStyles.wordWrappedLabel);
            // TODO: show progress when multiple progress delegates works
            GUILayout.Label("    ");
            if (Event.current.type == EventType.Repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                r.height += 1;
                bool downloading = item.downloadProgress >= 0;
                EditorGUI.ProgressBar(r,
                    downloading ? item.downloadProgress : item.buildProgress,
                    downloading ? "Downloading" : "Building");
            }

            GUILayout.EndVertical();
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Abort"))
                this.Close();
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }

        void PurchaseDeclinedGUI()
        {
            AssetStoreAsset.PreviewInfo item = m_Asset.previewInfo;
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label(s_AssetStoreLogo, GUIStyle.none, GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            GUILayout.Label("Purchase declined", EditorStyles.boldLabel);
            GUILayout.Label("No money has been drawn from you credit card");

            bool hasMessage = m_Message != null && m_Message != "";
            float newHeight = kStandardHeight + (hasMessage ? 20 : 0);
            if (newHeight != position.height)
                position = new Rect(position.x, position.y, position.width, newHeight);

            if (hasMessage)
                GUILayout.Label(m_Message, EditorStyles.wordWrappedLabel);

            GUILayout.Label("Package: " + item.packageName, EditorStyles.wordWrappedLabel);

            GUILayout.EndVertical();
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.Space(8);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close"))
            {
                UsabilityAnalytics.Track(string.Format("/AssetStore/DeclinedAbort/{0}/{1}", m_Asset.packageID, m_Asset.id));
                m_Asset = null;
                Close();
            }
            GUILayout.Space(5);
            if (GUILayout.Button("Put to basket"))
            {
                AssetStore.Open(string.Format("content/{0}/basketpurchase", m_Asset.packageID));
                UsabilityAnalytics.Track(string.Format("/AssetStore/DeclinedPutToBasket/{0}/{1}", m_Asset.packageID, m_Asset.id));
                m_Asset = null;
                Close();
            }
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }

        void CompletePurchase()
        {
            m_Message = "";
            string pw = m_Password;
            m_Password = "";
            m_Purchasing = PurchaseStatus.InProgress;
            AssetStoreClient.DirectPurchase(m_Asset.packageID, pw, delegate(PurchaseResult result) {
                    m_Purchasing = PurchaseStatus.Init;
                    if (result.error != null)
                    {
                        m_Purchasing = PurchaseStatus.Declined;
                        m_Message = "An error occurred while completing you purhase.";
                        Close();
                    }

                    string msg = null;
                    switch (result.status)
                    {
                        case PurchaseResult.Status.BasketNotEmpty:
                            m_Message = "Something else has been put in our Asset Store basket while doing this purchase.";
                            m_Purchasing = PurchaseStatus.Declined;
                            break;
                        case PurchaseResult.Status.ServiceDisabled:
                            m_Message = "Single click purchase has been disabled while doing this purchase.";
                            m_Purchasing = PurchaseStatus.Declined;
                            break;
                        case PurchaseResult.Status.AnonymousUser:
                            m_Message = "You have been logged out from somewhere else while doing this purchase.";
                            m_Purchasing = PurchaseStatus.Declined;
                            break;
                        case PurchaseResult.Status.PasswordMissing:
                            m_Message = result.message;
                            Repaint();
                            break;
                        case PurchaseResult.Status.PasswordWrong:
                            m_Message = result.message;
                            Repaint();
                            break;
                        case PurchaseResult.Status.PurchaseDeclined:
                            m_Purchasing = PurchaseStatus.Declined;
                            if (result.message != null)
                                m_Message = result.message;
                            Repaint();
                            break;
                        case PurchaseResult.Status.Ok:
                            m_Purchasing = PurchaseStatus.Complete;
                            if (result.message != null)
                                m_Message = result.message;
                            Repaint();
                            break;
                    }
                    if (msg != null)
                        EditorUtility.DisplayDialog("Purchase failed", msg + " This purchase has been cancelled.",
                            "Add this item to basket", "Cancel");
                });
            UsabilityAnalytics.Track(string.Format("/AssetStore/InstaBuy/{0}/{1}", m_Asset.packageID, m_Asset.id));
        }

        void BuildPackage()
        {
            AssetStoreAsset.PreviewInfo item = m_Asset.previewInfo;
            if (item == null)
                return;

            if (m_BuildAttempts++ > kMaxPolls)
            {
                EditorUtility.DisplayDialog("Building timed out", "Timed out during building of package", "Close");
                Close();
                return;
            }

            item.downloadProgress = -1f;
            m_Purchasing = PurchaseStatus.Building;

            AssetStoreClient.BuildPackage(m_Asset, delegate(BuildPackageResult result) {
                    if (this == null) return; // aborted
                    if (result.error != null)
                    {
                        Debug.Log(result.error);
                        if (EditorUtility.DisplayDialog("Error building package",
                                "The server was unable to build the package. Please re-import.",
                                "Ok"))
                            Close();
                        return;
                    }

                    if (m_Asset == null || m_Purchasing != PurchaseStatus.Building || result.packageID != m_Asset.packageID)
                    {
                        // Aborted
                        Close();
                    }

                    string url = result.asset.previewInfo.packageUrl;
                    if (url != null && url != "")
                    {
                        DownloadPackage();
                    }
                    else
                    {
                        // Retry since package is not done building on server
                        m_Purchasing = PurchaseStatus.StartBuild;
                    }
                    Repaint();
                });
        }

        void DownloadPackage()
        {
            AssetStoreAsset.PreviewInfo item = m_Asset.previewInfo;
            m_Purchasing = PurchaseStatus.Downloading;
            item.downloadProgress = 0;
            item.buildProgress = -1f;

            AssetStoreContext.Download(m_Asset.packageID.ToString(), item.packageUrl, item.encryptionKey, item.packageName,
                item.publisherName, item.categoryName, delegate(string id, string status, int bytes, int total) {
                    if (this == null) return; // aborted
                    item.downloadProgress = -1f;
                    if (status != "ok")
                    {
                        Debug.LogErrorFormat("Error downloading package {0} ({1})", item.packageName, id);
                        Debug.LogError(status);
                        Close();
                        return;
                    }

                    if (m_Asset == null || m_Purchasing != PurchaseStatus.Downloading || id != m_Asset.packageID.ToString())
                    {
                        // Aborted
                        Close();
                    }

                    if (!AssetStoreContext.OpenPackageInternal(id))
                    {
                        Debug.LogErrorFormat("Error importing package {0} ({1})", item.packageName, id);
                    }
                    Close();
                });
        }
    }
} // namespace
