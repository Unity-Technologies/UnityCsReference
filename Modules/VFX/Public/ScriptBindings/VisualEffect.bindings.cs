// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Experimental.VFX;
using UnityEngine.Scripting;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace UnityEngine.Experimental.VFX
{
    [UsedByNativeCode]
    [NativeHeader("Modules/VFX/Public/ScriptBindings/VisualEffectAssetBindings.h")]
    [NativeHeader("Modules/VFX/Public/VisualEffectAsset.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    public class VisualEffectAsset : Object
    {
    }

    [NativeHeader("Modules/VFX/Public/VisualEffect.h")]
    [RequireComponent(typeof(Transform))]
    public class VisualEffect : Behaviour
    {
        extern public bool pause { get; set; }
        extern public float playRate { get; set; }
        extern public uint startSeed { get; set; }

        extern public bool resetSeedOnPlay { get; set; }
        extern public bool culled { get; }

        extern public VisualEffectAsset visualEffectAsset { get; set; }

        public VFXEventAttribute CreateVFXEventAttribute()
        {
            if (visualEffectAsset == null)
                return null;
            var vfxEventAttribute = VFXEventAttribute.Internal_InstanciateVFXEventAttribute(visualEffectAsset);
            return vfxEventAttribute;
        }

        extern public void Play(VFXEventAttribute eventAttribute);

        public void Play()
        {
            Play(null);
        }

        extern public void Stop(VFXEventAttribute eventAttribute);
        public void Stop()
        {
            Stop(null);
        }

        extern public void SendEvent(int eventNameID, VFXEventAttribute eventAttribute);
        public void SendEvent(string eventName)
        {
            SendEvent(Shader.PropertyToID(eventName), null);
        }

        public void SendEvent(string eventName, VFXEventAttribute eventAttribute)
        {
            SendEvent(Shader.PropertyToID(eventName), eventAttribute);
        }

        extern public void Reinit();
        extern public void AdvanceOneFrame();

        [NativeName("ResetOverrideFromScript")] extern public void ResetOverride(int nameID);

        // Values check
        [NativeName("GetTextureDimensionFromScript")] extern public UnityEngine.Rendering.TextureDimension GetTextureDimension(int nameID);
        [NativeName("HasValueFromScript<bool>")] extern public bool HasBool(int nameID);
        [NativeName("HasValueFromScript<int>")] extern public bool HasInt(int nameID);
        [NativeName("HasValueFromScript<UInt32>")] extern public bool HasUInt(int nameID);
        [NativeName("HasValueFromScript<float>")] extern public bool HasFloat(int nameID);
        [NativeName("HasValueFromScript<Vector2f>")] extern public bool HasVector2(int nameID);
        [NativeName("HasValueFromScript<Vector3f>")] extern public bool HasVector3(int nameID);
        [NativeName("HasValueFromScript<Vector4f>")] extern public bool HasVector4(int nameID);
        [NativeName("HasValueFromScript<Matrix4x4f>")] extern public bool HasMatrix4x4(int nameID);
        [NativeName("HasValueFromScript<Texture*>")] extern public bool HasTexture(int nameID);
        [NativeName("HasValueFromScript<AnimationCurve*>")] extern public bool HasAnimationCurve(int nameID);
        [NativeName("HasValueFromScript<Gradient*>")] extern public bool HasGradient(int nameID);
        [NativeName("HasValueFromScript<Mesh*>")] extern public bool HasMesh(int nameID);

        // Value setters
        [NativeName("SetValueFromScript<bool>")] extern public void SetBool(int nameID, bool b);
        [NativeName("SetValueFromScript<int>")] extern public void SetInt(int nameID, int i);
        [NativeName("SetValueFromScript<UInt32>")] extern public void SetUInt(int nameID, uint i);
        [NativeName("SetValueFromScript<float>")] extern public void SetFloat(int nameID, float f);
        [NativeName("SetValueFromScript<Vector2f>")] extern public void SetVector2(int nameID, Vector2 v);
        [NativeName("SetValueFromScript<Vector3f>")] extern public void SetVector3(int nameID, Vector3 v);
        [NativeName("SetValueFromScript<Vector4f>")] extern public void SetVector4(int nameID, Vector4 v);
        [NativeName("SetValueFromScript<Matrix4x4f>")] extern public void SetMatrix4x4(int nameID, Matrix4x4 v);
        [NativeName("SetValueFromScript<Texture*>")] extern public void SetTexture(int nameID, Texture t);
        [NativeName("SetValueFromScript<AnimationCurve*>")] extern public void SetAnimationCurve(int nameID, AnimationCurve c);
        [NativeName("SetValueFromScript<Gradient*>")] extern public void SetGradient(int nameID, Gradient g);
        [NativeName("SetValueFromScript<Mesh*>")] extern public void SetMesh(int nameID, Mesh m);

        // Value getters
        [NativeName("GetValueFromScript<bool>")] extern public bool GetBool(int nameID);
        [NativeName("GetValueFromScript<int>")] extern public int GetInt(int nameID);
        [NativeName("GetValueFromScript<UInt32>")] extern public uint GetUInt(int nameID);
        [NativeName("GetValueFromScript<float>")] extern public float GetFloat(int nameID);
        [NativeName("GetValueFromScript<Vector2f>")] extern public Vector2 GetVector2(int nameID);
        [NativeName("GetValueFromScript<Vector3f>")] extern public Vector3 GetVector3(int nameID);
        [NativeName("GetValueFromScript<Vector4f>")] extern public Vector4 GetVector4(int nameID);
        [NativeName("GetValueFromScript<Matrix4x4f>")] extern public Matrix4x4 GetMatrix4x4(int nameID);
        [NativeName("GetValueFromScript<Texture*>")] extern public Texture GetTexture(int nameID);
        [NativeName("GetValueFromScript<Mesh*>")] extern public Mesh GetMesh(int nameID);
        public Gradient GetGradient(int nameID)
        {
            var gradient = new Gradient();
            Internal_GetGradient(nameID, gradient);
            return gradient;
        }

        [NativeName("Internal_GetGradientFromScript")] extern private void Internal_GetGradient(int nameID, Gradient gradient);

        public AnimationCurve GetAnimationCurve(int nameID)
        {
            var curve = new AnimationCurve();
            Internal_GetAnimationCurve(nameID, curve);
            return curve;
        }

        [NativeName("Internal_GetAnimationCurveFromScript")] extern private void Internal_GetAnimationCurve(int nameID, AnimationCurve curve);

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

        extern public int aliveParticleCount { get; }
    }

    // Bindings for VFXRenderer is needed but we dont want it to be accessible to users
    [NativeType(Header = "Modules/VFX/Public/VFXRenderer.h")]
    internal sealed partial class VFXRenderer : Renderer
    {
    }
}
