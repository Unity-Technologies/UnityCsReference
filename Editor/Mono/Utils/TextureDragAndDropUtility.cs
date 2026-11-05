// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    // Lets packages take Textures dropped on a GameObject in the Hierarchy, e.g. uGUI assigning them to a RawImage under a Canvas
    internal static partial class TextureDragAndDropUtility
    {
        internal delegate DragAndDropVisualMode GetTextureToGameObjectDragVisualModeDelegate(GameObject target, Texture[] textures, HierarchyDropFlags dropMode);
        internal delegate bool PerformTextureToGameObjectDropDelegate(GameObject target, Texture[] textures, HierarchyDropFlags dropMode, SpriteUtility.ShowFileDialogDelegate saveFileDialog);

        [AutoStaticsCleanupOnCodeReload]
        static List<(GetTextureToGameObjectDragVisualModeDelegate getDragVisualMode, PerformTextureToGameObjectDropDelegate perform)> s_TextureToGameObjectDragAndDropHandlers;

        internal static void AddTextureToGameObjectDragAndDropHandler(GetTextureToGameObjectDragVisualModeDelegate getDragVisualMode, PerformTextureToGameObjectDropDelegate perform)
        {
            s_TextureToGameObjectDragAndDropHandlers ??= new List<(GetTextureToGameObjectDragVisualModeDelegate, PerformTextureToGameObjectDropDelegate)>();
            if (!s_TextureToGameObjectDragAndDropHandlers.Contains((getDragVisualMode, perform)))
                s_TextureToGameObjectDragAndDropHandlers.Add((getDragVisualMode, perform));
        }

        internal static void RemoveTextureToGameObjectDragAndDropHandler(GetTextureToGameObjectDragVisualModeDelegate getDragVisualMode, PerformTextureToGameObjectDropDelegate perform)
        {
            s_TextureToGameObjectDragAndDropHandlers?.Remove((getDragVisualMode, perform));
        }

        static DragAndDropVisualMode GetTextureToGameObjectDragVisualModeAndHandler(GameObject target, Texture[] textures, HierarchyDropFlags dropMode, out PerformTextureToGameObjectDropDelegate perform)
        {
            if (s_TextureToGameObjectDragAndDropHandlers == null)
            {
                perform = null;
                return DragAndDropVisualMode.None;
            }

            for (var i = s_TextureToGameObjectDragAndDropHandlers.Count - 1; i >= 0; --i)
            {
                var result = s_TextureToGameObjectDragAndDropHandlers[i].getDragVisualMode(target, textures, dropMode);
                if (result != DragAndDropVisualMode.None)
                {
                    perform = s_TextureToGameObjectDragAndDropHandlers[i].perform;
                    return result;
                }
            }

            perform = null;
            return DragAndDropVisualMode.None;
        }

        [RequiredByNativeCode]
        internal static DragAndDropVisualMode GetTextureToGameObjectDragVisualMode(GameObject target, Texture[] textures, HierarchyDropFlags dropMode)
        {
            return GetTextureToGameObjectDragVisualModeAndHandler(target, textures, dropMode, out _);
        }

        [RequiredByNativeCode]
        internal static bool PerformTextureToGameObjectDrop(GameObject target, Texture[] textures, HierarchyDropFlags dropMode, SpriteUtility.ShowFileDialogDelegate saveFileDialog)
        {
            var result = GetTextureToGameObjectDragVisualModeAndHandler(target, textures, dropMode, out var perform);
            if (result == DragAndDropVisualMode.None || result == DragAndDropVisualMode.Rejected)
                return false;

            return perform(target, textures, dropMode, saveFileDialog);
        }
    }
}
