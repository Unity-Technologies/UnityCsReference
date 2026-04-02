// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Timeline.Foundation.Model
{
    interface IPlayer : IDisposable
    {
        IPlayerEvents playerEvents { get; }
        //void SetSequence(ISequenceAsset asset);
        //void SetPlaybackContext(object context);
        bool isPlaying { get; }
        bool isPreviewing { get; }
        bool isPaused { get; }
        bool enabled { get; }
        void Play();
        void Pause();
        void Stop();
        void Enable();
        void Disable();
        void Evaluate(TimeSourceData timeSource);
        void EnablePreview();
        void DisablePreview();
    }
}
