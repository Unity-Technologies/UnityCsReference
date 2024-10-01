// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    internal static class UnityConnectWebRequestUtils
    {
        internal static UnityConnectWebRequestException CreateUnityWebRequestException(UnityWebRequest request,
            string message)
            => new(L10n.Tr(message))
            {
                error = request.error,
                method = request.method,
                timeout = request.timeout,
                url = request.url,
                responseHeaders = request.GetResponseHeaders(),
                responseCode = request.responseCode,
                isHttpError = request.result == UnityWebRequest.Result.ProtocolError,
                isNetworkError = request.result == UnityWebRequest.Result.ConnectionError
            };

        /// <summary>
        /// Used to determine if the UnityWebRequest had an error or error code
        /// </summary>
        internal static bool IsRequestError(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(request.error))
            {
                return true;
            }

            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                    return true;
            }

            if (request.responseCode is < 200 or >= 300)
            {
                return true;
            }

            return false;
        }

        internal static bool IsUnityWebRequestReadyForJsonExtract(UnityWebRequest unityWebRequest)
        {
            return !IsRequestError(unityWebRequest)
                   && !string.IsNullOrEmpty(unityWebRequest.downloadHandler.text);
        }

        /// <summary>
        /// Used to run a UnityWebRequest on the main thread in an awaitable manner, while handling CancellationToken
        /// </summary>
        internal static async Task SendWebRequestAsync(
            UnityWebRequest unityWebRequest,
            CancellationToken cancellationToken = default)
        {
            var webRequestTask = AsyncUtils.RunUnityWebRequestOnMainThread(unityWebRequest);

            while (!unityWebRequest.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    unityWebRequest.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Yield();
            }

            await webRequestTask;
        }
    }
}
