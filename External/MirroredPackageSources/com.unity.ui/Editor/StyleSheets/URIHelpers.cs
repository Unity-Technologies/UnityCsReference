using System;
using System.IO;

namespace UnityEditor.UIElements.StyleSheets
{
    enum URIValidationResult
    {
        OK,
        InvalidURILocation,
        InvalidURIScheme,
        InvalidURIProjectAssetPath
    }

    static class URIHelpers
    {
        static readonly Uri s_ProjectRootUri = new UriBuilder("project", "").Uri;

        public static URIValidationResult ValidAssetURL(string assetPath, string path, out string errorMessage, out string resolvedProjectRelativePath)
        {
            return ValidAssetURL(assetPath, path, out errorMessage, out resolvedProjectRelativePath, out _);
        }

        public static URIValidationResult ValidAssetURL(string assetPath, string path, out string errorMessage, out string resolvedProjectRelativePath, out string resolvedSubAssetPath)
        {
            resolvedProjectRelativePath = null;
            resolvedSubAssetPath = null;

            if (string.IsNullOrEmpty(path))
            {
                errorMessage = "Empty URI";
                return URIValidationResult.InvalidURILocation;
            }

            // UriBuilder isn't able to process '#' fragments with our URL scheme,
            // so we process them manually here instead.
            resolvedSubAssetPath = ExtractUrlFragment(ref path);

            Uri absoluteUri = null;
            // Always treat URIs starting with "/" as implicit project schemes
            if (path.StartsWith("/"))
            {
                var builder = new UriBuilder(s_ProjectRootUri.Scheme, "", 0, path);
                absoluteUri = builder.Uri;
            }
            else if (Uri.TryCreate(path, UriKind.Absolute, out absoluteUri) == false)
            {
                // Resolve a relative URI compared to current file
                Uri assetPathUri = new Uri(s_ProjectRootUri, assetPath);

                if (Uri.TryCreate(assetPathUri, path, out absoluteUri) == false)
                {
                    errorMessage = assetPath;
                    return URIValidationResult.InvalidURILocation;
                }
            }
            else if (absoluteUri.Scheme != s_ProjectRootUri.Scheme)
            {
                errorMessage = absoluteUri.Scheme;
                return URIValidationResult.InvalidURIScheme;
            }

            string projectRelativePath = absoluteUri.LocalPath;

            // Remove any leading "/" as this now used as a path relative to the current directory
            if (projectRelativePath.StartsWith("/"))
            {
                projectRelativePath = projectRelativePath.Substring(1);
            }

            if (string.IsNullOrEmpty(projectRelativePath) || !File.Exists(projectRelativePath))
            {
                errorMessage = projectRelativePath;
                return URIValidationResult.InvalidURIProjectAssetPath;
            }

            resolvedProjectRelativePath = projectRelativePath;
            errorMessage = null;

            return URIValidationResult.OK;
        }

        private static string ExtractUrlFragment(ref string path)
        {
            int fragmentLocation = path.LastIndexOf('#');
            if (fragmentLocation == -1)
                return string.Empty;

            var fragment = Uri.UnescapeDataString(path.Substring(fragmentLocation + 1));
            path = path.Substring(0, fragmentLocation);

            return fragment;
        }

        public static string InjectFileNameSuffix(string path, string suffix)
        {
            string ext = Path.GetExtension(path);
            string fileRenamed = Path.GetFileNameWithoutExtension(path) + suffix + ext;
            return Path.Combine(Path.GetDirectoryName(path), fileRenamed);
        }
    }
}
