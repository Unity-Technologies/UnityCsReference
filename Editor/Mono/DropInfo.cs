// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    internal class DropInfo
    {
        internal enum Type
        {
            // The window will be inserted as a tab into dropArea
            Tab = 0,
            // The window will be a new pane (inside a scrollView)
            Pane = 1,
            // A new window should be created.
            Window
        }

        public DropInfo(IDropArea source)
        {
            dropArea = source;
        }

        // Who claimed the drop?
        public IDropArea dropArea;

        // Extra data for the recipient to communicate between DragOVer and PerformDrop
        public object userData = null;

        // Which type of dropzone are we looking for?
        public Type type = Type.Window;
        // Where should the preview end up on screen.
        public Rect rect;
    }
}
