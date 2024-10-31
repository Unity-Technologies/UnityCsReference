// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements.StyleSheets
{
    enum URIValidationResult
    {
        OK,
        InvalidURILocation,
        InvalidURIScheme,
        InvalidURIProjectAssetPath
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    static class URIHelpers
    {
        private const string k_ProjectScheme = "project";
        private const string k_ProjectSchemeEmptyHint = k_ProjectScheme + "://?";
        private const string k_AssetDatabaseHost = "database";
        static readonly Uri s_ProjectRootUri = new UriBuilder(k_ProjectScheme, "").Uri;
        static readonly Uri s_ThemeUri = new UriBuilder(ThemeRegistry.kThemeScheme, "").Uri;

        private const string k_AttrFileId = nameof(RawPPtrReference.fileID);
        private const string k_AttrGuid = nameof(RawPPtrReference.guid);
        private const string k_AttrType = nameof(RawPPtrReference.type);
        private const string k_ContainerRefName = nameof(RawPPtrContainer.o);

        public static string MakeAssetUri(Object asset)
        {
            return MakeAssetUri(asset, false);
        }

        public static string MakeAssetUri(Object asset, bool compact)
        {
            if (!asset)
                return null;

            // TODO: refactor to avoid the unnecessary JSON serialization
            var container = s_PPtrContainer;
            container.o = asset;
            var jsonContainer = EditorJsonUtility.ToJson(container);
            var rawContainer = s_RawPPtrContainer;
            EditorJsonUtility.FromJsonOverwrite(jsonContainer, rawContainer);

            var path = AssetDatabase.GetAssetPath(asset);

            var fileID = rawContainer.o.fileID;
            var guid = rawContainer.o.guid;
            var type = rawContainer.o.type;

            var query = $"{k_AttrFileId}={fileID}&{k_AttrGuid}={guid}&{k_AttrType}={type}";

            if (compact)
            {
                return $"?{query}";
            }

            var fragment = asset.name;

            // note: UriBuilder makes assumptions based on the input scheme
            // using a custom scheme with an empty host produces unexpected results
            var u = new UriBuilder(k_ProjectScheme, k_AssetDatabaseHost, -1, path);
            u.Query = query;
            u.Fragment = fragment;

            return u.ToString();
        }

        public static string EncodeUri(string uri)
        {
            uri = uri.Replace("&", "&amp;"); // Has to be done first!
            uri = uri.Replace("\"", "&quot;");
            uri = uri.Replace("\'", "&apos;");
            uri = uri.Replace("<", "&lt;");
            uri = uri.Replace(">", "&gt;");
            uri = uri.Replace("\n", "&#10;");
            uri = uri.Replace("\r", "");
            uri = uri.Replace("\t", "&#x9;");

            return uri;
        }

        public struct URIValidationResponse
        {
            public URIValidationResult result;
            public string errorToken;
            public string warningMessage;
            public string resolvedProjectRelativePath;
            public string resolvedSubAssetPath;
            public Object resolvedQueryAsset;

            // Indicates the url contains some parts that can be updated, such as when a file has been moved/renamed.
            public bool resolvedUrlChanged;

            public bool hasWarningMessage => !string.IsNullOrEmpty(warningMessage);

            public bool isLibraryAsset =>
                resolvedProjectRelativePath?.StartsWith("Library/", StringComparison.Ordinal) ?? false;
        }

        public static URIValidationResult ValidAssetURL(string assetPath, string path, out string errorToken, out string resolvedProjectRelativePath)
        {
            var response = ValidateAssetURL(assetPath, path);
            errorToken = response.errorToken;
            resolvedProjectRelativePath = response.resolvedProjectRelativePath;
            return response.result;
        }

        public static URIValidationResult ValidAssetURL(string assetPath, string path, out string errorToken,
            out string resolvedProjectRelativePath, out string resolvedSubAssetPath)
        {
            var response = ValidateAssetURL(assetPath, path);
            errorToken = response.errorToken;
            resolvedProjectRelativePath = response.resolvedProjectRelativePath;
            resolvedSubAssetPath = response.resolvedSubAssetPath;
            return response.result;
        }

        public static URIValidationResponse ValidateAssetURL(string assetPath, string path)
        {
            var response = new URIValidationResponse();

            if (string.IsNullOrEmpty(path))
            {
                response.errorToken = "''";
                response.result = URIValidationResult.InvalidURILocation;
                return response;
            }

            // UriBuilder isn't able to process '#' fragments with our URL scheme,
            // so we process them manually here instead.
            var originalPath = path;
            response.resolvedSubAssetPath = ExtractUrlFragment(ref path);

            Uri absoluteUri = null;
            bool isUnityThemePath = path.StartsWith($"{ThemeRegistry.kThemeScheme}://");
            // Always treat URIs starting with "/" as implicit project schemes
            if (path.StartsWith("/"))
            {
                var builder = new UriBuilder(s_ProjectRootUri.Scheme, "", 0, path);
                absoluteUri = builder.Uri;
            }
            else if (isUnityThemePath)
            {
                var themeName = path.Substring($"{s_ThemeUri.Scheme}://".Length);
                var builder = new UriBuilder(s_ThemeUri.Scheme, "", -1, themeName);
                absoluteUri = builder.Uri;
            }
            else if (Uri.TryCreate(path, UriKind.Absolute, out absoluteUri) == false)
            {
                // Resolve a relative URI compared to current file
                Uri assetPathUri = new Uri(s_ProjectRootUri, assetPath);

                if (Uri.TryCreate(assetPathUri, path, out absoluteUri) == false)
                {
                    response.errorToken = assetPath;
                    response.result = URIValidationResult.InvalidURILocation;
                    return response;
                }
            }
            else if (absoluteUri.Scheme != s_ProjectRootUri.Scheme)
            {
                response.errorToken = absoluteUri.Scheme;
                response.result = URIValidationResult.InvalidURIScheme;
                return response;
            }

            string projectRelativePath = Uri.UnescapeDataString(absoluteUri.AbsolutePath);

            // Remove any leading "/" as this now used as a path relative to the current directory
            if (projectRelativePath.StartsWith("/"))
            {
                projectRelativePath = projectRelativePath.Substring(1);
            }

            if (isUnityThemePath)
            {
                if (string.IsNullOrEmpty(path))
                {
                    response.errorToken = string.Empty;
                    response.result = URIValidationResult.InvalidURIProjectAssetPath;
                    return response;
                }
                response.resolvedProjectRelativePath = path;
            }
            else
            {
                response.resolvedProjectRelativePath = projectRelativePath;

                // support for: project://asset.type/Assets/Path/To/File.ext?fileID=FILEID&guid=GUID&type=TYPE#subAssetName
                // the idea here is to keep UXML/USS human-readable, but also support location-independent asset references

                var query = ExtractUriQueryParameters(absoluteUri);

                // note: we could relax this and support queries with only a guid parameter, resolving the main asset
                if (query.hasAllReferenceParams)
                {
                    // TODO: refactor to avoid the unnecessary JSON serialization
                    var json = $"{{\"{k_ContainerRefName}\":{{\"{k_AttrFileId}\": {query.fileId}, \"{k_AttrGuid}\":\"{query.guid}\", \"{k_AttrType}\": {query.type}}}}}";
                    var container = s_PPtrContainer;
                    EditorJsonUtility.FromJsonOverwrite(json, container);
                    var resolvedAssetReference = container.o;
                    response.resolvedQueryAsset = resolvedAssetReference;

                    // empty paths are resolved to the input assetPath, which is not an issue here
                    // since the GUID parameter takes precedence
                    // this case happens with compact asset URIs, and we don't want to warn users
                    var hasEmptyPathHint = originalPath.StartsWith("?", StringComparison.Ordinal) ||
                        originalPath.StartsWith(k_ProjectSchemeEmptyHint, StringComparison.Ordinal);

                    if (!resolvedAssetReference)
                    {
                        // could not resolve asset reference from query

                        var pathFromGuid = AssetDatabase.GUIDToAssetPath(query.guid);

                        if (!string.IsNullOrEmpty(pathFromGuid))
                        {
                            // but that's just because the ADB can't return the asset at the moment
                            // e.g. during GatherDependenciesFromSourceFile
                            if (pathFromGuid != response.resolvedProjectRelativePath)
                            {
                                if (!hasEmptyPathHint)
                                {
                                    response.warningMessage = string.Format(
                                        L10n.Tr(
                                            "Asset reference to GUID '{0}' resolved to '{2}', but URL path hints at '{1}'. Update the URL '{3}' to remove this warning."),
                                        query.guid, response.resolvedProjectRelativePath, pathFromGuid, originalPath);
                                }
                                response.resolvedUrlChanged = true;
                                response.resolvedProjectRelativePath = pathFromGuid;
                            }
                        }
                        else if (AssetExistsAtPath(response.resolvedProjectRelativePath))
                        {
                            // but the path points to a valid asset, so let's use that
                            response.warningMessage = string.Format(
                                L10n.Tr(
                                    "Could not resolve asset with GUID '{0}' and file ID '{1}' from URL '{2}'. Using asset path '{3}' instead. Update the URL to remove this warning."),
                                query.guid, query.fileId, path, response.resolvedProjectRelativePath);
                            response.resolvedUrlChanged = true;
                        }
                        else
                        {
                            response.warningMessage = string.Format(L10n.Tr("Invalid asset path hint \"{0}\" for referenced asset GUID \"{1}\""),
                                response.resolvedProjectRelativePath, query.guid);
                            response.errorToken = originalPath;
                            response.result = URIValidationResult.InvalidURIProjectAssetPath;
                            return response;
                        }
                    }
                    else
                    {
                        // the GUID-based asset reference takes precedence over paths and names
                        // warn users about any inconsistencies

                        var realAssetPath = AssetDatabase.GetAssetPath(resolvedAssetReference);
                        if (!hasEmptyPathHint &&
                            !string.IsNullOrEmpty(response.resolvedProjectRelativePath) &&
                            realAssetPath != response.resolvedProjectRelativePath)
                        {
                            // URL path differs from real path (from GUID)
                            if (AssetExistsAtPath(response.resolvedProjectRelativePath))
                            {
                                // URL path points to some other asset -> warn the user
                                response.warningMessage = string.Format(
                                    L10n.Tr(
                                        "Ambiguous asset reference detected. Asset reference to GUID '{0}' resolved to '{2}', but URL path hints at '{1}', which is also valid asset path. Update the URL '{3}' to remove this warning."),
                                    query.guid, response.resolvedProjectRelativePath, realAssetPath, originalPath);
                            }
                            else
                            {
                                // URL path points to nothing -> warn the user
                                response.warningMessage = string.Format(
                                    L10n.Tr(
                                        "Asset reference to GUID '{0}' was moved from '{1}' to '{2}'. Update the URL '{3}' to remove this warning."),
                                    query.guid, response.resolvedProjectRelativePath, realAssetPath, originalPath);
                                response.resolvedUrlChanged = true;
                            }
                        }

                        response.resolvedProjectRelativePath = realAssetPath;

                        var realAssetName = resolvedAssetReference.name;
                        if (!string.IsNullOrEmpty(response.resolvedSubAssetPath) &&
                            realAssetName != response.resolvedSubAssetPath)
                        {
                            response.warningMessage = string.Format(
                                L10n.Tr(
                                    "Asset reference to GUID '{0}' and file ID '{1}' was renamed from '{2}' to '{3}'. Update the URL '{4}' to remove this warning."),
                                query.guid, query.fileId, response.resolvedSubAssetPath, realAssetName, originalPath);
                        }
                        response.resolvedSubAssetPath = realAssetName;
                    }
                }
                else
                {
                    if (!AssetExistsAtPath(response.resolvedProjectRelativePath))
                    {
                        response.errorToken = string.IsNullOrEmpty(response.resolvedProjectRelativePath) ?
                            originalPath : response.resolvedProjectRelativePath;

                        response.result = URIValidationResult.InvalidURIProjectAssetPath;
                        return response;
                    }
                }
            }

            response.result = URIValidationResult.OK;
            return response;
        }

        private static bool AssetExistsAtPath(string path)
        {
            var guidFromPath = AssetDatabase.GUIDFromAssetPath(path);

            // also testing for file existence to cover scenarios where the ADB did not import the asset yet
            return !guidFromPath.Empty() || File.Exists(path);
        }

        // the following types exist only to (de)serialize UnityEngine.Object references (PPtr) from/to JSON

#pragma warning disable 649
        // serialize a PPtr to json: {"o":{"fileID":1234,"guid":"abcd1234","type":3}}
        private class PPtrContainer
        {
            // keep name in sync with RawPPtrContainer.o
            public UnityEngine.Object o;
        }

        // raw representation of the PPtrContainer serialization result
        // keep member names and types synchronized with PPtr serialization
        [Serializable]
        private struct RawPPtrReference
        {
            public long fileID;
            public string guid;
            public int type;
        }

        private class RawPPtrContainer
        {
            // keep name in sync with PPtrContainer.o
            public RawPPtrReference o;
        }
#pragma warning restore 649

        // pre-allocate these containers to avoid repeated managed allocations
        // JSON serialization only works on classes
        private static readonly PPtrContainer s_PPtrContainer = new PPtrContainer();
        private static readonly RawPPtrContainer s_RawPPtrContainer = new RawPPtrContainer();
        private static readonly char[] s_Separator = {'&'};

        private struct UriQueryParameters
        {
            public string query;
            public string guid;
            public long fileId;
            public int type;

            public bool hasQuery, hasValidQuery, hasExtraQueryParams;
            public bool hasGuid, hasFileId, hasType;

            public bool hasAllReferenceParams => hasValidQuery && hasGuid && hasFileId && hasType;
        }

        private static UriQueryParameters ExtractUriQueryParameters(Uri uri)
        {
            var result = new UriQueryParameters();

            var query = uri.Query;
            if (query == null || query.Length <= 1)
                return result;

            query = query.Substring(1); // skip '?'

            result.query = query;
            result.hasQuery = true;
            result.hasValidQuery = true;

            // no access to System.Web.dll here
            var queryParameters = query.Split(s_Separator);

            // all 3 parameters must be found in the query string for the asset reference to be valid
            foreach (var queryParameter in queryParameters)
            {
                var splitIndex = queryParameter.IndexOf('=');
                if (splitIndex > 0 && splitIndex < queryParameter.Length - 1)
                {
                    var key = queryParameter.Substring(0, splitIndex);
                    var value = queryParameter.Substring(splitIndex + 1);

                    switch (key)
                    {
                        case k_AttrGuid:
                            result.guid = value;
                            result.hasGuid = !string.IsNullOrEmpty(result.guid);
                            break;
                        case k_AttrFileId:
                            result.hasFileId = long.TryParse(value, out result.fileId);
                            break;
                        case k_AttrType:
                            result.hasType = int.TryParse(value, out result.type);
                            break;
                        default:
                            result.hasExtraQueryParams = true;
                            break;
                    }
                }
                else
                {
                    result.hasValidQuery = false;
                    return result;
                }
            }

            return result;
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
