// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.LightBaking;

namespace UnityEditor.LightBaking
{
    [NativeHeader("Editor/Src/GI/LightBaker/LightBaker.Bindings.h")]
    internal static partial class LightBaker
    {
        [NativeMethod(IsThreadSafe = true)]
        internal static extern Result PopulateWorldRadeonRays(BakeInput bakeInput, BakeProgressState progress,
            UnityEngine.LightTransport.RadeonRaysContext context, UnityEngine.LightBaking.IntegrationContext world);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe Result IntegrateProbeDirectRadianceRadeonRays(void* positions, IntegrationContext integrationContext,
            int positionOffset, int positionCount, float pushoff, int bounceCount, int directSampleCount, int giSampleCount, int envSampleCount,
            UnityEngine.LightTransport.RadeonRaysContext context, BakeProgressState progress, void* radianceBufferOut);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe Result IntegrateProbeIndirectRadianceRadeonRays(void* positions, IntegrationContext integrationContext,
            int positionOffset, int positionCount, float pushoff, int bounceCount, int directSampleCount, int giSampleCount, int envSampleCount,
            UnityEngine.LightTransport.RadeonRaysContext context, BakeProgressState progress, void* radianceBufferOut);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe Result IntegrateProbeValidityRadeonRays(void* positions, IntegrationContext integrationContext,
            int positionOffset, int positionCount, float pushoff, int bounceCount, int directSampleCount, int giSampleCount, int envSampleCount,
            UnityEngine.LightTransport.RadeonRaysContext context, BakeProgressState progress, void* validityBufferOut);
    }
}
