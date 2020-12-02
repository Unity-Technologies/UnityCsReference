// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.AssetStore
{
    internal interface IASyncHTTPClientFactory
    {
        IAsyncHTTPClient GetASyncHTTPClient(string url);

        IAsyncHTTPClient GetASyncHTTPClient(string url, string method);

        void AbortASyncHTTPClientByTag(string tag);
    }
}
