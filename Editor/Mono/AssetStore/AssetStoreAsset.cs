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
} // UnityEditor namespace
