// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEngine.Playables
{
    public interface IScriptPlayable
    {
        void OnGraphStart();
        void OnGraphStop();

        void PrepareFrame(FrameData info);
        void ProcessFrame(FrameData info, object playerData);
        void OnPlayStateChanged(FrameData info, PlayState newState);
    }
}
