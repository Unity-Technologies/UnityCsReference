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
    private static int GetColliderColliderContacts (Collider2D collider1, Collider2D collider2, ContactFilter2D contactFilter, ContactPoint2D[] results) {
        return INTERNAL_CALL_GetColliderColliderContacts ( collider1, collider2, ref contactFilter, results );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetColliderColliderContacts (Collider2D collider1, Collider2D collider2, ref ContactFilter2D contactFilter, ContactPoint2D[] results);
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
    public static int GetContacts(Collider2D collider1, Collider2D collider2, ContactFilter2D contactFilter, ContactPoint2D[] contacts)
        {
            return GetColliderColliderContacts(collider1, collider2, contactFilter, contacts);
        }
    
    
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

[RequireComponent(typeof(Transform))]
public sealed partial class Rigidbody2D : Component
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetAttachedColliders (Collider2D[] results) ;

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
    
    
}

public sealed partial class EdgeCollider2D : Collider2D
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

}

[RequireComponent(typeof(Rigidbody2D))]
public sealed partial class CompositeCollider2D : Collider2D
{
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
    
    
    public ContactPoint2D[] contacts
        {
            get
            {
                if (m_Contacts == null)
                    m_Contacts = CreateCollisionContacts(collider, otherCollider, rigidbody, otherRigidbody, enabled);

                return m_Contacts;
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  ContactPoint2D[] CreateCollisionContacts (Collider2D collider, Collider2D otherCollider, Rigidbody2D rigidbody, Rigidbody2D otherRigidbody, bool enabled) ;

    public int GetContacts(ContactPoint2D[] contacts)
        {
            return Physics2D.GetContacts(collider, otherCollider, new ContactFilter2D().NoFilter(), contacts);
        }
    
    
    public Vector2 relativeVelocity { get { return m_RelativeVelocity; } }
    
    
    public bool enabled { get { return m_Enabled == 1; } }
    
    
}

}
