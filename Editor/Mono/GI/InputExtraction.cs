// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.LightBaking;
using static UnityEditor.LightBaking.LightBaker;

namespace UnityEngine.LightTransport
{
    public static class InputExtraction
    {
        // Completely opaque in the public API, this effectively hides internal LightBaker details.
        public class BakeInput
        {
            internal BakeInput(LightBaker.BakeInput editorBakeInput)
            {
                bakeInput = editorBakeInput;
            }
            internal LightBaker.BakeInput bakeInput { get; }

            // Requests are not exposed in the public API, but we need them in LightBaker.PopulateWorld(). Going forward, requests will be split from the bake input in the public API as well.
            internal LightmapRequests lightmapRequests { get; set; }
            internal LightProbeRequests lightProbeRequests { get; set; }
        }

        public static bool ExtractFromScene(out BakeInput bakeInput)
        {
            const string outputFolderPath = "unused"; // We are not using disk IO.
            LightBaker.BakeInput lightBakerBakeInput = new();
            LightBaker.LightmapRequests lightBakerLightmapRequests = new();
            LightBaker.LightProbeRequests lightBakerlightProbeRequests = new();
            UnityEditor.LightBaking.InputExtraction.SourceMap map = new();
            bool result = UnityEditor.LightBaking.InputExtraction.ExtractFromScene(outputFolderPath, lightBakerBakeInput, lightBakerLightmapRequests, lightBakerlightProbeRequests, map);
            bakeInput = new BakeInput(lightBakerBakeInput)
            {
                lightmapRequests = lightBakerLightmapRequests,
                lightProbeRequests = lightBakerlightProbeRequests
            };

            return result;
        }

        public static bool PopulateWorld(BakeInput bakeInput, BakeProgressState progress, IDeviceContext context, IWorld world)
        {
            LightBaker.Result result = LightBaker.PopulateWorld(bakeInput.bakeInput, bakeInput.lightmapRequests, bakeInput.lightProbeRequests, progress, context, world);
            return result.type == LightBaker.ResultType.Success;
        }

        // Note that in the non-public API, serialization of bake input does not imply serialization of requests.
        internal static bool SerializeBakeInput(string path, InputExtraction.BakeInput bakeInput) => LightBaker.Serialize(path, bakeInput.bakeInput);

        // Note that in the non-public API, deserialization of bake input does not imply deserialization of requests.
        internal static bool DeserializeBakeInput(string path, out InputExtraction.BakeInput bakeInput)
        {
            UnityEditor.LightBaking.LightBaker.BakeInput lightBakerBakeInput = new();
            bakeInput = new BakeInput(lightBakerBakeInput);
            return LightBaker.Deserialize(path, bakeInput.bakeInput);
        }

        public static int[] ComputeOcclusionLightIndicesFromBakeInput(BakeInput bakeInput, Vector3[] probePositions, uint maxLightsPerProbe = 4)
        {
            return UnityEditor.LightBaking.InputExtraction.ComputeOcclusionLightIndicesFromBakeInput(bakeInput.bakeInput, probePositions, maxLightsPerProbe);
        }

        public static int[] GetShadowmaskChannelsFromLightIndices(BakeInput bakeInput, int[] lightIndices)
        {
            return UnityEditor.LightBaking.InputExtraction.GetShadowmaskChannelsFromLightIndices(bakeInput.bakeInput, lightIndices);
        }
    }
}
