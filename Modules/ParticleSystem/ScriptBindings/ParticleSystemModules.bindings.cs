// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine
{
    [NativeHeader("ParticleSystemScriptingClasses.h")]
    [NativeHeader("Modules/ParticleSystem/ParticleSystem.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemScriptBindings.h")]
    [NativeHeader("Modules/ParticleSystem/ScriptBindings/ParticleSystemModulesScriptBindings.h")]
    public partial class ParticleSystem : Component
    {
        // Modules
        public partial struct MainModule
        {
            internal MainModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public float duration { get; [NativeThrows] set; }
            extern public bool loop { get; [NativeThrows] set; }
            extern public bool prewarm { get; [NativeThrows] set; }
            extern public MinMaxCurve startDelay { get; [NativeThrows] set; }
            extern public float startDelayMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve startLifetime { get; [NativeThrows] set; }
            extern public float startLifetimeMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve startSpeed { get; [NativeThrows] set; }
            extern public float startSpeedMultiplier { get; [NativeThrows] set; }
            extern public bool startSize3D { get; [NativeThrows] set; }
            [NativeName("StartSizeX")]
            extern public MinMaxCurve startSize { get; [NativeThrows] set; }
            [NativeName("StartSizeXMultiplier")]
            extern public float startSizeMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve startSizeX { get; [NativeThrows] set; }
            extern public float startSizeXMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve startSizeY { get; [NativeThrows] set; }
            extern public float startSizeYMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve startSizeZ { get; [NativeThrows] set; }
            extern public float startSizeZMultiplier { get; [NativeThrows] set; }
            extern public bool startRotation3D { get; [NativeThrows] set; }
            [NativeName("StartRotationZ")]
            extern public MinMaxCurve startRotation { get; [NativeThrows] set; }
            [NativeName("StartRotationZMultiplier")]
            extern public float startRotationMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve startRotationX { get; [NativeThrows] set; }
            extern public float startRotationXMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve startRotationY { get; [NativeThrows] set; }
            extern public float startRotationYMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve startRotationZ { get; [NativeThrows] set; }
            extern public float startRotationZMultiplier { get; [NativeThrows] set; }
            extern public float flipRotation { get; [NativeThrows] set; }
            extern public MinMaxGradient startColor { get; [NativeThrows] set; }
            extern public MinMaxCurve gravityModifier { get; [NativeThrows] set; }
            extern public float gravityModifierMultiplier { get; [NativeThrows] set; }
            extern public ParticleSystemSimulationSpace simulationSpace { get; [NativeThrows] set; }
            extern public Transform customSimulationSpace { get; [NativeThrows] set; }
            extern public float simulationSpeed { get; [NativeThrows] set; }
            extern public bool useUnscaledTime { get; [NativeThrows] set; }
            extern public ParticleSystemScalingMode scalingMode { get; [NativeThrows] set; }
            extern public bool playOnAwake { get; [NativeThrows] set; }
            extern public int maxParticles { get; [NativeThrows] set; }
            extern public ParticleSystemEmitterVelocityMode emitterVelocityMode { get; [NativeThrows] set; }
            extern public ParticleSystemStopAction stopAction { get; [NativeThrows] set; }
            extern public ParticleSystemRingBufferMode ringBufferMode { get; [NativeThrows] set; }
            extern public Vector2 ringBufferLoopRange { get; [NativeThrows] set; }
            extern public ParticleSystemCullingMode cullingMode { get; [NativeThrows] set; }
        }

        public partial struct EmissionModule
        {
            internal EmissionModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public MinMaxCurve rateOverTime { get; [NativeThrows] set; }
            extern public float rateOverTimeMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve rateOverDistance { get; [NativeThrows] set; }
            extern public float rateOverDistanceMultiplier { get; [NativeThrows] set; }

            public void SetBursts(Burst[] bursts)
            {
                SetBursts(bursts, bursts.Length);
            }

            public void SetBursts(Burst[] bursts, int size)
            {
                burstCount = size;
                for (int i = 0; i < size; i++)
                    SetBurst(i, bursts[i]);
            }

            public int GetBursts(Burst[] bursts)
            {
                int returnValue = burstCount;
                for (int i = 0; i < returnValue; i++)
                    bursts[i] = GetBurst(i);
                return returnValue;
            }

            [NativeThrows]
            extern public void SetBurst(int index, Burst burst);
            [NativeThrows]
            extern public Burst GetBurst(int index);
            extern public int burstCount { get; [NativeThrows] set; }
        }

        public partial struct ShapeModule
        {
            internal ShapeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public ParticleSystemShapeType shapeType { get; [NativeThrows] set; }
            extern public float randomDirectionAmount { get; [NativeThrows] set; }
            extern public float sphericalDirectionAmount { get; [NativeThrows] set; }
            extern public float randomPositionAmount { get; [NativeThrows] set; }
            extern public bool alignToDirection { get; [NativeThrows] set; }
            extern public float radius { get; [NativeThrows] set; }
            extern public ParticleSystemShapeMultiModeValue radiusMode { get; [NativeThrows] set; }
            extern public float radiusSpread { get; [NativeThrows] set; }
            extern public MinMaxCurve radiusSpeed { get; [NativeThrows] set; }
            extern public float radiusSpeedMultiplier { get; [NativeThrows] set; }
            extern public float radiusThickness { get; [NativeThrows] set; }
            extern public float angle { get; [NativeThrows] set; }
            extern public float length { get; [NativeThrows] set; }
            extern public Vector3 boxThickness { get; [NativeThrows] set; }
            extern public ParticleSystemMeshShapeType meshShapeType { get; [NativeThrows] set; }
            extern public Mesh mesh { get; [NativeThrows] set; }
            extern public MeshRenderer meshRenderer { get; [NativeThrows] set; }
            extern public SkinnedMeshRenderer skinnedMeshRenderer { get; [NativeThrows] set; }
            extern public Sprite sprite { get; [NativeThrows] set; }
            extern public SpriteRenderer spriteRenderer { get; [NativeThrows] set; }
            extern public bool useMeshMaterialIndex { get; [NativeThrows] set; }
            extern public int meshMaterialIndex { get; [NativeThrows] set; }
            extern public bool useMeshColors { get; [NativeThrows] set; }
            extern public float normalOffset { get; [NativeThrows] set; }
            extern public ParticleSystemShapeMultiModeValue meshSpawnMode { get; [NativeThrows] set; }
            extern public float meshSpawnSpread { get; [NativeThrows] set; }
            extern public MinMaxCurve meshSpawnSpeed { get; [NativeThrows] set; }
            extern public float meshSpawnSpeedMultiplier { get; [NativeThrows] set; }
            extern public float arc { get; [NativeThrows] set; }
            extern public ParticleSystemShapeMultiModeValue arcMode { get; [NativeThrows] set; }
            extern public float arcSpread { get; [NativeThrows] set; }
            extern public MinMaxCurve arcSpeed { get; [NativeThrows] set; }
            extern public float arcSpeedMultiplier { get; [NativeThrows] set; }
            extern public float donutRadius { get; [NativeThrows] set; }
            extern public Vector3 position { get; [NativeThrows] set; }
            extern public Vector3 rotation { get; [NativeThrows] set; }
            extern public Vector3 scale { get; [NativeThrows] set; }
            extern public Texture2D texture { get; [NativeThrows] set; }
            extern public ParticleSystemShapeTextureChannel textureClipChannel { get; [NativeThrows] set; }
            extern public float textureClipThreshold { get; [NativeThrows] set; }
            extern public bool textureColorAffectsParticles { get; [NativeThrows] set; }
            extern public bool textureAlphaAffectsParticles { get; [NativeThrows] set; }
            extern public bool textureBilinearFiltering { get; [NativeThrows] set; }
            extern public int textureUVChannel { get; [NativeThrows] set; }
        }

        public partial struct VelocityOverLifetimeModule
        {
            internal VelocityOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public MinMaxCurve x { get; [NativeThrows] set; }
            extern public MinMaxCurve y { get; [NativeThrows] set; }
            extern public MinMaxCurve z { get; [NativeThrows] set; }
            extern public float xMultiplier { get; [NativeThrows] set; }
            extern public float yMultiplier { get; [NativeThrows] set; }
            extern public float zMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve orbitalX { get; [NativeThrows] set; }
            extern public MinMaxCurve orbitalY { get; [NativeThrows] set; }
            extern public MinMaxCurve orbitalZ { get; [NativeThrows] set; }
            extern public float orbitalXMultiplier { get; [NativeThrows] set; }
            extern public float orbitalYMultiplier { get; [NativeThrows] set; }
            extern public float orbitalZMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve orbitalOffsetX { get; [NativeThrows] set; }
            extern public MinMaxCurve orbitalOffsetY { get; [NativeThrows] set; }
            extern public MinMaxCurve orbitalOffsetZ { get; [NativeThrows] set; }
            extern public float orbitalOffsetXMultiplier { get; [NativeThrows] set; }
            extern public float orbitalOffsetYMultiplier { get; [NativeThrows] set; }
            extern public float orbitalOffsetZMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve radial { get; [NativeThrows] set; }
            extern public float radialMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve speedModifier { get; [NativeThrows] set; }
            extern public float speedModifierMultiplier { get; [NativeThrows] set; }
            extern public ParticleSystemSimulationSpace space { get; [NativeThrows] set; }
        }


        public partial struct LimitVelocityOverLifetimeModule
        {
            internal LimitVelocityOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public MinMaxCurve limitX { get; [NativeThrows] set; }
            extern public float limitXMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve limitY { get; [NativeThrows] set; }
            extern public float limitYMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve limitZ { get; [NativeThrows] set; }
            extern public float limitZMultiplier { get; [NativeThrows] set; }
            [NativeName("Magnitude")]
            extern public MinMaxCurve limit { get; [NativeThrows] set; }
            [NativeName("MagnitudeMultiplier")]
            extern public float limitMultiplier { get; [NativeThrows] set; }
            extern public float dampen { get; [NativeThrows] set; }
            extern public bool separateAxes { get; [NativeThrows] set; }
            extern public ParticleSystemSimulationSpace space { get; [NativeThrows] set; }
            extern public MinMaxCurve drag { get; [NativeThrows] set; }
            extern public float dragMultiplier { get; [NativeThrows] set; }
            extern public bool multiplyDragByParticleSize { get; [NativeThrows] set; }
            extern public bool multiplyDragByParticleVelocity { get; [NativeThrows] set; }
        }

        public partial struct InheritVelocityModule
        {
            internal InheritVelocityModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public ParticleSystemInheritVelocityMode mode { get; [NativeThrows] set; }
            extern public MinMaxCurve curve { get; [NativeThrows] set; }
            extern public float curveMultiplier { get; [NativeThrows] set; }
        }

        public partial struct LifetimeByEmitterSpeedModule
        {
            internal LifetimeByEmitterSpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public MinMaxCurve curve { get; [NativeThrows] set; }
            extern public float curveMultiplier { get; [NativeThrows] set; }
            extern public Vector2 range { get; [NativeThrows] set; }
        }

        public partial struct ForceOverLifetimeModule
        {
            internal ForceOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public MinMaxCurve x { get; [NativeThrows] set; }
            extern public MinMaxCurve y { get; [NativeThrows] set; }
            extern public MinMaxCurve z { get; [NativeThrows] set; }
            extern public float xMultiplier { get; [NativeThrows] set; }
            extern public float yMultiplier { get; [NativeThrows] set; }
            extern public float zMultiplier { get; [NativeThrows] set; }
            extern public ParticleSystemSimulationSpace space { get; [NativeThrows] set; }
            extern public bool randomized { get; [NativeThrows] set; }
        }

        public partial struct ColorOverLifetimeModule
        {
            internal ColorOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public MinMaxGradient color { get; [NativeThrows] set; }
        }

        public partial struct ColorBySpeedModule
        {
            internal ColorBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public MinMaxGradient color { get; [NativeThrows] set; }
            extern public Vector2 range { get; [NativeThrows] set; }
        }

        public partial struct SizeOverLifetimeModule
        {
            internal SizeOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            [NativeName("X")]
            extern public MinMaxCurve size { get; [NativeThrows] set; }
            [NativeName("XMultiplier")]
            extern public float sizeMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve x { get; [NativeThrows] set; }
            extern public float xMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve y { get; [NativeThrows] set; }
            extern public float yMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve z { get; [NativeThrows] set; }
            extern public float zMultiplier { get; [NativeThrows] set; }
            extern public bool separateAxes { get; [NativeThrows] set; }
        }

        public partial struct SizeBySpeedModule
        {
            internal SizeBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            [NativeName("X")]
            extern public MinMaxCurve size { get; [NativeThrows] set; }
            [NativeName("XMultiplier")]
            extern public float sizeMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve x { get; [NativeThrows] set; }
            extern public float xMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve y { get; [NativeThrows] set; }
            extern public float yMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve z { get; [NativeThrows] set; }
            extern public float zMultiplier { get; [NativeThrows] set; }
            extern public bool separateAxes { get; [NativeThrows] set; }
            extern public Vector2 range { get; [NativeThrows] set; }
        }

        public partial struct RotationOverLifetimeModule
        {
            internal RotationOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public MinMaxCurve x { get; [NativeThrows] set; }
            extern public float xMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve y { get; [NativeThrows] set; }
            extern public float yMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve z { get; [NativeThrows] set; }
            extern public float zMultiplier { get; [NativeThrows] set; }
            extern public bool separateAxes { get; [NativeThrows] set; }
        }

        public partial struct RotationBySpeedModule
        {
            internal RotationBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public MinMaxCurve x { get; [NativeThrows] set; }
            extern public float xMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve y { get; [NativeThrows] set; }
            extern public float yMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve z { get; [NativeThrows] set; }
            extern public float zMultiplier { get; [NativeThrows] set; }
            extern public bool separateAxes { get; [NativeThrows] set; }
            extern public Vector2 range { get; [NativeThrows] set; }
        }

        public partial struct ExternalForcesModule
        {
            internal ExternalForcesModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public float multiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve multiplierCurve { get; [NativeThrows] set; }
            extern public ParticleSystemGameObjectFilter influenceFilter { get; [NativeThrows] set; }
            extern public LayerMask influenceMask { get; [NativeThrows] set; }

            extern public int influenceCount { get; }

            extern public bool IsAffectedBy(ParticleSystemForceField field);

            [NativeThrows]
            extern public void AddInfluence([NotNull] ParticleSystemForceField field);

            [NativeThrows]
            extern private void RemoveInfluenceAtIndex(int index);
            public void RemoveInfluence(int index) { RemoveInfluenceAtIndex(index); }

            [NativeThrows]
            extern public void RemoveInfluence([NotNull] ParticleSystemForceField field);
            extern public void RemoveAllInfluences();
            [NativeThrows]
            extern public void SetInfluence(int index, [NotNull] ParticleSystemForceField field);
            [NativeThrows]
            extern public ParticleSystemForceField GetInfluence(int index);
        }

        public partial struct NoiseModule
        {
            internal NoiseModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public bool separateAxes { get; [NativeThrows] set; }
            [NativeName("StrengthX")]
            extern public MinMaxCurve strength { get; [NativeThrows] set; }
            [NativeName("StrengthXMultiplier")]
            extern public float strengthMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve strengthX { get; [NativeThrows] set; }
            extern public float strengthXMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve strengthY { get; [NativeThrows] set; }
            extern public float strengthYMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve strengthZ { get; [NativeThrows] set; }
            extern public float strengthZMultiplier { get; [NativeThrows] set; }
            extern public float frequency { get; [NativeThrows] set; }
            extern public bool damping { get; [NativeThrows] set; }
            extern public int octaveCount { get; [NativeThrows] set; }
            extern public float octaveMultiplier { get; [NativeThrows] set; }
            extern public float octaveScale { get; [NativeThrows] set; }
            extern public ParticleSystemNoiseQuality quality { get; [NativeThrows] set; }
            extern public MinMaxCurve scrollSpeed { get; [NativeThrows] set; }
            extern public float scrollSpeedMultiplier { get; [NativeThrows] set; }
            extern public bool remapEnabled { get; [NativeThrows] set; }
            [NativeName("RemapX")]
            extern public MinMaxCurve remap { get; [NativeThrows] set; }
            [NativeName("RemapXMultiplier")]
            extern public float remapMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve remapX { get; [NativeThrows] set; }
            extern public float remapXMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve remapY { get; [NativeThrows] set; }
            extern public float remapYMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve remapZ { get; [NativeThrows] set; }
            extern public float remapZMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve positionAmount { get; [NativeThrows] set; }
            extern public MinMaxCurve rotationAmount { get; [NativeThrows] set; }
            extern public MinMaxCurve sizeAmount { get; [NativeThrows] set; }
        }

        public partial struct CollisionModule
        {
            internal CollisionModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public ParticleSystemCollisionType type { get; [NativeThrows] set; }
            extern public ParticleSystemCollisionMode mode { get; [NativeThrows] set; }
            extern public MinMaxCurve dampen { get; [NativeThrows] set; }
            extern public float dampenMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve bounce { get; [NativeThrows] set; }
            extern public float bounceMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve lifetimeLoss { get; [NativeThrows] set; }
            extern public float lifetimeLossMultiplier { get; [NativeThrows] set; }
            extern public float minKillSpeed { get; [NativeThrows] set; }
            extern public float maxKillSpeed { get; [NativeThrows] set; }
            extern public LayerMask collidesWith { get; [NativeThrows] set; }
            extern public bool enableDynamicColliders { get; [NativeThrows] set; }
            extern public int maxCollisionShapes { get; [NativeThrows] set; }
            extern public ParticleSystemCollisionQuality quality { get; [NativeThrows] set; }
            extern public float voxelSize { get; [NativeThrows] set; }
            extern public float radiusScale { get; [NativeThrows] set; }
            extern public bool sendCollisionMessages { get; [NativeThrows] set; }
            extern public float colliderForce { get; [NativeThrows] set; }
            extern public bool multiplyColliderForceByCollisionAngle { get; [NativeThrows] set; }
            extern public bool multiplyColliderForceByParticleSpeed { get; [NativeThrows] set; }
            extern public bool multiplyColliderForceByParticleSize { get; [NativeThrows] set; }

            extern public void SetPlane(int index, Transform transform);
            extern public Transform GetPlane(int index);
            extern public int maxPlaneCount { get; }

            [Obsolete("enableInteriorCollisions property is deprecated and is no longer required and has no effect on the particle system.", false)]
            extern public bool enableInteriorCollisions { get; [NativeThrows] set; }
        }

        public partial struct TriggerModule
        {
            internal TriggerModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public ParticleSystemOverlapAction inside { get; [NativeThrows] set; }
            extern public ParticleSystemOverlapAction outside { get; [NativeThrows] set; }
            extern public ParticleSystemOverlapAction enter { get; [NativeThrows] set; }
            extern public ParticleSystemOverlapAction exit { get; [NativeThrows] set; }
            extern public float radiusScale { get; [NativeThrows] set; }

            [NativeThrows]
            extern public void SetCollider(int index, Component collider);
            [NativeThrows]
            extern public Component GetCollider(int index);
            extern public int maxColliderCount { get; }
        }

        public partial struct SubEmittersModule
        {
            internal SubEmittersModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public int subEmittersCount { get; }

            [NativeThrows]
            extern public void AddSubEmitter(ParticleSystem subEmitter, ParticleSystemSubEmitterType type, ParticleSystemSubEmitterProperties properties, float emitProbability);
            public void AddSubEmitter(ParticleSystem subEmitter, ParticleSystemSubEmitterType type, ParticleSystemSubEmitterProperties properties) { AddSubEmitter(subEmitter, type, properties, 1.0f); }
            [NativeThrows]
            extern public void RemoveSubEmitter(int index);
            [NativeThrows]
            extern public void SetSubEmitterSystem(int index, ParticleSystem subEmitter);
            [NativeThrows]
            extern public void SetSubEmitterType(int index, ParticleSystemSubEmitterType type);
            [NativeThrows]
            extern public void SetSubEmitterProperties(int index, ParticleSystemSubEmitterProperties properties);
            [NativeThrows]
            extern public void SetSubEmitterEmitProbability(int index, float emitProbability);
            [NativeThrows]
            extern public ParticleSystem GetSubEmitterSystem(int index);
            [NativeThrows]
            extern public ParticleSystemSubEmitterType GetSubEmitterType(int index);
            [NativeThrows]
            extern public ParticleSystemSubEmitterProperties GetSubEmitterProperties(int index);
            [NativeThrows]
            extern public float GetSubEmitterEmitProbability(int index);
        }

        public partial struct TextureSheetAnimationModule
        {
            internal TextureSheetAnimationModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public ParticleSystemAnimationMode mode { get; [NativeThrows] set; }
            extern public ParticleSystemAnimationTimeMode timeMode { get; [NativeThrows] set; }
            extern public float fps { get; [NativeThrows] set; }
            extern public int numTilesX { get; [NativeThrows] set; }
            extern public int numTilesY { get; [NativeThrows] set; }
            extern public ParticleSystemAnimationType animation { get; [NativeThrows] set; }
            extern public ParticleSystemAnimationRowMode rowMode { get; [NativeThrows] set; }
            extern public MinMaxCurve frameOverTime { get; [NativeThrows] set; }
            extern public float frameOverTimeMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve startFrame { get; [NativeThrows] set; }
            extern public float startFrameMultiplier { get; [NativeThrows] set; }
            extern public int cycleCount { get; [NativeThrows] set; }
            extern public int rowIndex { get; [NativeThrows] set; }
            extern public Rendering.UVChannelFlags uvChannelMask { get; [NativeThrows] set; }
            extern public int spriteCount { get; }
            extern public Vector2 speedRange { get; [NativeThrows] set; }

            [NativeThrows]
            extern public void AddSprite(Sprite sprite);
            [NativeThrows]
            extern public void RemoveSprite(int index);
            [NativeThrows]
            extern public void SetSprite(int index, Sprite sprite);
            [NativeThrows]
            extern public Sprite GetSprite(int index);
        }

        public partial struct LightsModule
        {
            internal LightsModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public float ratio { get; [NativeThrows] set; }
            extern public bool useRandomDistribution { get; [NativeThrows] set; }
            extern public Light light { get; [NativeThrows] set; }
            extern public bool useParticleColor { get; [NativeThrows] set; }
            extern public bool sizeAffectsRange { get; [NativeThrows] set; }
            extern public bool alphaAffectsIntensity { get; [NativeThrows] set; }
            extern public MinMaxCurve range { get; [NativeThrows] set; }
            extern public float rangeMultiplier { get; [NativeThrows] set; }
            extern public MinMaxCurve intensity { get; [NativeThrows] set; }
            extern public float intensityMultiplier { get; [NativeThrows] set; }
            extern public int maxLights { get; [NativeThrows] set; }
        }

        public partial struct TrailModule
        {
            internal TrailModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }
            extern public ParticleSystemTrailMode mode { get; [NativeThrows] set; }
            extern public float ratio { get; [NativeThrows] set; }
            extern public MinMaxCurve lifetime { get; [NativeThrows] set; }
            extern public float lifetimeMultiplier { get; [NativeThrows] set; }
            extern public float minVertexDistance { get; [NativeThrows] set; }
            extern public ParticleSystemTrailTextureMode textureMode { get; [NativeThrows] set; }
            extern public bool worldSpace { get; [NativeThrows] set; }
            extern public bool dieWithParticles { get; [NativeThrows] set; }
            extern public bool sizeAffectsWidth { get; [NativeThrows] set; }
            extern public bool sizeAffectsLifetime { get; [NativeThrows] set; }
            extern public bool inheritParticleColor { get; [NativeThrows] set; }
            extern public MinMaxGradient colorOverLifetime { get; [NativeThrows] set; }
            extern public MinMaxCurve widthOverTrail { get; [NativeThrows] set; }
            extern public float widthOverTrailMultiplier { get; [NativeThrows] set; }
            extern public MinMaxGradient colorOverTrail { get; [NativeThrows] set; }
            extern public bool generateLightingData { get; [NativeThrows] set; }
            extern public int ribbonCount { get; [NativeThrows] set; }
            extern public float shadowBias { get; [NativeThrows] set; }
            extern public bool splitSubEmitterRibbons { get; [NativeThrows] set; }
            extern public bool attachRibbonsToTransform { get; [NativeThrows] set; }
        }

        public partial struct CustomDataModule
        {
            internal CustomDataModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeThrows] set; }

            [NativeThrows]
            extern public void SetMode(ParticleSystemCustomData stream, ParticleSystemCustomDataMode mode);
            [NativeThrows]
            extern public ParticleSystemCustomDataMode GetMode(ParticleSystemCustomData stream);
            [NativeThrows]
            extern public void SetVectorComponentCount(ParticleSystemCustomData stream, int count);
            [NativeThrows]
            extern public int GetVectorComponentCount(ParticleSystemCustomData stream);
            [NativeThrows]
            extern public void SetVector(ParticleSystemCustomData stream, int component, MinMaxCurve curve);
            [NativeThrows]
            extern public MinMaxCurve GetVector(ParticleSystemCustomData stream, int component);
            [NativeThrows]
            extern public void SetColor(ParticleSystemCustomData stream, MinMaxGradient gradient);
            [NativeThrows]
            extern public MinMaxGradient GetColor(ParticleSystemCustomData stream);
        }

        // Module Accessors
        public MainModule main { get { return new MainModule(this); } }
        public EmissionModule emission { get { return new EmissionModule(this); } }
        public ShapeModule shape { get { return new ShapeModule(this); } }
        public VelocityOverLifetimeModule velocityOverLifetime { get { return new VelocityOverLifetimeModule(this); } }
        public LimitVelocityOverLifetimeModule limitVelocityOverLifetime { get { return new LimitVelocityOverLifetimeModule(this); } }
        public InheritVelocityModule inheritVelocity { get { return new InheritVelocityModule(this); } }
        public LifetimeByEmitterSpeedModule lifetimeByEmitterSpeed { get { return new LifetimeByEmitterSpeedModule(this); } }
        public ForceOverLifetimeModule forceOverLifetime { get { return new ForceOverLifetimeModule(this); } }
        public ColorOverLifetimeModule colorOverLifetime { get { return new ColorOverLifetimeModule(this); } }
        public ColorBySpeedModule colorBySpeed { get { return new ColorBySpeedModule(this); } }
        public SizeOverLifetimeModule sizeOverLifetime { get { return new SizeOverLifetimeModule(this); } }
        public SizeBySpeedModule sizeBySpeed { get { return new SizeBySpeedModule(this); } }
        public RotationOverLifetimeModule rotationOverLifetime { get { return new RotationOverLifetimeModule(this); } }
        public RotationBySpeedModule rotationBySpeed { get { return new RotationBySpeedModule(this); } }
        public ExternalForcesModule externalForces { get { return new ExternalForcesModule(this); } }
        public NoiseModule noise { get { return new NoiseModule(this); } }
        public CollisionModule collision { get { return new CollisionModule(this); } }
        public TriggerModule trigger { get { return new TriggerModule(this); } }
        public SubEmittersModule subEmitters { get { return new SubEmittersModule(this); } }
        public TextureSheetAnimationModule textureSheetAnimation { get { return new TextureSheetAnimationModule(this); } }
        public LightsModule lights { get { return new LightsModule(this); } }
        public TrailModule trails { get { return new TrailModule(this); } }
        public CustomDataModule customData { get { return new CustomDataModule(this); } }
    }
}
