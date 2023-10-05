// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.LightBaking;

namespace UnityEngine.LightTransport
{
    public interface IProbeIntegrator
    {
        public enum ResultType : UInt32
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
        public void Prepare(IWorld world, BufferSlice positions, float pushoff, int bounceCount);
        public void SetProgressReporter(BakeProgressState progress);
        public Result IntegrateDirectRadiance(IDeviceContext context, int positionOffset, int positionCount, int sampleCount, BufferSlice radianceEstimateOut);
        public Result IntegrateIndirectRadiance(IDeviceContext context, int positionOffset, int positionCount, int sampleCount, BufferSlice radianceEstimateOut);
        public Result IntegrateValidity(IDeviceContext context, int positionOffset, int positionCount, int sampleCount, BufferSlice validityEstimateOut);
    }
    internal class WintermuteProbeIntegrator : IProbeIntegrator
    {
        private IntegrationContext integrationContext;
        private BufferSlice _positions;
        private float _pushoff;
        private int _bounceCount;
        BakeProgressState _progress = null;
        const int sizeOfFloat = 4;
        const int SHL2RGBElements = 3 * 9;
        const int sizeOfSHL2 = sizeOfFloat * SHL2RGBElements;
        const int sizeOfVector3 = sizeOfFloat * 3;
        public void Prepare(IWorld world, BufferSlice positions, float pushoff, int bounceCount)
        {
            Debug.Assert(world is WintermuteWorld);
            var wmWorld = world as WintermuteWorld;
            integrationContext = wmWorld.GetIntegrationContext();
            _positions = positions;
            _pushoff = pushoff;
            _bounceCount = bounceCount;
        }
        public void SetProgressReporter(BakeProgressState progress)
        {
            _progress = progress;
        }
        public unsafe IProbeIntegrator.Result IntegrateDirectRadiance(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, BufferSlice radianceEstimateOut)
        {
            Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
            var wmContext = context as WintermuteContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.TempJob);
            EventID eventId = context.ReadBuffer(_positions.Id, positions.Reinterpret<byte>(sizeOfVector3));
            bool waitResult = context.WaitForAsyncOperation(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            UnityEngine.Vector3* positionsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            var radianceBuffer = new NativeArray<Rendering.SphericalHarmonicsL2>(positionCount, Allocator.TempJob);
            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(radianceBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = sampleCount;
            int envSampleCount = sampleCount;
            var lightBakerResult = LightBaker.IntegrateProbeDirectRadianceWintermute(positionsPtr, integrationContext, positionOffset, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, wmContext, _progress, shPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            eventId = context.WriteBuffer(radianceEstimateOut.Id, radianceBuffer.Reinterpret<byte>(sizeOfSHL2));
            waitResult = context.WaitForAsyncOperation(eventId);
            Debug.Assert(waitResult, "Failed to write radiance to context.");

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }
        public unsafe IProbeIntegrator.Result IntegrateIndirectRadiance(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, BufferSlice radianceEstimateOut)
        {
            Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
            var wmContext = context as WintermuteContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.TempJob);
            EventID eventId = context.ReadBuffer(_positions.Id, positions.Reinterpret<byte>(sizeOfVector3));
            bool waitResult = context.WaitForAsyncOperation(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            UnityEngine.Vector3* positionsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            var radianceBuffer = new NativeArray<Rendering.SphericalHarmonicsL2>(positionCount, Allocator.TempJob);
            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(radianceBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = sampleCount;
            int envSampleCount = sampleCount;
            var lightBakerResult = LightBaker.IntegrateProbeIndirectRadianceWintermute(positionsPtr, integrationContext, positionOffset, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, wmContext, _progress, shPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            eventId = context.WriteBuffer(radianceEstimateOut.Id, radianceBuffer.Reinterpret<byte>(sizeOfSHL2));
            waitResult = context.WaitForAsyncOperation(eventId);
            Debug.Assert(waitResult, "Failed to write radiance to context.");

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }
        public unsafe IProbeIntegrator.Result IntegrateValidity(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, BufferSlice validityEstimateOut)
        {
            Debug.Assert(context is WintermuteContext, "Expected RadeonRaysContext but got something else.");
            var wmContext = context as WintermuteContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.TempJob);
            EventID eventId = context.ReadBuffer(_positions.Id, positions.Reinterpret<byte>(sizeOfVector3));
            bool waitResult = context.WaitForAsyncOperation(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            void* positionsPtr = NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            var validityBuffer = new NativeArray<float>(positionCount, Allocator.TempJob);
            void* validityPtr = NativeArrayUnsafeUtility.GetUnsafePtr(validityBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = sampleCount;
            int envSampleCount = sampleCount;
            var lightBakerResult = LightBaker.IntegrateProbeValidityWintermute(positionsPtr, integrationContext, positionOffset, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, wmContext, _progress, validityPtr);
            
            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            eventId = context.WriteBuffer(validityEstimateOut.Id, validityBuffer.Reinterpret<byte>(sizeOfFloat));
            waitResult = context.WaitForAsyncOperation(eventId);
            Debug.Assert(waitResult, "Failed to write radiance to context.");

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }
    }
    public class RadeonRaysProbeIntegrator : IProbeIntegrator
    {
        private IntegrationContext integrationContext;
        private BufferSlice _positions;
        private float _pushoff;
        private int _bounceCount;
        BakeProgressState _progress = null;
        const int sizeOfFloat = 4;
        const int SHL2RGBElements = 3 * 9;
        const int sizeOfSHL2 = sizeOfFloat * SHL2RGBElements;
        const int sizeOfVector3 = sizeOfFloat * 3;
        public void Prepare(IWorld world, BufferSlice positions, float pushoff, int bounceCount)
        {
            Debug.Assert(world is RadeonRaysWorld);
            var rrWorld = world as RadeonRaysWorld;
            integrationContext = rrWorld.GetIntegrationContext();
            _positions = positions;
            _pushoff = pushoff;
            _bounceCount = bounceCount;
        }
        public void SetProgressReporter(BakeProgressState progress)
        {
            _progress = progress;
        }
        public unsafe IProbeIntegrator.Result IntegrateDirectRadiance(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, BufferSlice radianceEstimateOut)
        {
            Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
            var rrContext = context as RadeonRaysContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.TempJob);
            EventID eventId = context.ReadBuffer(_positions.Id, positions.Reinterpret<byte>(sizeOfVector3));
            bool waitResult = context.WaitForAsyncOperation(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            UnityEngine.Vector3* positionsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            var radianceBuffer = new NativeArray<Rendering.SphericalHarmonicsL2>(positionCount, Allocator.TempJob);
            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(radianceBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = sampleCount;
            int envSampleCount = sampleCount;
            var lightBakerResult = LightBaker.IntegrateProbeDirectRadianceRadeonRays(positionsPtr, integrationContext, positionOffset, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, rrContext, _progress, shPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            eventId = context.WriteBuffer(radianceEstimateOut.Id, radianceBuffer.Reinterpret<byte>(sizeOfSHL2));
            waitResult = context.WaitForAsyncOperation(eventId);
            Debug.Assert(waitResult, "Failed to write radiance to context.");

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }
        public unsafe IProbeIntegrator.Result IntegrateIndirectRadiance(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, BufferSlice radianceEstimateOut)
        {
            Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
            var rrContext = context as RadeonRaysContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.TempJob);
            EventID eventId = context.ReadBuffer(_positions.Id, positions.Reinterpret<byte>(sizeOfVector3));
            bool waitResult = context.WaitForAsyncOperation(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            UnityEngine.Vector3* positionsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            var radianceBuffer = new NativeArray<Rendering.SphericalHarmonicsL2>(positionCount, Allocator.TempJob);
            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(radianceBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = sampleCount;
            int envSampleCount = sampleCount;
            var lightBakerResult = LightBaker.IntegrateProbeIndirectRadianceRadeonRays(positionsPtr, integrationContext, positionOffset, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, rrContext, _progress, shPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            eventId = context.WriteBuffer(radianceEstimateOut.Id, radianceBuffer.Reinterpret<byte>(sizeOfSHL2));
            waitResult = context.WaitForAsyncOperation(eventId);
            Debug.Assert(waitResult, "Failed to write radiance to context.");

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }
        public unsafe IProbeIntegrator.Result IntegrateValidity(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, BufferSlice validityEstimateOut)
        {
            Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
            var rrContext = context as RadeonRaysContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.TempJob);
            EventID eventId = context.ReadBuffer(_positions.Id, positions.Reinterpret<byte>(sizeOfVector3));
            bool waitResult = context.WaitForAsyncOperation(eventId);
            Debug.Assert(waitResult, "Failed to read positions from context.");
            void* positionsPtr = NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            var validityBuffer = new NativeArray<float>(positionCount, Allocator.TempJob);
            void* validityPtr = NativeArrayUnsafeUtility.GetUnsafePtr(validityBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = sampleCount;
            int envSampleCount = sampleCount;
            var lightBakerResult = LightBaker.IntegrateProbeValidityRadeonRays(positionsPtr, integrationContext, positionOffset, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, rrContext, _progress, validityPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            eventId = context.WriteBuffer(validityEstimateOut.Id, validityBuffer.Reinterpret<byte>(sizeOfFloat));
            waitResult = context.WaitForAsyncOperation(eventId);
            Debug.Assert(waitResult, "Failed to write validity to context.");

            return lightBakerResult.ConvertToIProbeIntegratorResult();
        }
    }
}
