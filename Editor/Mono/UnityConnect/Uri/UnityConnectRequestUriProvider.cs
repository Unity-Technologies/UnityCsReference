// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEditor.Connect
{
    class UnityConnectRequestUriProvider : IUnityConnectRequestUriProvider
    {
        const string k_DefaultGenesisUrl = "https://core.cloud.unity3d.com/api";
        const string k_DefaultServicesGatewayUrl = "https://services.unity.com/api";

        static readonly Dictionary<ServicesGatewayUriId, string> k_ServicesGatewayUriPathParameters = new()
        {
            {ServicesGatewayUriId.GetOrganizationDetails, "/api/unity/legacy/v1/organizations/{0}"},
            {ServicesGatewayUriId.GetOrganizations, "/api/unity/legacy/v1/users/me/organizations"},
            {ServicesGatewayUriId.GetProjectsForOrganization, "/api/unity/legacy/v1/organizations/{0}/projects?limit=999999"},
            {ServicesGatewayUriId.CreateProject, "/api/unity/legacy/v1/organizations/{0}/projects"},
            {ServicesGatewayUriId.GetUserForProject, "/api/unity/legacy/v1/projects/{0}/users/me"}
        };

        static readonly Dictionary<GenesisUriId, string> k_GenesisUriPathParameters = new()
        {
            { GenesisUriId.LegacyGetOrganizationDetails, "/orgs/{0}" }
        };

        public async Task<string> GetUri(GenesisUriId uriId)
        {
            var uri = await RequestAsyncUrl(ServicesConfiguration.AsyncUrlId.ApiUrl, k_DefaultGenesisUrl);
            return uri + k_GenesisUriPathParameters[uriId];
        }

        public async Task<string> GetUri(ServicesGatewayUriId uriId)
        {
            var uri = await RequestAsyncUrl(ServicesConfiguration.AsyncUrlId.ServicesGatewayUrl, k_DefaultServicesGatewayUrl);
            return uri + k_ServicesGatewayUriPathParameters[uriId];
        }

        static Task<string> RequestAsyncUrl(ServicesConfiguration.AsyncUrlId asyncUrlId, string defaultUrl)
        {
            var tcs = new TaskCompletionSource<string>();

            try
            {
                ServicesConfiguration.instance.RequestAsyncUrl(
                    asyncUrlId,
                    UrlCallback);
            }
            catch (Exception e)
            {
                AsyncUtils.RunNextActionOnMainThread(() =>
                {
                    Debug.LogException(e);
                });

                tcs.SetResult(defaultUrl);
            }

            return tcs.Task;

            void UrlCallback(string returnValue)
            {
                tcs.SetResult(returnValue);
            }
        }
    }
}

