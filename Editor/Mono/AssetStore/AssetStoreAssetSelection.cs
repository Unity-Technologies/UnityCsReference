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
                client.doneCallback = delegate(AsyncHTTPClient c) {
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
                                    if (cr.assetBundle == null ||  cr.assetBundle.mainAsset == null)
                                    {
                                        // Failed downloading live preview. Fallback to static
                                        searchResult.dynamicPreviewURL = null;
                                        DownloadStaticPreview(searchResult);
                                    }
                                    else
                                        searchResult.previewAsset = searchResult.previewBundle.mainAsset;
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
            client.doneCallback = delegate(AsyncHTTPClient c) {
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
                    if (results.error != null && results.error != "")
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
} // UnityEditor namespace
