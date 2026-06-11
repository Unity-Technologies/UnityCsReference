// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class PackageValidator
    {
        // The maximum value of a package name follows this documentation: https://docs.unity3d.com/Manual/cus-naming.html
        public const int k_MaxAllowedCharsInTechnicalName = 214;
        private const string k_AllowedCharsInTechnicalName = @"a-z\d\-\._";
        private static readonly Regex k_CompleteTechnicalNameRegEx = new Regex(@"^([a-z\d][" + k_AllowedCharsInTechnicalName + "]{0," + (k_MaxAllowedCharsInTechnicalName - 1) + "})$");
        private static readonly Regex k_AllowedSemverRegEx = new Regex(@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$");
        private static readonly Regex k_UnityMajorVersionRegEx = new Regex(@"^([1-9][0-9]{3})$");
        private static readonly Regex k_UnityMinorVersionRegEx = new Regex(@"^([0-9])$");
        private static readonly Regex k_UnityReleaseVersionRegEx = new Regex(@"^(0|[1-9]\d{0,14})([abfp])(0|[1-9]\d*)$");

        public static string SanitizePackageTechnicalName(string value)
        {
            return Regex.Replace((value ?? string.Empty).ToLower(CultureInfo.InvariantCulture), $"[^{k_AllowedCharsInTechnicalName}]", "");
        }

        public static string SanitizeNamespace(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var result = new StringBuilder();
            var capitalizeNext = true;
            foreach (var c in value)
            {
                var isAsciiLetter = c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
                var isAsciiDigit = c is >= '0' and <= '9';
                if (!isAsciiLetter && !isAsciiDigit)
                    capitalizeNext = true;
                else if (isAsciiLetter || result.Length > 0)
                {
                    result.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
                    capitalizeNext = false;
                }
            }
            return result.ToString();
        }

        public static bool ValidateCompleteTechnicalName(string completeName)
        {
            return !string.IsNullOrEmpty(completeName) && k_CompleteTechnicalNameRegEx.IsMatch(completeName);
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
