// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Web
{
    [NativeHeader("Editor/Src/UnityConnect/Services/EditorProjectAccess.h")]
    internal partial class EditorProjectAccess : Object
    {
        public EditorProjectAccess()
        {
            Internal_Create(this);
        }

        extern private static void Internal_Create([Writable] EditorProjectAccess self);
        extern public string GetProjectEditorVersion();
        extern public string GetRESTServiceURI();
    }
}
