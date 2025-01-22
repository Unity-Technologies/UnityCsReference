// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    class TokenExchange : ITokenExchange
    {
        const string k_RequestContentType = "application/json";

        const string k_SerializationFailureMessage =
            "Token Exchange failed due to an issue with serialization/deserialization. ";
        const string k_WebRequestFailureMessage =
            "Token Exchange failed due a failure with the web request.";
        const string k_PayloadDeserializationFailureMessage =
            k_SerializationFailureMessage + "Payload that failed to deserialize: ";
        const string k_KeyMissingSerializationFailureMessage =
            k_SerializationFailureMessage + "Deserialized response does not contain the key: ";

        public async Task<string> GetServiceTokenAsync(
            string genesisToken,
            CancellationToken cancellationToken = default)
        {
            var tokenExchangeRequest = new TokenExchangeRequest(genesisToken);
            Dictionary<string, object> deserializedResponse;

            var exchangeResult = await TokenExchangeRequestAsync(tokenExchangeRequest, cancellationToken);

            try
            {
                deserializedResponse = Json.Deserialize(exchangeResult.ResponseJson) as Dictionary<string, object>;
            }
            catch (Exception exception)
            {
                throw new SerializationException(k_PayloadDeserializationFailureMessage +
                                                 $"'{exchangeResult.ResponseJson}'", exception);
            }

            if (deserializedResponse is null)
            {
                throw new SerializationException(k_PayloadDeserializationFailureMessage +
                                                 $"'{exchangeResult.ResponseJson}'");
            }

            if (!TokenExchangeResponseContainsTokenKey(deserializedResponse))
            {
                throw new SerializationException(k_KeyMissingSerializationFailureMessage +
                                                 $"'{nameof(TokenExchangeResponse.token)}'");
            }

            return deserializedResponse[nameof(TokenExchangeResponse.token)].ToString();
        }

        async Task<TokenExchangeResult> TokenExchangeRequestAsync(
            TokenExchangeRequest tokenExchangeRequest,
            CancellationToken cancellationToken = default)
        {
            var jsonPayload = Json.Serialize(tokenExchangeRequest);
            var postBytes = Encoding.UTF8.GetBytes(jsonPayload);
            var endpoint = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.ServicesGateway)
                + "/api/auth/v1/genesis-token-exchange/unity";

            using (var exchangeRequest = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
            {
                exchangeRequest.uploadHandler = new UploadHandlerRaw(postBytes) { contentType = k_RequestContentType };
                exchangeRequest.downloadHandler = new DownloadHandlerBuffer();

                var exchangeTask = exchangeRequest.SendWebRequest();

                while (!exchangeRequest.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        exchangeRequest.Abort();
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await Task.Yield();
                }

                await exchangeTask;

                VerifyTokenExchangeResponse(exchangeRequest);

                return new TokenExchangeResult(
                    exchangeRequest.result.ToString(),
                    exchangeRequest.error,
                    exchangeRequest.responseCode.ToString(),
                    exchangeRequest.downloadHandler.text);
            }
        }

        static void VerifyTokenExchangeResponse(UnityWebRequest exchangeRequest)
        {
            if (UnityConnectWebRequestUtils.IsUnityWebRequestReadyForJsonExtract(exchangeRequest))
            {
                return;
            }

            throw UnityConnectWebRequestUtils
                .CreateUnityWebRequestException(exchangeRequest, k_WebRequestFailureMessage);
        }


        bool TokenExchangeResponseContainsTokenKey(Dictionary<string, object> deserializedResponse)
            => deserializedResponse.ContainsKey(nameof(TokenExchangeResponse.token));
    }

    struct TokenExchangeResult
    {
        public string Result { get; }
        public string Error { get; }
        public string ResponseCode { get; }
        public string ResponseJson { get; }

        public TokenExchangeResult(string result, string error, string responseCode, string responseJson)
        {
            Result = result;
            Error = error;
            ResponseCode = responseCode;
            ResponseJson = responseJson;
        }
    }
}
