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
    [NativeHeader("Runtime/ParticleSystem/ParticleSystem.h")]
    [NativeHeader("Runtime/ParticleSystem/ScriptBindings/ParticleSystemScriptBindings.h")]
    [NativeHeader("Runtime/ParticleSystem/ScriptBindings/ParticleSystemModulesScriptBindings.h")]
    public partial class ParticleSystem : Component
    {
        // Modules
        public partial struct MainModule
        {
            internal MainModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public float duration { get; set; }
            extern public bool loop { get; set; }
            extern public bool prewarm { get; set; }
            extern public MinMaxCurve startDelay { get; set; }
            extern public float startDelayMultiplier { get; set; }
            extern public MinMaxCurve startLifetime { get; set; }
            extern public float startLifetimeMultiplier { get; set; }
            extern public MinMaxCurve startSpeed { get; set; }
            extern public float startSpeedMultiplier { get; set; }
            extern public bool startSize3D { get; set; }
            [NativeName("StartSizeX")]
            extern public MinMaxCurve startSize { get; set; }
            [NativeName("StartSizeXMultiplier")]
            extern public float startSizeMultiplier { get; set; }
            extern public MinMaxCurve startSizeX { get; set; }
            extern public float startSizeXMultiplier { get; set; }
            extern public MinMaxCurve startSizeY { get; set; }
            extern public float startSizeYMultiplier { get; set; }
            extern public MinMaxCurve startSizeZ { get; set; }
            extern public float startSizeZMultiplier { get; set; }
            extern public bool startRotation3D { get; set; }
            [NativeName("StartRotationZ")]
            extern public MinMaxCurve startRotation { get; set; }
            [NativeName("StartRotationZMultiplier")]
            extern public float startRotationMultiplier { get; set; }
            extern public MinMaxCurve startRotationX { get; set; }
            extern public float startRotationXMultiplier { get; set; }
            extern public MinMaxCurve startRotationY { get; set; }
            extern public float startRotationYMultiplier { get; set; }
            extern public MinMaxCurve startRotationZ { get; set; }
            extern public float startRotationZMultiplier { get; set; }
            extern public float flipRotation { get; set; }
            extern public MinMaxGradient startColor { get; set; }
            extern public MinMaxCurve gravityModifier { get; set; }
            extern public float gravityModifierMultiplier { get; set; }
            extern public ParticleSystemSimulationSpace simulationSpace { get; set; }
            extern public Transform customSimulationSpace { get; set; }
            extern public float simulationSpeed { get; set; }
            extern public bool useUnscaledTime { get; set; }
            extern public ParticleSystemScalingMode scalingMode { get; set; }
            extern public bool playOnAwake { get; set; }
            extern public int maxParticles { get; set; }
            extern public ParticleSystemEmitterVelocityMode emitterVelocityMode { get; set; }
            extern public ParticleSystemStopAction stopAction { get; set; }
            extern public ParticleSystemRingBufferMode ringBufferMode { get; set; }
            extern public Vector2 ringBufferLoopRange { get; set; }
            extern public ParticleSystemCullingMode cullingMode { get; set; }
        }

        public partial struct EmissionModule
        {
            internal EmissionModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public MinMaxCurve rateOverTime { get; set; }
            extern public float rateOverTimeMultiplier { get; set; }
            extern public MinMaxCurve rateOverDistance { get; set; }
            extern public float rateOverDistanceMultiplier { get; set; }

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

            extern public void SetBurst(int index, Burst burst);
            extern public Burst GetBurst(int index);
            extern public int burstCount { get; set; }
        }

        public partial struct ShapeModule
        {
            internal ShapeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public ParticleSystemShapeType shapeType { get; set; }
            extern public float randomDirectionAmount { get; set; }
            extern public float sphericalDirectionAmount { get; set; }
            extern public float randomPositionAmount { get; set; }
            extern public bool alignToDirection { get; set; }
            extern public float radius { get; set; }
            extern public ParticleSystemShapeMultiModeValue radiusMode { get; set; }
            extern public float radiusSpread { get; set; }
            extern public MinMaxCurve radiusSpeed { get; set; }
            extern public float radiusSpeedMultiplier { get; set; }
            extern public float radiusThickness { get; set; }
            extern public float angle { get; set; }
            extern public float length { get; set; }
            extern public Vector3 boxThickness { get; set; }
            extern public ParticleSystemMeshShapeType meshShapeType { get; set; }
            extern public Mesh mesh { get; set; }
            extern public MeshRenderer meshRenderer { get; set; }
            extern public SkinnedMeshRenderer skinnedMeshRenderer { get; set; }
            extern public Sprite sprite { get; set; }
            extern public SpriteRenderer spriteRenderer { get; set; }
            extern public bool useMeshMaterialIndex { get; set; }
            extern public int meshMaterialIndex { get; set; }
            extern public bool useMeshColors { get; set; }
            extern public float normalOffset { get; set; }
            extern public ParticleSystemShapeMultiModeValue meshSpawnMode { get; set; }
            extern public float meshSpawnSpread { get; set; }
            extern public MinMaxCurve meshSpawnSpeed { get; set; }
            extern public float meshSpawnSpeedMultiplier { get; set; }
            extern public float arc { get; set; }
            extern public ParticleSystemShapeMultiModeValue arcMode { get; set; }
            extern public float arcSpread { get; set; }
            extern public MinMaxCurve arcSpeed { get; set; }
            extern public float arcSpeedMultiplier { get; set; }
            extern public float donutRadius { get; set; }
            extern public Vector3 position { get; set; }
            extern public Vector3 rotation { get; set; }
            extern public Vector3 scale { get; set; }
            extern public Texture2D texture { get; set; }
            extern public ParticleSystemShapeTextureChannel textureClipChannel { get; set; }
            extern public float textureClipThreshold { get; set; }
            extern public bool textureColorAffectsParticles { get; set; }
            extern public bool textureAlphaAffectsParticles { get; set; }
            extern public bool textureBilinearFiltering { get; set; }
            extern public int textureUVChannel { get; set; }
        }

        public partial struct VelocityOverLifetimeModule
        {
            internal VelocityOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public MinMaxCurve x { get; set; }
            extern public MinMaxCurve y { get; set; }
            extern public MinMaxCurve z { get; set; }
            extern public float xMultiplier { get; set; }
            extern public float yMultiplier { get; set; }
            extern public float zMultiplier { get; set; }
            extern public MinMaxCurve orbitalX { get; set; }
            extern public MinMaxCurve orbitalY { get; set; }
            extern public MinMaxCurve orbitalZ { get; set; }
            extern public float orbitalXMultiplier { get; set; }
            extern public float orbitalYMultiplier { get; set; }
            extern public float orbitalZMultiplier { get; set; }
            extern public MinMaxCurve orbitalOffsetX { get; set; }
            extern public MinMaxCurve orbitalOffsetY { get; set; }
            extern public MinMaxCurve orbitalOffsetZ { get; set; }
            extern public float orbitalOffsetXMultiplier { get; set; }
            extern public float orbitalOffsetYMultiplier { get; set; }
            extern public float orbitalOffsetZMultiplier { get; set; }
            extern public MinMaxCurve radial { get; set; }
            extern public float radialMultiplier { get; set; }
            extern public MinMaxCurve speedModifier { get; set; }
            extern public float speedModifierMultiplier { get; set; }
            extern public ParticleSystemSimulationSpace space { get; set; }
        }


        public partial struct LimitVelocityOverLifetimeModule
        {
            internal LimitVelocityOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public MinMaxCurve limitX { get; set; }
            extern public float limitXMultiplier { get; set; }
            extern public MinMaxCurve limitY { get; set; }
            extern public float limitYMultiplier { get; set; }
            extern public MinMaxCurve limitZ { get; set; }
            extern public float limitZMultiplier { get; set; }
            [NativeName("LimitX")]
            extern public MinMaxCurve limit { get; set; }
            [NativeName("LimitXMultiplier")]
            extern public float limitMultiplier { get; set; }
            extern public float dampen { get; set; }
            extern public bool separateAxes { get; set; }
            extern public ParticleSystemSimulationSpace space { get; set; }
            extern public MinMaxCurve drag { get; set; }
            extern public float dragMultiplier { get; set; }
            extern public bool multiplyDragByParticleSize { get; set; }
            extern public bool multiplyDragByParticleVelocity { get; set; }
        }

        public partial struct InheritVelocityModule
        {
            internal InheritVelocityModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public ParticleSystemInheritVelocityMode mode { get; set; }
            extern public MinMaxCurve curve { get; set; }
            extern public float curveMultiplier { get; set; }
        }

        public partial struct ForceOverLifetimeModule
        {
            internal ForceOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public MinMaxCurve x { get; set; }
            extern public MinMaxCurve y { get; set; }
            extern public MinMaxCurve z { get; set; }
            extern public float xMultiplier { get; set; }
            extern public float yMultiplier { get; set; }
            extern public float zMultiplier { get; set; }
            extern public ParticleSystemSimulationSpace space { get; set; }
            extern public bool randomized { get; set; }
        }

        public partial struct ColorOverLifetimeModule
        {
            internal ColorOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public MinMaxGradient color { get; set; }
        }

        public partial struct ColorBySpeedModule
        {
            internal ColorBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public MinMaxGradient color { get; set; }
            extern public Vector2 range { get; set; }
        }

        public partial struct SizeOverLifetimeModule
        {
            internal SizeOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            [NativeName("X")]
            extern public MinMaxCurve size { get; set; }
            [NativeName("XMultiplier")]
            extern public float sizeMultiplier { get; set; }
            extern public MinMaxCurve x { get; set; }
            extern public float xMultiplier { get; set; }
            extern public MinMaxCurve y { get; set; }
            extern public float yMultiplier { get; set; }
            extern public MinMaxCurve z { get; set; }
            extern public float zMultiplier { get; set; }
            extern public bool separateAxes { get; set; }
        }

        public partial struct SizeBySpeedModule
        {
            internal SizeBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            [NativeName("X")]
            extern public MinMaxCurve size { get; set; }
            [NativeName("XMultiplier")]
            extern public float sizeMultiplier { get; set; }
            extern public MinMaxCurve x { get; set; }
            extern public float xMultiplier { get; set; }
            extern public MinMaxCurve y { get; set; }
            extern public float yMultiplier { get; set; }
            extern public MinMaxCurve z { get; set; }
            extern public float zMultiplier { get; set; }
            extern public bool separateAxes { get; set; }
            extern public Vector2 range { get; set; }
        }

        public partial struct RotationOverLifetimeModule
        {
            internal RotationOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public MinMaxCurve x { get; set; }
            extern public float xMultiplier { get; set; }
            extern public MinMaxCurve y { get; set; }
            extern public float yMultiplier { get; set; }
            extern public MinMaxCurve z { get; set; }
            extern public float zMultiplier { get; set; }
            extern public bool separateAxes { get; set; }
        }

        public partial struct RotationBySpeedModule
        {
            internal RotationBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public MinMaxCurve x { get; set; }
            extern public float xMultiplier { get; set; }
            extern public MinMaxCurve y { get; set; }
            extern public float yMultiplier { get; set; }
            extern public MinMaxCurve z { get; set; }
            extern public float zMultiplier { get; set; }
            extern public bool separateAxes { get; set; }
            extern public Vector2 range { get; set; }
        }

        public partial struct ExternalForcesModule
        {
            internal ExternalForcesModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public float multiplier { get; set; }
            extern public ParticleSystemGameObjectFilter influenceFilter { get; set; }

            extern public bool IsAffectedBy(ParticleSystemForceField field);
        }

        public partial struct NoiseModule
        {
            internal NoiseModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public bool separateAxes { get; set; }
            [NativeName("StrengthX")]
            extern public MinMaxCurve strength { get; set; }
            [NativeName("StrengthXMultiplier")]
            extern public float strengthMultiplier { get; set; }
            extern public MinMaxCurve strengthX { get; set; }
            extern public float strengthXMultiplier { get; set; }
            extern public MinMaxCurve strengthY { get; set; }
            extern public float strengthYMultiplier { get; set; }
            extern public MinMaxCurve strengthZ { get; set; }
            extern public float strengthZMultiplier { get; set; }
            extern public float frequency { get; set; }
            extern public bool damping { get; set; }
            extern public int octaveCount { get; set; }
            extern public float octaveMultiplier { get; set; }
            extern public float octaveScale { get; set; }
            extern public ParticleSystemNoiseQuality quality { get; set; }
            extern public MinMaxCurve scrollSpeed { get; set; }
            extern public float scrollSpeedMultiplier { get; set; }
            extern public bool remapEnabled { get; set; }
            [NativeName("RemapX")]
            extern public MinMaxCurve remap { get; set; }
            [NativeName("RemapXMultiplier")]
            extern public float remapMultiplier { get; set; }
            extern public MinMaxCurve remapX { get; set; }
            extern public float remapXMultiplier { get; set; }
            extern public MinMaxCurve remapY { get; set; }
            extern public float remapYMultiplier { get; set; }
            extern public MinMaxCurve remapZ { get; set; }
            extern public float remapZMultiplier { get; set; }
            extern public MinMaxCurve positionAmount { get; set; }
            extern public MinMaxCurve rotationAmount { get; set; }
            extern public MinMaxCurve sizeAmount { get; set; }
        }

        public partial struct CollisionModule
        {
            internal CollisionModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public ParticleSystemCollisionType type { get; set; }
            extern public ParticleSystemCollisionMode mode { get; set; }
            extern public MinMaxCurve dampen { get; set; }
            extern public float dampenMultiplier { get; set; }
            extern public MinMaxCurve bounce { get; set; }
            extern public float bounceMultiplier { get; set; }
            extern public MinMaxCurve lifetimeLoss { get; set; }
            extern public float lifetimeLossMultiplier { get; set; }
            extern public float minKillSpeed { get; set; }
            extern public float maxKillSpeed { get; set; }
            extern public LayerMask collidesWith { get; set; }
            extern public bool enableDynamicColliders { get; set; }
            extern public int maxCollisionShapes { get; set; }
            extern public ParticleSystemCollisionQuality quality { get; set; }
            extern public float voxelSize { get; set; }
            extern public float radiusScale { get; set; }
            extern public bool sendCollisionMessages { get; set; }
            extern public float colliderForce { get; set; }
            extern public bool multiplyColliderForceByCollisionAngle { get; set; }
            extern public bool multiplyColliderForceByParticleSpeed { get; set; }
            extern public bool multiplyColliderForceByParticleSize { get; set; }

            extern public void SetPlane(int index, Transform transform);
            extern public Transform GetPlane(int index);
            extern public int maxPlaneCount { get; }

            [Obsolete("enableInteriorCollisions property is deprecated and is no longer required and has no effect on the particle system.", false)]
            extern public bool enableInteriorCollisions { get; set; }
        }

        public partial struct TriggerModule
        {
            internal TriggerModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public ParticleSystemOverlapAction inside { get; set; }
            extern public ParticleSystemOverlapAction outside { get; set; }
            extern public ParticleSystemOverlapAction enter { get; set; }
            extern public ParticleSystemOverlapAction exit { get; set; }
            extern public float radiusScale { get; set; }

            extern public void SetCollider(int index, Component collider);
            extern public Component GetCollider(int index);
            extern public int maxColliderCount { get; }
        }

        public partial struct SubEmittersModule
        {
            internal SubEmittersModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public int subEmittersCount { get; }

            extern public void AddSubEmitter(ParticleSystem subEmitter, ParticleSystemSubEmitterType type, ParticleSystemSubEmitterProperties properties, float emitProbability);
            public void AddSubEmitter(ParticleSystem subEmitter, ParticleSystemSubEmitterType type, ParticleSystemSubEmitterProperties properties) { AddSubEmitter(subEmitter, type, properties, 1.0f); }
            extern public void RemoveSubEmitter(int index);
            extern public void SetSubEmitterSystem(int index, ParticleSystem subEmitter);
            extern public void SetSubEmitterType(int index, ParticleSystemSubEmitterType type);
            extern public void SetSubEmitterProperties(int index, ParticleSystemSubEmitterProperties properties);
            extern public void SetSubEmitterEmitProbability(int index, float emitProbability);
            extern public ParticleSystem GetSubEmitterSystem(int index);
            extern public ParticleSystemSubEmitterType GetSubEmitterType(int index);
            extern public ParticleSystemSubEmitterProperties GetSubEmitterProperties(int index);
            extern public float GetSubEmitterEmitProbability(int index);
        }

        public partial struct TextureSheetAnimationModule
        {
            internal TextureSheetAnimationModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public ParticleSystemAnimationMode mode { get; set; }
            extern public ParticleSystemAnimationTimeMode timeMode { get; set; }
            extern public float fps { get; set; }
            extern public int numTilesX { get; set; }
            extern public int numTilesY { get; set; }
            extern public ParticleSystemAnimationType animation { get; set; }
            extern public bool useRandomRow { get; set; }
            extern public MinMaxCurve frameOverTime { get; set; }
            extern public float frameOverTimeMultiplier { get; set; }
            extern public MinMaxCurve startFrame { get; set; }
            extern public float startFrameMultiplier { get; set; }
            extern public int cycleCount { get; set; }
            extern public int rowIndex { get; set; }
            extern public Rendering.UVChannelFlags uvChannelMask { get; set; }
            extern public int spriteCount { get; }
            extern public Vector2 speedRange { get; set; }

            extern public void AddSprite(Sprite sprite);
            extern public void RemoveSprite(int index);
            extern public void SetSprite(int index, Sprite sprite);
            extern public Sprite GetSprite(int index);
        }

        public partial struct LightsModule
        {
            internal LightsModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public float ratio { get; set; }
            extern public bool useRandomDistribution { get; set; }
            extern public Light light { get; set; }
            extern public bool useParticleColor { get; set; }
            extern public bool sizeAffectsRange { get; set; }
            extern public bool alphaAffectsIntensity { get; set; }
            extern public MinMaxCurve range { get; set; }
            extern public float rangeMultiplier { get; set; }
            extern public MinMaxCurve intensity { get; set; }
            extern public float intensityMultiplier { get; set; }
            extern public int maxLights { get; set; }
        }

        public partial struct TrailModule
        {
            internal TrailModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }
            extern public ParticleSystemTrailMode mode { get; set; }
            extern public float ratio { get; set; }
            extern public MinMaxCurve lifetime { get; set; }
            extern public float lifetimeMultiplier { get; set; }
            extern public float minVertexDistance { get; set; }
            extern public ParticleSystemTrailTextureMode textureMode { get; set; }
            extern public bool worldSpace { get; set; }
            extern public bool dieWithParticles { get; set; }
            extern public bool sizeAffectsWidth { get; set; }
            extern public bool sizeAffectsLifetime { get; set; }
            extern public bool inheritParticleColor { get; set; }
            extern public MinMaxGradient colorOverLifetime { get; set; }
            extern public MinMaxCurve widthOverTrail { get; set; }
            extern public float widthOverTrailMultiplier { get; set; }
            extern public MinMaxGradient colorOverTrail { get; set; }
            extern public bool generateLightingData { get; set; }
            extern public int ribbonCount { get; set; }
            extern public float shadowBias { get; set; }
            extern public bool splitSubEmitterRibbons { get; set; }
            extern public bool attachRibbonsToTransform { get; set; }
        }

        public partial struct CustomDataModule
        {
            internal CustomDataModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; set; }

            extern public void SetMode(ParticleSystemCustomData stream, ParticleSystemCustomDataMode mode);
            extern public ParticleSystemCustomDataMode GetMode(ParticleSystemCustomData stream);
            extern public void SetVectorComponentCount(ParticleSystemCustomData stream, int count);
            extern public int GetVectorComponentCount(ParticleSystemCustomData stream);
            extern public void SetVector(ParticleSystemCustomData stream, int component, MinMaxCurve curve);
            extern public MinMaxCurve GetVector(ParticleSystemCustomData stream, int component);
            extern public void SetColor(ParticleSystemCustomData stream, MinMaxGradient gradient);
            extern public MinMaxGradient GetColor(ParticleSystemCustomData stream);
        }

        // Module Accessors
        public MainModule main { get { return new MainModule(this); } }
        public EmissionModule emission { get { return new EmissionModule(this); } }
        public ShapeModule shape { get { return new ShapeModule(this); } }
        public VelocityOverLifetimeModule velocityOverLifetime { get { return new VelocityOverLifetimeModule(this); } }
        public LimitVelocityOverLifetimeModule limitVelocityOverLifetime { get { return new LimitVelocityOverLifetimeModule(this); } }
        public InheritVelocityModule inheritVelocity { get { return new InheritVelocityModule(this); } }
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
