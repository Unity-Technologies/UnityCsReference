// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;

namespace UnityEditor
{



public enum GizmoType
{
    
    Pickable = 1,
    
    NotInSelectionHierarchy = 2,
    
    NonSelected = 32,
    
    Selected = 4,
    
    Active = 8,
    
    InSelectionHierarchy = 16,
    [System.Obsolete ("Use NotInSelectionHierarchy instead (UnityUpgradable) -> NotInSelectionHierarchy")]
    NotSelected = -127,
    [System.Obsolete ("Use InSelectionHierarchy instead (UnityUpgradable) -> InSelectionHierarchy")]
    SelectedOrChild = -127
}

public sealed partial class DrawGizmo : System.Attribute
{
    public  DrawGizmo(GizmoType gizmo)
        {
            drawOptions = gizmo;
        }
    
    
    public DrawGizmo(GizmoType gizmo, Type drawnGizmoType)
        {
            drawnType = drawnGizmoType;
            drawOptions = gizmo;
        }
    
    
    public Type drawnType;
    public GizmoType drawOptions;
}


}
