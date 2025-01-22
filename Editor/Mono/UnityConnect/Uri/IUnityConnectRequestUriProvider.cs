// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading.Tasks;

namespace UnityEditor.Connect
{
    interface IUnityConnectRequestUriProvider
    {
        Task<string> GetUri(ServicesGatewayUriId servicesGatewayUriId);
        Task<string> GetUri(GenesisUriId uriId);
    }
}

enum ServicesGatewayUriId
{
    GetOrganizationDetails,
    GetOrganizations,
    GetProjectsForOrganization,
    CreateProject,
    GetUserForProject
}

enum GenesisUriId
{
    LegacyGetOrganizationDetails
}
