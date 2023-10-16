// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.ShortcutManagement;

namespace UnityEditor.EditorTools
{
    class ToolShortcutContext : IShortcutToolContext
    {
        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.delayCall += () =>
            {
                ShortcutIntegration.instance.contextManager.RegisterToolContext(new ToolShortcutContext());
            };
        }

        public bool active
        {
            get
            {
                var focus = EditorWindow.focusedWindow;
                return focus is SceneView || focus is SceneHierarchyWindow;
            }
        }
    }
}
