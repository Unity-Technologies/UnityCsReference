// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.ParticleSystemJobs;

namespace UnityEngine
{
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystem.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystemGeometryJob.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemScriptBindings.h")]
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    public sealed partial class ParticleSystem : Component
    {
        // Properties
        extern public bool isPlaying
        {
            [NativeName("SyncJobs(false)->IsPlaying")] get;
        }
        extern public bool isEmitting
        {
            [NativeName("SyncJobs(false)->IsEmitting")] get;
        }
        extern public bool isStopped
        {
            [NativeName("SyncJobs(false)->IsStopped")] get;
        }
        extern public bool isPaused
        {
            [NativeName("SyncJobs(false)->IsPaused")] get;
        }
        extern public int particleCount
        {
            [NativeName("SyncJobs(false)->GetParticleCount")] get;
        }

        extern public float time
        {
            [NativeName("SyncJobs(false)->GetSecPosition")]
            get;
            [NativeName("SyncJobs(false)->SetSecPosition")]
            set;
        }

        extern public UInt32 randomSeed
        {
            [NativeName("GetRandomSeed")]
            get;
            [NativeName("SyncJobs(false)->SetRandomSeed")]
            set;
        }

        extern public bool useAutoRandomSeed
        {
            [NativeName("GetAutoRandomSeed")]
            get;
            [NativeName("SyncJobs(false)->SetAutoRandomSeed")]
            set;
        }

        extern public bool proceduralSimulationSupported
        {
            get;
        }

        // Current size/color helpers
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticleCurrentSize", HasExplicitThis = true)]
        extern internal float GetParticleCurrentSize(ref Particle particle);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticleCurrentSize3D", HasExplicitThis = true)]
        extern internal Vector3 GetParticleCurrentSize3D(ref Particle particle);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticleCurrentColor", HasExplicitThis = true)]
        extern internal Color32 GetParticleCurrentColor(ref Particle particle);

        // Mesh index helper
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticleMeshIndex", HasExplicitThis = true)]
        extern internal int GetParticleMeshIndex(ref Particle particle);

        // Set/get particles
        [FreeFunction(Name = "ParticleSystemScriptBindings::SetParticles", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetParticles([Out] Particle[] particles, int size, int offset);
        public void SetParticles([Out] Particle[] particles, int size) { SetParticles(particles, size, 0); }
        public void SetParticles([Out] Particle[] particles) { SetParticles(particles, -1); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::SetParticlesWithNativeArray", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetParticlesWithNativeArray(IntPtr particles, int particlesLength, int size, int offset);
        unsafe public void SetParticles([Out] NativeArray<Particle> particles, int size, int offset) { SetParticlesWithNativeArray((IntPtr)particles.GetUnsafeReadOnlyPtr(), particles.Length, size, 0); }
        public void SetParticles([Out] NativeArray<Particle> particles, int size) { SetParticles(particles, size, 0); }
        public void SetParticles([Out] NativeArray<Particle> particles) { SetParticles(particles, -1); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticles", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetParticles([NotNull][Out] Particle[] particles, int size, int offset);
        public int GetParticles([Out] Particle[] particles, int size) { return GetParticles(particles, size, 0); }
        public int GetParticles([Out] Particle[] particles) { return GetParticles(particles, -1); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetParticlesWithNativeArray", HasExplicitThis = true, ThrowsException = true)]
        extern private int GetParticlesWithNativeArray(IntPtr particles, int particlesLength, int size, int offset);
        unsafe public int GetParticles([Out] NativeArray<Particle> particles, int size, int offset) { return GetParticlesWithNativeArray((IntPtr)particles.GetUnsafeReadOnlyPtr(), particles.Length, size, 0); }
        public int GetParticles([Out] NativeArray<Particle> particles, int size) { return GetParticles(particles, size, 0); }
        public int GetParticles([Out] NativeArray<Particle> particles) { return GetParticles(particles, -1); }

        // Set/get custom particle data
        [FreeFunction(Name = "ParticleSystemScriptBindings::SetCustomParticleData", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetCustomParticleData([NotNull] List<Vector4> customData, ParticleSystemCustomData streamIndex);
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetCustomParticleData", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetCustomParticleData([NotNull] List<Vector4> customData, ParticleSystemCustomData streamIndex);

        // Set/get the playback state
        extern public PlaybackState GetPlaybackState();
        extern public void SetPlaybackState(PlaybackState playbackState);

        // Set/get the trail data
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetTrailData", HasExplicitThis = true)]
        extern private void GetTrailDataInternal(ref Trails trailData);
        public Trails GetTrails()
        {
            var result = new Trails() { positions = new List<Vector4>(), frontPositions = new List<int>(), backPositions = new List<int>(), positionCounts = new List<int>() };
            GetTrailDataInternal(ref result);
            return result;
        }

        [FreeFunction(Name = "ParticleSystemScriptBindings::SetTrailData", HasExplicitThis = true)]
        extern public void SetTrails(Trails trailData);

        // Playback
        [FreeFunction(Name = "ParticleSystemScriptBindings::Simulate", HasExplicitThis = true)]
        extern public void Simulate(float t, bool withChildren, bool restart, bool fixedTimeStep);
        public void Simulate(float t, bool withChildren, bool restart) { Simulate(t, withChildren, restart, true); }
        public void Simulate(float t, bool withChildren) { Simulate(t, withChildren, true); }
        public void Simulate(float t) { Simulate(t, true); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::Play", HasExplicitThis = true)]
        extern public void Play(bool withChildren);
        public void Play() { Play(true);  }

        [FreeFunction(Name = "ParticleSystemScriptBindings::Pause", HasExplicitThis = true)]
        extern public void Pause(bool withChildren);
        public void Pause() { Pause(true); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::Stop", HasExplicitThis = true)]
        extern public void Stop(bool withChildren, ParticleSystemStopBehavior stopBehavior);
        public void Stop(bool withChildren) { Stop(withChildren, ParticleSystemStopBehavior.StopEmitting); }
        public void Stop() { Stop(true); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::Clear", HasExplicitThis = true)]
        extern public void Clear(bool withChildren);
        public void Clear() { Clear(true); }

        [FreeFunction(Name = "ParticleSystemScriptBindings::IsAlive", HasExplicitThis = true)]
        extern public bool IsAlive(bool withChildren);
        public bool IsAlive() { return IsAlive(true); }

        // Emission
        [RequiredByNativeCode]
        public void Emit(int count) { Emit_Internal(count); }
        [NativeName("SyncJobs()->Emit")]
        extern private void Emit_Internal(int count);

        [NativeName("SyncJobs()->EmitParticlesExternal")]
        extern public void Emit(EmitParams emitParams, int count);

        [NativeName("SyncJobs()->EmitParticleExternal")]
        extern private void EmitOld_Internal(ref ParticleSystem.Particle particle);

        // Fire a sub-emitter
        public void TriggerSubEmitter(int subEmitterIndex)
        {
            TriggerSubEmitter(subEmitterIndex, null);
        }

        public void TriggerSubEmitter(int subEmitterIndex, ref ParticleSystem.Particle particle)
        {
            TriggerSubEmitterForParticle(subEmitterIndex, particle);
        }

        [FreeFunction(Name = "ParticleSystemScriptBindings::TriggerSubEmitterForParticle", HasExplicitThis = true)]
        extern internal void TriggerSubEmitterForParticle(int subEmitterIndex, ParticleSystem.Particle particle);

        [FreeFunction(Name = "ParticleSystemScriptBindings::TriggerSubEmitter", HasExplicitThis = true)]
        extern public void TriggerSubEmitter(int subEmitterIndex, List<ParticleSystem.Particle> particles);

        [FreeFunction(Name = "ParticleSystemGeometryJob::ResetPreMappedBufferMemory")]
        extern public static void ResetPreMappedBufferMemory();

        unsafe extern internal void* GetManagedJobData();
        extern internal JobHandle GetManagedJobHandle();
        extern internal void SetManagedJobHandle(JobHandle handle);
        [FreeFunction("ScheduleManagedJob", ThrowsException = true)]
        unsafe internal static extern JobHandle ScheduleManagedJob(ref JobsUtility.JobScheduleParameters parameters, void* additionalData);
        [ThreadSafe]
        unsafe internal static extern void CopyManagedJobData(void* systemPtr, out NativeParticleData particleData);


        [FreeFunction(Name = "ParticleSystemEditor::SetupDefaultParticleSystemType", HasExplicitThis = true)]
        extern internal void SetupDefaultType(ParticleSystemSubEmitterType type);

        [NativeProperty("GetState().localToWorld", TargetType = TargetType.Field)]
        extern internal Matrix4x4 localToWorldMatrix { get; }

        [NativeName("GetNoiseModule().GeneratePreviewTexture")]
        extern internal void GenerateNoisePreviewTexture(Texture2D dst);

        extern internal void CalculateEffectUIData(ref int particleCount, ref float fastestParticle, ref float slowestParticle);

        extern internal int GenerateRandomSeed();

        [FreeFunction(Name = "ParticleSystemScriptBindings::CalculateEffectUISubEmitterData", HasExplicitThis = true)]
        extern internal bool CalculateEffectUISubEmitterData(ref int particleCount, ref float fastestParticle, ref float slowestParticle);

        [FreeFunction(Name = "ParticleSystemScriptBindings::CheckVertexStreamsMatchShader")]
        extern internal static bool CheckVertexStreamsMatchShader(bool hasTangent, bool hasColor, int texCoordChannelCount, Material material, ref bool tangentError, ref bool colorError, ref bool uvError);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetMaxTexCoordStreams")]
        extern internal static int GetMaxTexCoordStreams();

    }

    public partial struct ParticleCollisionEvent
    {
        [FreeFunction(Name = "ParticleSystemScriptBindings::InstanceIDToColliderComponent")]
        extern static private Component InstanceIDToColliderComponent(int instanceID);
    }

    internal class ParticleSystemExtensionsImpl
    {
        [FreeFunction(Name = "ParticleSystemScriptBindings::GetSafeCollisionEventSize")]
        extern internal static int GetSafeCollisionEventSize([NotNull] ParticleSystem ps);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetCollisionEventsDeprecated")]
        extern internal static int GetCollisionEventsDeprecated([NotNull] ParticleSystem ps, GameObject go, [Out] ParticleCollisionEvent[] collisionEvents);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetSafeTriggerParticlesSize")]
        extern internal static int GetSafeTriggerParticlesSize([NotNull] ParticleSystem ps, int type);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetCollisionEvents")]
        extern internal static int GetCollisionEvents([NotNull] ParticleSystem ps, [NotNull] GameObject go, [NotNull] List<ParticleCollisionEvent> collisionEvents);

        [FreeFunction(Name = "ParticleSystemScriptBindings::GetTriggerParticles")]
        extern internal static int GetTriggerParticles([NotNull] ParticleSystem ps, int type, [NotNull] List<ParticleSystem.Particle> particles);

        [FreeFunction(Name = "ParticleSystemScriptBindings::SetTriggerParticles")]
        extern internal static void SetTriggerParticles([NotNull] ParticleSystem ps, int type, [NotNull] List<ParticleSystem.Particle> particles, int offset, int count);
    }

    public static partial class ParticlePhysicsExtensions
    {
        public static int GetSafeCollisionEventSize(this ParticleSystem ps)
        {
            return ParticleSystemExtensionsImpl.GetSafeCollisionEventSize(ps);
        }

        public static int GetCollisionEvents(this ParticleSystem ps, GameObject go, List<ParticleCollisionEvent> collisionEvents)
        {
            return ParticleSystemExtensionsImpl.GetCollisionEvents(ps, go, collisionEvents);
        }

        public static int GetSafeTriggerParticlesSize(this ParticleSystem ps, ParticleSystemTriggerEventType type)
        {
            return ParticleSystemExtensionsImpl.GetSafeTriggerParticlesSize(ps, (int)type);
        }

        public static int GetTriggerParticles(this ParticleSystem ps, ParticleSystemTriggerEventType type, List<ParticleSystem.Particle> particles)
        {
            return ParticleSystemExtensionsImpl.GetTriggerParticles(ps, (int)type, particles);
        }

        public static void SetTriggerParticles(this ParticleSystem ps, ParticleSystemTriggerEventType type, List<ParticleSystem.Particle> particles, int offset, int count)
        {
            if (particles == null) throw new ArgumentNullException("particles");
            if (offset >= particles.Count) throw new ArgumentOutOfRangeException("offset", "offset should be smaller than the size of the particles list.");
            if ((offset + count) >= particles.Count) throw new ArgumentOutOfRangeException("count", "offset+count should be smaller than the size of the particles list.");

            ParticleSystemExtensionsImpl.SetTriggerParticles(ps, (int)type, particles, offset, count);
        }

        public static void SetTriggerParticles(this ParticleSystem ps, ParticleSystemTriggerEventType type, List<ParticleSystem.Particle> particles)
        {
            ParticleSystemExtensionsImpl.SetTriggerParticles(ps, (int)type, particles, 0, particles.Count);
        }
    }
}
