// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.Networking
{
    // This file extends UnityWebRequest's public API with convenient wrappers
    // for common operations.
    public partial class UnityWebRequest
    {
        ///<summary>Create a UnityWebRequest for HTTP GET.</summary>
        ///<remarks>Use <c>UnityWebRequest.Get</c> to retrieve simple data (textual or binary) from a URI. While HTTP and HTTPS are common, other URI schemes are also supported, such as <c>file://</c>. Support for additional schemes is platform-dependent.
        ///
        ///This method creates a <c>UnityWebRequest</c> and sets the target URL to the <c>uri</c> argument specified as a string or <c>Uri</c> object. It sets no other custom flags or headers.
        ///
        ///By default, this method attaches a standard <see cref="DownloadHandlerBuffer" /> to the <c>UnityWebRequest</c>. This handler buffers the data received from the server and makes it available to your scripts when the request is complete. No <see cref="UploadHandler" /> is attached by default, but you can attach one manually.</remarks>
        ///<param name="uri">The URI of the resource to retrieve via HTTP GET.</param>
        ///<returns>An object that retrieves data from the uri.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        /// // UnityWebRequest.Get example
        ///
        /// // Access a website and use UnityWebRequest.Get to download a page.
        /// // Also try to download a non-existing page. Display the error.
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // A correct website page.
        ///        StartCoroutine(GetRequest("https://www.example.com"));
        ///
        ///        // A non-existing page.
        ///        StartCoroutine(GetRequest("https://error.html"));
        ///    }
        ///
        ///    IEnumerator GetRequest(string uri)
        ///    {
        ///        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        ///        {
        ///            // Request and wait for the desired page.
        ///            yield return webRequest.SendWebRequest();
        ///
        ///            string[] pages = uri.Split('/');
        ///            int page = pages.Length - 1;
        ///
        ///            switch (webRequest.result)
        ///            {
        ///                case UnityWebRequest.Result.ConnectionError:
        ///                case UnityWebRequest.Result.DataProcessingError:
        ///                    Debug.LogError(pages[page] + ": Error: " + webRequest.error);
        ///                    break;
        ///                case UnityWebRequest.Result.ProtocolError:
        ///                    Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
        ///                    break;
        ///                case UnityWebRequest.Result.Success:
        ///                    Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
        ///                    break;
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest Get(string uri)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbGET, new DownloadHandlerBuffer(), null);
            return request;
        }

        ///<summary>Create a UnityWebRequest for HTTP GET.</summary>
        ///<remarks>Use <c>UnityWebRequest.Get</c> to retrieve simple data (textual or binary) from a URI. While HTTP and HTTPS are common, other URI schemes are also supported, such as <c>file://</c>. Support for additional schemes is platform-dependent.
        ///
        ///This method creates a <c>UnityWebRequest</c> and sets the target URL to the <c>uri</c> argument specified as a string or <c>Uri</c> object. It sets no other custom flags or headers.
        ///
        ///By default, this method attaches a standard <see cref="DownloadHandlerBuffer" /> to the <c>UnityWebRequest</c>. This handler buffers the data received from the server and makes it available to your scripts when the request is complete. No <see cref="UploadHandler" /> is attached by default, but you can attach one manually.</remarks>
        ///<param name="uri">The URI of the resource to retrieve via HTTP GET.</param>
        ///<returns>An object that retrieves data from the uri.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        /// // UnityWebRequest.Get example
        ///
        /// // Access a website and use UnityWebRequest.Get to download a page.
        /// // Also try to download a non-existing page. Display the error.
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // A correct website page.
        ///        StartCoroutine(GetRequest("https://www.example.com"));
        ///
        ///        // A non-existing page.
        ///        StartCoroutine(GetRequest("https://error.html"));
        ///    }
        ///
        ///    IEnumerator GetRequest(string uri)
        ///    {
        ///        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        ///        {
        ///            // Request and wait for the desired page.
        ///            yield return webRequest.SendWebRequest();
        ///
        ///            string[] pages = uri.Split('/');
        ///            int page = pages.Length - 1;
        ///
        ///            switch (webRequest.result)
        ///            {
        ///                case UnityWebRequest.Result.ConnectionError:
        ///                case UnityWebRequest.Result.DataProcessingError:
        ///                    Debug.LogError(pages[page] + ": Error: " + webRequest.error);
        ///                    break;
        ///                case UnityWebRequest.Result.ProtocolError:
        ///                    Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
        ///                    break;
        ///                case UnityWebRequest.Result.Success:
        ///                    Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
        ///                    break;
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest Get(Uri uri)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbGET, new DownloadHandlerBuffer(), null);
            return request;
        }

        ///<summary>Creates a UnityWebRequest configured for HTTP <c>DELETE</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the verb to <c>DELETE</c> and sets the target URL to the string argument <c>uri</c>. It sets no custom flags or headers.
        ///
        ///This method attaches no <see cref="DownloadHandler" /> or <see cref="UploadHandler" /> to the <see cref="UnityWebRequest" />.</remarks>
        ///<param name="uri">The URI to which a <c>DELETE</c> request should be sent.</param>
        ///<returns>A UnityWebRequest configured to send an HTTP <c>DELETE</c> request.</returns>
        public static UnityWebRequest Delete(string uri)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbDELETE);
            return request;
        }

        ///<summary>Creates a UnityWebRequest configured for HTTP <c>DELETE</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the verb to <c>DELETE</c> and sets the target URL to the string argument <c>uri</c>. It sets no custom flags or headers.
        ///
        ///This method attaches no <see cref="DownloadHandler" /> or <see cref="UploadHandler" /> to the <see cref="UnityWebRequest" />.</remarks>
        ///<param name="uri">The URI to which a <c>DELETE</c> request should be sent.</param>
        ///<returns>A UnityWebRequest configured to send an HTTP <c>DELETE</c> request.</returns>
        public static UnityWebRequest Delete(Uri uri)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbDELETE);
            return request;
        }

        ///<summary>Creates a UnityWebRequest configured to send a HTTP <c>HEAD</c> request.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the verb to <c>HEAD</c> and sets the target URL to the string argument <c>uri</c>. It sets no custom flags or headers.
        ///
        ///This method attaches no <see cref="DownloadHandler" /> or <see cref="UploadHandler" /> to the <see cref="UnityWebRequest" />.</remarks>
        ///<param name="uri">The URI to which to send a HTTP <c>HEAD</c> request.</param>
        ///<returns>A UnityWebRequest configured to transmit a HTTP <c>HEAD</c> request.</returns>
        public static UnityWebRequest Head(string uri)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbHEAD);
            return request;
        }

        ///<summary>Creates a UnityWebRequest configured to send a HTTP <c>HEAD</c> request.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the verb to <c>HEAD</c> and sets the target URL to the string argument <c>uri</c>. It sets no custom flags or headers.
        ///
        ///This method attaches no <see cref="DownloadHandler" /> or <see cref="UploadHandler" /> to the <see cref="UnityWebRequest" />.</remarks>
        ///<param name="uri">The URI to which to send a HTTP <c>HEAD</c> request.</param>
        ///<returns>A UnityWebRequest configured to transmit a HTTP <c>HEAD</c> request.</returns>
        public static UnityWebRequest Head(Uri uri)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbHEAD);
            return request;
        }

        ///<summary>Creates a <see cref="UnityWebRequest" /> intended to download an image via HTTP GET and create a <see cref="Texture" /> based on the retrieved data.</summary>
        ///<remarks>**Obsolete** - instead use <see cref="M:UnityEngine.Networking.UnityWebRequestTexture.GetTexture(System.String)" />.
        ///
        ///This method creates a <see cref="UnityWebRequest" /> and sets the target URL to the string <c>uri</c> argument. This method sets no other flags or custom headers.
        ///
        ///This method attaches a <see cref="T:UnityEngine.Networking.DownloadHandlerTexture" /> object to the <see cref="UnityWebRequest" />. <see cref="T:UnityEngine.Networking.DownloadHandlerTexture" /> is a specialized <see cref="DownloadHandler" /> which is optimized for storing images which are to be used as textures in the Unity Engine. Using this class significantly reduces memory reallocation compared to downloading raw bytes and creating a texture manually in script. In addition, texture conversion will be performed on a worker thread.
        ///
        ///This method attaches no <see cref="UploadHandler" /> to the <see cref="UnityWebRequest" />.
        ///
        ///UnityWebRequest.GetTexture is obsolete. Use UnityWebRequestTexture.GetTexture instead.</remarks>
        ///<param name="uri">The URI of the image to download.</param>
        ///<returns>A <see cref="UnityWebRequest" /> properly configured to download an image and convert it to a <see cref="Texture" />.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("UnityWebRequest.GetTexture is obsolete. Use UnityWebRequestTexture.GetTexture instead (UnityUpgradable) -> [UnityEngine] UnityWebRequestTexture.GetTexture(*)", true)]
        public static UnityWebRequest GetTexture(string uri)
        {
            throw new NotSupportedException("UnityWebRequest.GetTexture is obsolete. Use UnityWebRequestTexture.GetTexture instead.");
        }

        ///<summary>Creates a <see cref="UnityWebRequest" /> intended to download an image via HTTP GET and create a <see cref="Texture" /> based on the retrieved data.</summary>
        ///<remarks>**Obsolete** - instead use <see cref="M:UnityEngine.Networking.UnityWebRequestTexture.GetTexture(System.String)" />.
        ///
        ///This method creates a <see cref="UnityWebRequest" /> and sets the target URL to the string <c>uri</c> argument. This method sets no other flags or custom headers.
        ///
        ///This method attaches a <see cref="T:UnityEngine.Networking.DownloadHandlerTexture" /> object to the <see cref="UnityWebRequest" />. <see cref="T:UnityEngine.Networking.DownloadHandlerTexture" /> is a specialized <see cref="DownloadHandler" /> which is optimized for storing images which are to be used as textures in the Unity Engine. Using this class significantly reduces memory reallocation compared to downloading raw bytes and creating a texture manually in script. In addition, texture conversion will be performed on a worker thread.
        ///
        ///This method attaches no <see cref="UploadHandler" /> to the <see cref="UnityWebRequest" />.
        ///
        ///UnityWebRequest.GetTexture is obsolete. Use UnityWebRequestTexture.GetTexture instead.</remarks>
        ///<param name="uri">The URI of the image to download.</param>
        ///<param name="nonReadable">If true, the texture's raw data will not be accessible to script. This can conserve memory. Default: <c>false</c>.</param>
        ///<returns>A <see cref="UnityWebRequest" /> properly configured to download an image and convert it to a <see cref="Texture" />.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("UnityWebRequest.GetTexture is obsolete. Use UnityWebRequestTexture.GetTexture instead (UnityUpgradable) -> [UnityEngine] UnityWebRequestTexture.GetTexture(*)", true)]
        public static UnityWebRequest GetTexture(string uri, bool nonReadable)
        {
            throw new NotSupportedException("UnityWebRequest.GetTexture is obsolete. Use UnityWebRequestTexture.GetTexture instead.");
        }

        ///<summary>OBSOLETE. Use UnityWebRequestMultimedia.GetAudioClip().</summary>
        ///<seealso cref="M:UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(System.String,UnityEngine.AudioType)" />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("UnityWebRequest.GetAudioClip is obsolete. Use UnityWebRequestMultimedia.GetAudioClip instead (UnityUpgradable) -> [UnityEngine] UnityWebRequestMultimedia.GetAudioClip(*)", true)]
        public static UnityWebRequest GetAudioClip(string uri, AudioType audioType)
        {
            return null;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("UnityWebRequest.GetAssetBundle is obsolete. Use UnityWebRequestAssetBundle.GetAssetBundle instead (UnityUpgradable) -> [UnityEngine] UnityWebRequestAssetBundle.GetAssetBundle(*)", true)]
        public static UnityWebRequest GetAssetBundle(string uri)
        {
            return null;
        }

        ///<summary>Deprecated. Replaced by <see cref="M:UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(System.String)" />.</summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("UnityWebRequest.GetAssetBundle is obsolete. Use UnityWebRequestAssetBundle.GetAssetBundle instead (UnityUpgradable) -> [UnityEngine] UnityWebRequestAssetBundle.GetAssetBundle(*)", true)]
        public static UnityWebRequest GetAssetBundle(string uri, uint crc)
        {
            return null;
        }

        ///<summary>Deprecated. Replaced by <see cref="M:UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(System.String)" />.</summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("UnityWebRequest.GetAssetBundle is obsolete. Use UnityWebRequestAssetBundle.GetAssetBundle instead (UnityUpgradable) -> [UnityEngine] UnityWebRequestAssetBundle.GetAssetBundle(*)", true)]
        public static UnityWebRequest GetAssetBundle(string uri, uint version, uint crc)
        {
            return null;
        }

        ///<summary>Deprecated. Replaced by <see cref="M:UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(System.String)" />.</summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("UnityWebRequest.GetAssetBundle is obsolete. Use UnityWebRequestAssetBundle.GetAssetBundle instead (UnityUpgradable) -> [UnityEngine] UnityWebRequestAssetBundle.GetAssetBundle(*)", true)]
        public static UnityWebRequest GetAssetBundle(string uri, Hash128 hash, uint crc)
        {
            return null;
        }

        ///<summary>Deprecated. Replaced by <see cref="M:UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(System.String)" />.</summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("UnityWebRequest.GetAssetBundle is obsolete. Use UnityWebRequestAssetBundle.GetAssetBundle instead (UnityUpgradable) -> [UnityEngine] UnityWebRequestAssetBundle.GetAssetBundle(*)", true)]
        public static UnityWebRequest GetAssetBundle(string uri, CachedAssetBundle cachedAssetBundle, uint crc)
        {
            return null;
        }

        ///<summary>Creates a UnityWebRequest configured to upload raw data to a remote server via HTTP PUT.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the target URL to the string <c>uri</c> argument and the <c>method</c> to <c>PUT</c>. It also sets the <c>Content-Type</c> header to <c>application/octet-stream</c>.
        ///
        ///This method attaches a standard <see cref="DownloadHandlerBuffer" /> to the UnityWebRequest. This is for convenience during development, as well as for applications which return status information regarding the uploaded data in the HTTP response body.
        ///
        ///This method stores the input upload data in an <see cref="UploadHandlerRaw" /> object and attaches it to the <see cref="UnityWebRequest" />. <see cref="UploadHandlerRaw" /> copies the input data into a buffer. Therefore, changes to the <c>bodyData</c> array performed after the call to this method will not be reflected in the data sent to the server.</remarks>
        ///<param name="uri">The URI to which the data will be sent.</param>
        ///<param name="bodyData">The data to transmit to the remote server.
        ///
        ///If a string, the string will be converted to raw bytes via &lt;a href="https://msdn.microsoft.com/en-us/library/system.text.encoding.utf8"&gt;System.Text.Encoding.UTF8&lt;/a&gt;.</param>
        ///<returns>A UnityWebRequest configured to transmit <c>bodyData</c> to <c>uri</c> via HTTP PUT.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class MyBehavior : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        byte[] myData = System.Text.Encoding.UTF8.GetBytes("This is some test data");
        ///        using (UnityWebRequest www = UnityWebRequest.Put("https://www.my-server.com/upload", myData))
        ///        {
        ///            yield return www.SendWebRequest();
        ///
        ///            if (www.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.Log(www.error);
        ///            }
        ///            else
        ///            {
        ///                Debug.Log("Upload complete!");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest Put(string uri, byte[] bodyData)
        {
            UnityWebRequest request = new UnityWebRequest(
                uri,
                kHttpVerbPUT,
                new DownloadHandlerBuffer(),
                new UploadHandlerRaw(bodyData)
            );

            return request;
        }

        ///<summary>Creates a UnityWebRequest configured to upload raw data to a remote server via HTTP PUT.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the target URL to the string <c>uri</c> argument and the <c>method</c> to <c>PUT</c>. It also sets the <c>Content-Type</c> header to <c>application/octet-stream</c>.
        ///
        ///This method attaches a standard <see cref="DownloadHandlerBuffer" /> to the UnityWebRequest. This is for convenience during development, as well as for applications which return status information regarding the uploaded data in the HTTP response body.
        ///
        ///This method stores the input upload data in an <see cref="UploadHandlerRaw" /> object and attaches it to the <see cref="UnityWebRequest" />. <see cref="UploadHandlerRaw" /> copies the input data into a buffer. Therefore, changes to the <c>bodyData</c> array performed after the call to this method will not be reflected in the data sent to the server.</remarks>
        ///<param name="uri">The URI to which the data will be sent.</param>
        ///<param name="bodyData">The data to transmit to the remote server.
        ///
        ///If a string, the string will be converted to raw bytes via &lt;a href="https://msdn.microsoft.com/en-us/library/system.text.encoding.utf8"&gt;System.Text.Encoding.UTF8&lt;/a&gt;.</param>
        ///<returns>A UnityWebRequest configured to transmit <c>bodyData</c> to <c>uri</c> via HTTP PUT.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class MyBehavior : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        byte[] myData = System.Text.Encoding.UTF8.GetBytes("This is some test data");
        ///        using (UnityWebRequest www = UnityWebRequest.Put("https://www.my-server.com/upload", myData))
        ///        {
        ///            yield return www.SendWebRequest();
        ///
        ///            if (www.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.Log(www.error);
        ///            }
        ///            else
        ///            {
        ///                Debug.Log("Upload complete!");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest Put(Uri uri, byte[] bodyData)
        {
            UnityWebRequest request = new UnityWebRequest(
                uri,
                kHttpVerbPUT,
                new DownloadHandlerBuffer(),
                new UploadHandlerRaw(bodyData)
            );

            return request;
        }

        ///<summary>Creates a UnityWebRequest configured to upload raw data to a remote server via HTTP PUT.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the target URL to the string <c>uri</c> argument and the <c>method</c> to <c>PUT</c>. It also sets the <c>Content-Type</c> header to <c>application/octet-stream</c>.
        ///
        ///This method attaches a standard <see cref="DownloadHandlerBuffer" /> to the UnityWebRequest. This is for convenience during development, as well as for applications which return status information regarding the uploaded data in the HTTP response body.
        ///
        ///This method stores the input upload data in an <see cref="UploadHandlerRaw" /> object and attaches it to the <see cref="UnityWebRequest" />. <see cref="UploadHandlerRaw" /> copies the input data into a buffer. Therefore, changes to the <c>bodyData</c> array performed after the call to this method will not be reflected in the data sent to the server.</remarks>
        ///<param name="uri">The URI to which the data will be sent.</param>
        ///<param name="bodyData">The data to transmit to the remote server.
        ///
        ///If a string, the string will be converted to raw bytes via &lt;a href="https://msdn.microsoft.com/en-us/library/system.text.encoding.utf8"&gt;System.Text.Encoding.UTF8&lt;/a&gt;.</param>
        ///<returns>A UnityWebRequest configured to transmit <c>bodyData</c> to <c>uri</c> via HTTP PUT.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class MyBehavior : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        byte[] myData = System.Text.Encoding.UTF8.GetBytes("This is some test data");
        ///        using (UnityWebRequest www = UnityWebRequest.Put("https://www.my-server.com/upload", myData))
        ///        {
        ///            yield return www.SendWebRequest();
        ///
        ///            if (www.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.Log(www.error);
        ///            }
        ///            else
        ///            {
        ///                Debug.Log("Upload complete!");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest Put(string uri, string bodyData)
        {
            UnityWebRequest request = new UnityWebRequest(
                uri,
                kHttpVerbPUT,
                new DownloadHandlerBuffer(),
                new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(bodyData))
            );

            return request;
        }

        ///<summary>Creates a UnityWebRequest configured to upload raw data to a remote server via HTTP PUT.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the target URL to the string <c>uri</c> argument and the <c>method</c> to <c>PUT</c>. It also sets the <c>Content-Type</c> header to <c>application/octet-stream</c>.
        ///
        ///This method attaches a standard <see cref="DownloadHandlerBuffer" /> to the UnityWebRequest. This is for convenience during development, as well as for applications which return status information regarding the uploaded data in the HTTP response body.
        ///
        ///This method stores the input upload data in an <see cref="UploadHandlerRaw" /> object and attaches it to the <see cref="UnityWebRequest" />. <see cref="UploadHandlerRaw" /> copies the input data into a buffer. Therefore, changes to the <c>bodyData</c> array performed after the call to this method will not be reflected in the data sent to the server.</remarks>
        ///<param name="uri">The URI to which the data will be sent.</param>
        ///<param name="bodyData">The data to transmit to the remote server.
        ///
        ///If a string, the string will be converted to raw bytes via &lt;a href="https://msdn.microsoft.com/en-us/library/system.text.encoding.utf8"&gt;System.Text.Encoding.UTF8&lt;/a&gt;.</param>
        ///<returns>A UnityWebRequest configured to transmit <c>bodyData</c> to <c>uri</c> via HTTP PUT.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class MyBehavior : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        byte[] myData = System.Text.Encoding.UTF8.GetBytes("This is some test data");
        ///        using (UnityWebRequest www = UnityWebRequest.Put("https://www.my-server.com/upload", myData))
        ///        {
        ///            yield return www.SendWebRequest();
        ///
        ///            if (www.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.Log(www.error);
        ///            }
        ///            else
        ///            {
        ///                Debug.Log("Upload complete!");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest Put(Uri uri, string bodyData)
        {
            UnityWebRequest request = new UnityWebRequest(
                uri,
                kHttpVerbPUT,
                new DownloadHandlerBuffer(),
                new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(bodyData))
            );

            return request;
        }

        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("UnityWebRequest.Post with only a string data is obsolete. Use UnityWebRequest.Post with content type argument or UnityWebRequest.PostWwwForm instead (UnityUpgradable) -> [UnityEngine] UnityWebRequest.PostWwwForm(*)", false)]
        public static UnityWebRequest Post(string uri, string postData)
        {
            return PostWwwForm(uri, postData);
        }

        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("UnityWebRequest.Post with only a string data is obsolete. Use UnityWebRequest.Post with content type argument or UnityWebRequest.PostWwwForm instead (UnityUpgradable) -> [UnityEngine] UnityWebRequest.PostWwwForm(*)", false)]
        public static UnityWebRequest Post(Uri uri, string postData)
        {
            return PostWwwForm(uri, postData);
        }

        ///<summary>Creates a UnityWebRequest configured to send form data to a server via HTTP <c>POST</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the <c>uri</c> and sets the method to <c>POST</c>. The <c>Content-Type</c> header will be set to <c>application/x-www-form-urlencoded</c> by default.
        ///
        ///This method attaches a <see cref="DownloadHandlerBuffer" /> to the <see cref="UnityWebRequest" />. This is for convenience, as we anticipate most users will use the <see cref="DownloadHandler" /> to check replies from the server, particularly in the case of REST APIs.
        ///
        ///The string in the <c>form</c> parameter is expected to be a preformatted HTML form. It will be escaped and sent as UTF-8 string. Note that characters <c>=</c> and <c>&amp;</c> are not escaped as they separate fields and values. If your form contains values with these characters, use <see cref="Post(Uri, WWWForm)"/> instead.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<param name="form">An HTML form to send.</param>
        ///<returns>A UnityWebRequest configured to send form data to <c>uri</c> via <c>POST</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class MyBehavior : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        using (UnityWebRequest www = UnityWebRequest.PostWwwForm("https://www.my-server.com/myapi", "field1=1&field2=value2"))
        ///        {
        ///            yield return www.SendWebRequest();
        ///
        ///            if (www.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.LogError(www.error);
        ///            }
        ///            else
        ///            {
        ///                Debug.Log("Form upload complete!");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest PostWwwForm(string uri, string form)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);
            SetupPostWwwForm(request, form);
            return request;
        }

        ///<summary>Creates a UnityWebRequest configured to send form data to a server via HTTP <c>POST</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the <c>uri</c> and sets the method to <c>POST</c>. The <c>Content-Type</c> header will be set to <c>application/x-www-form-urlencoded</c> by default.
        ///
        ///This method attaches a <see cref="DownloadHandlerBuffer" /> to the <see cref="UnityWebRequest" />. This is for convenience, as we anticipate most users will use the <see cref="DownloadHandler" /> to check replies from the server, particularly in the case of REST APIs.
        ///
        ///The string in the <c>form</c> parameter is expected to be a preformatted HTML form. It will be escaped and sent as UTF-8 string. Note that characters <c>=</c> and <c>&amp;</c> are not escaped as they separate fields and values. If your form contains values with these characters, use <see cref="Post(string, WWWForm)"/> instead.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<param name="form">An HTML form to send.</param>
        ///<returns>A UnityWebRequest configured to send form data to <c>uri</c> via <c>POST</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class MyBehavior : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        using (UnityWebRequest www = UnityWebRequest.PostWwwForm("https://www.my-server.com/myapi", "field1=1&field2=value2"))
        ///        {
        ///            yield return www.SendWebRequest();
        ///
        ///            if (www.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.LogError(www.error);
        ///            }
        ///            else
        ///            {
        ///                Debug.Log("Form upload complete!");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest PostWwwForm(Uri uri, string form)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);
            SetupPostWwwForm(request, form);
            return request;
        }

        private static void SetupPostWwwForm(UnityWebRequest request, string postData)
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            if (string.IsNullOrEmpty(postData))
                return;  // no data to send, nothing more to setup
            byte[] payload = null;
            string urlencoded = WWWTranscoder.WwwFormEncode(postData, System.Text.Encoding.UTF8);
            payload = System.Text.Encoding.UTF8.GetBytes(urlencoded);
            request.uploadHandler = new UploadHandlerRaw(payload);
            request.uploadHandler.contentType = "application/x-www-form-urlencoded";
        }

        ///<summary>Creates a UnityWebRequest configured to send form data to a server via HTTP <c>POST</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the <c>url</c> to the string <c>uri</c> argument and sets the <c>method</c> to <c>POST</c>. The <c>Content-Type</c> header will be set to <c>contentType</c>.
        ///
        ///This method attaches a <see cref="DownloadHandlerBuffer" /> to the <see cref="UnityWebRequest" />. This is for convenience, as we anticipate most users will use the <see cref="DownloadHandler" /> to check replies from the server, particularly in the case of REST APIs.
        ///
        ///The data in <c>postData</c> will be interpreted into a byte stream via &lt;a href="https://msdn.microsoft.com/en-us/library/system.text.encoding.utf8"&gt;System.Text.Encoding.UTF8&lt;/a&gt;. The resulting byte stream will be stored in an <see cref="UploadHandlerRaw" /> and the Upload Handler will be attached to this UnityWebRequest.</remarks>
        ///<param name="uri">The target URI to which the string will be transmitted.</param>
        ///<param name="postData">Form body data. Will be converted to UTF-8 string.</param>
        ///<param name="contentType">Value for the Content-Type header, for example application/json.</param>
        ///<returns>A UnityWebRequest configured to send string to <c>uri</c> via <c>POST</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class MyBehavior : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        using (UnityWebRequest www = UnityWebRequest.Post("https://www.my-server.com/myapi", "{ \"field1\": 1, \"field2\": 2 }", "application/json"))
        ///        {
        ///            yield return www.SendWebRequest();
        ///
        ///            if (www.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.LogError(www.error);
        ///            }
        ///            else
        ///            {
        ///                Debug.Log("Form upload complete!");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest Post(string uri, string postData, string contentType)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);
            SetupPost(request, postData, contentType);
            return request;
        }

        ///<summary>Creates a UnityWebRequest configured to send form data to a server via HTTP <c>POST</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the <c>url</c> to the string <c>uri</c> argument and sets the <c>method</c> to <c>POST</c>. The <c>Content-Type</c> header will be set to <c>contentType</c>.
        ///
        ///This method attaches a <see cref="DownloadHandlerBuffer" /> to the <see cref="UnityWebRequest" />. This is for convenience, as we anticipate most users will use the <see cref="DownloadHandler" /> to check replies from the server, particularly in the case of REST APIs.
        ///
        ///The data in <c>postData</c> will be interpreted into a byte stream via &lt;a href="https://msdn.microsoft.com/en-us/library/system.text.encoding.utf8"&gt;System.Text.Encoding.UTF8&lt;/a&gt;. The resulting byte stream will be stored in an <see cref="UploadHandlerRaw" /> and the Upload Handler will be attached to this UnityWebRequest.</remarks>
        ///<param name="uri">The target URI to which the string will be transmitted.</param>
        ///<param name="postData">Form body data. Will be converted to UTF-8 string.</param>
        ///<param name="contentType">Value for the Content-Type header, for example application/json.</param>
        ///<returns>A UnityWebRequest configured to send string to <c>uri</c> via <c>POST</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class MyBehavior : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        using (UnityWebRequest www = UnityWebRequest.Post("https://www.my-server.com/myapi", "{ \"field1\": 1, \"field2\": 2 }", "application/json"))
        ///        {
        ///            yield return www.SendWebRequest();
        ///
        ///            if (www.result != UnityWebRequest.Result.Success)
        ///            {
        ///                Debug.LogError(www.error);
        ///            }
        ///            else
        ///            {
        ///                Debug.Log("Form upload complete!");
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest Post(Uri uri, string postData, string contentType)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);
            SetupPost(request, postData, contentType);
            return request;
        }

        private static void SetupPost(UnityWebRequest request, string postData, string contentType)
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            if (string.IsNullOrEmpty(postData))
            {
                request.SetRequestHeader("Content-Type", contentType);
                return;  // no data to send, nothing more to setup
            }
            byte[] payload = System.Text.Encoding.UTF8.GetBytes(postData);
            request.uploadHandler = new UploadHandlerRaw(payload);
            request.uploadHandler.contentType = contentType;
        }

        // Provides a shim for sending a multipart form as declared by the legacy WWWForm class.
        ///<summary>Create a UnityWebRequest configured to send form data to a server via HTTP <c>POST</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the <c>url</c> to the string <c>uri</c> argument and sets the <c>method</c> to <c>POST</c>. The <c>Content-Type</c> header will be copied from the <c>formData</c> parameter.
        ///
        ///This method attaches a <see cref="DownloadHandlerBuffer" /> to the <see cref="UnityWebRequest" />. This is for convenience, as we anticipate most users will use the <see cref="DownloadHandler" /> to check replies from the server, particularly in the case of REST APIs.
        ///
        ///The <c>formData</c> object will generate an appropriately-formatted byte stream, depending on its contents. The resulting byte stream will be stored in an <see cref="UploadHandlerRaw" /> and the Upload Handler will be attached to this UnityWebRequest.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<param name="formData">Form fields or files encapsulated in a <see cref="WWWForm" /> object, for formatting and transmission to the remote server.</param>
        ///<returns>A UnityWebRequest configured to send form data to <c>uri</c> via <c>POST</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class MyBehavior2 : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        WWWForm form = new WWWForm();
        ///        form.AddField("myField", "myData");
        ///
        ///        using UnityWebRequest www = UnityWebRequest.Post("https://www.my-server.com/myform", form);
        ///        yield return www.SendWebRequest();
        ///
        ///        if (www.result != UnityWebRequest.Result.Success)
        ///        {
        ///            Debug.LogError(www.error);
        ///        }
        ///        else
        ///        {
        ///            Debug.Log("Form upload complete!");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest Post(string uri, WWWForm formData)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);
            SetupPost(request, formData);
            return request;
        }

        ///<summary>Create a UnityWebRequest configured to send form data to a server via HTTP <c>POST</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the <c>url</c> to the string <c>uri</c> argument and sets the <c>method</c> to <c>POST</c>. The <c>Content-Type</c> header will be copied from the <c>formData</c> parameter.
        ///
        ///This method attaches a <see cref="DownloadHandlerBuffer" /> to the <see cref="UnityWebRequest" />. This is for convenience, as we anticipate most users will use the <see cref="DownloadHandler" /> to check replies from the server, particularly in the case of REST APIs.
        ///
        ///The <c>formData</c> object will generate an appropriately-formatted byte stream, depending on its contents. The resulting byte stream will be stored in an <see cref="UploadHandlerRaw" /> and the Upload Handler will be attached to this UnityWebRequest.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<param name="formData">Form fields or files encapsulated in a <see cref="WWWForm" /> object, for formatting and transmission to the remote server.</param>
        ///<returns>A UnityWebRequest configured to send form data to <c>uri</c> via <c>POST</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///
        ///public class MyBehavior2 : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        WWWForm form = new WWWForm();
        ///        form.AddField("myField", "myData");
        ///
        ///        using UnityWebRequest www = UnityWebRequest.Post("https://www.my-server.com/myform", form);
        ///        yield return www.SendWebRequest();
        ///
        ///        if (www.result != UnityWebRequest.Result.Success)
        ///        {
        ///            Debug.LogError(www.error);
        ///        }
        ///        else
        ///        {
        ///            Debug.Log("Form upload complete!");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest Post(Uri uri, WWWForm formData)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);
            SetupPost(request, formData);
            return request;
        }

        private static void SetupPost(UnityWebRequest request, WWWForm formData)
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            if (formData == null)
                return;
            byte[] payload = null;
            payload = formData.data;
            if (payload.Length == 0)
                payload = null;

            if (payload != null)
                request.uploadHandler = new UploadHandlerRaw(payload);

            Dictionary<string, string> formHeaders = formData.headers;
            foreach (KeyValuePair<string, string> header in formHeaders)
                request.SetRequestHeader(header.Key, header.Value);
        }

        // Provides a way to send a multipart form using the modern IMultipartFormSection API.
        ///<summary>Create a UnityWebRequest configured to send form data to a server via HTTP <c>POST</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the <c>url</c> to the string <c>uri</c> argument and sets the <c>method</c> to <c>POST</c>. The <c>Content-Type</c> header will be set to <c>multipart/form-data</c>, with an appropriate boundary specification.
        ///
        ///If you supply a custom <c>boundary</c> byte array, note that the sequence of bytes must be guaranteed to be unique and must not appear anywhere in the body of your form data. For more information on multipart forms and form boundaries, see &lt;a href="https://www.ietf.org/rfc/rfc2388.txt"&gt;RFC 2388&lt;/a&gt;.
        ///
        ///This method attaches a <see cref="DownloadHandlerBuffer" /> to the <see cref="UnityWebRequest" />. This is for convenience, as we anticipate most users will use the <see cref="DownloadHandler" /> to check replies from the server, particularly in the case of REST APIs.
        ///
        ///The List of <see cref="IMultipartFormSection" /> objects in <c>multipartFormSections</c> will be formatted into a valid multipart form body. Each object will be interpreted as a discrete form section. The byte stream resulting from formatting this multipart form body will be stored in an <see cref="UploadHandlerRaw" /> and attached to this UnityWebRequest.
        ///
        ///**Using IMultipartFormSection**
        ///
        ///To provide greater control over how you specify your form data, the UnityWebRequest system contains a (user-implementable) <see cref="IMultipartFormSection" /> interface. For standard applications, Unity also provides default implementations for data and file sections.
        ///
        ///
        ///
        ///A List of IMultipartFormSection objects can be provided to this method. The list's members will be formatted into a multipart form, as defined by &lt;a href="https://www.ietf.org/rfc/rfc2388.txt"&gt;RFC 2388&lt;/a&gt;.
        ///
        ///Multipart forms require a unique boundary string to define the separation between fields. The boundary string must be guaranteed to not be present anywhere within the body of any form field in the request. If you do not supply a boundary, Unity will generate one. The generated boundary is 40 random printable bytes, which effectively never collide with form field data. However, if your application requires you to supply a custom boundary string, you may do so.
        ///
        ///The supplied boundary, if any, will be automatically converted from a byte array to UTF8 characters.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<param name="multipartFormSections">A list of form fields or files to be formatted and transmitted to the remote server.</param>
        ///<returns>A UnityWebRequest configured to send form data to <c>uri</c> via <c>POST</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///
        ///public class MyBehavior3 : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        List<IMultipartFormSection> form = new();
        ///        form.Add(new MultipartFormDataSection("myField", "myData"));
        ///
        ///        using UnityWebRequest www = UnityWebRequest.Post("https://httpbin.org/post", form);
        ///        yield return www.SendWebRequest();
        ///
        ///        if (www.result != UnityWebRequest.Result.Success)
        ///        {
        ///            Debug.LogError(www.error);
        ///        }
        ///        else
        ///        {
        ///            Debug.Log("Form upload complete!");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="MultipartFormDataSection" />
        ///<seealso cref="MultipartFormFileSection" />
        public static UnityWebRequest Post(string uri, List<IMultipartFormSection> multipartFormSections)
        {
            byte[] boundary = GenerateBoundary();
            return Post(uri, multipartFormSections, boundary);
        }

        ///<summary>Create a UnityWebRequest configured to send form data to a server via HTTP <c>POST</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the <c>url</c> to the string <c>uri</c> argument and sets the <c>method</c> to <c>POST</c>. The <c>Content-Type</c> header will be set to <c>multipart/form-data</c>, with an appropriate boundary specification.
        ///
        ///If you supply a custom <c>boundary</c> byte array, note that the sequence of bytes must be guaranteed to be unique and must not appear anywhere in the body of your form data. For more information on multipart forms and form boundaries, see &lt;a href="https://www.ietf.org/rfc/rfc2388.txt"&gt;RFC 2388&lt;/a&gt;.
        ///
        ///This method attaches a <see cref="DownloadHandlerBuffer" /> to the <see cref="UnityWebRequest" />. This is for convenience, as we anticipate most users will use the <see cref="DownloadHandler" /> to check replies from the server, particularly in the case of REST APIs.
        ///
        ///The List of <see cref="IMultipartFormSection" /> objects in <c>multipartFormSections</c> will be formatted into a valid multipart form body. Each object will be interpreted as a discrete form section. The byte stream resulting from formatting this multipart form body will be stored in an <see cref="UploadHandlerRaw" /> and attached to this UnityWebRequest.
        ///
        ///**Using IMultipartFormSection**
        ///
        ///To provide greater control over how you specify your form data, the UnityWebRequest system contains a (user-implementable) <see cref="IMultipartFormSection" /> interface. For standard applications, Unity also provides default implementations for data and file sections.
        ///
        ///
        ///
        ///A List of IMultipartFormSection objects can be provided to this method. The list's members will be formatted into a multipart form, as defined by &lt;a href="https://www.ietf.org/rfc/rfc2388.txt"&gt;RFC 2388&lt;/a&gt;.
        ///
        ///Multipart forms require a unique boundary string to define the separation between fields. The boundary string must be guaranteed to not be present anywhere within the body of any form field in the request. If you do not supply a boundary, Unity will generate one. The generated boundary is 40 random printable bytes, which effectively never collide with form field data. However, if your application requires you to supply a custom boundary string, you may do so.
        ///
        ///The supplied boundary, if any, will be automatically converted from a byte array to UTF8 characters.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<param name="multipartFormSections">A list of form fields or files to be formatted and transmitted to the remote server.</param>
        ///<returns>A UnityWebRequest configured to send form data to <c>uri</c> via <c>POST</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///
        ///public class MyBehavior3 : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        List<IMultipartFormSection> form = new();
        ///        form.Add(new MultipartFormDataSection("myField", "myData"));
        ///
        ///        using UnityWebRequest www = UnityWebRequest.Post("https://httpbin.org/post", form);
        ///        yield return www.SendWebRequest();
        ///
        ///        if (www.result != UnityWebRequest.Result.Success)
        ///        {
        ///            Debug.LogError(www.error);
        ///        }
        ///        else
        ///        {
        ///            Debug.Log("Form upload complete!");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="MultipartFormDataSection" />
        ///<seealso cref="MultipartFormFileSection" />
        public static UnityWebRequest Post(Uri uri, List<IMultipartFormSection> multipartFormSections)
        {
            byte[] boundary = GenerateBoundary();
            return Post(uri, multipartFormSections, boundary);
        }

        ///<summary>Create a UnityWebRequest configured to send form data to a server via HTTP <c>POST</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the <c>url</c> to the string <c>uri</c> argument and sets the <c>method</c> to <c>POST</c>. The <c>Content-Type</c> header will be set to <c>multipart/form-data</c>, with an appropriate boundary specification.
        ///
        ///If you supply a custom <c>boundary</c> byte array, note that the sequence of bytes must be guaranteed to be unique and must not appear anywhere in the body of your form data. For more information on multipart forms and form boundaries, see &lt;a href="https://www.ietf.org/rfc/rfc2388.txt"&gt;RFC 2388&lt;/a&gt;.
        ///
        ///This method attaches a <see cref="DownloadHandlerBuffer" /> to the <see cref="UnityWebRequest" />. This is for convenience, as we anticipate most users will use the <see cref="DownloadHandler" /> to check replies from the server, particularly in the case of REST APIs.
        ///
        ///The List of <see cref="IMultipartFormSection" /> objects in <c>multipartFormSections</c> will be formatted into a valid multipart form body. Each object will be interpreted as a discrete form section. The byte stream resulting from formatting this multipart form body will be stored in an <see cref="UploadHandlerRaw" /> and attached to this UnityWebRequest.
        ///
        ///**Using IMultipartFormSection**
        ///
        ///To provide greater control over how you specify your form data, the UnityWebRequest system contains a (user-implementable) <see cref="IMultipartFormSection" /> interface. For standard applications, Unity also provides default implementations for data and file sections.
        ///
        ///
        ///
        ///A List of IMultipartFormSection objects can be provided to this method. The list's members will be formatted into a multipart form, as defined by &lt;a href="https://www.ietf.org/rfc/rfc2388.txt"&gt;RFC 2388&lt;/a&gt;.
        ///
        ///Multipart forms require a unique boundary string to define the separation between fields. The boundary string must be guaranteed to not be present anywhere within the body of any form field in the request. If you do not supply a boundary, Unity will generate one. The generated boundary is 40 random printable bytes, which effectively never collide with form field data. However, if your application requires you to supply a custom boundary string, you may do so.
        ///
        ///The supplied boundary, if any, will be automatically converted from a byte array to UTF8 characters.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<param name="multipartFormSections">A list of form fields or files to be formatted and transmitted to the remote server.</param>
        ///<param name="boundary">A unique boundary string, which will be used when separating form fields in a multipart form.  If not supplied, a boundary will be generated for you.</param>
        ///<returns>A UnityWebRequest configured to send form data to <c>uri</c> via <c>POST</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///
        ///public class MyBehavior3 : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        List<IMultipartFormSection> form = new();
        ///        form.Add(new MultipartFormDataSection("myField", "myData"));
        ///
        ///        using UnityWebRequest www = UnityWebRequest.Post("https://httpbin.org/post", form);
        ///        yield return www.SendWebRequest();
        ///
        ///        if (www.result != UnityWebRequest.Result.Success)
        ///        {
        ///            Debug.LogError(www.error);
        ///        }
        ///        else
        ///        {
        ///            Debug.Log("Form upload complete!");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="MultipartFormDataSection" />
        ///<seealso cref="MultipartFormFileSection" />
        public static UnityWebRequest Post(string uri, List<IMultipartFormSection> multipartFormSections, byte[] boundary)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);
            SetupPost(request, multipartFormSections, boundary);
            return request;
        }

        ///<summary>Create a UnityWebRequest configured to send form data to a server via HTTP <c>POST</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the <c>url</c> to the string <c>uri</c> argument and sets the <c>method</c> to <c>POST</c>. The <c>Content-Type</c> header will be set to <c>multipart/form-data</c>, with an appropriate boundary specification.
        ///
        ///If you supply a custom <c>boundary</c> byte array, note that the sequence of bytes must be guaranteed to be unique and must not appear anywhere in the body of your form data. For more information on multipart forms and form boundaries, see &lt;a href="https://www.ietf.org/rfc/rfc2388.txt"&gt;RFC 2388&lt;/a&gt;.
        ///
        ///This method attaches a <see cref="DownloadHandlerBuffer" /> to the <see cref="UnityWebRequest" />. This is for convenience, as we anticipate most users will use the <see cref="DownloadHandler" /> to check replies from the server, particularly in the case of REST APIs.
        ///
        ///The List of <see cref="IMultipartFormSection" /> objects in <c>multipartFormSections</c> will be formatted into a valid multipart form body. Each object will be interpreted as a discrete form section. The byte stream resulting from formatting this multipart form body will be stored in an <see cref="UploadHandlerRaw" /> and attached to this UnityWebRequest.
        ///
        ///**Using IMultipartFormSection**
        ///
        ///To provide greater control over how you specify your form data, the UnityWebRequest system contains a (user-implementable) <see cref="IMultipartFormSection" /> interface. For standard applications, Unity also provides default implementations for data and file sections.
        ///
        ///
        ///
        ///A List of IMultipartFormSection objects can be provided to this method. The list's members will be formatted into a multipart form, as defined by &lt;a href="https://www.ietf.org/rfc/rfc2388.txt"&gt;RFC 2388&lt;/a&gt;.
        ///
        ///Multipart forms require a unique boundary string to define the separation between fields. The boundary string must be guaranteed to not be present anywhere within the body of any form field in the request. If you do not supply a boundary, Unity will generate one. The generated boundary is 40 random printable bytes, which effectively never collide with form field data. However, if your application requires you to supply a custom boundary string, you may do so.
        ///
        ///The supplied boundary, if any, will be automatically converted from a byte array to UTF8 characters.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<param name="multipartFormSections">A list of form fields or files to be formatted and transmitted to the remote server.</param>
        ///<param name="boundary">A unique boundary string, which will be used when separating form fields in a multipart form.  If not supplied, a boundary will be generated for you.</param>
        ///<returns>A UnityWebRequest configured to send form data to <c>uri</c> via <c>POST</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///
        ///public class MyBehavior3 : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        List<IMultipartFormSection> form = new();
        ///        form.Add(new MultipartFormDataSection("myField", "myData"));
        ///
        ///        using UnityWebRequest www = UnityWebRequest.Post("https://httpbin.org/post", form);
        ///        yield return www.SendWebRequest();
        ///
        ///        if (www.result != UnityWebRequest.Result.Success)
        ///        {
        ///            Debug.LogError(www.error);
        ///        }
        ///        else
        ///        {
        ///            Debug.Log("Form upload complete!");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="MultipartFormDataSection" />
        ///<seealso cref="MultipartFormFileSection" />
        public static UnityWebRequest Post(Uri uri, List<IMultipartFormSection> multipartFormSections, byte[] boundary)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);
            SetupPost(request, multipartFormSections, boundary);
            return request;
        }

        private static void SetupPost(UnityWebRequest request, List<IMultipartFormSection> multipartFormSections, byte[] boundary)
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            byte[] payload = null;
            if (multipartFormSections != null && multipartFormSections.Count != 0)
                payload = SerializeFormSections(multipartFormSections, boundary);

            if (payload == null)
                return;
            UploadHandler uploadHandler = new UploadHandlerRaw(payload);
            uploadHandler.contentType = "multipart/form-data; boundary=" + System.Text.Encoding.UTF8.GetString(boundary, 0, boundary.Length);

            request.uploadHandler = uploadHandler;
        }

        // Provides a way to send a simple urlencoded form body, for simple forms without file sections.
        ///<summary>Create a UnityWebRequest configured to send form data to a server via HTTP <c>POST</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the <c>url</c> to the string <c>uri</c> argument and sets the <c>method</c> to <c>POST</c>. The <c>Content-Type</c> header will be set to <c>application/x-www-form-urlencoded</c>.
        ///
        ///The Dictionary of strings in <c>formFields</c> will be interpreted as a list of form fields whose field IDs are the dictionary keys, and whose field values are the dictionary values. Both keys and values will be escaped, and then joined into a URL-encoded form string. (for example, <c>key1=value1&amp;key2=value2</c>).
        ///
        ///This method, by default, attaches a <see cref="DownloadHandlerBuffer" /> to the <see cref="UnityWebRequest" />. This is for convenience, as we anticipate most users will use the <see cref="DownloadHandler" /> to check replies from the server, particularly in the case of REST APIs.
        ///
        ///The URL-encoded form string generated from <c>formFields</c> will be converted into a byte stream and stored in an <see cref="UploadHandlerRaw" />, which will be attached to this UnityWebRequest.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<param name="formFields">Strings indicating the keys and values of form fields. Will be automatically formatted into a URL-encoded form body.</param>
        ///<returns>A UnityWebRequest configured to send form data to <c>uri</c> via <c>POST</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///
        ///public class MyBehavior4 : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        Dictionary<string, string> form = new();
        ///        form["myField"] = "myData";
        ///
        ///        using UnityWebRequest www = UnityWebRequest.Post("https://httpbin.org/post", form);
        ///        yield return www.SendWebRequest();
        ///
        ///        if (www.result != UnityWebRequest.Result.Success)
        ///        {
        ///            Debug.LogError(www.error);
        ///        }
        ///        else
        ///        {
        ///            Debug.Log("Form upload complete!");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest Post(string uri, Dictionary<string, string> formFields)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);
            SetupPost(request, formFields);
            return request;
        }

        ///<summary>Create a UnityWebRequest configured to send form data to a server via HTTP <c>POST</c>.</summary>
        ///<remarks>This method creates a UnityWebRequest, sets the <c>url</c> to the string <c>uri</c> argument and sets the <c>method</c> to <c>POST</c>. The <c>Content-Type</c> header will be set to <c>application/x-www-form-urlencoded</c>.
        ///
        ///The Dictionary of strings in <c>formFields</c> will be interpreted as a list of form fields whose field IDs are the dictionary keys, and whose field values are the dictionary values. Both keys and values will be escaped, and then joined into a URL-encoded form string. (for example, <c>key1=value1&amp;key2=value2</c>).
        ///
        ///This method, by default, attaches a <see cref="DownloadHandlerBuffer" /> to the <see cref="UnityWebRequest" />. This is for convenience, as we anticipate most users will use the <see cref="DownloadHandler" /> to check replies from the server, particularly in the case of REST APIs.
        ///
        ///The URL-encoded form string generated from <c>formFields</c> will be converted into a byte stream and stored in an <see cref="UploadHandlerRaw" />, which will be attached to this UnityWebRequest.</remarks>
        ///<param name="uri">The target URI to which form data will be transmitted.</param>
        ///<param name="formFields">Strings indicating the keys and values of form fields. Will be automatically formatted into a URL-encoded form body.</param>
        ///<returns>A UnityWebRequest configured to send form data to <c>uri</c> via <c>POST</c>.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///
        ///public class MyBehavior4 : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        StartCoroutine(Upload());
        ///    }
        ///
        ///    IEnumerator Upload()
        ///    {
        ///        Dictionary<string, string> form = new();
        ///        form["myField"] = "myData";
        ///
        ///        using UnityWebRequest www = UnityWebRequest.Post("https://httpbin.org/post", form);
        ///        yield return www.SendWebRequest();
        ///
        ///        if (www.result != UnityWebRequest.Result.Success)
        ///        {
        ///            Debug.LogError(www.error);
        ///        }
        ///        else
        ///        {
        ///            Debug.Log("Form upload complete!");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static UnityWebRequest Post(Uri uri, Dictionary<string, string> formFields)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);
            SetupPost(request, formFields);
            return request;
        }

        private static void SetupPost(UnityWebRequest request, Dictionary<string, string> formFields)
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            byte[] payload = null;
            if (formFields != null && formFields.Count != 0)
                payload = SerializeSimpleForm(formFields);

            if (payload == null)
                return;
            UploadHandler formUploadHandler = new UploadHandlerRaw(payload);
            formUploadHandler.contentType = "application/x-www-form-urlencoded";

            request.uploadHandler = formUploadHandler;
        }


        ///<summary>Escapes characters in a string to ensure they are URL-friendly.</summary>
        ///<remarks>Certain text characters have special meanings when present in URLs. If you need to include those characters in URL parameters then you must represent them with escape sequences. It is recommended that you use this function on any text supplied by a user before passing the text as a URL parameter. This will ensure that a malicious user can't manipulate the contents of the URL to attack the webserver.
        ///See Also: <see cref="UnityWebRequest.UnEscapeURL" />.</remarks>
        ///<param name="s">A string with characters to be escaped.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        string escName = UnityWebRequest.EscapeURL("Fish & Chips");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string EscapeURL(string s)
        {
            return EscapeURL(s, System.Text.Encoding.UTF8);
        }

        ///<summary>Escapes characters in a string to ensure they are URL-friendly.</summary>
        ///<remarks>Certain text characters have special meanings when present in URLs. If you need to include those characters in URL parameters then you must represent them with escape sequences. It is recommended that you use this function on any text supplied by a user before passing the text as a URL parameter. This will ensure that a malicious user can't manipulate the contents of the URL to attack the webserver.
        ///See Also: <see cref="UnityWebRequest.UnEscapeURL" />.</remarks>
        ///<param name="s">A string with characters to be escaped.</param>
        ///<param name="e">The text encoding to use.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        string escName = UnityWebRequest.EscapeURL("Fish & Chips");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string EscapeURL(string s, Encoding e)
        {
            if (s == null)
                return null;

            if (s == "")
                return "";

            if (e == null)
                return null;

            var bytes = e.GetBytes(s);
            var decodedBytes = WWWTranscoder.URLEncode(bytes);
            return e.GetString(decodedBytes);
        }

        ///<summary>Converts URL-friendly escape sequences back to normal text.</summary>
        ///<remarks>Certain text characters have special meanings when present in URLs. If you need to include those characters in URL parameters then you must represent them with escape sequences. This function takes a string containing these escape sequences and converts them back to normal text.
        ///See Also: <see cref="UnityWebRequest.EscapeURL" />.</remarks>
        ///<param name="s">A string containing escaped characters.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        string plainName = UnityWebRequest.UnEscapeURL("Fish+%26+Chips");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string UnEscapeURL(string s)
        {
            return UnEscapeURL(s, System.Text.Encoding.UTF8);
        }

        ///<summary>Converts URL-friendly escape sequences back to normal text.</summary>
        ///<remarks>Certain text characters have special meanings when present in URLs. If you need to include those characters in URL parameters then you must represent them with escape sequences. This function takes a string containing these escape sequences and converts them back to normal text.
        ///See Also: <see cref="UnityWebRequest.EscapeURL" />.</remarks>
        ///<param name="s">A string containing escaped characters.</param>
        ///<param name="e">The text encoding to use.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Networking;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        string plainName = UnityWebRequest.UnEscapeURL("Fish+%26+Chips");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string UnEscapeURL(string s, Encoding e)
        {
            if (null == s)
                return null;

            if (s.IndexOf('%') == -1 && s.IndexOf('+') == -1)
                return s;

            var bytes = e.GetBytes(s);
            var decodedBytes = WWWTranscoder.URLDecode(bytes);
            return e.GetString(decodedBytes);
        }

        ///<summary>Converts a List of IMultipartFormSection objects into a byte array containing raw multipart form data.</summary>
        ///<param name="multipartFormSections">A List of <see cref="IMultipartFormSection" /> objects.</param>
        ///<param name="boundary">A unique boundary string to separate the form sections.</param>
        ///<returns>A byte array of raw multipart form data.</returns>
        ///<seealso cref="GenerateBoundary" />
        public static byte[] SerializeFormSections(List<IMultipartFormSection> multipartFormSections, byte[] boundary)
        {
            if (multipartFormSections == null || multipartFormSections.Count == 0)
                return null;

            byte[] crlf = System.Text.Encoding.UTF8.GetBytes("\r\n");
            byte[] dDash = WWWForm.DefaultEncoding.GetBytes("--");

            int estimatedSize = 0;
            foreach (IMultipartFormSection section in multipartFormSections)
            {
                estimatedSize += 64 + section.sectionData.Length;
            }

            List<byte> formData = new List<byte>(estimatedSize);
            foreach (IMultipartFormSection section in multipartFormSections)
            {
                string disposition = "form-data";

                string sectionName = section.sectionName;
                string fileName = section.fileName;

                string header = "Content-Disposition: " + disposition;

                if (!string.IsNullOrEmpty(sectionName))
                {
                    header += "; name=\"" + sectionName + "\"";
                }

                if (!string.IsNullOrEmpty(fileName))
                {
                    header += "; filename=\"" + fileName + "\"";
                }

                header += "\r\n";

                string contentType = section.contentType;
                if (!string.IsNullOrEmpty(contentType))
                {
                    header += "Content-Type: " + contentType + "\r\n";
                }

                formData.AddRange(crlf);
                formData.AddRange(dDash);
                formData.AddRange(boundary);
                formData.AddRange(crlf);
                formData.AddRange(System.Text.Encoding.UTF8.GetBytes(header));
                formData.AddRange(crlf);
                formData.AddRange(section.sectionData);
            }

            // end sections with boundary delimiter (https://tools.ietf.org/html/rfc2046)
            formData.AddRange(crlf);
            formData.AddRange(dDash);
            formData.AddRange(boundary);
            formData.AddRange(dDash);
            formData.AddRange(crlf);
            return formData.ToArray();
        }

        ///<summary>Generate a random 40-byte array for use as a multipart form boundary.</summary>
        ///<returns>40 random bytes, guaranteed to contain only printable ASCII values.</returns>
        public static byte[] GenerateBoundary()
        {
            // Generate a random boundary
            byte[] boundary = new byte[40];
            for (int i = 0; i < 40; i++)
            {
                int randomChar = Random.Range(48, 110);
                if (randomChar > 57) // skip unprintable chars between 57 and 64 (inclusive)
                    randomChar += 7;
                if (randomChar > 90) // and 91 and 96 (inclusive)
                    randomChar += 6;
                boundary[i] = (byte)randomChar;
            }
            return boundary;
        }

        ///<summary>Serialize a dictionary of strings into a byte array containing URL-encoded UTF8 characters.</summary>
        ///<remarks>This method will URL-encode the strings, then concatenate them as if they were in an HTTP query string. Keys and values will be separated with an equals sign (=) and different key-value pairs will be separated with ampersands (&amp;).</remarks>
        ///<param name="formFields">A dictionary containing the form keys and values to serialize.</param>
        ///<returns>A byte array containing the serialized form. The form's keys and values have been URL-encoded.</returns>
        public static byte[] SerializeSimpleForm(Dictionary<string, string> formFields)
        {
            string queryString = "";
            foreach (KeyValuePair<string, string> pair in formFields)
            {
                if (queryString.Length > 0) { queryString += "&"; }
                queryString += WWWTranscoder.DataEncode(pair.Key) + "=" + WWWTranscoder.DataEncode(pair.Value);
            }
            return System.Text.Encoding.UTF8.GetBytes(queryString);
        }
    }
}
