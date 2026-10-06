// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    internal partial class DrivenRectTransformUndo
    {
        // Both undo events are cleared on code reload, so these subscriptions have to be re-established on
        // every load. A static constructor would only run once per domain, and driven RectTransform values
        // would stop being refreshed around undo/redo after the first reload.
        [OnCodeLoaded]
        static void Initialize()
        {
            Undo.willFlushUndoRecord += ForceUpdateCanvases;
            // After undo or redo performed, the 'driven values' & 'driven properties mask' need to be updated.
            Undo.undoRedoEvent += OnUndoRedoPerformed;
        }

        [OnCodeUnloading]
        static void Shutdown()
        {
            Undo.willFlushUndoRecord -= ForceUpdateCanvases;
            Undo.undoRedoEvent -= OnUndoRedoPerformed;
        }

        static void ForceUpdateCanvases()
        {
            Canvas.ForceUpdateCanvases();
        }

        static void OnUndoRedoPerformed(in UndoRedoInfo info)
        {
            Canvas.ForceUpdateCanvases();
        }
    }
}
