// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace UnityEditor
{
    static class WaveformPreviewFactory
    {
        public static WaveformPreview Create(int initialSize, AudioClip clip)
        {
            return new StreamedAudioClipPreview(clip, initialSize);
        }
    }
}
