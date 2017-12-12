// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.Networking
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequestAssetBundle/Public/DownloadHandlerAssetBundle.h")]
    public sealed class DownloadHandlerAssetBundle : DownloadHandler
    {
        private extern static IntPtr Create(DownloadHandlerAssetBundle obj, string url, uint crc);
        private extern static IntPtr CreateCached(DownloadHandlerAssetBundle obj, string url, string name, Hash128 hash, uint crc);

        private void InternalCreateAssetBundle(string url, uint crc)
        {
            m_Ptr = Create(this, url, crc);
        }

        private void InternalCreateAssetBundleCached(string url, string name, Hash128 hash, uint crc)
        {
            m_Ptr = CreateCached(this, url, name, hash, crc);
        }

        public DownloadHandlerAssetBundle(string url, uint crc)
        {
            InternalCreateAssetBundle(url, crc);
        }

        public DownloadHandlerAssetBundle(string url, uint version, uint crc)
        {
            InternalCreateAssetBundleCached(url, "", new Hash128(0, 0, 0, version), crc);
        }

        public DownloadHandlerAssetBundle(string url, Hash128 hash, uint crc)
        {
            InternalCreateAssetBundleCached(url, "", hash, crc);
        }

        public DownloadHandlerAssetBundle(string url, string name, Hash128 hash, uint crc)
        {
            InternalCreateAssetBundleCached(url, name, hash, crc);
        }

        protected override byte[] GetData()
        {
            throw new System.NotSupportedException("Raw data access is not supported for asset bundles");
        }

        protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported for asset bundles");
        }

        public extern AssetBundle assetBundle { get; }

        public static AssetBundle GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerAssetBundle>(www).assetBundle;
        }

    }
}
