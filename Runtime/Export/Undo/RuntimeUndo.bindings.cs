// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEngine
{
    [NativeHeader("Editor/Src/Undo/Undo.h")]
    internal class RuntimeUndo
    {
        internal void SetTransformParentUndo(Transform transform, Transform newParent, string name)
        {
            SetTransformParent(transform, newParent, true, name);
        }

        [FreeFunction("SetTransformParentUndo")]
        extern internal static void SetTransformParent([NotNull] Transform transform, Transform newParent, bool worldPositionStays, string name);

        [FreeFunction("RecordUndoDiff")]
        extern internal static void RecordObject(Object objectToUndo, string name);

        [FreeFunction("RecordUndoDiff")]
        extern internal static void RecordObjects(Object[] objectsToUndo, string name);
    }
}
