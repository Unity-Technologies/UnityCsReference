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

namespace UnityEngine.Networking
{
    public sealed class UnityHttpMessageHandler : HttpMessageHandler
    {
        private static readonly string ContentTypeHeaderKey = "Content-Type";

        public HttpForcedVersion HttpForcedVersion { get; set; } = HttpForcedVersion.NotForced;
        public CertificateHandler CertificateHandler { get; set; } = null;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
        {
            return SendAsyncInternal(httpRequest, cancellationToken);
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
