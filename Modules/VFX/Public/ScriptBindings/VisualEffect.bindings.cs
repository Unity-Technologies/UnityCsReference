// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.VFX;
using UnityEngine.Scripting;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.VFX
{
    [UsedByNativeCode]
    public struct VFXExposedProperty
    {
        public string name;
        public Type type;
    }

    [UsedByNativeCode]
    [NativeHeader("Modules/VFX/Public/ScriptBindings/VisualEffectAssetBindings.h")]
    [NativeHeader("Modules/VFX/Public/VisualEffectAsset.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    public abstract class VisualEffectObject : Object
    {
    }

    [UsedByNativeCode]
    [NativeHeader("Modules/VFX/Public/VisualEffectAsset.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    public class VisualEffectAsset : VisualEffectObject
    {
        public const string PlayEventName = "OnPlay";
        public const string StopEventName = "OnStop";
        public static readonly int PlayEventID = Shader.PropertyToID(PlayEventName);
        public static readonly int StopEventID = Shader.PropertyToID(StopEventName);
        internal extern uint GetCompilationVersion();

        static internal extern uint currentRuntimeDataVersion { get; }
        [FreeFunction(Name = "VisualEffectAssetBindings::GetTextureDimension", HasExplicitThis = true)] extern public UnityEngine.Rendering.TextureDimension GetTextureDimension(int nameID);
        [FreeFunction(Name = "VisualEffectAssetBindings::GetExposedProperties", HasExplicitThis = true)] extern public void GetExposedProperties([NotNull] List<VFXExposedProperty> exposedProperties);
        [FreeFunction(Name = "VisualEffectAssetBindings::GetEvents", HasExplicitThis = true)] extern public void GetEvents([NotNull] List<string> names);

        public UnityEngine.Rendering.TextureDimension GetTextureDimension(string name)
        {
            return GetTextureDimension(Shader.PropertyToID(name));
        }
    }

    [NativeHeader("Modules/VFX/Public/ScriptBindings/VisualEffectBindings.h")]
    [NativeHeader("Modules/VFX/Public/VisualEffect.h")]
    [RequireComponent(typeof(Transform))]
    public class VisualEffect : Behaviour
    {
        extern public bool pause { get; set; }
        extern public float playRate { get; set; }
        extern public uint startSeed { get; set; }
        extern public bool resetSeedOnPlay { get; set; }
        extern public int initialEventID
        {
            [FreeFunction(Name = "VisualEffectBindings::GetInitialEventID", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "VisualEffectBindings::SetInitialEventID", HasExplicitThis = true)]
            set;
        }

        extern public string initialEventName
        {
            [FreeFunction(Name = "VisualEffectBindings::GetInitialEventName", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "VisualEffectBindings::SetInitialEventName", HasExplicitThis = true)]
            set;
        }

        extern public bool culled { get; }

        extern public VisualEffectAsset visualEffectAsset { get; set; }

        public VFXEventAttribute CreateVFXEventAttribute()
        {
            if (visualEffectAsset == null)
                return null;
            var vfxEventAttribute = VFXEventAttribute.Internal_InstanciateVFXEventAttribute(visualEffectAsset);
            return vfxEventAttribute;
        }

        private void CheckValidVFXEventAttribute(VFXEventAttribute eventAttribute)
        {
            if (eventAttribute != null && eventAttribute.vfxAsset != visualEffectAsset)
            {
                throw new InvalidOperationException("Invalid VFXEventAttribute provided to VisualEffect, has been created with another VisualEffectAsset");
            }
        }

        [FreeFunction(Name = "VisualEffectBindings::SendEventFromScript", HasExplicitThis = true)]
        extern private void SendEventFromScript(int eventNameID, VFXEventAttribute eventAttribute);

        public void SendEvent(int eventNameID, VFXEventAttribute eventAttribute)
        {
            CheckValidVFXEventAttribute(eventAttribute);
            SendEventFromScript(eventNameID, eventAttribute);
        }

        public void SendEvent(string eventName, VFXEventAttribute eventAttribute)
        {
            SendEvent(Shader.PropertyToID(eventName), eventAttribute);
        }

        public void SendEvent(int eventNameID)
        {
            SendEventFromScript(eventNameID, null);
        }

        public void SendEvent(string eventName)
        {
            SendEvent(Shader.PropertyToID(eventName), null);
        }

        public void Play(VFXEventAttribute eventAttribute)
        {
            SendEvent(VisualEffectAsset.PlayEventID, eventAttribute);
        }

        public void Play()
        {
            SendEvent(VisualEffectAsset.PlayEventID);
        }

        public void Stop(VFXEventAttribute eventAttribute)
        {
            SendEvent(VisualEffectAsset.StopEventID, eventAttribute);
        }

        public void Stop()
        {
            SendEvent(VisualEffectAsset.StopEventID);
        }

        extern public void Reinit();
        extern public void AdvanceOneFrame();

        [FreeFunction(Name = "VisualEffectBindings::ResetOverrideFromScript", HasExplicitThis = true)] extern public void ResetOverride(int nameID);

        // Values check
        [FreeFunction(Name = "VisualEffectBindings::GetTextureDimensionFromScript", HasExplicitThis = true)] extern public UnityEngine.Rendering.TextureDimension GetTextureDimension(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<bool>", HasExplicitThis = true)] extern public bool HasBool(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<int>", HasExplicitThis = true)] extern public bool HasInt(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<UInt32>", HasExplicitThis = true)] extern public bool HasUInt(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<float>", HasExplicitThis = true)] extern public bool HasFloat(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Vector2f>", HasExplicitThis = true)] extern public bool HasVector2(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Vector3f>", HasExplicitThis = true)] extern public bool HasVector3(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Vector4f>", HasExplicitThis = true)] extern public bool HasVector4(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Matrix4x4f>", HasExplicitThis = true)] extern public bool HasMatrix4x4(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Texture*>", HasExplicitThis = true)] extern public bool HasTexture(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<AnimationCurve*>", HasExplicitThis = true)] extern public bool HasAnimationCurve(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Gradient*>", HasExplicitThis = true)] extern public bool HasGradient(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Mesh*>", HasExplicitThis = true)] extern public bool HasMesh(int nameID);

        // Value setters
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<bool>", HasExplicitThis = true)] extern public void SetBool(int nameID, bool b);
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<int>", HasExplicitThis = true)] extern public void SetInt(int nameID, int i);
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<UInt32>", HasExplicitThis = true)] extern public void SetUInt(int nameID, uint i);
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<float>", HasExplicitThis = true)] extern public void SetFloat(int nameID, float f);
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Vector2f>", HasExplicitThis = true)] extern public void SetVector2(int nameID, Vector2 v);
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Vector3f>", HasExplicitThis = true)] extern public void SetVector3(int nameID, Vector3 v);
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Vector4f>", HasExplicitThis = true)] extern public void SetVector4(int nameID, Vector4 v);
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Matrix4x4f>", HasExplicitThis = true)] extern public void SetMatrix4x4(int nameID, Matrix4x4 v);
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Texture*>", HasExplicitThis = true)] extern public void SetTexture(int nameID, Texture t);
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<AnimationCurve*>", HasExplicitThis = true)] extern public void SetAnimationCurve(int nameID, AnimationCurve c);
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Gradient*>", HasExplicitThis = true)] extern public void SetGradient(int nameID, Gradient g);
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Mesh*>", HasExplicitThis = true)] extern public void SetMesh(int nameID, Mesh m);

        // Value getters
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<bool>", HasExplicitThis = true)] extern public bool GetBool(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<int>", HasExplicitThis = true)] extern public int GetInt(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<UInt32>", HasExplicitThis = true)] extern public uint GetUInt(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<float>", HasExplicitThis = true)] extern public float GetFloat(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<Vector2f>", HasExplicitThis = true)] extern public Vector2 GetVector2(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<Vector3f>", HasExplicitThis = true)] extern public Vector3 GetVector3(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<Vector4f>", HasExplicitThis = true)] extern public Vector4 GetVector4(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<Matrix4x4f>", HasExplicitThis = true)] extern public Matrix4x4 GetMatrix4x4(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<Texture*>", HasExplicitThis = true)] extern public Texture GetTexture(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<Mesh*>", HasExplicitThis = true)] extern public Mesh GetMesh(int nameID);
        public Gradient GetGradient(int nameID)
        {
            var gradient = new Gradient();
            Internal_GetGradient(nameID, gradient);
            return gradient;
        }

        [FreeFunction(Name = "VisualEffectBindings::Internal_GetGradientFromScript", HasExplicitThis = true)] extern private void Internal_GetGradient(int nameID, Gradient gradient);

        public AnimationCurve GetAnimationCurve(int nameID)
        {
            var curve = new AnimationCurve();
            Internal_GetAnimationCurve(nameID, curve);
            return curve;
        }

        [FreeFunction(Name = "VisualEffectBindings::Internal_GetAnimationCurveFromScript", HasExplicitThis = true)] extern private void Internal_GetAnimationCurve(int nameID, AnimationCurve curve);

        [FreeFunction(Name = "VisualEffectBindings::HasSystemFromScript", HasExplicitThis = true)] extern public bool HasSystem(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::GetParticleSystemInfo", HasExplicitThis = true, ThrowsException = true)] extern public VFXParticleSystemInfo GetParticleSystemInfo(int nameID);

        [FreeFunction(Name = "VisualEffectBindings::GetSystemNamesFromScript", HasExplicitThis = true)] extern public void GetSystemNames([NotNull] List<string> names);
        [FreeFunction(Name = "VisualEffectBindings::GetParticleSystemNamesFromScript", HasExplicitThis = true)] extern public void GetParticleSystemNames([NotNull] List<string> names);

        public void ResetOverride(string name)
        {
            ResetOverride(Shader.PropertyToID(name));
        }

        // Values check
        public bool HasInt(string name)
        {
            return HasInt(Shader.PropertyToID(name));
        }

        public bool HasUInt(string name)
        {
            return HasUInt(Shader.PropertyToID(name));
        }

        public bool HasFloat(string name)
        {
            return HasFloat(Shader.PropertyToID(name));
        }

        public bool HasVector2(string name)
        {
            return HasVector2(Shader.PropertyToID(name));
        }

        public bool HasVector3(string name)
        {
            return HasVector3(Shader.PropertyToID(name));
        }

        public bool HasVector4(string name)
        {
            return HasVector4(Shader.PropertyToID(name));
        }

        public bool HasMatrix4x4(string name)
        {
            return HasMatrix4x4(Shader.PropertyToID(name));
        }

        public bool HasTexture(string name)
        {
            return HasTexture(Shader.PropertyToID(name));
        }

        public UnityEngine.Rendering.TextureDimension GetTextureDimension(string name)
        {
            return GetTextureDimension(Shader.PropertyToID(name));
        }

        public bool HasAnimationCurve(string name)
        {
            return HasAnimationCurve(Shader.PropertyToID(name));
        }

        public bool HasGradient(string name)
        {
            return HasGradient(Shader.PropertyToID(name));
        }

        public bool HasMesh(string name)
        {
            return HasMesh(Shader.PropertyToID(name));
        }

        public bool HasBool(string name)
        {
            return HasBool(Shader.PropertyToID(name));
        }

        // Value setters
        public void SetInt(string name, int i)
        {
            SetInt(Shader.PropertyToID(name), i);
        }

        public void SetUInt(string name, uint i)
        {
            SetUInt(Shader.PropertyToID(name), i);
        }

        public void SetFloat(string name, float f)
        {
            SetFloat(Shader.PropertyToID(name), f);
        }

        public void SetVector2(string name, Vector2 v)
        {
            SetVector2(Shader.PropertyToID(name), v);
        }

        public void SetVector3(string name, Vector3 v)
        {
            SetVector3(Shader.PropertyToID(name), v);
        }

        public void SetVector4(string name, Vector4 v)
        {
            SetVector4(Shader.PropertyToID(name), v);
        }

        public void SetMatrix4x4(string name, Matrix4x4 v)
        {
            SetMatrix4x4(Shader.PropertyToID(name), v);
        }

        public void SetTexture(string name, Texture t)
        {
            SetTexture(Shader.PropertyToID(name), t);
        }

        public void SetAnimationCurve(string name, AnimationCurve c)
        {
            SetAnimationCurve(Shader.PropertyToID(name), c);
        }

        public void SetGradient(string name, Gradient g)
        {
            SetGradient(Shader.PropertyToID(name), g);
        }

        public void SetMesh(string name, Mesh m)
        {
            SetMesh(Shader.PropertyToID(name), m);
        }

        public void SetBool(string name, bool b)
        {
            SetBool(Shader.PropertyToID(name), b);
        }

        // Value getters
        public int GetInt(string name)
        {
            return GetInt(Shader.PropertyToID(name));
        }

        public uint GetUInt(string name)
        {
            return GetUInt(Shader.PropertyToID(name));
        }

        public float GetFloat(string name)
        {
            return GetFloat(Shader.PropertyToID(name));
        }

        public Vector2 GetVector2(string name)
        {
            return GetVector2(Shader.PropertyToID(name));
        }

        public Vector3 GetVector3(string name)
        {
            return GetVector3(Shader.PropertyToID(name));
        }

        public Vector4 GetVector4(string name)
        {
            return GetVector4(Shader.PropertyToID(name));
        }

        public Matrix4x4 GetMatrix4x4(string name)
        {
            return GetMatrix4x4(Shader.PropertyToID(name));
        }

        public Texture GetTexture(string name)
        {
            return GetTexture(Shader.PropertyToID(name));
        }

        public Mesh GetMesh(string name)
        {
            return GetMesh(Shader.PropertyToID(name));
        }

        public bool GetBool(string name)
        {
            return GetBool(Shader.PropertyToID(name));
        }

        public AnimationCurve GetAnimationCurve(string name)
        {
            return GetAnimationCurve(Shader.PropertyToID(name));
        }

        public Gradient GetGradient(string name)
        {
            return GetGradient(Shader.PropertyToID(name));
        }

        public bool HasSystem(string name)
        {
            return HasSystem(Shader.PropertyToID(name));
        }

        public VFXParticleSystemInfo GetParticleSystemInfo(string name)
        {
            return GetParticleSystemInfo(Shader.PropertyToID(name));
        }

        extern public int aliveParticleCount { get; }

        extern public void Simulate(float stepDeltaTime, uint stepCount = 1);

        //Could be exposed publicly but requires a specific function from bindings which doesn't call BaseObject::Reset (because it also resets the awake flags)
        //extern internal void Reset();
    }

    // Bindings for VFXRenderer is needed but we dont want it to be accessible to users
    [NativeType(Header = "Modules/VFX/Public/VFXRenderer.h")]
    internal sealed partial class VFXRenderer : Renderer
    {
    }

    [UsedByNativeCode]
    [NativeHeader("Modules/VFX/Public/VFXSystem.h")]
    public struct VFXParticleSystemInfo
    {
        public uint aliveCount;
        public uint capacity;
        public bool sleeping;
        public Bounds bounds;

        public VFXParticleSystemInfo(uint aliveCount, uint capacity, bool sleeping, Bounds bounds)
        {
            this.aliveCount = aliveCount;
            this.capacity = capacity;
            this.sleeping = sleeping;
            this.bounds = bounds;
        }
    }
}
