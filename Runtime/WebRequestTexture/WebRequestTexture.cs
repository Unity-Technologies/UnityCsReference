// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Networking
{
    public static class UnityWebRequestTexture
    {
        public static UnityWebRequest GetTexture(string uri)
        {
            return UnityWebRequestTexture.GetTexture(uri, false);
        }

        public static UnityWebRequest GetTexture(string uri, bool nonReadable)
        {
            return new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET, new DownloadHandlerTexture(!nonReadable), null);
        }

    }
}
