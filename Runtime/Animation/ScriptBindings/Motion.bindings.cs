// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Animation/Motion.h")]
    public partial class Motion : Object
    {
        protected Motion() {}

        extern public float averageDuration { get; }
        extern public float averageAngularSpeed { get; }
        extern public Vector3 averageSpeed { get; }
        extern public float apparentSpeed { get; }

        extern public bool isLooping
        {
            [NativeMethod("IsLooping")]
            get;
        }

        extern public bool legacy
        {
            [NativeMethod("IsLegacy")]
            get;
        }

        extern public bool isHumanMotion
        {
            [NativeMethod("IsHumanMotion")]
            get;
        }

        [Obsolete("ValidateIfRetargetable is not supported anymore, please use isHumanMotion instead.", true)]
        public bool ValidateIfRetargetable(bool val) { return false; }

        [Obsolete("isAnimatorMotion is not supported anymore, please use !legacy instead.", true)]
        public bool isAnimatorMotion { get; }
    }
}
