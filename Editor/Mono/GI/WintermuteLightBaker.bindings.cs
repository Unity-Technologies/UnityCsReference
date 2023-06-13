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
        internal static extern Result PopulateWorldWintermute(BakeInput bakeInput, BakeProgressState progress,
            UnityEngine.LightTransport.WintermuteContext context, UnityEngine.LightBaking.IntegrationContext world);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe Result IntegrateProbeDirectRadianceWintermute(UnityEngine.Vector3* positions, IntegrationContext integrationContext,
            int positionOffset, int positionCount, float pushoff, int bounceCount, int directSampleCount, int giSampleCount, int envSampleCount,
            UnityEngine.LightTransport.WintermuteContext context, BakeProgressState progress, void* radianceBufferOut);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe Result IntegrateProbeIndirectRadianceWintermute(UnityEngine.Vector3* positions, IntegrationContext integrationContext,
            int positionOffset, int positionCount, float pushoff, int bounceCount, int directSampleCount, int giSampleCount, int envSampleCount,
            UnityEngine.LightTransport.WintermuteContext context, BakeProgressState progress, void* radianceBufferOut);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe Result IntegrateProbeValidityWintermute(void* positions, IntegrationContext integrationContext,
            int positionOffset, int positionCount, float pushoff, int bounceCount, int directSampleCount, int giSampleCount, int envSampleCount,
            UnityEngine.LightTransport.WintermuteContext context, BakeProgressState progress, void* validityBufferOut);
    }
}
