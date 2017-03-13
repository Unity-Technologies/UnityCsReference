// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using System.Text.RegularExpressions;

namespace UnityEngineInternal
{
    static class WebRequestUtils
    {
        private static Regex domainRegex = new Regex("^\\s*\\w+(?:\\.\\w+)+\\s*$");

        [RequiredByNativeCode]
        internal static string RedirectTo(string baseUri, string redirectUri)
        {
            Uri redirectURI;
            // On UNIX systems URI starting with / is misidentified as absolute path and is considered absolute
            // while it is actually a relative URI. Enforce that.
            if (redirectUri[0] == '/')
                redirectURI = new Uri(redirectUri, UriKind.Relative);
            else
                redirectURI = new Uri(redirectUri, UriKind.RelativeOrAbsolute);
            if (redirectURI.IsAbsoluteUri)
                return redirectUri;

            var baseURI = new Uri(baseUri, UriKind.Absolute);
            var finalUri = new Uri(baseURI, redirectURI);
            return finalUri.AbsoluteUri;
        }

        internal static string MakeInitialUrl(string targetUrl, string localUrl)
        {
            var localUri = new System.Uri(localUrl);

            if (targetUrl.StartsWith("//"))
            {
                // Prepend current protocol/scheme
                targetUrl = localUri.Scheme + ":" + targetUrl;
            }

            if (targetUrl.StartsWith("/"))
            {
                // Prepend scheme and host
                targetUrl = localUri.Scheme + "://" + localUri.Host + targetUrl;
            }

            if (domainRegex.IsMatch(targetUrl))
            {
                targetUrl = localUri.Scheme + "://" + targetUrl;
            }

            System.Uri targetUri = null;
            try
            {
                targetUri = new System.Uri(targetUrl);
            }
            catch (FormatException e1)
            {
                // Technically, this should be UriFormatException but MSDN says WSA/PCL doesn't support
                // UriFormatException, and recommends FormatException instead
                // See: https://msdn.microsoft.com/en-us/library/system.uriformatexception%28v=vs.110%29.aspx
                try
                {
                    targetUri = new System.Uri(localUri, targetUrl);
                }
                catch (FormatException)
                {
                    throw e1;
                }
            }

            // if URL contains '%', assume it is properly escaped, otherwise '%2f' gets unescaped as '/' (which may not be correct)
            // otherwise escape it, i.e. replaces spaces by '%20'
            return targetUrl.Contains("%") ? targetUri.OriginalString : targetUri.AbsoluteUri;
        }
    }
}
