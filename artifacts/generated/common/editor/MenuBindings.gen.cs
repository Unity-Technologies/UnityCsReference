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
using UnityEngine.Scripting;

namespace UnityEditor
{


public sealed partial class Menu
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetChecked (string menuPath, bool isChecked) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetChecked (string menuPath) ;

}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[RequiredByNativeCode]
public sealed partial class MenuItem : Attribute
{
    public MenuItem(string itemName) : this(itemName, false) {}
    
    
    public MenuItem(string itemName, bool isValidateFunction) : this(itemName, isValidateFunction, itemName.StartsWith("GameObject/Create Other") ? 10 : 1000) {}
    
    
    public MenuItem(string itemName, bool isValidateFunction, int priority) : this(itemName, isValidateFunction, priority, false) {}
    
    
    internal MenuItem(string itemName, bool isValidateFunction, int priority, bool internalMenu)
        {
            if (internalMenu)
                menuItem = "internal:" + itemName;
            else
                menuItem = itemName;
            validate = isValidateFunction;
            this.priority = priority;
        }
    
    
    public string menuItem;
    public bool validate;
    public int priority;
}

[StructLayout(LayoutKind.Sequential)]
[RequiredByNativeCode]
public sealed partial class MenuCommand
{
    public Object context;
    public int    userData;
    
    
    public MenuCommand(Object inContext, int inUserData) { context = inContext; userData = inUserData;  }
    public MenuCommand(Object inContext) { context = inContext; userData = 0;  }
}


}
