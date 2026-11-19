// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.IntegerTime;
using UnityEngine;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    static public partial class Core
    {
        /// <summary>
        /// Clip necessary pre baked data to sample root motion during runtime.
        /// </summary>
        internal struct ClipRootMotionSamplingData
        {
            public int RootTranslationIndex;
            public int RootRotationIndex;
            // The math.RotationOrder of the root rotation when it is animated by Euler angles in degrees (3 curves),
            // or -1 when it is animated by quaternion curves (4 curves).
            public int RootRotationEulerOrder;
            public RigidTransform InvStartX;
            public RigidTransform TotalX;
        }

        internal static RigidTransform ComputeRootMotionDelta(in ClipRootMotionSamplingData samplingData, RigidTransform prevX, RigidTransform curX, WrapMode wrapMode, int loopCount)
        {
            prevX = math.mul(samplingData.InvStartX, prevX);
            curX = math.mul(samplingData.InvStartX, curX);

            if (wrapMode == WrapMode.Loop || (wrapMode == WrapMode.Once && loopCount != 0))
            {
                var totalX = MathUtility.select(samplingData.TotalX, math.inverse(samplingData.TotalX), loopCount < 0);
                var cycleX = MathUtility.rigidPow(totalX, math.asuint(math.abs(loopCount)));

                curX = math.mul(cycleX, curX);
            }

            var deltaX = math.mul(math.inverse(prevX), curX);

            return deltaX;
        }
    }
}
