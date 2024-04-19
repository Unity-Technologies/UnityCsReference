// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.LightBaking;
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
            bool ignoreDirectEnvironment, BufferSlice<SphericalHarmonicsL2> radianceEstimateOut);
        public Result IntegrateIndirectRadiance(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            bool ignoreIndirectEnvironment, BufferSlice<SphericalHarmonicsL2> radianceEstimateOut);
        public Result IntegrateValidity(IDeviceContext context, int positionOffset, int positionCount, int sampleCount, BufferSlice<float> validityEstimateOut);
        public Result IntegrateOcclusion(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            int maxLightsPerProbe, BufferSlice<int> perProbeLightIndices, BufferSlice<float> probeOcclusionEstimateOut);
    }
    internal class WintermuteProbeIntegrator : IProbeIntegrator
    {
        private IntegrationContext _integrationContext;
        private BufferSlice<Vector3> _positions;
        private float _pushoff;
        private int _bounceCount;
        private BakeProgressState _progress;
        private const int SizeOfFloat = 4;
        private const int SHL2RGBElements = 3 * 9;
        private const int SizeOfSHL2 = SizeOfFloat * SHL2RGBElements;
        private const int SizeOfVector3 = SizeOfFloat * 3;

        public void Prepare(IDeviceContext context, IWorld world, BufferSlice<Vector3> positions, float pushoff, int bounceCount)
        {
            Debug.Assert(world is WintermuteWorld);
            var wmWorld = world as WintermuteWorld;
            _integrationContext = wmWorld.GetIntegrationContext();
            _positions = positions;
            _pushoff = pushoff;
            _bounceCount = bounceCount;
        }
        public void SetProgressReporter(BakeProgressState progress)
        {
            _progress = progress;
        }
        public unsafe IProbeIntegrator.Result IntegrateDirectRadiance(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            bool ignoreDirectEnvironment, BufferSlice<SphericalHarmonicsL2> radianceEstimateOut)
        {
            Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
            var wmContext = context as WintermuteContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            BufferSlice<Vector3> offsetPositions = _positions;
            offsetPositions.Offset += (ulong)positionOffset;
            EventID eventId = context.CreateEvent();
            context.ReadBuffer(offsetPositions, positions, eventId);
            bool waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            context.DestroyEvent(eventId);
            var positionsPtr = (Vector3*)positions.GetUnsafePtr();
            using var radianceBuffer = new NativeArray<Rendering.SphericalHarmonicsL2>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(radianceBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = 0;
            int envSampleCount = 0;
            const bool ignoreIndirectEnvironment = true;
            var lightBakerResult = LightBaker.IntegrateProbeDirectRadianceWintermute(positionsPtr, _integrationContext, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, ignoreDirectEnvironment, ignoreIndirectEnvironment, wmContext, _progress, shPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            if (lightBakerResult.type != LightBaker.ResultType.Success)
                return lightBakerResult.ConvertToIProbeIntegratorResult();

            eventId = context.CreateEvent();
            context.WriteBuffer(radianceEstimateOut, radianceBuffer, eventId);
            waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to write radiance to context.");
            context.DestroyEvent(eventId);
            if (!waitResult)
                lightBakerResult = new LightBaker.Result {type = LightBaker.ResultType.IOFailed, message = "Failed to write radiance to context."};

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }
        public unsafe IProbeIntegrator.Result IntegrateIndirectRadiance(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, bool ignoreIndirectEnvironment,
            BufferSlice<SphericalHarmonicsL2> radianceEstimateOut)
        {
            Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
            var wmContext = context as WintermuteContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            BufferSlice<Vector3> offsetPositions = _positions;
            offsetPositions.Offset += (ulong)positionOffset;
            EventID eventId = context.CreateEvent();
            context.ReadBuffer(offsetPositions, positions, eventId);
            bool waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            context.DestroyEvent(eventId);
            var positionsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            using var radianceBuffer = new NativeArray<Rendering.SphericalHarmonicsL2>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(radianceBuffer);
            int directSampleCount = 0;
            const bool ignoreDirectEnvironment = false;
            int giSampleCount = sampleCount;
            int envSampleCount = ignoreIndirectEnvironment ? 0 : sampleCount;
            var lightBakerResult = LightBaker.IntegrateProbeIndirectRadianceWintermute(positionsPtr, _integrationContext, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, ignoreDirectEnvironment, ignoreIndirectEnvironment, wmContext, _progress, shPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            if (lightBakerResult.type != LightBaker.ResultType.Success)
                return lightBakerResult.ConvertToIProbeIntegratorResult();

            eventId = context.CreateEvent();
            context.WriteBuffer(radianceEstimateOut, radianceBuffer, eventId);
            waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to write radiance to context.");
            context.DestroyEvent(eventId);
            if (!waitResult)
                lightBakerResult = new LightBaker.Result {type = LightBaker.ResultType.IOFailed, message = "Failed to write radiance to context."};

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }
        public unsafe IProbeIntegrator.Result IntegrateValidity(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, BufferSlice<float> validityEstimateOut)
        {
            Debug.Assert(context is WintermuteContext, "Expected RadeonRaysContext but got something else.");
            var wmContext = context as WintermuteContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            BufferSlice<Vector3> offsetPositions = _positions;
            offsetPositions.Offset += (ulong)positionOffset;
            EventID eventId = context.CreateEvent();
            context.ReadBuffer(offsetPositions, positions, eventId);
            bool waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            context.DestroyEvent(eventId);
            void* positionsPtr = NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            using var validityBuffer = new NativeArray<float>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            void* validityPtr = NativeArrayUnsafeUtility.GetUnsafePtr(validityBuffer);
            int directSampleCount = 0;
            int giSampleCount = sampleCount;
            int envSampleCount = 0;
            var lightBakerResult = LightBaker.IntegrateProbeValidityWintermute(positionsPtr, _integrationContext, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, wmContext, _progress, validityPtr);
            
            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            if (lightBakerResult.type != LightBaker.ResultType.Success)
                return lightBakerResult.ConvertToIProbeIntegratorResult();

            eventId = context.CreateEvent();
            context.WriteBuffer(validityEstimateOut, validityBuffer, eventId);
            waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to write validity to context.");
            context.DestroyEvent(eventId);
            if (!waitResult)
                lightBakerResult = new LightBaker.Result {type = LightBaker.ResultType.IOFailed, message = "Failed to write validity to context."};

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }

        public unsafe IProbeIntegrator.Result IntegrateOcclusion(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            int maxLightsPerProbe, BufferSlice<int> perProbeLightIndices, BufferSlice<float> probeOcclusionEstimateOut)
        {
            Debug.Assert(context is WintermuteContext, "Expected RadeonRaysContext but got something else.");
            var wmContext = context as WintermuteContext;
            Debug.Assert(maxLightsPerProbe == 4, "WintermuteProbeIntegrator only supports 4 light per probe.");
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            BufferSlice<Vector3> offsetPositions = _positions;
            offsetPositions.Offset += (ulong)positionOffset;
            EventID eventId = context.CreateEvent();
            context.ReadBuffer(offsetPositions, positions, eventId);
            bool waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            context.DestroyEvent(eventId);
            void* positionsPtr = NativeArrayUnsafeUtility.GetUnsafePtr(positions);

            using var perProbeLightIndicesArray = new NativeArray<int>(positionCount * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            eventId = context.CreateEvent();
            context.ReadBuffer(perProbeLightIndices, perProbeLightIndicesArray, eventId);
            waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to read indices from context.");
            context.DestroyEvent(eventId);
            void* perProbeLightIndicesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(perProbeLightIndicesArray);

            using var occlusionBuffer = new NativeArray<float>(positionCount * maxLightsPerProbe, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            void* occlusionPtr = NativeArrayUnsafeUtility.GetUnsafePtr(occlusionBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = 0;
            int envSampleCount = 0;
            var lightBakerResult = LightBaker.IntegrateProbeOcclusionWintermute(positionsPtr, perProbeLightIndicesPtr, positionCount,
                _pushoff, _bounceCount, directSampleCount, giSampleCount, envSampleCount, wmContext, _progress, occlusionPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            if (lightBakerResult.type != LightBaker.ResultType.Success)
                return lightBakerResult.ConvertToIProbeIntegratorResult();

            eventId = context.CreateEvent();
            context.WriteBuffer(probeOcclusionEstimateOut, occlusionBuffer, eventId);
            waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to write validity to context.");
            context.DestroyEvent(eventId);
            if (!waitResult)
                lightBakerResult = new LightBaker.Result { type = LightBaker.ResultType.IOFailed, message = "Failed to write validity to context." };

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }

        public void Dispose()
        {
        }
    }
    public class RadeonRaysProbeIntegrator : IProbeIntegrator
    {
        private IntegrationContext _integrationContext;
        private BufferSlice<Vector3> _positions;
        private float _pushoff;
        private int _bounceCount;
        BakeProgressState _progress = null;
        const int SizeOfFloat = 4;
        const int SHL2RGBElements = 3 * 9;
        const int SizeOfSHL2 = SizeOfFloat * SHL2RGBElements;
        public void Prepare(IDeviceContext context, IWorld world, BufferSlice<Vector3> positions, float pushoff, int bounceCount)
        {
            Debug.Assert(world is RadeonRaysWorld);
            var rrWorld = world as RadeonRaysWorld;
            _integrationContext = rrWorld.GetIntegrationContext();
            _positions = positions;
            _pushoff = pushoff;
            _bounceCount = bounceCount;
        }
        public void SetProgressReporter(BakeProgressState progress)
        {
            _progress = progress;
        }
        public unsafe IProbeIntegrator.Result IntegrateDirectRadiance(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            bool ignoreDirectEnvironment, BufferSlice<SphericalHarmonicsL2> radianceEstimateOut)
        {
            Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
            var rrContext = context as RadeonRaysContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            BufferSlice<Vector3> offsetPositions = _positions;
            offsetPositions.Offset += (ulong)positionOffset;
            EventID eventId = context.CreateEvent();
            context.ReadBuffer(offsetPositions, positions, eventId);
            bool waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            context.DestroyEvent(eventId);
            UnityEngine.Vector3* positionsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            using var radianceBuffer = new NativeArray<Rendering.SphericalHarmonicsL2>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(radianceBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = 0;
            int envSampleCount = 0;
            const bool ignoreIndirectEnvironment = true;
            var lightBakerResult = LightBaker.IntegrateProbeDirectRadianceRadeonRays(positionsPtr, _integrationContext, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, ignoreDirectEnvironment, ignoreIndirectEnvironment, rrContext, _progress, shPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            if (lightBakerResult.type != LightBaker.ResultType.Success)
                return lightBakerResult.ConvertToIProbeIntegratorResult();

            eventId = context.CreateEvent();
            context.WriteBuffer(radianceEstimateOut, radianceBuffer, eventId);
            waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to write radiance to context.");
            context.DestroyEvent(eventId);
            if (!waitResult)
                lightBakerResult = new LightBaker.Result {type = LightBaker.ResultType.IOFailed, message = "Failed to write radiance to context."};

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }

        public unsafe IProbeIntegrator.Result IntegrateIndirectRadiance(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            bool ignoreIndirectEnvironment, BufferSlice<SphericalHarmonicsL2> radianceEstimateOut)
        {
            Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
            var rrContext = context as RadeonRaysContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            BufferSlice<Vector3> offsetPositions = _positions;
            offsetPositions.Offset += (ulong)positionOffset;
            EventID eventId = context.CreateEvent();
            context.ReadBuffer(offsetPositions, positions, eventId);
            bool waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            context.DestroyEvent(eventId);
            UnityEngine.Vector3* positionsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            using var radianceBuffer = new NativeArray<Rendering.SphericalHarmonicsL2>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(radianceBuffer);
            int directSampleCount = 0;
            const bool ignoreDirectEnvironment = false;
            int giSampleCount = sampleCount;
            int envSampleCount = ignoreIndirectEnvironment ? 0 : sampleCount;
            var lightBakerResult = LightBaker.IntegrateProbeIndirectRadianceRadeonRays(positionsPtr, _integrationContext, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, ignoreDirectEnvironment, ignoreIndirectEnvironment, rrContext, _progress, shPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            if (lightBakerResult.type != LightBaker.ResultType.Success)
                return lightBakerResult.ConvertToIProbeIntegratorResult();

            eventId = context.CreateEvent();
            context.WriteBuffer(radianceEstimateOut, radianceBuffer, eventId);
            waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to write radiance to context.");
            context.DestroyEvent(eventId);
            if (!waitResult)
                lightBakerResult = new LightBaker.Result {type = LightBaker.ResultType.IOFailed, message = "Failed to write radiance to context."};

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }
        public unsafe IProbeIntegrator.Result IntegrateValidity(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, BufferSlice<float> validityEstimateOut)
        {
            Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
            var rrContext = context as RadeonRaysContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            BufferSlice<Vector3> offsetPositions = _positions;
            offsetPositions.Offset += (ulong)positionOffset;
            EventID eventId = context.CreateEvent();
            context.ReadBuffer(offsetPositions, positions, eventId);
            bool waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            context.DestroyEvent(eventId);
            void* positionsPtr = NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            using var validityBuffer = new NativeArray<float>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            void* validityPtr = NativeArrayUnsafeUtility.GetUnsafePtr(validityBuffer);
            int directSampleCount = 0;
            int giSampleCount = sampleCount;
            int envSampleCount = 0;
            var lightBakerResult = LightBaker.IntegrateProbeValidityRadeonRays(positionsPtr, _integrationContext, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, rrContext, _progress, validityPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            if (lightBakerResult.type != LightBaker.ResultType.Success)
                return lightBakerResult.ConvertToIProbeIntegratorResult();

            eventId = context.CreateEvent();
            context.WriteBuffer(validityEstimateOut, validityBuffer, eventId);
            waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to write validity to context.");
            context.DestroyEvent(eventId);
            if (!waitResult)
                lightBakerResult = new LightBaker.Result {type = LightBaker.ResultType.IOFailed, message = "Failed to write validity to context."};

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }

        public unsafe IProbeIntegrator.Result IntegrateOcclusion(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            int maxLightsPerProbe, BufferSlice<int> perProbeLightIndices, BufferSlice<float> probeOcclusionEstimateOut)
        {
            Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
            var rrContext = context as RadeonRaysContext;
            Debug.Assert(maxLightsPerProbe == 4, "RadeonRaysProbeIntegrator only supports 4 light per probe.");
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            BufferSlice<Vector3> offsetPositions = _positions;
            offsetPositions.Offset += (ulong)positionOffset;
            EventID eventId = context.CreateEvent();
            context.ReadBuffer(offsetPositions, positions, eventId);
            bool waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            context.DestroyEvent(eventId);
            void* positionsPtr = NativeArrayUnsafeUtility.GetUnsafePtr(positions);

            using var perProbeLightIndicesArray = new NativeArray<int>(positionCount * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            eventId = context.CreateEvent();
            context.ReadBuffer(perProbeLightIndices, perProbeLightIndicesArray, eventId);
            waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to read indices from context.");
            context.DestroyEvent(eventId);
            void* perProbeLightIndicesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(perProbeLightIndicesArray);

            using var occlusionBuffer = new NativeArray<float>(positionCount * maxLightsPerProbe, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            void* occlusionPtr = NativeArrayUnsafeUtility.GetUnsafePtr(occlusionBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = 0;
            int envSampleCount = 0;
            var lightBakerResult = LightBaker.IntegrateProbeOcclusionRadeonRays(positionsPtr, perProbeLightIndicesPtr, positionCount,
                _pushoff, _bounceCount, directSampleCount, giSampleCount, envSampleCount, rrContext, _progress, occlusionPtr);

            if (lightBakerResult.type != LightBaker.ResultType.Success)
                return lightBakerResult.ConvertToIProbeIntegratorResult();

            eventId = context.CreateEvent();
            context.WriteBuffer(probeOcclusionEstimateOut, occlusionBuffer, eventId);
            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            waitResult = context.Wait(eventId);
            Debug.Assert(waitResult, "Failed to write validity to context.");
            context.DestroyEvent(eventId);
            if (!waitResult)
                lightBakerResult = new LightBaker.Result { type = LightBaker.ResultType.IOFailed, message = "Failed to write validity to context." };

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }

        public void Dispose()
        {
        }
    }
}
