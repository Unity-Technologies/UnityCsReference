// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    // Interface for drag-dropping windows over each other.
    // Must be implemented by anyone who can handle a dragged tab.
    internal interface IDropArea
    {
        // Fill out a dropinfo class telling what should be done.
        // NULL if no action
        DropInfo DragOver(EditorWindow w, Vector2 screenPos);

        // If the client returned a DropInfo from the DragOver, they will get this call when the user releases the mouse
        bool PerformDrop(EditorWindow w, DropInfo dropInfo, Vector2 screenPos);
    }
}
