// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Connect;

namespace UnityEditor
{
    public class CloudProjectSettings
    {
        /// <summary>
        /// The user ID is derived from the user name without the domain (removing all characters starting with '@'),
        /// formatted in lowercase with no symbols.
        /// </summary>
        public static string userId
        {
            get
            {
                return UnityConnect.instance.GetUserId();
            }
        }

        /// <summary>
        /// The user name is the email used for the user's Unity account.
        /// </summary>
        public static string userName
        {
            get
            {
                return UnityConnect.instance.GetUserName();
            }
        }

        public static string accessToken
        {
            get
            {
                return UnityConnect.instance.GetAccessToken();
            }
        }

        public static void RefreshAccessToken(Action<bool> refresh)
        {
            UnityConnect.instance.RefreshAccessToken(refresh);
        }

        public static Task<string> GetServiceTokenAsync(CancellationToken cancellationToken = default)
            => ServiceToken.Instance.GetServiceTokenAsync(accessToken, cancellationToken);

        /// <summary>
        /// This method shows the Unity login popup.
        /// </summary>
        public static void ShowLogin()
        {
            UnityConnect.instance.ShowLogin();
        }

        /// <summary>
        /// The Project ID, or GUID.
        /// </summary>
        public static string projectId
        {
            get
            {
                return UnityConnect.instance.GetProjectGUID();
            }
        }

        /// <summary>
        /// The name of the project.
        /// </summary>
        public static string projectName
        {
            get
            {
                return UnityConnect.instance.GetProjectName();
            }
        }

        /// <summary>
        /// The Organization ID, formatted in lowercase with no symbols.
        /// </summary>
        public static string organizationId
        {
            get
            {
                return UnityConnect.instance.GetOrganizationId();
            }
        }

        /// <summary>
        /// The Organization name used on the dashboard.
        /// </summary>
        public static string organizationName
        {
            get
            {
                return UnityConnect.instance.GetOrganizationName();
            }
        }

        /// <summary>
        /// The key of the organization used on the dashboard
        /// </summary>
        public static string organizationKey
        {
            get
            {
                return UnityConnect.instance.GetOrganizationForeignKey();
            }
        }

        /// <summary>
        /// The current COPPA compliance state.
        /// </summary>
        public static CoppaCompliance coppaCompliance
        {
            get
            {
                return UnityConnect.instance.projectInfo.COPPA;
            }
        }

        /// <summary>
        /// Returns true if the project has been bound.
        /// </summary>
        public static bool projectBound
        {
            get
            {
                return UnityConnect.instance.projectInfo.projectBound;
            }
        }
    }
}
