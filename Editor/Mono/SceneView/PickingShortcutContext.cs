// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.ShortcutManagement;

namespace UnityEditor
{
    class SceneViewPickingShortcutContext : IShortcutContext
    {
        public SceneView window => EditorWindow.focusedWindow as SceneView;

        public bool active
        {
            get
            {
                if (!(EditorWindow.focusedWindow is SceneView view))
                    return false;

                return view.sceneViewMotion.viewportsUnderMouse && Tools.current != Tool.View;
            }
        }
    }
}
