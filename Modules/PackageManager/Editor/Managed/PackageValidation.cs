// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;

namespace UnityEditor.PackageManager
{
    internal static class PackageValidation
    {
        private static readonly Regex s_NameRegEx = new Regex(@"^([a-z][a-z\d\-\._]{0,99})$");
        private static readonly Regex s_OrganizationNameRegEx = new Regex(@"^([a-z][a-z\d\-_]{0,99})$");
        private static readonly Regex s_AllowedSemverRegEx = new Regex(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(\-.+)?$");
        private static readonly Regex s_UnityMajorVersionRegEx = new Regex(@"^([1-9][0-9]{3})$");
        private static readonly Regex s_UnityMinorVersionRegEx = new Regex(@"^([1-9])$");
        private static readonly Regex s_UnityReleaseVersionRegEx = new Regex(@"^(0|[1-9]\d*)([abfp])(0|[1-9]\d*)$");

        public static bool ValidateName(string name)
        {
            return !string.IsNullOrEmpty(name) && s_NameRegEx.IsMatch(name);
        }

        public static bool ValidateOrganizationName(string organizationName)
        {
            return !string.IsNullOrEmpty(organizationName) && s_OrganizationNameRegEx.IsMatch(organizationName);
        }

        public static bool ValidateVersion(string version)
        {
            return !string.IsNullOrEmpty(version) && s_AllowedSemverRegEx.IsMatch(version);
        }

        public static bool ValidateUnityVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return false;

            var splitVersions = version.Split('.');
            switch (splitVersions.Length)
            {
                case 2:
                    return ValidateUnityVersion(splitVersions[0], splitVersions[1]);
                case 3:
                    return ValidateUnityVersion(splitVersions[0], splitVersions[1], splitVersions[2]);
                default:
                    return false;
            }
        }

        public static bool ValidateUnityVersion(string majorVersion, string minorVersion, string releaseVersion = null)
        {
            return !string.IsNullOrEmpty(majorVersion) && s_UnityMajorVersionRegEx.IsMatch(majorVersion) &&
                !string.IsNullOrEmpty(minorVersion) && s_UnityMinorVersionRegEx.IsMatch(minorVersion) &&
                (string.IsNullOrEmpty(releaseVersion) || s_UnityReleaseVersionRegEx.IsMatch(releaseVersion));
        }
    }
}
