// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

namespace UnityEngine.LightTransport
{
    public interface IProbeIntegrator : IDisposable
    {
        public enum ResultType : uint
        {
            Success = 0,
            Cancelled,
            JobFailed,
            OutOfMemory,
            InvalidInput,
            LowLevelAPIFailure,
            IOFailed,
            Undefined
        }
        public struct Result
        {
            public Result(ResultType _type, String _message)
            {
                type = _type;
                message = _message;
            }
            public ResultType type;
            public String message;
            public override string ToString()
            {
                if (message.Length == 0)
                    return $"Result type: '{type}'";
                else
                    return $"Result type: '{type}', message: '{message}'";
            }
        }
        public void Prepare(IDeviceContext context, IWorld world, BufferSlice<Vector3> positions, float pushoff, int bounceCount);
        public void SetProgressReporter(BakeProgressState progress);
        public Result IntegrateDirectRadiance(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            bool ignoreEnvironment, BufferSlice<SphericalHarmonicsL2> radianceEstimateOut);
        public Result IntegrateIndirectRadiance(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            bool ignoreEnvironment, BufferSlice<SphericalHarmonicsL2> radianceEstimateOut);
        public Result IntegrateValidity(IDeviceContext context, int positionOffset, int positionCount, int sampleCount, BufferSlice<float> validityEstimateOut);
        public Result IntegrateOcclusion(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            int maxLightsPerProbe, BufferSlice<int> perProbeLightIndices, BufferSlice<float> probeOcclusionEstimateOut);
    }
}
