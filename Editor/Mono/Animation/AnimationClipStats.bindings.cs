// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Internal;

namespace UnityEditor
{
    // Must be kept in sync with AnimationClipStats in AnimationClipStats
    internal struct AnimationClipStats
    {
        public int size;
        public int positionCurves;
        public int quaternionCurves;
        public int eulerCurves;
        public int scaleCurves;
        public int muscleCurves;
        public int genericCurves;
        public int pptrCurves;
        public int totalCurves;
        public int constantCurves;
        public int denseCurves;
        public int streamCurves;

        public void Reset()
        {
            size = 0;
            positionCurves = 0;
            quaternionCurves = 0;
            eulerCurves = 0;
            scaleCurves = 0;
            muscleCurves = 0;
            genericCurves = 0;
            pptrCurves = 0;
            totalCurves = 0;
            constantCurves = 0;
            denseCurves = 0;
            streamCurves = 0;
        }

        public void Combine(AnimationClipStats other)
        {
            size += other.size;
            positionCurves += other.positionCurves;
            quaternionCurves += other.quaternionCurves;
            eulerCurves += other.eulerCurves;
            scaleCurves += other.scaleCurves;
            muscleCurves += other.muscleCurves;
            genericCurves += other.genericCurves;
            pptrCurves += other.pptrCurves;
            totalCurves += other.totalCurves;
            constantCurves += other.constantCurves;
            denseCurves += other.denseCurves;
            streamCurves += other.streamCurves;
        }
    }
}
