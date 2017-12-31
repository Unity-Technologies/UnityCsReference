// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Internal;

namespace UnityEditor
{
    [NativeType(CodegenOptions.Custom, "MonoAnimationClipSettings")]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class AnimationClipSettings
    {
        public AnimationClip additiveReferencePoseClip;
        public float additiveReferencePoseTime;
        public float startTime;
        public float stopTime;
        public float orientationOffsetY;
        public float level;
        public float cycleOffset;
        public bool hasAdditiveReferencePose;
        public bool loopTime;
        public bool loopBlend;
        public bool loopBlendOrientation;
        public bool loopBlendPositionY;
        public bool loopBlendPositionXZ;
        public bool keepOriginalOrientation;
        public bool keepOriginalPositionY;
        public bool keepOriginalPositionXZ;
        public bool heightFromFeet;
        public bool mirror;
    }
}
