// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using System.Text.RegularExpressions;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class PackageValidator
    {
        private const string k_AllowedCharsInTechnicalName = @"a-z\d\-\._";
        private static readonly Regex k_CompleteTechnicalNameRegEx = new Regex(@"^([a-z\d][" + k_AllowedCharsInTechnicalName + "]{0,213})$");
        private static readonly Regex k_PartialTechnicalNameRegEx = new Regex(@"^([" + k_AllowedCharsInTechnicalName + "]{1,113})$");
        private static readonly Regex k_OrganizationNameRegEx = new Regex(@"^([a-z\d][a-z\d\-_]{0,99})$");
        private static readonly Regex k_AllowedSemverRegEx = new Regex(@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$");
        private static readonly Regex k_UnityMajorVersionRegEx = new Regex(@"^([1-9][0-9]{3})$");
        private static readonly Regex k_UnityMinorVersionRegEx = new Regex(@"^([0-9])$");
        private static readonly Regex k_UnityReleaseVersionRegEx = new Regex(@"^(0|[1-9]\d*)([abfp])(0|[1-9]\d*)$");

        public static string SanitizeDisplayName(string value)
        {
            // Removing invalid characters because Windows does not allow them in folder names
            return Regex.Replace(value, @"[\\/:*?""<>|]", "").Trim();
        }

        public static string SanitizePackageTechnicalName(string value)
        {
            return Regex.Replace((value ?? string.Empty).ToLower(CultureInfo.InvariantCulture), $"[^{k_AllowedCharsInTechnicalName}]", "");
        }

        public static string SanitizeNamespace(string value)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Regex.Replace(Regex.Replace(value ?? string.Empty, @"[^a-zA-Z\d]", ""), @"^\d+", ""));
        }

        public static bool ValidateCompleteTechnicalName(string completeName)
        {
            return !string.IsNullOrEmpty(completeName) && k_CompleteTechnicalNameRegEx.IsMatch(completeName);
        }

        public static bool ValidatePartialTechnicalName(string name)
        {
            return !string.IsNullOrEmpty(name) && k_PartialTechnicalNameRegEx.IsMatch(name);
        }

        public static bool ValidateOrganizationName(string organizationName)
        {
            return !string.IsNullOrEmpty(organizationName) && k_OrganizationNameRegEx.IsMatch(organizationName);
        }

        public static bool ValidateVersion(string version, out string major, out string minor, out string patch)
        {
            major = string.Empty;
            minor = string.Empty;
            patch = string.Empty;
            var match = k_AllowedSemverRegEx.Match(version);
            if (!match.Success)
                return false;

            major = match.Groups["major"].Value;
            minor = match.Groups["minor"].Value;
            patch = match.Groups["patch"].Value;
            return true;
        }

        public static bool ValidateUnityVersion(string majorVersion, string minorVersion, string releaseVersion = null)
        {
            return !string.IsNullOrEmpty(majorVersion) && k_UnityMajorVersionRegEx.IsMatch(majorVersion) &&
                !string.IsNullOrEmpty(minorVersion) && k_UnityMinorVersionRegEx.IsMatch(minorVersion) &&
                (string.IsNullOrEmpty(releaseVersion) || k_UnityReleaseVersionRegEx.IsMatch(releaseVersion));
        }
    }
}
