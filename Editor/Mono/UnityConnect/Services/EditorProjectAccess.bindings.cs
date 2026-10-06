// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Web
{
    [NativeHeader("Editor/Src/UnityConnect/Services/EditorProjectAccess.h")]
    [NativeClass("EditorProjectAccess", PersistentTypeId = 0x1968D9A2)]
    internal partial class EditorProjectAccess : Object
    {
        internal EditorProjectAccess(global::UnityEngine.EntityId id) : base(id) {}
        public EditorProjectAccess()
        {
            SetEntityIdFromConstructor(Internal_Create());
        }

        extern private static EntityId Internal_Create();
        extern public string GetProjectEditorVersion();
    }
}
