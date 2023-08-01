// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.LightBaking;
using UnityEngine.LightBaking;

namespace UnityEngine.LightTransport
{
    internal interface IProbeIntegrator
    {
        internal enum ResultType : UInt32
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
        internal struct Result
        {
            public Result(LightBaker.Result lightBakerResult)
            {
                type = (ResultType)lightBakerResult.type;
                message = lightBakerResult.message;
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
            context.ReadBuffer(_positions.Id, positions.Reinterpret<byte>(sizeOfVector3));
            UnityEngine.Vector3* positionsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            var radianceBuffer = new NativeArray<Rendering.SphericalHarmonicsL2>(positionCount, Allocator.TempJob);
            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(radianceBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = sampleCount;
            int envSampleCount = sampleCount;
            var result = LightBaker.IntegrateProbeDirectRadianceWintermute(positionsPtr, integrationContext, positionOffset, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, wmContext, _progress, shPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            context.WriteBuffer(radianceEstimateOut.Id, radianceBuffer.Reinterpret<byte>(sizeOfSHL2));

            return new IProbeIntegrator.Result(result);
        }
        public unsafe IProbeIntegrator.Result IntegrateIndirectRadiance(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, BufferSlice radianceEstimateOut)
        {
            Debug.Assert(context is WintermuteContext, "Expected WintermuteContext but got something else.");
            var wmContext = context as WintermuteContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.TempJob);
            context.ReadBuffer(_positions.Id, positions.Reinterpret<byte>(sizeOfVector3));
            UnityEngine.Vector3* positionsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            var radianceBuffer = new NativeArray<Rendering.SphericalHarmonicsL2>(positionCount, Allocator.TempJob);
            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(radianceBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = sampleCount;
            int envSampleCount = sampleCount;
            var result = LightBaker.IntegrateProbeIndirectRadianceWintermute(positionsPtr, integrationContext, positionOffset, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, wmContext, _progress, shPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            context.WriteBuffer(radianceEstimateOut.Id, radianceBuffer.Reinterpret<byte>(sizeOfSHL2));

            return new IProbeIntegrator.Result(result);
        }
        public unsafe IProbeIntegrator.Result IntegrateValidity(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, BufferSlice validityEstimateOut)
        {
            Debug.Assert(context is WintermuteContext, "Expected RadeonRaysContext but got something else.");
            var wmContext = context as WintermuteContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.TempJob);
            context.ReadBuffer(_positions.Id, positions.Reinterpret<byte>(sizeOfVector3));
            void* positionsPtr = NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            var validityBuffer = new NativeArray<float>(positionCount, Allocator.TempJob);
            void* validityPtr = NativeArrayUnsafeUtility.GetUnsafePtr(validityBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = sampleCount;
            int envSampleCount = sampleCount;
            var result = LightBaker.IntegrateProbeValidityWintermute(positionsPtr, integrationContext, positionOffset, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, wmContext, _progress, validityPtr);
            
            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            context.WriteBuffer(validityEstimateOut.Id, validityBuffer.Reinterpret<byte>(sizeOfFloat));

            return new IProbeIntegrator.Result(result);
        }
    }
    internal class RadeonRaysProbeIntegrator : IProbeIntegrator
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
            context.ReadBuffer(_positions.Id, positions.Reinterpret<byte>(sizeOfVector3));
            UnityEngine.Vector3* positionsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            var radianceBuffer = new NativeArray<Rendering.SphericalHarmonicsL2>(positionCount, Allocator.TempJob);
            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(radianceBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = sampleCount;
            int envSampleCount = sampleCount;
            var result = LightBaker.IntegrateProbeDirectRadianceRadeonRays(positionsPtr, integrationContext, positionOffset, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, rrContext, _progress, shPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            context.WriteBuffer(radianceEstimateOut.Id, radianceBuffer.Reinterpret<byte>(sizeOfSHL2));

            return new IProbeIntegrator.Result(result);
        }
        public unsafe IProbeIntegrator.Result IntegrateIndirectRadiance(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, BufferSlice radianceEstimateOut)
        {
            Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
            var rrContext = context as RadeonRaysContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.TempJob);
            context.ReadBuffer(_positions.Id, positions.Reinterpret<byte>(sizeOfVector3));
            UnityEngine.Vector3* positionsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            var radianceBuffer = new NativeArray<Rendering.SphericalHarmonicsL2>(positionCount, Allocator.TempJob);
            void* shPtr = NativeArrayUnsafeUtility.GetUnsafePtr(radianceBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = sampleCount;
            int envSampleCount = sampleCount;
            var result = LightBaker.IntegrateProbeIndirectRadianceRadeonRays(positionsPtr, integrationContext, positionOffset, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, rrContext, _progress, shPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            context.WriteBuffer(radianceEstimateOut.Id, radianceBuffer.Reinterpret<byte>(sizeOfSHL2));

            return new IProbeIntegrator.Result(result);
        }
        public unsafe IProbeIntegrator.Result IntegrateValidity(IDeviceContext context,
            int positionOffset, int positionCount, int sampleCount, BufferSlice validityEstimateOut)
        {
            Debug.Assert(context is RadeonRaysContext, "Expected RadeonRaysContext but got something else.");
            var rrContext = context as RadeonRaysContext;
            using var positions = new NativeArray<Vector3>(positionCount, Allocator.TempJob);
            context.ReadBuffer(_positions.Id, positions.Reinterpret<byte>(sizeOfVector3));
            void* positionsPtr = NativeArrayUnsafeUtility.GetUnsafePtr(positions);
            var validityBuffer = new NativeArray<float>(positionCount, Allocator.TempJob);
            void* validityPtr = NativeArrayUnsafeUtility.GetUnsafePtr(validityBuffer);
            int directSampleCount = sampleCount;
            int giSampleCount = sampleCount;
            int envSampleCount = sampleCount;
            var result = LightBaker.IntegrateProbeValidityRadeonRays(positionsPtr, integrationContext, positionOffset, positionCount, _pushoff,
                _bounceCount, directSampleCount, giSampleCount, envSampleCount, rrContext, _progress, validityPtr);

            // TODO: Fix this in LIGHT-1479, synchronization and read-back should be done by the user.
            context.WriteBuffer(validityEstimateOut.Id, validityBuffer.Reinterpret<byte>(sizeOfFloat));

            return new IProbeIntegrator.Result(result);
        }
    }
}
