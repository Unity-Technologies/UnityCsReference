// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UnityEditor
{
    /*
     * A HTTP job for performing HTTP requests in a thread
     * This class is primarily used by the Server class.
     */
    internal partial class AsyncHTTPClient
    {
        internal enum State
        {
            INIT,
            CONNECTING,
            CONNECTED,
            UPLOADING,
            DOWNLOADING,
            CONFIRMING,
            DONE_OK,
            DONE_FAILED,
            ABORTED,
            TIMEOUT
        }
        private IntPtr m_Handle;
        public delegate void DoneCallback(AsyncHTTPClient client);
        public delegate void StatusCallback(State status, int bytesDone, int bytesTotal);

        public StatusCallback statusCallback;
        public DoneCallback doneCallback;

        string m_ToUrl;
        string m_FromData;
        string m_Method;

        public string url
        {
            get {  return m_ToUrl; }
        }

        public string text
        {
            get
            {
                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                byte[] b = bytes;
                if (b == null) return null;
                return encoding.GetString(b);
            }
        }

        public byte[] bytes
        {
            get
            {
                return GetBytesByHandle(m_Handle);
            }
        }
        public Texture2D texture
        {
            get
            {
                return GetTextureByHandle(m_Handle);
            }
        }
        public State state { get; private set; }
        public int responseCode { get; private set; }
        public string tag { get; set; }

        public Dictionary<string, string> header;

        /* GET request
         *
         */
        public AsyncHTTPClient(string _toUrl)
        {
            m_ToUrl = _toUrl;
            m_FromData = null;
            m_Method = "";
            state = State.INIT;
            header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            m_Handle = (IntPtr)0;
            tag = "";
            statusCallback = null;
        }

        /* Any method request
         *
         */
        public AsyncHTTPClient(string _toUrl, string _method)
        {
            m_ToUrl = _toUrl;
            m_FromData = null;
            m_Method = _method;
            state = State.INIT;
            header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            m_Handle = (IntPtr)0;
            tag = "";
            statusCallback = null;
        }

        /* If this job has been set as a POST job this will overwrite the
         * data to be posted. The job must not have been started yet
         * ie. Begin() should not have been called.
         */
        public string postData
        {
            set
            {
                m_FromData = value;
                if (m_Method == "")
                    m_Method = "POST";
                if (!header.ContainsKey("Content-Type"))
                    header["Content-Type"] = "application/x-www-form-urlencoded";
            }
        }

        /*
         * POST request for uploading url application/x-www-form-urlencoded dictionary.
         * The encoding normally allows for duplicate keys, but this method is restricted
         * to unique keys.
         */
        public Dictionary<string, string> postDictionary
        {
            set
            {
                postData = string.Join("&", value.Select(kv => EscapeLong(kv.Key) + "=" + EscapeLong(kv.Value)).ToArray());
            }
        }

        /*
         *
         */
        public void Abort()
        {
            state = State.ABORTED;

            AbortByHandle(m_Handle);
        }

        public bool IsAborted()
        {
            return state == State.ABORTED;
        }

        public bool IsDone()
        {
            return IsDone(state);
        }

        public static bool IsDone(State state)
        {
            switch (state)
            {
                case State.DONE_OK:
                case State.DONE_FAILED:
                case State.ABORTED:
                case State.TIMEOUT:
                    return true;
                default: return false;
            }
        }

        public bool IsSuccess()
        {
            return state == State.DONE_OK;
        }

        public static bool IsSuccess(State state)
        {
            return state == State.DONE_OK;
        }

        public void Begin()
        {
            if (IsAborted())
            {
                state = State.ABORTED;
                return;
            }
            if (m_Method == "")
                m_Method = "GET";

            string[] headerFlattened = header.Select(kv => string.Format("{0}: {1}", kv.Key, kv.Value)).ToArray();

            m_Handle = SubmitClientRequest(tag, m_ToUrl, headerFlattened, m_Method, m_FromData, Done, Progress);
        }

        private void Done(State status, int i_ResponseCode)
        {
            state = status;
            responseCode = i_ResponseCode;

            if (doneCallback != null)
                doneCallback(this);

            m_Handle = (IntPtr)0; // The CurlRequestMessage will be deallocated after this callback returns
        }

        private void Progress(State status, int bytesDone, int bytesTotal)
        {
            state = status;
            if (statusCallback != null)
                statusCallback(status, bytesDone, bytesTotal);
        }

        /*
         * The normal escape function does not support strings longer than 32766 characters
         */
        private string EscapeLong(string v)
        {
            StringBuilder q = new StringBuilder();
            const int c_ChunkLength = 32766;
            for (int i = 0; i < v.Length; i += c_ChunkLength)
            {
                q.Append(System.Uri.EscapeDataString(v.Substring(i, v.Length - i > c_ChunkLength ? c_ChunkLength : v.Length - i)));
            }
            return q.ToString();
        }
    }
}
