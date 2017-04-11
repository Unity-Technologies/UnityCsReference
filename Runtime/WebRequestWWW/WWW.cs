// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityEngine
{
    partial class WWW
    {
        static readonly char[] forbiddenCharacters =
        {
            (char)0,  (char)1,  (char)2,  (char)3,  (char)4, (char)5,
            (char)6,  (char)7,  (char)8,  (char)9,  (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15,
            (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25,
            (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31, (char)127
        };

        static readonly char[] forbiddenCharactersForNames =
        {
            (char)32
        };

        // From http://www.w3.org/TR/XMLHttpRequest/#the-setrequestheader()-method
        static readonly string[] forbiddenHeaderKeys =
        {
            "Accept-Charset",
            "Accept-Encoding",
            "Access-Control-Request-Headers",
            "Access-Control-Request-Method",
            "Connection",
            "Content-Length",
            "Cookie",
            "Cookie2",
            "Date",
            "DNT",
            "Expect",
            "Host",
            "Keep-Alive",
            "Origin",
            "Referer",
            "TE",
            "Trailer",
            "Transfer-Encoding",
            "Upgrade",
            "User-Agent",
            "Via",
            "X-Unity-Version",
        };

        // Ensure headers do not contain invalid characters (ASCII 0-31, 127)
        // This prevents XSS and header-spoofing vulnerabilities.
        private static void CheckSecurityOnHeaders(string[] headers)
        {
            // On non-webplayer platforms, we permit the platform itself to define the restrictions.
            for (int i = 0; i < headers.Length; i += 2)
            {
                // headers[i] is the header name
                // headers[i+1] is the header body

                foreach (string forbiddenHeaderKey in forbiddenHeaderKeys)
                {
                    if (string.Equals(headers[i], forbiddenHeaderKey, StringComparison.CurrentCultureIgnoreCase))
                    {
                        throw new ArgumentException("Cannot overwrite header: " + headers[i]);
                    }
                }

                if (headers[i].StartsWith("Sec-") || headers[i].StartsWith("Proxy-"))
                {
                    throw new ArgumentException("Cannot overwrite header: " + headers[i]);
                }

                if (headers[i].IndexOfAny(forbiddenCharacters) > -1 ||
                    headers[i].IndexOfAny(forbiddenCharactersForNames) > -1 ||
                    headers[i + 1].IndexOfAny(forbiddenCharacters) > -1)
                {
                    throw new ArgumentException("Cannot include control characters " +
                        "in a HTTP header, either as key or value.");
                }
            }
        }


        public static string EscapeURL(string s)
        {
            return EscapeURL(s, System.Text.Encoding.UTF8);
        }

        public static string EscapeURL(string s, Encoding e)
        {
            if (s == null)
                return null;

            if (s == "")
                return "";

            if (e == null)
                return null;

            return WWWTranscoder.URLEncode(s, e);
        }

        public static string UnEscapeURL(string s)
        {
            return UnEscapeURL(s, System.Text.Encoding.UTF8);
        }

        public static string UnEscapeURL(string s, Encoding e)
        {
            if (null == s)
                return null;

            if (s.IndexOf('%') == -1 && s.IndexOf('+') == -1)
                return s;

            return WWWTranscoder.URLDecode(s, e);
        }

        public static WWW LoadFromCacheOrDownload(string url, int version)
        {
            return LoadFromCacheOrDownload(url, version, 0);
        }

        public static WWW LoadFromCacheOrDownload(string url, int version, uint crc)
        {
            Hash128 tempHash = new Hash128(0, 0, 0, (uint)version);
            return LoadFromCacheOrDownload(url, tempHash, crc);
        }

        public static WWW LoadFromCacheOrDownload(string url, Hash128 hash)
        {
            return LoadFromCacheOrDownload(url, hash, 0);
        }

        public static WWW LoadFromCacheOrDownload(string url, Hash128 hash, uint crc)
        {
            return new WWW(url, hash, crc);
        }

        private static string[] FlattenedHeadersFrom(Dictionary<string, string> headers)
        {
            if (headers == null) return null;

            var flattenedHeaders = new string[headers.Count * 2];
            var i = 0;
            foreach (KeyValuePair<string, string> entry in headers)
            {
                flattenedHeaders[i++] = entry.Key.ToString();
                flattenedHeaders[i++] = entry.Value.ToString();
            }

            return flattenedHeaders;
        }

        internal static Dictionary<string, string> ParseHTTPHeaderString(string input)
        {
            if (input == null) throw new ArgumentException("input was null to ParseHTTPHeaderString");
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var reader = new StringReader(input);

            int count = 0;
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null) break;

                // The first line in the response header is the HTTP status line, according to the
                // HTTP 1.1 specification (http://tools.ietf.org/html/rfc2616#section-6). Lets save it.
                if (count++ == 0 && line.StartsWith("HTTP"))
                {
                    result["STATUS"] = line;
                    continue;
                }

                var index = line.IndexOf(": ");
                if (index == -1) continue;
                var key = line.Substring(0, index).ToUpper();
                var value = line.Substring(index + 2);
                // According HTTP spec header with the same name can appear more than-once if and only if
                // it's entire value is comma separated values. Multiple such headers always can be combines into one. (case 791722)
                string currentValue;
                if (result.TryGetValue(key, out currentValue))
                    value = currentValue + "," + value;
                result[key] = value;
            }
            return result;
        }

        // Returns a [[AudioClip]] generated from the downloaded data (RO).
        [Obsolete("Obsolete msg (UnityUpgradable) -> * UnityEngine.WWWAudioExtensions.GetAudioClip(UnityEngine.WWW)", true)]
        public Object audioClip { get { return null; } }

        // Returns a [[MovieTexture]] generated from the downloaded data (RO).
        [Obsolete("Obsolete msg (UnityUpgradable) -> * UnityEngine.WWWAudioExtensions.GetMovieTexture(UnityEngine.WWW)", true)]
        public Object movie { get { return null; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Obsolete msg (UnityUpgradable) -> * UnityEngine.WWWAudioExtensions.GetAudioClip(UnityEngine.WWW)", true)]
        public Object oggVorbis { get { return null; } }
    }
}
