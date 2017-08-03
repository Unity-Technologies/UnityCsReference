// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine
{



public sealed partial class OcclusionArea : Component
{
    public Vector3 center
    {
        get { Vector3 tmp; INTERNAL_get_center(out tmp); return tmp;  }
        set { INTERNAL_set_center(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_center (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_center (ref Vector3 value) ;

    public Vector3 size
    {
        get { Vector3 tmp; INTERNAL_get_size(out tmp); return tmp;  }
        set { INTERNAL_set_size(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_size (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_size (ref Vector3 value) ;

}

public sealed partial class RenderSettings : Object
{
    public static SphericalHarmonicsL2 ambientProbe
    {
        get { SphericalHarmonicsL2 tmp; INTERNAL_get_ambientProbe(out tmp); return tmp;  }
        set { INTERNAL_set_ambientProbe(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_ambientProbe (out SphericalHarmonicsL2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_ambientProbe (ref SphericalHarmonicsL2 value) ;

    public static Color subtractiveShadowColor
    {
        get { Color tmp; INTERNAL_get_subtractiveShadowColor(out tmp); return tmp;  }
        set { INTERNAL_set_subtractiveShadowColor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_subtractiveShadowColor (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_subtractiveShadowColor (ref Color value) ;

    public extern static Material skybox
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static Light sun
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static Cubemap customReflection
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Reset () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Object GetRenderSettings () ;

}

public sealed partial class QualitySettings : Object
{
    public extern static string[] names
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetQualityLevel () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetQualityLevel (int index, [uei.DefaultValue("true")]  bool applyExpensiveChanges ) ;

    [uei.ExcludeFromDocs]
    public static void SetQualityLevel (int index) {
        bool applyExpensiveChanges = true;
        SetQualityLevel ( index, applyExpensiveChanges );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void IncreaseLevel ( [uei.DefaultValue("false")] bool applyExpensiveChanges ) ;

    [uei.ExcludeFromDocs]
    public static void IncreaseLevel () {
        bool applyExpensiveChanges = false;
        IncreaseLevel ( applyExpensiveChanges );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DecreaseLevel ( [uei.DefaultValue("false")] bool applyExpensiveChanges ) ;

    [uei.ExcludeFromDocs]
    public static void DecreaseLevel () {
        bool applyExpensiveChanges = false;
        DecreaseLevel ( applyExpensiveChanges );
    }

    public static Vector3 shadowCascade4Split
    {
        get { Vector3 tmp; INTERNAL_get_shadowCascade4Split(out tmp); return tmp;  }
        set { INTERNAL_set_shadowCascade4Split(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_shadowCascade4Split (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_shadowCascade4Split (ref Vector3 value) ;

    public extern static AnisotropicFiltering anisotropicFiltering
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static int maxQueuedFrames
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static BlendWeights blendWeights
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public enum TextureCompressionQuality
{
    
    Fast = 0,
    
    Normal = 50,
    
    Best = 100
}

[RequireComponent(typeof(Transform))]
internal sealed partial class Halo : Behaviour
{
}

[RequireComponent(typeof(Transform))]
public sealed partial class MeshFilter : Component
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

    public extern  Mesh sharedMesh
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public partial class SkinnedMeshRenderer : Renderer
{
    public extern  Transform[] bones
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern Transform rootBone
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern SkinQuality quality
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern Mesh sharedMesh
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public  Bounds localBounds
    {
        get { Bounds tmp; INTERNAL_get_localBounds(out tmp); return tmp;  }
        set { INTERNAL_set_localBounds(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_localBounds (out Bounds value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_localBounds (ref Bounds value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void BakeMesh (Mesh mesh) ;

    internal extern  Transform actualRootBone
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

[RequireComponent(typeof(Transform))]
public partial class Renderer : Component
{
    internal extern  Transform staticBatchRootTransform
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    internal extern  int staticBatchIndex
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SetStaticBatchInfo (int firstSubMesh, int subMeshCount) ;

    public extern  bool isPartOfStaticBatch
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Matrix4x4 worldToLocalMatrix
    {
        get { Matrix4x4 tmp; INTERNAL_get_worldToLocalMatrix(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_worldToLocalMatrix (out Matrix4x4 value) ;


    public Matrix4x4 localToWorldMatrix
    {
        get { Matrix4x4 tmp; INTERNAL_get_localToWorldMatrix(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_localToWorldMatrix (out Matrix4x4 value) ;


    public extern  Material material
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  Material sharedMaterial
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  Material[] materials
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  Material[] sharedMaterials
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public  Bounds bounds
    {
        get { Bounds tmp; INTERNAL_get_bounds(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_bounds (out Bounds value) ;


    public  Vector4 lightmapScaleOffset
    {
        get { Vector4 tmp; INTERNAL_get_lightmapScaleOffset(out tmp); return tmp;  }
        set { INTERNAL_set_lightmapScaleOffset(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_lightmapScaleOffset (out Vector4 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_lightmapScaleOffset (ref Vector4 value) ;

    public  Vector4 realtimeLightmapScaleOffset
    {
        get { Vector4 tmp; INTERNAL_get_realtimeLightmapScaleOffset(out tmp); return tmp;  }
        set { INTERNAL_set_realtimeLightmapScaleOffset(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_realtimeLightmapScaleOffset (out Vector4 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_realtimeLightmapScaleOffset (ref Vector4 value) ;

    public extern GameObject lightProbeProxyVolumeOverride
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern Transform probeAnchor
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetPropertyBlock (MaterialPropertyBlock properties) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void GetPropertyBlock (MaterialPropertyBlock dest) ;

    internal extern  int sortingGroupID
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  int sortingGroupOrder
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void RenderNow (int material) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetClosestReflectionProbesInternal (object result) ;

    public void GetClosestReflectionProbes(List<ReflectionProbeBlendInfo> result)
        {
            GetClosestReflectionProbesInternal(result);
        }
    
    
}

public static partial class RendererExtensions
{
    static public void UpdateGIMaterials(this Renderer renderer)
        {
            UpdateGIMaterialsForRenderer(renderer);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void UpdateGIMaterialsForRenderer (Renderer renderer) ;

}

public sealed partial class Projector : Behaviour
{
    public extern Material material
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class Skybox : Behaviour
{
    public extern  Material material
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class TrailRenderer : Renderer
{
    public extern  AnimationCurve widthCurve
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Color startColor
    {
        get { Color tmp; INTERNAL_get_startColor(out tmp); return tmp;  }
        set { INTERNAL_set_startColor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_startColor (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_startColor (ref Color value) ;

    public Color endColor
    {
        get { Color tmp; INTERNAL_get_endColor(out tmp); return tmp;  }
        set { INTERNAL_set_endColor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_endColor (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_endColor (ref Color value) ;

    public extern  Gradient colorGradient
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Vector3 GetPosition (int index) {
        Vector3 result;
        INTERNAL_CALL_GetPosition ( this, index, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPosition (TrailRenderer self, int index, out Vector3 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetPositions (Vector3[] positions) ;

}

public sealed partial class LineRenderer : Renderer
{
    public extern  AnimationCurve widthCurve
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Color startColor
    {
        get { Color tmp; INTERNAL_get_startColor(out tmp); return tmp;  }
        set { INTERNAL_set_startColor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_startColor (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_startColor (ref Color value) ;

    public Color endColor
    {
        get { Color tmp; INTERNAL_get_endColor(out tmp); return tmp;  }
        set { INTERNAL_set_endColor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_endColor (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_endColor (ref Color value) ;

    public extern  Gradient colorGradient
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public void SetPosition (int index, Vector3 position) {
        INTERNAL_CALL_SetPosition ( this, index, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetPosition (LineRenderer self, int index, ref Vector3 position);
    public Vector3 GetPosition (int index) {
        Vector3 result;
        INTERNAL_CALL_GetPosition ( this, index, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPosition (LineRenderer self, int index, out Vector3 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetPositions (Vector3[] positions) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetPositions (Vector3[] positions) ;

}

public sealed partial class MaterialPropertyBlock
{
    internal IntPtr m_Ptr;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void InitBlock () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void DestroyBlock () ;

    public MaterialPropertyBlock()    { InitBlock(); }
    ~MaterialPropertyBlock()          { DestroyBlock(); }
    
    
    public extern  bool isEmpty
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Clear () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetFloatImpl (int nameID, float value) ;

    private void SetVectorImpl (int nameID, Vector4 value) {
        INTERNAL_CALL_SetVectorImpl ( this, nameID, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetVectorImpl (MaterialPropertyBlock self, int nameID, ref Vector4 value);
    private void SetMatrixImpl (int nameID, Matrix4x4 value) {
        INTERNAL_CALL_SetMatrixImpl ( this, nameID, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetMatrixImpl (MaterialPropertyBlock self, int nameID, ref Matrix4x4 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetTextureImpl (int nameID, Texture value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetBufferImpl (int nameID, ComputeBuffer value) ;

    private void SetColorImpl (int nameID, Color value) {
        INTERNAL_CALL_SetColorImpl ( this, nameID, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetColorImpl (MaterialPropertyBlock self, int nameID, ref Color value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  System.Array ExtractArrayFromList (object list) ;

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetFloatArrayImpl (int nameID, float[] values, int count) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetVectorArrayImpl (int nameID, Vector4[] values, int count) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetMatrixArrayImpl (int nameID, Matrix4x4[] values, int count) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private float GetFloatImpl (int nameID) ;

    private Vector4 GetVectorImpl (int nameID) {
        Vector4 result;
        INTERNAL_CALL_GetVectorImpl ( this, nameID, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetVectorImpl (MaterialPropertyBlock self, int nameID, out Vector4 value);
    private Matrix4x4 GetMatrixImpl (int nameID) {
        Matrix4x4 result;
        INTERNAL_CALL_GetMatrixImpl ( this, nameID, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetMatrixImpl (MaterialPropertyBlock self, int nameID, out Matrix4x4 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private float[] GetFloatArrayImpl (int nameID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Vector4[] GetVectorArrayImpl (int nameID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Matrix4x4[] GetMatrixArrayImpl (int nameID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetFloatArrayImplList (int nameID, object list) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetVectorArrayImplList (int nameID, object list) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetMatrixArrayImplList (int nameID, object list) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Texture GetTextureImpl (int nameID) ;

}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct RenderBufferHelper
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetLoadAction (out RenderBuffer b) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetLoadAction (out RenderBuffer b, int a) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetStoreAction (out RenderBuffer b) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetStoreAction (out RenderBuffer b, int a) ;

    internal static IntPtr GetNativeRenderBufferPtr (IntPtr rb) {
        IntPtr result;
        INTERNAL_CALL_GetNativeRenderBufferPtr ( rb, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetNativeRenderBufferPtr (IntPtr rb, out IntPtr value);
}

public sealed partial class Graphics
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  System.Array ExtractArrayFromList (object list) ;

    
    
    [uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera , int submeshIndex , MaterialPropertyBlock properties , bool castShadows , bool receiveShadows ) {
    bool useLightProbes = true;
    DrawMesh ( mesh, position, rotation, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera , int submeshIndex , MaterialPropertyBlock properties , bool castShadows ) {
    bool useLightProbes = true;
    bool receiveShadows = true;
    DrawMesh ( mesh, position, rotation, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera , int submeshIndex , MaterialPropertyBlock properties ) {
    bool useLightProbes = true;
    bool receiveShadows = true;
    bool castShadows = true;
    DrawMesh ( mesh, position, rotation, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera , int submeshIndex ) {
    bool useLightProbes = true;
    bool receiveShadows = true;
    bool castShadows = true;
    MaterialPropertyBlock properties = null;
    DrawMesh ( mesh, position, rotation, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera ) {
    bool useLightProbes = true;
    bool receiveShadows = true;
    bool castShadows = true;
    MaterialPropertyBlock properties = null;
    int submeshIndex = 0;
    DrawMesh ( mesh, position, rotation, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer) {
    bool useLightProbes = true;
    bool receiveShadows = true;
    bool castShadows = true;
    MaterialPropertyBlock properties = null;
    int submeshIndex = 0;
    Camera camera = null;
    DrawMesh ( mesh, position, rotation, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, useLightProbes );
}

public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, [uei.DefaultValue("null")]  Camera camera , [uei.DefaultValue("0")]  int submeshIndex , [uei.DefaultValue("null")]  MaterialPropertyBlock properties , [uei.DefaultValue("true")]  bool castShadows , [uei.DefaultValue("true")]  bool receiveShadows , [uei.DefaultValue("true")]  bool useLightProbes )
        {
            DrawMesh(mesh, position, rotation, material, layer, camera, submeshIndex, properties, castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows, null, useLightProbes);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, UnityEngine.Rendering.ShadowCastingMode castShadows, bool receiveShadows , Transform probeAnchor ) {
    bool useLightProbes = true;
    DrawMesh ( mesh, position, rotation, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, UnityEngine.Rendering.ShadowCastingMode castShadows, bool receiveShadows ) {
    bool useLightProbes = true;
    Transform probeAnchor = null;
    DrawMesh ( mesh, position, rotation, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, UnityEngine.Rendering.ShadowCastingMode castShadows) {
    bool useLightProbes = true;
    Transform probeAnchor = null;
    bool receiveShadows = true;
    DrawMesh ( mesh, position, rotation, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, useLightProbes );
}

public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, UnityEngine.Rendering.ShadowCastingMode castShadows, [uei.DefaultValue("true")]  bool receiveShadows , [uei.DefaultValue("null")]  Transform probeAnchor , [uei.DefaultValue("true")]  bool useLightProbes )
        {
            DrawMeshImpl(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, useLightProbes);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera , int submeshIndex , MaterialPropertyBlock properties , bool castShadows , bool receiveShadows ) {
    bool useLightProbes = true;
    DrawMesh ( mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera , int submeshIndex , MaterialPropertyBlock properties , bool castShadows ) {
    bool useLightProbes = true;
    bool receiveShadows = true;
    DrawMesh ( mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera , int submeshIndex , MaterialPropertyBlock properties ) {
    bool useLightProbes = true;
    bool receiveShadows = true;
    bool castShadows = true;
    DrawMesh ( mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera , int submeshIndex ) {
    bool useLightProbes = true;
    bool receiveShadows = true;
    bool castShadows = true;
    MaterialPropertyBlock properties = null;
    DrawMesh ( mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera ) {
    bool useLightProbes = true;
    bool receiveShadows = true;
    bool castShadows = true;
    MaterialPropertyBlock properties = null;
    int submeshIndex = 0;
    DrawMesh ( mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material, int layer) {
    bool useLightProbes = true;
    bool receiveShadows = true;
    bool castShadows = true;
    MaterialPropertyBlock properties = null;
    int submeshIndex = 0;
    Camera camera = null;
    DrawMesh ( mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, useLightProbes );
}

public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, [uei.DefaultValue("null")]  Camera camera , [uei.DefaultValue("0")]  int submeshIndex , [uei.DefaultValue("null")]  MaterialPropertyBlock properties , [uei.DefaultValue("true")]  bool castShadows , [uei.DefaultValue("true")]  bool receiveShadows , [uei.DefaultValue("true")]  bool useLightProbes )
        {
            DrawMeshImpl(mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows, null, useLightProbes);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, UnityEngine.Rendering.ShadowCastingMode castShadows, bool receiveShadows , Transform probeAnchor ) {
    bool useLightProbes = true;
    DrawMesh ( mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, UnityEngine.Rendering.ShadowCastingMode castShadows, bool receiveShadows ) {
    bool useLightProbes = true;
    Transform probeAnchor = null;
    DrawMesh ( mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, useLightProbes );
}

[uei.ExcludeFromDocs]
public static void DrawMesh (Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, UnityEngine.Rendering.ShadowCastingMode castShadows) {
    bool useLightProbes = true;
    Transform probeAnchor = null;
    bool receiveShadows = true;
    DrawMesh ( mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, useLightProbes );
}

public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, UnityEngine.Rendering.ShadowCastingMode castShadows, [uei.DefaultValue("true")]  bool receiveShadows , [uei.DefaultValue("null")]  Transform probeAnchor , [uei.DefaultValue("true")]  bool useLightProbes )
        {
            DrawMeshImpl(mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, useLightProbes);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_DrawMeshMatrix (ref Internal_DrawMeshMatrixArguments arguments, MaterialPropertyBlock properties, Material material, Mesh mesh, Camera camera) ;

    private static void Internal_DrawMeshNow1 (Mesh mesh, int subsetIndex, Vector3 position, Quaternion rotation) {
        INTERNAL_CALL_Internal_DrawMeshNow1 ( mesh, subsetIndex, ref position, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_DrawMeshNow1 (Mesh mesh, int subsetIndex, ref Vector3 position, ref Quaternion rotation);
    private static void Internal_DrawMeshNow2 (Mesh mesh, int subsetIndex, Matrix4x4 matrix) {
        INTERNAL_CALL_Internal_DrawMeshNow2 ( mesh, subsetIndex, ref matrix );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_DrawMeshNow2 (Mesh mesh, int subsetIndex, ref Matrix4x4 matrix);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DrawProcedural (MeshTopology topology, int vertexCount, [uei.DefaultValue("1")]  int instanceCount ) ;

    [uei.ExcludeFromDocs]
    public static void DrawProcedural (MeshTopology topology, int vertexCount) {
        int instanceCount = 1;
        DrawProcedural ( topology, vertexCount, instanceCount );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DrawProceduralIndirect (MeshTopology topology, ComputeBuffer bufferWithArgs, [uei.DefaultValue("0")]  int argsOffset ) ;

    [uei.ExcludeFromDocs]
    public static void DrawProceduralIndirect (MeshTopology topology, ComputeBuffer bufferWithArgs) {
        int argsOffset = 0;
        DrawProceduralIndirect ( topology, bufferWithArgs, argsOffset );
    }

    internal static readonly int kMaxDrawMeshInstanceCount = Internal_GetMaxDrawMeshInstanceCount();
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetMaxDrawMeshInstanceCount () ;

    [uei.ExcludeFromDocs]
public static void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count , MaterialPropertyBlock properties , ShadowCastingMode castShadows , bool receiveShadows , int layer ) {
    Camera camera = null;
    DrawMeshInstanced ( mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count , MaterialPropertyBlock properties , ShadowCastingMode castShadows , bool receiveShadows ) {
    Camera camera = null;
    int layer = 0;
    DrawMeshInstanced ( mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count , MaterialPropertyBlock properties , ShadowCastingMode castShadows ) {
    Camera camera = null;
    int layer = 0;
    bool receiveShadows = true;
    DrawMeshInstanced ( mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count , MaterialPropertyBlock properties ) {
    Camera camera = null;
    int layer = 0;
    bool receiveShadows = true;
    ShadowCastingMode castShadows = ShadowCastingMode.On;
    DrawMeshInstanced ( mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count ) {
    Camera camera = null;
    int layer = 0;
    bool receiveShadows = true;
    ShadowCastingMode castShadows = ShadowCastingMode.On;
    MaterialPropertyBlock properties = null;
    DrawMeshInstanced ( mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices) {
    Camera camera = null;
    int layer = 0;
    bool receiveShadows = true;
    ShadowCastingMode castShadows = ShadowCastingMode.On;
    MaterialPropertyBlock properties = null;
    int count = matrices.Length;
    DrawMeshInstanced ( mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera );
}

public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, [uei.DefaultValue("matrices.Length")]  int count , [uei.DefaultValue("null")]  MaterialPropertyBlock properties , [uei.DefaultValue("ShadowCastingMode.On")]  ShadowCastingMode castShadows , [uei.DefaultValue("true")]  bool receiveShadows , [uei.DefaultValue("0")]  int layer , [uei.DefaultValue("null")]  Camera camera )
        {
            DrawMeshInstancedImpl(mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties , ShadowCastingMode castShadows , bool receiveShadows , int layer ) {
    Camera camera = null;
    DrawMeshInstanced ( mesh, submeshIndex, material, matrices, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties , ShadowCastingMode castShadows , bool receiveShadows ) {
    Camera camera = null;
    int layer = 0;
    DrawMeshInstanced ( mesh, submeshIndex, material, matrices, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties , ShadowCastingMode castShadows ) {
    Camera camera = null;
    int layer = 0;
    bool receiveShadows = true;
    DrawMeshInstanced ( mesh, submeshIndex, material, matrices, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties ) {
    Camera camera = null;
    int layer = 0;
    bool receiveShadows = true;
    ShadowCastingMode castShadows = ShadowCastingMode.On;
    DrawMeshInstanced ( mesh, submeshIndex, material, matrices, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices) {
    Camera camera = null;
    int layer = 0;
    bool receiveShadows = true;
    ShadowCastingMode castShadows = ShadowCastingMode.On;
    MaterialPropertyBlock properties = null;
    DrawMeshInstanced ( mesh, submeshIndex, material, matrices, properties, castShadows, receiveShadows, layer, camera );
}

public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, [uei.DefaultValue("null")]  MaterialPropertyBlock properties , [uei.DefaultValue("ShadowCastingMode.On")]  ShadowCastingMode castShadows , [uei.DefaultValue("true")]  bool receiveShadows , [uei.DefaultValue("0")]  int layer , [uei.DefaultValue("null")]  Camera camera )
        {
            DrawMeshInstancedImpl(mesh, submeshIndex, material, matrices, properties, castShadows, receiveShadows, layer, camera);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_DrawMeshInstanced (Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera) ;

    [uei.ExcludeFromDocs]
public static void DrawMeshInstancedIndirect (Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset , MaterialPropertyBlock properties , ShadowCastingMode castShadows , bool receiveShadows , int layer ) {
    Camera camera = null;
    DrawMeshInstancedIndirect ( mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstancedIndirect (Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset , MaterialPropertyBlock properties , ShadowCastingMode castShadows , bool receiveShadows ) {
    Camera camera = null;
    int layer = 0;
    DrawMeshInstancedIndirect ( mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstancedIndirect (Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset , MaterialPropertyBlock properties , ShadowCastingMode castShadows ) {
    Camera camera = null;
    int layer = 0;
    bool receiveShadows = true;
    DrawMeshInstancedIndirect ( mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstancedIndirect (Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset , MaterialPropertyBlock properties ) {
    Camera camera = null;
    int layer = 0;
    bool receiveShadows = true;
    ShadowCastingMode castShadows = ShadowCastingMode.On;
    DrawMeshInstancedIndirect ( mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstancedIndirect (Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset ) {
    Camera camera = null;
    int layer = 0;
    bool receiveShadows = true;
    ShadowCastingMode castShadows = ShadowCastingMode.On;
    MaterialPropertyBlock properties = null;
    DrawMeshInstancedIndirect ( mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera );
}

[uei.ExcludeFromDocs]
public static void DrawMeshInstancedIndirect (Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs) {
    Camera camera = null;
    int layer = 0;
    bool receiveShadows = true;
    ShadowCastingMode castShadows = ShadowCastingMode.On;
    MaterialPropertyBlock properties = null;
    int argsOffset = 0;
    DrawMeshInstancedIndirect ( mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera );
}

public static void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, [uei.DefaultValue("0")]  int argsOffset , [uei.DefaultValue("null")]  MaterialPropertyBlock properties , [uei.DefaultValue("ShadowCastingMode.On")]  ShadowCastingMode castShadows , [uei.DefaultValue("true")]  bool receiveShadows , [uei.DefaultValue("0")]  int layer , [uei.DefaultValue("null")]  Camera camera )
        {
            DrawMeshInstancedIndirectImpl(mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera);
        }

    
    
    private static void Internal_DrawMeshInstancedIndirect (Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera) {
        INTERNAL_CALL_Internal_DrawMeshInstancedIndirect ( mesh, submeshIndex, material, ref bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_DrawMeshInstancedIndirect (Mesh mesh, int submeshIndex, Material material, ref Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera);
    [uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, Material mat ) {
    int pass = -1;
    DrawTexture ( screenRect, texture, mat, pass );
}

[uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture) {
    int pass = -1;
    Material mat = null;
    DrawTexture ( screenRect, texture, mat, pass );
}

public static void DrawTexture(Rect screenRect, Texture texture, [uei.DefaultValue("null")]  Material mat , [uei.DefaultValue("-1")]  int pass )
        {
            DrawTexture(screenRect, texture, 0, 0, 0, 0, mat, pass);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Material mat ) {
    int pass = -1;
    DrawTexture ( screenRect, texture, leftBorder, rightBorder, topBorder, bottomBorder, mat, pass );
}

[uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, int leftBorder, int rightBorder, int topBorder, int bottomBorder) {
    int pass = -1;
    Material mat = null;
    DrawTexture ( screenRect, texture, leftBorder, rightBorder, topBorder, bottomBorder, mat, pass );
}

public static void DrawTexture(Rect screenRect, Texture texture, int leftBorder, int rightBorder, int topBorder, int bottomBorder, [uei.DefaultValue("null")]  Material mat , [uei.DefaultValue("-1")]  int pass )
        {
            DrawTexture(screenRect, texture, new Rect(0, 0, 1, 1), leftBorder, rightBorder, topBorder, bottomBorder, mat, pass);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Material mat ) {
    int pass = -1;
    DrawTexture ( screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, mat, pass );
}

[uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder) {
    int pass = -1;
    Material mat = null;
    DrawTexture ( screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, mat, pass );
}

public static void DrawTexture(Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, [uei.DefaultValue("null")]  Material mat , [uei.DefaultValue("-1")]  int pass )
        {
            Color32 color = new Color32(128, 128, 128, 128);
            DrawTextureImpl(screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, color, mat, pass);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Color color, Material mat ) {
    int pass = -1;
    DrawTexture ( screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, color, mat, pass );
}

[uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Color color) {
    int pass = -1;
    Material mat = null;
    DrawTexture ( screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, color, mat, pass );
}

public static void DrawTexture(Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Color color, [uei.DefaultValue("null")]  Material mat , [uei.DefaultValue("-1")]  int pass )
        {
            DrawTextureImpl(screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, color, mat, pass);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Internal_DrawTexture (ref Internal_DrawTextureArguments args) ;

    [uei.ExcludeFromDocs]
public static GPUFence CreateGPUFence () {
    SynchronisationStage stage = SynchronisationStage.PixelProcessing;
    return CreateGPUFence ( stage );
}

public static GPUFence CreateGPUFence( [uei.DefaultValue("SynchronisationStage.PixelProcessing")] SynchronisationStage stage )
        {
            GPUFence newFence = new GPUFence();
            newFence.m_Ptr = Internal_CreateGPUFence(stage);
            newFence.InitPostAllocation();
            newFence.Validate();
            return newFence;

        }

    
    
    private static IntPtr Internal_CreateGPUFence (SynchronisationStage stage) {
        IntPtr result;
        INTERNAL_CALL_Internal_CreateGPUFence ( stage, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_CreateGPUFence (SynchronisationStage stage, out IntPtr value);
    [uei.ExcludeFromDocs]
public static void WaitOnGPUFence (GPUFence fence) {
    SynchronisationStage stage = SynchronisationStage.VertexProcessing;
    WaitOnGPUFence ( fence, stage );
}

public static void WaitOnGPUFence(GPUFence fence, [uei.DefaultValue("SynchronisationStage.VertexProcessing")]  SynchronisationStage stage )
        {
            fence.Validate();

            if (fence.IsFencePending())
                WaitOnGPUFence_Internal(fence.m_Ptr, stage);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void WaitOnGPUFence_Internal (IntPtr fencePtr, SynchronisationStage stage) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ExecuteCommandBuffer (UnityEngine.Rendering.CommandBuffer buffer) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ExecuteCommandBufferAsync (UnityEngine.Rendering.CommandBuffer buffer, ComputeQueueType queueType) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Blit (Texture source, RenderTexture dest) ;

    public static void Blit (Texture source, RenderTexture dest, Vector2 scale, Vector2 offset) {
        INTERNAL_CALL_Blit ( source, dest, ref scale, ref offset );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Blit (Texture source, RenderTexture dest, ref Vector2 scale, ref Vector2 offset);
    [uei.ExcludeFromDocs]
public static void Blit (Texture source, RenderTexture dest, Material mat) {
    int pass = -1;
    Blit ( source, dest, mat, pass );
}

public static void Blit(Texture source, RenderTexture dest, Material mat, [uei.DefaultValue("-1")]  int pass )
        {
            Internal_BlitMaterial(source, dest, mat, pass, true, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f));
        }

    
    
    [uei.ExcludeFromDocs]
public static void Blit (Texture source, Material mat) {
    int pass = -1;
    Blit ( source, mat, pass );
}

public static void Blit(Texture source, Material mat, [uei.DefaultValue("-1")]  int pass )
        {
            Internal_BlitMaterial(source, null, mat, pass, false, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f));
        }

    
    
    private static void Internal_BlitMaterial (Texture source, RenderTexture dest, Material mat, int pass, bool setRT, Vector2 scale, Vector2 offset) {
        INTERNAL_CALL_Internal_BlitMaterial ( source, dest, mat, pass, setRT, ref scale, ref offset );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_BlitMaterial (Texture source, RenderTexture dest, Material mat, int pass, bool setRT, ref Vector2 scale, ref Vector2 offset);
    public static void BlitMultiTap(Texture source, RenderTexture dest, Material mat, params Vector2[] offsets)
        {
            Internal_BlitMultiTap(source, dest, mat, offsets);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_BlitMultiTap (Texture source, RenderTexture dest, Material mat, Vector2[] offsets) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void CopyTexture_Full (Texture src, Texture dst) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void CopyTexture_Slice_AllMips (Texture src, int srcElement, Texture dst, int dstElement) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void CopyTexture_Slice (Texture src, int srcElement, int srcMip, Texture dst, int dstElement, int dstMip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void CopyTexture_Region (Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, Texture dst, int dstElement, int dstMip, int dstX, int dstY) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool ConvertTexture_Full (Texture src, Texture dst) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool ConvertTexture_Slice (Texture src, int srcElement, Texture dst, int dstElement) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetNullRT () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetRTSimple (out RenderBuffer color, out RenderBuffer depth, int mip, CubemapFace face, int depthSlice) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetMRTFullSetup (
            RenderBuffer[] colorSA, out RenderBuffer depth, int mip, CubemapFace face, int depthSlice,
            Rendering.RenderBufferLoadAction[] colorLoadSA, Rendering.RenderBufferStoreAction[] colorStoreSA,
            Rendering.RenderBufferLoadAction depthLoad, Rendering.RenderBufferStoreAction depthStore
            ) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetMRTSimple (RenderBuffer[] colorSA, out RenderBuffer depth, int mip, CubemapFace face, int depthSlice) ;

    public static RenderBuffer activeColorBuffer { get { RenderBuffer res; GetActiveColorBuffer(out res); return res; } }
    
    
    public static RenderBuffer activeDepthBuffer { get { RenderBuffer res; GetActiveDepthBuffer(out res); return res; } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void GetActiveColorBuffer (out RenderBuffer res) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void GetActiveDepthBuffer (out RenderBuffer res) ;

    public static void SetRandomWriteTarget(int index, RenderTexture uav)
        {
            Internal_SetRandomWriteTargetRT(index, uav);
        }
    
    
    [uei.ExcludeFromDocs]
public static void SetRandomWriteTarget (int index, ComputeBuffer uav) {
    bool preserveCounterValue = false;
    SetRandomWriteTarget ( index, uav, preserveCounterValue );
}

public static void SetRandomWriteTarget(int index, ComputeBuffer uav, [uei.DefaultValue("false")]  bool preserveCounterValue )
        {
            if (uav == null) throw new ArgumentNullException("uav");
            if (uav.m_Ptr == IntPtr.Zero) throw new System.ObjectDisposedException("uav");

            Internal_SetRandomWriteTargetBuffer(index, uav, preserveCounterValue);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearRandomWriteTargets () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetRandomWriteTargetRT (int index, RenderTexture uav) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetRandomWriteTargetBuffer (int index, ComputeBuffer uav, bool preserveCounterValue) ;

    public extern static GraphicsTier activeTier
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static ColorGamut activeColorGamut
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

[StructLayout(LayoutKind.Sequential)]
[UsedByNativeCode]
public sealed partial class LightmapData
{
    internal Texture2D m_Light;
    internal Texture2D m_Dir;
    internal Texture2D m_ShadowMask;
    
    
    [System.Obsolete ("Use lightmapColor property (UnityUpgradable) -> lightmapColor")]
    public Texture2D lightmapLight { get { return m_Light; } set { m_Light = value; } }
    
    
    public Texture2D lightmapColor { get { return m_Light; } set { m_Light = value; } }
    
    
    public Texture2D lightmapDir { get { return m_Dir; } set { m_Dir = value; } }
    
    
    public Texture2D shadowMask { get { return m_ShadowMask; } set { m_ShadowMask = value; } }
}

public enum LightmapsModeLegacy
{
    
    Single = 0,
    
    Dual = 1,
    
    Directional = 2,
}

public sealed partial class LightProbes : Object
{
    public static void GetInterpolatedProbe (Vector3 position, Renderer renderer, out SphericalHarmonicsL2 probe) {
        INTERNAL_CALL_GetInterpolatedProbe ( ref position, renderer, out probe );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetInterpolatedProbe (ref Vector3 position, Renderer renderer, out SphericalHarmonicsL2 probe);
    public extern  Vector3[] positions
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  UnityEngine.Rendering.SphericalHarmonicsL2[] bakedProbes
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  int count
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int cellCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

public sealed partial class LightmapSettings : Object
{
    public extern static LightmapData[] lightmaps
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static LightmapsMode lightmapsMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static LightProbes lightProbes
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Reset () ;

}

public sealed partial class GeometryUtility
{
    private static void Internal_ExtractPlanes (Plane[] planes, Matrix4x4 worldToProjectionMatrix) {
        INTERNAL_CALL_Internal_ExtractPlanes ( planes, ref worldToProjectionMatrix );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_ExtractPlanes (Plane[] planes, ref Matrix4x4 worldToProjectionMatrix);
    public static bool TestPlanesAABB (Plane[] planes, Bounds bounds) {
        return INTERNAL_CALL_TestPlanesAABB ( planes, ref bounds );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_TestPlanesAABB (Plane[] planes, ref Bounds bounds);
    private static Bounds Internal_CalculateBounds (Vector3[] positions, Matrix4x4 transform) {
        Bounds result;
        INTERNAL_CALL_Internal_CalculateBounds ( positions, ref transform, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_CalculateBounds (Vector3[] positions, ref Matrix4x4 transform, out Bounds value);
}

public sealed partial class Screen
{
    public extern static Resolution[] resolutions
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static Resolution currentResolution
    {
        get { Resolution tmp; INTERNAL_get_currentResolution(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_currentResolution (out Resolution value) ;


    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetResolution (int width, int height, bool fullscreen, [uei.DefaultValue("0")]  int preferredRefreshRate ) ;

    [uei.ExcludeFromDocs]
    public static void SetResolution (int width, int height, bool fullscreen) {
        int preferredRefreshRate = 0;
        SetResolution ( width, height, fullscreen, preferredRefreshRate );
    }

    public extern static bool fullScreen
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class GL
{
    public const int TRIANGLES = 0x0004;
    
    
    public const int TRIANGLE_STRIP = 0x0005;
    
    
    public const int QUADS = 0x0007;
    
    
    public const int LINES = 0x0001;
    
    
    public const int LINE_STRIP = 0x0002;
    
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void BeginInternal (int mode) ;

    public static void Begin(int mode)
        {
            BeginInternal(mode);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void End () ;

    public static void Viewport (Rect pixelRect) {
        INTERNAL_CALL_Viewport ( ref pixelRect );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Viewport (ref Rect pixelRect);
    public static void LoadProjectionMatrix (Matrix4x4 mat) {
        INTERNAL_CALL_LoadProjectionMatrix ( ref mat );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_LoadProjectionMatrix (ref Matrix4x4 mat);
    public static Matrix4x4 modelview
    {
        get { Matrix4x4 tmp; INTERNAL_get_modelview(out tmp); return tmp;  }
        set { INTERNAL_set_modelview(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_modelview (out Matrix4x4 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_modelview (ref Matrix4x4 value) ;

    public static void MultMatrix (Matrix4x4 mat) {
        INTERNAL_CALL_MultMatrix ( ref mat );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MultMatrix (ref Matrix4x4 mat);
    public static Matrix4x4 GetGPUProjectionMatrix (Matrix4x4 proj, bool renderIntoTexture) {
        Matrix4x4 result;
        INTERNAL_CALL_GetGPUProjectionMatrix ( ref proj, renderIntoTexture, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetGPUProjectionMatrix (ref Matrix4x4 proj, bool renderIntoTexture, out Matrix4x4 value);
    [System.Obsolete ("Use invertCulling property")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetRevertBackfacing (bool revertBackFaces) ;

    [uei.ExcludeFromDocs]
public static void Clear (bool clearDepth, bool clearColor, Color backgroundColor) {
    float depth = 1.0f;
    Clear ( clearDepth, clearColor, backgroundColor, depth );
}

public static void Clear(bool clearDepth, bool clearColor, Color backgroundColor, [uei.DefaultValue("1.0f")]  float depth )
        {
            Internal_Clear(clearDepth, clearColor, backgroundColor, depth);
        }

    
    
    private static void Internal_Clear (bool clearDepth, bool clearColor, Color backgroundColor, float depth) {
        INTERNAL_CALL_Internal_Clear ( clearDepth, clearColor, ref backgroundColor, depth );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_Clear (bool clearDepth, bool clearColor, ref Color backgroundColor, float depth);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearWithSkybox (bool clearDepth, Camera camera) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void InvalidateState () ;

    [System.Obsolete ("IssuePluginEvent(eventID) is deprecated. Use IssuePluginEvent(callback, eventID) instead.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void IssuePluginEvent (int eventID) ;

    static public void IssuePluginEvent(IntPtr callback, int eventID)
        {
            if (callback == IntPtr.Zero)
                throw new ArgumentException("Null callback specified.");

            IssuePluginEventInternal(callback, eventID);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void IssuePluginEventInternal (IntPtr callback, int eventID) ;

}

public sealed partial class MeshRenderer : Renderer
{
    public extern Mesh additionalVertexStreams
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}


}
