// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEditor.Connect
{
    internal class UnityConnectWebRequestException : Exception
    {
        public string error { get; set; }
        public string method { get; set; }
        public string url { get; set; }
        public long responseCode { get; set; }
        public bool isHttpError { get; set; }
        public bool isNetworkError { get; set; }
        public Dictionary<string, string> responseHeaders { get; set; }
        public int timeout { get; set; }

        public UnityConnectWebRequestException(string message) : base(message)
        {
        }
    }
}
