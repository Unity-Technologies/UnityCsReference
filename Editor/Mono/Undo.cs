// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor
{
    public partial class Undo
    {
        internal enum UndoRedoType { Undo, Redo };

        [RequiredByNativeCode]
        static void OnSelectionUndo(bool redo)
        {
            if (selectionUndoRedoPerformed != null)
                selectionUndoRedoPerformed(redo ? UndoRedoType.Redo : UndoRedoType.Undo);
        }

        internal static event Action<UndoRedoType> selectionUndoRedoPerformed;
    }
}
