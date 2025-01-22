// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Connect
{
    class ProjectRequestResponse
    {
        public string Id { get; }
        public string GenesisId { get; }
        public string Name { get; }
        public string Coppa { get; }
        public string OrganizationName { get; internal set; }
        public string OrganizationLegacyId { get; internal set; }
        public string OrganizationId { get; }
        public string OrganizationGenesisId { get; }

        public ProjectRequestResponse(
            string id,
            string genesisId,
            string name,
            string coppa,
            string organizationLegacyId,
            string organizationId,
            string organizationGenesisId)
        {
            Id = id;
            GenesisId = genesisId;
            Name = name;
            Coppa = coppa;
            OrganizationLegacyId = organizationLegacyId;
            OrganizationId = organizationId;
            OrganizationGenesisId = organizationGenesisId;
        }
    }
}

