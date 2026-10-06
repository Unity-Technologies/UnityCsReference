// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Networking
{
    internal static class WebRequestProfilerUrl
    {
        internal static string RawQueryString(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            var fragment = url.IndexOf('#');
            if (fragment >= 0)
                url = url.Substring(0, fragment);

            var query = url.IndexOf('?');
            return query < 0 ? string.Empty : url.Substring(query + 1);
        }

        internal static List<KeyValuePair<string, string>> ParseQueryParameters(string url)
        {
            var parameters = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrEmpty(url))
                return parameters;

            var fragment = url.IndexOf('#');
            if (fragment >= 0)
                url = url.Substring(0, fragment);

            var query = url.IndexOf('?');
            if (query < 0)
                return parameters;

            foreach (var pair in url.Substring(query + 1).Split('&'))
            {
                if (pair.Length == 0)
                    continue;

                var equals = pair.IndexOf('=');
                var name = equals >= 0 ? pair.Substring(0, equals) : pair;
                var value = equals >= 0 ? pair.Substring(equals + 1) : string.Empty;

                // Decoded, which is what the parameter means; General above has the raw url. Invalid
                // escaping is shown as it stands rather than throwing into the view.
                parameters.Add(new KeyValuePair<string, string>(Unescape(name), Unescape(value)));
            }

            return parameters;
        }

        static string Unescape(string text)
        {
            try
            {
                return Uri.UnescapeDataString(text);
            }
            catch (UriFormatException)
            {
                return text;
            }
        }
    }
}
