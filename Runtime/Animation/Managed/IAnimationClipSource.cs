// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

namespace UnityEngine
{
    public interface IAnimationClipSource
    {
        void GetAnimationClips(List<AnimationClip> results);
    }
}
