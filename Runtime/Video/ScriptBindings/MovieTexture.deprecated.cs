// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    [ExcludeFromPreset]
    [ExcludeFromObjectFactory]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("MovieTexture is removed. Use VideoPlayer instead.", true)]
    public sealed class MovieTexture : Texture
    {
        static void FeatureRemoved() { throw new Exception("MovieTexture has been removed from Unity. Use VideoPlayer instead."); }

        private MovieTexture() {}

        [Obsolete("MovieTexture is removed. Use VideoPlayer instead.", true)]
        public void Play() { FeatureRemoved(); }
        [Obsolete("MovieTexture is removed. Use VideoPlayer instead.", true)]
        public void Stop() { FeatureRemoved(); }
        [Obsolete("MovieTexture is removed. Use VideoPlayer instead.", true)]
        public void Pause() { FeatureRemoved(); }
        [Obsolete("MovieTexture is removed. Use VideoPlayer instead.", true)]
        public AudioClip audioClip
        {
            get { FeatureRemoved(); return null; }
        }
        [Obsolete("MovieTexture is removed. Use VideoPlayer instead.", true)]
        public bool loop
        {
            get { FeatureRemoved(); return false; }
            set { FeatureRemoved(); }
        }
        [Obsolete("MovieTexture is removed. Use VideoPlayer instead.", true)]
        public bool isPlaying
        {
            get { FeatureRemoved(); return false; }
        }
        [Obsolete("MovieTexture is removed. Use VideoPlayer instead.", true)]
        public bool isReadyToPlay
        {
            get { FeatureRemoved(); return false; }
        }
        [Obsolete("MovieTexture is removed. Use VideoPlayer instead.", true)]
        public float duration
        {
            get { FeatureRemoved(); return 1.0f; }
        }
    }
}
