// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine
{



public partial class Physics2D
{
    public const int IgnoreRaycastLayer = 1 << 2;
    public const int DefaultRaycastLayers = ~IgnoreRaycastLayer;
    public const int AllLayers = ~0;
    
    
    
    
    
    [ThreadAndSerializationSafe ()]
    public extern static int velocityIterations
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static int positionIterations
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public static Vector2 gravity
    {
        get { Vector2 tmp; INTERNAL_get_gravity(out tmp); return tmp;  }
        set { INTERNAL_set_gravity(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_gravity (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_gravity (ref Vector2 value) ;

    public extern static bool queriesHitTriggers
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool queriesStartInColliders
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool changeStopsCallbacks
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float velocityThreshold
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float maxLinearCorrection
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float maxAngularCorrection
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float maxTranslationSpeed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float maxRotationSpeed
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

    public extern static float baumgarteScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float baumgarteTOIScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float timeToSleep
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float linearSleepTolerance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float angularSleepTolerance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool alwaysShowColliders
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool showColliderSleep
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool showColliderContacts
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool showColliderAABB
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float contactArrowScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public static Color colliderAwakeColor
    {
        get { Color tmp; INTERNAL_get_colliderAwakeColor(out tmp); return tmp;  }
        set { INTERNAL_set_colliderAwakeColor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_colliderAwakeColor (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_colliderAwakeColor (ref Color value) ;

    public static Color colliderAsleepColor
    {
        get { Color tmp; INTERNAL_get_colliderAsleepColor(out tmp); return tmp;  }
        set { INTERNAL_set_colliderAsleepColor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_colliderAsleepColor (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_colliderAsleepColor (ref Color value) ;

    public static Color colliderContactColor
    {
        get { Color tmp; INTERNAL_get_colliderContactColor(out tmp); return tmp;  }
        set { INTERNAL_set_colliderContactColor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_colliderContactColor (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_colliderContactColor (ref Color value) ;

    public static Color colliderAABBColor
    {
        get { Color tmp; INTERNAL_get_colliderAABBColor(out tmp); return tmp;  }
        set { INTERNAL_set_colliderAABBColor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_colliderAABBColor (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_colliderAABBColor (ref Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void IgnoreCollision (Collider2D collider1, Collider2D collider2, [uei.DefaultValue("true")]  bool ignore ) ;

    [uei.ExcludeFromDocs]
    public static void IgnoreCollision (Collider2D collider1, Collider2D collider2) {
        bool ignore = true;
        IgnoreCollision ( collider1, collider2, ignore );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetIgnoreCollision (Collider2D collider1, Collider2D collider2) ;

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

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetLayerCollisionMask (int layer, int layerMask) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetLayerCollisionMask (int layer) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsTouching (Collider2D collider1, Collider2D collider2) ;

    static public bool IsTouching(Collider2D collider1, Collider2D collider2, ContactFilter2D contactFilter) { return Physics2D.Internal_IsTouching(collider1, collider2, contactFilter); }
    private static bool Internal_IsTouching (Collider2D collider1, Collider2D collider2, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_IsTouching ( collider1, collider2, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_IsTouching (Collider2D collider1, Collider2D collider2, ref ContactFilter2D contactFilter);
    public static bool IsTouching (Collider2D collider, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_IsTouching ( collider, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IsTouching (Collider2D collider, ref ContactFilter2D contactFilter);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsTouchingLayers (Collider2D collider, [uei.DefaultValue("AllLayers")]  int layerMask ) ;

    [uei.ExcludeFromDocs]
    public static bool IsTouchingLayers (Collider2D collider) {
        int layerMask = AllLayers;
        return IsTouchingLayers ( collider, layerMask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  ColliderDistance2D Distance (Collider2D colliderA, Collider2D colliderB) ;

    internal static void SetEditorDragMovement(bool dragging, GameObject[] objs)
        {
            foreach (var body in m_LastDisabledRigidbody2D)
            {
                if (body != null)
                    body.SetDragBehaviour(false);
            }
            m_LastDisabledRigidbody2D.Clear();

            if (!dragging)
                return;

            foreach (var obj in objs)
            {
                var bodyComponents = obj.GetComponentsInChildren<Rigidbody2D>(false);
                foreach (var body in bodyComponents)
                {
                    m_LastDisabledRigidbody2D.Add(body);
                    body.SetDragBehaviour(true);
                }
            }
        }
    
    
    private static List<Rigidbody2D> m_LastDisabledRigidbody2D = new List<Rigidbody2D>();
    
    
    
    
    [uei.ExcludeFromDocs]
public static RaycastHit2D Linecast (Vector2 start, Vector2 end, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return Linecast ( start, end, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D Linecast (Vector2 start, Vector2 end, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return Linecast ( start, end, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D Linecast (Vector2 start, Vector2 end) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return Linecast ( start, end, layerMask, minDepth, maxDepth );
}

public static RaycastHit2D Linecast(Vector2 start, Vector2 end, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            RaycastHit2D raycastHit;
            Internal_Linecast(start, end, contactFilter, out raycastHit);
            return raycastHit;
        }

    
    
    [uei.ExcludeFromDocs]
public static RaycastHit2D[] LinecastAll (Vector2 start, Vector2 end, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return LinecastAll ( start, end, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] LinecastAll (Vector2 start, Vector2 end, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return LinecastAll ( start, end, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] LinecastAll (Vector2 start, Vector2 end) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return LinecastAll ( start, end, layerMask, minDepth, maxDepth );
}

public static RaycastHit2D[] LinecastAll(Vector2 start, Vector2 end, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_LinecastAll(start, end, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static int LinecastNonAlloc (Vector2 start, Vector2 end, RaycastHit2D[] results, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return LinecastNonAlloc ( start, end, results, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int LinecastNonAlloc (Vector2 start, Vector2 end, RaycastHit2D[] results, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return LinecastNonAlloc ( start, end, results, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int LinecastNonAlloc (Vector2 start, Vector2 end, RaycastHit2D[] results) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return LinecastNonAlloc ( start, end, results, layerMask, minDepth, maxDepth );
}

public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_LinecastNonAlloc(start, end, contactFilter, results);
        }

    
    
    public static int Linecast(Vector2 start, Vector2 end, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return Internal_LinecastNonAlloc(start, end, contactFilter, results);
        }
    
    
    private static void Internal_Linecast (Vector2 start, Vector2 end, ContactFilter2D contactFilter, out RaycastHit2D raycastHit) {
        INTERNAL_CALL_Internal_Linecast ( ref start, ref end, ref contactFilter, out raycastHit );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_Linecast (ref Vector2 start, ref Vector2 end, ref ContactFilter2D contactFilter, out RaycastHit2D raycastHit);
    private static RaycastHit2D[] Internal_LinecastAll (Vector2 start, Vector2 end, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_LinecastAll ( ref start, ref end, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static RaycastHit2D[] INTERNAL_CALL_Internal_LinecastAll (ref Vector2 start, ref Vector2 end, ref ContactFilter2D contactFilter);
    private static int Internal_LinecastNonAlloc (Vector2 start, Vector2 end, ContactFilter2D contactFilter, RaycastHit2D[] results) {
        return INTERNAL_CALL_Internal_LinecastNonAlloc ( ref start, ref end, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_LinecastNonAlloc (ref Vector2 start, ref Vector2 end, ref ContactFilter2D contactFilter, RaycastHit2D[] results);
    [RequiredByNativeCode]
    [uei.ExcludeFromDocs]
public static RaycastHit2D Raycast (Vector2 origin, Vector2 direction, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return Raycast ( origin, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D Raycast (Vector2 origin, Vector2 direction, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return Raycast ( origin, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D Raycast (Vector2 origin, Vector2 direction, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return Raycast ( origin, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D Raycast (Vector2 origin, Vector2 direction) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return Raycast ( origin, direction, distance, layerMask, minDepth, maxDepth );
}

public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            RaycastHit2D raycastHit;
            Internal_Raycast(origin, direction, distance, contactFilter, out raycastHit);
            return raycastHit;
        }

    
    
    [uei.ExcludeFromDocs]
public static RaycastHit2D[] RaycastAll (Vector2 origin, Vector2 direction, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return RaycastAll ( origin, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] RaycastAll (Vector2 origin, Vector2 direction, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return RaycastAll ( origin, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] RaycastAll (Vector2 origin, Vector2 direction, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return RaycastAll ( origin, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] RaycastAll (Vector2 origin, Vector2 direction) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return RaycastAll ( origin, direction, distance, layerMask, minDepth, maxDepth );
}

public static RaycastHit2D[] RaycastAll(Vector2 origin, Vector2 direction, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_RaycastAll(origin, direction, distance, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static int RaycastNonAlloc (Vector2 origin, Vector2 direction, RaycastHit2D[] results, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return RaycastNonAlloc ( origin, direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int RaycastNonAlloc (Vector2 origin, Vector2 direction, RaycastHit2D[] results, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return RaycastNonAlloc ( origin, direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int RaycastNonAlloc (Vector2 origin, Vector2 direction, RaycastHit2D[] results, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return RaycastNonAlloc ( origin, direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int RaycastNonAlloc (Vector2 origin, Vector2 direction, RaycastHit2D[] results) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return RaycastNonAlloc ( origin, direction, results, distance, layerMask, minDepth, maxDepth );
}

public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_RaycastNonAlloc(origin, direction, distance, contactFilter, results);
        }

    
    
    [uei.ExcludeFromDocs]
public static int Raycast (Vector2 origin, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results) {
    float distance = Mathf.Infinity;
    return Raycast ( origin, direction, contactFilter, results, distance );
}

public static int Raycast(Vector2 origin, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance )
        {
            return Internal_RaycastNonAlloc(origin, direction, distance, contactFilter, results);
        }

    
    
    private static void Internal_Raycast (Vector2 origin, Vector2 direction, float distance, ContactFilter2D contactFilter, out RaycastHit2D raycastHit) {
        INTERNAL_CALL_Internal_Raycast ( ref origin, ref direction, distance, ref contactFilter, out raycastHit );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_Raycast (ref Vector2 origin, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter, out RaycastHit2D raycastHit);
    private static RaycastHit2D[] Internal_RaycastAll (Vector2 origin, Vector2 direction, float distance, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_RaycastAll ( ref origin, ref direction, distance, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static RaycastHit2D[] INTERNAL_CALL_Internal_RaycastAll (ref Vector2 origin, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter);
    private static int Internal_RaycastNonAlloc (Vector2 origin, Vector2 direction, float distance, ContactFilter2D contactFilter, RaycastHit2D[] results) {
        return INTERNAL_CALL_Internal_RaycastNonAlloc ( ref origin, ref direction, distance, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_RaycastNonAlloc (ref Vector2 origin, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter, RaycastHit2D[] results);
    [uei.ExcludeFromDocs]
public static RaycastHit2D CircleCast (Vector2 origin, float radius, Vector2 direction, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return CircleCast ( origin, radius, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D CircleCast (Vector2 origin, float radius, Vector2 direction, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return CircleCast ( origin, radius, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D CircleCast (Vector2 origin, float radius, Vector2 direction, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return CircleCast ( origin, radius, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D CircleCast (Vector2 origin, float radius, Vector2 direction) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return CircleCast ( origin, radius, direction, distance, layerMask, minDepth, maxDepth );
}

public static RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            RaycastHit2D raycastHit;
            Internal_CircleCast(origin, radius, direction, distance, contactFilter, out raycastHit);
            return raycastHit;
        }

    
    
    [uei.ExcludeFromDocs]
public static RaycastHit2D[] CircleCastAll (Vector2 origin, float radius, Vector2 direction, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return CircleCastAll ( origin, radius, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] CircleCastAll (Vector2 origin, float radius, Vector2 direction, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return CircleCastAll ( origin, radius, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] CircleCastAll (Vector2 origin, float radius, Vector2 direction, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return CircleCastAll ( origin, radius, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] CircleCastAll (Vector2 origin, float radius, Vector2 direction) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return CircleCastAll ( origin, radius, direction, distance, layerMask, minDepth, maxDepth );
}

public static RaycastHit2D[] CircleCastAll(Vector2 origin, float radius, Vector2 direction, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_CircleCastAll(origin, radius, direction, distance, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static int CircleCastNonAlloc (Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return CircleCastNonAlloc ( origin, radius, direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int CircleCastNonAlloc (Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return CircleCastNonAlloc ( origin, radius, direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int CircleCastNonAlloc (Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return CircleCastNonAlloc ( origin, radius, direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int CircleCastNonAlloc (Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return CircleCastNonAlloc ( origin, radius, direction, results, distance, layerMask, minDepth, maxDepth );
}

public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_CircleCastNonAlloc(origin, radius, direction, distance, contactFilter, results);
        }

    
    
    [uei.ExcludeFromDocs]
public static int CircleCast (Vector2 origin, float radius, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results) {
    float distance = Mathf.Infinity;
    return CircleCast ( origin, radius, direction, contactFilter, results, distance );
}

public static int CircleCast(Vector2 origin, float radius, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance )
        {
            return Internal_CircleCastNonAlloc(origin, radius, direction, distance, contactFilter, results);
        }

    
    
    private static void Internal_CircleCast (Vector2 origin, float radius, Vector2 direction, float distance, ContactFilter2D contactFilter, out RaycastHit2D raycastHit) {
        INTERNAL_CALL_Internal_CircleCast ( ref origin, radius, ref direction, distance, ref contactFilter, out raycastHit );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_CircleCast (ref Vector2 origin, float radius, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter, out RaycastHit2D raycastHit);
    private static RaycastHit2D[] Internal_CircleCastAll (Vector2 origin, float radius, Vector2 direction, float distance, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_CircleCastAll ( ref origin, radius, ref direction, distance, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static RaycastHit2D[] INTERNAL_CALL_Internal_CircleCastAll (ref Vector2 origin, float radius, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter);
    private static int Internal_CircleCastNonAlloc (Vector2 origin, float radius, Vector2 direction, float distance, ContactFilter2D contactFilter, RaycastHit2D[] results) {
        return INTERNAL_CALL_Internal_CircleCastNonAlloc ( ref origin, radius, ref direction, distance, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_CircleCastNonAlloc (ref Vector2 origin, float radius, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter, RaycastHit2D[] results);
    [uei.ExcludeFromDocs]
public static RaycastHit2D BoxCast (Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return BoxCast ( origin, size, angle, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D BoxCast (Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return BoxCast ( origin, size, angle, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D BoxCast (Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return BoxCast ( origin, size, angle, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D BoxCast (Vector2 origin, Vector2 size, float angle, Vector2 direction) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return BoxCast ( origin, size, angle, direction, distance, layerMask, minDepth, maxDepth );
}

public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            RaycastHit2D raycastHit;
            Internal_BoxCast(origin, size, angle, direction, distance, contactFilter, out raycastHit);
            return raycastHit;
        }

    
    
    [uei.ExcludeFromDocs]
public static RaycastHit2D[] BoxCastAll (Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return BoxCastAll ( origin, size, angle, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] BoxCastAll (Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return BoxCastAll ( origin, size, angle, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] BoxCastAll (Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return BoxCastAll ( origin, size, angle, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] BoxCastAll (Vector2 origin, Vector2 size, float angle, Vector2 direction) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return BoxCastAll ( origin, size, angle, direction, distance, layerMask, minDepth, maxDepth );
}

public static RaycastHit2D[] BoxCastAll(Vector2 origin, Vector2 size, float angle, Vector2 direction, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_BoxCastAll(origin, size, angle, direction, distance, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static int BoxCastNonAlloc (Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return BoxCastNonAlloc ( origin, size, angle, direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int BoxCastNonAlloc (Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return BoxCastNonAlloc ( origin, size, angle, direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int BoxCastNonAlloc (Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return BoxCastNonAlloc ( origin, size, angle, direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int BoxCastNonAlloc (Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return BoxCastNonAlloc ( origin, size, angle, direction, results, distance, layerMask, minDepth, maxDepth );
}

public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_BoxCastNonAlloc(origin, size, angle, direction, distance, contactFilter, results);
        }

    
    
    [uei.ExcludeFromDocs]
public static int BoxCast (Vector2 origin, Vector2 size, float angle, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results) {
    float distance = Mathf.Infinity;
    return BoxCast ( origin, size, angle, direction, contactFilter, results, distance );
}

public static int BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance )
        {
            return Internal_BoxCastNonAlloc(origin, size, angle, direction, distance, contactFilter, results);
        }

    
    
    private static void Internal_BoxCast (Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, out RaycastHit2D raycastHit) {
        INTERNAL_CALL_Internal_BoxCast ( ref origin, ref size, angle, ref direction, distance, ref contactFilter, out raycastHit );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_BoxCast (ref Vector2 origin, ref Vector2 size, float angle, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter, out RaycastHit2D raycastHit);
    private static RaycastHit2D[] Internal_BoxCastAll (Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_BoxCastAll ( ref origin, ref size, angle, ref direction, distance, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static RaycastHit2D[] INTERNAL_CALL_Internal_BoxCastAll (ref Vector2 origin, ref Vector2 size, float angle, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter);
    private static int Internal_BoxCastNonAlloc (Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, RaycastHit2D[] results) {
        return INTERNAL_CALL_Internal_BoxCastNonAlloc ( ref origin, ref size, angle, ref direction, distance, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_BoxCastNonAlloc (ref Vector2 origin, ref Vector2 size, float angle, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter, RaycastHit2D[] results);
    [uei.ExcludeFromDocs]
public static RaycastHit2D CapsuleCast (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return CapsuleCast ( origin, size, capsuleDirection, angle, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D CapsuleCast (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return CapsuleCast ( origin, size, capsuleDirection, angle, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D CapsuleCast (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return CapsuleCast ( origin, size, capsuleDirection, angle, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D CapsuleCast (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return CapsuleCast ( origin, size, capsuleDirection, angle, direction, distance, layerMask, minDepth, maxDepth );
}

public static RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            RaycastHit2D raycastHit;
            Internal_CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter, out raycastHit);
            return raycastHit;
        }

    
    
    [uei.ExcludeFromDocs]
public static RaycastHit2D[] CapsuleCastAll (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return CapsuleCastAll ( origin, size, capsuleDirection, angle, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] CapsuleCastAll (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return CapsuleCastAll ( origin, size, capsuleDirection, angle, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] CapsuleCastAll (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return CapsuleCastAll ( origin, size, capsuleDirection, angle, direction, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D[] CapsuleCastAll (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return CapsuleCastAll ( origin, size, capsuleDirection, angle, direction, distance, layerMask, minDepth, maxDepth );
}

public static RaycastHit2D[] CapsuleCastAll(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_CapsuleCastAll(origin, size, capsuleDirection, angle, direction, distance, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static int CapsuleCastNonAlloc (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return CapsuleCastNonAlloc ( origin, size, capsuleDirection, angle, direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int CapsuleCastNonAlloc (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return CapsuleCastNonAlloc ( origin, size, capsuleDirection, angle, direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int CapsuleCastNonAlloc (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return CapsuleCastNonAlloc ( origin, size, capsuleDirection, angle, direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int CapsuleCastNonAlloc (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return CapsuleCastNonAlloc ( origin, size, capsuleDirection, angle, direction, results, distance, layerMask, minDepth, maxDepth );
}

public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_CapsuleCastNonAlloc(origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

    
    
    [uei.ExcludeFromDocs]
public static int CapsuleCast (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results) {
    float distance = Mathf.Infinity;
    return CapsuleCast ( origin, size, capsuleDirection, angle, direction, contactFilter, results, distance );
}

public static int CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction,  ContactFilter2D contactFilter, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance )
        {
            return Internal_CapsuleCastNonAlloc(origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

    
    
    private static void Internal_CapsuleCast (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, out RaycastHit2D raycastHit) {
        INTERNAL_CALL_Internal_CapsuleCast ( ref origin, ref size, capsuleDirection, angle, ref direction, distance, ref contactFilter, out raycastHit );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_CapsuleCast (ref Vector2 origin, ref Vector2 size, CapsuleDirection2D capsuleDirection, float angle, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter, out RaycastHit2D raycastHit);
    private static RaycastHit2D[] Internal_CapsuleCastAll (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_CapsuleCastAll ( ref origin, ref size, capsuleDirection, angle, ref direction, distance, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static RaycastHit2D[] INTERNAL_CALL_Internal_CapsuleCastAll (ref Vector2 origin, ref Vector2 size, CapsuleDirection2D capsuleDirection, float angle, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter);
    private static int Internal_CapsuleCastNonAlloc (Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, RaycastHit2D[] results) {
        return INTERNAL_CALL_Internal_CapsuleCastNonAlloc ( ref origin, ref size, capsuleDirection, angle, ref direction, distance, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_CapsuleCastNonAlloc (ref Vector2 origin, ref Vector2 size, CapsuleDirection2D capsuleDirection, float angle, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter, RaycastHit2D[] results);
    private static void Internal_GetRayIntersection (Ray ray, float distance, int layerMask, out RaycastHit2D raycastHit) {
        INTERNAL_CALL_Internal_GetRayIntersection ( ref ray, distance, layerMask, out raycastHit );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_GetRayIntersection (ref Ray ray, float distance, int layerMask, out RaycastHit2D raycastHit);
    [uei.ExcludeFromDocs]
public static RaycastHit2D GetRayIntersection (Ray ray, float distance ) {
    int layerMask = DefaultRaycastLayers;
    return GetRayIntersection ( ray, distance, layerMask );
}

[uei.ExcludeFromDocs]
public static RaycastHit2D GetRayIntersection (Ray ray) {
    int layerMask = DefaultRaycastLayers;
    float distance = Mathf.Infinity;
    return GetRayIntersection ( ray, distance, layerMask );
}

public static RaycastHit2D GetRayIntersection(Ray ray, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask )
        {
            RaycastHit2D raycastHit;
            Internal_GetRayIntersection(ray, distance, layerMask, out raycastHit);
            return raycastHit;
        }

    
    
    [RequiredByNativeCode]
    public static RaycastHit2D[] GetRayIntersectionAll (Ray ray, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask ) {
        return INTERNAL_CALL_GetRayIntersectionAll ( ref ray, distance, layerMask );
    }

    [uei.ExcludeFromDocs]
    public static RaycastHit2D[] GetRayIntersectionAll (Ray ray, float distance ) {
        int layerMask = DefaultRaycastLayers;
        return INTERNAL_CALL_GetRayIntersectionAll ( ref ray, distance, layerMask );
    }

    [uei.ExcludeFromDocs]
    public static RaycastHit2D[] GetRayIntersectionAll (Ray ray) {
        int layerMask = DefaultRaycastLayers;
        float distance = Mathf.Infinity;
        return INTERNAL_CALL_GetRayIntersectionAll ( ref ray, distance, layerMask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static RaycastHit2D[] INTERNAL_CALL_GetRayIntersectionAll (ref Ray ray, float distance, int layerMask);
    public static int GetRayIntersectionNonAlloc (Ray ray, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask ) {
        return INTERNAL_CALL_GetRayIntersectionNonAlloc ( ref ray, results, distance, layerMask );
    }

    [uei.ExcludeFromDocs]
    public static int GetRayIntersectionNonAlloc (Ray ray, RaycastHit2D[] results, float distance ) {
        int layerMask = DefaultRaycastLayers;
        return INTERNAL_CALL_GetRayIntersectionNonAlloc ( ref ray, results, distance, layerMask );
    }

    [uei.ExcludeFromDocs]
    public static int GetRayIntersectionNonAlloc (Ray ray, RaycastHit2D[] results) {
        int layerMask = DefaultRaycastLayers;
        float distance = Mathf.Infinity;
        return INTERNAL_CALL_GetRayIntersectionNonAlloc ( ref ray, results, distance, layerMask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetRayIntersectionNonAlloc (ref Ray ray, RaycastHit2D[] results, float distance, int layerMask);
    [uei.ExcludeFromDocs]
public static Collider2D OverlapPoint (Vector2 point, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapPoint ( point, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D OverlapPoint (Vector2 point, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapPoint ( point, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D OverlapPoint (Vector2 point) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapPoint ( point, layerMask, minDepth, maxDepth );
}

public static Collider2D OverlapPoint(Vector2 point, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapPoint(point, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static Collider2D[] OverlapPointAll (Vector2 point, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapPointAll ( point, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D[] OverlapPointAll (Vector2 point, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapPointAll ( point, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D[] OverlapPointAll (Vector2 point) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapPointAll ( point, layerMask, minDepth, maxDepth );
}

public static Collider2D[] OverlapPointAll(Vector2 point, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapPointAll(point, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static int OverlapPointNonAlloc (Vector2 point, Collider2D[] results, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapPointNonAlloc ( point, results, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int OverlapPointNonAlloc (Vector2 point, Collider2D[] results, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapPointNonAlloc ( point, results, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int OverlapPointNonAlloc (Vector2 point, Collider2D[] results) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapPointNonAlloc ( point, results, layerMask, minDepth, maxDepth );
}

public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapPointNonAlloc(point, contactFilter, results);
        }

    
    
    public static int OverlapPoint(Vector2 point, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return Internal_OverlapPointNonAlloc(point, contactFilter, results);
        }
    
    
    private static Collider2D Internal_OverlapPoint (Vector2 point, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_OverlapPoint ( ref point, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider2D INTERNAL_CALL_Internal_OverlapPoint (ref Vector2 point, ref ContactFilter2D contactFilter);
    private static Collider2D[] Internal_OverlapPointAll (Vector2 point, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_OverlapPointAll ( ref point, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider2D[] INTERNAL_CALL_Internal_OverlapPointAll (ref Vector2 point, ref ContactFilter2D contactFilter);
    private static int Internal_OverlapPointNonAlloc (Vector2 point, ContactFilter2D contactFilter, Collider2D[] results) {
        return INTERNAL_CALL_Internal_OverlapPointNonAlloc ( ref point, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_OverlapPointNonAlloc (ref Vector2 point, ref ContactFilter2D contactFilter, Collider2D[] results);
    [uei.ExcludeFromDocs]
public static Collider2D OverlapCircle (Vector2 point, float radius, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapCircle ( point, radius, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D OverlapCircle (Vector2 point, float radius, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapCircle ( point, radius, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D OverlapCircle (Vector2 point, float radius) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapCircle ( point, radius, layerMask, minDepth, maxDepth );
}

public static Collider2D OverlapCircle(Vector2 point, float radius, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapCircle(point, radius, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static Collider2D[] OverlapCircleAll (Vector2 point, float radius, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapCircleAll ( point, radius, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D[] OverlapCircleAll (Vector2 point, float radius, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapCircleAll ( point, radius, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D[] OverlapCircleAll (Vector2 point, float radius) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapCircleAll ( point, radius, layerMask, minDepth, maxDepth );
}

public static Collider2D[] OverlapCircleAll(Vector2 point, float radius, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapCircleAll(point, radius, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static int OverlapCircleNonAlloc (Vector2 point, float radius, Collider2D[] results, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapCircleNonAlloc ( point, radius, results, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int OverlapCircleNonAlloc (Vector2 point, float radius, Collider2D[] results, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapCircleNonAlloc ( point, radius, results, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int OverlapCircleNonAlloc (Vector2 point, float radius, Collider2D[] results) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapCircleNonAlloc ( point, radius, results, layerMask, minDepth, maxDepth );
}

public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapCircleNonAlloc(point, radius, contactFilter, results);
        }

    
    
    public static int OverlapCircle(Vector2 point, float radius, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return Internal_OverlapCircleNonAlloc(point, radius, contactFilter, results);
        }
    
    
    private static Collider2D Internal_OverlapCircle (Vector2 point, float radius, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_OverlapCircle ( ref point, radius, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider2D INTERNAL_CALL_Internal_OverlapCircle (ref Vector2 point, float radius, ref ContactFilter2D contactFilter);
    private static Collider2D[] Internal_OverlapCircleAll (Vector2 point, float radius, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_OverlapCircleAll ( ref point, radius, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider2D[] INTERNAL_CALL_Internal_OverlapCircleAll (ref Vector2 point, float radius, ref ContactFilter2D contactFilter);
    private static int Internal_OverlapCircleNonAlloc (Vector2 point, float radius, ContactFilter2D contactFilter, Collider2D[] results) {
        return INTERNAL_CALL_Internal_OverlapCircleNonAlloc ( ref point, radius, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_OverlapCircleNonAlloc (ref Vector2 point, float radius, ref ContactFilter2D contactFilter, Collider2D[] results);
    [uei.ExcludeFromDocs]
public static Collider2D OverlapBox (Vector2 point, Vector2 size, float angle, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapBox ( point, size, angle, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D OverlapBox (Vector2 point, Vector2 size, float angle, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapBox ( point, size, angle, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D OverlapBox (Vector2 point, Vector2 size, float angle) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapBox ( point, size, angle, layerMask, minDepth, maxDepth );
}

public static Collider2D OverlapBox(Vector2 point, Vector2 size, float angle, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapBox(point, size, angle, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static Collider2D[] OverlapBoxAll (Vector2 point, Vector2 size, float angle, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapBoxAll ( point, size, angle, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D[] OverlapBoxAll (Vector2 point, Vector2 size, float angle, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapBoxAll ( point, size, angle, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D[] OverlapBoxAll (Vector2 point, Vector2 size, float angle) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapBoxAll ( point, size, angle, layerMask, minDepth, maxDepth );
}

public static Collider2D[] OverlapBoxAll(Vector2 point, Vector2 size, float angle, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapBoxAll(point, size, angle, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static int OverlapBoxNonAlloc (Vector2 point, Vector2 size, float angle, Collider2D[] results, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapBoxNonAlloc ( point, size, angle, results, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int OverlapBoxNonAlloc (Vector2 point, Vector2 size, float angle, Collider2D[] results, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapBoxNonAlloc ( point, size, angle, results, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int OverlapBoxNonAlloc (Vector2 point, Vector2 size, float angle, Collider2D[] results) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapBoxNonAlloc ( point, size, angle, results, layerMask, minDepth, maxDepth );
}

public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapBoxNonAlloc(point, size, angle, contactFilter, results);
        }

    
    
    public static int OverlapBox(Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return Internal_OverlapBoxNonAlloc(point, size, angle, contactFilter, results);
        }
    
    
    private static Collider2D Internal_OverlapBox (Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_OverlapBox ( ref point, ref size, angle, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider2D INTERNAL_CALL_Internal_OverlapBox (ref Vector2 point, ref Vector2 size, float angle, ref ContactFilter2D contactFilter);
    private static Collider2D[] Internal_OverlapBoxAll (Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_OverlapBoxAll ( ref point, ref size, angle, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider2D[] INTERNAL_CALL_Internal_OverlapBoxAll (ref Vector2 point, ref Vector2 size, float angle, ref ContactFilter2D contactFilter);
    private static int Internal_OverlapBoxNonAlloc (Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter, Collider2D[] results) {
        return INTERNAL_CALL_Internal_OverlapBoxNonAlloc ( ref point, ref size, angle, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_OverlapBoxNonAlloc (ref Vector2 point, ref Vector2 size, float angle, ref ContactFilter2D contactFilter, Collider2D[] results);
    [uei.ExcludeFromDocs]
public static Collider2D OverlapArea (Vector2 pointA, Vector2 pointB, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapArea ( pointA, pointB, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D OverlapArea (Vector2 pointA, Vector2 pointB, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapArea ( pointA, pointB, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D OverlapArea (Vector2 pointA, Vector2 pointB) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapArea ( pointA, pointB, layerMask, minDepth, maxDepth );
}

public static Collider2D OverlapArea(Vector2 pointA, Vector2 pointB, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapArea(pointA, pointB, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static Collider2D[] OverlapAreaAll (Vector2 pointA, Vector2 pointB, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapAreaAll ( pointA, pointB, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D[] OverlapAreaAll (Vector2 pointA, Vector2 pointB, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapAreaAll ( pointA, pointB, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D[] OverlapAreaAll (Vector2 pointA, Vector2 pointB) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapAreaAll ( pointA, pointB, layerMask, minDepth, maxDepth );
}

public static Collider2D[] OverlapAreaAll(Vector2 pointA, Vector2 pointB, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapAreaAll(pointA, pointB, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static int OverlapAreaNonAlloc (Vector2 pointA, Vector2 pointB, Collider2D[] results, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapAreaNonAlloc ( pointA, pointB, results, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int OverlapAreaNonAlloc (Vector2 pointA, Vector2 pointB, Collider2D[] results, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapAreaNonAlloc ( pointA, pointB, results, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int OverlapAreaNonAlloc (Vector2 pointA, Vector2 pointB, Collider2D[] results) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapAreaNonAlloc ( pointA, pointB, results, layerMask, minDepth, maxDepth );
}

public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapAreaNonAlloc(pointA, pointB, contactFilter, results);
        }

    
    
    public static int OverlapArea(Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return Internal_OverlapAreaNonAlloc(pointA, pointB, contactFilter, results);
        }
    
    
    private static Collider2D Internal_OverlapArea (Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_OverlapArea ( ref pointA, ref pointB, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider2D INTERNAL_CALL_Internal_OverlapArea (ref Vector2 pointA, ref Vector2 pointB, ref ContactFilter2D contactFilter);
    private static Collider2D[] Internal_OverlapAreaAll (Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_OverlapAreaAll ( ref pointA, ref pointB, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider2D[] INTERNAL_CALL_Internal_OverlapAreaAll (ref Vector2 pointA, ref Vector2 pointB, ref ContactFilter2D contactFilter);
    private static int Internal_OverlapAreaNonAlloc (Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter, Collider2D[] results) {
        return INTERNAL_CALL_Internal_OverlapAreaNonAlloc ( ref pointA, ref pointB, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_OverlapAreaNonAlloc (ref Vector2 pointA, ref Vector2 pointB, ref ContactFilter2D contactFilter, Collider2D[] results);
    [uei.ExcludeFromDocs]
public static Collider2D OverlapCapsule (Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapCapsule ( point, size, direction, angle, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D OverlapCapsule (Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapCapsule ( point, size, direction, angle, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D OverlapCapsule (Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapCapsule ( point, size, direction, angle, layerMask, minDepth, maxDepth );
}

public static Collider2D OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapCapsule(point, size, direction, angle, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static Collider2D[] OverlapCapsuleAll (Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapCapsuleAll ( point, size, direction, angle, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D[] OverlapCapsuleAll (Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapCapsuleAll ( point, size, direction, angle, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static Collider2D[] OverlapCapsuleAll (Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapCapsuleAll ( point, size, direction, angle, layerMask, minDepth, maxDepth );
}

public static Collider2D[] OverlapCapsuleAll(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapCapsuleAll(point, size, direction, angle, contactFilter);
        }

    
    
    [uei.ExcludeFromDocs]
public static int OverlapCapsuleNonAlloc (Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return OverlapCapsuleNonAlloc ( point, size, direction, angle, results, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int OverlapCapsuleNonAlloc (Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return OverlapCapsuleNonAlloc ( point, size, direction, angle, results, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public static int OverlapCapsuleNonAlloc (Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = DefaultRaycastLayers;
    return OverlapCapsuleNonAlloc ( point, size, direction, angle, results, layerMask, minDepth, maxDepth );
}

public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, [uei.DefaultValue("DefaultRaycastLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_OverlapCapsuleNonAlloc(point, size, direction, angle, contactFilter, results);
        }

    
    
    public static int OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return Internal_OverlapCapsuleNonAlloc(point, size, direction, angle, contactFilter, results);
        }
    
    
    private static Collider2D Internal_OverlapCapsule (Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_OverlapCapsule ( ref point, ref size, direction, angle, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider2D INTERNAL_CALL_Internal_OverlapCapsule (ref Vector2 point, ref Vector2 size, CapsuleDirection2D direction, float angle, ref ContactFilter2D contactFilter);
    private static Collider2D[] Internal_OverlapCapsuleAll (Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_OverlapCapsuleAll ( ref point, ref size, direction, angle, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Collider2D[] INTERNAL_CALL_Internal_OverlapCapsuleAll (ref Vector2 point, ref Vector2 size, CapsuleDirection2D direction, float angle, ref ContactFilter2D contactFilter);
    private static int Internal_OverlapCapsuleNonAlloc (Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter, Collider2D[] results) {
        return INTERNAL_CALL_Internal_OverlapCapsuleNonAlloc ( ref point, ref size, direction, angle, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_OverlapCapsuleNonAlloc (ref Vector2 point, ref Vector2 size, CapsuleDirection2D direction, float angle, ref ContactFilter2D contactFilter, Collider2D[] results);
    public static int OverlapCollider (Collider2D collider, ContactFilter2D contactFilter, Collider2D[] results) {
        return INTERNAL_CALL_OverlapCollider ( collider, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_OverlapCollider (Collider2D collider, ref ContactFilter2D contactFilter, Collider2D[] results);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Rigidbody2D GetRigidbodyFromInstanceID (int instanceID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Collider2D GetColliderFromInstanceID (int instanceID) ;

    private static int GetColliderContacts (Collider2D collider, ContactFilter2D contactFilter, ContactPoint2D[] results) {
        return INTERNAL_CALL_GetColliderContacts ( collider, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetColliderContacts (Collider2D collider, ref ContactFilter2D contactFilter, ContactPoint2D[] results);
    private static int GetRigidbodyContacts (Rigidbody2D rigidbody, ContactFilter2D contactFilter, ContactPoint2D[] results) {
        return INTERNAL_CALL_GetRigidbodyContacts ( rigidbody, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetRigidbodyContacts (Rigidbody2D rigidbody, ref ContactFilter2D contactFilter, ContactPoint2D[] results);
    private static int GetColliderContactsCollidersOnly (Collider2D collider, ContactFilter2D contactFilter, Collider2D[] results) {
        return INTERNAL_CALL_GetColliderContactsCollidersOnly ( collider, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetColliderContactsCollidersOnly (Collider2D collider, ref ContactFilter2D contactFilter, Collider2D[] results);
    private static int GetRigidbodyContactsCollidersOnly (Rigidbody2D rigidbody, ContactFilter2D contactFilter, Collider2D[] results) {
        return INTERNAL_CALL_GetRigidbodyContactsCollidersOnly ( rigidbody, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetRigidbodyContactsCollidersOnly (Rigidbody2D rigidbody, ref ContactFilter2D contactFilter, Collider2D[] results);
    public static int GetContacts(Collider2D collider, ContactPoint2D[] contacts)
        {
            return GetColliderContacts(collider, new ContactFilter2D().NoFilter(), contacts);
        }
    
    
    public static int GetContacts(Collider2D collider, ContactFilter2D contactFilter, ContactPoint2D[] contacts)
        {
            return GetColliderContacts(collider, contactFilter, contacts);
        }
    
    
    public static int GetContacts(Collider2D collider, Collider2D[] colliders)
        {
            return GetColliderContactsCollidersOnly(collider, new ContactFilter2D().NoFilter(), colliders);
        }
    
    
    public static int GetContacts(Collider2D collider, ContactFilter2D contactFilter, Collider2D[] colliders)
        {
            return GetColliderContactsCollidersOnly(collider, contactFilter, colliders);
        }
    
    
    public static int GetContacts(Rigidbody2D rigidbody, ContactPoint2D[] contacts)
        {
            return GetRigidbodyContacts(rigidbody, new ContactFilter2D().NoFilter(), contacts);
        }
    
    
    public static int GetContacts(Rigidbody2D rigidbody, ContactFilter2D contactFilter, ContactPoint2D[] contacts)
        {
            return GetRigidbodyContacts(rigidbody, contactFilter, contacts);
        }
    
    
    public static int GetContacts(Rigidbody2D rigidbody, Collider2D[] colliders)
        {
            return GetRigidbodyContactsCollidersOnly(rigidbody, new ContactFilter2D().NoFilter(), colliders);
        }
    
    
    public static int GetContacts(Rigidbody2D rigidbody, ContactFilter2D contactFilter, Collider2D[] colliders)
        {
            return GetRigidbodyContactsCollidersOnly(rigidbody, contactFilter, colliders);
        }
    
    
}

public enum RigidbodyInterpolation2D
{
    
    None = 0,
    
    Interpolate = 1,
    
    Extrapolate = 2
}

public enum RigidbodySleepMode2D
{
    
    NeverSleep = 0,
    
    StartAwake = 1,
    
    StartAsleep = 2
}

public enum CollisionDetectionMode2D
{
    
    [Obsolete("Enum member CollisionDetectionMode2D.None has been deprecated. Use CollisionDetectionMode2D.Discrete instead (UnityUpgradable) -> Discrete", true)]
    None = 0,
    
    Discrete = 0,
    
    Continuous = 1
}

public enum ForceMode2D
{
    
    Force = 0,
    
    Impulse = 1,
}

internal enum ColliderErrorState2D
{
    
    None = 0,
    
    NoShapes = 1,
    
    RemovedShapes = 2
}

[Flags]
public enum RigidbodyConstraints2D
{
    
    None = 0,
    
    FreezePositionX = 1 << 0,
    
    FreezePositionY = 1 << 1,
    
    FreezeRotation = 1 << 2,
    
    FreezePosition = FreezePositionX | FreezePositionY,
    
    FreezeAll = FreezePosition | FreezeRotation,
}

public enum RigidbodyType2D
{
    
    Dynamic = 0,
    
    Kinematic = 1,
    
    Static = 2,
}

[Serializable]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct ContactFilter2D
{
    public ContactFilter2D NoFilter()
        {
            useTriggers = false;

            useLayerMask = false;
            layerMask = Physics2D.AllLayers;

            useDepth = false;
            minDepth = -Mathf.Infinity;
            maxDepth = Mathf.Infinity;

            useNormalAngle = false;
            minNormalAngle = -Mathf.Infinity;
            maxNormalAngle = Mathf.Infinity;

            return this;
        }
    
    
    public void SetLayerMask(LayerMask layerMask) { this.layerMask = layerMask; useLayerMask = true; }
    public void ClearLayerMask() { useLayerMask = false; }
    public void SetDepth(float minDepth, float maxDepth) { this.minDepth = minDepth; this.maxDepth = maxDepth; useDepth = true; }
    public void ClearDepth() { useDepth = false; }
    public void SetNormalAngle(float minNormalAngle, float maxNormalAngle) { this.minNormalAngle = minNormalAngle; this.maxNormalAngle = maxNormalAngle; useNormalAngle = true; }
    public void ClearNormalAngle() { useNormalAngle = false; }
    
    
    public bool useTriggers;
    public bool useLayerMask;
    public bool useDepth;
    public bool useNormalAngle;
    public LayerMask layerMask;
    public float minDepth;
    public float maxDepth;
    public float minNormalAngle;
    public float maxNormalAngle;
    
    
    static internal ContactFilter2D CreateLegacyFilter(int layerMask, float minDepth, float maxDepth)
        {
            var contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = Physics2D.queriesHitTriggers;
            contactFilter.SetLayerMask(layerMask);
            contactFilter.SetDepth(minDepth, maxDepth);
            return contactFilter;
        }
    
    
}

[RequireComponent(typeof(Transform))]
public sealed partial class Rigidbody2D : Component
{
    public Vector2 position
    {
        get { Vector2 tmp; INTERNAL_get_position(out tmp); return tmp;  }
        set { INTERNAL_set_position(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_position (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_position (ref Vector2 value) ;

    public extern float rotation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public void MovePosition (Vector2 position) {
        INTERNAL_CALL_MovePosition ( this, ref position );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MovePosition (Rigidbody2D self, ref Vector2 position);
    public void MoveRotation (float angle) {
        INTERNAL_CALL_MoveRotation ( this, angle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MoveRotation (Rigidbody2D self, float angle);
    public Vector2 velocity
    {
        get { Vector2 tmp; INTERNAL_get_velocity(out tmp); return tmp;  }
        set { INTERNAL_set_velocity(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_velocity (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_velocity (ref Vector2 value) ;

    public extern float angularVelocity
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useAutoMass
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

    public extern  PhysicsMaterial2D sharedMaterial
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Vector2 centerOfMass
    {
        get { Vector2 tmp; INTERNAL_get_centerOfMass(out tmp); return tmp;  }
        set { INTERNAL_set_centerOfMass(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_centerOfMass (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_centerOfMass (ref Vector2 value) ;

    public Vector2 worldCenterOfMass
    {
        get { Vector2 tmp; INTERNAL_get_worldCenterOfMass(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_worldCenterOfMass (out Vector2 value) ;


    public extern float inertia
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

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

    public extern float gravityScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  RigidbodyType2D bodyType
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
    extern internal void SetDragBehaviour (bool dragged) ;

    public extern bool useFullKinematicContacts
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public bool isKinematic { get { return bodyType == RigidbodyType2D.Kinematic; } set { bodyType = value ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic; } }
    
    
    [System.Obsolete ("The fixedAngle is no longer supported. Use constraints instead.")]
    public extern bool fixedAngle
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

    public extern RigidbodyConstraints2D constraints
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
    extern public bool IsSleeping () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool IsAwake () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Sleep () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void WakeUp () ;

    public extern  bool simulated
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern RigidbodyInterpolation2D interpolation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern RigidbodySleepMode2D sleepMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern CollisionDetectionMode2D collisionDetectionMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  int attachedColliderCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetAttachedColliders (Collider2D[] results) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool IsTouching (Collider2D collider) ;

    public bool IsTouching(Collider2D collider, ContactFilter2D contactFilter) { return Internal_IsTouching(collider, contactFilter); }
    private bool Internal_IsTouching (Collider2D collider, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_IsTouching ( this, collider, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_IsTouching (Rigidbody2D self, Collider2D collider, ref ContactFilter2D contactFilter);
    public bool IsTouching (ContactFilter2D contactFilter) {
        return INTERNAL_CALL_IsTouching ( this, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IsTouching (Rigidbody2D self, ref ContactFilter2D contactFilter);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool IsTouchingLayers ( [uei.DefaultValue("Physics2D.AllLayers")] int layerMask ) ;

    [uei.ExcludeFromDocs]
    public bool IsTouchingLayers () {
        int layerMask = Physics2D.AllLayers;
        return IsTouchingLayers ( layerMask );
    }

    public bool OverlapPoint (Vector2 point) {
        return INTERNAL_CALL_OverlapPoint ( this, ref point );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_OverlapPoint (Rigidbody2D self, ref Vector2 point);
    public int OverlapCollider (ContactFilter2D contactFilter, Collider2D[] results) {
        return INTERNAL_CALL_OverlapCollider ( this, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_OverlapCollider (Rigidbody2D self, ref ContactFilter2D contactFilter, Collider2D[] results);
    public int Cast (Vector2 direction, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance ) {
        return INTERNAL_CALL_Cast ( this, ref direction, results, distance );
    }

    [uei.ExcludeFromDocs]
    public int Cast (Vector2 direction, RaycastHit2D[] results) {
        float distance = Mathf.Infinity;
        return INTERNAL_CALL_Cast ( this, ref direction, results, distance );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Cast (Rigidbody2D self, ref Vector2 direction, RaycastHit2D[] results, float distance);
    [uei.ExcludeFromDocs]
public int Cast (Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results) {
    float distance = Mathf.Infinity;
    return Cast ( direction, contactFilter, results, distance );
}

public int Cast(Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance )
        {
            return Internal_Cast(direction, distance, contactFilter, results);
        }

    
    
    private int Internal_Cast (Vector2 direction, float distance, ContactFilter2D contactFilter, RaycastHit2D[] results) {
        return INTERNAL_CALL_Internal_Cast ( this, ref direction, distance, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_Cast (Rigidbody2D self, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter, RaycastHit2D[] results);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public ColliderDistance2D Distance (Collider2D collider) ;

    public void AddForce (Vector2 force, [uei.DefaultValue("ForceMode2D.Force")]  ForceMode2D mode ) {
        INTERNAL_CALL_AddForce ( this, ref force, mode );
    }

    [uei.ExcludeFromDocs]
    public void AddForce (Vector2 force) {
        ForceMode2D mode = ForceMode2D.Force;
        INTERNAL_CALL_AddForce ( this, ref force, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddForce (Rigidbody2D self, ref Vector2 force, ForceMode2D mode);
    public void AddRelativeForce (Vector2 relativeForce, [uei.DefaultValue("ForceMode2D.Force")]  ForceMode2D mode ) {
        INTERNAL_CALL_AddRelativeForce ( this, ref relativeForce, mode );
    }

    [uei.ExcludeFromDocs]
    public void AddRelativeForce (Vector2 relativeForce) {
        ForceMode2D mode = ForceMode2D.Force;
        INTERNAL_CALL_AddRelativeForce ( this, ref relativeForce, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddRelativeForce (Rigidbody2D self, ref Vector2 relativeForce, ForceMode2D mode);
    public void AddForceAtPosition (Vector2 force, Vector2 position, [uei.DefaultValue("ForceMode2D.Force")]  ForceMode2D mode ) {
        INTERNAL_CALL_AddForceAtPosition ( this, ref force, ref position, mode );
    }

    [uei.ExcludeFromDocs]
    public void AddForceAtPosition (Vector2 force, Vector2 position) {
        ForceMode2D mode = ForceMode2D.Force;
        INTERNAL_CALL_AddForceAtPosition ( this, ref force, ref position, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddForceAtPosition (Rigidbody2D self, ref Vector2 force, ref Vector2 position, ForceMode2D mode);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void AddTorque (float torque, [uei.DefaultValue("ForceMode2D.Force")]  ForceMode2D mode ) ;

    [uei.ExcludeFromDocs]
    public void AddTorque (float torque) {
        ForceMode2D mode = ForceMode2D.Force;
        AddTorque ( torque, mode );
    }

    public Vector2 GetPoint(Vector2 point) { Vector2 value; Internal_GetPoint(this, point, out value); return value; }
    private static void Internal_GetPoint (Rigidbody2D rigidbody, Vector2 point, out Vector2 value) {
        INTERNAL_CALL_Internal_GetPoint ( rigidbody, ref point, out value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_GetPoint (Rigidbody2D rigidbody, ref Vector2 point, out Vector2 value);
    public Vector2 GetRelativePoint(Vector2 relativePoint) { Vector2 value; Internal_GetRelativePoint(this, relativePoint, out value); return value; }
    private static void Internal_GetRelativePoint (Rigidbody2D rigidbody, Vector2 relativePoint, out Vector2 value) {
        INTERNAL_CALL_Internal_GetRelativePoint ( rigidbody, ref relativePoint, out value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_GetRelativePoint (Rigidbody2D rigidbody, ref Vector2 relativePoint, out Vector2 value);
    public Vector2 GetVector(Vector2 vector) { Vector2 value; Internal_GetVector(this, vector, out value); return value; }
    private static void Internal_GetVector (Rigidbody2D rigidbody, Vector2 vector, out Vector2 value) {
        INTERNAL_CALL_Internal_GetVector ( rigidbody, ref vector, out value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_GetVector (Rigidbody2D rigidbody, ref Vector2 vector, out Vector2 value);
    public Vector2 GetRelativeVector(Vector2 relativeVector) { Vector2 value; Internal_GetRelativeVector(this, relativeVector, out value); return value; }
    private static void Internal_GetRelativeVector (Rigidbody2D rigidbody, Vector2 relativeVector, out Vector2 value) {
        INTERNAL_CALL_Internal_GetRelativeVector ( rigidbody, ref relativeVector, out value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_GetRelativeVector (Rigidbody2D rigidbody, ref Vector2 relativeVector, out Vector2 value);
    public Vector2 GetPointVelocity(Vector2 point) { Vector2 value; Internal_GetPointVelocity(this, point, out value); return value; }
    private static void Internal_GetPointVelocity (Rigidbody2D rigidbody, Vector2 point, out Vector2 value) {
        INTERNAL_CALL_Internal_GetPointVelocity ( rigidbody, ref point, out value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_GetPointVelocity (Rigidbody2D rigidbody, ref Vector2 point, out Vector2 value);
    public Vector2 GetRelativePointVelocity(Vector2 relativePoint) { Vector2 value; Internal_GetRelativePointVelocity(this, relativePoint, out value); return value; }
    private static void Internal_GetRelativePointVelocity (Rigidbody2D rigidbody, Vector2 relativePoint, out Vector2 value) {
        INTERNAL_CALL_Internal_GetRelativePointVelocity ( rigidbody, ref relativePoint, out value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_GetRelativePointVelocity (Rigidbody2D rigidbody, ref Vector2 relativePoint, out Vector2 value);
    public int GetContacts(ContactPoint2D[] contacts)
        {
            return Physics2D.GetContacts(this, new ContactFilter2D().NoFilter(), contacts);
        }
    
    
    public int GetContacts(ContactFilter2D contactFilter, ContactPoint2D[] contacts)
        {
            return Physics2D.GetContacts(this, contactFilter, contacts);
        }
    
    
    public int GetContacts(Collider2D[] colliders)
        {
            return Physics2D.GetContacts(this, new ContactFilter2D().NoFilter(), colliders);
        }
    
    
    public int GetContacts(ContactFilter2D contactFilter, Collider2D[] colliders)
        {
            return Physics2D.GetContacts(this, contactFilter, colliders);
        }
    
    
}

[RequireComponent(typeof(Transform))]
public partial class Collider2D : Behaviour
{
    public extern float density
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
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

    public extern bool usedByEffector
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool usedByComposite
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  CompositeCollider2D composite
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Vector2 offset
    {
        get { Vector2 tmp; INTERNAL_get_offset(out tmp); return tmp;  }
        set { INTERNAL_set_offset(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_offset (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_offset (ref Vector2 value) ;

    public extern  Rigidbody2D attachedRigidbody
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern int shapeCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Bounds bounds
    {
        get { Bounds tmp; INTERNAL_get_bounds(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_bounds (out Bounds value) ;


    internal extern  ColliderErrorState2D errorState
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  bool compositeCapable
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  PhysicsMaterial2D sharedMaterial
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float friction
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float bounciness
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool IsTouching (Collider2D collider) ;

    public bool IsTouching(Collider2D collider, ContactFilter2D contactFilter) { return Internal_IsTouching(collider, contactFilter); }
    private bool Internal_IsTouching (Collider2D collider, ContactFilter2D contactFilter) {
        return INTERNAL_CALL_Internal_IsTouching ( this, collider, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_IsTouching (Collider2D self, Collider2D collider, ref ContactFilter2D contactFilter);
    public bool IsTouching (ContactFilter2D contactFilter) {
        return INTERNAL_CALL_IsTouching ( this, ref contactFilter );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IsTouching (Collider2D self, ref ContactFilter2D contactFilter);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool IsTouchingLayers ( [uei.DefaultValue("Physics2D.AllLayers")] int layerMask ) ;

    [uei.ExcludeFromDocs]
    public bool IsTouchingLayers () {
        int layerMask = Physics2D.AllLayers;
        return IsTouchingLayers ( layerMask );
    }

    public bool OverlapPoint (Vector2 point) {
        return INTERNAL_CALL_OverlapPoint ( this, ref point );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_OverlapPoint (Collider2D self, ref Vector2 point);
    public int OverlapCollider(ContactFilter2D contactFilter, Collider2D[] results)
        {
            return Physics2D.OverlapCollider(this, contactFilter, results);
        }
    
    
    [uei.ExcludeFromDocs]
public int Raycast (Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results) {
    float distance = Mathf.Infinity;
    return Raycast ( direction, contactFilter, results, distance );
}

public int Raycast(Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance )
        {
            return Internal_Raycast(direction, distance, contactFilter, results);
        }

    
    
    [uei.ExcludeFromDocs]
public int Raycast (Vector2 direction, RaycastHit2D[] results, float distance , int layerMask , float minDepth ) {
    float maxDepth = Mathf.Infinity;
    return Raycast ( direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public int Raycast (Vector2 direction, RaycastHit2D[] results, float distance , int layerMask ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    return Raycast ( direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public int Raycast (Vector2 direction, RaycastHit2D[] results, float distance ) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = Physics2D.AllLayers;
    return Raycast ( direction, results, distance, layerMask, minDepth, maxDepth );
}

[uei.ExcludeFromDocs]
public int Raycast (Vector2 direction, RaycastHit2D[] results) {
    float maxDepth = Mathf.Infinity;
    float minDepth = -Mathf.Infinity;
    int layerMask = Physics2D.AllLayers;
    float distance = Mathf.Infinity;
    return Raycast ( direction, results, distance, layerMask, minDepth, maxDepth );
}

public int Raycast(Vector2 direction, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("Physics2D.AllLayers")]  int layerMask , [uei.DefaultValue("-Mathf.Infinity")]  float minDepth , [uei.DefaultValue("Mathf.Infinity")]  float maxDepth )
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);

            return Internal_Raycast(direction, distance, contactFilter, results);
        }

    
    
    private int Internal_Raycast (Vector2 direction, float distance, ContactFilter2D contactFilter, RaycastHit2D[] results) {
        return INTERNAL_CALL_Internal_Raycast ( this, ref direction, distance, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_Raycast (Collider2D self, ref Vector2 direction, float distance, ref ContactFilter2D contactFilter, RaycastHit2D[] results);
    [uei.ExcludeFromDocs]
public int Cast (Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, float distance ) {
    bool ignoreSiblingColliders = true;
    return Cast ( direction, contactFilter, results, distance, ignoreSiblingColliders );
}

[uei.ExcludeFromDocs]
public int Cast (Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results) {
    bool ignoreSiblingColliders = true;
    float distance = Mathf.Infinity;
    return Cast ( direction, contactFilter, results, distance, ignoreSiblingColliders );
}

public int Cast(Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("true")]  bool ignoreSiblingColliders )
        {
            return Internal_Cast(direction, contactFilter, distance, ignoreSiblingColliders, results);
        }

    
    
    [uei.ExcludeFromDocs]
public int Cast (Vector2 direction, RaycastHit2D[] results, float distance ) {
    bool ignoreSiblingColliders = true;
    return Cast ( direction, results, distance, ignoreSiblingColliders );
}

[uei.ExcludeFromDocs]
public int Cast (Vector2 direction, RaycastHit2D[] results) {
    bool ignoreSiblingColliders = true;
    float distance = Mathf.Infinity;
    return Cast ( direction, results, distance, ignoreSiblingColliders );
}

public int Cast(Vector2 direction, RaycastHit2D[] results, [uei.DefaultValue("Mathf.Infinity")]  float distance , [uei.DefaultValue("true")]  bool ignoreSiblingColliders )
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = Physics2D.queriesHitTriggers;
            contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(this.gameObject.layer));

            return Internal_Cast(direction, contactFilter, distance, ignoreSiblingColliders, results);
        }

    
    
    private int Internal_Cast (Vector2 direction, ContactFilter2D contactFilter, float distance, bool ignoreSiblingColliders, RaycastHit2D[] results) {
        return INTERNAL_CALL_Internal_Cast ( this, ref direction, ref contactFilter, distance, ignoreSiblingColliders, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_Internal_Cast (Collider2D self, ref Vector2 direction, ref ContactFilter2D contactFilter, float distance, bool ignoreSiblingColliders, RaycastHit2D[] results);
    public int GetContacts(ContactPoint2D[] contacts)
        {
            return Physics2D.GetContacts(this, new ContactFilter2D().NoFilter(), contacts);
        }
    
    
    public int GetContacts(ContactFilter2D contactFilter, ContactPoint2D[] contacts)
        {
            return Physics2D.GetContacts(this, contactFilter, contacts);
        }
    
    
    public int GetContacts(Collider2D[] colliders)
        {
            return Physics2D.GetContacts(this, new ContactFilter2D().NoFilter(), colliders);
        }
    
    
    public int GetContacts(ContactFilter2D contactFilter, Collider2D[] colliders)
        {
            return Physics2D.GetContacts(this, contactFilter, colliders);
        }
    
    
    public ColliderDistance2D Distance(Collider2D collider)
        {
            return Physics2D.Distance(this, collider);
        }
    
    
}

public sealed partial class CircleCollider2D : Collider2D
{
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

public sealed partial class BoxCollider2D : Collider2D
{
    public Vector2 size
    {
        get { Vector2 tmp; INTERNAL_get_size(out tmp); return tmp;  }
        set { INTERNAL_set_size(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_size (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_size (ref Vector2 value) ;

    public extern float edgeRadius
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool autoTiling
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class EdgeCollider2D : Collider2D
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Reset () ;

    public extern float edgeRadius
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  int edgeCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int pointCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  Vector2[] points
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public enum CapsuleDirection2D
{
    
    Vertical = 0,
    
    Horizontal = 1
}

public sealed partial class CapsuleCollider2D : Collider2D
{
    public Vector2 size
    {
        get { Vector2 tmp; INTERNAL_get_size(out tmp); return tmp;  }
        set { INTERNAL_set_size(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_size (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_size (ref Vector2 value) ;

    public extern CapsuleDirection2D direction
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

[RequireComponent(typeof(Rigidbody2D))]
public sealed partial class CompositeCollider2D : Collider2D
{
    public enum GeometryType { Outlines = 0, Polygons = 1 }
    public enum GenerationType { Synchronous = 0, Manual = 1 }
    
    
    public extern CompositeCollider2D.GeometryType geometryType
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern CompositeCollider2D.GenerationType generationType
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float vertexDistance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float edgeRadius
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public void GenerateGeometry () {
        INTERNAL_CALL_GenerateGeometry ( this );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GenerateGeometry (CompositeCollider2D self);
    public int GetPathPointCount(int index)
        {
            if (index < 0 || index >= pathCount)
                throw new ArgumentOutOfRangeException("index", String.Format("Path index {0} must be in the range of 0 to {1}.", index, pathCount - 1));

            return Internal_GetPathPointCount(index);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private int Internal_GetPathPointCount (int index) ;

    public int GetPath(int index, Vector2[] points)
        {
            if (index < 0 || index >= pathCount)
                throw new ArgumentOutOfRangeException("index", String.Format("Path index {0} must be in the range of 0 to {1}.", index, pathCount - 1));

            if (points == null)
                throw new ArgumentNullException("points");

            return Internal_GetPath(index, points);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private int Internal_GetPath (int index, Vector2[] points) ;

    public extern  int pathCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int pointCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

public sealed partial class PolygonCollider2D : Collider2D
{
    public extern  Vector2[] points
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
    extern public Vector2[] GetPath (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetPath (int index, Vector2[] points) ;

    public extern  int pathCount
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
    extern public int GetTotalPointCount () ;

    public void CreatePrimitive (int sides, [uei.DefaultValue("Vector2.one")]  Vector2 scale , [uei.DefaultValue("Vector2.zero")]  Vector2 offset ) {
        INTERNAL_CALL_CreatePrimitive ( this, sides, ref scale, ref offset );
    }

    [uei.ExcludeFromDocs]
    public void CreatePrimitive (int sides, Vector2 scale ) {
        Vector2 offset = Vector2.zero;
        INTERNAL_CALL_CreatePrimitive ( this, sides, ref scale, ref offset );
    }

    [uei.ExcludeFromDocs]
    public void CreatePrimitive (int sides) {
        Vector2 offset = Vector2.zero;
        Vector2 scale = Vector2.one;
        INTERNAL_CALL_CreatePrimitive ( this, sides, ref scale, ref offset );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CreatePrimitive (PolygonCollider2D self, int sides, ref Vector2 scale, ref Vector2 offset);
    public extern bool autoTiling
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct ColliderDistance2D
{
    private Vector2 m_PointA;
    private Vector2 m_PointB;
    private Vector2 m_Normal;
    private float m_Distance;
    private int m_IsValid;
    
    
    public Vector2 pointA { get { return m_PointA; } set { m_PointA = value; } }
    public Vector2 pointB { get { return m_PointB; } set { m_PointB = value; } }
    
    
    public Vector2 normal { get { return m_Normal; } }
    
    
    public float distance { get { return m_Distance; } set { m_Distance = value; } }
    
    
    public bool isOverlapped { get { return m_Distance < 0.0f; } }
    
    
    public bool isValid { get { return m_IsValid != 0; } set { m_IsValid = value ? 1 : 0; } }
    
    
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct ContactPoint2D
{
    internal Vector2 m_Point;
    internal Vector2 m_Normal;
    internal Vector2 m_RelativeVelocity;
    internal float m_Separation;
    internal float m_NormalImpulse;
    internal float m_TangentImpulse;
    internal int m_Collider;
    internal int m_OtherCollider;
    internal int m_Rigidbody;
    internal int m_OtherRigidbody;
    internal int m_Enabled;
    
    
    public Vector2 point  { get { return m_Point; } }
    
    
    public Vector2 normal { get { return m_Normal; } }
    
    
    public float separation { get { return m_Separation; } }
    
    
    public float normalImpulse { get { return m_NormalImpulse; } }
    
    
    public float tangentImpulse { get { return m_TangentImpulse; } }
    
    
    public Vector2 relativeVelocity { get { return m_RelativeVelocity; } }
    
    
    public Collider2D collider { get { return Physics2D.GetColliderFromInstanceID(m_Collider); } }
    
    
    public Collider2D otherCollider { get { return Physics2D.GetColliderFromInstanceID(m_OtherCollider); } }
    
    
    public Rigidbody2D rigidbody { get { return Physics2D.GetRigidbodyFromInstanceID(m_Rigidbody); } }
    
    
    public Rigidbody2D otherRigidbody { get { return Physics2D.GetRigidbodyFromInstanceID(m_OtherRigidbody); } }
    
    
    public bool enabled { get { return m_Enabled == 1; } }
    
    
}

[StructLayout(LayoutKind.Sequential)]
[RequiredByNativeCode]
public partial class Collision2D
{
    internal int m_Collider;
    internal int m_OtherCollider;
    internal int m_Rigidbody;
    internal int m_OtherRigidbody;
    internal ContactPoint2D[] m_Contacts;
    internal Vector2 m_RelativeVelocity;
    internal int m_Enabled;
    
    
    public Collider2D collider { get { return Physics2D.GetColliderFromInstanceID(m_Collider); } }
    
    
    public Collider2D otherCollider { get { return Physics2D.GetColliderFromInstanceID(m_OtherCollider); } }
    
    
    public Rigidbody2D rigidbody { get { return Physics2D.GetRigidbodyFromInstanceID(m_Rigidbody); } }
    
    
    public Rigidbody2D otherRigidbody { get { return Physics2D.GetRigidbodyFromInstanceID(m_OtherRigidbody); } }
    
    
    public Transform transform { get { return rigidbody != null ? rigidbody.transform : collider.transform; } }
    
    
    public GameObject gameObject { get { return rigidbody != null ? rigidbody.gameObject : collider.gameObject; } }
    
    
    public ContactPoint2D[] contacts { get { return m_Contacts; } }
    
    
    public Vector2 relativeVelocity { get { return m_RelativeVelocity; } }
    
    
    public bool enabled { get { return m_Enabled == 1; } }
    
    
}

public enum JointLimitState2D
{
    
    Inactive = 0,
    
    LowerLimit = 1,
    
    UpperLimit = 2,
    
    EqualLimits = 3,
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct JointAngleLimits2D
{
    private float m_LowerAngle;
    private float m_UpperAngle;
    
    
    public float min { get { return m_LowerAngle; } set { m_LowerAngle = value; } }
    
    
    public float max { get { return m_UpperAngle; } set { m_UpperAngle = value; } }
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct JointTranslationLimits2D
{
    private float m_LowerTranslation;
    private float m_UpperTranslation;
    
    
    public float min { get { return m_LowerTranslation; } set { m_LowerTranslation = value; } }
    
    
    public float max { get { return m_UpperTranslation; } set { m_UpperTranslation = value; } }
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct JointMotor2D
{
    private float m_MotorSpeed;
    private float m_MaximumMotorTorque;
    
    
    public float motorSpeed { get { return m_MotorSpeed; } set { m_MotorSpeed = value; } }
    
    
    public float maxMotorTorque { get { return m_MaximumMotorTorque; } set { m_MaximumMotorTorque = value; } }
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct JointSuspension2D
{
    private float m_DampingRatio;
    private float m_Frequency;
    private float m_Angle;
    
    
    public float dampingRatio { get { return m_DampingRatio; } set { m_DampingRatio = value; } }
    
    
    public float frequency { get { return m_Frequency; } set { m_Frequency = value; } }
    
    
    public float angle { get { return m_Angle; } set { m_Angle = value; } }
    
    
}

[RequireComponent(typeof(Transform), typeof(Rigidbody2D))]
public partial class Joint2D : Behaviour
{
    public extern Rigidbody2D connectedBody
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

    public Vector2 reactionForce { get { return GetReactionForce(Time.fixedDeltaTime); } }
    
    
    public float reactionTorque { get { return GetReactionTorque(Time.fixedDeltaTime); } }
    
    
    public Vector2 GetReactionForce(float timeStep) { Vector2 value; Internal_GetReactionForce(this, timeStep, out value); return value; }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_GetReactionForce (Joint2D joint, float timeStep, out Vector2 value) ;

    public float GetReactionTorque (float timeStep) {
        return INTERNAL_CALL_GetReactionTorque ( this, timeStep );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static float INTERNAL_CALL_GetReactionTorque (Joint2D self, float timeStep);
}

public partial class AnchoredJoint2D : Joint2D
{
    public Vector2 anchor
    {
        get { Vector2 tmp; INTERNAL_get_anchor(out tmp); return tmp;  }
        set { INTERNAL_set_anchor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_anchor (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_anchor (ref Vector2 value) ;

    public Vector2 connectedAnchor
    {
        get { Vector2 tmp; INTERNAL_get_connectedAnchor(out tmp); return tmp;  }
        set { INTERNAL_set_connectedAnchor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_connectedAnchor (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_connectedAnchor (ref Vector2 value) ;

    public extern bool autoConfigureConnectedAnchor
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class SpringJoint2D : AnchoredJoint2D
{
    public extern bool autoConfigureDistance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float distance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float dampingRatio
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float frequency
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class DistanceJoint2D : AnchoredJoint2D
{
    public extern bool autoConfigureDistance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float distance
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool maxDistanceOnly
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class FrictionJoint2D : AnchoredJoint2D
{
    public extern float maxForce
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float maxTorque
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class HingeJoint2D : AnchoredJoint2D
{
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

    public JointMotor2D motor
    {
        get { JointMotor2D tmp; INTERNAL_get_motor(out tmp); return tmp;  }
        set { INTERNAL_set_motor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_motor (out JointMotor2D value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_motor (ref JointMotor2D value) ;

    public JointAngleLimits2D limits
    {
        get { JointAngleLimits2D tmp; INTERNAL_get_limits(out tmp); return tmp;  }
        set { INTERNAL_set_limits(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_limits (out JointAngleLimits2D value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_limits (ref JointAngleLimits2D value) ;

    public extern JointLimitState2D limitState
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float referenceAngle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float jointAngle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float jointSpeed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public float GetMotorTorque (float timeStep) {
        return INTERNAL_CALL_GetMotorTorque ( this, timeStep );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static float INTERNAL_CALL_GetMotorTorque (HingeJoint2D self, float timeStep);
}

public sealed partial class RelativeJoint2D : Joint2D
{
    public extern float maxForce
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float maxTorque
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float correctionScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool autoConfigureOffset
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Vector2 linearOffset
    {
        get { Vector2 tmp; INTERNAL_get_linearOffset(out tmp); return tmp;  }
        set { INTERNAL_set_linearOffset(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_linearOffset (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_linearOffset (ref Vector2 value) ;

    public extern float angularOffset
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Vector2 target
    {
        get { Vector2 tmp; INTERNAL_get_target(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_target (out Vector2 value) ;


}

public sealed partial class SliderJoint2D : AnchoredJoint2D
{
    public extern bool autoConfigureAngle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float angle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

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

    public JointMotor2D motor
    {
        get { JointMotor2D tmp; INTERNAL_get_motor(out tmp); return tmp;  }
        set { INTERNAL_set_motor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_motor (out JointMotor2D value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_motor (ref JointMotor2D value) ;

    public JointTranslationLimits2D limits
    {
        get { JointTranslationLimits2D tmp; INTERNAL_get_limits(out tmp); return tmp;  }
        set { INTERNAL_set_limits(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_limits (out JointTranslationLimits2D value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_limits (ref JointTranslationLimits2D value) ;

    public extern JointLimitState2D limitState
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float referenceAngle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float jointTranslation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float jointSpeed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public float GetMotorForce (float timeStep) {
        return INTERNAL_CALL_GetMotorForce ( this, timeStep );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static float INTERNAL_CALL_GetMotorForce (SliderJoint2D self, float timeStep);
}

public sealed partial class TargetJoint2D : Joint2D
{
    public Vector2 anchor
    {
        get { Vector2 tmp; INTERNAL_get_anchor(out tmp); return tmp;  }
        set { INTERNAL_set_anchor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_anchor (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_anchor (ref Vector2 value) ;

    public Vector2 target
    {
        get { Vector2 tmp; INTERNAL_get_target(out tmp); return tmp;  }
        set { INTERNAL_set_target(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_target (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_target (ref Vector2 value) ;

    public extern bool autoConfigureTarget
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float maxForce
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float dampingRatio
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float frequency
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class FixedJoint2D : AnchoredJoint2D
{
    public extern float dampingRatio
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float frequency
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float referenceAngle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

public sealed partial class WheelJoint2D : AnchoredJoint2D
{
    public JointSuspension2D suspension
    {
        get { JointSuspension2D tmp; INTERNAL_get_suspension(out tmp); return tmp;  }
        set { INTERNAL_set_suspension(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_suspension (out JointSuspension2D value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_suspension (ref JointSuspension2D value) ;

    public extern bool useMotor
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public JointMotor2D motor
    {
        get { JointMotor2D tmp; INTERNAL_get_motor(out tmp); return tmp;  }
        set { INTERNAL_set_motor(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_motor (out JointMotor2D value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_motor (ref JointMotor2D value) ;

    public extern float jointTranslation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float jointLinearSpeed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float jointSpeed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern float jointAngle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public float GetMotorTorque (float timeStep) {
        return INTERNAL_CALL_GetMotorTorque ( this, timeStep );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static float INTERNAL_CALL_GetMotorTorque (WheelJoint2D self, float timeStep);
}

public sealed partial class PhysicsMaterial2D : Object
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_Create ([Writable] PhysicsMaterial2D mat, string name) ;

    public PhysicsMaterial2D() { Internal_Create(this, null); }
    
    
    public PhysicsMaterial2D(string name) { Internal_Create(this, name); }
    
    
    public extern float bounciness
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float friction
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public partial class PhysicsUpdateBehaviour2D : Behaviour
{
}

[RequireComponent(typeof(Rigidbody2D))]
public sealed partial class ConstantForce2D : PhysicsUpdateBehaviour2D
{
    public  Vector2 force
    {
        get { Vector2 tmp; INTERNAL_get_force(out tmp); return tmp;  }
        set { INTERNAL_set_force(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_force (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_force (ref Vector2 value) ;

    public  Vector2 relativeForce
    {
        get { Vector2 tmp; INTERNAL_get_relativeForce(out tmp); return tmp;  }
        set { INTERNAL_set_relativeForce(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_relativeForce (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_relativeForce (ref Vector2 value) ;

    public extern  float torque
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public enum EffectorSelection2D
{
    
    Rigidbody = 0,
    
    Collider = 1,
}

public enum EffectorForceMode2D
{
    
    Constant = 0,
    
    InverseLinear = 1,
    
    InverseSquared = 2,
}

public partial class Effector2D : Behaviour
{
    public extern bool useColliderMask
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern int colliderMask
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    internal extern  bool requiresCollider
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  bool designedForTrigger
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  bool designedForNonTrigger
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

public sealed partial class AreaEffector2D : Effector2D
{
    public extern float forceAngle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useGlobalAngle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float forceMagnitude
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float forceVariation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

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

    public extern EffectorSelection2D forceTarget
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class BuoyancyEffector2D : Effector2D
{
    public extern float surfaceLevel
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float density
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float linearDrag
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

    public extern float flowAngle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float flowMagnitude
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float flowVariation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class PointEffector2D : Effector2D
{
    public extern float forceMagnitude
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float forceVariation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float distanceScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

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

    public extern EffectorSelection2D forceSource
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern EffectorSelection2D forceTarget
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern EffectorForceMode2D forceMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class PlatformEffector2D : Effector2D
{
    public extern bool useOneWay
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useOneWayGrouping
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useSideFriction
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useSideBounce
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float surfaceArc
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float sideArc
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float rotationalOffset
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

public sealed partial class SurfaceEffector2D : Effector2D
{
    public extern float speed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float speedVariation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float forceScale
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useContactForce
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useFriction
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool useBounce
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
