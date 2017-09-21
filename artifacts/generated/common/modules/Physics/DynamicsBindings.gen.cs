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

#pragma warning disable 649

namespace UnityEngine
{


public partial class Physics
{
    internal const float k_MaxFloatMinusEpsilon = 340282326356119260000000000000000000000f;
    public const int IgnoreRaycastLayer = 1 << 2;
    [System.Obsolete ("Please use Physics.IgnoreRaycastLayer instead. (UnityUpgradable) -> IgnoreRaycastLayer", true)]
    public const int kIgnoreRaycastLayer = IgnoreRaycastLayer;
    
    
    public const int DefaultRaycastLayers = ~IgnoreRaycastLayer;
    [System.Obsolete ("Please use Physics.DefaultRaycastLayers instead. (UnityUpgradable) -> DefaultRaycastLayers", true)]
    public const int kDefaultRaycastLayers = DefaultRaycastLayers;
    
    
    public const int AllLayers = ~0;
    [System.Obsolete ("Please use Physics.AllLayers instead. (UnityUpgradable) -> AllLayers", true)]
    public const int kAllLayers = AllLayers;
    
    
    [ThreadAndSerializationSafe ()]
    public static Vector3 gravity
    {
        get { Vector3 tmp; INTERNAL_get_gravity(out tmp); return tmp;  }
        set { INTERNAL_set_gravity(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_gravity (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_gravity (ref Vector3 value) ;

    [System.Obsolete ("use Physics.defaultContactOffset or Collider.contactOffset instead.", true)]
    public extern static float minPenetrationForPenalty
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float defaultContactOffset
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float bounceThreshold
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Please use bounceThreshold instead.")]
    static public  float bounceTreshold { get { return bounceThreshold; } set { bounceThreshold = value; }  }
    
    
    [System.Obsolete ("The sleepVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.")]
    public extern static float sleepVelocity
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("The sleepAngularVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.")]
    public extern static float sleepAngularVelocity
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("use Rigidbody.maxAngularVelocity instead.", true)]
    public extern static float maxAngularVelocity
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static int defaultSolverIterations
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Please use Physics.defaultSolverIterations instead. (UnityUpgradable) -> defaultSolverIterations")]
    public static int solverIterationCount { get { return defaultSolverIterations; } set { defaultSolverIterations = value; } }
    
    
    public extern static int defaultSolverVelocityIterations
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Please use Physics.defaultSolverVelocityIterations instead. (UnityUpgradable) -> defaultSolverVelocityIterations")]
    public static int solverVelocityIterationCount { get { return defaultSolverVelocityIterations; } set { defaultSolverVelocityIterations = value; } }
    
    
    public extern static float sleepThreshold
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool queriesHitTriggers
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool queriesHitBackfaces
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float interCollisionDistance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float interCollisionStiffness
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool interCollisionSettingsToggle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [uei.ExcludeFromDocs]
public static bool Raycast (Vector3 origin, Vector3 direction, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return Raycast ( origin, direction, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool Raycast (Vector3 origin, Vector3 direction, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return Raycast ( origin, direction, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool Raycast (Vector3 origin, Vector3 direction) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return Raycast ( origin, direction, maxDistance, layerMask, queryTriggerInteraction );
}

public static bool Raycast(Vector3 origin, Vector3 direction, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return Internal_RaycastTest(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [RequiredByNativeCode]
    [uei.ExcludeFromDocs]
public static bool Raycast (Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return Raycast ( origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool Raycast (Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return Raycast ( origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool Raycast (Vector3 origin, Vector3 direction, out RaycastHit hitInfo) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return Raycast ( origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return Internal_Raycast(origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool Raycast (Ray ray, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return Raycast ( ray, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool Raycast (Ray ray, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return Raycast ( ray, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool Raycast (Ray ray) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return Raycast ( ray, maxDistance, layerMask, queryTriggerInteraction );
}

public static bool Raycast(Ray ray, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return Raycast(ray.origin, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool Raycast (Ray ray, out RaycastHit hitInfo, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return Raycast ( ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool Raycast (Ray ray, out RaycastHit hitInfo, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return Raycast ( ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool Raycast (Ray ray, out RaycastHit hitInfo) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return Raycast ( ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

public static bool Raycast(Ray ray, out RaycastHit hitInfo, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [RequiredByNativeCode]
    [uei.ExcludeFromDocs]
public static RaycastHit[] RaycastAll (Ray ray, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return RaycastAll ( ray, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static RaycastHit[] RaycastAll (Ray ray, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return RaycastAll ( ray, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static RaycastHit[] RaycastAll (Ray ray) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return RaycastAll ( ray, maxDistance, layerMask, queryTriggerInteraction );
}

public static RaycastHit[] RaycastAll(Ray ray, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    public static RaycastHit[] RaycastAll (Vector3 origin, Vector3 direction, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layermask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_RaycastAll ( ref origin, ref direction, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static RaycastHit[] RaycastAll (Vector3 origin, Vector3 direction, float maxDistance , int layermask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_RaycastAll ( ref origin, ref direction, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static RaycastHit[] RaycastAll (Vector3 origin, Vector3 direction, float maxDistance ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        return INTERNAL_CALL_RaycastAll ( ref origin, ref direction, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static RaycastHit[] RaycastAll (Vector3 origin, Vector3 direction) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        float maxDistance = Mathf.Infinity;
        return INTERNAL_CALL_RaycastAll ( ref origin, ref direction, maxDistance, layermask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static RaycastHit[] INTERNAL_CALL_RaycastAll (ref Vector3 origin, ref Vector3 direction, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction);
    [uei.ExcludeFromDocs]
public static int RaycastNonAlloc (Ray ray, RaycastHit[] results, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return RaycastNonAlloc ( ray, results, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static int RaycastNonAlloc (Ray ray, RaycastHit[] results, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return RaycastNonAlloc ( ray, results, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static int RaycastNonAlloc (Ray ray, RaycastHit[] results) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return RaycastNonAlloc ( ray, results, maxDistance, layerMask, queryTriggerInteraction );
}

public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return RaycastNonAlloc(ray.origin, ray.direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    public static int RaycastNonAlloc (Vector3 origin, Vector3 direction, RaycastHit[] results, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layermask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_RaycastNonAlloc ( ref origin, ref direction, results, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int RaycastNonAlloc (Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance , int layermask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_RaycastNonAlloc ( ref origin, ref direction, results, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int RaycastNonAlloc (Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        return INTERNAL_CALL_RaycastNonAlloc ( ref origin, ref direction, results, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int RaycastNonAlloc (Vector3 origin, Vector3 direction, RaycastHit[] results) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        float maxDistance = Mathf.Infinity;
        return INTERNAL_CALL_RaycastNonAlloc ( ref origin, ref direction, results, maxDistance, layermask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_RaycastNonAlloc (ref Vector3 origin, ref Vector3 direction, RaycastHit[] results, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction);
    [uei.ExcludeFromDocs]
public static bool Linecast (Vector3 start, Vector3 end, int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return Linecast ( start, end, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool Linecast (Vector3 start, Vector3 end) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return Linecast ( start, end, layerMask, queryTriggerInteraction );
}

public static bool Linecast(Vector3 start, Vector3 end, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            Vector3 dir = end - start;
            return Raycast(start, dir, dir.magnitude, layerMask, queryTriggerInteraction);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool Linecast (Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return Linecast ( start, end, out hitInfo, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool Linecast (Vector3 start, Vector3 end, out RaycastHit hitInfo) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return Linecast ( start, end, out hitInfo, layerMask, queryTriggerInteraction );
}

public static bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            Vector3 dir = end - start;
            return Raycast(start, dir, out hitInfo, dir.magnitude, layerMask, queryTriggerInteraction);
        }

    
    
    public static Collider[] OverlapSphere (Vector3 position, float radius, [uei.DefaultValue("AllLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_OverlapSphere ( ref position, radius, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static Collider[] OverlapSphere (Vector3 position, float radius, int layerMask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_OverlapSphere ( ref position, radius, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static Collider[] OverlapSphere (Vector3 position, float radius) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layerMask = AllLayers;
        return INTERNAL_CALL_OverlapSphere ( ref position, radius, layerMask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider[] INTERNAL_CALL_OverlapSphere (ref Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
    public static int OverlapSphereNonAlloc (Vector3 position, float radius, Collider[] results, [uei.DefaultValue("AllLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_OverlapSphereNonAlloc ( ref position, radius, results, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int OverlapSphereNonAlloc (Vector3 position, float radius, Collider[] results, int layerMask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_OverlapSphereNonAlloc ( ref position, radius, results, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int OverlapSphereNonAlloc (Vector3 position, float radius, Collider[] results) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layerMask = AllLayers;
        return INTERNAL_CALL_OverlapSphereNonAlloc ( ref position, radius, results, layerMask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_OverlapSphereNonAlloc (ref Vector3 position, float radius, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
    public static Collider[] OverlapCapsule (Vector3 point0, Vector3 point1, float radius, [uei.DefaultValue("AllLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_OverlapCapsule ( ref point0, ref point1, radius, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static Collider[] OverlapCapsule (Vector3 point0, Vector3 point1, float radius, int layerMask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_OverlapCapsule ( ref point0, ref point1, radius, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static Collider[] OverlapCapsule (Vector3 point0, Vector3 point1, float radius) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layerMask = AllLayers;
        return INTERNAL_CALL_OverlapCapsule ( ref point0, ref point1, radius, layerMask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider[] INTERNAL_CALL_OverlapCapsule (ref Vector3 point0, ref Vector3 point1, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
    public static int OverlapCapsuleNonAlloc (Vector3 point0, Vector3 point1, float radius, Collider[] results, [uei.DefaultValue("AllLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_OverlapCapsuleNonAlloc ( ref point0, ref point1, radius, results, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int OverlapCapsuleNonAlloc (Vector3 point0, Vector3 point1, float radius, Collider[] results, int layerMask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_OverlapCapsuleNonAlloc ( ref point0, ref point1, radius, results, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int OverlapCapsuleNonAlloc (Vector3 point0, Vector3 point1, float radius, Collider[] results) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layerMask = AllLayers;
        return INTERNAL_CALL_OverlapCapsuleNonAlloc ( ref point0, ref point1, radius, results, layerMask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_OverlapCapsuleNonAlloc (ref Vector3 point0, ref Vector3 point1, float radius, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
    [uei.ExcludeFromDocs]
public static bool CapsuleCast (Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return CapsuleCast ( point1, point2, radius, direction, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool CapsuleCast (Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return CapsuleCast ( point1, point2, radius, direction, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool CapsuleCast (Vector3 point1, Vector3 point2, float radius, Vector3 direction) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return CapsuleCast ( point1, point2, radius, direction, maxDistance, layerMask, queryTriggerInteraction );
}

public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            RaycastHit hitInfo;
            return Internal_CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool CapsuleCast (Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return CapsuleCast ( point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool CapsuleCast (Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return CapsuleCast ( point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool CapsuleCast (Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return CapsuleCast ( point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return Internal_CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool SphereCast (Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return SphereCast ( origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool SphereCast (Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return SphereCast ( origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool SphereCast (Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return SphereCast ( origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return Internal_CapsuleCast(origin, origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool SphereCast (Ray ray, float radius, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return SphereCast ( ray, radius, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool SphereCast (Ray ray, float radius, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return SphereCast ( ray, radius, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool SphereCast (Ray ray, float radius) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return SphereCast ( ray, radius, maxDistance, layerMask, queryTriggerInteraction );
}

public static bool SphereCast(Ray ray, float radius, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            RaycastHit hitInfo;
            return Internal_CapsuleCast(ray.origin, ray.origin, radius, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool SphereCast (Ray ray, float radius, out RaycastHit hitInfo, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return SphereCast ( ray, radius, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool SphereCast (Ray ray, float radius, out RaycastHit hitInfo, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return SphereCast ( ray, radius, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool SphereCast (Ray ray, float radius, out RaycastHit hitInfo) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return SphereCast ( ray, radius, out hitInfo, maxDistance, layerMask, queryTriggerInteraction );
}

public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return Internal_CapsuleCast(ray.origin, ray.origin, radius, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    public static RaycastHit[] CapsuleCastAll (Vector3 point1, Vector3 point2, float radius, Vector3 direction, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layermask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_CapsuleCastAll ( ref point1, ref point2, radius, ref direction, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static RaycastHit[] CapsuleCastAll (Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance , int layermask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_CapsuleCastAll ( ref point1, ref point2, radius, ref direction, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static RaycastHit[] CapsuleCastAll (Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        return INTERNAL_CALL_CapsuleCastAll ( ref point1, ref point2, radius, ref direction, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static RaycastHit[] CapsuleCastAll (Vector3 point1, Vector3 point2, float radius, Vector3 direction) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        float maxDistance = Mathf.Infinity;
        return INTERNAL_CALL_CapsuleCastAll ( ref point1, ref point2, radius, ref direction, maxDistance, layermask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static RaycastHit[] INTERNAL_CALL_CapsuleCastAll (ref Vector3 point1, ref Vector3 point2, float radius, ref Vector3 direction, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction);
    public static int CapsuleCastNonAlloc (Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layermask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_CapsuleCastNonAlloc ( ref point1, ref point2, radius, ref direction, results, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int CapsuleCastNonAlloc (Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance , int layermask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_CapsuleCastNonAlloc ( ref point1, ref point2, radius, ref direction, results, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int CapsuleCastNonAlloc (Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        return INTERNAL_CALL_CapsuleCastNonAlloc ( ref point1, ref point2, radius, ref direction, results, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int CapsuleCastNonAlloc (Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        float maxDistance = Mathf.Infinity;
        return INTERNAL_CALL_CapsuleCastNonAlloc ( ref point1, ref point2, radius, ref direction, results, maxDistance, layermask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_CapsuleCastNonAlloc (ref Vector3 point1, ref Vector3 point2, float radius, ref Vector3 direction, RaycastHit[] results, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction);
    [uei.ExcludeFromDocs]
public static RaycastHit[] SphereCastAll (Vector3 origin, float radius, Vector3 direction, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return SphereCastAll ( origin, radius, direction, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static RaycastHit[] SphereCastAll (Vector3 origin, float radius, Vector3 direction, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return SphereCastAll ( origin, radius, direction, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static RaycastHit[] SphereCastAll (Vector3 origin, float radius, Vector3 direction) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return SphereCastAll ( origin, radius, direction, maxDistance, layerMask, queryTriggerInteraction );
}

public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return CapsuleCastAll(origin, origin, radius, direction, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [uei.ExcludeFromDocs]
public static RaycastHit[] SphereCastAll (Ray ray, float radius, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return SphereCastAll ( ray, radius, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static RaycastHit[] SphereCastAll (Ray ray, float radius, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return SphereCastAll ( ray, radius, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static RaycastHit[] SphereCastAll (Ray ray, float radius) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return SphereCastAll ( ray, radius, maxDistance, layerMask, queryTriggerInteraction );
}

public static RaycastHit[] SphereCastAll(Ray ray, float radius, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return CapsuleCastAll(ray.origin, ray.origin, radius, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [uei.ExcludeFromDocs]
public static int SphereCastNonAlloc (Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return SphereCastNonAlloc ( origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static int SphereCastNonAlloc (Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return SphereCastNonAlloc ( origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static int SphereCastNonAlloc (Vector3 origin, float radius, Vector3 direction, RaycastHit[] results) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return SphereCastNonAlloc ( origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction );
}

public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return CapsuleCastNonAlloc(origin, origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [uei.ExcludeFromDocs]
public static int SphereCastNonAlloc (Ray ray, float radius, RaycastHit[] results, float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return SphereCastNonAlloc ( ray, radius, results, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static int SphereCastNonAlloc (Ray ray, float radius, RaycastHit[] results, float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return SphereCastNonAlloc ( ray, radius, results, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static int SphereCastNonAlloc (Ray ray, float radius, RaycastHit[] results) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return SphereCastNonAlloc ( ray, radius, results, maxDistance, layerMask, queryTriggerInteraction );
}

public static int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return CapsuleCastNonAlloc(ray.origin, ray.origin, radius, ray.direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    public static bool CheckSphere (Vector3 position, float radius, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_CheckSphere ( ref position, radius, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static bool CheckSphere (Vector3 position, float radius, int layerMask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_CheckSphere ( ref position, radius, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static bool CheckSphere (Vector3 position, float radius) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layerMask = DefaultRaycastLayers;
        return INTERNAL_CALL_CheckSphere ( ref position, radius, layerMask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CheckSphere (ref Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
    public static bool CheckCapsule (Vector3 start, Vector3 end, float radius, [uei.DefaultValue("DefaultRaycastLayers")]  int layermask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_CheckCapsule ( ref start, ref end, radius, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static bool CheckCapsule (Vector3 start, Vector3 end, float radius, int layermask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_CheckCapsule ( ref start, ref end, radius, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static bool CheckCapsule (Vector3 start, Vector3 end, float radius) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask =  DefaultRaycastLayers;
        return INTERNAL_CALL_CheckCapsule ( ref start, ref end, radius, layermask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CheckCapsule (ref Vector3 start, ref Vector3 end, float radius, int layermask, QueryTriggerInteraction queryTriggerInteraction);
    public static bool CheckBox (Vector3 center, Vector3 halfExtents, [uei.DefaultValue("Quaternion.identity")]  Quaternion orientation , [uei.DefaultValue("DefaultRaycastLayers")]  int layermask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_CheckBox ( ref center, ref halfExtents, ref orientation, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static bool CheckBox (Vector3 center, Vector3 halfExtents, Quaternion orientation , int layermask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_CheckBox ( ref center, ref halfExtents, ref orientation, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static bool CheckBox (Vector3 center, Vector3 halfExtents, Quaternion orientation ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask =  DefaultRaycastLayers;
        return INTERNAL_CALL_CheckBox ( ref center, ref halfExtents, ref orientation, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static bool CheckBox (Vector3 center, Vector3 halfExtents) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask =  DefaultRaycastLayers;
        Quaternion orientation = Quaternion.identity;
        return INTERNAL_CALL_CheckBox ( ref center, ref halfExtents, ref orientation, layermask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CheckBox (ref Vector3 center, ref Vector3 halfExtents, ref Quaternion orientation, int layermask, QueryTriggerInteraction queryTriggerInteraction);
    public static Collider[] OverlapBox (Vector3 center, Vector3 halfExtents, [uei.DefaultValue("Quaternion.identity")]  Quaternion orientation , [uei.DefaultValue("AllLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_OverlapBox ( ref center, ref halfExtents, ref orientation, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static Collider[] OverlapBox (Vector3 center, Vector3 halfExtents, Quaternion orientation , int layerMask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_OverlapBox ( ref center, ref halfExtents, ref orientation, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static Collider[] OverlapBox (Vector3 center, Vector3 halfExtents, Quaternion orientation ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layerMask = AllLayers;
        return INTERNAL_CALL_OverlapBox ( ref center, ref halfExtents, ref orientation, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static Collider[] OverlapBox (Vector3 center, Vector3 halfExtents) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layerMask = AllLayers;
        Quaternion orientation = Quaternion.identity;
        return INTERNAL_CALL_OverlapBox ( ref center, ref halfExtents, ref orientation, layerMask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider[] INTERNAL_CALL_OverlapBox (ref Vector3 center, ref Vector3 halfExtents, ref Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
    public static int OverlapBoxNonAlloc (Vector3 center, Vector3 halfExtents, Collider[] results, [uei.DefaultValue("Quaternion.identity")]  Quaternion orientation , [uei.DefaultValue("AllLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_OverlapBoxNonAlloc ( ref center, ref halfExtents, results, ref orientation, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int OverlapBoxNonAlloc (Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation , int layerMask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_OverlapBoxNonAlloc ( ref center, ref halfExtents, results, ref orientation, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int OverlapBoxNonAlloc (Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layerMask = AllLayers;
        return INTERNAL_CALL_OverlapBoxNonAlloc ( ref center, ref halfExtents, results, ref orientation, layerMask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int OverlapBoxNonAlloc (Vector3 center, Vector3 halfExtents, Collider[] results) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layerMask = AllLayers;
        Quaternion orientation = Quaternion.identity;
        return INTERNAL_CALL_OverlapBoxNonAlloc ( ref center, ref halfExtents, results, ref orientation, layerMask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_OverlapBoxNonAlloc (ref Vector3 center, ref Vector3 halfExtents, Collider[] results, ref Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
    public static RaycastHit[] BoxCastAll (Vector3 center, Vector3 halfExtents, Vector3 direction, [uei.DefaultValue("Quaternion.identity")]  Quaternion orientation , [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layermask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_BoxCastAll ( ref center, ref halfExtents, ref direction, ref orientation, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static RaycastHit[] BoxCastAll (Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation , float maxDistance , int layermask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_BoxCastAll ( ref center, ref halfExtents, ref direction, ref orientation, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static RaycastHit[] BoxCastAll (Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation , float maxDistance ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        return INTERNAL_CALL_BoxCastAll ( ref center, ref halfExtents, ref direction, ref orientation, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static RaycastHit[] BoxCastAll (Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        float maxDistance = Mathf.Infinity;
        return INTERNAL_CALL_BoxCastAll ( ref center, ref halfExtents, ref direction, ref orientation, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static RaycastHit[] BoxCastAll (Vector3 center, Vector3 halfExtents, Vector3 direction) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        float maxDistance = Mathf.Infinity;
        Quaternion orientation = Quaternion.identity;
        return INTERNAL_CALL_BoxCastAll ( ref center, ref halfExtents, ref direction, ref orientation, maxDistance, layermask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static RaycastHit[] INTERNAL_CALL_BoxCastAll (ref Vector3 center, ref Vector3 halfExtents, ref Vector3 direction, ref Quaternion orientation, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction);
    public static int BoxCastNonAlloc (Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, [uei.DefaultValue("Quaternion.identity")]  Quaternion orientation , [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layermask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_BoxCastNonAlloc ( ref center, ref halfExtents, ref direction, results, ref orientation, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int BoxCastNonAlloc (Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation , float maxDistance , int layermask ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_BoxCastNonAlloc ( ref center, ref halfExtents, ref direction, results, ref orientation, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int BoxCastNonAlloc (Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation , float maxDistance ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        return INTERNAL_CALL_BoxCastNonAlloc ( ref center, ref halfExtents, ref direction, results, ref orientation, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int BoxCastNonAlloc (Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        float maxDistance = Mathf.Infinity;
        return INTERNAL_CALL_BoxCastNonAlloc ( ref center, ref halfExtents, ref direction, results, ref orientation, maxDistance, layermask, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public static int BoxCastNonAlloc (Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        int layermask = DefaultRaycastLayers;
        float maxDistance = Mathf.Infinity;
        Quaternion orientation = Quaternion.identity;
        return INTERNAL_CALL_BoxCastNonAlloc ( ref center, ref halfExtents, ref direction, results, ref orientation, maxDistance, layermask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_BoxCastNonAlloc (ref Vector3 center, ref Vector3 halfExtents, ref Vector3 direction, RaycastHit[] results, ref Quaternion orientation, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction);
    private static bool Internal_BoxCast (Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction) {
        return INTERNAL_CALL_Internal_BoxCast ( ref center, ref halfExtents, ref orientation, ref direction, out hitInfo, maxDistance, layermask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_BoxCast (ref Vector3 center, ref Vector3 halfExtents, ref Quaternion orientation, ref Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction);
    [uei.ExcludeFromDocs]
public static bool BoxCast (Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation , float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return BoxCast ( center, halfExtents, direction, orientation, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool BoxCast (Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation , float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return BoxCast ( center, halfExtents, direction, orientation, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool BoxCast (Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return BoxCast ( center, halfExtents, direction, orientation, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool BoxCast (Vector3 center, Vector3 halfExtents, Vector3 direction) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    Quaternion orientation = Quaternion.identity;
    return BoxCast ( center, halfExtents, direction, orientation, maxDistance, layerMask, queryTriggerInteraction );
}

public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, [uei.DefaultValue("Quaternion.identity")]  Quaternion orientation , [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            RaycastHit hitInfo;
            return Internal_BoxCast(center, halfExtents, orientation, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [uei.ExcludeFromDocs]
public static bool BoxCast (Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation , float maxDistance , int layerMask ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    return BoxCast ( center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool BoxCast (Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation , float maxDistance ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    return BoxCast ( center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool BoxCast (Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation ) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    return BoxCast ( center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction );
}

[uei.ExcludeFromDocs]
public static bool BoxCast (Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo) {
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
    int layerMask = DefaultRaycastLayers;
    float maxDistance = Mathf.Infinity;
    Quaternion orientation = Quaternion.identity;
    return BoxCast ( center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction );
}

public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, [uei.DefaultValue("Quaternion.identity")]  Quaternion orientation , [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction )
        {
            return Internal_BoxCast(center, halfExtents, orientation, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

    
    
    [System.Obsolete ("penetrationPenaltyForce has no effect.")]
    public extern static float penetrationPenaltyForce
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
    extern public static  void IgnoreCollision (Collider collider1, Collider collider2, [uei.DefaultValue("true")]  bool ignore ) ;

    [uei.ExcludeFromDocs]
    public static void IgnoreCollision (Collider collider1, Collider collider2) {
        bool ignore = true;
        IgnoreCollision ( collider1, collider2, ignore );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void IgnoreLayerCollision (int layer1, int layer2, [uei.DefaultValue("true")]  bool ignore ) ;

    [uei.ExcludeFromDocs]
    public static void IgnoreLayerCollision (int layer1, int layer2) {
        bool ignore = true;
        IgnoreLayerCollision ( layer1, layer2, ignore );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetIgnoreLayerCollision (int layer1, int layer2) ;

    private static bool Internal_Raycast (Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction) {
        return INTERNAL_CALL_Internal_Raycast ( ref origin, ref direction, out hitInfo, maxDistance, layermask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_Raycast (ref Vector3 origin, ref Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction);
    private static bool Internal_CapsuleCast (Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction) {
        return INTERNAL_CALL_Internal_CapsuleCast ( ref point1, ref point2, radius, ref direction, out hitInfo, maxDistance, layermask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_CapsuleCast (ref Vector3 point1, ref Vector3 point2, float radius, ref Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction);
    private static bool Internal_RaycastTest (Vector3 origin, Vector3 direction, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction) {
        return INTERNAL_CALL_Internal_RaycastTest ( ref origin, ref direction, maxDistance, layermask, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_RaycastTest (ref Vector3 origin, ref Vector3 direction, float maxDistance, int layermask, QueryTriggerInteraction queryTriggerInteraction);
    public static bool ComputePenetration (Collider colliderA, Vector3 positionA, Quaternion rotationA, Collider colliderB, Vector3 positionB, Quaternion rotationB, out Vector3 direction, out float distance) {
        return INTERNAL_CALL_ComputePenetration ( colliderA, ref positionA, ref rotationA, colliderB, ref positionB, ref rotationB, out direction, out distance );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_ComputePenetration (Collider colliderA, ref Vector3 positionA, ref Quaternion rotationA, Collider colliderB, ref Vector3 positionB, ref Quaternion rotationB, out Vector3 direction, out float distance);
    public static Vector3 ClosestPoint (Vector3 point, Collider collider, Vector3 position, Quaternion rotation) {
        Vector3 result;
        INTERNAL_CALL_ClosestPoint ( ref point, collider, ref position, ref rotation, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ClosestPoint (ref Vector3 point, Collider collider, ref Vector3 position, ref Quaternion rotation, out Vector3 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Simulate (float step) ;

    public extern static bool autoSimulation
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
    extern public static  void SyncTransforms () ;

    public extern static bool autoSyncTransforms
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public static void RebuildBroadphaseRegions (Bounds worldBounds, int subdivisions) {
        INTERNAL_CALL_RebuildBroadphaseRegions ( ref worldBounds, subdivisions );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_RebuildBroadphaseRegions (ref Bounds worldBounds, int subdivisions);
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct ContactPoint
{
    internal Vector3  m_Point;
    internal Vector3  m_Normal;
    internal int m_ThisColliderInstanceID;
    internal int m_OtherColliderInstanceID;
    internal float m_Separation;
    
    
    public Vector3 point  { get { return m_Point; } }
    
    
    public Vector3 normal { get { return m_Normal; } }
    
    
    public Collider thisCollider { get { return ColliderFromInstanceId(m_ThisColliderInstanceID); } }
    
    
    public Collider otherCollider { get { return ColliderFromInstanceId(m_OtherColliderInstanceID); } }
    
    
    public float separation { get { return m_Separation; }}
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  Collider ColliderFromInstanceId (int instanceID) ;

}

[RequireComponent(typeof(Transform))]
public sealed partial class Rigidbody : Component
{
    public Vector3 velocity
    {
        get { Vector3 tmp; INTERNAL_get_velocity(out tmp); return tmp;  }
        set { INTERNAL_set_velocity(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_velocity (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_velocity (ref Vector3 value) ;

    public Vector3 angularVelocity
    {
        get { Vector3 tmp; INTERNAL_get_angularVelocity(out tmp); return tmp;  }
        set { INTERNAL_set_angularVelocity(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_angularVelocity (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_angularVelocity (ref Vector3 value) ;

    public extern float drag
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float angularDrag
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float mass
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public void SetDensity (float density) {
        INTERNAL_CALL_SetDensity ( this, density );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetDensity (Rigidbody self, float density);
    public extern bool useGravity
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float maxDepenetrationVelocity
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool isKinematic
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool freezeRotation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern RigidbodyConstraints constraints
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern CollisionDetectionMode collisionDetectionMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public void AddForce (Vector3 force, [uei.DefaultValue("ForceMode.Force")]  ForceMode mode ) {
        INTERNAL_CALL_AddForce ( this, ref force, mode );
    }

    [uei.ExcludeFromDocs]
    public void AddForce (Vector3 force) {
        ForceMode mode = ForceMode.Force;
        INTERNAL_CALL_AddForce ( this, ref force, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddForce (Rigidbody self, ref Vector3 force, ForceMode mode);
    [uei.ExcludeFromDocs]
public void AddForce (float x, float y, float z) {
    ForceMode mode = ForceMode.Force;
    AddForce ( x, y, z, mode );
}

public void AddForce(float x, float y, float z, [uei.DefaultValue("ForceMode.Force")]  ForceMode mode ) { AddForce(new Vector3(x, y, z), mode); }

    
    
    public void AddRelativeForce (Vector3 force, [uei.DefaultValue("ForceMode.Force")]  ForceMode mode ) {
        INTERNAL_CALL_AddRelativeForce ( this, ref force, mode );
    }

    [uei.ExcludeFromDocs]
    public void AddRelativeForce (Vector3 force) {
        ForceMode mode = ForceMode.Force;
        INTERNAL_CALL_AddRelativeForce ( this, ref force, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddRelativeForce (Rigidbody self, ref Vector3 force, ForceMode mode);
    [uei.ExcludeFromDocs]
public void AddRelativeForce (float x, float y, float z) {
    ForceMode mode = ForceMode.Force;
    AddRelativeForce ( x, y, z, mode );
}

public void AddRelativeForce(float x, float y, float z, [uei.DefaultValue("ForceMode.Force")]  ForceMode mode ) { AddRelativeForce(new Vector3(x, y, z), mode); }

    
    
    public void AddTorque (Vector3 torque, [uei.DefaultValue("ForceMode.Force")]  ForceMode mode ) {
        INTERNAL_CALL_AddTorque ( this, ref torque, mode );
    }

    [uei.ExcludeFromDocs]
    public void AddTorque (Vector3 torque) {
        ForceMode mode = ForceMode.Force;
        INTERNAL_CALL_AddTorque ( this, ref torque, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddTorque (Rigidbody self, ref Vector3 torque, ForceMode mode);
    [uei.ExcludeFromDocs]
public void AddTorque (float x, float y, float z) {
    ForceMode mode = ForceMode.Force;
    AddTorque ( x, y, z, mode );
}

public void AddTorque(float x, float y, float z, [uei.DefaultValue("ForceMode.Force")]  ForceMode mode ) { AddTorque(new Vector3(x, y, z), mode); }

    
    
    public void AddRelativeTorque (Vector3 torque, [uei.DefaultValue("ForceMode.Force")]  ForceMode mode ) {
        INTERNAL_CALL_AddRelativeTorque ( this, ref torque, mode );
    }

    [uei.ExcludeFromDocs]
    public void AddRelativeTorque (Vector3 torque) {
        ForceMode mode = ForceMode.Force;
        INTERNAL_CALL_AddRelativeTorque ( this, ref torque, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddRelativeTorque (Rigidbody self, ref Vector3 torque, ForceMode mode);
    [uei.ExcludeFromDocs]
public void AddRelativeTorque (float x, float y, float z) {
    ForceMode mode = ForceMode.Force;
    AddRelativeTorque ( x, y, z, mode );
}

public void AddRelativeTorque(float x, float y, float z, [uei.DefaultValue("ForceMode.Force")]  ForceMode mode ) { AddRelativeTorque(new Vector3(x, y, z), mode); }

    
    
    public void AddForceAtPosition (Vector3 force, Vector3 position, [uei.DefaultValue("ForceMode.Force")]  ForceMode mode ) {
        INTERNAL_CALL_AddForceAtPosition ( this, ref force, ref position, mode );
    }

    [uei.ExcludeFromDocs]
    public void AddForceAtPosition (Vector3 force, Vector3 position) {
        ForceMode mode = ForceMode.Force;
        INTERNAL_CALL_AddForceAtPosition ( this, ref force, ref position, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddForceAtPosition (Rigidbody self, ref Vector3 force, ref Vector3 position, ForceMode mode);
    public void AddExplosionForce (float explosionForce, Vector3 explosionPosition, float explosionRadius, [uei.DefaultValue("0.0F")]  float upwardsModifier , [uei.DefaultValue("ForceMode.Force")]  ForceMode mode ) {
        INTERNAL_CALL_AddExplosionForce ( this, explosionForce, ref explosionPosition, explosionRadius, upwardsModifier, mode );
    }

    [uei.ExcludeFromDocs]
    public void AddExplosionForce (float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier ) {
        ForceMode mode = ForceMode.Force;
        INTERNAL_CALL_AddExplosionForce ( this, explosionForce, ref explosionPosition, explosionRadius, upwardsModifier, mode );
    }

    [uei.ExcludeFromDocs]
    public void AddExplosionForce (float explosionForce, Vector3 explosionPosition, float explosionRadius) {
        ForceMode mode = ForceMode.Force;
        float upwardsModifier = 0.0F;
        INTERNAL_CALL_AddExplosionForce ( this, explosionForce, ref explosionPosition, explosionRadius, upwardsModifier, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddExplosionForce (Rigidbody self, float explosionForce, ref Vector3 explosionPosition, float explosionRadius, float upwardsModifier, ForceMode mode);
    public Vector3 ClosestPointOnBounds (Vector3 position) {
        Vector3 result;
        INTERNAL_CALL_ClosestPointOnBounds ( this, ref position, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ClosestPointOnBounds (Rigidbody self, ref Vector3 position, out Vector3 value);
    public Vector3 GetRelativePointVelocity (Vector3 relativePoint) {
        Vector3 result;
        INTERNAL_CALL_GetRelativePointVelocity ( this, ref relativePoint, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetRelativePointVelocity (Rigidbody self, ref Vector3 relativePoint, out Vector3 value);
    public Vector3 GetPointVelocity (Vector3 worldPoint) {
        Vector3 result;
        INTERNAL_CALL_GetPointVelocity ( this, ref worldPoint, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPointVelocity (Rigidbody self, ref Vector3 worldPoint, out Vector3 value);
    public Vector3 centerOfMass
    {
        get { Vector3 tmp; INTERNAL_get_centerOfMass(out tmp); return tmp;  }
        set { INTERNAL_set_centerOfMass(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_centerOfMass (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_centerOfMass (ref Vector3 value) ;

    public Vector3 worldCenterOfMass
    {
        get { Vector3 tmp; INTERNAL_get_worldCenterOfMass(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_worldCenterOfMass (out Vector3 value) ;


    public Quaternion inertiaTensorRotation
    {
        get { Quaternion tmp; INTERNAL_get_inertiaTensorRotation(out tmp); return tmp;  }
        set { INTERNAL_set_inertiaTensorRotation(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_inertiaTensorRotation (out Quaternion value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_inertiaTensorRotation (ref Quaternion value) ;

    public Vector3 inertiaTensor
    {
        get { Vector3 tmp; INTERNAL_get_inertiaTensor(out tmp); return tmp;  }
        set { INTERNAL_set_inertiaTensor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_inertiaTensor (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_inertiaTensor (ref Vector3 value) ;

    public extern bool detectCollisions
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Cone friction is no longer supported.")]
    public extern bool useConeFriction
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Vector3 position
    {
        get { Vector3 tmp; INTERNAL_get_position(out tmp); return tmp;  }
        set { INTERNAL_set_position(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_position (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_position (ref Vector3 value) ;

    public Quaternion rotation
    {
        get { Quaternion tmp; INTERNAL_get_rotation(out tmp); return tmp;  }
        set { INTERNAL_set_rotation(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_rotation (out Quaternion value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_rotation (ref Quaternion value) ;

    public void MovePosition (Vector3 position) {
        INTERNAL_CALL_MovePosition ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MovePosition (Rigidbody self, ref Vector3 position);
    public void MoveRotation (Quaternion rot) {
        INTERNAL_CALL_MoveRotation ( this, ref rot );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MoveRotation (Rigidbody self, ref Quaternion rot);
    public extern RigidbodyInterpolation interpolation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public void Sleep () {
        INTERNAL_CALL_Sleep ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Sleep (Rigidbody self);
    public bool IsSleeping () {
        return INTERNAL_CALL_IsSleeping ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IsSleeping (Rigidbody self);
    public void WakeUp () {
        INTERNAL_CALL_WakeUp ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_WakeUp (Rigidbody self);
    public void ResetCenterOfMass () {
        INTERNAL_CALL_ResetCenterOfMass ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ResetCenterOfMass (Rigidbody self);
    public void ResetInertiaTensor () {
        INTERNAL_CALL_ResetInertiaTensor ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ResetInertiaTensor (Rigidbody self);
    public extern int solverIterations
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Please use Rigidbody.solverIterations instead. (UnityUpgradable) -> solverIterations")]
    public int solverIterationCount { get { return solverIterations; } set { solverIterations = value; } }
    
    
    public extern int solverVelocityIterations
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Please use Rigidbody.solverVelocityIterations instead. (UnityUpgradable) -> solverVelocityIterations")]
    public int solverVelocityIterationCount { get { return solverVelocityIterations; } set { solverVelocityIterations = value; } }
    
    
    [System.Obsolete ("The sleepVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.")]
    public extern float sleepVelocity
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("The sleepAngularVelocity is no longer supported. Set Use sleepThreshold to specify energy.")]
    public extern float sleepAngularVelocity
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float sleepThreshold
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float maxAngularVelocity
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public bool SweepTest (Vector3 direction, out RaycastHit hitInfo, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_SweepTest ( this, ref direction, out hitInfo, maxDistance, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public bool SweepTest (Vector3 direction, out RaycastHit hitInfo, float maxDistance ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_SweepTest ( this, ref direction, out hitInfo, maxDistance, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public bool SweepTest (Vector3 direction, out RaycastHit hitInfo) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        float maxDistance = Mathf.Infinity;
        return INTERNAL_CALL_SweepTest ( this, ref direction, out hitInfo, maxDistance, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_SweepTest (Rigidbody self, ref Vector3 direction, out RaycastHit hitInfo, float maxDistance, QueryTriggerInteraction queryTriggerInteraction);
    public RaycastHit[] SweepTestAll (Vector3 direction, [uei.DefaultValue("Mathf.Infinity")]  float maxDistance , [uei.DefaultValue("QueryTriggerInteraction.UseGlobal")]  QueryTriggerInteraction queryTriggerInteraction ) {
        return INTERNAL_CALL_SweepTestAll ( this, ref direction, maxDistance, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public RaycastHit[] SweepTestAll (Vector3 direction, float maxDistance ) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        return INTERNAL_CALL_SweepTestAll ( this, ref direction, maxDistance, queryTriggerInteraction );
    }

    [uei.ExcludeFromDocs]
    public RaycastHit[] SweepTestAll (Vector3 direction) {
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        float maxDistance = Mathf.Infinity;
        return INTERNAL_CALL_SweepTestAll ( this, ref direction, maxDistance, queryTriggerInteraction );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static RaycastHit[] INTERNAL_CALL_SweepTestAll (Rigidbody self, ref Vector3 direction, float maxDistance, QueryTriggerInteraction queryTriggerInteraction);
    [System.Obsolete ("use Rigidbody.maxAngularVelocity instead.")]
public void SetMaxAngularVelocity(float a) { maxAngularVelocity = a; }
}

[RequireComponent(typeof(Rigidbody))]
[NativeClass("Unity::Joint")]
public partial class Joint : Component
{
    public extern Rigidbody connectedBody
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Vector3 axis
    {
        get { Vector3 tmp; INTERNAL_get_axis(out tmp); return tmp;  }
        set { INTERNAL_set_axis(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_axis (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_axis (ref Vector3 value) ;

    public Vector3 anchor
    {
        get { Vector3 tmp; INTERNAL_get_anchor(out tmp); return tmp;  }
        set { INTERNAL_set_anchor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_anchor (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_anchor (ref Vector3 value) ;

    public Vector3 connectedAnchor
    {
        get { Vector3 tmp; INTERNAL_get_connectedAnchor(out tmp); return tmp;  }
        set { INTERNAL_set_connectedAnchor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_connectedAnchor (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_connectedAnchor (ref Vector3 value) ;

    public extern bool autoConfigureConnectedAnchor
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float breakForce
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float breakTorque
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool enableCollision
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool enablePreprocessing
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public  Vector3 currentForce
    {
        get { Vector3 tmp; INTERNAL_get_currentForce(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_currentForce (out Vector3 value) ;


    public  Vector3 currentTorque
    {
        get { Vector3 tmp; INTERNAL_get_currentTorque(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_currentTorque (out Vector3 value) ;


    public extern float massScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float connectedMassScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    internal Matrix4x4 GetActorLocalPose (int actorIndex) {
        Matrix4x4 result;
        INTERNAL_CALL_GetActorLocalPose ( this, actorIndex, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetActorLocalPose (Joint self, int actorIndex, out Matrix4x4 value);
}

public sealed partial class HingeJoint : Joint
{
    public JointMotor motor
    {
        get { JointMotor tmp; INTERNAL_get_motor(out tmp); return tmp;  }
        set { INTERNAL_set_motor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_motor (out JointMotor value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_motor (ref JointMotor value) ;

    public JointLimits limits
    {
        get { JointLimits tmp; INTERNAL_get_limits(out tmp); return tmp;  }
        set { INTERNAL_set_limits(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_limits (out JointLimits value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_limits (ref JointLimits value) ;

    public JointSpring spring
    {
        get { JointSpring tmp; INTERNAL_get_spring(out tmp); return tmp;  }
        set { INTERNAL_set_spring(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_spring (out JointSpring value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_spring (ref JointSpring value) ;

    public extern bool useMotor
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useLimits
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useSpring
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float velocity
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float angle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

public sealed partial class SpringJoint : Joint
{
    public extern float spring
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float damper
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float minDistance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float maxDistance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float tolerance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class FixedJoint : Joint
{
}

public sealed partial class CharacterJoint : Joint
{
    public Vector3 swingAxis
    {
        get { Vector3 tmp; INTERNAL_get_swingAxis(out tmp); return tmp;  }
        set { INTERNAL_set_swingAxis(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_swingAxis (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_swingAxis (ref Vector3 value) ;

    public SoftJointLimitSpring twistLimitSpring
    {
        get { SoftJointLimitSpring tmp; INTERNAL_get_twistLimitSpring(out tmp); return tmp;  }
        set { INTERNAL_set_twistLimitSpring(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_twistLimitSpring (out SoftJointLimitSpring value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_twistLimitSpring (ref SoftJointLimitSpring value) ;

    public SoftJointLimitSpring swingLimitSpring
    {
        get { SoftJointLimitSpring tmp; INTERNAL_get_swingLimitSpring(out tmp); return tmp;  }
        set { INTERNAL_set_swingLimitSpring(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_swingLimitSpring (out SoftJointLimitSpring value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_swingLimitSpring (ref SoftJointLimitSpring value) ;

    public SoftJointLimit lowTwistLimit
    {
        get { SoftJointLimit tmp; INTERNAL_get_lowTwistLimit(out tmp); return tmp;  }
        set { INTERNAL_set_lowTwistLimit(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_lowTwistLimit (out SoftJointLimit value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_lowTwistLimit (ref SoftJointLimit value) ;

    public SoftJointLimit highTwistLimit
    {
        get { SoftJointLimit tmp; INTERNAL_get_highTwistLimit(out tmp); return tmp;  }
        set { INTERNAL_set_highTwistLimit(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_highTwistLimit (out SoftJointLimit value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_highTwistLimit (ref SoftJointLimit value) ;

    public SoftJointLimit swing1Limit
    {
        get { SoftJointLimit tmp; INTERNAL_get_swing1Limit(out tmp); return tmp;  }
        set { INTERNAL_set_swing1Limit(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_swing1Limit (out SoftJointLimit value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_swing1Limit (ref SoftJointLimit value) ;

    public SoftJointLimit swing2Limit
    {
        get { SoftJointLimit tmp; INTERNAL_get_swing2Limit(out tmp); return tmp;  }
        set { INTERNAL_set_swing2Limit(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_swing2Limit (out SoftJointLimit value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_swing2Limit (ref SoftJointLimit value) ;

    public extern bool enableProjection
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float projectionDistance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float projectionAngle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("TargetRotation not in use for Unity 5 and assumed disabled.", true)]
    public Quaternion targetRotation;
    [System.Obsolete ("TargetAngularVelocity not in use for Unity 5 and assumed disabled.", true)]
    public Vector3 targetAngularVelocity;
    [System.Obsolete ("RotationDrive not in use for Unity 5 and assumed disabled.", true)]
    public JointDrive rotationDrive;
}

public enum ConfigurableJointMotion
{
    
    Locked = 0,
    
    Limited = 1,
    
    Free = 2
}

public enum RotationDriveMode
{
    
    XYAndZ = 0,
    
    Slerp = 1
}

public sealed partial class ConfigurableJoint : Joint
{
    public Vector3 secondaryAxis
    {
        get { Vector3 tmp; INTERNAL_get_secondaryAxis(out tmp); return tmp;  }
        set { INTERNAL_set_secondaryAxis(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_secondaryAxis (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_secondaryAxis (ref Vector3 value) ;

    public extern ConfigurableJointMotion xMotion
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern ConfigurableJointMotion yMotion
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern ConfigurableJointMotion zMotion
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern ConfigurableJointMotion angularXMotion
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern ConfigurableJointMotion angularYMotion
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern ConfigurableJointMotion angularZMotion
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public SoftJointLimitSpring linearLimitSpring
    {
        get { SoftJointLimitSpring tmp; INTERNAL_get_linearLimitSpring(out tmp); return tmp;  }
        set { INTERNAL_set_linearLimitSpring(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_linearLimitSpring (out SoftJointLimitSpring value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_linearLimitSpring (ref SoftJointLimitSpring value) ;

    public SoftJointLimitSpring angularXLimitSpring
    {
        get { SoftJointLimitSpring tmp; INTERNAL_get_angularXLimitSpring(out tmp); return tmp;  }
        set { INTERNAL_set_angularXLimitSpring(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_angularXLimitSpring (out SoftJointLimitSpring value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_angularXLimitSpring (ref SoftJointLimitSpring value) ;

    public SoftJointLimitSpring angularYZLimitSpring
    {
        get { SoftJointLimitSpring tmp; INTERNAL_get_angularYZLimitSpring(out tmp); return tmp;  }
        set { INTERNAL_set_angularYZLimitSpring(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_angularYZLimitSpring (out SoftJointLimitSpring value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_angularYZLimitSpring (ref SoftJointLimitSpring value) ;

    public SoftJointLimit linearLimit
    {
        get { SoftJointLimit tmp; INTERNAL_get_linearLimit(out tmp); return tmp;  }
        set { INTERNAL_set_linearLimit(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_linearLimit (out SoftJointLimit value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_linearLimit (ref SoftJointLimit value) ;

    public SoftJointLimit lowAngularXLimit
    {
        get { SoftJointLimit tmp; INTERNAL_get_lowAngularXLimit(out tmp); return tmp;  }
        set { INTERNAL_set_lowAngularXLimit(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_lowAngularXLimit (out SoftJointLimit value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_lowAngularXLimit (ref SoftJointLimit value) ;

    public SoftJointLimit highAngularXLimit
    {
        get { SoftJointLimit tmp; INTERNAL_get_highAngularXLimit(out tmp); return tmp;  }
        set { INTERNAL_set_highAngularXLimit(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_highAngularXLimit (out SoftJointLimit value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_highAngularXLimit (ref SoftJointLimit value) ;

    public SoftJointLimit angularYLimit
    {
        get { SoftJointLimit tmp; INTERNAL_get_angularYLimit(out tmp); return tmp;  }
        set { INTERNAL_set_angularYLimit(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_angularYLimit (out SoftJointLimit value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_angularYLimit (ref SoftJointLimit value) ;

    public SoftJointLimit angularZLimit
    {
        get { SoftJointLimit tmp; INTERNAL_get_angularZLimit(out tmp); return tmp;  }
        set { INTERNAL_set_angularZLimit(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_angularZLimit (out SoftJointLimit value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_angularZLimit (ref SoftJointLimit value) ;

    public Vector3 targetPosition
    {
        get { Vector3 tmp; INTERNAL_get_targetPosition(out tmp); return tmp;  }
        set { INTERNAL_set_targetPosition(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_targetPosition (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_targetPosition (ref Vector3 value) ;

    public Vector3 targetVelocity
    {
        get { Vector3 tmp; INTERNAL_get_targetVelocity(out tmp); return tmp;  }
        set { INTERNAL_set_targetVelocity(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_targetVelocity (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_targetVelocity (ref Vector3 value) ;

    public JointDrive xDrive
    {
        get { JointDrive tmp; INTERNAL_get_xDrive(out tmp); return tmp;  }
        set { INTERNAL_set_xDrive(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_xDrive (out JointDrive value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_xDrive (ref JointDrive value) ;

    public JointDrive yDrive
    {
        get { JointDrive tmp; INTERNAL_get_yDrive(out tmp); return tmp;  }
        set { INTERNAL_set_yDrive(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_yDrive (out JointDrive value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_yDrive (ref JointDrive value) ;

    public JointDrive zDrive
    {
        get { JointDrive tmp; INTERNAL_get_zDrive(out tmp); return tmp;  }
        set { INTERNAL_set_zDrive(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_zDrive (out JointDrive value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_zDrive (ref JointDrive value) ;

    public Quaternion targetRotation
    {
        get { Quaternion tmp; INTERNAL_get_targetRotation(out tmp); return tmp;  }
        set { INTERNAL_set_targetRotation(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_targetRotation (out Quaternion value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_targetRotation (ref Quaternion value) ;

    public Vector3 targetAngularVelocity
    {
        get { Vector3 tmp; INTERNAL_get_targetAngularVelocity(out tmp); return tmp;  }
        set { INTERNAL_set_targetAngularVelocity(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_targetAngularVelocity (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_targetAngularVelocity (ref Vector3 value) ;

    public extern RotationDriveMode rotationDriveMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public JointDrive angularXDrive
    {
        get { JointDrive tmp; INTERNAL_get_angularXDrive(out tmp); return tmp;  }
        set { INTERNAL_set_angularXDrive(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_angularXDrive (out JointDrive value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_angularXDrive (ref JointDrive value) ;

    public JointDrive angularYZDrive
    {
        get { JointDrive tmp; INTERNAL_get_angularYZDrive(out tmp); return tmp;  }
        set { INTERNAL_set_angularYZDrive(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_angularYZDrive (out JointDrive value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_angularYZDrive (ref JointDrive value) ;

    public JointDrive slerpDrive
    {
        get { JointDrive tmp; INTERNAL_get_slerpDrive(out tmp); return tmp;  }
        set { INTERNAL_set_slerpDrive(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_slerpDrive (out JointDrive value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_slerpDrive (ref JointDrive value) ;

    public extern JointProjectionMode projectionMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float projectionDistance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float projectionAngle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool configuredInWorldSpace
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool swapBodies
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

[RequireComponent(typeof(Rigidbody))]
public sealed partial class ConstantForce : Behaviour
{
    public  Vector3 force
    {
        get { Vector3 tmp; INTERNAL_get_force(out tmp); return tmp;  }
        set { INTERNAL_set_force(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_force (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_force (ref Vector3 value) ;

    public  Vector3 relativeForce
    {
        get { Vector3 tmp; INTERNAL_get_relativeForce(out tmp); return tmp;  }
        set { INTERNAL_set_relativeForce(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_relativeForce (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_relativeForce (ref Vector3 value) ;

    public  Vector3 torque
    {
        get { Vector3 tmp; INTERNAL_get_torque(out tmp); return tmp;  }
        set { INTERNAL_set_torque(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_torque (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_torque (ref Vector3 value) ;

    public  Vector3 relativeTorque
    {
        get { Vector3 tmp; INTERNAL_get_relativeTorque(out tmp); return tmp;  }
        set { INTERNAL_set_relativeTorque(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_relativeTorque (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_relativeTorque (ref Vector3 value) ;

}

public enum CollisionDetectionMode
{
    
    Discrete = 0,
    
    Continuous = 1,
    
    ContinuousDynamic = 2
}

[RequiredByNativeCode]
[RequireComponent(typeof(Transform))]
public partial class Collider : Component
{
    public extern bool enabled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern Rigidbody attachedRigidbody
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern bool isTrigger
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float contactOffset
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  PhysicMaterial material
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Vector3 ClosestPointOnBounds (Vector3 position) {
        Vector3 result;
        INTERNAL_CALL_ClosestPointOnBounds ( this, ref position, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ClosestPointOnBounds (Collider self, ref Vector3 position, out Vector3 value);
    public Vector3 ClosestPoint (Vector3 position) {
        Vector3 result;
        INTERNAL_CALL_ClosestPoint ( this, ref position, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ClosestPoint (Collider self, ref Vector3 position, out Vector3 value);
    public extern  PhysicMaterial sharedMaterial
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Bounds bounds
    {
        get { Bounds tmp; INTERNAL_get_bounds(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_bounds (out Bounds value) ;


    private static bool Internal_Raycast (Collider col, Ray ray, out RaycastHit hitInfo, float maxDistance) {
        return INTERNAL_CALL_Internal_Raycast ( col, ref ray, out hitInfo, maxDistance );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_Raycast (Collider col, ref Ray ray, out RaycastHit hitInfo, float maxDistance);
    public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            return Internal_Raycast(this, ray, out hitInfo, maxDistance);
        }
    
    
}

[RequiredByNativeCode]
public sealed partial class BoxCollider : Collider
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

    [System.Obsolete ("use BoxCollider.size instead.")]
    public Vector3 extents { get { return size * 0.5F; }  set { size = value * 2.0F; } }
}

[RequiredByNativeCode]
public sealed partial class SphereCollider : Collider
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

    public extern float radius
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

[RequiredByNativeCode]
public sealed partial class MeshCollider : Collider
{
    public extern Mesh sharedMesh
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool convex
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern MeshColliderCookingOptions cookingOptions
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public bool inflateMesh
        {
            get
            {
                return (cookingOptions & MeshColliderCookingOptions.InflateConvexMesh) != 0;
            }
            set
            {
                MeshColliderCookingOptions newOptions = cookingOptions & ~MeshColliderCookingOptions.InflateConvexMesh;
                if (value)
                    newOptions |= MeshColliderCookingOptions.InflateConvexMesh;
                cookingOptions = newOptions;
            }
        }
    
    
    public extern float skinWidth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Configuring smooth sphere collisions is no longer needed. PhysX3 has a better behaviour in place.")]
    public bool smoothSphereCollisions { get { return true; } set {} }
}

[RequiredByNativeCode]
public sealed partial class CapsuleCollider : Collider
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

    public extern float radius
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float height
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern int direction
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct RaycastHit
{
    private Vector3   m_Point;
    private Vector3   m_Normal;
    private int       m_FaceID;
    private float     m_Distance;
    private Vector2   m_UV;
    private Collider  m_Collider;
    
    
    private static void CalculateRaycastTexCoord (out Vector2 output, Collider col, Vector2 uv, Vector3 point, int face, int index) {
        INTERNAL_CALL_CalculateRaycastTexCoord ( out output, col, ref uv, ref point, face, index );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CalculateRaycastTexCoord (out Vector2 output, Collider col, ref Vector2 uv, ref Vector3 point, int face, int index);
    public Vector3 point { get { return m_Point; } set { m_Point = value; } }
    
    
    public Vector3  normal { get { return m_Normal; } set { m_Normal = value; } }
    
    
    public Vector3 barycentricCoordinate { get { return new Vector3(1.0F - (m_UV.y + m_UV.x), m_UV.x, m_UV.y); } set { m_UV = value; } }
    
    
    public float    distance { get { return m_Distance; } set { m_Distance = value; } }
    
    
    public int    triangleIndex { get { return m_FaceID; }  }
    
    
    public Vector2 textureCoord { get { Vector2 coord; CalculateRaycastTexCoord(out coord, collider, m_UV, m_Point, m_FaceID, 0); return coord;  } }
    
    
    public Vector2 textureCoord2 { get { Vector2 coord; CalculateRaycastTexCoord(out coord, collider, m_UV, m_Point, m_FaceID, 1); return coord;  } }
    
    
    [System.Obsolete ("Use textureCoord2 instead")]
    public Vector2 textureCoord1 { get { Vector2 coord; CalculateRaycastTexCoord(out coord, collider, m_UV, m_Point, m_FaceID, 1); return coord;  } }
    
    
    public Vector2 lightmapCoord
        {
            get
            {
                Vector2 coord;
                CalculateRaycastTexCoord(out coord, collider, m_UV, m_Point, m_FaceID, 1);
                if (collider.GetComponent<Renderer>() != null)
                {
                    Vector4 st = collider.GetComponent<Renderer>().lightmapScaleOffset;
                    coord.x = coord.x * st.x + st.z;
                    coord.y = coord.y * st.y + st.w;
                }
                return coord;
            }
        }
    
    
    public Collider collider { get { return m_Collider; }   }
    
    
    
    
    public Rigidbody rigidbody { get { return collider != null ? collider.attachedRigidbody : null; }  }
    
    
    public Transform transform
        {
            get
            {
                Rigidbody body = rigidbody;
                if (body != null)
                    return body.transform;
                else if (collider != null)
                    return collider.transform;
                else
                    return null;
            }
        }
}

public sealed partial class PhysicMaterial : Object
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_CreateDynamicsMaterial ([Writable] PhysicMaterial mat, string name) ;

    public PhysicMaterial() { Internal_CreateDynamicsMaterial(this, null); }
    
    
    public PhysicMaterial(string name) { Internal_CreateDynamicsMaterial(this, name); }
    
    
    public extern float dynamicFriction
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float staticFriction
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float bounciness
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Use PhysicMaterial.bounciness instead", true)]
    public float bouncyness { get { return bounciness; } set { bounciness = value; } }
    
    
    [System.Obsolete ("Anisotropic friction is no longer supported since Unity 5.0.", true)]
    public Vector3 frictionDirection2 { get { return Vector3.zero; } set {} }
    
    
    [System.Obsolete ("Anisotropic friction is no longer supported since Unity 5.0.", true)]
    public extern  float dynamicFriction2
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Anisotropic friction is no longer supported since Unity 5.0.", true)]
    public extern  float staticFriction2
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern PhysicMaterialCombine frictionCombine
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern PhysicMaterialCombine bounceCombine
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("Anisotropic friction is no longer supported since Unity 5.0.", true)]
    public Vector3 frictionDirection { get { return Vector3.zero; } set {} }
}

public sealed partial class CharacterController : Collider
{
    public bool SimpleMove (Vector3 speed) {
        return INTERNAL_CALL_SimpleMove ( this, ref speed );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_SimpleMove (CharacterController self, ref Vector3 speed);
    public CollisionFlags Move (Vector3 motion) {
        return INTERNAL_CALL_Move ( this, ref motion );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static CollisionFlags INTERNAL_CALL_Move (CharacterController self, ref Vector3 motion);
    public extern bool isGrounded
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Vector3 velocity
    {
        get { Vector3 tmp; INTERNAL_get_velocity(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_velocity (out Vector3 value) ;


    public extern CollisionFlags collisionFlags
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float radius
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float height
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

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

    public extern float slopeLimit
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float stepOffset
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float skinWidth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float minMoveDistance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool detectCollisions
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool enableOverlapRecovery
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
