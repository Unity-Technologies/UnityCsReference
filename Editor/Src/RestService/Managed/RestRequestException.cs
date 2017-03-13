// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.RestService
{
    internal class RestRequestException : Exception
    {
        public RestRequestException()
        {
        }

        public RestRequestException(HttpStatusCode httpStatusCode, string restErrorString) : this(httpStatusCode, restErrorString, null)
        {
        }

        public RestRequestException(HttpStatusCode httpStatusCode, string restErrorString, string restErrorDescription)
        {
            HttpStatusCode = httpStatusCode;
            RestErrorString = restErrorString;
            RestErrorDescription = restErrorDescription;
        }

        public string RestErrorString { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }
        public string RestErrorDescription { get; set; }
    }
}
