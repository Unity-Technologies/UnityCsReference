// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace UnityEditor
{
    class DefaultMainToolbar : MainToolbarVisual
    {
        EditorToolbar m_CenterToolbar;
        EditorToolbar m_RightToolbar;

        protected override VisualElement CreateRoot()
        {
            var visualTree = EditorToolbarUtility.LoadUxml("MainToolbar");
            VisualElement root = new VisualElement();
            root.style.flexGrow = 1;
            visualTree.CloneTree(root);

            m_CenterToolbar = new EditorToolbar(null, root.Q("ToolbarZonePlayMode"),
                "Editor Utility/Play Mode");

            var rightContainer = root.Q("ToolbarZoneRightAlign");
            m_RightToolbar = new EditorToolbar(null, rightContainer,
                "Editor Utility/Layout",
                "Editor Utility/Layers",
                "Services/Account",
                "Services/Cloud",
                "Editor Utility/Imgui Subtoolbars",
                "Editor Utility/Search",
                "Package Manager/PreviewPackagesInUse",
                "Editor Utility/Modes");

            EditorToolbarUtility.LoadStyleSheets("MainToolbar", root);
            return root;
        }
    }
}
