// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Connect;

namespace UnityEditor.AssetPackage
{
    internal class SignatureRequest
    {
        public string artifactType { get; set; }
        public string integrity { get; set; }
        public string ownerOrgId { get; set; }
    }

    internal class SignatureResponseData
    {
        public string attestation { get; set; }
    }

    internal class SignatureResponse
    {
        public SignatureResponseData data { get; set; }
    }

    internal class SignatureService
    {
        string m_BaseUrl;
        string m_AccessToken;

        // This should only be instantiated in the main thread as it relies on UnityConnect which is not thread safe, but we want to be able to update the access token in case it changes during the editor session
        public SignatureService()
        {
            // We get those here while in the main thread to make sure UnityConnect is properly initialized and we have the correct access token and base url even if they change during the editor session
            m_BaseUrl = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudPackagesApi);

            // This should only be called in the main thread as it relies on UnityConnect which is not thread safe, but we want to be able to update the access token in case it changes during the editor session
            m_AccessToken = UnityConnect.instance.GetAccessToken();
        }

        public async Task<string> RequestAttestationFromPackageRegistry(string integrity, string ownerOrgId)
        {
            if (string.IsNullOrEmpty(m_AccessToken))
                throw new InvalidOperationException("Access token is not set.");

            var requestBody = new SignatureRequest
            {
                artifactType = "dotUnityPackage",
                integrity = integrity,
                ownerOrgId = ownerOrgId
            };

            var json = Json.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", m_AccessToken);

            try
            {
                var response = await httpClient.PostAsync(
                        $"{m_BaseUrl}/-/api/v1/signatures",
                        content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                            $"Request failed with status {response.StatusCode}: {errorContent}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var signatureResponse = DeserializeSignatureResponse(responseBody);

                if (signatureResponse.data.attestation == null)
                {
                    throw new FormatException($"Invalid attestation returned from the server.");
                }
                return signatureResponse.data.attestation;

            }
            catch (Exception ex)
            {
                throw new Exception("Failed to request signature " + ex.Message, ex);
            }
        }

        private static SignatureResponse DeserializeSignatureResponse(string json)
        {
            if (Json.Deserialize(json) is not Dictionary<string, object> root)
                throw new FormatException("Response is not a valid JSON object.");

            var result = new SignatureResponse();
            result.data = new SignatureResponseData();

            if (root.TryGetValue("data", out var dataObj) && dataObj is Dictionary<string, object> dataDict)
            {
                if (dataDict.TryGetValue("attestation", out var attestationObj) && attestationObj is string attestation)
                {
                    result.data.attestation = attestation;
                }
            }

            return result;
        }

    }

}
