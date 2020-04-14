// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    internal class HttpClientFactory
    {
        public virtual IAsyncHTTPClient GetASyncHTTPClient(string url)
        {
            return new AsyncHTTPClient(url);
        }

        public virtual IAsyncHTTPClient PostASyncHTTPClient(string url, string postData)
        {
            return new AsyncHTTPClient(url, "POST") { postData = postData };
        }
    }
}
