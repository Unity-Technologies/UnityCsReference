// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Collections.Generic;

namespace UnityEngine
{
    namespace Experimental.GlobalIllumination
    {
        [NativeConditional("ENABLE_RUNTIME_GI")]
        [NativeHeader("Runtime/Export/GI/ScriptBindings/Lights.h")]
        internal static class Lights
        {
            // called by the Lightmapper (C# -> C++)
            public static extern void GetModified(List<Light> outLights);

            public static void SetFromScript(List<LightDataGI> lights)
            {
                // there's a copy here until the new binding system properly supports passing down List<...>
                SetFromScript_Internal((LightDataGI[])NoAllocHelpers.ExtractArrayFromList(lights),
                    NoAllocHelpers.SafeLength(lights));
            }

            extern private static void SetFromScript_Internal(LightDataGI[] lights, int count);

            // called by the baking backends (C++ -> C#)
            [RequiredByNativeCode]
            private static void RequestLights_Internal()
            {
                Lightmapping.RequestLights();
            }
        }
    }
}
