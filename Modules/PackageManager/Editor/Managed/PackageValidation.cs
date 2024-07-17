// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;

namespace UnityEditor.PackageManager
{
    internal static class PackageValidation
    {
        private static readonly Regex s_CompleteNameRegEx = new Regex(@"^([a-z\d][a-z\d-._]{0,213})$");
        private static readonly Regex s_NameRegEx = new Regex(@"^([a-z\d][a-z\d\-\._]{0,112})$");
        private static readonly Regex s_OrganizationNameRegEx = new Regex(@"^([a-z\d][a-z\d\-_]{0,99})$");
        private static readonly Regex s_AllowedSemverRegEx = new Regex(@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$");
        private static readonly Regex s_UnityMajorVersionRegEx = new Regex(@"^([1-9][0-9]{3})$");
        private static readonly Regex s_UnityMinorVersionRegEx = new Regex(@"^([0-9])$");
        private static readonly Regex s_UnityReleaseVersionRegEx = new Regex(@"^(0|[1-9]\d*)([abfp])(0|[1-9]\d*)$");

        public static bool ValidateCompleteName(string completeName)
        {
            return !string.IsNullOrEmpty(completeName) && s_CompleteNameRegEx.IsMatch(completeName);
        }

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
            return ValidateVersion(version, out _, out _, out _);
        }

        public static bool ValidateVersion(string version, out string major, out string minor, out string patch)
        {
            major = string.Empty;
            minor = string.Empty;
            patch = string.Empty;
            var match = s_AllowedSemverRegEx.Match(version);
            if (!match.Success)
                return false;

            major = match.Groups["major"].Value;
            minor = match.Groups["minor"].Value;
            patch = match.Groups["patch"].Value;
            return true;
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
