// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngineInternal;

namespace UnityEngine
{
    // A class used as a stand-in when a script, used by a ScriptedObject, is missing.
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeClass(null)]
    [ExcludeFromObjectFactory]
    class FailedToLoadScriptObject : Object
    {
        private FailedToLoadScriptObject() {}
    }
}
