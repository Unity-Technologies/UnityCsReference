// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public partial class ParticleSystem
    {
        [StructLayout(LayoutKind.Sequential), NativeType(CodegenOptions.Custom, "MonoBurst", Header = "Runtime/Scripting/ScriptingCommonStructDefinitions.h")]
        public partial struct Burst
        {
            public Burst(float _time, short _count) { m_Time = _time; m_Count = _count; m_RepeatCount = 0; m_RepeatInterval = 0.0f; m_InvProbability = 0.0f; }
            public Burst(float _time, short _minCount, short _maxCount) { m_Time = _time; m_Count = new MinMaxCurve(_minCount, _maxCount); m_RepeatCount = 0; m_RepeatInterval = 0.0f; m_InvProbability = 0.0f; }
            public Burst(float _time, short _minCount, short _maxCount, int _cycleCount, float _repeatInterval) { m_Time = _time; m_Count = new MinMaxCurve(_minCount, _maxCount); m_RepeatCount = _cycleCount - 1; m_RepeatInterval = _repeatInterval; m_InvProbability = 0.0f; }
            public Burst(float _time, MinMaxCurve _count) { m_Time = _time; m_Count = _count; m_RepeatCount = 0; m_RepeatInterval = 0.0f; m_InvProbability = 0.0f; }
            public Burst(float _time, MinMaxCurve _count, int _cycleCount, float _repeatInterval) { m_Time = _time; m_Count = _count; m_RepeatCount = _cycleCount - 1; m_RepeatInterval = _repeatInterval; m_InvProbability = 0.0f; }

            public float time { get { return m_Time; } set { m_Time = value; } }                                                // The time the burst happens.
            public MinMaxCurve count { get { return m_Count; } set { m_Count = value; } }                                       // Number of particles to be emitted.
            public short minCount { get { return (short)m_Count.constantMin; } set { m_Count.constantMin = (short)value; } }    // Minimum number of particles to be emitted.
            public short maxCount { get { return (short)m_Count.constantMax; } set { m_Count.constantMax = (short)value; } }    // Maximum number of particles to be emitted.

            // How many times to play the burst.
            public int cycleCount
            {
                get
                {
                    return m_RepeatCount + 1;
                }
                set
                {
                    if (value < 0)
                        throw new ArgumentOutOfRangeException("cycleCount", "cycleCount must be at least 0: " + value);
                    m_RepeatCount = value - 1;
                }
            }

            // The interval between repeats of the burst.
            public float repeatInterval
            {
                get
                {
                    return m_RepeatInterval;
                }
                set
                {
                    if (value <= 0.0f)
                        throw new ArgumentOutOfRangeException("repeatInterval", "repeatInterval must be greater than 0.0f: " + value);
                    m_RepeatInterval = value;
                }
            }

            // The chance a burst will trigger.
            public float probability
            {
                get
                {
                    return 1.0f - m_InvProbability;
                }
                set
                {
                    if (value < 0.0f || value > 1.0f)
                        throw new ArgumentOutOfRangeException("probability", "probability must be between 0.0f and 1.0f: " + value);
                    m_InvProbability = 1.0f - value;
                }
            }

            private float m_Time;
            private MinMaxCurve m_Count;
            private int m_RepeatCount; // externally, we use "cycles", because users preferred that, but internally, we must use something that defaults to 0, due to C# struct rules
            private float m_RepeatInterval;
            private float m_InvProbability; // internally, we must use something that defaults to 0, due to C# struct rules, so reverse the storage from 0-1 to 1-0
        }

        [Serializable, NativeType(CodegenOptions.Custom, "MonoMinMaxCurve", Header = "Runtime/Scripting/ScriptingCommonStructDefinitions.h")]
        public partial struct MinMaxCurve
        {
            public MinMaxCurve(float constant) { m_Mode = ParticleSystemCurveMode.Constant; m_CurveMultiplier = 0.0f; m_CurveMin = null; m_CurveMax = null; m_ConstantMin = 0.0f; m_ConstantMax = constant; }
            public MinMaxCurve(float multiplier, AnimationCurve curve) { m_Mode = ParticleSystemCurveMode.Curve; m_CurveMultiplier = multiplier; m_CurveMin = null; m_CurveMax = curve; m_ConstantMin = 0.0f; m_ConstantMax = 0.0f; }
            public MinMaxCurve(float multiplier, AnimationCurve min, AnimationCurve max) { m_Mode = ParticleSystemCurveMode.TwoCurves; m_CurveMultiplier = multiplier; m_CurveMin = min; m_CurveMax = max; m_ConstantMin = 0.0f; m_ConstantMax = 0.0f; }
            public MinMaxCurve(float min, float max) { m_Mode = ParticleSystemCurveMode.TwoConstants; m_CurveMultiplier = 0.0f; m_CurveMin = null; m_CurveMax = null; m_ConstantMin = min; m_ConstantMax = max; }

            public ParticleSystemCurveMode mode { get { return m_Mode; } set { m_Mode = value; } }                      // The current curve mode.
            public float curveMultiplier { get { return m_CurveMultiplier; } set { m_CurveMultiplier = value; } }       // The multiplier applied to the 0-1 curves.
            public AnimationCurve curveMax { get { return m_CurveMax; } set { m_CurveMax = value; } }                   // The maximum curve.
            public AnimationCurve curveMin { get { return m_CurveMin; } set { m_CurveMin = value; } }                   // The minimum curve.
            public float constantMax { get { return m_ConstantMax; } set { m_ConstantMax = value; } }                   // The maximum constant.
            public float constantMin { get { return m_ConstantMin; } set { m_ConstantMin = value; } }                   // The minimum constant.
            public float constant { get { return m_ConstantMax; } set { m_ConstantMax = value; } }                      // The single constant.
            public AnimationCurve curve { get { return m_CurveMax; } set { m_CurveMax = value; } }                      // The single curve.

            // Evaluate the curve
            public float Evaluate(float time) { return Evaluate(time, 1.0f); }
            public float Evaluate(float time, float lerpFactor)
            {
                switch (mode)
                {
                    case ParticleSystemCurveMode.Constant:
                        return m_ConstantMax;
                    case ParticleSystemCurveMode.TwoCurves:
                        return Mathf.Lerp(m_CurveMin.Evaluate(time), m_CurveMax.Evaluate(time), lerpFactor) * m_CurveMultiplier;
                    case ParticleSystemCurveMode.TwoConstants:
                        return Mathf.Lerp(m_ConstantMin, m_ConstantMax, lerpFactor);
                    default: // ParticleSystemCurveMode.Curve:
                        return m_CurveMax.Evaluate(time) * m_CurveMultiplier;
                }
            }

            // Implicit conversion operator, to allow better syntax when using 1 float
            static public implicit operator MinMaxCurve(float constant)
            {
                return new MinMaxCurve(constant);
            }

            [SerializeField] private ParticleSystemCurveMode m_Mode;
            [SerializeField] private float m_CurveMultiplier;
            [SerializeField] private AnimationCurve m_CurveMin;
            [SerializeField] private AnimationCurve m_CurveMax;
            [SerializeField] private float m_ConstantMin;
            [SerializeField] private float m_ConstantMax;
        }

        [Serializable, NativeType(CodegenOptions.Custom, "MonoMinMaxGradient", Header = "Runtime/Scripting/ScriptingCommonStructDefinitions.h")]
        public partial struct MinMaxGradient
        {
            public MinMaxGradient(Color color) { m_Mode = ParticleSystemGradientMode.Color; m_GradientMin = null; m_GradientMax = null; m_ColorMin = Color.black; m_ColorMax = color; }
            public MinMaxGradient(Gradient gradient) { m_Mode = ParticleSystemGradientMode.Gradient; m_GradientMin = null; m_GradientMax = gradient; m_ColorMin = Color.black; m_ColorMax = Color.black; }
            public MinMaxGradient(Color min, Color max) { m_Mode = ParticleSystemGradientMode.TwoColors; m_GradientMin = null; m_GradientMax = null; m_ColorMin = min; m_ColorMax = max; }
            public MinMaxGradient(Gradient min, Gradient max) { m_Mode = ParticleSystemGradientMode.TwoGradients; m_GradientMin = min; m_GradientMax = max; m_ColorMin = Color.black; m_ColorMax = Color.black; }

            public ParticleSystemGradientMode mode { get { return m_Mode; } set { m_Mode = value; } }           // The current gradient mode.
            public Gradient gradientMax { get { return m_GradientMax; } set { m_GradientMax = value; } }        // The maximum gradient.
            public Gradient gradientMin { get { return m_GradientMin; } set { m_GradientMin = value; } }        // The minimum gradient.
            public Color colorMax { get { return m_ColorMax; } set { m_ColorMax = value; } }                    // The maximum color.
            public Color colorMin { get { return m_ColorMin; } set { m_ColorMin = value; } }                    // The minimum color.
            public Color color { get { return m_ColorMax; } set { m_ColorMax = value; } }                       // The single color.
            public Gradient gradient { get { return m_GradientMax; } set { m_GradientMax = value; } }           // The single gradient.

            // Evaluate the gradient
            public Color Evaluate(float time) { return Evaluate(time, 1.0f); }
            public Color Evaluate(float time, float lerpFactor)
            {
                switch (m_Mode)
                {
                    case ParticleSystemGradientMode.Color:
                        return m_ColorMax;
                    case ParticleSystemGradientMode.TwoColors:
                        return Color.Lerp(m_ColorMin, m_ColorMax, lerpFactor);
                    case ParticleSystemGradientMode.TwoGradients:
                        return Color.Lerp(m_GradientMin.Evaluate(time), m_GradientMax.Evaluate(time), lerpFactor);
                    case ParticleSystemGradientMode.RandomColor:
                        return m_GradientMax.Evaluate(lerpFactor);
                    default: // ParticleSystemGradientMode.Gradient
                        return m_GradientMax.Evaluate(time);
                }
            }

            // Implicit conversion operator, to allow better syntax when using 1 color or 1 gradient
            static public implicit operator MinMaxGradient(Color color)
            {
                return new MinMaxGradient(color);
            }

            static public implicit operator MinMaxGradient(Gradient gradient)
            {
                return new MinMaxGradient(gradient);
            }

            [SerializeField] private ParticleSystemGradientMode m_Mode;
            [SerializeField] private Gradient m_GradientMin;
            [SerializeField] private Gradient m_GradientMax;
            [SerializeField] private Color m_ColorMin;
            [SerializeField] private Color m_ColorMax;
        }

        [RequiredByNativeCode("particleSystemParticle", Optional = true)]
        [StructLayout(LayoutKind.Sequential)]
        public partial struct Particle
        {
            [Flags]
            private enum Flags
            {
                Size3D = 1 << 0,
                Rotation3D = 1 << 1,
                MeshIndex = 1 << 2
            }

            public Vector3 position { get { return m_Position; } set { m_Position = value; } }
            public Vector3 velocity { get { return m_Velocity; } set { m_Velocity = value; } }
            public Vector3 animatedVelocity { get { return m_AnimatedVelocity; } }
            public Vector3 totalVelocity { get { return m_Velocity + m_AnimatedVelocity; } }
            public float remainingLifetime { get { return m_Lifetime; } set { m_Lifetime = value; } }
            public float startLifetime { get { return m_StartLifetime; } set { m_StartLifetime = value; } }
            public Color32 startColor { get { return m_StartColor; } set { m_StartColor = value; } }
            public UInt32 randomSeed { get { return m_RandomSeed; } set { m_RandomSeed = value; } }
            public Vector3 axisOfRotation { get { return m_AxisOfRotation; } set { m_AxisOfRotation = value; } }

            public float startSize { get { return m_StartSize.x; } set { m_StartSize = new Vector3(value, value, value); } }
            public Vector3 startSize3D { get { return m_StartSize; } set { m_StartSize = value; m_Flags |= (UInt32)Flags.Size3D; } }

            public float rotation { get { return m_Rotation.z * Mathf.Rad2Deg; } set { m_Rotation = new Vector3(0.0f, 0.0f, value * Mathf.Deg2Rad); } }
            public Vector3 rotation3D { get { return m_Rotation * Mathf.Rad2Deg; } set { m_Rotation = value * Mathf.Deg2Rad; m_Flags |= (UInt32)Flags.Rotation3D; } }

            public float angularVelocity { get { return m_AngularVelocity.z * Mathf.Rad2Deg; } set { m_AngularVelocity = new Vector3(0.0f, 0.0f, value * Mathf.Deg2Rad); } }
            public Vector3 angularVelocity3D { get { return m_AngularVelocity * Mathf.Rad2Deg; } set { m_AngularVelocity = value * Mathf.Deg2Rad; m_Flags |= (UInt32)Flags.Rotation3D; } }

            public float GetCurrentSize(ParticleSystem system) { return system.GetParticleCurrentSize(ref this); }              // The current (curve-corrected) size of the particle.
            public Vector3 GetCurrentSize3D(ParticleSystem system) { return system.GetParticleCurrentSize3D(ref this); }        // The current (curve-corrected) 3D size of the particle.
            public Color32 GetCurrentColor(ParticleSystem system) { return system.GetParticleCurrentColor(ref this); }          // The current (curve-corrected) color of the particle.

            public void SetMeshIndex(int index) { m_MeshIndex = index; m_Flags |= (UInt32)Flags.MeshIndex; }
            public int GetMeshIndex(ParticleSystem system) { return system.GetParticleMeshIndex(ref this); }                    // Clamped based on the mesh count in the Renderer Module

            private Vector3 m_Position;
            private Vector3 m_Velocity;
            private Vector3 m_AnimatedVelocity;
            private Vector3 m_InitialVelocity;
            private Vector3 m_AxisOfRotation;
            private Vector3 m_Rotation;
            private Vector3 m_AngularVelocity;
            private Vector3 m_StartSize;
            private Color32 m_StartColor;
            private UInt32 m_RandomSeed;
            private UInt32 m_ParentRandomSeed;
            private float m_Lifetime;
            private float m_StartLifetime;
            private int m_MeshIndex;
            private float m_EmitAccumulator0;
            private float m_EmitAccumulator1;
            private UInt32 m_Flags;
        }

        // Script interface for emitting Particles, whilst allowing for overriding of some/all properties
        [StructLayout(LayoutKind.Sequential)]
        public partial struct EmitParams
        {
            public Particle particle
            {
                get
                {
                    return m_Particle;
                }

                set
                {
                    m_Particle = value;

                    m_PositionSet = true;
                    m_VelocitySet = true;
                    m_AxisOfRotationSet = true;
                    m_RotationSet = true;
                    m_AngularVelocitySet = true;
                    m_StartSizeSet = true;
                    m_StartColorSet = true;
                    m_RandomSeedSet = true;
                    m_StartLifetimeSet = true;
                    m_MeshIndexSet = true;
                }
            }

            public Vector3 position { get { return m_Particle.position; } set { m_Particle.position = value; m_PositionSet = true; } }
            public bool applyShapeToPosition { get { return m_ApplyShapeToPosition; } set { m_ApplyShapeToPosition = value; } }
            public Vector3 velocity { get { return m_Particle.velocity; } set { m_Particle.velocity = value; m_VelocitySet = true; } }
            public float startLifetime { get { return m_Particle.startLifetime; } set { m_Particle.startLifetime = value; m_StartLifetimeSet = true; } }
            public float startSize { get { return m_Particle.startSize; } set { m_Particle.startSize = value; m_StartSizeSet = true; } }
            public Vector3 startSize3D { get { return m_Particle.startSize3D; } set { m_Particle.startSize3D = value; m_StartSizeSet = true; } }
            public Vector3 axisOfRotation { get { return m_Particle.axisOfRotation; } set { m_Particle.axisOfRotation = value; m_AxisOfRotationSet = true; } }
            public float rotation { get { return m_Particle.rotation; } set { m_Particle.rotation = value; m_RotationSet = true; } }
            public Vector3 rotation3D { get { return m_Particle.rotation3D; } set { m_Particle.rotation3D = value; m_RotationSet = true; } }
            public float angularVelocity { get { return m_Particle.angularVelocity; } set { m_Particle.angularVelocity = value; m_AngularVelocitySet = true; } }
            public Vector3 angularVelocity3D { get { return m_Particle.angularVelocity3D; } set { m_Particle.angularVelocity3D = value; m_AngularVelocitySet = true; } }
            public Color32 startColor { get { return m_Particle.startColor; } set { m_Particle.startColor = value; m_StartColorSet = true; } }
            public UInt32 randomSeed { get { return m_Particle.randomSeed; } set { m_Particle.randomSeed = value; m_RandomSeedSet = true; } }
            public int meshIndex { set { m_Particle.SetMeshIndex(value); m_MeshIndexSet = true; } }

            public void ResetPosition() { m_PositionSet = false; }
            public void ResetVelocity() { m_VelocitySet = false; }
            public void ResetAxisOfRotation() { m_AxisOfRotationSet = false; }
            public void ResetRotation() { m_RotationSet = false; }
            public void ResetAngularVelocity() { m_AngularVelocitySet = false; }
            public void ResetStartSize() { m_StartSizeSet = false; }
            public void ResetStartColor() { m_StartColorSet = false; }
            public void ResetRandomSeed() { m_RandomSeedSet = false; }
            public void ResetStartLifetime() { m_StartLifetimeSet = false; }
            public void ResetMeshIndex() { m_MeshIndexSet = false; }

            [NativeName("particle")] private Particle m_Particle;
            [NativeName("positionSet")] private bool m_PositionSet;
            [NativeName("velocitySet")] private bool m_VelocitySet;
            [NativeName("axisOfRotationSet")] private bool m_AxisOfRotationSet;
            [NativeName("rotationSet")] private bool m_RotationSet;
            [NativeName("rotationalSpeedSet")] private bool m_AngularVelocitySet;
            [NativeName("startSizeSet")] private bool m_StartSizeSet;
            [NativeName("startColorSet")] private bool m_StartColorSet;
            [NativeName("randomSeedSet")] private bool m_RandomSeedSet;
            [NativeName("startLifetimeSet")] private bool m_StartLifetimeSet;
            [NativeName("meshIndexSet")] private bool m_MeshIndexSet;
            [NativeName("applyShapeToPosition")] private bool m_ApplyShapeToPosition;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PlaybackState
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct Seed
            {
                public UInt32 x, y, z, w;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Seed4
            {
                public Seed x, y, z, w;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Emission
            {
                public float m_ParticleSpacing;
                public float m_ToEmitAccumulator;
                public Seed m_Random;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Initial
            {
                public Seed4 m_Random;
            };

            [StructLayout(LayoutKind.Sequential)]
            internal struct Shape
            {
                public Seed4 m_Random;
                public float m_RadiusTimer;
                public float m_RadiusTimerPrev;
                public float m_ArcTimer;
                public float m_ArcTimerPrev;
                public float m_MeshSpawnTimer;
                public float m_MeshSpawnTimerPrev;
                public int m_OrderedMeshVertexIndex;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Force
            {
                public Seed4 m_Random;
            };

            [StructLayout(LayoutKind.Sequential)]
            internal struct Collision
            {
                public Seed4 m_Random;
            };

            [StructLayout(LayoutKind.Sequential)]
            internal struct Noise
            {
                public float m_ScrollOffset;
            };

            [StructLayout(LayoutKind.Sequential)]
            internal struct Lights
            {
                public Seed m_Random;
                public float m_ParticleEmissionCounter;
            };

            [StructLayout(LayoutKind.Sequential)]
            internal struct Trail
            {
                public float m_Timer;
            };

            internal float m_AccumulatedDt;
            internal float m_StartDelay;
            internal float m_PlaybackTime;
            internal int m_RingBufferIndex;
            internal Emission m_Emission;
            internal Initial m_Initial;
            internal Shape m_Shape;
            internal Force m_Force;
            internal Collision m_Collision;
            internal Noise m_Noise;
            internal Lights m_Lights;
            internal Trail m_Trail;
        }

        [NativeType(CodegenOptions.Custom, "MonoParticleTrails")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Trails
        {
            internal List<Vector4> positions;
            internal List<int> frontPositions;
            internal List<int> backPositions;
            internal List<int> positionCounts;
            internal int maxTrailCount;
            internal int maxPositionsPerTrailCount;
        }
    }

    [RequiredByNativeCode(Optional = true)]
    public partial struct ParticleCollisionEvent
    {
        internal Vector3 m_Intersection;
        internal Vector3 m_Normal;
        internal Vector3 m_Velocity;
        internal int m_ColliderInstanceID;

        public Vector3 intersection { get { return m_Intersection; } }
        public Vector3 normal { get { return m_Normal; } }
        public Vector3 velocity { get { return m_Velocity; } }

        public Component colliderComponent { get { return InstanceIDToColliderComponent(m_ColliderInstanceID); } }
    }
}
