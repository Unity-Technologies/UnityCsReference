// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace EmbeddedScriptedObjectsTests
{
    public sealed class ExtensionOfNativeClass : Attribute {}

    // Mimic exactly data layout of UnityEnging.Object!
    [StructLayout(LayoutKind.Sequential)]
    public class Object
    {
        public IntPtr m_CachedPtr;
        public int m_InstanceID;
        public string m_UnityRuntimeErrorString;
    }

    [ExtensionOfNativeClass]
    public class DummyClass : Object
    {
        public int Attribute1 = 1;
        public int GetValue() { return Attribute1; }
        public void SetValue(int val) { Attribute1 = val; }
    }
}
