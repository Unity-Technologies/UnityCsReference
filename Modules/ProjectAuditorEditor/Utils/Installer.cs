// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class Installer
    {
        // Major.Minor.Micro followed by one of abxfp followed by an identifier, optionally suffixed with " (revisionhash)"
        static readonly Regex s_VersionPattern = new Regex(@"(?<shortVersion>\d+\.\d+\.\d+(?<suffix>((?<alphabeta>[abx])|[fp])[^\s]*))( \((?<revision>[a-fA-F\d]+)\))?",
            RegexOptions.Compiled);

        public static string GetUnityHubModuleDownloadURL(string moduleName)
        {
            var fullVersion = InternalEditorUtility.GetFullUnityVersion();
            var revision = "";
            var shortVersion = "";
            var versionMatch = s_VersionPattern.Match(fullVersion);
            if (!versionMatch.Success || !versionMatch.Groups["shortVersion"].Success || !versionMatch.Groups["suffix"].Success)
                Debug.LogWarningFormat("Error parsing version '{0}'", fullVersion);

            if (versionMatch.Groups["shortVersion"].Success)
                shortVersion = versionMatch.Groups["shortVersion"].Value;
            if (versionMatch.Groups["revision"].Success)
                revision = versionMatch.Groups["revision"].Value;

            return string.Format("unityhub://{0}/{1}/module={2}", shortVersion, revision, moduleName.ToLower());
        }
    }
}
