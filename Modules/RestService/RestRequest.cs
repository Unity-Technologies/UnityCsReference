// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Net;
using System.Text;

namespace UnityEditor.RestService
{
    internal class RestRequest
    {
        static public bool Send(string endpoint, string payload, int timeout)
        {
            if (ScriptEditorSettings.ServerURL == null)
                return false;

            // Send POST request
            byte[] content = Encoding.UTF8.GetBytes(payload);

            var request = WebRequest.Create(ScriptEditorSettings.ServerURL + endpoint);
            request.Timeout = timeout;
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = content.Length;

            try
            {
                var stream = request.GetRequestStream();
                stream.Write(content, 0, content.Length);
                stream.Close();
            }
            catch (Exception e)
            {
                Logger.Log(e);
                return false;
            }

            try
            {
                request.BeginGetResponse(GetResponseCallback, request);
            }
            catch (Exception e)
            {
                Logger.Log(e);
                return false;
            }

            return true;
        }

        private static void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            var request = (WebRequest)asynchronousResult.AsyncState;
            var response = request.EndGetResponse(asynchronousResult);

            try
            {
                var stream = response.GetResponseStream();
                var reader = new StreamReader(stream);

                reader.ReadToEnd();

                reader.Close();
                stream.Close();
            }
            finally
            {
                response.Close();
            }
        }
    }
}
