// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using UnityEngine;
using System;
using System.ComponentModel;
using registry = UnityEditorInternal;

namespace UnityEditorInternal
{


public enum RegistryView
{
    Default = 0,
    _32 = 1,
    _64 = 2,
}

public sealed partial class RegistryUtil
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  uint GetRegistryUInt32Value (string subKey, string valueName, uint defaultValue, registry::RegistryView view) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetRegistryStringValue (string subKey, string valueName, string defaultValue, registry::RegistryView view) ;

}


}
