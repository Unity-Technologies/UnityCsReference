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

namespace UnityEngine
{


internal sealed partial class DrivenPropertyManager
{
    [System.Diagnostics.ConditionalAttribute("UNITY_EDITOR")]
    public static void RegisterProperty(Object driver, Object target, string propertyPath)
        {
            if (driver == null)
                throw new ArgumentNullException("driver");
            if (target == null)
                throw new ArgumentNullException("target");
            if (propertyPath == null)
                throw new ArgumentNullException("propertyPath");

            RegisterPropertyInternal(driver, target, propertyPath);
        }
    
    
    [System.Diagnostics.ConditionalAttribute("UNITY_EDITOR")]
    public static void UnregisterProperty(Object driver, Object target, string propertyPath)
        {
            if (driver == null)
                throw new ArgumentNullException("driver");
            if (target == null)
                throw new ArgumentNullException("target");
            if (propertyPath == null)
                throw new ArgumentNullException("propertyPath");

            UnregisterPropertyInternal(driver, target, propertyPath);
        }
    
    
    [System.Diagnostics.ConditionalAttribute("UNITY_EDITOR")]
    public static void UnregisterProperties(Object driver)
        {
            if (driver == null)
                throw new ArgumentNullException("driver");

            UnregisterPropertiesInternal(driver);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void RegisterPropertyInternal (Object driver, Object target, string propertyPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void UnregisterPropertyInternal (Object driver, Object target, string propertyPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void UnregisterPropertiesInternal (Object driver) ;

}


}
