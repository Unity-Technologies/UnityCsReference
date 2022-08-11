// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace ManagedAttributeManagerTests
{
    [StructLayout(LayoutKind.Sequential)]
    public class Object
    {
        public IntPtr m_CachedPtr;
        public int m_InstanceID;
        public string m_UnityRuntimeErrorString;
    }

    public class ScriptableObject : Object { }

    public class Component : Object { }

    public class MonoBehaviour : Component { }

    public sealed class ExecuteInEditMode : Attribute { }

    public sealed class ExecuteAlways : Attribute { }

    public class SomeMonoBehaviour : MonoBehaviour { }

    [ExecuteInEditMode]
    public class SomeInEditModeMonoBehaviour : MonoBehaviour { }

    [ExecuteAlways]
    public class SomeExecuteAlwaysMonoBehaviour : MonoBehaviour { }

    public class NestedInEditModeMonoBehaviour : SomeInEditModeMonoBehaviour { }

    public class NestedExecuteAlwaysMonoBehaviour : SomeExecuteAlwaysMonoBehaviour { }

    [ExecuteAlways]
    public class NestedOverrideExecuteAlwaysMonoBehaviour : SomeInEditModeMonoBehaviour { }

    [ExecuteInEditMode]
    public class NestedOverrideInEditModeMonoBehavior : SomeInEditModeMonoBehaviour { }

    public class SomeScriptableObject : ScriptableObject { }

    [ExecuteInEditMode]
    public class SomeInEditModeScriptableObject : ScriptableObject { }

    [ExecuteAlways]
    public class SomeExecuteAlwaysScriptableObject : ScriptableObject { }

    public class NestedInEditModeScriptableObject : SomeInEditModeScriptableObject { }

    public class NestedExecuteAlwaysScriptableObject : SomeExecuteAlwaysScriptableObject { }

    [ExecuteAlways]
    public class NestedOverrideExecuteAlwaysScriptableObject : SomeInEditModeScriptableObject { }

    [ExecuteInEditMode]
    public class NestedOverrideInEditModeScriptableObject : SomeInEditModeScriptableObject { }
}
