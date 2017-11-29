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
using UnityEngineInternal;
using UnityEngine.SceneManagement;

namespace UnityEngine
{


internal enum RotationOrder { OrderXYZ, OrderXZY, OrderYZX, OrderYXZ, OrderZXY, OrderZYX }


public partial class Transform : Component, IEnumerable
{
    protected Transform() {}
    
    
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

    public Vector3 localPosition
    {
        get { Vector3 tmp; INTERNAL_get_localPosition(out tmp); return tmp;  }
        set { INTERNAL_set_localPosition(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_localPosition (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_localPosition (ref Vector3 value) ;

    internal Vector3 GetLocalEulerAngles (RotationOrder order) {
        Vector3 result;
        INTERNAL_CALL_GetLocalEulerAngles ( this, order, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetLocalEulerAngles (Transform self, RotationOrder order, out Vector3 value);
    internal void SetLocalEulerAngles (Vector3 euler, RotationOrder order) {
        INTERNAL_CALL_SetLocalEulerAngles ( this, ref euler, order );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetLocalEulerAngles (Transform self, ref Vector3 euler, RotationOrder order);
    internal void SetLocalEulerHint (Vector3 euler) {
        INTERNAL_CALL_SetLocalEulerHint ( this, ref euler );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetLocalEulerHint (Transform self, ref Vector3 euler);
    public Vector3 eulerAngles { get { return rotation.eulerAngles; } set { rotation = Quaternion.Euler(value); }  }
    
    
    public Vector3 localEulerAngles { get { return localRotation.eulerAngles; } set { localRotation = Quaternion.Euler(value); }  }
    
    
    public Vector3 right  { get { return rotation * Vector3.right; } set { rotation = Quaternion.FromToRotation(Vector3.right, value); } }
    
    
    public Vector3 up       { get { return rotation * Vector3.up; }  set { rotation = Quaternion.FromToRotation(Vector3.up, value); } }
    
    
    public Vector3 forward { get { return rotation * Vector3.forward; } set { rotation = Quaternion.LookRotation(value); } }
    
    
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

    public Quaternion localRotation
    {
        get { Quaternion tmp; INTERNAL_get_localRotation(out tmp); return tmp;  }
        set { INTERNAL_set_localRotation(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_localRotation (out Quaternion value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_localRotation (ref Quaternion value) ;

    internal extern  RotationOrder rotationOrder
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Vector3 localScale
    {
        get { Vector3 tmp; INTERNAL_get_localScale(out tmp); return tmp;  }
        set { INTERNAL_set_localScale(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_localScale (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_localScale (ref Vector3 value) ;

    public Transform parent
        {
            get { return parentInternal; }
            set
            {
                if (this is RectTransform)
                    Debug.LogWarning("Parent of RectTransform is being set with parent property. Consider using the SetParent method instead, with the worldPositionStays argument set to false. This will retain local orientation and scale rather than world orientation and scale, which can prevent common UI scaling issues.", this);
                parentInternal = value;
            }
        }
    
    
    internal extern  Transform parentInternal
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public void SetParent(Transform parent)
        {
            SetParent(parent, true);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetParent (Transform parent, bool worldPositionStays) ;

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


    public void SetPositionAndRotation (Vector3 position, Quaternion rotation) {
        INTERNAL_CALL_SetPositionAndRotation ( this, ref position, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetPositionAndRotation (Transform self, ref Vector3 position, ref Quaternion rotation);
    [uei.ExcludeFromDocs]
public void Translate (Vector3 translation) {
    Space relativeTo = Space.Self;
    Translate ( translation, relativeTo );
}

public void Translate(Vector3 translation, [uei.DefaultValue("Space.Self")]  Space relativeTo )
        {
            if (relativeTo == Space.World)
                position += translation;
            else
                position += TransformDirection(translation);
        }

    
    
    [uei.ExcludeFromDocs]
public void Translate (float x, float y, float z) {
    Space relativeTo = Space.Self;
    Translate ( x, y, z, relativeTo );
}

public void Translate(float x, float y, float z, [uei.DefaultValue("Space.Self")]  Space relativeTo )
        {
            Translate(new Vector3(x, y, z), relativeTo);
        }

    
    
    public void Translate(Vector3 translation, Transform relativeTo)
        {
            if (relativeTo)
                position += relativeTo.TransformDirection(translation);
            else
                position += translation;
        }
    
    
    public void Translate(float x, float y, float z, Transform relativeTo)
        {
            Translate(new Vector3(x, y, z), relativeTo);
        }
    
    
    [uei.ExcludeFromDocs]
public void Rotate (Vector3 eulerAngles) {
    Space relativeTo = Space.Self;
    Rotate ( eulerAngles, relativeTo );
}

public void Rotate(Vector3 eulerAngles, [uei.DefaultValue("Space.Self")]  Space relativeTo )
        {
            Quaternion eulerRot = Quaternion.Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
            if (relativeTo == Space.Self)
                localRotation = localRotation * eulerRot;
            else
            {
                rotation = rotation * (Quaternion.Inverse(rotation) * eulerRot * rotation);
            }
        }

    
    
    [uei.ExcludeFromDocs]
public void Rotate (float xAngle, float yAngle, float zAngle) {
    Space relativeTo = Space.Self;
    Rotate ( xAngle, yAngle, zAngle, relativeTo );
}

public void Rotate(float xAngle, float yAngle, float zAngle, [uei.DefaultValue("Space.Self")]  Space relativeTo )
        {
            Rotate(new Vector3(xAngle, yAngle, zAngle), relativeTo);
        }

    
    
    internal void RotateAroundInternal (Vector3 axis, float angle) {
        INTERNAL_CALL_RotateAroundInternal ( this, ref axis, angle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_RotateAroundInternal (Transform self, ref Vector3 axis, float angle);
    [uei.ExcludeFromDocs]
public void Rotate (Vector3 axis, float angle) {
    Space relativeTo = Space.Self;
    Rotate ( axis, angle, relativeTo );
}

public void Rotate(Vector3 axis, float angle, [uei.DefaultValue("Space.Self")]  Space relativeTo )
        {
            if (relativeTo == Space.Self)
                RotateAroundInternal(transform.TransformDirection(axis), angle * Mathf.Deg2Rad);
            else
                RotateAroundInternal(axis, angle * Mathf.Deg2Rad);
        }

    
    
    public void RotateAround(Vector3 point, Vector3 axis, float angle)
        {
            Vector3 worldPos = position;
            Quaternion q = Quaternion.AngleAxis(angle , axis);
            Vector3 dif = worldPos - point;
            dif = q * dif;
            worldPos = point + dif;
            position = worldPos;
            RotateAroundInternal(axis, angle * Mathf.Deg2Rad);
        }
    
    
    [uei.ExcludeFromDocs]
public void LookAt (Transform target) {
    Vector3 worldUp = Vector3.up;
    LookAt ( target, worldUp );
}

public void LookAt(Transform target, [uei.DefaultValue("Vector3.up")]  Vector3 worldUp ) { if (target) LookAt(target.position, worldUp); }

    
    
    public void LookAt (Vector3 worldPosition, [uei.DefaultValue("Vector3.up")]  Vector3 worldUp ) {
        INTERNAL_CALL_LookAt ( this, ref worldPosition, ref worldUp );
    }

    [uei.ExcludeFromDocs]
    public void LookAt (Vector3 worldPosition) {
        Vector3 worldUp = Vector3.up;
        INTERNAL_CALL_LookAt ( this, ref worldPosition, ref worldUp );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_LookAt (Transform self, ref Vector3 worldPosition, ref Vector3 worldUp);
    public Vector3 TransformDirection (Vector3 direction) {
        Vector3 result;
        INTERNAL_CALL_TransformDirection ( this, ref direction, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_TransformDirection (Transform self, ref Vector3 direction, out Vector3 value);
    public Vector3 TransformDirection(float x, float y, float z) { return TransformDirection(new Vector3(x, y, z)); }
    
    
    public Vector3 InverseTransformDirection (Vector3 direction) {
        Vector3 result;
        INTERNAL_CALL_InverseTransformDirection ( this, ref direction, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InverseTransformDirection (Transform self, ref Vector3 direction, out Vector3 value);
    public Vector3 InverseTransformDirection(float x, float y, float z) { return InverseTransformDirection(new Vector3(x, y, z)); }
    
    
    
    public Vector3 TransformVector (Vector3 vector) {
        Vector3 result;
        INTERNAL_CALL_TransformVector ( this, ref vector, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_TransformVector (Transform self, ref Vector3 vector, out Vector3 value);
    public Vector3 TransformVector(float x, float y, float z) { return TransformVector(new Vector3(x, y, z)); }
    
    
    public Vector3 InverseTransformVector (Vector3 vector) {
        Vector3 result;
        INTERNAL_CALL_InverseTransformVector ( this, ref vector, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InverseTransformVector (Transform self, ref Vector3 vector, out Vector3 value);
    public Vector3 InverseTransformVector(float x, float y, float z) { return InverseTransformVector(new Vector3(x, y, z)); }
    
    
    
    public Vector3 TransformPoint (Vector3 position) {
        Vector3 result;
        INTERNAL_CALL_TransformPoint ( this, ref position, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_TransformPoint (Transform self, ref Vector3 position, out Vector3 value);
    public Vector3 TransformPoint(float x, float y, float z) { return TransformPoint(new Vector3(x, y, z)); }
    
    
    public Vector3 InverseTransformPoint (Vector3 position) {
        Vector3 result;
        INTERNAL_CALL_InverseTransformPoint ( this, ref position, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InverseTransformPoint (Transform self, ref Vector3 position, out Vector3 value);
    public Vector3 InverseTransformPoint(float x, float y, float z) { return InverseTransformPoint(new Vector3(x, y, z)); }
    
    
    
    public extern  Transform root
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int childCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void DetachChildren () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetAsFirstSibling () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetAsLastSibling () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetSiblingIndex (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetSiblingIndex () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Transform Find (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SendTransformChangedScale () ;

    public Vector3 lossyScale
    {
        get { Vector3 tmp; INTERNAL_get_lossyScale(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_lossyScale (out Vector3 value) ;


    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool IsChildOf (Transform parent) ;

    public extern  bool hasChanged
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [Obsolete("FindChild has been deprecated. Use Find instead (UnityUpgradable) -> Find([mscorlib] System.String)", false)]
    public Transform FindChild(string name) { return Find(name); }
    
    
    public IEnumerator GetEnumerator()
        {
            return new Transform.Enumerator(this);
        }
    
    
    private sealed partial class Enumerator : IEnumerator    
    {
        
                    Transform outer;
                    int currentIndex = -1;
        
                    internal Enumerator(Transform outer) { this.outer = outer; }
                    public object Current
            {
                get { return outer.GetChild(currentIndex); }
            }
        
        public bool MoveNext()
            {
                int childCount = outer.childCount;
                return ++currentIndex < childCount;
            }
        
        public void Reset() { currentIndex = -1; }
    }

    [System.Obsolete ("use Transform.Rotate instead.")]
    public void RotateAround (Vector3 axis, float angle) {
        INTERNAL_CALL_RotateAround ( this, ref axis, angle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_RotateAround (Transform self, ref Vector3 axis, float angle);
    [System.Obsolete ("use Transform.Rotate instead.")]
    public void RotateAroundLocal (Vector3 axis, float angle) {
        INTERNAL_CALL_RotateAroundLocal ( this, ref axis, angle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_RotateAroundLocal (Transform self, ref Vector3 axis, float angle);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public Transform GetChild (int index) ;

    [System.Obsolete ("use Transform.childCount instead.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int GetChildCount () ;

    public extern  int hierarchyCapacity
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  int hierarchyCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal bool IsNonUniformScaleTransform () ;

}

}
