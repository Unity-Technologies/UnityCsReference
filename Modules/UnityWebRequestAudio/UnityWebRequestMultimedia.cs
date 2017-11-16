// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Networking
{
    public static class UnityWebRequestMultimedia
    {
        public static UnityWebRequest GetAudioClip(string uri, AudioType audioType)
        {
            return new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET, new DownloadHandlerAudioClip(uri, audioType), null);
        }

        public static UnityWebRequest GetAudioClip(Uri uri, AudioType audioType)
        {
            return new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET, new DownloadHandlerAudioClip(uri, audioType), null);
        }


        public static UnityWebRequest GetMovieTexture(string uri)
        {
            return new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET, new DownloadHandlerMovieTexture(), null);
        }

        public static UnityWebRequest GetMovieTexture(Uri uri)
        {
            return new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET, new DownloadHandlerMovieTexture(), null);
        }

    }
}
