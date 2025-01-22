// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;

namespace UnityEditor.Connect
{
    class UserRequestResponse
    {
        const string k_UnknownRoleMessage = "Unknown role: {0}";
        const string k_JsonRoleNodeName = "roles";
        const string k_RoleOrganization = "organization";

        // Keep in order of privilege
        public enum UserRole
        {
            Guest = 0,
            User = 1,
            Manager = 2,
            Owner = 3,
        }

        public UserRole OrganizationRole { get; private set; } = UserRole.Guest;

        public UserRequestResponse(string jsonResponse)
        {
            var jsonParser = new JSONParser(jsonResponse);

            var json = jsonParser.Parse().AsDict();
            var roles = json[k_JsonRoleNodeName].AsList();

            foreach (var role in roles)
            {
                var roleInternal = new UserRoleInternal(role);
                if (roleInternal.EntityType == k_RoleOrganization
                    && roleInternal.IsLegacy)
                {
                    if (roleInternal.CompareName(UserRole.Manager)
                        && UserRole.Manager > OrganizationRole)
                    {
                        OrganizationRole = UserRole.Manager;
                    }
                    else if (roleInternal.CompareName(UserRole.Owner)
                        && UserRole.Owner > OrganizationRole)
                    {
                        OrganizationRole = UserRole.Owner;
                    }
                    else if (roleInternal.CompareName(UserRole.User)
                             && UserRole.User > OrganizationRole)
                    {
                        OrganizationRole = UserRole.User;
                    }
                }
            }
        }
    }

    class UserRoleInternal
    {
        const string k_JsonEntityType = "entityType";
        const string k_JsonName = "name";
        const string k_IsLegacyKey = "isLegacy";

        public string EntityType { get; private set; }
        public string Name { get; private set; }
        public bool IsLegacy { get; private set; }

        public UserRoleInternal(JSONValue jsonValue)
        {
            var dict = jsonValue.AsDict();
            EntityType = dict[k_JsonEntityType].AsString();
            Name = dict[k_JsonName].AsString();
            IsLegacy = dict[k_IsLegacyKey].AsBool();
        }

        public bool CompareName(UserRequestResponse.UserRole userRole)
        {
            return Name.Equals(userRole.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}

