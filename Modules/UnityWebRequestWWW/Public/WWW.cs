// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEngine
{
    public partial class WWW
        : CustomYieldInstruction
        , IDisposable
    {
        public static string EscapeURL(string s)
        {
            return EscapeURL(s, Encoding.UTF8);
        }

        public static string EscapeURL(string s, Encoding e)
        {
            return UnityWebRequest.EscapeURL(s, e);
        }

        public static string UnEscapeURL(string s)
        {
            return UnEscapeURL(s, Encoding.UTF8);
        }

        public static string UnEscapeURL(string s, Encoding e)
        {
            return UnityWebRequest.UnEscapeURL(s, e);
        }

        public static WWW LoadFromCacheOrDownload(string url, int version)
        {
            return LoadFromCacheOrDownload(url, version, 0);
        }

        public static WWW LoadFromCacheOrDownload(string url, int version, uint crc)
        {
            Hash128 tempHash = new Hash128(0, 0, 0, (uint)version);
            return LoadFromCacheOrDownload(url, tempHash, crc);
        }

        public static WWW LoadFromCacheOrDownload(string url, Hash128 hash)
        {
            return LoadFromCacheOrDownload(url, hash, 0);
        }

        public static WWW LoadFromCacheOrDownload(string url, Hash128 hash, uint crc)
        {
            return new WWW(url, "", hash, crc);
        }

        public static WWW LoadFromCacheOrDownload(string url, CachedAssetBundle cachedBundle, uint crc = 0)
        {
            return new WWW(url, cachedBundle.name, cachedBundle.hash, crc);
        }

        public WWW(string url)
        {
            _uwr = UnityWebRequest.Get(url);
            _uwr.SendWebRequest();
        }

        public WWW(string url, WWWForm form)
        {
            _uwr = UnityWebRequest.Post(url, form);
            _uwr.chunkedTransfer = false;
            _uwr.SendWebRequest();
        }

        public WWW(string url, byte[] postData)
        {
            _uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            _uwr.chunkedTransfer = false;
            UploadHandler formUploadHandler = new UploadHandlerRaw(postData);
            formUploadHandler.contentType = "application/x-www-form-urlencoded";
            _uwr.uploadHandler = formUploadHandler;
            _uwr.downloadHandler = new DownloadHandlerBuffer();
            _uwr.SendWebRequest();
        }

        [Obsolete("This overload is deprecated. Use UnityEngine.WWW.WWW(string, byte[], System.Collections.Generic.Dictionary<string, string>) instead.")]
        public WWW(string url, byte[] postData, Hashtable headers)
        {
            var verb = postData == null ? UnityWebRequest.kHttpVerbGET : UnityWebRequest.kHttpVerbPOST;
            _uwr = new UnityWebRequest(url, verb);
            _uwr.chunkedTransfer = false;
            UploadHandler formUploadHandler = new UploadHandlerRaw(postData);
            formUploadHandler.contentType = "application/x-www-form-urlencoded";
            _uwr.uploadHandler = formUploadHandler;
            _uwr.downloadHandler = new DownloadHandlerBuffer();
            foreach (var header in headers.Keys)
                _uwr.SetRequestHeader((string)header, (string)headers[header]);
            _uwr.SendWebRequest();
        }


        public WWW(string url, byte[] postData, Dictionary<string, string> headers)
        {
            var verb = postData == null ? UnityWebRequest.kHttpVerbGET : UnityWebRequest.kHttpVerbPOST;
            _uwr = new UnityWebRequest(url, verb);
            _uwr.chunkedTransfer = false;
            UploadHandler formUploadHandler = new UploadHandlerRaw(postData);
            formUploadHandler.contentType = "application/x-www-form-urlencoded";
            _uwr.uploadHandler = formUploadHandler;
            _uwr.downloadHandler = new DownloadHandlerBuffer();
            foreach (var header in headers)
                _uwr.SetRequestHeader(header.Key, header.Value);
            _uwr.SendWebRequest();
        }

        internal WWW(string url, string name, Hash128 hash, uint crc)
        {
            _uwr = UnityWebRequest.GetAssetBundle(url, new CachedAssetBundle(name, hash), crc);
            _uwr.SendWebRequest();
        }

        public AssetBundle assetBundle
        {
            get
            {
                if (_assetBundle == null)
                {
                    if (!WaitUntilDoneIfPossible())
                        return null;
                    if (_uwr.isNetworkError)
                        return null;
                    var dh = _uwr.downloadHandler as DownloadHandlerAssetBundle;
                    if (dh != null)
                        _assetBundle = dh.assetBundle;
                    else
                    {
                        var data = bytes;
                        if (data == null)
                            return null;
                        _assetBundle = AssetBundle.LoadFromMemory(data);
                    }
                }

                return _assetBundle;
            }
        }

        // Returns a [[AudioClip]] generated from the downloaded data (RO).
        [Obsolete("Obsolete msg (UnityUpgradable) -> * UnityEngine.WWW.GetAudioClip()", true)]
        public Object audioClip { get { return null; } }

        public byte[] bytes
        {
            get
            {
                if (!WaitUntilDoneIfPossible())
                    return new byte[] {};
                if (_uwr.isNetworkError)
                    return new byte[] {};
                var dh = _uwr.downloadHandler;
                if (dh == null)
                    return new byte[] {};
                return dh.data;
            }
        }

        // Returns a [[MovieTexture]] generated from the downloaded data (RO).
        [Obsolete("Obsolete msg (UnityUpgradable) -> * UnityEngine.WWW.GetMovieTexture()", true)]
        public Object movie { get { return null; } }

        [Obsolete("WWW.size is obsolete. Please use WWW.bytesDownloaded instead")]
        public int size { get { return bytesDownloaded; } }

        public int bytesDownloaded
        {
            get { return (int)_uwr.downloadedBytes; }
        }

        public string error
        {
            get
            {
                if (!_uwr.isDone)
                    return null;
                if (_uwr.isNetworkError)
                    return _uwr.error;
                if (_uwr.responseCode >= 400)
                    return string.Format("{0} {1}", _uwr.responseCode, GetStatusCodeName(_uwr.responseCode));
                return null;
            }
        }

        public bool isDone { get { return _uwr.isDone; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Obsolete msg (UnityUpgradable) -> * UnityEngine.WWW.GetAudioClip()", true)]
        public Object oggVorbis { get { return null; } }

        public float progress
        {
            get
            {
                var progress = _uwr.downloadProgress;
                // UWR returns negative if not sent yet, WWW always returns between 0 and 1
                if (progress < 0)
                    progress = 0.0f;
                return progress;
            }
        }

        public Dictionary<string, string> responseHeaders
        {
            get
            {
                if (!isDone)
                    return new Dictionary<string, string>();
                if (_responseHeaders == null)
                {
                    _responseHeaders = _uwr.GetResponseHeaders();
                    if (_responseHeaders != null)
                        _responseHeaders["STATUS"] = string.Format("HTTP/1.1 {0} {1}", _uwr.responseCode, GetStatusCodeName(_uwr.responseCode));
                    else
                        _responseHeaders = new Dictionary<string, string>();
                }
                return _responseHeaders;
            }
        }

        [Obsolete("Please use WWW.text instead. (UnityUpgradable) -> text", true)]
        public string data { get { return text; } }

        public string text
        {
            get
            {
                if (!WaitUntilDoneIfPossible())
                    return "";
                if (_uwr.isNetworkError)
                    return "";
                var dh = _uwr.downloadHandler;
                if (dh == null)
                    return "";
                return dh.text;
            }
        }

        private Texture2D CreateTextureFromDownloadedData(bool markNonReadable)
        {
            if (!WaitUntilDoneIfPossible())
                return new Texture2D(2, 2);
            if (_uwr.isNetworkError)
                return null;
            var dh = _uwr.downloadHandler;
            if (dh == null)
                return null;
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(dh.data, markNonReadable);
            return texture;
        }

        public Texture2D texture { get { return CreateTextureFromDownloadedData(false); } }

        public Texture2D textureNonReadable { get { return CreateTextureFromDownloadedData(true); } }

        public void LoadImageIntoTexture(Texture2D texture)
        {
            if (!WaitUntilDoneIfPossible())
                return;
            if (_uwr.isNetworkError)
            {
                Debug.LogError("Cannot load image: download failed");
                return;
            }
            var dh = _uwr.downloadHandler;
            if (dh == null)
            {
                Debug.LogError("Cannot load image: internal error");
                return;
            }
            texture.LoadImage(dh.data, false);
        }

        public ThreadPriority threadPriority { get; set; }

        public float uploadProgress
        {
            get
            {
                var progress = _uwr.uploadProgress;
                // UWR returns negative if not sent yet, WWW always returns between 0 and 1
                if (progress < 0)
                    progress = 0.0f;
                return progress;
            }
        }

        public string url { get { return _uwr.url; } }

        public override bool keepWaiting { get { return _uwr == null ? false : !_uwr.isDone; } }

        public void Dispose()
        {
            if (_uwr != null)
            {
                _uwr.Dispose();
                _uwr = null;
            }
        }

        internal Object GetAudioClipInternal(bool threeD, bool stream, bool compressed, AudioType audioType)
        {
            return WebRequestWWW.InternalCreateAudioClipUsingDH(_uwr.downloadHandler, _uwr.url, stream, compressed, audioType);
        }

        internal object GetMovieTextureInternal()
        {
            return WebRequestWWW.InternalCreateMovieTextureUsingDH(_uwr.downloadHandler);
        }


        public AudioClip GetAudioClip()
        {
            return GetAudioClip(true, false, AudioType.UNKNOWN);
        }

        public AudioClip GetAudioClip(bool threeD)
        {
            return GetAudioClip(threeD, false, AudioType.UNKNOWN);
        }

        public AudioClip GetAudioClip(bool threeD, bool stream)
        {
            return GetAudioClip(threeD, stream, AudioType.UNKNOWN);
        }

        public AudioClip GetAudioClip(bool threeD, bool stream, AudioType audioType)
        {
            return (AudioClip)GetAudioClipInternal(threeD, stream, false, audioType);
        }

        public AudioClip GetAudioClipCompressed()
        {
            return GetAudioClipCompressed(false, AudioType.UNKNOWN);
        }

        public AudioClip GetAudioClipCompressed(bool threeD)
        {
            return GetAudioClipCompressed(threeD, AudioType.UNKNOWN);
        }

        public AudioClip GetAudioClipCompressed(bool threeD, AudioType audioType)
        {
            return (AudioClip)GetAudioClipInternal(threeD, false, true, audioType);
        }

        public MovieTexture GetMovieTexture()
        {
            return (MovieTexture)GetMovieTextureInternal();
        }


        private bool WaitUntilDoneIfPossible()
        {
            if (_uwr.isDone)
                return true;
            if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                // Reading file should be already done on non-threaded platforms
                // on threaded simply spin until done
                while (!_uwr.isDone) {}

                return true;
            }
            else
            {
                Debug.LogError("You are trying to load data from a www stream which has not completed the download yet.\nYou need to yield the download or wait until isDone returns true.");
                return false;
            }
        }

        private string GetStatusCodeName(long statusCode)
        {
            switch (statusCode)
            {
                case 200:
                    return "OK";
                case 201:
                    return "Created";
                case 202:
                    return "Accepted";
                case 203:
                    return "Non-Authoritative Information";
                case 204:
                    return "No Content";
                case 205:
                    return "Reset Content";
                case 206:
                    return "Partial Content";
                case 300:
                    return "Multiple Choices";
                case 301:
                    return "Moved Permanently";
                case 302:
                    return "Found";
                case 303:
                    return "See Other";
                case 304:
                    return "Not Modified";
                case 305:
                    return "Use Proxy";
                // 306 is no longer used and is reserved
                case 307:
                    return "Temporary Redirect";
                case 400:
                    return "Bad Request";
                case 401:
                    return "Unauthorized";
                case 402:
                    return "Payment Required";
                case 403:
                    return "Forbidden";
                case 404:
                    return "Not Found";
                case 405:
                    return "Method Not Allowed";
                case 406:
                    return "Not Acceptable";
                case 407:
                    return "Proxy Authentication Required";
                case 408:
                    return "Request Timeout";
                case 409:
                    return "Conflict";
                case 410:
                    return "Gone";
                case 411:
                    return "Length Required";
                case 412:
                    return "Precondition Failed";
                case 413:
                    return "Request Entity Too Large";
                case 414:
                    return "Request-URI Too Long";
                case 415:
                    return "Unsupported Media Type";
                case 416:
                    return "Requested Range Not Satisfiable";
                case 417:
                    return "Expectation Failed";
                case 500:
                    return "Internal Server Error";
                case 501:
                    return "Not Implemented";
                case 502:
                    return "Bad Gateway";
                case 503:
                    return "Service Unavailable";
                case 504:
                    return "Gateway Timeout";
                case 505:
                    return "HTTP Version Not Supported";
                default:
                    return "";
            }
        }

        private UnityWebRequest _uwr;
        private AssetBundle _assetBundle;
        private Dictionary<string, string> _responseHeaders;
    }

}
