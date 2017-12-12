// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Networking
{
    public static class UnityWebRequestAssetBundle
    {
        public static UnityWebRequest GetAssetBundle(string uri)
        {
            return GetAssetBundle(uri, 0);
        }

        public static UnityWebRequest GetAssetBundle(Uri uri)
        {
            return GetAssetBundle(uri, 0);
        }

        public static UnityWebRequest GetAssetBundle(string uri, uint crc)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    UnityWebRequest.kHttpVerbGET,
                    new DownloadHandlerAssetBundle(uri, crc),
                    null
                    );

            return request;
        }

        public static UnityWebRequest GetAssetBundle(Uri uri, uint crc)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    UnityWebRequest.kHttpVerbGET,
                    new DownloadHandlerAssetBundle(uri.AbsoluteUri, crc),
                    null
                    );

            return request;
        }

        public static UnityWebRequest GetAssetBundle(string uri, uint version, uint crc)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    UnityWebRequest.kHttpVerbGET,
                    new DownloadHandlerAssetBundle(uri, version, crc),
                    null
                    );

            return request;
        }

        public static UnityWebRequest GetAssetBundle(Uri uri, uint version, uint crc)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    UnityWebRequest.kHttpVerbGET,
                    new DownloadHandlerAssetBundle(uri.AbsoluteUri, version, crc),
                    null
                    );

            return request;
        }

        public static UnityWebRequest GetAssetBundle(string uri, Hash128 hash, uint crc)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    UnityWebRequest.kHttpVerbGET,
                    new DownloadHandlerAssetBundle(uri, hash, crc),
                    null
                    );

            return request;
        }

        public static UnityWebRequest GetAssetBundle(Uri uri, Hash128 hash, uint crc)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    UnityWebRequest.kHttpVerbGET,
                    new DownloadHandlerAssetBundle(uri.AbsoluteUri, hash, crc),
                    null
                    );

            return request;
        }

        public static UnityWebRequest GetAssetBundle(string uri, CachedAssetBundle cachedAssetBundle, uint crc)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    UnityWebRequest.kHttpVerbGET,
                    new DownloadHandlerAssetBundle(uri, cachedAssetBundle.name, cachedAssetBundle.hash, crc),
                    null
                    );

            return request;
        }

        public static UnityWebRequest GetAssetBundle(Uri uri, CachedAssetBundle cachedAssetBundle, uint crc)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    UnityWebRequest.kHttpVerbGET,
                    new DownloadHandlerAssetBundle(uri.AbsoluteUri, cachedAssetBundle.name, cachedAssetBundle.hash, crc),
                    null
                    );

            return request;
        }

    }
}
