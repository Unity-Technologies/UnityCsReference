// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEngine
{
    public static class WWWAudioExtensions
    {
        [System.Obsolete("WWWAudioExtensions.GetAudioClip extension method has been replaced by WWW.GetAudioClip instance method. (UnityUpgradable) -> WWW.GetAudioClip()", true)]
        public static AudioClip GetAudioClip(this WWW www)
        {
            return www.GetAudioClip();
        }

        [System.Obsolete("WWWAudioExtensions.GetAudioClip extension method has been replaced by WWW.GetAudioClip instance method. (UnityUpgradable) -> WWW.GetAudioClip([mscorlib] System.Boolean)", true)]
        public static AudioClip GetAudioClip(this WWW www, bool threeD)
        {
            return www.GetAudioClip(threeD);
        }

        [System.Obsolete("WWWAudioExtensions.GetAudioClip extension method has been replaced by WWW.GetAudioClip instance method. (UnityUpgradable) -> WWW.GetAudioClip([mscorlib] System.Boolean, [mscorlib] System.Boolean)", true)]
        public static AudioClip GetAudioClip(this WWW www, bool threeD, bool stream)
        {
            return www.GetAudioClip(threeD, stream);
        }

        [System.Obsolete("WWWAudioExtensions.GetAudioClip extension method has been replaced by WWW.GetAudioClip instance method. (UnityUpgradable) -> WWW.GetAudioClip([mscorlib] System.Boolean, [mscorlib] System.Boolean, UnityEngine.AudioType)", true)]
        public static AudioClip GetAudioClip(this WWW www, bool threeD, bool stream, AudioType audioType)
        {
            return www.GetAudioClip(threeD, stream, audioType);
        }

        [System.Obsolete("WWWAudioExtensions.GetAudioClipCompressed extension method has been replaced by WWW.GetAudioClipCompressed instance method. (UnityUpgradable) -> WWW.GetAudioClipCompressed()", true)]
        public static AudioClip GetAudioClipCompressed(this WWW www)
        {
            return www.GetAudioClipCompressed();
        }

        [System.Obsolete("WWWAudioExtensions.GetAudioClipCompressed extension method has been replaced by WWW.GetAudioClipCompressed instance method. (UnityUpgradable) -> WWW.GetAudioClipCompressed([mscorlib] System.Boolean)", true)]
        public static AudioClip GetAudioClipCompressed(this WWW www, bool threeD)
        {
            return www.GetAudioClipCompressed(threeD);
        }

        [System.Obsolete("WWWAudioExtensions.GetAudioClipCompressed extension method has been replaced by WWW.GetAudioClipCompressed instance method. (UnityUpgradable) -> WWW.GetAudioClipCompressed([mscorlib] System.Boolean, UnityEngine.AudioType)", true)]
        public static AudioClip GetAudioClipCompressed(this WWW www, bool threeD, AudioType audioType)
        {
            return www.GetAudioClipCompressed(threeD, audioType);
        }

        [System.Obsolete("WWWAudioExtensions.GetMovieTexture extension method has been replaced by WWW.GetMovieTexture instance method. (UnityUpgradable) -> WWW.GetMovieTexture()", true)]
        public static MovieTexture GetMovieTexture(this WWW www)
        {
            return www.GetMovieTexture();
        }

    }
}

