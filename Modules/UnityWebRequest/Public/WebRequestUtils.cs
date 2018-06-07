// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Scripting;
using UnityEngine.Internal;


namespace UnityEngine
{
    // Helper class to generate form data to post to web servers using the [[WWW]] class.
    public class WWWForm
    {
        private List<byte[]> formData; // <byte[]>
        private List<string> fieldNames; // <string>
        private List<string> fileNames; // <string>
        private List<string> types; // <string>
        private byte[] boundary;
        private bool containsFiles = false;

        internal static System.Text.Encoding DefaultEncoding
        {
            get
            {
                return System.Text.Encoding.ASCII;
            }
        }

        // Creates an empty WWWForm object.
        public WWWForm()
        {
            formData = new List<byte[]>();
            fieldNames = new List<string>();
            fileNames = new List<string>();
            types = new List<string>();

            // Generate a random boundary
            boundary = new byte[40];
            for (int i = 0; i < 40; i++)
            {
                int randomChar = Random.Range(48, 110);
                if (randomChar > 57) // skip unprintable chars between 57 and 64 (inclusive)
                    randomChar += 7;
                if (randomChar > 90) // and 91 and 96 (inclusive)
                    randomChar += 6;
                boundary[i] = (byte)randomChar;
            }
        }

        // Add a simple field to the form.
        public void AddField(string fieldName, string value)
        {
            AddField(fieldName, value, Encoding.UTF8);
        }

        // Add a simple field to the form.
        public void AddField(string fieldName, string value, Encoding e)
        {
            fieldNames.Add(fieldName);
            fileNames.Add(null);
            formData.Add(e.GetBytes(value));
            types.Add("text/plain; charset=\"" + e.WebName + "\"");
        }

        // Adds a simple field to the form.
        public void AddField(string fieldName, int i)
        {
            AddField(fieldName, i.ToString());
        }

        // Add binary data to the form.
        [ExcludeFromDocs]
        public void AddBinaryData(string fieldName, byte[] contents)
        {
            AddBinaryData(fieldName, contents, null, null);
        }

        // Add binary data to the form.
        [ExcludeFromDocs]
        public void AddBinaryData(string fieldName, byte[] contents, string fileName)
        {
            AddBinaryData(fieldName, contents, fileName, null);
        }

        // Add binary data to the form.
        public void AddBinaryData(string fieldName, byte[] contents, [DefaultValue("null")] string fileName, [DefaultValue("null")] string mimeType)
        {
            containsFiles = true;

            // We handle png files automatically as we suspect people will be uploading png files a lot due to the new
            // screen shot feature. If we want to add support for detecting other file types, we will need to do it in a more extensible way.
            bool isPng = contents.Length > 8 && contents[0] == 0x89 && contents[1] == 0x50 && contents[2] == 0x4e &&
                contents[3] == 0x47
                && contents[4] == 0x0d && contents[5] == 0x0a && contents[6] == 0x1a && contents[7] == 0x0a;
            if (fileName == null)
            {
                fileName = fieldName + (isPng ? ".png" : ".dat");
            }
            if (mimeType == null)
            {
                if (isPng)
                    mimeType = "image/png";
                else
                    mimeType = "application/octet-stream";
            }

            fieldNames.Add(fieldName);
            fileNames.Add(fileName);
            formData.Add(contents);
            types.Add(mimeType);
        }

        // (RO) Returns the correct request headers for posting the form using the [[WWW]] class.
        public Dictionary<string, string> headers
        {
            get
            {
                Dictionary<string, string> retval = new Dictionary<string, string>();
                if (containsFiles)
                    retval["Content-Type"] = "multipart/form-data; boundary=\"" +
                        System.Text.Encoding.UTF8.GetString(boundary, 0, boundary.Length) + "\"";
                else
                    retval["Content-Type"] = "application/x-www-form-urlencoded";
                return retval;
            }
        }

        // (RO) The raw data to pass as the POST request body when sending the form.
        public byte[] data
        {
            get
            {
                if (containsFiles)
                {
                    byte[] dDash = DefaultEncoding.GetBytes("--");
                    byte[] crlf = DefaultEncoding.GetBytes("\r\n");
                    byte[] contentTypeHeader = DefaultEncoding.GetBytes("Content-Type: ");
                    byte[] dispositionHeader = DefaultEncoding.GetBytes("Content-disposition: form-data; name=\"");
                    byte[] endQuote = DefaultEncoding.GetBytes("\"");
                    byte[] fileNameField = DefaultEncoding.GetBytes("; filename=\"");

                    using (MemoryStream memStream = new MemoryStream(1024))
                    {
                        for (int i = 0; i < formData.Count; i++)
                        {
                            memStream.Write(crlf, 0, (int)crlf.Length);
                            memStream.Write(dDash, 0, (int)dDash.Length);
                            memStream.Write(boundary, 0, (int)boundary.Length);
                            memStream.Write(crlf, 0, (int)crlf.Length);
                            memStream.Write(contentTypeHeader, 0, (int)contentTypeHeader.Length);

                            byte[] type = System.Text.Encoding.UTF8.GetBytes((string)types[i]);
                            memStream.Write(type, 0, (int)type.Length);
                            memStream.Write(crlf, 0, (int)crlf.Length);
                            memStream.Write(dispositionHeader, 0, (int)dispositionHeader.Length);

                            string headerName = System.Text.Encoding.UTF8.HeaderName;
                            // Headers must be 7 bit clean, so encode as per rfc1522 using quoted-printable if needed.
                            string encodedFieldName = (string)fieldNames[i];
                            if (!WWWTranscoder.SevenBitClean(encodedFieldName, System.Text.Encoding.UTF8) ||
                                encodedFieldName.IndexOf("=?") > -1)
                            {
                                encodedFieldName = "=?" + headerName + "?Q?" +
                                    WWWTranscoder.QPEncode(encodedFieldName, System.Text.Encoding.UTF8) + "?=";
                            }
                            byte[] name = System.Text.Encoding.UTF8.GetBytes(encodedFieldName);
                            memStream.Write(name, 0, (int)name.Length);
                            memStream.Write(endQuote, 0, (int)endQuote.Length);

                            if (fileNames[i] != null)
                            {
                                // Headers must be 7 bit clean, so encode as per rfc1522 using quoted-printable if needed.
                                string encodedFileName = (string)fileNames[i];
                                if (!WWWTranscoder.SevenBitClean(encodedFileName, System.Text.Encoding.UTF8) ||
                                    encodedFileName.IndexOf("=?") > -1)
                                {
                                    encodedFileName = "=?" + headerName + "?Q?" +
                                        WWWTranscoder.QPEncode(encodedFileName, System.Text.Encoding.UTF8) + "?=";
                                }
                                byte[] fileName = System.Text.Encoding.UTF8.GetBytes(encodedFileName);

                                memStream.Write(fileNameField, 0, (int)fileNameField.Length);
                                memStream.Write(fileName, 0, (int)fileName.Length);
                                memStream.Write(endQuote, 0, (int)endQuote.Length);
                            }
                            memStream.Write(crlf, 0, (int)crlf.Length);
                            memStream.Write(crlf, 0, (int)crlf.Length);

                            byte[] formBytes = (byte[])formData[i];
                            memStream.Write(formBytes, 0, (int)formBytes.Length);
                        }
                        memStream.Write(crlf, 0, (int)crlf.Length);
                        memStream.Write(dDash, 0, (int)dDash.Length);
                        memStream.Write(boundary, 0, (int)boundary.Length);
                        memStream.Write(dDash, 0, (int)dDash.Length);
                        memStream.Write(crlf, 0, (int)crlf.Length);

                        return memStream.ToArray();
                    }
                }
                else
                {
                    byte[] ampersand = DefaultEncoding.GetBytes("&");
                    byte[] equal = DefaultEncoding.GetBytes("=");

                    using (MemoryStream memStream = new MemoryStream(1024))
                    {
                        for (int i = 0; i < formData.Count; i++)
                        {
                            byte[] name = WWWTranscoder.DataEncode(System.Text.Encoding.UTF8.GetBytes((string)fieldNames[i]));
                            byte[] formBytes = (byte[])formData[i];
                            byte[] value = WWWTranscoder.DataEncode(formBytes);

                            if (i > 0) memStream.Write(ampersand, 0, (int)ampersand.Length);
                            memStream.Write(name, 0, (int)name.Length);
                            memStream.Write(equal, 0, (int)equal.Length);
                            memStream.Write(value, 0, (int)value.Length);
                        }
                        return memStream.ToArray();
                    }
                }
            }
        }
    }


    internal class WWWTranscoder
    {
        private static byte[] ucHexChars = WWWForm.DefaultEncoding.GetBytes("0123456789ABCDEF");
        private static byte[] lcHexChars = WWWForm.DefaultEncoding.GetBytes("0123456789abcdef");
        private static byte urlEscapeChar = (byte)'%';
        private static byte[] urlSpace = new byte[] { (byte)'+' };
        private static byte[] dataSpace = WWWForm.DefaultEncoding.GetBytes("%20");
        private static byte[] urlForbidden = WWWForm.DefaultEncoding.GetBytes("@&;:<>=?\"'/\\!#%+$,{}|^[]`");
        private static byte qpEscapeChar = (byte)'=';
        private static byte[] qpSpace = new byte[] {  (byte)'_' };
        private static byte[] qpForbidden = WWWForm.DefaultEncoding.GetBytes("&;=?\"'%+_");

        private static byte Hex2Byte(byte[] b, int offset)
        {
            byte result = (byte)0;

            for (int i = offset; i < offset + 2; i++)
            {
                result *= 16;
                int d = b[i];

                if (d >= 48 && d <= 57) // 0 - 9
                    d -= 48;
                else if (d >= 65 && d <= 75) // A -F
                    d -= 55;
                else if (d >= 97 && d <= 102) // a - f
                    d -= 87;
                if (d > 15)
                {
                    return 63; // ?
                }

                result += (byte)d;
            }

            return result;
        }

        private static byte[] Byte2Hex(byte b, byte[] hexChars)
        {
            byte[] dest = new byte[2];
            dest[0] = hexChars[b >> 4];
            dest[1] = hexChars[b & 0xf];
            return dest;
        }

        public static string URLEncode(string toEncode)
        {
            return URLEncode(toEncode, Encoding.UTF8);
        }

        public static string URLEncode(string toEncode, Encoding e)
        {
            byte[] data = Encode(e.GetBytes(toEncode), urlEscapeChar, urlSpace, urlForbidden, false);
            return WWWForm.DefaultEncoding.GetString(data, 0, data.Length);
        }

        public static byte[] URLEncode(byte[] toEncode)
        {
            return Encode(toEncode, urlEscapeChar, urlSpace, urlForbidden, false);
        }

        public static string DataEncode(string toEncode)
        {
            return DataEncode(toEncode, Encoding.UTF8);
        }

        public static string DataEncode(string toEncode, Encoding e)
        {
            byte[] data = Encode(e.GetBytes(toEncode), urlEscapeChar, dataSpace, urlForbidden, false);
            return WWWForm.DefaultEncoding.GetString(data, 0, data.Length);
        }

        public static byte[] DataEncode(byte[] toEncode)
        {
            return Encode(toEncode, urlEscapeChar, dataSpace, urlForbidden, false);
        }

        public static string QPEncode(string toEncode)
        {
            return QPEncode(toEncode, Encoding.UTF8);
        }

        public static string QPEncode(string toEncode, Encoding e)
        {
            byte[] data = Encode(e.GetBytes(toEncode), qpEscapeChar, qpSpace, qpForbidden, true);
            return WWWForm.DefaultEncoding.GetString(data, 0, data.Length);
        }

        public static byte[] QPEncode(byte[] toEncode)
        {
            return Encode(toEncode, qpEscapeChar, qpSpace, qpForbidden, true);
        }

        public static byte[] Encode(byte[] input, byte escapeChar, byte[] space, byte[] forbidden, bool uppercase)
        {
            using (MemoryStream memStream = new MemoryStream(input.Length * 2))
            {
                // encode
                for (int i = 0; i < input.Length; i++)
                {
                    if (input[i] == 32)
                    {
                        memStream.Write(space, 0, space.Length);
                    }
                    else if (input[i] < 32 || input[i] > 126 || ByteArrayContains(forbidden, input[i]))
                    {
                        memStream.WriteByte(escapeChar);
                        memStream.Write(Byte2Hex(input[i], uppercase ? ucHexChars : lcHexChars), 0, 2);
                    }
                    else
                    {
                        memStream.WriteByte(input[i]);
                    }
                }

                return memStream.ToArray();
            }
        }

        private static bool ByteArrayContains(byte[] array, byte b)
        {
            var arrayLength = array.Length;

            for (int i = 0; i < arrayLength; i++)
            {
                if (array[i] == b)
                    return true;
            }

            return false;
        }

        public static string URLDecode(string toEncode)
        {
            return URLDecode(toEncode, Encoding.UTF8);
        }

        public static string URLDecode(string toEncode, Encoding e)
        {
            byte[] data = Decode(WWWForm.DefaultEncoding.GetBytes(toEncode), urlEscapeChar, urlSpace);
            return e.GetString(data, 0, data.Length);
        }

        public static byte[] URLDecode(byte[] toEncode)
        {
            return Decode(toEncode, urlEscapeChar, urlSpace);
        }

        public static string DataDecode(string toDecode)
        {
            return DataDecode(toDecode, Encoding.UTF8);
        }

        public static string DataDecode(string toDecode, Encoding e)
        {
            byte[] data = Decode(WWWForm.DefaultEncoding.GetBytes(toDecode), urlEscapeChar, dataSpace);
            return e.GetString(data, 0, data.Length);
        }

        public static byte[] DataDecode(byte[] toDecode)
        {
            return Decode(toDecode, urlEscapeChar, dataSpace);
        }

        public static string QPDecode(string toEncode)
        {
            return QPDecode(toEncode, Encoding.UTF8);
        }

        public static string QPDecode(string toEncode, Encoding e)
        {
            byte[] data = Decode(WWWForm.DefaultEncoding.GetBytes(toEncode), qpEscapeChar, qpSpace);
            return e.GetString(data, 0, data.Length);
        }

        public static byte[] QPDecode(byte[] toEncode)
        {
            return Decode(toEncode, qpEscapeChar, qpSpace);
        }

        private static bool ByteSubArrayEquals(byte[] array, int index, byte[] comperand)
        {
            if (array.Length - index < comperand.Length)
                return false;
            for (int i = 0; i < comperand.Length; ++i)
                if (array[index + i] != comperand[i])
                    return false;
            return true;
        }

        public static byte[] Decode(byte[] input, byte escapeChar, byte[] space)
        {
            using (MemoryStream memStream = new MemoryStream(input.Length))
            {
                // decode
                for (int i = 0; i < input.Length; i++)
                {
                    if (ByteSubArrayEquals(input, i, space))
                    {
                        i += space.Length - 1;
                        memStream.WriteByte((byte)32);
                    }
                    else if (input[i] == escapeChar && i + 2 < input.Length)
                    {
                        i++;
                        memStream.WriteByte(Hex2Byte(input, i++));
                    }
                    else
                    {
                        memStream.WriteByte(input[i]);
                    }
                }

                return memStream.ToArray();
            }
        }

        public static bool SevenBitClean(string s)
        {
            return SevenBitClean(s, Encoding.UTF8);
        }

        public static bool SevenBitClean(string s, Encoding e)
        {
            return SevenBitClean(e.GetBytes(s));
        }

        public static bool SevenBitClean(byte[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] < 32 || input[i] > 126)
                    return false;
            }

            return true;
        }
    }
}

namespace UnityEngineInternal
{
    static class WebRequestUtils
    {
        private static Regex domainRegex = new Regex("^\\s*\\w+(?:\\.\\w+)+(\\/.*)?$");

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
                return redirectURI.AbsoluteUri;

            var baseURI = new Uri(baseUri, UriKind.Absolute);
            var finalUri = new Uri(baseURI, redirectURI);
            return finalUri.AbsoluteUri;
        }

        internal static string MakeInitialUrl(string targetUrl, string localUrl)
        {
            bool prependingProtocol = false;
            var localUri = new System.Uri(localUrl);
            Uri targetUri = null;

            if (targetUrl[0] == '/')
            {
                // Prepend scheme and (if needed) host
                targetUri = new Uri(localUri, targetUrl);
                prependingProtocol = true;
            }

            if (targetUri == null && domainRegex.IsMatch(targetUrl))
            {
                targetUrl = localUri.Scheme + "://" + targetUrl;
                prependingProtocol = true;
            }

            FormatException ex = null;
            try
            {
                // If URL starts with dot, it is relative and this would throw, skip to combining
                if (targetUri == null && targetUrl[0] != '.')
                    targetUri = new System.Uri(targetUrl);
            }
            catch (FormatException e1)
            {
                // Technically, this should be UriFormatException but MSDN says WSA/PCL doesn't support
                // UriFormatException, and recommends FormatException instead
                // See: https://msdn.microsoft.com/en-us/library/system.uriformatexception%28v=vs.110%29.aspx
                ex = e1;
            }

            if (targetUri == null)
                try
                {
                    targetUri = new System.Uri(localUri, targetUrl);
                    prependingProtocol = true;
                }
                catch (FormatException)
                {
                    throw ex;
                }

            return MakeUriString(targetUri, targetUrl, prependingProtocol);
        }

        internal static string MakeUriString(Uri targetUri, string targetUrl, bool prependingProtocol)
        {
            // for file://protocol pass in unescaped string so we can pass it to VFS
            if (targetUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                if (!targetUri.IsLoopback)
                    return targetUri.OriginalString;
                string path = targetUri.AbsolutePath;
                if (path.Contains("%"))
                    path = URLDecode(path);
                if (path.Length > 0 && path[0] != '/')
                    path = '/' + path;
                return "file://" + path;
            }

            // if URL contains '%', assume it is properly escaped, otherwise '%2f' gets unescaped as '/' (which may not be correct)
            // otherwise escape it, i.e. replaces spaces by '%20'
            if (targetUrl.Contains("%"))
                return targetUri.OriginalString;

            // Special handling for URIs like jar:file (Android), blob:http (WebGL and similar
            // Uri.AbsoluteUri class in those cases results in jar:file/path, which is incorrect because of only one slash
            // Uri.Scheme also returns scheme part before the colon (jar, blob)
            // so if we didn't prepend the scheme and scheme has colon it it, construct the URI from it's parts
            var scheme = targetUri.Scheme;
            if (!prependingProtocol && (targetUrl.Length >= scheme.Length + 2) && targetUrl[scheme.Length + 1] != '/')
            {
                StringBuilder sb = new StringBuilder(scheme, targetUrl.Length);
                sb.Append(':');
                // for these spec URIs path also has the part of URI to right of colon
                // jar:file URIs should be treated like file URIs (unescaped and stripped of query&fragment)
                if (scheme == "jar")
                {
                    string path = targetUri.AbsolutePath;
                    if (path.Contains("%"))
                        path = URLDecode(path);
                    sb.Append(path);
                    return sb.ToString();
                }
                sb.Append(targetUri.PathAndQuery);
                sb.Append(targetUri.Fragment);
                return sb.ToString();
            }

            return targetUri.AbsoluteUri;
        }

        static string URLDecode(string encoded)
        {
            var urlBytes = Encoding.UTF8.GetBytes(encoded);
            var decodedBytes = UnityEngine.WWWTranscoder.URLDecode(urlBytes);
            return Encoding.UTF8.GetString(decodedBytes);
        }
    }
}
