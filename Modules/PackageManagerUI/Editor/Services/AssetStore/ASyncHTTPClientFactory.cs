// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.AssetStore
{
    internal class ASyncHTTPClientFactory : IASyncHTTPClientFactory
    {
        public IAsyncHTTPClient GetASyncHTTPClient(string url) => new AsyncHTTPClient(url);

        public IAsyncHTTPClient GetASyncHTTPClient(string url, string method) => new AsyncHTTPClient(url, method);

        public void AbortASyncHTTPClientByTag(string tag) => AsyncHTTPClient.AbortByTag(tag);
    }
}
