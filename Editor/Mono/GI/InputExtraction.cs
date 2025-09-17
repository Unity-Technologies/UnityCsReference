// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.LightBaking;

namespace UnityEngine.LightTransport
{
    public enum ProbeBakeRequestOutput : uint
    {
        RadianceDirect = 1 << 0,
        RadianceIndirect = 1 << 1,
        Validity = 1 << 2,
        Occlusion = 1 << 3,
        All = 0xFFFFFFFF
    }

    public struct ProbeBakeRequest
    {
        public ProbeBakeRequestOutput outputTypes { get; set; }
        public ulong positionOffset { get; set; }
        public ulong positionLength { get; set; }
        public string bakeOutputFolderPath { get; set; }
        public string postProcessOutputFolderPath { get; set; }
        public bool ignoreDirectEnvironment { get; set; }
        public bool ignoreIndirectEnvironment { get; set; }
        public float pushoff { get; set; }
        public float indirectScale { get; set; }
        public bool dering { get; set; }
    }

    public static class InputExtraction
    {
        // Completely opaque in the public API, this effectively hides internal LightBaker details.
        public class BakeInput
        {
            internal BakeInput(UnityEditor.LightBaking.BakeInput editorBakeInput)
            {
                bakeInput = editorBakeInput;
            }
            internal UnityEditor.LightBaking.BakeInput bakeInput { get; }

            // Requests are not exposed in the public API, but we need them in LightBaker.PopulateWorld(). Going forward, requests will be split from the bake input in the public API as well.
            internal LightmapRequests lightmapRequests { get; set; }
            internal LightProbeRequests lightProbeRequests { get; set; }
            internal PostProcessRequests postProcessRequests { get; set; }

            public void SetProbePositions(Vector3[] probePositions)
            {
                lightProbeRequests.SetProbePositions(probePositions);
            }

            public void SetOcclusionLightIndices(int[] occlusionLightIndices)
            {
                lightProbeRequests.SetOcclusionLightIndices(occlusionLightIndices);
            }

            public Vector3[] GetProbePositions()
            {
                return lightProbeRequests.GetProbePositions();
            }

            public int[] GetOcclusionLightIndices()
            {
                return lightProbeRequests.GetOcclusionLightIndices();
            }

            public void AddProbeRequest(ProbeBakeRequest request)
            {
                static ProbeRequestOutputType ConvertOutputEnum(ProbeBakeRequestOutput val)
                {
                    var output = (ProbeRequestOutputType)0;
                    if (val.HasFlag(ProbeBakeRequestOutput.RadianceDirect))
                        output |= ProbeRequestOutputType.RadianceDirect;
                    if (val.HasFlag(ProbeBakeRequestOutput.RadianceIndirect))
                        output |= ProbeRequestOutputType.RadianceIndirect;
                    if (val.HasFlag(ProbeBakeRequestOutput.Validity))
                        output |= ProbeRequestOutputType.Validity;
                    if (val.HasFlag(ProbeBakeRequestOutput.Occlusion))
                    {
                        output |= ProbeRequestOutputType.LightProbeOcclusion;
                        output |= ProbeRequestOutputType.MixedLightOcclusion;
                    }
                    return output;
                }

                ProbeRequest bakeRequest = new()
                {
                    outputTypeMask = ConvertOutputEnum(request.outputTypes),
                    positionOffset = request.positionOffset,
                    positionLength = request.positionLength,
                    outputFolderPath = request.bakeOutputFolderPath,
                    ignoreDirectEnvironment = request.ignoreDirectEnvironment,
                    ignoreIndirectEnvironment = request.ignoreIndirectEnvironment,
                    pushoff = request.pushoff,
                };

                PostProcessProbeRequest postProcessRequest = new()
                {
                    dering = request.dering,
                    indirectScale = request.indirectScale,
                    outputFolderPath = request.postProcessOutputFolderPath
                };

                var requests = lightProbeRequests.GetProbeRequests();
                Array.Resize(ref requests, requests.Length + 1);
                requests[requests.Length - 1] = bakeRequest;
                lightProbeRequests.SetLightProbeRequests(requests);

                var ppRequests = postProcessRequests.GetProbeRequests();
                Array.Resize(ref ppRequests, ppRequests.Length + 1);
                ppRequests[ppRequests.Length - 1] = postProcessRequest;
                postProcessRequests.SetProbeRequests(ppRequests);
            }
        }

        public static bool ExtractFromScene(out BakeInput bakeInput)
        {
            const string outputFolderPath = "unused"; // We are not using disk IO.
            UnityEditor.LightBaking.BakeInput lightBakerBakeInput = new();
            LightmapRequests lightBakerLightmapRequests = new();
            LightProbeRequests lightBakerlightProbeRequests = new();
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
            Result result = LightBaker.PopulateWorld(bakeInput.bakeInput, bakeInput.lightmapRequests, bakeInput.lightProbeRequests, progress, context, world);
            return result.type == ResultType.Success;
        }

        // Note that in the non-public API, serialization of bake input does not imply serialization of requests.
        internal static bool SerializeBakeInput(string path, InputExtraction.BakeInput bakeInput) => LightBaker.Serialize(path, bakeInput.bakeInput);

        // Note that in the non-public API, deserialization of bake input does not imply deserialization of requests.
        internal static bool DeserializeBakeInput(string path, out InputExtraction.BakeInput bakeInput)
        {
            UnityEditor.LightBaking.BakeInput lightBakerBakeInput = new();
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
