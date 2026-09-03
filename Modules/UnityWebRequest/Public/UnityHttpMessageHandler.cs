// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine.Networking
{
    [VisibleToOtherModules("UnityEngine.UnityWinHttpMessageHandlerModule")]
    internal interface IUnityHttpMessageHandlerFactory
    {
        public HttpMessageHandler CreateUnderlyingHttpMessageHandler();
    }

    [VisibleToOtherModules("UnityEngine.UnityWinHttpMessageHandlerModule")]
    internal interface IUnityHttpMessageHandler
    {
        public CertificateHandler CertificateHandler { get; set; }
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken);
    }

    ///<summary>An <c>HttpMessageHandler</c> that can be used with <c>HttpClient</c> to perform web requests using <see cref="UnityWebRequest" />.</summary>
    ///<remarks>
    ///  <para>
    ///    <c>UnityHttpMessageHandler</c> enables sending web requests to HTTP servers using the standard <c>HttpClient</c>. This allows the use of libraries that expect to use <c>HttpClient</c>, but allow the developer to replace the underlying <c>HttpMessageHandler</c> that <c>HttpClient</c> uses. The request and response streams can be used to stream data for uploading and downloading, respectively.</para>
    ///  <para>
    ///    <c>UnityHttpMessageHandler</c> can be used with <c>GrpcChannel</c> to make gRPC calls.</para>
    ///  <para>In a basic use case, <c>UnityHttpMessageHandler</c> can be used with <c>HttpClient</c> to make HTTP requests.</para>
    ///</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[using UnityEngine;
    ///using System.Net.Http;
    ///using UnityEngine.Networking;
    ///using Grpc.Core;
    ///using Grpc.Net.Client;
    ///
    ///public class GrpcChannelFactory
    ///{
    ///    public static GrpcChannel CreateGrpcChannel()
    ///    {
    ///        var httpHandler = new UnityEngine.Networking.UnityHttpMessageHandler()
    ///        var channel = GrpcChannel.ForAddress("https://www.my-server.com", new GrpcChannelOptions
    ///        {
    ///            HttpHandler = httpHandler
    ///        });
    ///        return channel;
    ///    }
    ///}]]></code>
    ///</example>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using System.Net.Http;
    ///using UnityEngine.Networking;
    ///
    ///public class MyBehaviour : MonoBehaviour
    ///{
    ///    void Start()
    ///    {
    ///        DoRequestAsync();
    ///    }
    ///
    ///    async void DoRequestAsync()
    ///    {
    ///        HttpClient client = new HttpClient(new UnityEngine.Networking.UnityHttpMessageHandler());
    ///        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.my-server.com");
    ///        var response = await client.SendAsync(request);
    ///
    ///        var status = response.StatusCode;
    ///        var content = await response.Content.ReadAsStringAsync(); // content will contain the HTTP response body
    ///
    ///        // Show results as text
    ///        Debug.Log(content);
    ///    }
    ///}
    ///]]></code>
    ///</example>
    public sealed class UnityHttpMessageHandler : HttpMessageHandler
    {
        private static readonly string ContentTypeHeaderKey = "Content-Type";
        [NoAutoStaticsCleanup] // platform factory injected once at startup; resetting would break HTTP handling
        private static IUnityHttpMessageHandlerFactory s_underlyingHttpHandlerFactory = null;
        private HttpMessageHandler _underlyingHttpMessageHandler = null;
        private bool _underlyingMessageHandlerOwnsCertificateHandler = false;

        ///<summary>Force the version of HTTP used when making web requests with <see cref="UnityHttpMessageHandler" />.</summary>
        ///<remarks>
        ///  <para>Setting this property to <c>HttpForcedVersion.NotForced</c> causes <see cref="UnityHttpMessageHandler" /> to use standard negotiation with the server to determine the HTTP version to use.
        ///
        ///Using other values causes <see cref="UnityHttpMessageHandler" /> to force the web requests to a particular version of HTTP even if insecure HTTP is being used.
        ///
        ///Default value: <c>HttpForcedVersion.NotForced</c>.</para>
        ///  <para>Demonstrating how to force web requests to HTTP/2 using <c>UnityHttpMessageHandler</c>.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine.Networking;
        ///
        ///public class HttpForcedVersionExample
        ///{
        ///    public static UnityEngine.Networking.UnityHttpMessageHandler MakeHttpHandlerWithForcedHttp2()
        ///    {
        ///        var httpHandler = new UnityEngine.Networking.UnityHttpMessageHandler()
        ///        {
        ///            HttpForcedVersion = HttpForcedVersion.HTTP2
        ///        };
        ///        return httpHandler;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public HttpForcedVersion HttpForcedVersion { get; set; } = HttpForcedVersion.NotForced;
        ///<summary>Holds a reference to a <see cref="CertificateHandler" /> object, which manages certificate validation for the underlying <see cref="UnityWebRequest" /> that this <see cref="UnityHttpMessageHandler" /> creates.</summary>
        ///<remarks>Setting this property to <c>null</c> makes the platform use the default certificate validation, which validates certificates against a root certificate authority store (most commonly Operating System store).
        ///
        ///Not all platforms support certificate validation callbacks. Refer to <see cref="CertificateHandler" /> for a list of supported platforms.
        ///
        ///Default value: <c>null</c>.</remarks>
        public CertificateHandler CertificateHandler { get; set; } = null;

        ///<summary>Creates a <c>UnityHttpMessageHandler</c> with the default options.</summary>
        public UnityHttpMessageHandler() : base()
        {
            if (s_underlyingHttpHandlerFactory != null)
            {
                _underlyingHttpMessageHandler = s_underlyingHttpHandlerFactory.CreateUnderlyingHttpMessageHandler();
            }
        }

        [VisibleToOtherModules("UnityEngine.UnityWinHttpMessageHandlerModule")]
        internal static void SetIUnityHttpMessageHandlerFactory(IUnityHttpMessageHandlerFactory factory)
        {
            s_underlyingHttpHandlerFactory = factory;
        }

        ///<summary>Send an HTTP request as an asynchronous operation.</summary>
        ///<param name="httpRequest">The HTTP request message to send.</param>
        ///<param name="cancellationToken">The cancellation token to cancel operation.</param>
        ///<returns>The task object representing the asynchronous operation.</returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
        {
            if (_underlyingHttpMessageHandler != null && _underlyingHttpMessageHandler is IUnityHttpMessageHandler unityHandler)
            {
                unityHandler.CertificateHandler = CertificateHandler;
                _underlyingMessageHandlerOwnsCertificateHandler = true;
                return unityHandler.SendAsync(httpRequest, cancellationToken);
            }
            else
            {
                return SendAsyncInternal(httpRequest, cancellationToken);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (!_underlyingMessageHandlerOwnsCertificateHandler)
                {
                    CertificateHandler?.Dispose();
                    CertificateHandler = null;
                }

                _underlyingHttpMessageHandler?.Dispose();
                _underlyingHttpMessageHandler = null;
            }
        }

        private async Task<HttpResponseMessage> SendAsyncInternal(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
        {
            UploadHandlerStream uploadHandler = null;
            DownloadHandlerStream downloadHandler = null;
            UnityWebRequest unityWebRequest = null;

            try
            {
                await Awaitable.MainThreadAsync(); // UnityWebRequest must be created on the main thread
                unityWebRequest = new UnityWebRequest(httpRequest.RequestUri, httpRequest.Method.ToString());
                unityWebRequest.profilerSource = 1; // WebRequestProfilerSource: HttpClient
                unityWebRequest.httpForcedVersion = HttpForcedVersion;
                if (CertificateHandler != null)
                {
                    unityWebRequest.certificateHandler = CertificateHandler;
                    unityWebRequest.disposeCertificateHandlerOnDispose = false; // The HttpMessageHandler is used for multiple requests
                }
                cancellationToken.Register(() =>
                {
                    unityWebRequest.Abort(); // Completed event will Dispose
                });

                var requestContentType = SetWebRequestHeaders(unityWebRequest, httpRequest.Headers);
                if (httpRequest.Content != null)
                {
                    var contentType = SetWebRequestHeaders(unityWebRequest, httpRequest.Content.Headers);
                    contentType = contentType ?? requestContentType;
                    uploadHandler = new UploadHandlerStream();
                    if (!string.IsNullOrEmpty(contentType))
                    {
                        uploadHandler.contentType = contentType;
                    }
                    unityWebRequest.uploadHandler = uploadHandler;
                }

                downloadHandler = new DownloadHandlerStream();
                unityWebRequest.downloadHandler = downloadHandler;

                DownloadStream downloadStream = new DownloadStream(downloadHandler);
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage
                {
                    RequestMessage = httpRequest,
                    Content = new StreamHttpContent(downloadStream)
                };

                TaskCompletionSource<HttpResponseMessage> httpResponseTask = new TaskCompletionSource<HttpResponseMessage>();
                downloadHandler.headersCompleted += () =>
                {
                    var headers = unityWebRequest.GetResponseHeaders();
                    if (headers != null)
                    {
                        foreach (var key in headers.Keys)
                        {
                            var val = headers[key];
                            httpResponseMessage.Content.Headers.TryAddWithoutValidation(key, val);
                            httpResponseMessage.Headers.TryAddWithoutValidation(key, val);
                        }
                    }

                    httpResponseMessage.StatusCode = (HttpStatusCode)unityWebRequest.responseCode;
                    httpResponseMessage.Version = unityWebRequest.responseVersion;
                    if (!httpResponseTask.Task.IsCompleted)
                        httpResponseTask.SetResult(httpResponseMessage);
                };

                if (httpRequest.Content != null)
                {
                    _ = SendContentAsync(httpRequest, new UploadStream(uploadHandler), cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                unityWebRequest.SendWebRequest().completed += (_) =>
                {
                    var trailers = unityWebRequest.GetResponseTrailers();
                    if (trailers != null)
                    {
                        foreach (var key in trailers.Keys)
                        {
                            var val = trailers[key];
                            httpResponseMessage.TrailingHeaders.TryAddWithoutValidation(key, val);
                        }
                    }

                    downloadHandler.Close();
                    unityWebRequest.Dispose();
                };

                return await httpResponseTask.Task;
            }
            catch (OperationCanceledException)
            {
                uploadHandler?.Close();
                downloadHandler?.Close();
                unityWebRequest?.Dispose();
                throw;
            }
        }

        private string SetWebRequestHeaders(UnityWebRequest unityWebRequest, HttpHeaders headers)
        {
            string contentType = null;
            foreach (var kv in headers)
            {
                foreach (var headerItem in kv.Value)
                {
                    // Grab the content-type from the headers to set on the UploadHandler (if it is present)
                    if (string.Equals(kv.Key, ContentTypeHeaderKey, StringComparison.InvariantCultureIgnoreCase))
                    {
                        contentType = headerItem;
                    }

                    unityWebRequest.SetRequestHeader(kv.Key, headerItem);
                }
            }
            return contentType;
        }

        private async Task SendContentAsync(HttpRequestMessage httpRequest, UploadStream uploadStream, CancellationToken cancellationToken)
        {
            try
            {
                await httpRequest.Content.CopyToAsync(uploadStream).ConfigureAwait(false);
                await httpRequest.Content.ReadAsStreamAsync().ContinueWith(_ =>
                {
                    uploadStream.Close();
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                uploadStream.Close();
                throw;
            }
        }

        #region UploadStream
        internal class UploadStream : Stream
        {
            private UploadHandlerStream uploadHandler;

            public UploadStream(UploadHandlerStream uploadHandler)
            {
                this.uploadHandler = uploadHandler;
            }

            public override void Close()
            {
                base.Close();
                this.uploadHandler.Close();
                this.uploadHandler = null;
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new System.NotSupportedException();
            public override void SetLength(long value) => throw new System.NotSupportedException();
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => 0;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new System.NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                uploadHandler?.WriteData(buffer.AsSpan().Slice(offset, count));
            }
        }
        #endregion

        #region DownloadStream
        internal class DownloadStream : Stream
        {
            private DownloadHandlerStream downloadHandler;

            public DownloadStream(DownloadHandlerStream downloadHandler)
            {
                this.downloadHandler = downloadHandler;
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new System.NotSupportedException();
            public override void SetLength(long value) => throw new System.NotSupportedException();
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => 0;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return downloadHandler?.ReadData(buffer.AsSpan().Slice(offset, count)) ?? 0;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new System.NotImplementedException();
            }
        }
        #endregion

        #region StreamHttpContent
        internal class StreamHttpContent : HttpContent
        {
            private readonly Stream m_Stream;
            public StreamHttpContent(Stream memoryStream) => m_Stream = memoryStream;
            protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult(m_Stream);

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                if (stream == null) throw new ArgumentNullException(nameof(stream));
                await stream.FlushAsync().ConfigureAwait(false);

                // Copy in chunks to avoid high memory usage
                await m_Stream.CopyToAsync(stream, 8192, CancellationToken.None).ConfigureAwait(false);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = -1;
                return false;
            }
        }
        #endregion
    }
}
