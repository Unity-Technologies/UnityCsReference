// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.U2D.Interface
{
    internal interface IUndoableObject
    {}

    internal interface IUndoSystem
    {
        void RegisterUndoCallback(Undo.UndoRedoCallback undoCallback);
        void UnregisterUndoCallback(Undo.UndoRedoCallback undoCallback);
        void RegisterCompleteObjectUndo(IUndoableObject obj, string undoText);
        void ClearUndo(IUndoableObject obj);
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

        public void RegisterCompleteObjectUndo(IUndoableObject obj, string undoText)
        {
            var so = CheckUndoObjectType(obj);
            if (so != null)
            {
                Undo.RegisterCompleteObjectUndo(so, undoText);
            }
        }

        ScriptableObject CheckUndoObjectType(IUndoableObject obj)
        {
            ScriptableObject so = obj as ScriptableObject;
            if (so == null)
            {
                Debug.LogError("Register Undo object is not a ScriptableObject");
            }
            return so;
        }

        public void ClearUndo(IUndoableObject obj)
        {
            var so = CheckUndoObjectType(obj);
            if (so != null)
            {
                Undo.ClearUndo(so);
            }
        }
    }
}
