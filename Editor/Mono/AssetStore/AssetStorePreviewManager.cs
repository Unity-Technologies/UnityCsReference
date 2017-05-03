// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    /**
     * Fetching and caches preview images of asset store assets from the
     * asset store server
     */
    internal sealed class AssetStorePreviewManager
    {
        private AssetStorePreviewManager() {} // disallow instantiation

        static AssetStorePreviewManager s_SharedAssetStorePreviewManager = null;
        static RenderTexture s_RenderTexture = null;

        static internal AssetStorePreviewManager Instance
        {
            get
            {
                if (s_SharedAssetStorePreviewManager == null)
                {
                    s_SharedAssetStorePreviewManager = new AssetStorePreviewManager();
                    s_SharedAssetStorePreviewManager.m_DummyItem.lastUsed = -1;
                }
                return s_SharedAssetStorePreviewManager;
            }
        }

        public class CachedAssetStoreImage
        {
            private const double kFadeTime = 0.5;

            public Texture2D image;
            public double lastUsed; // time since app start
            public double lastFetched; // time since app start
            public int requestedWidth; //
            public string label; //
            internal AsyncHTTPClient client; // null if not in progress of fetching preview
            public Color color
            {
                get
                {
                    return Color.Lerp(new Color(1, 1, 1, 0), new Color(1, 1, 1, 1),
                        Mathf.Min(1f, (float)((EditorApplication.timeSinceStartup - lastFetched) / kFadeTime)));
                }
            }
        }


        // @TODO: MAKE handling of incoming images span several repaints in order to
        //        not have the jumpy behaviour. Just put the in a queue an handle them from there.
        //


        Dictionary<string, CachedAssetStoreImage> m_CachedAssetStoreImages;
        const double kQueryDelay = 0.2;
        const int kMaxConcurrentDownloads = 15;
        const int kMaxConvertionsPerTick = 1;
        int m_MaxCachedAssetStoreImages = 10;
        int m_Aborted = 0;
        int m_Success = 0;
        internal int Requested = 0;
        internal int CacheHit = 0;
        int m_CacheRemove = 0;
        int m_ConvertedThisTick = 0;
        CachedAssetStoreImage m_DummyItem = new CachedAssetStoreImage();

        static bool s_NeedsRepaint = false;

        static Dictionary<string, CachedAssetStoreImage> CachedAssetStoreImages
        {
            get
            {
                if (Instance.m_CachedAssetStoreImages == null)
                {
                    Instance.m_CachedAssetStoreImages = new Dictionary<string, CachedAssetStoreImage>();
                }
                return Instance.m_CachedAssetStoreImages;
            }
        }

        public static int MaxCachedImages
        {
            get { return Instance.m_MaxCachedAssetStoreImages; }
            set { Instance.m_MaxCachedAssetStoreImages  = value; }
        }

        public static bool CacheFull
        {
            get { return CachedAssetStoreImages.Count >= MaxCachedImages; }
        }

        public static int Downloading
        {
            get
            {
                int c = 0;
                foreach (KeyValuePair<string, CachedAssetStoreImage> kv in CachedAssetStoreImages)
                    if (kv.Value.client != null) c++;
                return c;
            }
        }

        public static string StatsString()
        {
            return string.Format("Reqs: {0}, Ok: {1}, Abort: {2}, CacheDel: {3}, Cache: {4}/{5}, CacheHit: {6}",
                Instance.Requested, Instance.m_Success, Instance.m_Aborted, Instance.m_CacheRemove,
                AssetStorePreviewManager.CachedAssetStoreImages.Count,
                Instance.m_MaxCachedAssetStoreImages,
                Instance.CacheHit);
        }

        /**
         * Return a texture from a url that points to an image resource
         *
         * This method does not block but queues a request to fetch the image and return null.
         * When the image has been fetched this method will return the image texture downloaded.
         */
        public static CachedAssetStoreImage TextureFromUrl(string url, string label, int textureSize, GUIStyle labelStyle, GUIStyle iconStyle, bool onlyCached)
        {
            if (string.IsNullOrEmpty(url))
                return Instance.m_DummyItem;

            CachedAssetStoreImage cached;
            bool newentry = true;

            if (CachedAssetStoreImages.TryGetValue(url, out cached))
            {
                cached.lastUsed = EditorApplication.timeSinceStartup;

                // Refetch the image if the size has changed and is not in the progress of being fetched
                bool refetchInitiated = cached.requestedWidth == textureSize;
                bool correctSize = cached.image != null && cached.image.width == textureSize;

                bool cacheRequestAborted = cached.requestedWidth == -1;

                if ((correctSize || refetchInitiated || onlyCached) && !cacheRequestAborted)
                {
                    Instance.CacheHit++;

                    // Use cached image (that may be null) if we're in progress of fetching the image
                    // or if we have rendered the images correctly
                    //return cached;
                    bool fetchingImage = cached.client != null;
                    bool labelDrawn = cached.label == null;
                    bool valid = fetchingImage || labelDrawn;
                    bool convPerTickExceeded = Instance.m_ConvertedThisTick > kMaxConvertionsPerTick;
                    s_NeedsRepaint = s_NeedsRepaint || convPerTickExceeded;
                    return (valid || convPerTickExceeded) ?
                        cached :
                        RenderEntry(cached, labelStyle, iconStyle);
                }
                //Debug.Log(string.Format("Found {0} {1} {2} {3}", correctSize, refetchInitiated, onlyCached, cacheRequestAborted));
                newentry = false;
                if (Downloading >= kMaxConcurrentDownloads) return cached.image == null ? Instance.m_DummyItem : cached;
            }
            else
            {
                if (onlyCached || Downloading >= kMaxConcurrentDownloads)   return Instance.m_DummyItem;
                cached = new CachedAssetStoreImage();
                cached.image = null;
                cached.lastUsed = EditorApplication.timeSinceStartup;
                //Debug.Log("url is " + textureSize.ToString() + " " + url);
            }

            // Only set fetch time when there is not image in order to use it for
            // fading in the image when it becomes available
            if (cached.image == null)
                cached.lastFetched = EditorApplication.timeSinceStartup;

            cached.requestedWidth = textureSize;
            cached.label = label;

            AsyncHTTPClient client = null;
            client = SetupTextureDownload(cached, url, "previewSize-" + textureSize);

            ExpireCacheEntries();

            if (newentry)
                CachedAssetStoreImages.Add(url, cached);

            client.Begin();

            Instance.Requested++;
            return cached;
        }

        private static AsyncHTTPClient SetupTextureDownload(CachedAssetStoreImage cached, string url, string tag)
        {
            AsyncHTTPClient client = new AsyncHTTPClient(url);
            cached.client = client;
            client.tag = tag;
            client.doneCallback = delegate(AsyncHTTPClient c) {
                    // Debug.Log("Got image " + EditorApplication.timeSinceStartup.ToString());
                    cached.client = null;
                    if (!client.IsSuccess())
                    {
                        if (client.state != AsyncHTTPClient.State.ABORTED)
                        {
                            string err = "error " + client.text + " " + client.state.ToString() + " '" + url + "'";
                            if (ObjectListArea.s_Debug)
                                Debug.LogError(err);
                            else
                                System.Console.Write(err);
                        }
                        else
                        {
                            Instance.m_Aborted++;
                        }
                        return;
                    }

                    // In the case of refetch because of resize first destroy the current image
                    if (cached.image != null)
                        Object.DestroyImmediate(cached.image);

                    cached.image = c.texture;

                    s_NeedsRepaint = true;
                    Instance.m_Success++;
                };
            return client;
        }

        // Make room for image if needed (because of cache limits)
        private static void ExpireCacheEntries()
        {
            while (CacheFull)
            {
                string oldestUrl = null;
                CachedAssetStoreImage oldestEntry = null;
                foreach (KeyValuePair<string, CachedAssetStoreImage> kv in CachedAssetStoreImages)
                {
                    if (oldestEntry == null || oldestEntry.lastUsed > kv.Value.lastUsed)
                    {
                        oldestEntry = kv.Value;
                        oldestUrl = kv.Key;
                    }
                }
                CachedAssetStoreImages.Remove(oldestUrl);
                Instance.m_CacheRemove++;
                if (oldestEntry == null)
                {
                    Debug.LogError("Null entry found while removing cache entry");
                    break;
                }
                if (oldestEntry.client != null)
                {
                    oldestEntry.client.Abort();
                    oldestEntry.client = null;
                }
                if (oldestEntry.image != null)
                    Object.DestroyImmediate(oldestEntry.image);
            }
        }

        /*
         * When the new GUI system is is place this should render the label to the cached icon
         * to speed up rendering. For now we just scale the incoming image and render the label
         * separately
         */
        private static CachedAssetStoreImage RenderEntry(CachedAssetStoreImage cached, GUIStyle labelStyle, GUIStyle iconStyle)
        {
            if (cached.label == null || cached.image == null) return cached;
            Texture2D tmp = cached.image;
            cached.image = new Texture2D(cached.requestedWidth, cached.requestedWidth, TextureFormat.RGB24, false, true);
            ScaleImage(cached.requestedWidth, cached.requestedWidth, tmp, cached.image, iconStyle);
            // Compressing creates artifacts on the images
            // cached.image.Compress(true);
            Object.DestroyImmediate(tmp);
            cached.label = null;
            Instance.m_ConvertedThisTick++;
            return cached;
        }

        /** Slow version of scaling an image but i doesn't
         *  matter since it is not performance critical
         */
        internal static void ScaleImage(int w, int h, Texture2D inimage, Texture2D outimage, GUIStyle bgStyle)
        {
            SavedRenderTargetState saved = new SavedRenderTargetState();

            if (s_RenderTexture != null && (s_RenderTexture.width != w || s_RenderTexture.height != h))
            {
                Object.DestroyImmediate(s_RenderTexture);
                s_RenderTexture = null;
            }

            if (s_RenderTexture == null)
            {
                s_RenderTexture = RenderTexture.GetTemporary(w, h, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                s_RenderTexture.hideFlags = HideFlags.HideAndDontSave;
            }

            RenderTexture r = s_RenderTexture;
            RenderTexture.active = r;
            Rect rect = new Rect(0, 0, w, h);

            EditorGUIUtility.SetRenderTextureNoViewport(r);
            GL.LoadOrtho();
            GL.LoadPixelMatrix(0, w, h, 0);
            ShaderUtil.rawViewportRect = new Rect(0, 0, w, h);
            ShaderUtil.rawScissorRect = new Rect(0 , 0, w, h);
            GL.Clear(true, true, new Color(0, 0, 0, 0));

            Rect blitRect = rect;
            if (inimage.width > inimage.height)
            {
                float newHeight = (float)blitRect.height * ((float)inimage.height / (float)inimage.width);
                blitRect.height = (int)newHeight;
                blitRect.y += (int)(newHeight * 0.5f);
            }
            else if (inimage.width < inimage.height)
            {
                float newWidth = (float)blitRect.width * ((float)inimage.width / (float)inimage.height);
                blitRect.width = (int)newWidth;
                blitRect.x += (int)(newWidth * 0.5f);
            }
            if (bgStyle != null && bgStyle.normal != null && bgStyle.normal.background != null)
                Graphics.DrawTexture(rect, bgStyle.normal.background);

            Graphics.DrawTexture(blitRect, inimage);
            outimage.ReadPixels(rect, 0, 0, false);
            outimage.Apply();
            outimage.hideFlags = HideFlags.HideAndDontSave;
            saved.Restore();
        }

        /*
         * Check if textures queued for download has finished downloading.
         * This call will reset the flag for finished downloads.
         */
        public static bool CheckRepaint()
        {
            bool needsRepaint = s_NeedsRepaint;
            s_NeedsRepaint = false;
            return needsRepaint;
        }

        // Abort fetching all previews with the specified size
        public static void AbortSize(int size)
        {
            AsyncHTTPClient.AbortByTag("previewSize-" + size.ToString());

            // Mark any pending requests for that width in the cases as invalid
            // now that requests has been aborted.
            foreach (KeyValuePair<string, CachedAssetStoreImage> kv in AssetStorePreviewManager.CachedAssetStoreImages)
            {
                if (kv.Value.requestedWidth != size)
                    continue;
                kv.Value.requestedWidth = -1;
                kv.Value.client = null;
            }
        }

        // Abort fetching all reviews that haven't been used since timestamp
        public static void AbortOlderThan(double timestamp)
        {
            foreach (KeyValuePair<string, CachedAssetStoreImage> kv in AssetStorePreviewManager.CachedAssetStoreImages)
            {
                CachedAssetStoreImage entry = kv.Value;
                if (entry.lastUsed >= timestamp || entry.client == null)
                    continue;
                entry.requestedWidth = -1;
                entry.client.Abort();
                entry.client = null;
            }

            // @TODO: Currently we know that AbortOlderThan is called exactly once each repaint.
            //        Therefore this counter can be reset here. Should probably be moved to a
            //        more intuitive location.
            Instance.m_ConvertedThisTick = 0;
        }
    }
} // UnityEditor namespace
