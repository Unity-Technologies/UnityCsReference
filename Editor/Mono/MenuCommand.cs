// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Used to extract the context for a [[MenuItem]]. MenuCommand objects are passed to custom menu item functions defined using the [[MenuItem]] attribute.
    // Keep in sync with MenuCommandBinding in Runtime\Scripting\ManagedAttributeManager.h
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public sealed class MenuCommand
    {
        // Context is the object that is the target of a menu command.
        public Object context;
        // An integer for passing custom information to a menu item.
        public int    userData;

        // Creates a new MenuCommand object.
        public MenuCommand(Object inContext, int inUserData) { context = inContext; userData = inUserData;  }
        // Creates a new MenuCommand object.
        public MenuCommand(Object inContext) { context = inContext; userData = 0;  }
    }
}
