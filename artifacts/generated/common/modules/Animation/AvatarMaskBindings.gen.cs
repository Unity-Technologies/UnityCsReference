// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

using Object = UnityEngine.Object;

namespace UnityEngine
{
[MovedFrom("UnityEditor.Animations", true)]
public enum AvatarMaskBodyPart
{
    Root = 0,
    Body = 1,
    Head = 2,
    LeftLeg = 3,
    RightLeg = 4,
    LeftArm = 5,
    RightArm = 6,
    LeftFingers = 7,
    RightFingers = 8,
    LeftFootIK = 9,
    RightFootIK = 10,
    LeftHandIK = 11,
    RightHandIK = 12,
    LastBodyPart = 13
}

[MovedFrom("UnityEditor.Animations", true)]
[NativeHeader("Runtime/Animation/AvatarMask.h")]
public sealed partial class AvatarMask : Object
{
    public AvatarMask()
        {
            Internal_CreateAvatarMask(this);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_CreateAvatarMask ([Writable] AvatarMask mono) ;

    [System.Obsolete ("AvatarMask.humanoidBodyPartCount is deprecated. Use AvatarMaskBodyPart.LastBodyPart instead.")]
    int         humanoidBodyPartCount
        {
            get { return (int)AvatarMaskBodyPart.LastBodyPart; }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool GetHumanoidBodyPartActive (AvatarMaskBodyPart index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetHumanoidBodyPartActive (AvatarMaskBodyPart index, bool value) ;

    public extern  int transformCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [uei.ExcludeFromDocs]
public void AddTransformPath (Transform transform) {
    bool recursive = true;
    AddTransformPath ( transform, recursive );
}

public void AddTransformPath(Transform transform, [uei.DefaultValue("true")]  bool recursive )
        {
            if (transform == null)
                throw new System.ArgumentNullException("transform");

            Internal_AddTransformPath(transform, recursive);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_AddTransformPath (Transform transform, bool recursive) ;

    [uei.ExcludeFromDocs]
public void RemoveTransformPath (Transform transform) {
    bool recursive = true;
    RemoveTransformPath ( transform, recursive );
}

public void RemoveTransformPath(Transform transform, [uei.DefaultValue("true")]  bool recursive )
        {
            if (transform == null)
                throw new System.ArgumentNullException("transform");

            Internal_RemoveTransformPath(transform, recursive);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_RemoveTransformPath (Transform transform, bool recursive) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public string GetTransformPath (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetTransformPath (int index, string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool GetTransformActive (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void SetTransformActive (int index, bool value) ;

    internal extern  bool hasFeetIK
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal void Copy(AvatarMask other)
        {
            for (AvatarMaskBodyPart i = 0; i < AvatarMaskBodyPart.LastBodyPart; i++)
                SetHumanoidBodyPartActive(i, other.GetHumanoidBodyPartActive(i));

            transformCount = other.transformCount;

            for (int i = 0; i < other.transformCount; i++)
            {
                SetTransformPath(i, other.GetTransformPath(i));
                SetTransformActive(i, other.GetTransformActive(i));
            }
        }
    
    
}

}
