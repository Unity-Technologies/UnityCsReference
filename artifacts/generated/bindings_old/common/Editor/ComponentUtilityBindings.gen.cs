// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using Object = UnityEngine.Object;







namespace UnityEditorInternal
{
public sealed partial class ComponentUtility
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool MoveComponentUp (Component component) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool MoveComponentDown (Component component) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool CopyComponent (Component component) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool PasteComponentValues (Component component) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool PasteComponentAsNew (GameObject go) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool CollectConnectedComponents (GameObject targetGameObject, Component[] components, bool copy, out Component[] outCollectedComponents, out string outErrorMessage) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool MoveComponentToGameObject (Component component, GameObject targetGameObject, [uei.DefaultValue("false")]  bool validateOnly ) ;

    [uei.ExcludeFromDocs]
    internal static bool MoveComponentToGameObject (Component component, GameObject targetGameObject) {
        bool validateOnly = false;
        return MoveComponentToGameObject ( component, targetGameObject, validateOnly );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool MoveComponentRelativeToComponent (Component component, Component targetComponent, bool aboveTarget, [uei.DefaultValue("false")]  bool validateOnly ) ;

    [uei.ExcludeFromDocs]
    internal static bool MoveComponentRelativeToComponent (Component component, Component targetComponent, bool aboveTarget) {
        bool validateOnly = false;
        return MoveComponentRelativeToComponent ( component, targetComponent, aboveTarget, validateOnly );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool MoveComponentsRelativeToComponents (Component[] components, Component[] targetComponents, bool aboveTarget, [uei.DefaultValue("false")]  bool validateOnly ) ;

    [uei.ExcludeFromDocs]
    internal static bool MoveComponentsRelativeToComponents (Component[] components, Component[] targetComponents, bool aboveTarget) {
        bool validateOnly = false;
        return MoveComponentsRelativeToComponents ( components, targetComponents, aboveTarget, validateOnly );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool CopyComponentToGameObject (Component component, GameObject targetGameObject, bool validateOnly, out Component outNewComponent) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool CopyComponentToGameObjects (Component component, GameObject[] targetGameObjects, bool validateOnly, out Component[] outNewComponents) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool CopyComponentRelativeToComponent (Component component, Component targetComponent, bool aboveTarget, bool validateOnly, out Component outNewComponent) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool CopyComponentRelativeToComponents (Component component, Component[] targetComponents, bool aboveTarget, bool validateOnly, out Component[] outNewComponents) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool CopyComponentsRelativeToComponents (Component[] components, Component[] targetComponents, bool aboveTarget, bool validateOnly, out Component[] outNewComponents) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool WarnCanAddScriptComponent (GameObject gameObject, MonoScript script) ;

}


}
