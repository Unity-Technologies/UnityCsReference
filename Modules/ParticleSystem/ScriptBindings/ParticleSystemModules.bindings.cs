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

            extern public Vector3 emitterVelocity { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float duration { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool loop { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool prewarm { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startDelay { get => startDelayBlittable; set => startDelayBlittable = value; }
            [NativeName("StartDelay")] private extern MinMaxCurveBlittable startDelayBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startDelayMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startLifetime { get => startLifetimeBlittable; set => startLifetimeBlittable = value; }
            [NativeName("StartLifetime")] private extern MinMaxCurveBlittable startLifetimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startLifetimeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startSpeed { get => startSpeedBlittable; set => startSpeedBlittable = value; }
            [NativeName("StartSpeed")] private extern MinMaxCurveBlittable startSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool startSize3D { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startSize { get => startSizeBlittable; set => startSizeBlittable = value; }
            [NativeName("StartSizeX")] private extern MinMaxCurveBlittable startSizeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("StartSizeXMultiplier")]
            extern public float startSizeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startSizeX { get => startSizeXBlittable; set => startSizeXBlittable = value; }
            [NativeName("StartSizeX")] private extern MinMaxCurveBlittable startSizeXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startSizeXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startSizeY { get => startSizeYBlittable; set => startSizeYBlittable = value; }
            [NativeName("StartSizeY")] private extern MinMaxCurveBlittable startSizeYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startSizeYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startSizeZ { get => startSizeZBlittable; set => startSizeZBlittable = value; }
            [NativeName("StartSizeZ")] private extern MinMaxCurveBlittable startSizeZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startSizeZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool startRotation3D { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startRotation { get => startRotationBlittable; set => startRotationBlittable = value; }
            [NativeName("StartRotationZ")] private extern MinMaxCurveBlittable startRotationBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("StartRotationZMultiplier")]
            extern public float startRotationMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startRotationX { get => startRotationXBlittable; set => startRotationXBlittable = value; }
            [NativeName("StartRotationX")] private extern MinMaxCurveBlittable startRotationXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startRotationXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startRotationY { get => startRotationYBlittable; set => startRotationYBlittable = value; }
            [NativeName("StartRotationY")] private extern MinMaxCurveBlittable startRotationYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startRotationYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startRotationZ { get => startRotationZBlittable; set => startRotationZBlittable = value; }
            [NativeName("StartRotationZ")] private extern MinMaxCurveBlittable startRotationZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startRotationZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float flipRotation { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxGradient startColor { get => startColorBlittable; set => startColorBlittable = value; }
            [NativeName("StartColor")] private extern MinMaxGradientBlittable startColorBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public ParticleSystemGravitySource gravitySource { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve gravityModifier { get => gravityModifierBlittable; set => gravityModifierBlittable = value; }
            [NativeName("GravityModifier")] private extern MinMaxCurveBlittable gravityModifierBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float gravityModifierMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemSimulationSpace simulationSpace { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Transform customSimulationSpace { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float simulationSpeed { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool useUnscaledTime { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemScalingMode scalingMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool playOnAwake { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int maxParticles { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemEmitterVelocityMode emitterVelocityMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemStopAction stopAction { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemRingBufferMode ringBufferMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector2 ringBufferLoopRange { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemCullingMode cullingMode { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct EmissionModule
        {
            internal EmissionModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve rateOverTime { get => rateOverTimeBlittable; set => rateOverTimeBlittable = value; }
            [NativeName("RateOverTime")] private extern MinMaxCurveBlittable rateOverTimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float rateOverTimeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve rateOverDistance { get => rateOverDistanceBlittable; set => rateOverDistanceBlittable = value; }
            [NativeName("RateOverDistance")] private extern MinMaxCurveBlittable rateOverDistanceBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float rateOverDistanceMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

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

            [NativeMethod(ThrowsException = true)]
            extern public void SetBurst(int index, Burst burst);
            [NativeMethod(ThrowsException = true)]
            extern public Burst GetBurst(int index);
            extern public int burstCount { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct ShapeModule
        {
            internal ShapeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemShapeType shapeType { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float randomDirectionAmount { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float sphericalDirectionAmount { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float randomPositionAmount { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool alignToDirection { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float radius { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemShapeMultiModeValue radiusMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float radiusSpread { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve radiusSpeed { get => radiusSpeedBlittable; set => radiusSpeedBlittable = value; }
            [NativeName("RadiusSpeed")] private extern MinMaxCurveBlittable radiusSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float radiusSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float radiusThickness { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float angle { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float length { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector3 boxThickness { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemMeshShapeType meshShapeType { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Mesh mesh { get; [NativeMethod(ThrowsException = true)] set; }
            extern public MeshRenderer meshRenderer { get; [NativeMethod(ThrowsException = true)] set; }
            extern public SkinnedMeshRenderer skinnedMeshRenderer { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Sprite sprite { get; [NativeMethod(ThrowsException = true)] set; }
            extern public SpriteRenderer spriteRenderer { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool useMeshMaterialIndex { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int meshMaterialIndex { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool useMeshColors { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float normalOffset { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemShapeMultiModeValue meshSpawnMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float meshSpawnSpread { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve meshSpawnSpeed { get => meshSpawnSpeedBlittable; set => meshSpawnSpeedBlittable = value; }
            [NativeName("MeshSpawnSpeed")] private extern MinMaxCurveBlittable meshSpawnSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float meshSpawnSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float arc { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemShapeMultiModeValue arcMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float arcSpread { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve arcSpeed { get => arcSpeedBlittable; set => arcSpeedBlittable = value; }
            [NativeName("ArcSpeed")] private extern MinMaxCurveBlittable arcSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float arcSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float donutRadius { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector3 position { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector3 rotation { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector3 scale { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Texture2D texture { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemShapeTextureChannel textureClipChannel { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float textureClipThreshold { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool textureColorAffectsParticles { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool textureAlphaAffectsParticles { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool textureBilinearFiltering { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int textureUVChannel { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct VelocityOverLifetimeModule
        {
            internal VelocityOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve orbitalX { get => orbitalXBlittable; set => orbitalXBlittable = value; }
            [NativeName("OrbitalX")] private extern MinMaxCurveBlittable orbitalXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve orbitalY { get => orbitalYBlittable; set => orbitalYBlittable = value; }
            [NativeName("OrbitalY")] private extern MinMaxCurveBlittable orbitalYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve orbitalZ { get => orbitalZBlittable; set => orbitalZBlittable = value; }
            [NativeName("OrbitalZ")] private extern MinMaxCurveBlittable orbitalZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float orbitalXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float orbitalYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float orbitalZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve orbitalOffsetX { get => orbitalOffsetXBlittable; set => orbitalOffsetXBlittable = value; }
            [NativeName("OrbitalOffsetX")] private extern MinMaxCurveBlittable orbitalOffsetXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve orbitalOffsetY { get => orbitalOffsetYBlittable; set => orbitalOffsetYBlittable = value; }
            [NativeName("OrbitalOffsetY")] private extern MinMaxCurveBlittable orbitalOffsetYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve orbitalOffsetZ { get => orbitalOffsetZBlittable; set => orbitalOffsetZBlittable = value; }
            [NativeName("OrbitalOffsetZ")] private extern MinMaxCurveBlittable orbitalOffsetZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float orbitalOffsetXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float orbitalOffsetYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float orbitalOffsetZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve radial { get => radialBlittable; set => radialBlittable = value; }
            [NativeName("Radial")] private extern MinMaxCurveBlittable radialBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float radialMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve speedModifier { get => speedModifierBlittable; set => speedModifierBlittable = value; }
            [NativeName("SpeedModifier")] private extern MinMaxCurveBlittable speedModifierBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float speedModifierMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemSimulationSpace space { get; [NativeMethod(ThrowsException = true)] set; }
        }


        public partial struct LimitVelocityOverLifetimeModule
        {
            internal LimitVelocityOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve limitX { get => limitXBlittable; set => limitXBlittable = value; }
            [NativeName("LimitX")] private extern MinMaxCurveBlittable limitXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float limitXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve limitY { get => limitYBlittable; set => limitYBlittable = value; }
            [NativeName("LimitY")] private extern MinMaxCurveBlittable limitYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float limitYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve limitZ { get => limitZBlittable; set => limitZBlittable = value; }
            [NativeName("LimitZ")] private extern MinMaxCurveBlittable limitZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float limitZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve limit { get => limitBlittable; set => limitBlittable = value; }
            [NativeName("Magnitude")] private extern MinMaxCurveBlittable limitBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("MagnitudeMultiplier")]
            extern public float limitMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float dampen { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemSimulationSpace space { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve drag { get => dragBlittable; set => dragBlittable = value; }
            [NativeName("Drag")] private extern MinMaxCurveBlittable dragBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float dragMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool multiplyDragByParticleSize { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool multiplyDragByParticleVelocity { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct InheritVelocityModule
        {
            internal InheritVelocityModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemInheritVelocityMode mode { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve curve { get => curveBlittable; set => curveBlittable = value; }
            [NativeName("Curve")] private extern MinMaxCurveBlittable curveBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float curveMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct LifetimeByEmitterSpeedModule
        {
            internal LifetimeByEmitterSpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve curve { get => curveBlittable; set => curveBlittable = value; }
            [NativeName("Curve")] private extern MinMaxCurveBlittable curveBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float curveMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector2 range { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct ForceOverLifetimeModule
        {
            internal ForceOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemSimulationSpace space { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool randomized { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct ColorOverLifetimeModule
        {
            internal ColorOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxGradient color { get => colorBlittable; set => colorBlittable = value; }
            [NativeName("Color")] private extern MinMaxGradientBlittable colorBlittable { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct ColorBySpeedModule
        {
            internal ColorBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxGradient color { get => colorBlittable; set => colorBlittable = value; }
            [NativeName("Color")] private extern MinMaxGradientBlittable colorBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public Vector2 range { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct SizeOverLifetimeModule
        {
            internal SizeOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve size { get => sizeBlittable; set => sizeBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable sizeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("XMultiplier")]
            extern public float sizeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct SizeBySpeedModule
        {
            internal SizeBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve size { get => sizeBlittable; set => sizeBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable sizeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("XMultiplier")]
            extern public float sizeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector2 range { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct RotationOverLifetimeModule
        {
            internal RotationOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct RotationBySpeedModule
        {
            internal RotationBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve x { get => xBlittable; set => xBlittable = value; }
            [NativeName("X")] private extern MinMaxCurveBlittable xBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float xMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve y { get => yBlittable; set => yBlittable = value; }
            [NativeName("Y")] private extern MinMaxCurveBlittable yBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float yMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve z { get => zBlittable; set => zBlittable = value; }
            [NativeName("Z")] private extern MinMaxCurveBlittable zBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float zMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector2 range { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct ExternalForcesModule
        {
            internal ExternalForcesModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float multiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve multiplierCurve { get => multiplierCurveBlittable; set => multiplierCurveBlittable = value; }
            [NativeName("MultiplierCurve")] private extern MinMaxCurveBlittable multiplierCurveBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public ParticleSystemGameObjectFilter influenceFilter { get; [NativeMethod(ThrowsException = true)] set; }
            extern public LayerMask influenceMask { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeMethod(ThrowsException = true)]
            extern public int influenceCount { get; }

            extern public bool IsAffectedBy(ParticleSystemForceField field);

            [NativeMethod(ThrowsException = true)]
            extern public void AddInfluence([NotNull] ParticleSystemForceField field);

            [NativeMethod(ThrowsException = true)]
            extern private void RemoveInfluenceAtIndex(int index);
            public void RemoveInfluence(int index) { RemoveInfluenceAtIndex(index); }

            [NativeMethod(ThrowsException = true)]
            extern public void RemoveInfluence([NotNull] ParticleSystemForceField field);
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveAllInfluences();
            [NativeMethod(ThrowsException = true)]
            extern public void SetInfluence(int index, [NotNull] ParticleSystemForceField field);
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystemForceField GetInfluence(int index);
        }

        public partial struct NoiseModule
        {
            internal NoiseModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool separateAxes { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve strength { get => strengthBlittable; set => strengthBlittable = value; }
            [NativeName("StrengthX")] private extern MinMaxCurveBlittable strengthBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("StrengthXMultiplier")]
            extern public float strengthMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve strengthX { get => strengthXBlittable; set => strengthXBlittable = value; }
            [NativeName("StrengthX")] private extern MinMaxCurveBlittable strengthXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float strengthXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve strengthY { get => strengthYBlittable; set => strengthYBlittable = value; }
            [NativeName("StrengthY")] private extern MinMaxCurveBlittable strengthYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float strengthYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve strengthZ { get => strengthZBlittable; set => strengthZBlittable = value; }
            [NativeName("StrengthZ")] private extern MinMaxCurveBlittable strengthZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float strengthZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float frequency { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool damping { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int octaveCount { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float octaveMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float octaveScale { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemNoiseQuality quality { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve scrollSpeed { get => scrollSpeedBlittable; set => scrollSpeedBlittable = value; }
            [NativeName("ScrollSpeed")] private extern MinMaxCurveBlittable scrollSpeedBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float scrollSpeedMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool remapEnabled { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve remap { get => remapBlittable; set => remapBlittable = value; }
            [NativeName("RemapX")] private extern MinMaxCurveBlittable remapBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeName("RemapXMultiplier")]
            extern public float remapMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve remapX { get => remapXBlittable; set => remapXBlittable = value; }
            [NativeName("RemapX")] private extern MinMaxCurveBlittable remapXBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float remapXMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve remapY { get => remapYBlittable; set => remapYBlittable = value; }
            [NativeName("RemapY")] private extern MinMaxCurveBlittable remapYBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float remapYMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve remapZ { get => remapZBlittable; set => remapZBlittable = value; }
            [NativeName("RemapZ")] private extern MinMaxCurveBlittable remapZBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float remapZMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve positionAmount { get => positionAmountBlittable; set => positionAmountBlittable = value; }
            [NativeName("PositionAmount")] private extern MinMaxCurveBlittable positionAmountBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve rotationAmount { get => rotationAmountBlittable; set => rotationAmountBlittable = value; }
            [NativeName("RotationAmount")] private extern MinMaxCurveBlittable rotationAmountBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve sizeAmount { get => sizeAmountBlittable; set => sizeAmountBlittable = value; }
            [NativeName("SizeAmount")] private extern MinMaxCurveBlittable sizeAmountBlittable { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct CollisionModule
        {
            internal CollisionModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemCollisionType type { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemCollisionMode mode { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve dampen { get => dampenBlittable; set => dampenBlittable = value; }
            [NativeName("Dampen")] private extern MinMaxCurveBlittable dampenBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float dampenMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve bounce { get => bounceBlittable; set => bounceBlittable = value; }
            [NativeName("Bounce")] private extern MinMaxCurveBlittable bounceBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float bounceMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve lifetimeLoss { get => lifetimeLossBlittable; set => lifetimeLossBlittable = value; }
            [NativeName("LifetimeLoss")] private extern MinMaxCurveBlittable lifetimeLossBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float lifetimeLossMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float minKillSpeed { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float maxKillSpeed { get; [NativeMethod(ThrowsException = true)] set; }
            extern public LayerMask collidesWith { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool enableDynamicColliders { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int maxCollisionShapes { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemCollisionQuality quality { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float voxelSize { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float radiusScale { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool sendCollisionMessages { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float colliderForce { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool multiplyColliderForceByCollisionAngle { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool multiplyColliderForceByParticleSpeed { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool multiplyColliderForceByParticleSize { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeMethod(ThrowsException = true)]
            extern public void AddPlane(Transform transform);
            [NativeMethod(ThrowsException = true)]
            extern public void RemovePlane(int index);
            public void RemovePlane(Transform transform) { RemovePlaneObject(transform); }
            [NativeMethod(ThrowsException = true)]
            extern private void RemovePlaneObject(Transform transform);
            [NativeMethod(ThrowsException = true)]
            extern public void SetPlane(int index, Transform transform);
            [NativeMethod(ThrowsException = true)]
            extern public Transform GetPlane(int index);
            [NativeMethod(ThrowsException = true)]
            extern public int planeCount { get; }

            [Obsolete("enableInteriorCollisions property is deprecated and is no longer required and has no effect on the particle system.", false)]
            extern public bool enableInteriorCollisions { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct TriggerModule
        {
            internal TriggerModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemOverlapAction inside { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemOverlapAction outside { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemOverlapAction enter { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemOverlapAction exit { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemColliderQueryMode colliderQueryMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float radiusScale { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeMethod(ThrowsException = true)]
            extern public void AddCollider(Component collider);
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveCollider(int index);
            public void RemoveCollider(Component collider) { RemoveColliderObject(collider); }
            [NativeMethod(ThrowsException = true)]
            extern private void RemoveColliderObject(Component collider);
            [NativeMethod(ThrowsException = true)]
            extern public void SetCollider(int index, Component collider);
            [NativeMethod(ThrowsException = true)]
            extern public Component GetCollider(int index);
            [NativeMethod(ThrowsException = true)]
            extern public int colliderCount { get; }
        }

        public partial struct SubEmittersModule
        {
            internal SubEmittersModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int subEmittersCount { get; }

            [NativeMethod(ThrowsException = true)]
            extern public void AddSubEmitter(ParticleSystem subEmitter, ParticleSystemSubEmitterType type, ParticleSystemSubEmitterProperties properties, float emitProbability);
            public void AddSubEmitter(ParticleSystem subEmitter, ParticleSystemSubEmitterType type, ParticleSystemSubEmitterProperties properties) { AddSubEmitter(subEmitter, type, properties, 1.0f); }
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveSubEmitter(int index);
            public void RemoveSubEmitter(ParticleSystem subEmitter) { RemoveSubEmitterObject(subEmitter); }
            [NativeMethod(ThrowsException = true)]
            extern private void RemoveSubEmitterObject(ParticleSystem subEmitter);
            [NativeMethod(ThrowsException = true)]
            extern public void SetSubEmitterSystem(int index, ParticleSystem subEmitter);
            [NativeMethod(ThrowsException = true)]
            extern public void SetSubEmitterType(int index, ParticleSystemSubEmitterType type);
            [NativeMethod(ThrowsException = true)]
            extern public void SetSubEmitterProperties(int index, ParticleSystemSubEmitterProperties properties);
            [NativeMethod(ThrowsException = true)]
            extern public void SetSubEmitterEmitProbability(int index, float emitProbability);
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystem GetSubEmitterSystem(int index);
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystemSubEmitterType GetSubEmitterType(int index);
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystemSubEmitterProperties GetSubEmitterProperties(int index);
            [NativeMethod(ThrowsException = true)]
            extern public float GetSubEmitterEmitProbability(int index);
        }

        public partial struct TextureSheetAnimationModule
        {
            internal TextureSheetAnimationModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemAnimationMode mode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemAnimationTimeMode timeMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float fps { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int numTilesX { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int numTilesY { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemAnimationType animation { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemAnimationRowMode rowMode { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve frameOverTime { get => frameOverTimeBlittable; set => frameOverTimeBlittable = value; }
            [NativeName("FrameOverTime")] private extern MinMaxCurveBlittable frameOverTimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float frameOverTimeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve startFrame { get => startFrameBlittable; set => startFrameBlittable = value; }
            [NativeName("StartFrame")] private extern MinMaxCurveBlittable startFrameBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float startFrameMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int cycleCount { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int rowIndex { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Rendering.UVChannelFlags uvChannelMask { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int spriteCount { get; }
            extern public Vector2 speedRange { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeMethod(ThrowsException = true)]
            extern public void AddSprite(Sprite sprite);
            [NativeMethod(ThrowsException = true)]
            extern public void RemoveSprite(int index);
            [NativeMethod(ThrowsException = true)]
            extern public void SetSprite(int index, Sprite sprite);
            [NativeMethod(ThrowsException = true)]
            extern public Sprite GetSprite(int index);
        }

        public partial struct LightsModule
        {
            internal LightsModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float ratio { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool useRandomDistribution { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Light light { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool useParticleColor { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool sizeAffectsRange { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool alphaAffectsIntensity { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve range { get => rangeBlittable; set => rangeBlittable = value; }
            [NativeName("Range")] private extern MinMaxCurveBlittable rangeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float rangeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve intensity { get => intensityBlittable; set => intensityBlittable = value; }
            [NativeName("Intensity")] private extern MinMaxCurveBlittable intensityBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float intensityMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int maxLights { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct TrailModule
        {
            internal TrailModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemTrailMode mode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float ratio { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve lifetime { get => lifetimeBlittable; set => lifetimeBlittable = value; }
            [NativeName("Lifetime")] private extern MinMaxCurveBlittable lifetimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float lifetimeMultiplier { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float minVertexDistance { get; [NativeMethod(ThrowsException = true)] set; }
            extern public ParticleSystemTrailTextureMode textureMode { get; [NativeMethod(ThrowsException = true)] set; }
            extern public Vector2 textureScale { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool worldSpace { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool dieWithParticles { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool sizeAffectsWidth { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool sizeAffectsLifetime { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool inheritParticleColor { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxGradient colorOverLifetime { get => colorOverLifetimeBlittable; set => colorOverLifetimeBlittable = value; }
            [NativeName("ColorOverLifetime")] private extern MinMaxGradientBlittable colorOverLifetimeBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxCurve widthOverTrail { get => widthOverTrailBlittable; set => widthOverTrailBlittable = value; }
            [NativeName("WidthOverTrail")] private extern MinMaxCurveBlittable widthOverTrailBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public float widthOverTrailMultiplier { get; [NativeMethod(ThrowsException = true)] set; }

            public MinMaxGradient colorOverTrail { get => colorOverTrailBlittable; set => colorOverTrailBlittable = value; }
            [NativeName("ColorOverTrail")] private extern MinMaxGradientBlittable colorOverTrailBlittable { get; [NativeMethod(ThrowsException = true)] set; }

            extern public bool generateLightingData { get; [NativeMethod(ThrowsException = true)] set; }
            extern public int ribbonCount { get; [NativeMethod(ThrowsException = true)] set; }
            extern public float shadowBias { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool splitSubEmitterRibbons { get; [NativeMethod(ThrowsException = true)] set; }
            extern public bool attachRibbonsToTransform { get; [NativeMethod(ThrowsException = true)] set; }
        }

        public partial struct CustomDataModule
        {
            internal CustomDataModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
            internal ParticleSystem m_ParticleSystem;

            extern public bool enabled { get; [NativeMethod(ThrowsException = true)] set; }

            [NativeMethod(ThrowsException = true)]
            extern public void SetMode(ParticleSystemCustomData stream, ParticleSystemCustomDataMode mode);
            [NativeMethod(ThrowsException = true)]
            extern public ParticleSystemCustomDataMode GetMode(ParticleSystemCustomData stream);
            [NativeMethod(ThrowsException = true)]
            extern public void SetVectorComponentCount(ParticleSystemCustomData stream, int count);
            [NativeMethod(ThrowsException = true)]
            extern public int GetVectorComponentCount(ParticleSystemCustomData stream);

            public void SetVector(ParticleSystemCustomData stream, int component, MinMaxCurve curve)
            {
                SetVectorInternal(stream, component, MinMaxCurveBlittable.FromMixMaxCurve(curve));
            }

            [NativeMethod(ThrowsException = true)]
            private extern void SetVectorInternal(ParticleSystemCustomData stream, int component, MinMaxCurveBlittable curve);

            public MinMaxCurve GetVector(ParticleSystemCustomData stream, int component)
            {
                return MinMaxCurveBlittable.ToMinMaxCurve(GetVectorInternal(stream, component));
            }
            [NativeMethod(ThrowsException = true)]
            private extern MinMaxCurveBlittable GetVectorInternal(ParticleSystemCustomData stream, int component);

            public void SetColor(ParticleSystemCustomData stream, MinMaxGradient gradient)
            {
                SetColorInternal(stream, MinMaxGradientBlittable.FromMixMaxGradient(gradient));
            }
            [NativeMethod(ThrowsException = true)]
            private extern void SetColorInternal(ParticleSystemCustomData stream, MinMaxGradientBlittable gradient);

            public MinMaxGradient GetColor(ParticleSystemCustomData stream)
            {
                return MinMaxGradientBlittable.ToMinMaxGradient(GetColorInternal(stream));
            }
            [NativeMethod(ThrowsException = true)]
            extern private MinMaxGradientBlittable GetColorInternal(ParticleSystemCustomData stream);
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
