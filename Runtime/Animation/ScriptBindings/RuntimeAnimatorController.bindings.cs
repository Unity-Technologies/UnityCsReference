// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Animation/RuntimeAnimatorController.h")]
    [UsedByNativeCode]
    [ExcludeFromObjectFactory]
    public partial class RuntimeAnimatorController : Object
    {
        protected RuntimeAnimatorController() {}

        extern public AnimationClip[] animationClips { get; }
    }
}
