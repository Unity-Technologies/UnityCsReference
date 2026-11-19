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
    ///<summary>Simple access to web pages.</summary>
    ///<remarks>Obsolete: WWW has been replaced with <see cref="UnityWebRequest" />.
    ///
    ///This is a small utility module for retrieving the contents of URLs.
    ///
    ///You start a download in the background by calling <c>WWW(url)</c> which returns a new WWW object.
    ///
    ///You can inspect the <c>isDone</c> property to see if the download has completed or yield
    ///the download object to automatically wait until it is (without blocking the rest of the game).
    ///
    ///Use it if you want to get some data from a web server for integration with a game
    ///such as highscore lists or calling home for some reason. There is also functionality
    ///to create textures from images downloaded from the web and to stream &amp; load new web
    ///player data files.
    ///
    ///The WWW class can be used to send both GET and POST requests to the server. The WWW class will use GET
    ///by default and POST if you supply a postData parameter.
    ///
    ///
    ///
    ///**Note:** URLs passed to WWW class must be '%' escaped.
    ///
    ///**Notes** **http://**, **https://** and **file://** protocols are supported on iPhone.
    ///**ftp://** protocol support is limited to anonymous downloads only. Other protocols are not supported.
    ///
    ///**Note:** When using file protocol on Windows and Windows Store Apps for accessing local files, you have to specify **file:///** (with three slashes).</remarks>
    ///<example>
    ///  <code><![CDATA[
    /// // Get the Unity logo as a texture from the Unity website
    ///using UnityEngine;
    ///using System.Collections;
    ///
    ///public class ExampleClass : MonoBehaviour
    ///{
    ///    public string url = "https://unity3d.com/files/images/ogimg.jpg";
    ///    IEnumerator Start()
    ///    {
    ///        using (WWW www = new WWW(url))
    ///        {
    ///            yield return www;
    ///            Renderer renderer = GetComponent<Renderer>();
    ///            renderer.material.mainTexture = www.texture;
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="WWWForm" />
    [Obsolete("Use UnityWebRequest, a fully featured replacement which is more efficient and has additional features")]
    public partial class WWW
        : CustomYieldInstruction
        , IDisposable
    {
        ///<summary>Escapes characters in a string to ensure they are URL-friendly.</summary>
        ///<remarks>
        ///  <para>Certain text characters have special meanings when present in URLs. If you need to include those characters in URL parameters then you must represent them with escape sequences. It is recommended that you use this function on any text supplied by a user before passing the text as a URL parameter. This will ensure that a malicious user can't manipulate the contents of the URL to attack the webserver.</para>
        ///  <para />
        ///</remarks>
        ///<param name="s">A string with characters to be escaped.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Escaped name is "Fish+%26+Chips".
        ///        var escName = WWW.EscapeURL("Fish & Chips");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string EscapeURL(string s)
        {
            return EscapeURL(s, Encoding.UTF8);
        }

        ///<summary>Escapes characters in a string to ensure they are URL-friendly.</summary>
        ///<remarks>
        ///  <para>Certain text characters have special meanings when present in URLs. If you need to include those characters in URL parameters then you must represent them with escape sequences. It is recommended that you use this function on any text supplied by a user before passing the text as a URL parameter. This will ensure that a malicious user can't manipulate the contents of the URL to attack the webserver.</para>
        ///  <para />
        ///</remarks>
        ///<param name="s">A string with characters to be escaped.</param>
        ///<param name="e">The text encoding to use.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Escaped name is "Fish+%26+Chips".
        ///        var escName = WWW.EscapeURL("Fish & Chips");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string EscapeURL(string s, Encoding e)
        {
            return UnityWebRequest.EscapeURL(s, e);
        }

        ///<summary>Converts URL-friendly escape sequences back to normal text.</summary>
        ///<remarks>
        ///  <para>Certain text characters have special meanings when present in URLs. If you need to include those characters in URL parameters then you must represent them with escape sequences. This function takes a string containing these escape sequences and converts them back to normal text.</para>
        ///  <para />
        ///</remarks>
        ///<param name="s">A string containing escaped characters.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Plain name is "Fish & Chips".
        ///        var plainName = WWW.UnEscapeURL("Fish+%26+Chips");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string UnEscapeURL(string s)
        {
            return UnEscapeURL(s, Encoding.UTF8);
        }

        ///<summary>Converts URL-friendly escape sequences back to normal text.</summary>
        ///<remarks>
        ///  <para>Certain text characters have special meanings when present in URLs. If you need to include those characters in URL parameters then you must represent them with escape sequences. This function takes a string containing these escape sequences and converts them back to normal text.</para>
        ///  <para />
        ///</remarks>
        ///<param name="s">A string containing escaped characters.</param>
        ///<param name="e">The text encoding to use.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Plain name is "Fish & Chips".
        ///        var plainName = WWW.UnEscapeURL("Fish+%26+Chips");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string UnEscapeURL(string s, Encoding e)
        {
            return UnityWebRequest.UnEscapeURL(s, e);
        }

        ///<summary>Loads an AssetBundle with the specified version number from the cache. If the AssetBundle is not currently cached, it will automatically be downloaded and stored in the cache for future retrieval from local storage.</summary>
        ///<remarks>
        ///  <para>LoadFromCacheOrDownload() must be used in place of "new WWW (url)" in order to utilize caching functionality.
        ///
        ///Cached AssetBundles are uniquely identified solely by the filename and version. All domain and path information in <c>url</c> is ignored by Caching. Since cached AssetBundles are identified by filename instead of the full URL, you can change the directory from where the asset bundle is downloaded at any time. This is useful for pushing out new versions of the game and ensuring that files are not cached incorrectly by the browser or by a CDN.
        ///
        ///Usually using the filename of the AssetBundle to generate the cache path is fine. But if there are different AssetBundles with the same last file name, cache conflicts will happen. With CachedAssetBundle, users can use <see cref="CachedAssetBundle.name" /> to customized the cache path to avoid the cache conflicts. Users can also utilize this to organize the cache data structure.
        ///
        ///If the cache folder does not have any space for caching additional files, LoadFromCacheOrDownload will iteratively delete the least-recently-used AssetBundles from the Cache until sufficient space is available to store the new AssetBundle. If making space is not possible (because the hard disk is full, or all files in the cache are currently in use), LoadFromCacheOrDownload() will bypass Caching and stream the file into memory like a normal "new WWW()" call.
        ///
        ///Cached data can be stored in a compressed form depending on <see cref="P:UnityEngine.Caching.compressionEnabled" /> value.
        ///
        ///This function can only be used to access AssetBundles. No other types or content are cacheable.
        ///
        ///The CRC passed into this function is computed during Asset Bundle build time, see <see cref="M:UnityEditor.BuildPipeline.BuildAssetBundles" />.
        ///
        ///**Note:** URL must be '%' escaped.</para>
        ///  <para />
        ///</remarks>
        ///<param name="url">The URL to download the AssetBundle from, if it is not present in the cache. Must be '%' escaped.</param>
        ///<param name="version">Version of the AssetBundle. The file will only be loaded from the disk cache if it has previously been downloaded with the same <c>version</c> parameter. By incrementing the version number requested by your application, you can force Caching to download a new copy of the AssetBundle from <c>url</c>.</param>
        ///<returns>A WWW instance, which can be used to access the data once the load/download operation is completed.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class LoadFromCacheOrDownloadExample : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        while (!Caching.ready)
        ///            yield return null;
        ///
        ///        using (var www = WWW.LoadFromCacheOrDownload("https://myserver.com/myassetBundle.unity3d", 5))
        ///        {
        ///            yield return www;
        ///            if (!string.IsNullOrEmpty(www.error))
        ///            {
        ///                Debug.Log(www.error);
        ///                yield return null;
        ///            }
        ///            var myLoadedAssetBundle = www.assetBundle;
        ///
        ///            var asset = myLoadedAssetBundle.mainAsset;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="M:UnityEditor.BuildPipeline.BuildAssetBundles" />
        public static WWW LoadFromCacheOrDownload(string url, int version)
        {
            return LoadFromCacheOrDownload(url, version, 0);
        }

        ///<summary>Loads an AssetBundle with the specified version number from the cache. If the AssetBundle is not currently cached, it will automatically be downloaded and stored in the cache for future retrieval from local storage.</summary>
        ///<remarks>
        ///  <para>LoadFromCacheOrDownload() must be used in place of "new WWW (url)" in order to utilize caching functionality.
        ///
        ///Cached AssetBundles are uniquely identified solely by the filename and version. All domain and path information in <c>url</c> is ignored by Caching. Since cached AssetBundles are identified by filename instead of the full URL, you can change the directory from where the asset bundle is downloaded at any time. This is useful for pushing out new versions of the game and ensuring that files are not cached incorrectly by the browser or by a CDN.
        ///
        ///Usually using the filename of the AssetBundle to generate the cache path is fine. But if there are different AssetBundles with the same last file name, cache conflicts will happen. With CachedAssetBundle, users can use <see cref="CachedAssetBundle.name" /> to customized the cache path to avoid the cache conflicts. Users can also utilize this to organize the cache data structure.
        ///
        ///If the cache folder does not have any space for caching additional files, LoadFromCacheOrDownload will iteratively delete the least-recently-used AssetBundles from the Cache until sufficient space is available to store the new AssetBundle. If making space is not possible (because the hard disk is full, or all files in the cache are currently in use), LoadFromCacheOrDownload() will bypass Caching and stream the file into memory like a normal "new WWW()" call.
        ///
        ///Cached data can be stored in a compressed form depending on <see cref="P:UnityEngine.Caching.compressionEnabled" /> value.
        ///
        ///This function can only be used to access AssetBundles. No other types or content are cacheable.
        ///
        ///The CRC passed into this function is computed during Asset Bundle build time, see <see cref="M:UnityEditor.BuildPipeline.BuildAssetBundles" />.
        ///
        ///**Note:** URL must be '%' escaped.</para>
        ///  <para />
        ///</remarks>
        ///<param name="url">The URL to download the AssetBundle from, if it is not present in the cache. Must be '%' escaped.</param>
        ///<param name="version">Version of the AssetBundle. The file will only be loaded from the disk cache if it has previously been downloaded with the same <c>version</c> parameter. By incrementing the version number requested by your application, you can force Caching to download a new copy of the AssetBundle from <c>url</c>.</param>
        ///<param name="crc">An optional CRC-32 Checksum of the uncompressed contents. If this is non-zero, then the content will be compared against the checksum before loading it, and give an error if it does not match. You can use this to avoid data corruption from bad downloads or users tampering with the cached files on disk. If the CRC does not match, Unity will try to redownload the data, and if the CRC on the server does not match it will fail with an error. Look at the error string returned to see the correct CRC value to use for an AssetBundle.</param>
        ///<returns>A WWW instance, which can be used to access the data once the load/download operation is completed.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class LoadFromCacheOrDownloadExample : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        while (!Caching.ready)
        ///            yield return null;
        ///
        ///        using (var www = WWW.LoadFromCacheOrDownload("https://myserver.com/myassetBundle.unity3d", 5))
        ///        {
        ///            yield return www;
        ///            if (!string.IsNullOrEmpty(www.error))
        ///            {
        ///                Debug.Log(www.error);
        ///                yield return null;
        ///            }
        ///            var myLoadedAssetBundle = www.assetBundle;
        ///
        ///            var asset = myLoadedAssetBundle.mainAsset;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="M:UnityEditor.BuildPipeline.BuildAssetBundles" />
        public static WWW LoadFromCacheOrDownload(string url, int version, uint crc)
        {
            Hash128 tempHash = new Hash128(0, 0, 0, (uint)version);
            return LoadFromCacheOrDownload(url, tempHash, crc);
        }

        public static WWW LoadFromCacheOrDownload(string url, Hash128 hash)
        {
            return LoadFromCacheOrDownload(url, hash, 0);
        }

        ///<summary>Loads an AssetBundle with the specified version number from the cache. If the AssetBundle is not currently cached, it will automatically be downloaded and stored in the cache for future retrieval from local storage.</summary>
        ///<remarks>
        ///  <para>LoadFromCacheOrDownload() must be used in place of "new WWW (url)" in order to utilize caching functionality.
        ///
        ///Cached AssetBundles are uniquely identified solely by the filename and version. All domain and path information in <c>url</c> is ignored by Caching. Since cached AssetBundles are identified by filename instead of the full URL, you can change the directory from where the asset bundle is downloaded at any time. This is useful for pushing out new versions of the game and ensuring that files are not cached incorrectly by the browser or by a CDN.
        ///
        ///Usually using the filename of the AssetBundle to generate the cache path is fine. But if there are different AssetBundles with the same last file name, cache conflicts will happen. With CachedAssetBundle, users can use <see cref="CachedAssetBundle.name" /> to customized the cache path to avoid the cache conflicts. Users can also utilize this to organize the cache data structure.
        ///
        ///If the cache folder does not have any space for caching additional files, LoadFromCacheOrDownload will iteratively delete the least-recently-used AssetBundles from the Cache until sufficient space is available to store the new AssetBundle. If making space is not possible (because the hard disk is full, or all files in the cache are currently in use), LoadFromCacheOrDownload() will bypass Caching and stream the file into memory like a normal "new WWW()" call.
        ///
        ///Cached data can be stored in a compressed form depending on <see cref="P:UnityEngine.Caching.compressionEnabled" /> value.
        ///
        ///This function can only be used to access AssetBundles. No other types or content are cacheable.
        ///
        ///The CRC passed into this function is computed during Asset Bundle build time, see <see cref="M:UnityEditor.BuildPipeline.BuildAssetBundles" />.
        ///
        ///**Note:** URL must be '%' escaped.</para>
        ///  <para />
        ///</remarks>
        ///<param name="url">The URL to download the AssetBundle from, if it is not present in the cache. Must be '%' escaped.</param>
        ///<param name="hash">Hash128 which is used as the version of the AssetBundle.</param>
        ///<param name="crc">An optional CRC-32 Checksum of the uncompressed contents. If this is non-zero, then the content will be compared against the checksum before loading it, and give an error if it does not match. You can use this to avoid data corruption from bad downloads or users tampering with the cached files on disk. If the CRC does not match, Unity will try to redownload the data, and if the CRC on the server does not match it will fail with an error. Look at the error string returned to see the correct CRC value to use for an AssetBundle.</param>
        ///<returns>A WWW instance, which can be used to access the data once the load/download operation is completed.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class LoadFromCacheOrDownloadExample : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        while (!Caching.ready)
        ///            yield return null;
        ///
        ///        using (var www = WWW.LoadFromCacheOrDownload("https://myserver.com/myassetBundle.unity3d", 5))
        ///        {
        ///            yield return www;
        ///            if (!string.IsNullOrEmpty(www.error))
        ///            {
        ///                Debug.Log(www.error);
        ///                yield return null;
        ///            }
        ///            var myLoadedAssetBundle = www.assetBundle;
        ///
        ///            var asset = myLoadedAssetBundle.mainAsset;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="M:UnityEditor.BuildPipeline.BuildAssetBundles" />
        public static WWW LoadFromCacheOrDownload(string url, Hash128 hash, uint crc)
        {
            return new WWW(url, "", hash, crc);
        }

        ///<summary>Loads an AssetBundle with the specified version number from the cache. If the AssetBundle is not currently cached, it will automatically be downloaded and stored in the cache for future retrieval from local storage.</summary>
        ///<remarks>
        ///  <para>LoadFromCacheOrDownload() must be used in place of "new WWW (url)" in order to utilize caching functionality.
        ///
        ///Cached AssetBundles are uniquely identified solely by the filename and version. All domain and path information in <c>url</c> is ignored by Caching. Since cached AssetBundles are identified by filename instead of the full URL, you can change the directory from where the asset bundle is downloaded at any time. This is useful for pushing out new versions of the game and ensuring that files are not cached incorrectly by the browser or by a CDN.
        ///
        ///Usually using the filename of the AssetBundle to generate the cache path is fine. But if there are different AssetBundles with the same last file name, cache conflicts will happen. With CachedAssetBundle, users can use <see cref="CachedAssetBundle.name" /> to customized the cache path to avoid the cache conflicts. Users can also utilize this to organize the cache data structure.
        ///
        ///If the cache folder does not have any space for caching additional files, LoadFromCacheOrDownload will iteratively delete the least-recently-used AssetBundles from the Cache until sufficient space is available to store the new AssetBundle. If making space is not possible (because the hard disk is full, or all files in the cache are currently in use), LoadFromCacheOrDownload() will bypass Caching and stream the file into memory like a normal "new WWW()" call.
        ///
        ///Cached data can be stored in a compressed form depending on <see cref="P:UnityEngine.Caching.compressionEnabled" /> value.
        ///
        ///This function can only be used to access AssetBundles. No other types or content are cacheable.
        ///
        ///The CRC passed into this function is computed during Asset Bundle build time, see <see cref="M:UnityEditor.BuildPipeline.BuildAssetBundles" />.
        ///
        ///**Note:** URL must be '%' escaped.</para>
        ///  <para />
        ///</remarks>
        ///<param name="url">The URL to download the AssetBundle from, if it is not present in the cache. Must be '%' escaped.</param>
        ///<param name="cachedBundle">A structure used to download a given version of AssetBundle to a customized cache path.
        ///
        ///Analogous to the <c>cachedAssetBundle</c> parameter for <see cref="UnityWebRequestAssetBundle.GetAssetBundle" />.&lt;/param&gt;</param>
        ///<param name="crc">An optional CRC-32 Checksum of the uncompressed contents. If this is non-zero, then the content will be compared against the checksum before loading it, and give an error if it does not match. You can use this to avoid data corruption from bad downloads or users tampering with the cached files on disk. If the CRC does not match, Unity will try to redownload the data, and if the CRC on the server does not match it will fail with an error. Look at the error string returned to see the correct CRC value to use for an AssetBundle.</param>
        ///<returns>A WWW instance, which can be used to access the data once the load/download operation is completed.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class LoadFromCacheOrDownloadExample : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        while (!Caching.ready)
        ///            yield return null;
        ///
        ///        using (var www = WWW.LoadFromCacheOrDownload("https://myserver.com/myassetBundle.unity3d", 5))
        ///        {
        ///            yield return www;
        ///            if (!string.IsNullOrEmpty(www.error))
        ///            {
        ///                Debug.Log(www.error);
        ///                yield return null;
        ///            }
        ///            var myLoadedAssetBundle = www.assetBundle;
        ///
        ///            var asset = myLoadedAssetBundle.mainAsset;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="M:UnityEditor.BuildPipeline.BuildAssetBundles" />
        public static WWW LoadFromCacheOrDownload(string url, CachedAssetBundle cachedBundle, uint crc = 0)
        {
            return new WWW(url, cachedBundle.name, cachedBundle.hash, crc);
        }

        ///<summary>Creates a WWW request with the given URL.</summary>
        ///<remarks>This function creates and sends a GET request.
        ///The stream will automatically start downloading the response.
        ///
        ///After the stream is created you have to wait for it to complete, then you can access the downloaded data.
        ///As a convenience the stream can be yielded, so you can very easily tell Unity to wait for the download to complete.
        ///
        ///**Note:** URL must be '%' escaped.</remarks>
        ///<param name="url">The url to download. Must be '%' escaped.</param>
        ///<returns>A new WWW object. When it has been downloaded, the results can be fetched from the returned object.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Get the latest webcam shot from outside "Friday's" in Times Square
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string url = "https://images.earthcam.com/ec_metros/ourcams/fridays.jpg";
        ///    IEnumerator Start()
        ///    {
        ///        // Start a download of the given URL
        ///        using (WWW www = new WWW(url))
        ///        {
        ///            // Wait for download to complete
        ///            yield return www;
        ///
        ///            // assign texture
        ///            Renderer renderer = GetComponent<Renderer>();
        ///            renderer.material.mainTexture = www.textureNonReadable;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public WWW(string url)
        {
            _uwr = UnityWebRequest.Get(url);
            _uwr.SendWebRequest();
        }

        ///<summary>Creates a WWW request with the given URL.</summary>
        ///<remarks>This function creates and sends a POST request with form data contained in a <see cref="WWWForm" />
        ///parameter. This is the same as calling <c>new WWW(url,form.data, form.headers)</c>.
        ///The stream will automatically start downloading the response.
        ///
        ///After the stream is created you have to wait for it to complete, then you can access the downloaded data.
        ///As a convenience the stream can be yielded, so you can very easily tell Unity to wait for the download to complete.
        ///
        ///**Note:** URL must be '%' escaped.</remarks>
        ///<param name="url">The url to download. Must be '%' escaped.</param>
        ///<param name="form">A <see cref="WWWForm" /> instance containing the form data to post.</param>
        ///<returns>A new WWW object. When it has been downloaded, the results can be fetched from the returned object.</returns>
        public WWW(string url, WWWForm form)
        {
            _uwr = UnityWebRequest.Post(url, form);
            _uwr.chunkedTransfer = false;
            _uwr.SendWebRequest();
        }

        ///<summary>Creates a WWW request with the given URL.</summary>
        ///<remarks>This function creates and sends a POST request with raw post data contained in postData.
        ///The stream will automatically start downloading the response.
        ///Use this version if you need to post raw post data in a custom format to the server.
        ///
        ///After the stream is created you have to wait for it to complete, then you can access the downloaded data.
        ///As a convenience the stream can be yielded, so you can very easily tell Unity to wait for the download to complete.
        ///
        ///**Note:** URL must be '%' escaped.</remarks>
        ///<param name="url">The url to download. Must be '%' escaped.</param>
        ///<param name="postData">A byte array of data to be posted to the url.</param>
        ///<returns>A new WWW object. When it has been downloaded, the results can be fetched from the returned object.</returns>
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

        ///<summary>Creates a WWW request with the given URL.</summary>
        ///<remarks>This function creates and sends a POST request with raw post data contained in
        ///postData and custom request headers supplied in the <c>headers</c> hashtable.
        ///The stream will automatically start downloading the response.
        ///Use this version if you need to post raw post data in a custom format to the server or if you need to supply custom request headers.
        ///
        ///After the stream is created you have to wait for it to complete, then you can access the downloaded data.
        ///As a convenience the stream can be yielded, so you can very easily tell Unity to wait for the download to complete.
        ///
        ///**Note:** URL must be '%' escaped.</remarks>
        ///<param name="url">The url to download. Must be '%' escaped.</param>
        ///<param name="postData">A byte array of data to be posted to the url.</param>
        ///<param name="headers">A hash table of custom headers to send with the request.</param>
        ///<returns>A new WWW object. When it has been downloaded, the results can be fetched from the returned object.</returns>
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

        ///<summary>Creates a WWW request with the given URL.</summary>
        ///<remarks>This function creates and sends a POST request with raw post data contained in
        ///postData and custom request headers supplied in the <c>headers</c> Dictionary.
        ///The stream will automatically start downloading the response.
        ///Use this version if you need to post raw post data in a custom format to the server or if you need to supply custom request headers.
        ///
        ///After the stream is created you have to wait for it to complete, then you can access the downloaded data.
        ///As a convenience the stream can be yielded, so you can very easily tell Unity to wait for the download to complete.
        ///
        ///**Note:** URL must be '%' escaped.</remarks>
        ///<param name="url">The url to download. Must be '%' escaped.</param>
        ///<param name="postData">A byte array of data to be posted to the url.</param>
        ///<param name="headers">A dictionary that contains the header keys and values to pass to the server.</param>
        ///<returns>A new WWW object. When it has been downloaded, the results can be fetched from the returned object.</returns>
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
            _uwr = UnityWebRequestAssetBundle.GetAssetBundle(url, new CachedAssetBundle(name, hash), crc);
            _uwr.SendWebRequest();
        }

        ///<summary>Streams an AssetBundle that can contain any kind of asset from the project folder.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        using (WWW www = new WWW("https://myserver/myBundle.unity3d"))
        ///        {
        ///            yield return www;
        ///
        ///            // Get the designated main asset and instantiate it.
        ///            Instantiate(www.assetBundle.mainAsset);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AssetBundle" />
        public AssetBundle assetBundle
        {
            get
            {
                if (_assetBundle == null)
                {
                    if (!WaitUntilDoneIfPossible())
                        return null;
                    if (_uwr.result == UnityWebRequest.Result.ConnectionError)
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

        ///<summary>Returns the contents of the fetched web page as a byte array (RO).</summary>
        ///<remarks>If the object has not finished downloading the data, it will return an empty byte array.
        ///Use <see cref="isDone" /> or <c>yield</c> to see if the data is available.</remarks>
        public byte[] bytes
        {
            get
            {
                if (!WaitUntilDoneIfPossible())
                    return Array.Empty<byte>();
                if (_uwr.result == UnityWebRequest.Result.ConnectionError)
                    return Array.Empty<byte>();
                var dh = _uwr.downloadHandler;
                if (dh == null)
                    return Array.Empty<byte>();
                return dh.data;
            }
        }

        ///<exclude />
        [Obsolete("WWW.size is obsolete. Please use WWW.bytesDownloaded instead")]
        public int size { get { return bytesDownloaded; } }

        ///<summary>The number of bytes downloaded by this WWW query (read only).</summary>
        ///<remarks>Returns the number of bytes downloaded when fetching content from a WWW source.</remarks>
        public int bytesDownloaded
        {
            get { return (int)_uwr.downloadedBytes; }
        }

        ///<summary>Returns an error message if there was an error during the download (RO).</summary>
        ///<remarks>
        ///  <para>If there was no error, <c>error</c> will return <c>null</c> or an empty string (this is because some platforms don't allow nulls for string values). We recommend that you use String.IsNullOrEmpty to check for the presence of an error so that both cases are covered.
        ///
        ///If the object has not finished downloading the data, it will block until the download has finished.
        ///Use <see cref="isDone" /> or <c>yield</c> to see if the data is available.</para>
        ///  <para>In the example the URL is not valid so the error message will be "Couldn't resolve host".</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // Make a request with an invalid url
        ///    public string url = "invalid_url";
        ///    IEnumerator Start()
        ///    {
        ///        using (WWW www = new WWW(url))
        ///        {
        ///            yield return www;
        ///            if (!string.IsNullOrEmpty(www.error))
        ///                Debug.Log(www.error);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public string error
        {
            get
            {
                if (!_uwr.isDone)
                    return null;
                if (_uwr.result == UnityWebRequest.Result.ConnectionError)
                    return _uwr.error;
                if (_uwr.responseCode >= 400)
                {
                    var statusString = UnityWebRequest.GetHTTPStatusString(_uwr.responseCode);
                    return string.Format("{0} {1}", _uwr.responseCode, statusString);
                }
                return null;
            }
        }

        ///<summary>Is the download already finished? (RO)</summary>
        ///<remarks>You should not write loops that spin until download is done; use coroutines instead.</remarks>
        public bool isDone { get { return _uwr.isDone; } }

        ///<summary>How far has the download progressed (RO).</summary>
        ///<remarks>This is a value between zero and one; 0 means nothing is downloaded, 1 means download
        ///complete.
        ///
        ///progress will remain at 0.0 while sending the request to the server. For monitoring
        ///progress when uploading files to a web server, see <see cref="WWW.uploadProgress" />.</remarks>
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

        ///<summary>Dictionary of headers returned by the request.</summary>
        ///<remarks>Note when using these code examples you will want to set the WWW Security Emulation  Host URL to "https://unity3d.com" in  Editor Settings.  Failure to do this may give you security exceptions.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string url = "https://unity3d.com";
        ///    IEnumerator Start()
        ///    {
        ///        using (WWW www = new WWW(url))
        ///        {
        ///            yield return www;
        ///
        ///            if (www.responseHeaders.Count > 0)
        ///            {
        ///                foreach (KeyValuePair<string, string> entry in www.responseHeaders)
        ///                {
        ///                    Debug.Log(entry.Value + "=" + entry.Key);
        ///                }
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
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
                    {
                        var statusString = UnityWebRequest.GetHTTPStatusString(_uwr.responseCode);
                        _responseHeaders["STATUS"] = string.Format("HTTP/1.1 {0} {1}", _uwr.responseCode, statusString);
                    }
                    else
                        _responseHeaders = new Dictionary<string, string>();
                }
                return _responseHeaders;
            }
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use WWW.text instead. (UnityUpgradable) -> text", true)]
        public string data { get { return text; } }

        ///<summary>Returns the contents of the fetched web page as a string (RO).</summary>
        ///<remarks>If the object has not finished downloading the data, it will return an empty string.
        ///Use <see cref="isDone" /> or <c>yield</c> to see if the data is available.
        ///
        ///This function expects the web page contents in UTF-8 or ASCII character set. The returned
        ///string might be not correct for other characters or binary data. Use <see cref="bytes" /> property to
        ///get raw byte array in these cases.</remarks>
        ///<seealso cref="bytes" />
        public string text
        {
            get
            {
                if (!WaitUntilDoneIfPossible())
                    return "";
                if (_uwr.result == UnityWebRequest.Result.ConnectionError)
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
            if (_uwr.result == UnityWebRequest.Result.ConnectionError)
                return null;
            var dh = _uwr.downloadHandler;
            if (dh == null)
                return null;
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(dh.data, markNonReadable);
            return texture;
        }

        ///<summary>Returns a <see cref="Texture2D" /> generated from the downloaded data (RO).</summary>
        ///<remarks>
        ///  <para>The data must be an image in JPG or PNG format. If the data is not a valid
        ///image, the generated texture will be a small image of a question mark.
        ///It is recommended to use power-of-two size for each dimension of the image;
        ///arbitrary sizes will also work but can load slightly slower and take up
        ///a bit more memory. Each invocation of texture property allocates a new <see cref="Texture2D" />. If you
        ///continously download textures you must use <see cref="WWW.LoadImageIntoTexture" /> or <see cref="Object.Destroy" />
        ///the previously created texture.
        ///
        ///For PNG files, gamma correction is applied to the texture if PNG file contains
        ///gamma information. Display gamma for correction is assumed to be 2.0. If file
        ///does not contain gamma information, no color correction will be performed.
        ///
        ///JPG files are loaded into <see cref="TextureFormat.RGB24" /> format, PNG files are loaded into
        ///<see cref="TextureFormat.ARGB32" /> format. If you want to DXT-compress the downloaded image,
        ///use <see cref="WWW.LoadImageIntoTexture" /> instead.
        ///
        ///If the object has not finished downloading the data a dummy image will be returned.
        ///Use <see cref="isDone" /> or <see cref="YieldInstruction">yield</see> to see if the data is available.</para>
        ///  <para>**Note:** The <see cref="WWW.texture" /> property allocates a new <see cref="Texture2D" /> every time it is called.
        ///Therefore, it is important to always assign the result to a local variable so that it can
        ///later be freed using Destroy().
        ///
        ///The call to www.texture allocates a new texture, but the texture is never deallocated because
        ///no local reference to it exists.
        ///
        ///Alternatively, use <see cref="WWW.LoadImageIntoTexture" />.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // Get the latest webcam shot from outside "Friday's" in Times Square
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string url = "https://images.earthcam.com/ec_metros/ourcams/fridays.jpg";
        ///
        ///    IEnumerator Start()
        ///    {
        ///        // Start a download of the given URL
        ///        using (WWW www = new WWW(url))
        ///        {
        ///            // Wait for download to complete
        ///            yield return www;
        ///
        ///            // assign texture
        ///            Renderer renderer = GetComponent<Renderer>();
        ///            renderer.material.mainTexture = www.texture;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Texture2D texture { get { return CreateTextureFromDownloadedData(false); } }

        ///<summary>Returns a non-readable <see cref="Texture2D" /> generated from the downloaded data (RO).</summary>
        ///<remarks>Same as <see cref="texture" />, but marks texture as non-readable, effectively freeing system memory.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Get the latest webcam shot from outside "Friday's" in Times Square
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string url = "https://images.earthcam.com/ec_metros/ourcams/fridays.jpg";
        ///    IEnumerator Start()
        ///    {
        ///        // Start a download of the given URL
        ///        using (WWW www = new WWW(url))
        ///        {
        ///            // Wait for download to complete
        ///            yield return www;
        ///
        ///            // assign texture
        ///            Renderer renderer = GetComponent<Renderer>();
        ///            renderer.material.mainTexture = www.textureNonReadable;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="P:UnityEditor.TextureImporter.isReadable" />
        public Texture2D textureNonReadable { get { return CreateTextureFromDownloadedData(true); } }

        ///<summary>Replaces the contents of an existing <see cref="Texture2D" /> with an image from the downloaded data.</summary>
        ///<remarks>The data must be an image in JPG or PNG format. If the data is not a valid
        ///image, the generated texture will be a small image of a question mark.
        ///It is recommended to use power-of-two size for each dimension of the image;
        ///arbitrary sizes will also work but can load slightly slower and take up
        ///a bit more memory.
        ///
        ///For PNG files, gamma correction is applied to the texture if PNG file contains
        ///gamma information. Display gamma for correction is assumed to be 2.0. If file
        ///does not contain gamma information, no color correction will be performed.
        ///
        ///This function replaces texture contents with downloaded image data, so texture
        ///size and format might change. JPG files are loaded into <see cref="TextureFormat.RGB24" /> format,
        ///PNG files are loaded into <see cref="TextureFormat.ARGB32" /> format. If texture format before
        ///calling LoadImage is <see cref="TextureFormat.DXT1" /> or <see cref="TextureFormat.DXT5" />,
        ///then the loaded image will be DXT-compressed (into DXT1 for JPG images and DXT5 for PNG images).
        ///
        ///If the data has not finished downloading the texture will be left untouched.
        ///Use <see cref="isDone" /> or <c>yield</c> to see if the data is available.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Add this script to a GameObject. The Start() function fetches an
        /// // image from the documentation site.  It is then applied as the
        /// // texture on the GameObject.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string url = "https://docs.unity3d.com/uploads/Main/ShadowIntro.png";
        ///
        ///    IEnumerator Start()
        ///    {
        ///        Texture2D tex;
        ///        tex = new Texture2D(4, 4, TextureFormat.DXT1, false);
        ///        using (WWW www = new WWW(url))
        ///        {
        ///            yield return www;
        ///            www.LoadImageIntoTexture(tex);
        ///            GetComponent<Renderer>().material.mainTexture = tex;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void LoadImageIntoTexture(Texture2D texture)
        {
            if (!WaitUntilDoneIfPossible())
                return;
            if (_uwr.result == UnityWebRequest.Result.ConnectionError)
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

        ///<summary>Obsolete, has no effect.</summary>
        public ThreadPriority threadPriority { get; set; }

        ///<summary>How far has the upload progressed (RO).</summary>
        ///<remarks>This is a value between zero and one; 0 means nothing is sent yet, 1 means upload
        ///complete.
        ///
        ///Since all sending of data to the server is done before receiving data,  <c>uploadProgress</c> will always be 1.0 when <c>progress</c> is larger than 0.</remarks>
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

        ///<summary>The URL of this WWW request (RO).</summary>
        public string url { get { return _uwr.url; } }

        public override bool keepWaiting { get { return _uwr == null ? false : !_uwr.isDone; } }

        ///<summary>Disposes of an existing WWW object.</summary>
        ///<remarks>This function can be used to abort a download in progress. This can be useful, say, if you want to give the user an option to cancel the remote loading of a level in the game.</remarks>
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

        ///<summary>OBSOLETE. Use UnityWebRequestMultimedia.GetAudioClip().</summary>
        ///<seealso cref="UnityWebRequestMultimedia.GetAudioClip" />
        public AudioClip GetAudioClip()
        {
            return GetAudioClip(true, false, AudioType.UNKNOWN);
        }

        ///<summary>OBSOLETE. Use UnityWebRequestMultimedia.GetAudioClip().</summary>
        ///<seealso cref="UnityWebRequestMultimedia.GetAudioClip" />
        public AudioClip GetAudioClip(bool threeD)
        {
            return GetAudioClip(threeD, false, AudioType.UNKNOWN);
        }

        ///<summary>OBSOLETE. Use UnityWebRequestMultimedia.GetAudioClip().</summary>
        ///<seealso cref="UnityWebRequestMultimedia.GetAudioClip" />
        public AudioClip GetAudioClip(bool threeD, bool stream)
        {
            return GetAudioClip(threeD, stream, AudioType.UNKNOWN);
        }

        ///<summary>OBSOLETE. Use UnityWebRequestMultimedia.GetAudioClip().</summary>
        ///<seealso cref="UnityWebRequestMultimedia.GetAudioClip" />
        public AudioClip GetAudioClip(bool threeD, bool stream, AudioType audioType)
        {
            return (AudioClip)GetAudioClipInternal(threeD, stream, false, audioType);
        }

        ///<summary>OBSOLETE. Use UnityWebRequestMultimedia.GetAudioClip().</summary>
        ///<seealso cref="UnityWebRequestMultimedia.GetAudioClip" />
        public AudioClip GetAudioClipCompressed()
        {
            return GetAudioClipCompressed(false, AudioType.UNKNOWN);
        }

        ///<summary>OBSOLETE. Use UnityWebRequestMultimedia.GetAudioClip().</summary>
        ///<seealso cref="UnityWebRequestMultimedia.GetAudioClip" />
        public AudioClip GetAudioClipCompressed(bool threeD)
        {
            return GetAudioClipCompressed(threeD, AudioType.UNKNOWN);
        }

        ///<summary>OBSOLETE. Use UnityWebRequestMultimedia.GetAudioClip().</summary>
        ///<seealso cref="UnityWebRequestMultimedia.GetAudioClip" />
        public AudioClip GetAudioClipCompressed(bool threeD, AudioType audioType)
        {
            return (AudioClip)GetAudioClipInternal(threeD, false, true, audioType);
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

        private UnityWebRequest _uwr;
        private AssetBundle _assetBundle;
        private Dictionary<string, string> _responseHeaders;
    }

}
