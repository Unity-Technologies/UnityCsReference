// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    /// <summary>
    /// Class containing the legacy identity api requests
    /// Documentation for the apis: https://services.docs.internal.unity3d.com/unity-services-gateway/api-documentation/routes/unity/legacy/v1
    /// </summary>
    internal static class UnityConnectRequests
    {
        static readonly IUnityConnectRequestUriProvider k_UnityConnectRequestUriProvider =
            new UnityConnectRequestUriProvider();

        const string k_NetworkIssueWarningMessage = "This might be caused by network issues. Try using the refresh button.";
        const string k_CouldNotCreateProjectMessage = "Could not create project with name \"{0}\".";
        const string k_CouldNotObtainProjectMessage = "Could not obtain projects within the \"{0}\" organization. " + k_NetworkIssueWarningMessage;
        const string k_CouldNotObtainOrganizationDetailsMessage = "Could not obtain organization details for organization \"{0}\". " + k_NetworkIssueWarningMessage;
        const string k_CouldNotObtainOrganizationsMessage = "Could not obtain organizations for user \"{0}\". " + k_NetworkIssueWarningMessage;
        const string k_ParsingErrorWhileFetchingProjects = "Parsing error while fetching projects. Json: \"{0}\".";
        const string k_ParsingErrorWhileFetchingOrganizationDetails = "Parsing error while fetching organization details. Json: \"{0}\".";
        const string k_ParsingErrorWhileFetchingOrganizations = "Parsing error while fetching organizations. Json: \"{0}\".";
        const string k_ParsingErrorWhileCreatingProject = "Parsing error while creating a cloud project. Json: \"{0}\".";
        const string k_ErrorGettingProjectNameWhileCreatingProject = "Error getting the next available cloud project name for organization \"{0}\" and name \"{1}\".";
        const string k_FailedToGetServiceToken =
            "Failed to get the service authentication token. Try again or contact Unity support.";
        const string k_ErrorOperationCancelled = "The following operation was cancelled: {0}. Error: {1}.";

        /// <summary>
        /// Adds the authentication authentication token to a UnityWebRequest
        /// </summary>
        static void AddAccessTokenHeaderToRequest(UnityWebRequest webRequest)
        {
            string accessToken;

            try
            {
                accessToken = CloudProjectSettings.accessToken;
            }
            catch (Exception e)
            {
                throw new Exception(k_FailedToGetServiceToken, e);
            }

            webRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {accessToken}");
        }

        /// <summary>
        /// Adds the service authentication token to a UnityWebRequest
        /// </summary>
        /// <param name="webRequest">Web request to add the service token header to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        static async Task AddServiceTokenHeaderToRequestAsync(
            UnityWebRequest webRequest,
            CancellationToken cancellationToken = default)
        {
            var serviceToken = string.Empty;

            try
            {
                await AsyncUtils.RunNextActionOnMainThread(async () =>
                {
                    serviceToken = await CloudProjectSettings.GetServiceTokenAsync(cancellationToken);
                });
            }
            catch (OperationCanceledException e)
            {
                await AsyncUtils.RunNextActionOnMainThread(() =>
                {
                    Debug.LogError(string.Format(k_ErrorOperationCancelled, "Get Service Token", e.Message));
                });
                throw;
            }
            catch (Exception e)
            {
                throw new Exception(k_FailedToGetServiceToken, e);
            }

            webRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {serviceToken}");
        }

        /// <summary>
        /// Lists all of the current user's organizations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        internal static async Task<List<OrganizationRequestResponse>> GetOrganizationsAsync(
            CancellationToken cancellationToken = default)
        {
            var organizationsInfo = new List<OrganizationRequestResponse>();
            var uri = await k_UnityConnectRequestUriProvider.GetUri(ServicesGatewayUriId.GetOrganizations);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                try
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.suppressErrorsToConsole = true;
                    await AddServiceTokenHeaderToRequestAsync(request, cancellationToken);
                    await UnityConnectWebRequestUtils.SendWebRequestAsync(request, cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    await AsyncUtils.RunNextActionOnMainThread(() =>
                    {
                        Debug.LogError(string.Format(k_ErrorOperationCancelled, "Get Organizations", e.Message));
                    });
                    throw;
                }

                if (UnityConnectWebRequestUtils.IsRequestError(request))
                {
                    var exception = UnityConnectWebRequestUtils.CreateUnityWebRequestException(
                        request,
                        string.Format(k_CouldNotObtainOrganizationsMessage, CloudProjectSettings.userName));
                    throw exception;
                }

                try
                {
                    var deserializedResponse =
                        Json.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;

                    if (deserializedResponse == null)
                    {
                        throw new JSONParseException($"Failed to parse JSON response: {request.downloadHandler.text}");
                    }

                    var organizations = deserializedResponse["organizations"] as List<object>;
                    foreach (var org in organizations)
                    {
                        var orgDict = org as Dictionary<string, object>;
                        if (orgDict != null)
                        {
                            // We don't get the legacyId information through this call, so we have to make an
                            // additional call to LegacyGetOrganizationIdAsync before binding
                            organizationsInfo.Add(
                                new OrganizationRequestResponse(
                                    "",
                                    orgDict.GetValueOrDefault("id").ToString(),
                                    orgDict.GetValueOrDefault("genesisId").ToString(),
                                    orgDict.GetValueOrDefault("name").ToString(),
                                    orgDict.GetValueOrDefault("role").ToString()));
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(
                        string.Format(k_ParsingErrorWhileFetchingOrganizations, request.downloadHandler.text),
                        e);
                }
            }

            return organizationsInfo;
        }

        /// <summary>
        /// This is a call to the old/legacy endpoint. We're currently obligated to make that call to get the old
        /// organizationId in order to not break the CloudProjectSettings.OrganizationId public api.
        /// </summary>
        /// <returns></returns>
        /// <param name="genesisOrganizationId">Genesis id of the organization</param>
        /// <param name="cancellationToken">Cancellation token</param>
        internal static async Task<OrganizationRequestResponse> LegacyGetOrganizationIdAsync(
            string genesisOrganizationId,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> deserializedResponse;
            var uri = await k_UnityConnectRequestUriProvider.GetUri(GenesisUriId.LegacyGetOrganizationDetails);
            uri = string.Format(uri, genesisOrganizationId);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                try
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.suppressErrorsToConsole = true;
                    AddAccessTokenHeaderToRequest(request);
                    await UnityConnectWebRequestUtils.SendWebRequestAsync(request, cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    await AsyncUtils.RunNextActionOnMainThread(() =>
                    {
                        Debug.LogError(string.Format(k_ErrorOperationCancelled, "Get Organization Id", e.Message));
                    });
                    throw;
                }

                if (UnityConnectWebRequestUtils.IsRequestError(request))
                {
                    var exception = UnityConnectWebRequestUtils.CreateUnityWebRequestException(
                        request,
                        string.Format(k_CouldNotObtainOrganizationDetailsMessage, genesisOrganizationId));
                    throw exception;
                }

                try
                {
                    deserializedResponse =
                        Json.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;
                }
                catch (Exception e)
                {
                    throw new Exception(
                        string.Format(k_ParsingErrorWhileFetchingOrganizationDetails, request.downloadHandler.text),
                        e);
                }
            }

            return new OrganizationRequestResponse(
                deserializedResponse.GetValueOrDefault("id").ToString(),
                "", // Information not available from this call
                "", // Information not available from this call
                deserializedResponse.GetValueOrDefault("name").ToString());
        }

        /// <summary>
        /// Lists all of the projects the user has access to within an organization
        /// </summary>
        /// <param name="genesisOrganizationId">Genesis id of the organization</param>
        /// <param name="cancellationToken">Cancellation token</param>
        internal static async Task<List<ProjectRequestResponse>> GetOrganizationProjectsAsync(
            string genesisOrganizationId,
            CancellationToken cancellationToken = default)
        {
            var projectsInfo = new List<ProjectRequestResponse>();
            var uri = await k_UnityConnectRequestUriProvider.GetUri(ServicesGatewayUriId.GetProjectsForOrganization);
            uri = string.Format(uri, genesisOrganizationId);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                try
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.suppressErrorsToConsole = true;
                    await AddServiceTokenHeaderToRequestAsync(request, cancellationToken);
                    await UnityConnectWebRequestUtils.SendWebRequestAsync(request, cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    await AsyncUtils.RunNextActionOnMainThread(() =>
                    {
                        Debug.LogError(string.Format(k_ErrorOperationCancelled, "Get Organization Projects", e.Message));
                    });
                    throw;
                }

                if (UnityConnectWebRequestUtils.IsRequestError(request))
                {
                    var exception = UnityConnectWebRequestUtils.CreateUnityWebRequestException(
                        request,
                        string.Format(k_CouldNotObtainProjectMessage, genesisOrganizationId));
                    throw exception;
                }

                try
                {
                    var deserializedResponse =
                        Json.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;

                    if (deserializedResponse == null)
                    {
                        throw new JSONParseException($"Failed to parse JSON response: {request.downloadHandler.text}");
                    }

                    var projects = deserializedResponse["results"] as List<object>;

                    foreach (var project in projects)
                    {
                        if (project is Dictionary<string, object> projectDict)
                        {
                            projectsInfo.Add(
                                new ProjectRequestResponse(
                                    projectDict.GetValueOrDefault("id").ToString(),
                                    projectDict.GetValueOrDefault("genesisId").ToString(),
                                    projectDict.GetValueOrDefault("name").ToString(),
                                    projectDict.GetValueOrDefault("coppa").ToString(),
                                    "", // Information not available from this call
                                    projectDict.GetValueOrDefault("organizationId").ToString(),
                                    projectDict.GetValueOrDefault("organizationGenesisId").ToString()));
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(
                        string.Format(k_ParsingErrorWhileFetchingProjects, request.downloadHandler.text),
                        e);
                }
            }

            return projectsInfo;
        }

        /// <summary>
        /// Creates a new cloud project under the specified organization
        /// </summary>
        /// <param name="genesisOrganizationId">Genesis id of the organization</param>
        /// <param name="projectName">Name that the project should be created with</param>
        /// <param name="cancellationToken">Cancellation token</param>
        internal static async Task<ProjectRequestResponse> CreateNewProjectInOrganizationAsync(
            string genesisOrganizationId,
            string projectName = null,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> deserializedResponse;
            var uri = await k_UnityConnectRequestUriProvider.GetUri(ServicesGatewayUriId.CreateProject);
            uri = string.Format(uri, genesisOrganizationId);

            try
            {
                projectName = await GetAvailableProjectName(genesisOrganizationId, cancellationToken, projectName);
            }
            catch (Exception e)
            {
                throw new Exception(
                    string.Format(k_ErrorGettingProjectNameWhileCreatingProject, genesisOrganizationId, projectName ??= Application.productName),
                    e);
            }

            using (UnityWebRequest request = UnityWebRequest.Post(
                       uri,
                       $"{{\"name\": \"{projectName}\", \"coppa\": \"not_compliant\"}}",
                       "application/json"))
            {
                try
                {
                    request.suppressErrorsToConsole = true;
                    await AddServiceTokenHeaderToRequestAsync(request, cancellationToken);
                    await UnityConnectWebRequestUtils.SendWebRequestAsync(request, cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    await AsyncUtils.RunNextActionOnMainThread(() =>
                    {
                        Debug.LogError(string.Format(k_ErrorOperationCancelled, "Create New Project", e.Message));
                    });
                    throw;
                }

                if (UnityConnectWebRequestUtils.IsRequestError(request))
                {
                    var exception = UnityConnectWebRequestUtils.CreateUnityWebRequestException(
                        request,
                        string.Format(k_CouldNotCreateProjectMessage, projectName ??= Application.productName));
                    throw exception;
                }

                try
                {
                    deserializedResponse =
                        Json.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;
                }
                catch (Exception e)
                {
                    throw new Exception(
                        string.Format(k_ParsingErrorWhileCreatingProject, request.downloadHandler.text),
                        e);
                }
            }

            return new ProjectRequestResponse(
                deserializedResponse.GetValueOrDefault("id").ToString(),
                deserializedResponse.GetValueOrDefault("genesisId").ToString(),
                deserializedResponse.GetValueOrDefault("name").ToString(),
                deserializedResponse.GetValueOrDefault("coppa").ToString(),
                "", // Information not available from this call
                deserializedResponse.GetValueOrDefault("organizationId").ToString(),
                deserializedResponse.GetValueOrDefault("organizationGenesisId").ToString()
                );
        }

        /// <summary>
        /// Gets the current user role for the project
        /// </summary>
        /// <param name="projectId">The project id for the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        internal static async Task<UserRequestResponse> GetCurrentUserRoleForProject(
            string projectId,
            CancellationToken cancellationToken = default)
        {
            UserRequestResponse userRequestResponse = null;
            var uri = await k_UnityConnectRequestUriProvider.GetUri(ServicesGatewayUriId.GetUserForProject);
            uri = string.Format(uri, projectId);

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                try
                {
                    request.suppressErrorsToConsole = true;
                    request.downloadHandler = new DownloadHandlerBuffer();
                    await AddServiceTokenHeaderToRequestAsync(request, cancellationToken);
                    await UnityConnectWebRequestUtils.SendWebRequestAsync(request, cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    await AsyncUtils.RunNextActionOnMainThread(() =>
                    {
                        Debug.LogError(string.Format(k_ErrorOperationCancelled, "Get Current User Role", e.Message));
                    });
                    throw;
                }

                if (UnityConnectWebRequestUtils.IsUnityWebRequestReadyForJsonExtract(request))
                {
                    userRequestResponse = new UserRequestResponse(request.downloadHandler.text);
                }
                else
                {
                    throw new UnityConnectWebRequestException("Error while getting current user for project.")
                    {
                        url = request.url,
                        method = request.method,
                        error = request.error,
                        responseCode = request.responseCode
                    };
                }
            }

            return userRequestResponse;
        }

        /// <summary>
        /// Returns a project name that does not already exist on the cloud for the specified organization
        /// </summary>
        /// <param name="genesisOrganizationId">Genesis id of the organization</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="projectName">Name of the project</param>
        static async Task<string> GetAvailableProjectName(
            string genesisOrganizationId,
            CancellationToken cancellationToken = default,
            string projectName = null)
        {
            var projectCreationNameIteration = 0;
            projectName ??= Application.productName;
            var existingProjects =
                await GetOrganizationProjectsAsync(genesisOrganizationId, cancellationToken);

            for (int i = 0; i < existingProjects.Count; i++)
            {
                if (existingProjects[i].Name.Equals(projectName + GetProjectNameSuffix(projectCreationNameIteration)))
                {
                    projectCreationNameIteration++;
                    i = 0;
                }
            }

            projectName += GetProjectNameSuffix(projectCreationNameIteration);
            return projectName;

            string GetProjectNameSuffix(int iteration)
                => iteration > 0 ? $" ({iteration})" : string.Empty;
        }
    }
}
