// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Connect
{
    class OrganizationRequestResponse
    {
        public string LegacyId { get; internal set; }
        public string Id { get; }
        public string GenesisId { get; }
        public string Name { get; }
        public string Role { get; }

        public OrganizationRequestResponse(
            string legacyId,
            string id,
            string genesisId,
            string name,
            string role = "")
        {
            LegacyId = legacyId;
            Id = id;
            GenesisId = genesisId;
            Name = name;
            Role = role;
        }
    }
}
