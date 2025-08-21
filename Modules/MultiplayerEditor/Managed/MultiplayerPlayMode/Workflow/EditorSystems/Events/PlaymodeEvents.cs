// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class PlaymodeEvents
    {
        public event Action Play;
        public event Action Pause;
        public event Action Step;
        public event Action Unpause;
        public event Action Stop;

        internal void InvokePlay()
        {
            Play?.Invoke();
        }

        internal void InvokePause()
        {
            Pause?.Invoke();
        }

        internal void InvokeStep()
        {
            Step?.Invoke();
        }

        internal void InvokeUnpause()
        {
            Unpause?.Invoke();
        }

        internal void InvokeStop()
        {
            Stop?.Invoke();
        }
    }
}
