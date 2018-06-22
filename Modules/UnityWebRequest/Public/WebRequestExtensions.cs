// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.Networking
{
    // This file extends UnityWebRequest's public API with convenient wrappers
    // for common operations.
    public partial class UnityWebRequest
    {
        public static UnityWebRequest Get(string uri)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbGET, new DownloadHandlerBuffer(), null);
            return request;
        }

        public static UnityWebRequest Delete(string uri)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbDELETE);
            return request;
        }

        public static UnityWebRequest Head(string uri)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbHEAD);
            return request;
        }

        [Obsolete("UnityWebRequest.GetTexture is obsolete. Use UnityWebRequestTexture.GetTexture instead (UnityUpgradable) -> [UnityEngine] UnityWebRequestTexture.GetTexture(*)", true)]
        public static UnityWebRequest GetTexture(string uri)
        {
            throw new NotSupportedException("UnityWebRequest.GetTexture is obsolete. Use UnityWebRequestTexture.GetTexture instead.");
        }

        [Obsolete("UnityWebRequest.GetTexture is obsolete. Use UnityWebRequestTexture.GetTexture instead (UnityUpgradable) -> [UnityEngine] UnityWebRequestTexture.GetTexture(*)", true)]
        public static UnityWebRequest GetTexture(string uri, bool nonReadable)
        {
            throw new NotSupportedException("UnityWebRequest.GetTexture is obsolete. Use UnityWebRequestTexture.GetTexture instead.");
        }

        [Obsolete("UnityWebRequest.GetAudioClip is obsolete. Use UnityWebRequestMultimedia.GetAudioClip instead (UnityUpgradable) -> [UnityEngine] UnityWebRequestMultimedia.GetAudioClip(*)", true)]
        public static UnityWebRequest GetAudioClip(string uri, AudioType audioType)
        {
            return null;
        }

        public static UnityWebRequest GetAssetBundle(string uri)
        {
            return GetAssetBundle(uri, 0);
        }

        public static UnityWebRequest GetAssetBundle(string uri, uint crc)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    kHttpVerbGET,
                    new DownloadHandlerAssetBundle(uri, crc),
                    null
                    );

            return request;
        }

        public static UnityWebRequest GetAssetBundle(string uri, uint version, uint crc)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    kHttpVerbGET,
                    new DownloadHandlerAssetBundle(uri, version, crc),
                    null
                    );

            return request;
        }

        public static UnityWebRequest GetAssetBundle(string uri, Hash128 hash, uint crc)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    kHttpVerbGET,
                    new DownloadHandlerAssetBundle(uri, hash, crc),
                    null
                    );

            return request;
        }

        public static UnityWebRequest GetAssetBundle(string uri, CachedAssetBundle cachedAssetBundle, uint crc)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    kHttpVerbGET,
                    new DownloadHandlerAssetBundle(uri, cachedAssetBundle.name, cachedAssetBundle.hash, crc),
                    null
                    );

            return request;
        }

        public static UnityWebRequest Put(string uri, byte[] bodyData)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    kHttpVerbPUT,
                    new DownloadHandlerBuffer(),
                    new UploadHandlerRaw(bodyData)
                    );

            return request;
        }

        public static UnityWebRequest Put(string uri, string bodyData)
        {
            UnityWebRequest request = new UnityWebRequest(
                    uri,
                    kHttpVerbPUT,
                    new DownloadHandlerBuffer(),
                    new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(bodyData))
                    );

            return request;
        }

        public static UnityWebRequest Post(string uri, string postData)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);

            byte[] payload = null;
            if (!string.IsNullOrEmpty(postData))
            {
                string urlencoded = WWWTranscoder.DataEncode(postData, System.Text.Encoding.UTF8);
                payload = System.Text.Encoding.UTF8.GetBytes(urlencoded);
            }

            request.uploadHandler = new UploadHandlerRaw(payload);
            request.uploadHandler.contentType = "application/x-www-form-urlencoded";
            request.downloadHandler = new DownloadHandlerBuffer();

            return request;
        }

        // Provides a shim for sending a multipart form as declared by the legacy WWWForm class.
        public static UnityWebRequest Post(string uri, WWWForm formData)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);

            byte[] payload = null;
            if (formData != null)
            {
                payload = formData.data;
                if (payload.Length == 0)
                    payload = null;
            }

            request.uploadHandler = new UploadHandlerRaw(payload);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Add in appropriate Content-Type header
            if (formData != null)
            {
                Dictionary<string, string> formHeaders = formData.headers;
                foreach (KeyValuePair<string, string> header in formHeaders)
                    request.SetRequestHeader(header.Key, header.Value);
            }

            return request;
        }

        // Provides a way to send a multipart form using the modern IMultipartFormSection API.
        public static UnityWebRequest Post(string uri, List<IMultipartFormSection> multipartFormSections)
        {
            byte[] boundary = GenerateBoundary();
            return Post(uri, multipartFormSections, boundary);
        }

        public static UnityWebRequest Post(string uri, List<IMultipartFormSection> multipartFormSections, byte[] boundary)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);

            byte[] payload = null;
            if (multipartFormSections != null && multipartFormSections.Count != 0)
                payload = SerializeFormSections(multipartFormSections, boundary);

            UploadHandler uploadHandler = new UploadHandlerRaw(payload);
            uploadHandler.contentType = "multipart/form-data; boundary=" + System.Text.Encoding.UTF8.GetString(boundary, 0, boundary.Length);

            request.uploadHandler = uploadHandler;
            request.downloadHandler = new DownloadHandlerBuffer();

            return request;
        }

        // Provides a way to send a simple urlencoded form body, for simple forms without file sections.
        public static UnityWebRequest Post(string uri, Dictionary<string, string> formFields)
        {
            UnityWebRequest request = new UnityWebRequest(uri, kHttpVerbPOST);

            byte[] payload = null;
            if (formFields != null && formFields.Count != 0)
                payload = SerializeSimpleForm(formFields);

            UploadHandler formUploadHandler = new UploadHandlerRaw(payload);
            formUploadHandler.contentType = "application/x-www-form-urlencoded";

            request.uploadHandler = formUploadHandler;
            request.downloadHandler = new DownloadHandlerBuffer();

            return request;
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

            var bytes = Encoding.UTF8.GetBytes(s);
            var decodedBytes = WWWTranscoder.URLEncode(bytes);
            return e.GetString(decodedBytes);
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

            var bytes = Encoding.UTF8.GetBytes(s);
            var decodedBytes = WWWTranscoder.URLDecode(bytes);
            return e.GetString(decodedBytes);
        }

        public static byte[] SerializeFormSections(List<IMultipartFormSection> multipartFormSections, byte[] boundary)
        {
            if (multipartFormSections == null || multipartFormSections.Count == 0)
                return null;

            byte[] crlf = System.Text.Encoding.UTF8.GetBytes("\r\n");
            byte[] dDash = WWWForm.DefaultEncoding.GetBytes("--");

            int estimatedSize = 0;
            foreach (IMultipartFormSection section in multipartFormSections)
            {
                estimatedSize += 64 + section.sectionData.Length;
            }

            List<byte> formData = new List<byte>(estimatedSize);
            foreach (IMultipartFormSection section in multipartFormSections)
            {
                string disposition = "form-data";

                string sectionName = section.sectionName;
                string fileName = section.fileName;

                string header = "Content-Disposition: " + disposition;

                if (!string.IsNullOrEmpty(sectionName))
                {
                    header += "; name=\"" + sectionName + "\"";
                }

                if (!string.IsNullOrEmpty(fileName))
                {
                    header += "; filename=\"" + fileName + "\"";
                }

                header += "\r\n";

                string contentType = section.contentType;
                if (!string.IsNullOrEmpty(contentType))
                {
                    header += "Content-Type: " + contentType + "\r\n";
                }

                formData.AddRange(crlf);
                formData.AddRange(dDash);
                formData.AddRange(boundary);
                formData.AddRange(crlf);
                formData.AddRange(System.Text.Encoding.UTF8.GetBytes(header));
                formData.AddRange(crlf);
                formData.AddRange(section.sectionData);
            }

            // end sections with boundary delimiter (https://tools.ietf.org/html/rfc2046)
            formData.AddRange(crlf);
            formData.AddRange(dDash);
            formData.AddRange(boundary);
            formData.AddRange(dDash);
            formData.AddRange(crlf);
            return formData.ToArray();
        }

        public static byte[] GenerateBoundary()
        {
            // Generate a random boundary
            byte[] boundary = new byte[40];
            for (int i = 0; i < 40; i++)
            {
                int randomChar = Random.Range(48, 110);
                if (randomChar > 57) // skip unprintable chars between 57 and 64 (inclusive)
                    randomChar += 7;
                if (randomChar > 90) // and 91 and 96 (inclusive)
                    randomChar += 6;
                boundary[i] = (byte)randomChar;
            }
            return boundary;
        }

        public static byte[] SerializeSimpleForm(Dictionary<string, string> formFields)
        {
            string queryString = "";
            foreach (KeyValuePair<string, string> pair in formFields)
            {
                if (queryString.Length > 0) { queryString += "&"; }
                queryString += WWWTranscoder.DataEncode(pair.Key) + "=" + WWWTranscoder.DataEncode(pair.Value);
            }
            return System.Text.Encoding.UTF8.GetBytes(queryString);
        }
    }
}
