// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using Unity.Jobs.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Animations
{
    [JobProducerType(typeof(ProcessAnimationJobStruct<>))]
    public interface IAnimationJob
    {
        void ProcessAnimation(AnimationStream stream);
        void ProcessRootMotion(AnimationStream stream);
    }
}

