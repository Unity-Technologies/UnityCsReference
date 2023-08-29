// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IHttpClientFactory : IService
    {
        IAsyncHTTPClient GetASyncHTTPClient(string url);
        IAsyncHTTPClient PostASyncHTTPClient(string url, string postData);
        void AbortByTag(string tag);
        Dictionary<string, object> ParseResponseAsDictionary(IAsyncHTTPClient request);
    }

    internal class HttpClientFactory : BaseService<IHttpClientFactory>, IHttpClientFactory
    {
        public static readonly string k_InvalidJSONErrorMessage = L10n.Tr("Server response is not a valid JSON");
        public static readonly string k_ServerErrorMessage = L10n.Tr("Server response is");

        [ExcludeFromCodeCoverage]
        public IAsyncHTTPClient GetASyncHTTPClient(string url)
        {
            return new AsyncHTTPClient(url);
        }

        [ExcludeFromCodeCoverage]
        public IAsyncHTTPClient PostASyncHTTPClient(string url, string postData)
        {
            return new AsyncHTTPClient(url, "POST") { postData = postData };
        }

        [ExcludeFromCodeCoverage]
        public void AbortByTag(string tag)
        {
            AsyncHTTPClient.AbortByTag(tag);
        }

        public Dictionary<string, object> ParseResponseAsDictionary(IAsyncHTTPClient request)
        {
            string errorMessage;
            if (request.IsSuccess() && request.responseCode == 200)
            {
                if (string.IsNullOrEmpty(request.text))
                    return null;

                try
                {
                    var response = Json.Deserialize(request.text) as Dictionary<string, object>;
                    if (response != null)
                        return response;

                    errorMessage = k_InvalidJSONErrorMessage;
                }
                catch (Exception e)
                {
                    errorMessage = $"{k_InvalidJSONErrorMessage} {e.Message}";
                }
            }
            else
            {
                if (request.responseCode == 0 &&
                    (string.IsNullOrEmpty(request.text) || request.text.ToLower().Contains("timeout")))
                    return null;

                var text = request.text.Length <= 128 ? request.text : request.text.Substring(0, 128) + "...";
                errorMessage = $"{k_ServerErrorMessage} \"{text}\" [Code {request.responseCode}]";
            }

            return string.IsNullOrEmpty(errorMessage) ? null : new Dictionary<string, object> { ["errorMessage"] = errorMessage };
        }
    }
}
