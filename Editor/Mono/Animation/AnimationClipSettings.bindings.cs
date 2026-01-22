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
    [NativeHeader("Modules/Animation/AnimationClipSettings.h")]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class AnimationClipSettings
    {
        [NativeName("m_AdditiveReferencePoseClip")]
        public AnimationClip additiveReferencePoseClip;
        [NativeName("m_AdditiveReferencePoseTime")]
        public float additiveReferencePoseTime;
        [NativeName("m_StartTime")]
        public float startTime;
        [NativeName("m_StopTime")]
        public float stopTime;
        [NativeName("m_OrientationOffsetY")]
        public float orientationOffsetY;
        [NativeName("m_Level")]
        public float level;
        [NativeName("m_CycleOffset")]
        public float cycleOffset;
        [NativeName("m_HasAdditiveReferencePose")]
        public bool hasAdditiveReferencePose;
        [NativeName("m_LoopTime")]
        public bool loopTime;
        [NativeName("m_LoopBlend")]
        public bool loopBlend;
        [NativeName("m_LoopBlendOrientation")]
        public bool loopBlendOrientation;
        [NativeName("m_LoopBlendPositionY")]
        public bool loopBlendPositionY;
        [NativeName("m_LoopBlendPositionXZ")]
        public bool loopBlendPositionXZ;
        [NativeName("m_KeepOriginalOrientation")]
        public bool keepOriginalOrientation;
        [NativeName("m_KeepOriginalPositionY")]
        public bool keepOriginalPositionY;
        [NativeName("m_KeepOriginalPositionXZ")]
        public bool keepOriginalPositionXZ;
        [NativeName("m_HeightFromFeet")]
        public bool heightFromFeet;
        [NativeName("m_Mirror")]
        public bool mirror;
    }
}
