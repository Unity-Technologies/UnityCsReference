// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.U2D.Interface
{
    internal interface IUndoSystem
    {
        void RegisterUndoCallback(Undo.UndoRedoCallback undoCallback);
        void UnregisterUndoCallback(Undo.UndoRedoCallback undoCallback);
        void RegisterCompleteObjectUndo(ScriptableObject obj, string undoText);
        void ClearUndo(ScriptableObject obj);
    }

    internal class UndoSystem : IUndoSystem
    {
        public void RegisterUndoCallback(Undo.UndoRedoCallback undoCallback)
        {
            Undo.undoRedoPerformed += undoCallback;
        }

        public void UnregisterUndoCallback(Undo.UndoRedoCallback undoCallback)
        {
            Undo.undoRedoPerformed -= undoCallback;
        }

        public void RegisterCompleteObjectUndo(ScriptableObject so, string undoText)
        {
            if (so != null)
            {
                Undo.RegisterCompleteObjectUndo(so, undoText);
            }
        }

        public void ClearUndo(ScriptableObject so)
        {
            if (so != null)
            {
                Undo.ClearUndo(so);
            }
        }
    }
}
