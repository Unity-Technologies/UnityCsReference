// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Collections.Generic;

namespace UnityEngine
{
[RequireComponent(typeof(Transform))]
[UsedByNativeCode]
public sealed partial class ParticleSystem : Component
{
    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct MainModule
    {
        
                    internal MainModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public float duration { get { return GetDuration(m_ParticleSystem); } set { SetDuration(m_ParticleSystem, value); } }
                    public bool loop { get { return GetLoop(m_ParticleSystem); } set { SetLoop(m_ParticleSystem, value); } }
                    public bool prewarm { get { return GetPrewarm(m_ParticleSystem); } set { SetPrewarm(m_ParticleSystem, value); } }
                    public MinMaxCurve startDelay { set { SetStartDelay(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStartDelay(m_ParticleSystem, ref r); return r; } }
                    public float startDelayMultiplier { get { return GetStartDelayMultiplier(m_ParticleSystem); } set { SetStartDelayMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve startLifetime { set { SetStartLifetime(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStartLifetime(m_ParticleSystem, ref r); return r; } }
                    public float startLifetimeMultiplier { get { return GetStartLifetimeMultiplier(m_ParticleSystem); } set { SetStartLifetimeMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve startSpeed { set { SetStartSpeed(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStartSpeed(m_ParticleSystem, ref r); return r; } }
                    public float startSpeedMultiplier { get { return GetStartSpeedMultiplier(m_ParticleSystem); } set { SetStartSpeedMultiplier(m_ParticleSystem, value); } }
                    public bool startSize3D { get { return GetStartSize3D(m_ParticleSystem); } set { SetStartSize3D(m_ParticleSystem, value); } }
                    public MinMaxCurve startSize { set { SetStartSizeX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStartSizeX(m_ParticleSystem, ref r); return r; } }
                    public float startSizeMultiplier { get { return GetStartSizeXMultiplier(m_ParticleSystem); } set { SetStartSizeXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve startSizeX { set { SetStartSizeX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStartSizeX(m_ParticleSystem, ref r); return r; } }
                    public float startSizeXMultiplier { get { return GetStartSizeXMultiplier(m_ParticleSystem); } set { SetStartSizeXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve startSizeY { set { SetStartSizeY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStartSizeY(m_ParticleSystem, ref r); return r; } }
                    public float startSizeYMultiplier { get { return GetStartSizeYMultiplier(m_ParticleSystem); } set { SetStartSizeYMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve startSizeZ { set { SetStartSizeZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStartSizeZ(m_ParticleSystem, ref r); return r; } }
                    public float startSizeZMultiplier { get { return GetStartSizeZMultiplier(m_ParticleSystem); } set { SetStartSizeZMultiplier(m_ParticleSystem, value); } }
                    public bool startRotation3D { get { return GetStartRotation3D(m_ParticleSystem); } set { SetStartRotation3D(m_ParticleSystem, value); } }
                    public MinMaxCurve startRotation { set { SetStartRotationZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStartRotationZ(m_ParticleSystem, ref r); return r; } }
                    public float startRotationMultiplier { get { return GetStartRotationZMultiplier(m_ParticleSystem); } set { SetStartRotationZMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve startRotationX { set { SetStartRotationX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStartRotationX(m_ParticleSystem, ref r); return r; } }
                    public float startRotationXMultiplier { get { return GetStartRotationXMultiplier(m_ParticleSystem); } set { SetStartRotationXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve startRotationY { set { SetStartRotationY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStartRotationY(m_ParticleSystem, ref r); return r; } }
                    public float startRotationYMultiplier { get { return GetStartRotationYMultiplier(m_ParticleSystem); } set { SetStartRotationYMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve startRotationZ { set { SetStartRotationZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStartRotationZ(m_ParticleSystem, ref r); return r; } }
                    public float startRotationZMultiplier { get { return GetStartRotationZMultiplier(m_ParticleSystem); } set { SetStartRotationZMultiplier(m_ParticleSystem, value); } }
                    public float flipRotation { get { return GetFlipRotation(m_ParticleSystem); } set { SetFlipRotation(m_ParticleSystem, value); } }
                    public MinMaxGradient startColor { set { SetStartColor(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxGradient(); GetStartColor(m_ParticleSystem, ref r); return r; } }
                    public MinMaxCurve gravityModifier { set { SetGravityModifier(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetGravityModifier(m_ParticleSystem, ref r); return r; } }
                    public float gravityModifierMultiplier { get { return GetGravityModifierMultiplier(m_ParticleSystem); } set { SetGravityModifierMultiplier(m_ParticleSystem, value); } }
                    public ParticleSystemSimulationSpace simulationSpace { get { return GetSimulationSpace(m_ParticleSystem); } set { SetSimulationSpace(m_ParticleSystem, value); } }
                    public Transform customSimulationSpace { get { return GetCustomSimulationSpace(m_ParticleSystem); } set { SetCustomSimulationSpace(m_ParticleSystem, value); } }
                    public float simulationSpeed { get { return GetSimulationSpeed(m_ParticleSystem); } set { SetSimulationSpeed(m_ParticleSystem, value); } }
                    public bool useUnscaledTime { get { return GetUseUnscaledTime(m_ParticleSystem); } set { SetUseUnscaledTime(m_ParticleSystem, value); } }
                    public ParticleSystemScalingMode scalingMode { get { return GetScalingMode(m_ParticleSystem); } set { SetScalingMode(m_ParticleSystem, value); } }
                    public bool playOnAwake { get { return GetPlayOnAwake(m_ParticleSystem); } set { SetPlayOnAwake(m_ParticleSystem, value); } }
                    public int maxParticles { get { return GetMaxParticles(m_ParticleSystem); } set { SetMaxParticles(m_ParticleSystem, value); } }
                    public ParticleSystemEmitterVelocityMode emitterVelocityMode { get { return GetUseRigidbodyForVelocity(m_ParticleSystem) ? ParticleSystemEmitterVelocityMode.Rigidbody : ParticleSystemEmitterVelocityMode.Transform; } set { SetUseRigidbodyForVelocity(m_ParticleSystem, value == ParticleSystemEmitterVelocityMode.Rigidbody); } }
                    public ParticleSystemStopAction stopAction { get { return GetStopAction(m_ParticleSystem); } set { SetStopAction(m_ParticleSystem, value); } }
                    public ParticleSystemCullingMode cullingMode { get { return GetCullingMode(m_ParticleSystem); } set { SetCullingMode(m_ParticleSystem, value); } }
                    public ParticleSystemRingBufferMode ringBufferMode { get { return GetRingBufferMode(m_ParticleSystem); } set { SetRingBufferMode(m_ParticleSystem, value); } }
                    public Vector2 ringBufferLoopRange { get { return GetRingBufferLoopRange(m_ParticleSystem); } set { SetRingBufferLoopRange(m_ParticleSystem, value); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetDuration (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetDuration (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetLoop (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetLoop (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetPrewarm (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetPrewarm (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartDelay (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStartDelay (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartDelayMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStartDelayMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartLifetime (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStartLifetime (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartLifetimeMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStartLifetimeMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartSpeed (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStartSpeed (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartSpeedMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStartSpeedMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartSize3D (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetStartSize3D (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartSizeX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStartSizeX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartSizeXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStartSizeXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartSizeY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStartSizeY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartSizeYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStartSizeYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartSizeZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStartSizeZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartSizeZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStartSizeZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartRotation3D (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetStartRotation3D (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartRotationX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStartRotationX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartRotationXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStartRotationXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartRotationY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStartRotationY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartRotationYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStartRotationYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartRotationZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStartRotationZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartRotationZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStartRotationZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetFlipRotation (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetFlipRotation (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartColor (ParticleSystem system, ref ParticleSystem.MinMaxGradient gradient) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStartColor (ParticleSystem system, ref ParticleSystem.MinMaxGradient gradient) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetGravityModifier (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetGravityModifier (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetGravityModifierMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetGravityModifierMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSimulationSpace (ParticleSystem system, ParticleSystemSimulationSpace value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemSimulationSpace GetSimulationSpace (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetCustomSimulationSpace (ParticleSystem system, Transform value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  Transform GetCustomSimulationSpace (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSimulationSpeed (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetSimulationSpeed (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetUseUnscaledTime (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetUseUnscaledTime (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetScalingMode (ParticleSystem system, ParticleSystemScalingMode value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemScalingMode GetScalingMode (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetPlayOnAwake (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetPlayOnAwake (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMaxParticles (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetMaxParticles (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetUseRigidbodyForVelocity (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetUseRigidbodyForVelocity (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStopAction (ParticleSystem system, ParticleSystemStopAction value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemStopAction GetStopAction (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetCullingMode (ParticleSystem system, ParticleSystemCullingMode value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemCullingMode GetCullingMode (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRingBufferMode (ParticleSystem system, ParticleSystemRingBufferMode value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemRingBufferMode GetRingBufferMode (ParticleSystem system) ;

        private static void SetRingBufferLoopRange (ParticleSystem system, Vector2 value) {
            INTERNAL_CALL_SetRingBufferLoopRange ( system, ref value );
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_SetRingBufferLoopRange (ParticleSystem system, ref Vector2 value);
        private static Vector2 GetRingBufferLoopRange (ParticleSystem system) {
            Vector2 result;
            INTERNAL_CALL_GetRingBufferLoopRange ( system, out result );
            return result;
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_GetRingBufferLoopRange (ParticleSystem system, out Vector2 value);
    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct EmissionModule
    {
        
                    internal EmissionModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public MinMaxCurve rateOverTime { set { SetRateOverTime(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetRateOverTime(m_ParticleSystem, ref r); return r; } }
                    public float rateOverTimeMultiplier { get { return GetRateOverTimeMultiplier(m_ParticleSystem); } set { SetRateOverTimeMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve rateOverDistance { set { SetRateOverDistance(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetRateOverDistance(m_ParticleSystem, ref r); return r; } }
                    public float rateOverDistanceMultiplier { get { return GetRateOverDistanceMultiplier(m_ParticleSystem); } set { SetRateOverDistanceMultiplier(m_ParticleSystem, value); } }
        
        public void SetBursts(Burst[] bursts)
            {
                SetBursts(bursts, bursts.Length);
            }
        
        public void SetBursts(Burst[] bursts, int size)
            {
                burstCount = size;
                for (int i = 0; i < size; i++)
                {
                    SetBurst(m_ParticleSystem, i, bursts[i]);
                }
            }
        
        public int GetBursts(Burst[] bursts)
            {
                int returnValue = burstCount;
                for (int i = 0; i < returnValue; i++)
                {
                    bursts[i] = GetBurst(m_ParticleSystem, i);
                }
                return returnValue;
            }
        
        public void SetBurst(int index, Burst burst) { SetBurst(m_ParticleSystem, index, burst); }
        public Burst GetBurst(int index) { return GetBurst(m_ParticleSystem, index); }
        
                    public int burstCount
            {
                get { return GetBurstCount(m_ParticleSystem); }
                set { SetBurstCount(m_ParticleSystem, value); }
            }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetBurstCount (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRateOverTime (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetRateOverTime (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRateOverTimeMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRateOverTimeMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRateOverDistance (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetRateOverDistance (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRateOverDistanceMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRateOverDistanceMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetBurstCount (ParticleSystem system, int value) ;

        private static void SetBurst (ParticleSystem system, int index, ParticleSystem.Burst burst) {
            INTERNAL_CALL_SetBurst ( system, index, ref burst );
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_SetBurst (ParticleSystem system, int index, ref ParticleSystem.Burst burst);
        private static ParticleSystem.Burst GetBurst (ParticleSystem system, int index) {
            ParticleSystem.Burst result;
            INTERNAL_CALL_GetBurst ( system, index, out result );
            return result;
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_GetBurst (ParticleSystem system, int index, out ParticleSystem.Burst value);
    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ShapeModule
    {
        
                    internal ShapeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public ParticleSystemShapeType shapeType { get { return GetShapeType(m_ParticleSystem); } set { SetShapeType(m_ParticleSystem, value); } }
                    public float randomDirectionAmount { get { return GetRandomDirectionAmount(m_ParticleSystem); } set { SetRandomDirectionAmount(m_ParticleSystem, value); } }
                    public float sphericalDirectionAmount { get { return GetSphericalDirectionAmount(m_ParticleSystem); } set { SetSphericalDirectionAmount(m_ParticleSystem, value); } }
                    public float randomPositionAmount { get { return GetRandomPositionAmount(m_ParticleSystem); } set { SetRandomPositionAmount(m_ParticleSystem, value); } }
                    public bool alignToDirection { get { return GetAlignToDirection(m_ParticleSystem); } set { SetAlignToDirection(m_ParticleSystem, value); } }
                    public float radius { get { return GetRadius(m_ParticleSystem); } set { SetRadius(m_ParticleSystem, value); } }
                    public ParticleSystemShapeMultiModeValue radiusMode { get { return GetRadiusMode(m_ParticleSystem); } set { SetRadiusMode(m_ParticleSystem, value); } }
                    public float radiusSpread { get { return GetRadiusSpread(m_ParticleSystem); } set { SetRadiusSpread(m_ParticleSystem, value); } }
                    public MinMaxCurve radiusSpeed { set { SetRadiusSpeed(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetRadiusSpeed(m_ParticleSystem, ref r); return r; } }
                    public float radiusSpeedMultiplier { get { return GetRadiusSpeedMultiplier(m_ParticleSystem); } set { SetRadiusSpeedMultiplier(m_ParticleSystem, value); } }
                    public float radiusThickness { get { return GetRadiusThickness(m_ParticleSystem); } set { SetRadiusThickness(m_ParticleSystem, value); } }
                    public float angle { get { return GetAngle(m_ParticleSystem); } set { SetAngle(m_ParticleSystem, value); } }
                    public float length { get { return GetLength(m_ParticleSystem); } set { SetLength(m_ParticleSystem, value); } }
                    public Vector3 boxThickness { get { return GetBoxThickness(m_ParticleSystem); } set { SetBoxThickness(m_ParticleSystem, value); } }
                    public ParticleSystemMeshShapeType meshShapeType { get { return GetMeshShapeType(m_ParticleSystem); } set { SetMeshShapeType(m_ParticleSystem, value); } }
                    public Mesh mesh { get { return GetMesh(m_ParticleSystem); } set { SetMesh(m_ParticleSystem, value); } }
                    public MeshRenderer meshRenderer { get { return GetMeshRenderer(m_ParticleSystem); } set { SetMeshRenderer(m_ParticleSystem, value); } }
                    public SkinnedMeshRenderer skinnedMeshRenderer { get { return GetSkinnedMeshRenderer(m_ParticleSystem); } set { SetSkinnedMeshRenderer(m_ParticleSystem, value); } }
                    public Sprite sprite { get { return GetSprite(m_ParticleSystem); } set { SetSprite(m_ParticleSystem, value); } }
                    public SpriteRenderer spriteRenderer { get { return GetSpriteRenderer(m_ParticleSystem); } set { SetSpriteRenderer(m_ParticleSystem, value); } }
                    public bool useMeshMaterialIndex { get { return GetUseMeshMaterialIndex(m_ParticleSystem); } set { SetUseMeshMaterialIndex(m_ParticleSystem, value); } }
                    public int meshMaterialIndex { get { return GetMeshMaterialIndex(m_ParticleSystem); } set { SetMeshMaterialIndex(m_ParticleSystem, value); } }
                    public bool useMeshColors { get { return GetUseMeshColors(m_ParticleSystem); } set { SetUseMeshColors(m_ParticleSystem, value); } }
                    public float normalOffset { get { return GetNormalOffset(m_ParticleSystem); } set { SetNormalOffset(m_ParticleSystem, value); } }
                    public ParticleSystemShapeMultiModeValue meshSpawnMode { get { return GetMeshSpawnMode(m_ParticleSystem); } set { SetMeshSpawnMode(m_ParticleSystem, value); } }
                    public float meshSpawnSpread { get { return GetMeshSpawnSpread(m_ParticleSystem); } set { SetMeshSpawnSpread(m_ParticleSystem, value); } }
                    public MinMaxCurve meshSpawnSpeed { set { SetMeshSpawnSpeed(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetMeshSpawnSpeed(m_ParticleSystem, ref r); return r; } }
                    public float meshSpawnSpeedMultiplier { get { return GetMeshSpawnSpeedMultiplier(m_ParticleSystem); } set { SetMeshSpawnSpeedMultiplier(m_ParticleSystem, value); } }
                    public float arc { get { return GetArc(m_ParticleSystem); } set { SetArc(m_ParticleSystem, value); } }
                    public ParticleSystemShapeMultiModeValue arcMode { get { return GetArcMode(m_ParticleSystem); } set { SetArcMode(m_ParticleSystem, value); } }
                    public float arcSpread { get { return GetArcSpread(m_ParticleSystem); } set { SetArcSpread(m_ParticleSystem, value); } }
                    public MinMaxCurve arcSpeed { set { SetArcSpeed(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetArcSpeed(m_ParticleSystem, ref r); return r; } }
                    public float arcSpeedMultiplier { get { return GetArcSpeedMultiplier(m_ParticleSystem); } set { SetArcSpeedMultiplier(m_ParticleSystem, value); } }
                    public float donutRadius { get { return GetDonutRadius(m_ParticleSystem); } set { SetDonutRadius(m_ParticleSystem, value); } }
                    public Vector3 position { get { return GetPosition(m_ParticleSystem); } set { SetPosition(m_ParticleSystem, value); } }
                    public Vector3 rotation { get { return GetRotation(m_ParticleSystem); } set { SetRotation(m_ParticleSystem, value); } }
                    public Vector3 scale { get { return GetScale(m_ParticleSystem); } set { SetScale(m_ParticleSystem, value); } }
                    public Texture2D texture { get { return GetTexture(m_ParticleSystem); } set { SetTexture(m_ParticleSystem, value); } }
                    public ParticleSystemShapeTextureChannel textureClipChannel { get { return (ParticleSystemShapeTextureChannel)GetTextureClipChannel(m_ParticleSystem); } set { SetTextureClipChannel(m_ParticleSystem, (int)value); } }
                    public float textureClipThreshold { get { return GetTextureClipThreshold(m_ParticleSystem); } set { SetTextureClipThreshold(m_ParticleSystem, value); } }
                    public bool textureColorAffectsParticles { get { return GetTextureColorAffectsParticles(m_ParticleSystem); } set { SetTextureColorAffectsParticles(m_ParticleSystem, value); } }
                    public bool textureAlphaAffectsParticles { get { return GetTextureAlphaAffectsParticles(m_ParticleSystem); } set { SetTextureAlphaAffectsParticles(m_ParticleSystem, value); } }
                    public bool textureBilinearFiltering { get { return GetTextureBilinearFiltering(m_ParticleSystem); } set { SetTextureBilinearFiltering(m_ParticleSystem, value); } }
                    public int textureUVChannel { get { return GetTextureUVChannel(m_ParticleSystem); } set { SetTextureUVChannel(m_ParticleSystem, value); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetShapeType (ParticleSystem system, ParticleSystemShapeType value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemShapeType GetShapeType (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRandomDirectionAmount (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRandomDirectionAmount (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSphericalDirectionAmount (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetSphericalDirectionAmount (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRandomPositionAmount (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRandomPositionAmount (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetAlignToDirection (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetAlignToDirection (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRadius (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRadius (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRadiusMode (ParticleSystem system, ParticleSystemShapeMultiModeValue value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemShapeMultiModeValue GetRadiusMode (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRadiusSpread (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRadiusSpread (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRadiusSpeed (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetRadiusSpeed (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRadiusSpeedMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRadiusSpeedMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRadiusThickness (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRadiusThickness (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetAngle (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetAngle (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetLength (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetLength (ParticleSystem system) ;

        private static void SetBoxThickness (ParticleSystem system, Vector3 value) {
            INTERNAL_CALL_SetBoxThickness ( system, ref value );
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_SetBoxThickness (ParticleSystem system, ref Vector3 value);
        private static Vector3 GetBoxThickness (ParticleSystem system) {
            Vector3 result;
            INTERNAL_CALL_GetBoxThickness ( system, out result );
            return result;
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_GetBoxThickness (ParticleSystem system, out Vector3 value);
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMeshShapeType (ParticleSystem system, ParticleSystemMeshShapeType value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemMeshShapeType GetMeshShapeType (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMesh (ParticleSystem system, Mesh value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  Mesh GetMesh (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMeshRenderer (ParticleSystem system, MeshRenderer value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  MeshRenderer GetMeshRenderer (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSkinnedMeshRenderer (ParticleSystem system, SkinnedMeshRenderer value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  SkinnedMeshRenderer GetSkinnedMeshRenderer (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSprite (ParticleSystem system, Sprite value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  Sprite GetSprite (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSpriteRenderer (ParticleSystem system, SpriteRenderer value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  SpriteRenderer GetSpriteRenderer (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetUseMeshMaterialIndex (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetUseMeshMaterialIndex (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMeshMaterialIndex (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetMeshMaterialIndex (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetUseMeshColors (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetUseMeshColors (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetNormalOffset (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetNormalOffset (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMeshSpawnMode (ParticleSystem system, ParticleSystemShapeMultiModeValue value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemShapeMultiModeValue GetMeshSpawnMode (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMeshSpawnSpread (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetMeshSpawnSpread (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMeshSpawnSpeed (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetMeshSpawnSpeed (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMeshSpawnSpeedMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetMeshSpawnSpeedMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetArc (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetArc (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetArcMode (ParticleSystem system, ParticleSystemShapeMultiModeValue value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemShapeMultiModeValue GetArcMode (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetArcSpread (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetArcSpread (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetArcSpeed (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetArcSpeed (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetArcSpeedMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetArcSpeedMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetDonutRadius (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetDonutRadius (ParticleSystem system) ;

        private static void SetPosition (ParticleSystem system, Vector3 value) {
            INTERNAL_CALL_SetPosition ( system, ref value );
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_SetPosition (ParticleSystem system, ref Vector3 value);
        private static Vector3 GetPosition (ParticleSystem system) {
            Vector3 result;
            INTERNAL_CALL_GetPosition ( system, out result );
            return result;
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_GetPosition (ParticleSystem system, out Vector3 value);
        private static void SetRotation (ParticleSystem system, Vector3 value) {
            INTERNAL_CALL_SetRotation ( system, ref value );
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_SetRotation (ParticleSystem system, ref Vector3 value);
        private static Vector3 GetRotation (ParticleSystem system) {
            Vector3 result;
            INTERNAL_CALL_GetRotation ( system, out result );
            return result;
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_GetRotation (ParticleSystem system, out Vector3 value);
        private static void SetScale (ParticleSystem system, Vector3 value) {
            INTERNAL_CALL_SetScale ( system, ref value );
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_SetScale (ParticleSystem system, ref Vector3 value);
        private static Vector3 GetScale (ParticleSystem system) {
            Vector3 result;
            INTERNAL_CALL_GetScale ( system, out result );
            return result;
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_GetScale (ParticleSystem system, out Vector3 value);
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetTexture (ParticleSystem system, Texture2D value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  Texture2D GetTexture (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetTextureClipChannel (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetTextureClipChannel (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetTextureClipThreshold (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetTextureClipThreshold (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetTextureColorAffectsParticles (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetTextureColorAffectsParticles (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetTextureAlphaAffectsParticles (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetTextureAlphaAffectsParticles (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetTextureBilinearFiltering (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetTextureBilinearFiltering (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetTextureUVChannel (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetTextureUVChannel (ParticleSystem system) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct VelocityOverLifetimeModule
    {
        
                    internal VelocityOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public MinMaxCurve x { set { SetX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetX(m_ParticleSystem, ref r); return r; } }
                    public MinMaxCurve y { set { SetY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetY(m_ParticleSystem, ref r); return r; } }
                    public MinMaxCurve z { set { SetZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetZ(m_ParticleSystem, ref r); return r; } }
                    public float xMultiplier { get { return GetXMultiplier(m_ParticleSystem); } set { SetXMultiplier(m_ParticleSystem, value); } }
                    public float yMultiplier { get { return GetYMultiplier(m_ParticleSystem); } set { SetYMultiplier(m_ParticleSystem, value); } }
                    public float zMultiplier { get { return GetZMultiplier(m_ParticleSystem); } set { SetZMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve orbitalX { set { SetOrbitalX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetOrbitalX(m_ParticleSystem, ref r); return r; } }
                    public MinMaxCurve orbitalY { set { SetOrbitalY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetOrbitalY(m_ParticleSystem, ref r); return r; } }
                    public MinMaxCurve orbitalZ { set { SetOrbitalZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetOrbitalZ(m_ParticleSystem, ref r); return r; } }
                    public float orbitalXMultiplier { get { return GetOrbitalXMultiplier(m_ParticleSystem); } set { SetOrbitalXMultiplier(m_ParticleSystem, value); } }
                    public float orbitalYMultiplier { get { return GetOrbitalYMultiplier(m_ParticleSystem); } set { SetOrbitalYMultiplier(m_ParticleSystem, value); } }
                    public float orbitalZMultiplier { get { return GetOrbitalZMultiplier(m_ParticleSystem); } set { SetOrbitalZMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve orbitalOffsetX { set { SetOrbitalOffsetX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetOrbitalOffsetX(m_ParticleSystem, ref r); return r; } }
                    public MinMaxCurve orbitalOffsetY { set { SetOrbitalOffsetY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetOrbitalOffsetY(m_ParticleSystem, ref r); return r; } }
                    public MinMaxCurve orbitalOffsetZ { set { SetOrbitalOffsetZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetOrbitalOffsetZ(m_ParticleSystem, ref r); return r; } }
                    public float orbitalOffsetXMultiplier { get { return GetOrbitalOffsetXMultiplier(m_ParticleSystem); } set { SetOrbitalOffsetXMultiplier(m_ParticleSystem, value); } }
                    public float orbitalOffsetYMultiplier { get { return GetOrbitalOffsetYMultiplier(m_ParticleSystem); } set { SetOrbitalOffsetYMultiplier(m_ParticleSystem, value); } }
                    public float orbitalOffsetZMultiplier { get { return GetOrbitalOffsetZMultiplier(m_ParticleSystem); } set { SetOrbitalOffsetZMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve radial { set { SetRadial(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetRadial(m_ParticleSystem, ref r); return r; } }
                    public float radialMultiplier { get { return GetRadialMultiplier(m_ParticleSystem); } set { SetRadialMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve speedModifier { set { SetSpeedModifier(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetSpeedModifier(m_ParticleSystem, ref r); return r; } }
                    public float speedModifierMultiplier { get { return GetSpeedModifierMultiplier(m_ParticleSystem); } set { SetSpeedModifierMultiplier(m_ParticleSystem, value); } }
                    public ParticleSystemSimulationSpace space { get { return GetWorldSpace(m_ParticleSystem) ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local; } set { SetWorldSpace(m_ParticleSystem, value == ParticleSystemSimulationSpace.World); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOrbitalX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetOrbitalX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOrbitalY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetOrbitalY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOrbitalZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetOrbitalZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOrbitalXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetOrbitalXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOrbitalYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetOrbitalYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOrbitalZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetOrbitalZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOrbitalOffsetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetOrbitalOffsetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOrbitalOffsetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetOrbitalOffsetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOrbitalOffsetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetOrbitalOffsetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOrbitalOffsetXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetOrbitalOffsetXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOrbitalOffsetYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetOrbitalOffsetYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOrbitalOffsetZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetOrbitalOffsetZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRadial (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetRadial (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRadialMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRadialMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSpeedModifier (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetSpeedModifier (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSpeedModifierMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetSpeedModifierMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetWorldSpace (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetWorldSpace (ParticleSystem system) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct LimitVelocityOverLifetimeModule
    {
        
                    internal LimitVelocityOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public MinMaxCurve limitX { set { SetX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetX(m_ParticleSystem, ref r); return r; } }
                    public float limitXMultiplier { get { return GetXMultiplier(m_ParticleSystem); } set { SetXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve limitY { set { SetY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetY(m_ParticleSystem, ref r); return r; } }
                    public float limitYMultiplier { get { return GetYMultiplier(m_ParticleSystem); } set { SetYMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve limitZ { set { SetZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetZ(m_ParticleSystem, ref r); return r; } }
                    public float limitZMultiplier { get { return GetZMultiplier(m_ParticleSystem); } set { SetZMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve limit { set { SetMagnitude(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetMagnitude(m_ParticleSystem, ref r); return r; } }
                    public float limitMultiplier { get { return GetMagnitudeMultiplier(m_ParticleSystem); } set { SetMagnitudeMultiplier(m_ParticleSystem, value); } }
                    public float dampen { get { return GetDampen(m_ParticleSystem); } set { SetDampen(m_ParticleSystem, value); } }
                    public bool separateAxes { get { return GetSeparateAxes(m_ParticleSystem); } set { SetSeparateAxes(m_ParticleSystem, value); } }
                    public ParticleSystemSimulationSpace space { get { return GetWorldSpace(m_ParticleSystem) ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local; } set { SetWorldSpace(m_ParticleSystem, value == ParticleSystemSimulationSpace.World); } }
                    public MinMaxCurve drag { set { SetDrag(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetDrag(m_ParticleSystem, ref r); return r; } }
                    public float dragMultiplier { get { return GetDragMultiplier(m_ParticleSystem); } set { SetDragMultiplier(m_ParticleSystem, value); } }
                    public bool multiplyDragByParticleSize { get { return GetMultiplyDragByParticleSize(m_ParticleSystem); } set { SetMultiplyDragByParticleSize(m_ParticleSystem, value); } }
                    public bool multiplyDragByParticleVelocity { get { return GetMultiplyDragByParticleVelocity(m_ParticleSystem); } set { SetMultiplyDragByParticleVelocity(m_ParticleSystem, value); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMagnitude (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetMagnitude (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMagnitudeMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetMagnitudeMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetDampen (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetDampen (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSeparateAxes (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetSeparateAxes (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetWorldSpace (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetWorldSpace (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetDrag (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetDrag (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetDragMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetDragMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMultiplyDragByParticleSize (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetMultiplyDragByParticleSize (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMultiplyDragByParticleVelocity (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetMultiplyDragByParticleVelocity (ParticleSystem system) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct InheritVelocityModule
    {
        
                    internal InheritVelocityModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public ParticleSystemInheritVelocityMode mode { get { return GetMode(m_ParticleSystem); } set { SetMode(m_ParticleSystem, value); } }
                    public MinMaxCurve curve { set { SetCurve(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetCurve(m_ParticleSystem, ref r); return r; } }
                    public float curveMultiplier { get { return GetCurveMultiplier(m_ParticleSystem); } set { SetCurveMultiplier(m_ParticleSystem, value); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMode (ParticleSystem system, ParticleSystemInheritVelocityMode value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemInheritVelocityMode GetMode (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetCurve (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetCurve (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetCurveMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetCurveMultiplier (ParticleSystem system) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ForceOverLifetimeModule
    {
        
                    internal ForceOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public MinMaxCurve x { set { SetX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetX(m_ParticleSystem, ref r); return r; } }
                    public MinMaxCurve y { set { SetY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetY(m_ParticleSystem, ref r); return r; } }
                    public MinMaxCurve z { set { SetZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetZ(m_ParticleSystem, ref r); return r; } }
                    public float xMultiplier { get { return GetXMultiplier(m_ParticleSystem); } set { SetXMultiplier(m_ParticleSystem, value); } }
                    public float yMultiplier { get { return GetYMultiplier(m_ParticleSystem); } set { SetYMultiplier(m_ParticleSystem, value); } }
                    public float zMultiplier { get { return GetZMultiplier(m_ParticleSystem); } set { SetZMultiplier(m_ParticleSystem, value); } }
                    public ParticleSystemSimulationSpace space { get { return GetWorldSpace(m_ParticleSystem) ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local; } set { SetWorldSpace(m_ParticleSystem, value == ParticleSystemSimulationSpace.World); } }
                    public bool randomized { set { SetRandomized(m_ParticleSystem, value); } get { return GetRandomized(m_ParticleSystem); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetWorldSpace (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetWorldSpace (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRandomized (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetRandomized (ParticleSystem system) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ColorOverLifetimeModule
    {
        
                    internal ColorOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public MinMaxGradient color { set { SetColor(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxGradient(); GetColor(m_ParticleSystem, ref r); return r; } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetColor (ParticleSystem system, ref ParticleSystem.MinMaxGradient gradient) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetColor (ParticleSystem system, ref ParticleSystem.MinMaxGradient gradient) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ColorBySpeedModule
    {
        
                    internal ColorBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public MinMaxGradient color { set { SetColor(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxGradient(); GetColor(m_ParticleSystem, ref r); return r; } }
                    public Vector2 range { set { SetRange(m_ParticleSystem, value); } get { return GetRange(m_ParticleSystem); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetColor (ParticleSystem system, ref ParticleSystem.MinMaxGradient gradient) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetColor (ParticleSystem system, ref ParticleSystem.MinMaxGradient gradient) ;

        private static void SetRange (ParticleSystem system, Vector2 value) {
            INTERNAL_CALL_SetRange ( system, ref value );
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_SetRange (ParticleSystem system, ref Vector2 value);
        private static Vector2 GetRange (ParticleSystem system) {
            Vector2 result;
            INTERNAL_CALL_GetRange ( system, out result );
            return result;
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_GetRange (ParticleSystem system, out Vector2 value);
    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SizeOverLifetimeModule
    {
        
                    internal SizeOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public MinMaxCurve size { set { SetX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetX(m_ParticleSystem, ref r); return r; } }
                    public float sizeMultiplier { get { return GetXMultiplier(m_ParticleSystem); } set { SetXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve x { set { SetX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetX(m_ParticleSystem, ref r); return r; } }
                    public float xMultiplier { get { return GetXMultiplier(m_ParticleSystem); } set { SetXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve y { set { SetY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetY(m_ParticleSystem, ref r); return r; } }
                    public float yMultiplier { get { return GetYMultiplier(m_ParticleSystem); } set { SetYMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve z { set { SetZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetZ(m_ParticleSystem, ref r); return r; } }
                    public float zMultiplier { get { return GetZMultiplier(m_ParticleSystem); } set { SetZMultiplier(m_ParticleSystem, value); } }
                    public bool separateAxes { get { return GetSeparateAxes(m_ParticleSystem); } set { SetSeparateAxes(m_ParticleSystem, value); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSeparateAxes (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetSeparateAxes (ParticleSystem system) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SizeBySpeedModule
    {
        
                    internal SizeBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public MinMaxCurve size { set { SetX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetX(m_ParticleSystem, ref r); return r; } }
                    public float sizeMultiplier { get { return GetXMultiplier(m_ParticleSystem); } set { SetXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve x { set { SetX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetX(m_ParticleSystem, ref r); return r; } }
                    public float xMultiplier { get { return GetXMultiplier(m_ParticleSystem); } set { SetXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve y { set { SetY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetY(m_ParticleSystem, ref r); return r; } }
                    public float yMultiplier { get { return GetYMultiplier(m_ParticleSystem); } set { SetYMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve z { set { SetZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetZ(m_ParticleSystem, ref r); return r; } }
                    public float zMultiplier { get { return GetZMultiplier(m_ParticleSystem); } set { SetZMultiplier(m_ParticleSystem, value); } }
                    public bool separateAxes { get { return GetSeparateAxes(m_ParticleSystem); } set { SetSeparateAxes(m_ParticleSystem, value); } }
                    public Vector2 range { set { SetRange(m_ParticleSystem, value); } get { return GetRange(m_ParticleSystem); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSeparateAxes (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetSeparateAxes (ParticleSystem system) ;

        private static void SetRange (ParticleSystem system, Vector2 value) {
            INTERNAL_CALL_SetRange ( system, ref value );
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_SetRange (ParticleSystem system, ref Vector2 value);
        private static Vector2 GetRange (ParticleSystem system) {
            Vector2 result;
            INTERNAL_CALL_GetRange ( system, out result );
            return result;
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_GetRange (ParticleSystem system, out Vector2 value);
    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct RotationOverLifetimeModule
    {
        
                    internal RotationOverLifetimeModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public MinMaxCurve x { set { SetX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetX(m_ParticleSystem, ref r); return r; } }
                    public float xMultiplier { get { return GetXMultiplier(m_ParticleSystem); } set { SetXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve y { set { SetY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetY(m_ParticleSystem, ref r); return r; } }
                    public float yMultiplier { get { return GetYMultiplier(m_ParticleSystem); } set { SetYMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve z { set { SetZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetZ(m_ParticleSystem, ref r); return r; } }
                    public float zMultiplier { get { return GetZMultiplier(m_ParticleSystem); } set { SetZMultiplier(m_ParticleSystem, value); } }
                    public bool separateAxes { get { return GetSeparateAxes(m_ParticleSystem); } set { SetSeparateAxes(m_ParticleSystem, value); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSeparateAxes (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetSeparateAxes (ParticleSystem system) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct RotationBySpeedModule
    {
        
                    internal RotationBySpeedModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public MinMaxCurve x { set { SetX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetX(m_ParticleSystem, ref r); return r; } }
                    public float xMultiplier { get { return GetXMultiplier(m_ParticleSystem); } set { SetXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve y { set { SetY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetY(m_ParticleSystem, ref r); return r; } }
                    public float yMultiplier { get { return GetYMultiplier(m_ParticleSystem); } set { SetYMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve z { set { SetZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetZ(m_ParticleSystem, ref r); return r; } }
                    public float zMultiplier { get { return GetZMultiplier(m_ParticleSystem); } set { SetZMultiplier(m_ParticleSystem, value); } }
                    public bool separateAxes { get { return GetSeparateAxes(m_ParticleSystem); } set { SetSeparateAxes(m_ParticleSystem, value); } }
                    public Vector2 range { set { SetRange(m_ParticleSystem, value); } get { return GetRange(m_ParticleSystem); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSeparateAxes (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetSeparateAxes (ParticleSystem system) ;

        private static void SetRange (ParticleSystem system, Vector2 value) {
            INTERNAL_CALL_SetRange ( system, ref value );
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_SetRange (ParticleSystem system, ref Vector2 value);
        private static Vector2 GetRange (ParticleSystem system) {
            Vector2 result;
            INTERNAL_CALL_GetRange ( system, out result );
            return result;
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_GetRange (ParticleSystem system, out Vector2 value);
    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ExternalForcesModule
    {
        
                    internal ExternalForcesModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public float multiplier { get { return GetMultiplier(m_ParticleSystem); } set { SetMultiplier(m_ParticleSystem, value); } }
                    public ParticleSystemGameObjectFilter influenceFilter { get { return GetInfluenceFilter(m_ParticleSystem); } set { SetInfluenceFilter(m_ParticleSystem, value); } }
        
        public bool IsAffectedBy(ParticleSystemForceField field)                                                    { return IsAffectedBy_Internal(m_ParticleSystem, field); }
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool IsAffectedBy_Internal (ParticleSystem system, ParticleSystemForceField field) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetInfluenceFilter (ParticleSystem system, ParticleSystemGameObjectFilter value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemGameObjectFilter GetInfluenceFilter (ParticleSystem system) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct NoiseModule
    {
        
                    internal NoiseModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public bool separateAxes { get { return GetSeparateAxes(m_ParticleSystem); } set { SetSeparateAxes(m_ParticleSystem, value); } }
                    public MinMaxCurve strength { set { SetStrengthX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStrengthX(m_ParticleSystem, ref r); return r; } }
                    public float strengthMultiplier { get { return GetStrengthXMultiplier(m_ParticleSystem); } set { SetStrengthXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve strengthX { set { SetStrengthX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStrengthX(m_ParticleSystem, ref r); return r; } }
                    public float strengthXMultiplier { get { return GetStrengthXMultiplier(m_ParticleSystem); } set { SetStrengthXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve strengthY { set { SetStrengthY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStrengthY(m_ParticleSystem, ref r); return r; } }
                    public float strengthYMultiplier { get { return GetStrengthYMultiplier(m_ParticleSystem); } set { SetStrengthYMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve strengthZ { set { SetStrengthZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStrengthZ(m_ParticleSystem, ref r); return r; } }
                    public float strengthZMultiplier { get { return GetStrengthZMultiplier(m_ParticleSystem); } set { SetStrengthZMultiplier(m_ParticleSystem, value); } }
                    public float frequency { get { return GetFrequency(m_ParticleSystem); } set { SetFrequency(m_ParticleSystem, value); } }
                    public bool damping { get { return GetDamping(m_ParticleSystem); } set { SetDamping(m_ParticleSystem, value); } }
                    public int octaveCount { get { return GetOctaveCount(m_ParticleSystem); } set { SetOctaveCount(m_ParticleSystem, value); } }
                    public float octaveMultiplier { get { return GetOctaveMultiplier(m_ParticleSystem); } set { SetOctaveMultiplier(m_ParticleSystem, value); } }
                    public float octaveScale { get { return GetOctaveScale(m_ParticleSystem); } set { SetOctaveScale(m_ParticleSystem, value); } }
                    public ParticleSystemNoiseQuality quality { get { return (ParticleSystemNoiseQuality)GetQuality(m_ParticleSystem); } set { SetQuality(m_ParticleSystem, (int)value); } }
                    public MinMaxCurve scrollSpeed { set { SetScrollSpeed(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetScrollSpeed(m_ParticleSystem, ref r); return r; } }
                    public float scrollSpeedMultiplier { get { return GetScrollSpeedMultiplier(m_ParticleSystem); } set { SetScrollSpeedMultiplier(m_ParticleSystem, value); } }
                    public bool remapEnabled { get { return GetRemapEnabled(m_ParticleSystem); } set { SetRemapEnabled(m_ParticleSystem, value); } }
                    public MinMaxCurve remap { set { SetRemapX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetRemapX(m_ParticleSystem, ref r); return r; } }
                    public float remapMultiplier { get { return GetRemapXMultiplier(m_ParticleSystem); } set { SetRemapXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve remapX { set { SetRemapX(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetRemapX(m_ParticleSystem, ref r); return r; } }
                    public float remapXMultiplier { get { return GetRemapXMultiplier(m_ParticleSystem); } set { SetRemapXMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve remapY { set { SetRemapY(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetRemapY(m_ParticleSystem, ref r); return r; } }
                    public float remapYMultiplier { get { return GetRemapYMultiplier(m_ParticleSystem); } set { SetRemapYMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve remapZ { set { SetRemapZ(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetRemapZ(m_ParticleSystem, ref r); return r; } }
                    public float remapZMultiplier { get { return GetRemapZMultiplier(m_ParticleSystem); } set { SetRemapZMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve positionAmount { set { SetPositionAmount(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetPositionAmount(m_ParticleSystem, ref r); return r; } }
                    public MinMaxCurve rotationAmount { set { SetRotationAmount(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetRotationAmount(m_ParticleSystem, ref r); return r; } }
                    public MinMaxCurve sizeAmount { set { SetSizeAmount(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetSizeAmount(m_ParticleSystem, ref r); return r; } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSeparateAxes (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetSeparateAxes (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStrengthX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStrengthX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStrengthY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStrengthY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStrengthZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStrengthZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStrengthXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStrengthXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStrengthYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStrengthYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStrengthZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStrengthZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetFrequency (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetFrequency (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetDamping (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetDamping (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOctaveCount (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetOctaveCount (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOctaveMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetOctaveMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOctaveScale (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetOctaveScale (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetQuality (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetQuality (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetScrollSpeed (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetScrollSpeed (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetScrollSpeedMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetScrollSpeedMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRemapEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetRemapEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRemapX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetRemapX (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRemapY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetRemapY (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRemapZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetRemapZ (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRemapXMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRemapXMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRemapYMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRemapYMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRemapZMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRemapZMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetPositionAmount (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetPositionAmount (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRotationAmount (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetRotationAmount (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSizeAmount (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetSizeAmount (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CollisionModule
    {
        
                    internal CollisionModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public ParticleSystemCollisionType type { set { SetType(m_ParticleSystem, value); } get { return GetType(m_ParticleSystem); } }
                    public ParticleSystemCollisionMode mode { set { SetMode(m_ParticleSystem, value); } get { return GetMode(m_ParticleSystem); } }
                    public MinMaxCurve dampen { set { SetDampen(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetDampen(m_ParticleSystem, ref r); return r; } }
                    public float dampenMultiplier { get { return GetDampenMultiplier(m_ParticleSystem); } set { SetDampenMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve bounce { set { SetBounce(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetBounce(m_ParticleSystem, ref r); return r; } }
                    public float bounceMultiplier { get { return GetBounceMultiplier(m_ParticleSystem); } set { SetBounceMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve lifetimeLoss { set { SetLifetimeLoss(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetLifetimeLoss(m_ParticleSystem, ref r); return r; } }
                    public float lifetimeLossMultiplier { get { return GetLifetimeLossMultiplier(m_ParticleSystem); } set { SetLifetimeLossMultiplier(m_ParticleSystem, value); } }
                    public float minKillSpeed { get { return GetMinKillSpeed(m_ParticleSystem); } set { SetMinKillSpeed(m_ParticleSystem, value); } }
                    public float maxKillSpeed { get { return GetMaxKillSpeed(m_ParticleSystem); } set { SetMaxKillSpeed(m_ParticleSystem, value); } }
                    public LayerMask collidesWith { get { return GetCollidesWith(m_ParticleSystem); } set { SetCollidesWith(m_ParticleSystem, value); } }
                    public bool enableDynamicColliders { get { return GetEnableDynamicColliders(m_ParticleSystem); } set { SetEnableDynamicColliders(m_ParticleSystem, value); } }
                    public int maxCollisionShapes { get { return GetMaxCollisionShapes(m_ParticleSystem); } set { SetMaxCollisionShapes(m_ParticleSystem, value); } }
                    public ParticleSystemCollisionQuality quality { set { SetQuality(m_ParticleSystem, (int)value); } get { return (ParticleSystemCollisionQuality)GetQuality(m_ParticleSystem); } }
                    public float voxelSize { get { return GetVoxelSize(m_ParticleSystem); } set { SetVoxelSize(m_ParticleSystem, value); } }
                    public float radiusScale { get { return GetRadiusScale(m_ParticleSystem); } set { SetRadiusScale(m_ParticleSystem, value); } }
                    public bool sendCollisionMessages { get { return GetUsesCollisionMessages(m_ParticleSystem); } set { SetUsesCollisionMessages(m_ParticleSystem, value); } }
                    public float colliderForce { get { return GetColliderForce(m_ParticleSystem); } set { SetColliderForce(m_ParticleSystem, value); } }
                    public bool multiplyColliderForceByCollisionAngle { get { return GetMultiplyColliderForceByCollisionAngle(m_ParticleSystem); } set { SetMultiplyColliderForceByCollisionAngle(m_ParticleSystem, value); } }
                    public bool multiplyColliderForceByParticleSpeed { get { return GetMultiplyColliderForceByParticleSpeed(m_ParticleSystem); } set { SetMultiplyColliderForceByParticleSpeed(m_ParticleSystem, value); } }
                    public bool multiplyColliderForceByParticleSize { get { return GetMultiplyColliderForceByParticleSize(m_ParticleSystem); } set { SetMultiplyColliderForceByParticleSize(m_ParticleSystem, value); } }
        
        public void SetPlane(int index, Transform transform) { SetPlane(m_ParticleSystem, index, transform); }
        public Transform GetPlane(int index) { return GetPlane(m_ParticleSystem, index); }
                    public int maxPlaneCount { get { return GetMaxPlaneCount(m_ParticleSystem); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetType (ParticleSystem system, ParticleSystemCollisionType value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemCollisionType GetType (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMode (ParticleSystem system, ParticleSystemCollisionMode value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemCollisionMode GetMode (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetDampen (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetDampen (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetDampenMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetDampenMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetBounce (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetBounce (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetBounceMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetBounceMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetLifetimeLoss (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetLifetimeLoss (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetLifetimeLossMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetLifetimeLossMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMinKillSpeed (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetMinKillSpeed (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMaxKillSpeed (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetMaxKillSpeed (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetCollidesWith (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetCollidesWith (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnableDynamicColliders (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnableDynamicColliders (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnableInteriorCollisions (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnableInteriorCollisions (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMaxCollisionShapes (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetMaxCollisionShapes (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetQuality (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetQuality (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetVoxelSize (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetVoxelSize (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRadiusScale (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRadiusScale (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetUsesCollisionMessages (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetUsesCollisionMessages (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetColliderForce (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetColliderForce (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMultiplyColliderForceByCollisionAngle (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetMultiplyColliderForceByCollisionAngle (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMultiplyColliderForceByParticleSpeed (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetMultiplyColliderForceByParticleSpeed (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMultiplyColliderForceByParticleSize (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetMultiplyColliderForceByParticleSize (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetPlane (ParticleSystem system, int index, Transform transform) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  Transform GetPlane (ParticleSystem system, int index) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetMaxPlaneCount (ParticleSystem system) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TriggerModule
    {
        
                    internal TriggerModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public ParticleSystemOverlapAction inside { set { SetInside(m_ParticleSystem, value); } get { return GetInside(m_ParticleSystem); } }
                    public ParticleSystemOverlapAction outside { set { SetOutside(m_ParticleSystem, value); } get { return GetOutside(m_ParticleSystem); } }
                    public ParticleSystemOverlapAction enter { set { SetEnter(m_ParticleSystem, value); } get { return GetEnter(m_ParticleSystem); } }
                    public ParticleSystemOverlapAction exit { set { SetExit(m_ParticleSystem, value); } get { return GetExit(m_ParticleSystem); } }
                    public float radiusScale { get { return GetRadiusScale(m_ParticleSystem); } set { SetRadiusScale(m_ParticleSystem, value); } }
        
        public void SetCollider(int index, Component collider) { SetCollider(m_ParticleSystem, index, collider); }
        public Component GetCollider(int index) { return GetCollider(m_ParticleSystem, index); }
                    public int maxColliderCount { get { return GetMaxColliderCount(m_ParticleSystem); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetInside (ParticleSystem system, ParticleSystemOverlapAction value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemOverlapAction GetInside (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetOutside (ParticleSystem system, ParticleSystemOverlapAction value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemOverlapAction GetOutside (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnter (ParticleSystem system, ParticleSystemOverlapAction value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemOverlapAction GetEnter (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetExit (ParticleSystem system, ParticleSystemOverlapAction value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemOverlapAction GetExit (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRadiusScale (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRadiusScale (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetCollider (ParticleSystem system, int index, Component collider) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  Component GetCollider (ParticleSystem system, int index) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetMaxColliderCount (ParticleSystem system) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SubEmittersModule
    {
        
                    internal SubEmittersModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public int subEmittersCount { get { return GetSubEmittersCount(m_ParticleSystem); } }
        
        public void AddSubEmitter(ParticleSystem subEmitter, ParticleSystemSubEmitterType type, ParticleSystemSubEmitterProperties properties) { AddSubEmitter(m_ParticleSystem, subEmitter, (int)type, (int)properties); }
        public void AddSubEmitter(ParticleSystem subEmitter, ParticleSystemSubEmitterType type, ParticleSystemSubEmitterProperties properties, float emitProbability) { AddSubEmitter(m_ParticleSystem, subEmitter, (int)type, (int)properties, emitProbability); }
        public void RemoveSubEmitter(int index) { RemoveSubEmitter(m_ParticleSystem, index); }
        public void SetSubEmitterSystem(int index, ParticleSystem subEmitter) { SetSubEmitterSystem(m_ParticleSystem, index, subEmitter); }
        public void SetSubEmitterType(int index, ParticleSystemSubEmitterType type) { SetSubEmitterType(m_ParticleSystem, index, (int)type); }
        public void SetSubEmitterEmitProbability(int index, float emitProbability) { SetSubEmitterEmitProbability(m_ParticleSystem, index, emitProbability); }
        public void SetSubEmitterProperties(int index, ParticleSystemSubEmitterProperties properties) { SetSubEmitterProperties(m_ParticleSystem, index, (int)properties); }
        public ParticleSystem GetSubEmitterSystem(int index) { return GetSubEmitterSystem(m_ParticleSystem, index); }
        public ParticleSystemSubEmitterType GetSubEmitterType(int index) { return (ParticleSystemSubEmitterType)GetSubEmitterType(m_ParticleSystem, index); }
        public float GetSubEmitterEmitProbability(int index) { return GetSubEmitterEmitProbability(m_ParticleSystem, index); }
        public ParticleSystemSubEmitterProperties GetSubEmitterProperties(int index) { return (ParticleSystemSubEmitterProperties)GetSubEmitterProperties(m_ParticleSystem, index); }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetSubEmittersCount (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetBirth (ParticleSystem system, int index, ParticleSystem value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystem GetBirth (ParticleSystem system, int index) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetCollision (ParticleSystem system, int index, ParticleSystem value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystem GetCollision (ParticleSystem system, int index) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetDeath (ParticleSystem system, int index, ParticleSystem value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystem GetDeath (ParticleSystem system, int index) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void AddSubEmitter (ParticleSystem system, ParticleSystem subEmitter, int type, int properties, [uei.DefaultValue("1")]  float emitProbability ) ;

        [uei.ExcludeFromDocs]
        private static void AddSubEmitter (ParticleSystem system, ParticleSystem subEmitter, int type, int properties) {
            float emitProbability = 1;
            AddSubEmitter ( system, subEmitter, type, properties, emitProbability );
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void RemoveSubEmitter (ParticleSystem system, int index) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSubEmitterSystem (ParticleSystem system, int index, ParticleSystem subEmitter) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSubEmitterType (ParticleSystem system, int index, int type) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSubEmitterEmitProbability (ParticleSystem system, int index, float emitProbability) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSubEmitterProperties (ParticleSystem system, int index, int properties) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystem GetSubEmitterSystem (ParticleSystem system, int index) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetSubEmitterType (ParticleSystem system, int index) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetSubEmitterEmitProbability (ParticleSystem system, int index) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetSubEmitterProperties (ParticleSystem system, int index) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TextureSheetAnimationModule
    {
        
                    internal TextureSheetAnimationModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public ParticleSystemAnimationMode mode { set { SetMode(m_ParticleSystem, value); } get { return GetMode(m_ParticleSystem); } }
                    public ParticleSystemAnimationTimeMode timeMode { set { SetTimeMode(m_ParticleSystem, value); } get { return GetTimeMode(m_ParticleSystem); } }
                    public float fps { get { return GetFPS(m_ParticleSystem); } set { SetFPS(m_ParticleSystem, value); } }
                    public int numTilesX { set { SetNumTilesX(m_ParticleSystem, value); } get { return GetNumTilesX(m_ParticleSystem); } }
                    public int numTilesY { set { SetNumTilesY(m_ParticleSystem, value); } get { return GetNumTilesY(m_ParticleSystem); } }
                    public ParticleSystemAnimationType animation { set { SetAnimationType(m_ParticleSystem, value); } get { return GetAnimationType(m_ParticleSystem); } }
                    public bool useRandomRow { set { SetUseRandomRow(m_ParticleSystem, value); } get { return GetUseRandomRow(m_ParticleSystem); } }
                    public MinMaxCurve frameOverTime { set { SetFrameOverTime(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetFrameOverTime(m_ParticleSystem, ref r); return r; } }
                    public float frameOverTimeMultiplier { get { return GetFrameOverTimeMultiplier(m_ParticleSystem); } set { SetFrameOverTimeMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve startFrame { set { SetStartFrame(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetStartFrame(m_ParticleSystem, ref r); return r; } }
                    public float startFrameMultiplier { get { return GetStartFrameMultiplier(m_ParticleSystem); } set { SetStartFrameMultiplier(m_ParticleSystem, value); } }
                    public int cycleCount { set { SetCycleCount(m_ParticleSystem, value); } get { return GetCycleCount(m_ParticleSystem); } }
                    public int rowIndex { set { SetRowIndex(m_ParticleSystem, value); } get { return GetRowIndex(m_ParticleSystem); } }
                    public Rendering.UVChannelFlags uvChannelMask { set { SetUVChannelMask(m_ParticleSystem, (int)value); } get { return (Rendering.UVChannelFlags)GetUVChannelMask(m_ParticleSystem); } }
                    public int spriteCount { get { return GetSpriteCount(m_ParticleSystem); } }
                    public Vector2 speedRange { set { SetSpeedRange(m_ParticleSystem, value); } get { return GetSpeedRange(m_ParticleSystem); } }
        
        public void AddSprite(Sprite sprite) { AddSprite(m_ParticleSystem, sprite); }
        public void RemoveSprite(int index) { RemoveSprite(m_ParticleSystem, index); }
        public void SetSprite(int index, Sprite sprite) { SetSprite(m_ParticleSystem, index, sprite); }
        public Sprite GetSprite(int index) { return GetSprite(m_ParticleSystem, index); }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMode (ParticleSystem system, ParticleSystemAnimationMode value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemAnimationMode GetMode (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetTimeMode (ParticleSystem system, ParticleSystemAnimationTimeMode value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemAnimationTimeMode GetTimeMode (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetFPS (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetFPS (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetNumTilesX (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetNumTilesX (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetNumTilesY (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetNumTilesY (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetAnimationType (ParticleSystem system, ParticleSystemAnimationType value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemAnimationType GetAnimationType (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetUseRandomRow (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetUseRandomRow (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetFrameOverTime (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetFrameOverTime (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetFrameOverTimeMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetFrameOverTimeMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartFrame (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetStartFrame (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetStartFrameMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetStartFrameMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetCycleCount (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetCycleCount (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRowIndex (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetRowIndex (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetUVChannelMask (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetUVChannelMask (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetSpriteCount (ParticleSystem system) ;

        private static void SetSpeedRange (ParticleSystem system, Vector2 value) {
            INTERNAL_CALL_SetSpeedRange ( system, ref value );
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_SetSpeedRange (ParticleSystem system, ref Vector2 value);
        private static Vector2 GetSpeedRange (ParticleSystem system) {
            Vector2 result;
            INTERNAL_CALL_GetSpeedRange ( system, out result );
            return result;
        }

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        private extern static void INTERNAL_CALL_GetSpeedRange (ParticleSystem system, out Vector2 value);
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void AddSprite (ParticleSystem system, Object sprite) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void RemoveSprite (ParticleSystem system, int index) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSprite (ParticleSystem system, int index, Object sprite) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  Sprite GetSprite (ParticleSystem system, int index) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct LightsModule
    {
        
                    internal LightsModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public float ratio { get { return GetRatio(m_ParticleSystem); } set { SetRatio(m_ParticleSystem, value); } }
                    public bool useRandomDistribution { set { SetUseRandomDistribution(m_ParticleSystem, value); } get { return GetUseRandomDistribution(m_ParticleSystem); } }
                    public Light light { set { SetLightPrefab(m_ParticleSystem, value); } get { return GetLightPrefab(m_ParticleSystem); } }
                    public bool useParticleColor { set { SetUseParticleColor(m_ParticleSystem, value); } get { return GetUseParticleColor(m_ParticleSystem); } }
                    public bool sizeAffectsRange { set { SetSizeAffectsRange(m_ParticleSystem, value); } get { return GetSizeAffectsRange(m_ParticleSystem); } }
                    public bool alphaAffectsIntensity { set { SetAlphaAffectsIntensity(m_ParticleSystem, value); } get { return GetAlphaAffectsIntensity(m_ParticleSystem); } }
                    public MinMaxCurve range { set { SetRange(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetRange(m_ParticleSystem, ref r); return r; } }
                    public float rangeMultiplier { get { return GetRangeMultiplier(m_ParticleSystem); } set { SetRangeMultiplier(m_ParticleSystem, value); } }
                    public MinMaxCurve intensity { set { SetIntensity(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetIntensity(m_ParticleSystem, ref r); return r; } }
                    public float intensityMultiplier { get { return GetIntensityMultiplier(m_ParticleSystem); } set { SetIntensityMultiplier(m_ParticleSystem, value); } }
                    public int maxLights { set { SetMaxLights(m_ParticleSystem, value); } get { return GetMaxLights(m_ParticleSystem); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRatio (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRatio (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetUseRandomDistribution (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetUseRandomDistribution (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetLightPrefab (ParticleSystem system, Light value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  Light GetLightPrefab (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetUseParticleColor (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetUseParticleColor (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSizeAffectsRange (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetSizeAffectsRange (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetAlphaAffectsIntensity (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetAlphaAffectsIntensity (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRange (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetRange (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRangeMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRangeMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetIntensity (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetIntensity (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetIntensityMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetIntensityMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMaxLights (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetMaxLights (ParticleSystem system) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TrailModule
    {
        
                    internal TrailModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
                    public ParticleSystemTrailMode mode { get { return GetMode(m_ParticleSystem); } set { SetMode(m_ParticleSystem, value); } }
                    public float ratio { get { return GetRatio(m_ParticleSystem); } set { SetRatio(m_ParticleSystem, value); } }
                    public MinMaxCurve lifetime { set { SetLifetime(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetLifetime(m_ParticleSystem, ref r); return r; } }
                    public float lifetimeMultiplier { get { return GetLifetimeMultiplier(m_ParticleSystem); } set { SetLifetimeMultiplier(m_ParticleSystem, value); } }
                    public float minVertexDistance { set { SetMinVertexDistance(m_ParticleSystem, value); } get { return GetMinVertexDistance(m_ParticleSystem); } }
                    public ParticleSystemTrailTextureMode textureMode { get { return GetTextureMode(m_ParticleSystem); } set { SetTextureMode(m_ParticleSystem, value); } }
                    public bool worldSpace { set { SetWorldSpace(m_ParticleSystem, value); } get { return GetWorldSpace(m_ParticleSystem); } }
                    public bool dieWithParticles { set { SetDieWithParticles(m_ParticleSystem, value); } get { return GetDieWithParticles(m_ParticleSystem); } }
                    public bool sizeAffectsWidth { set { SetSizeAffectsWidth(m_ParticleSystem, value); } get { return GetSizeAffectsWidth(m_ParticleSystem); } }
                    public bool sizeAffectsLifetime { set { SetSizeAffectsLifetime(m_ParticleSystem, value); } get { return GetSizeAffectsLifetime(m_ParticleSystem); } }
                    public bool inheritParticleColor { set { SetInheritParticleColor(m_ParticleSystem, value); } get { return GetInheritParticleColor(m_ParticleSystem); } }
                    public MinMaxGradient colorOverLifetime { set { SetColorOverLifetime(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxGradient(); GetColorOverLifetime(m_ParticleSystem, ref r); return r; } }
                    public MinMaxCurve widthOverTrail { set { SetWidthOverTrail(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxCurve(); GetWidthOverTrail(m_ParticleSystem, ref r); return r; } }
                    public float widthOverTrailMultiplier { get { return GetWidthOverTrailMultiplier(m_ParticleSystem); } set { SetWidthOverTrailMultiplier(m_ParticleSystem, value); } }
                    public MinMaxGradient colorOverTrail { set { SetColorOverTrail(m_ParticleSystem, ref value); } get { var r = new ParticleSystem.MinMaxGradient(); GetColorOverTrail(m_ParticleSystem, ref r); return r; } }
                    public bool generateLightingData { set { SetGenerateLightingData(m_ParticleSystem, value); } get { return GetGenerateLightingData(m_ParticleSystem); } }
                    public int ribbonCount { get { return GetRibbonCount(m_ParticleSystem); } set { SetRibbonCount(m_ParticleSystem, value); } }
                    public float shadowBias { set { SetShadowBias(m_ParticleSystem, value); } get { return GetShadowBias(m_ParticleSystem); } }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMode (ParticleSystem system, ParticleSystemTrailMode value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemTrailMode GetMode (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRatio (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetRatio (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetLifetime (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetLifetime (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetLifetimeMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetLifetimeMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMinVertexDistance (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetMinVertexDistance (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetTextureMode (ParticleSystem system, ParticleSystemTrailTextureMode value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemTrailTextureMode GetTextureMode (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetWorldSpace (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetWorldSpace (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetDieWithParticles (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetDieWithParticles (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSizeAffectsWidth (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetSizeAffectsWidth (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetSizeAffectsLifetime (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetSizeAffectsLifetime (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetInheritParticleColor (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetInheritParticleColor (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetColorOverLifetime (ParticleSystem system, ref ParticleSystem.MinMaxGradient gradient) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetColorOverLifetime (ParticleSystem system, ref ParticleSystem.MinMaxGradient gradient) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetWidthOverTrail (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetWidthOverTrail (ParticleSystem system, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetWidthOverTrailMultiplier (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetWidthOverTrailMultiplier (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetColorOverTrail (ParticleSystem system, ref ParticleSystem.MinMaxGradient gradient) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetColorOverTrail (ParticleSystem system, ref ParticleSystem.MinMaxGradient gradient) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetGenerateLightingData (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetGenerateLightingData (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetRibbonCount (ParticleSystem system, int value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetRibbonCount (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetShadowBias (ParticleSystem system, float value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  float GetShadowBias (ParticleSystem system) ;

    }

    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CustomDataModule
    {
        
                    internal CustomDataModule(ParticleSystem particleSystem) { m_ParticleSystem = particleSystem; }
                    private ParticleSystem m_ParticleSystem;
        
                    public bool enabled { set { SetEnabled(m_ParticleSystem, value); } get { return GetEnabled(m_ParticleSystem); } }
        
        public void SetMode(ParticleSystemCustomData stream, ParticleSystemCustomDataMode mode) { SetMode(m_ParticleSystem, (int)stream, mode); }
        public ParticleSystemCustomDataMode GetMode(ParticleSystemCustomData stream) { return GetMode(m_ParticleSystem, (int)stream); }
        public void SetVectorComponentCount(ParticleSystemCustomData stream, int count) { SetVectorComponentCount(m_ParticleSystem, (int)stream, count); }
        public int GetVectorComponentCount(ParticleSystemCustomData stream) { return GetVectorComponentCount(m_ParticleSystem, (int)stream); }
        public void SetVector(ParticleSystemCustomData stream, int component, ParticleSystem.MinMaxCurve curve) { SetVector(m_ParticleSystem, (int)stream, component, ref curve); }
        public ParticleSystem.MinMaxCurve GetVector(ParticleSystemCustomData stream, int component) { var r = new ParticleSystem.MinMaxCurve(); GetVector(m_ParticleSystem, (int)stream, component, ref r); return r; }
        public void SetColor(ParticleSystemCustomData stream, ParticleSystem.MinMaxGradient gradient) { SetColor(m_ParticleSystem, (int)stream, ref gradient); }
        public ParticleSystem.MinMaxGradient GetColor(ParticleSystemCustomData stream) { var r = new ParticleSystem.MinMaxGradient(); GetColor(m_ParticleSystem, (int)stream, ref r); return r; }
        
        
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetEnabled (ParticleSystem system, bool value) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  bool GetEnabled (ParticleSystem system) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetMode (ParticleSystem system, int stream, ParticleSystemCustomDataMode mode) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetVectorComponentCount (ParticleSystem system, int stream, int count) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetVector (ParticleSystem system, int stream, int component, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void SetColor (ParticleSystem system, int stream, ref ParticleSystem.MinMaxGradient gradient) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  ParticleSystemCustomDataMode GetMode (ParticleSystem system, int stream) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  int GetVectorComponentCount (ParticleSystem system, int stream) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetVector (ParticleSystem system, int stream, int component, ref ParticleSystem.MinMaxCurve curve) ;

        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        extern private static  void GetColor (ParticleSystem system, int stream, ref ParticleSystem.MinMaxGradient gradient) ;

    }

    public void SetCustomParticleData(List<Vector4> customData, ParticleSystemCustomData streamIndex)
        {
            SetCustomParticleDataInternal(customData, (int)streamIndex);
        }
    
    
    
    
    public int GetCustomParticleData(List<Vector4> customData, ParticleSystemCustomData streamIndex)
        {
            return GetCustomParticleDataInternal(customData, (int)streamIndex);
        }
    
    
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SetCustomParticleDataInternal (object customData, int streamIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal int GetCustomParticleDataInternal (object customData, int streamIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_EmitOld (ref ParticleSystem.Particle particle) ;

    public void TriggerSubEmitter(int subEmitterIndex)
        {
            Internal_TriggerSubEmitter(subEmitterIndex, null);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void TriggerSubEmitter (int subEmitterIndex, ref ParticleSystem.Particle particle) ;

    public void TriggerSubEmitter(int subEmitterIndex, List<ParticleSystem.Particle> particles)
        {
            Internal_TriggerSubEmitter(subEmitterIndex, particles);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void Internal_TriggerSubEmitter (int subEmitterIndex, object particles) ;

    
    
}

[RequireComponent(typeof(Transform))]
public sealed partial class ParticleSystemRenderer : Renderer
{
    public extern  Mesh mesh
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public int meshCount { get { return Internal_GetMeshCount(); } }
    [RequiredByNativeCode]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private int Internal_GetMeshCount () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetMeshes (Mesh[] meshes) ;

    public void SetMeshes(Mesh[] meshes) { SetMeshes(meshes, meshes.Length); }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetMeshes (Mesh[] meshes, int size) ;

    public extern  int activeVertexStreamsCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public void SetActiveVertexStreams(List<ParticleSystemVertexStream> streams)
        {
            if (streams == null) throw new ArgumentNullException("streams");
            SetActiveVertexStreamsInternal(streams);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SetActiveVertexStreamsInternal (object streams) ;

    
    
    public void GetActiveVertexStreams(List<ParticleSystemVertexStream> streams)
        {
            if (streams == null) throw new ArgumentNullException("streams");
            GetActiveVertexStreamsInternal(streams);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void GetActiveVertexStreamsInternal (object streams) ;

    
    
    internal extern  bool supportsMeshInstancing
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

[RequiredByNativeCode(Optional = true)]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct ParticleCollisionEvent
{
    
            private Vector3 m_Intersection;
            private Vector3 m_Normal;
            private Vector3 m_Velocity;
            private int m_ColliderInstanceID;
    
    
    public Vector3 intersection { get { return m_Intersection; }  }
    public Vector3 normal { get { return m_Normal; }  }
    public Vector3 velocity { get { return m_Velocity; }  }
    
    
    public Component colliderComponent { get { return InstanceIDToColliderComponent(m_ColliderInstanceID); } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  Component InstanceIDToColliderComponent (int instanceID) ;

}

internal sealed partial class ParticleSystemExtensionsImpl
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetSafeCollisionEventSize (ParticleSystem ps) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetCollisionEventsDeprecated (ParticleSystem ps, GameObject go, ParticleCollisionEvent[] collisionEvents) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetSafeTriggerParticlesSize (ParticleSystem ps, int type) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetCollisionEvents (ParticleSystem ps, GameObject go, object collisionEvents) ;

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetTriggerParticles (ParticleSystem ps, int type, object particles) ;

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetTriggerParticles (ParticleSystem ps, int type, object particles, int offset, int count) ;

    
    
}

public static partial class ParticlePhysicsExtensions
{
    
    public static int GetSafeCollisionEventSize(this ParticleSystem ps) { return ParticleSystemExtensionsImpl.GetSafeCollisionEventSize(ps); }
    public static int GetCollisionEvents(this ParticleSystem ps, GameObject go, List<ParticleCollisionEvent> collisionEvents)
        {
            if (go == null) throw new ArgumentNullException("go");
            if (collisionEvents == null) throw new ArgumentNullException("collisionEvents");

            return ParticleSystemExtensionsImpl.GetCollisionEvents(ps, go, collisionEvents);
        }
    
    public static int GetSafeTriggerParticlesSize(this ParticleSystem ps, ParticleSystemTriggerEventType type)
        {
            return ParticleSystemExtensionsImpl.GetSafeTriggerParticlesSize(ps, (int)type);
        }
    
    public static int GetTriggerParticles(this ParticleSystem ps, ParticleSystemTriggerEventType type, List<ParticleSystem.Particle> particles)
        {
            if (particles == null) throw new ArgumentNullException("particles");

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
            if (particles == null) throw new ArgumentNullException("particles");

            ParticleSystemExtensionsImpl.SetTriggerParticles(ps, (int)type, particles, 0, particles.Count);
        }
    
    
}


}
