// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class PackageInfoExtension
    {
        public const string k_BuiltinPackageDocsUrlKey = "Scripting API: ";

        public static string GetShortVersionId(this PackageInfo packageInfo)
        {
            if (string.IsNullOrEmpty(packageInfo?.version))
                return string.Empty;
            SemVersionParser.TryParse(packageInfo.version, out var semVer);
            return semVer == null ? string.Empty : UpmPackageVersion.FormatPackageId(packageInfo.name, semVer.Value.ShortVersion());
        }

        public static bool ExtractBuiltinDescription(this PackageInfo packageInfo, out string result)
        {
            if (!packageInfo.IsBuiltIn())
            {
                result = string.Empty;
                return false;
            }

            result = string.IsNullOrEmpty(packageInfo?.description) ?
                     string.Format(L10n.Tr("This built in package controls the presence of the {0} module."), packageInfo.displayName) :
                     packageInfo.description.Split(new[] { k_BuiltinPackageDocsUrlKey }, StringSplitOptions.None)[0];

            return true;
        }

        // For built-in modules, the documentation url is embedded in the description.
        // We parse that to have the "View Documentation" button direct to it, instead of showing the link in the description text.
        // For example, the description for AI module looks like this :
        // "The AI module implements the path finding features in Unity. Scripting API: https://docs.unity3d.com/ScriptReference/UnityEngine.AIModule.html"
        public static bool ExtractUrlFromDescription(this PackageInfo packageInfo, string docUrlWithShortUnityVersion, out string result)
        {
            if (!packageInfo.IsBuiltIn() || string.IsNullOrEmpty(packageInfo.description))
            {
                result = string.Empty;
                return false;
            }

            var descriptionSplit = packageInfo.description.Split(new[] { k_BuiltinPackageDocsUrlKey }, StringSplitOptions.None);
            if (descriptionSplit.Length < 2)
            {
                result = string.Empty;
                return false;
            }

            var urlWithoutShortUnityVersion = descriptionSplit[1];
            var urlSplit = urlWithoutShortUnityVersion.Split(new[] { ApplicationProxy.k_UnityDocsUrl }, StringSplitOptions.None);

            if (urlSplit.Length > 1)
                result = $"{docUrlWithShortUnityVersion}Documentation/" + urlSplit[1];
            else
                result = urlWithoutShortUnityVersion;

            return true;
        }

        private static bool IsBuiltIn(this PackageInfo packageInfo)
        {
            return packageInfo.source == PackageSource.BuiltIn && packageInfo.type == "module";
        }
    }
}
