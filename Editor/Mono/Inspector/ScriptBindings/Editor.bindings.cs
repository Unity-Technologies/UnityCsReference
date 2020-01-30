// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using uei = UnityEngine.Internal;

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Editor/Mono/Inspector/ScriptBindings/Editor.bindings.h")]
    [StaticAccessor("EditorBindings", StaticAccessorType.DoubleColon)]
    public partial class Editor
    {
        // Make a custom editor for /targetObject/ or /objects/.
        extern static Editor CreateEditorWithContextInternal(Object[] targetObjects, Object context, Type editorType);
        internal extern static Vector2 GetCurrentMousePosition();
    }
}
