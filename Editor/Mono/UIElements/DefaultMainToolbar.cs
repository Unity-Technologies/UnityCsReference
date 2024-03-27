// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace UnityEditor
{
    class DefaultMainToolbar : MainToolbarVisual
    {
        static IEnumerable<string> leftToolbar
        {
            get
            {
                //Modules/EditorToolbar/ToolbarElements/*.cs
                yield return "Services/Account";
                yield return "Services/Cloud";
                //com.unity.collab-proxy/Editor/PlasticSCM/Toolbar/ToolbarButton.cs
                yield return "Services/Version Control";
                //Modules/EditorToolbar/ToolbarElements/*.cs
                yield return "Editor Utility/Store";
                //Editor/Mono/GUI/Toolbars/MainToolbarImguiContainer.cs
                yield return "Editor Utility/Imgui Subtoolbars";
            }
        }

        static IEnumerable<string> middleToolbar
        {
            get
            {
                // Modules/EditorToolbar/ToolbarElements/PlayModeButtons.cs
                yield return "Editor Utility/Play Mode";
            }
        }

        static IEnumerable<string> rightToolbar
        {
            get
            {
                // Modules/EditorToolbar/ToolbarElements/*.cs
                yield return "Editor Utility/Layout";
                yield return "Editor Utility/Layers";
                yield return "Editor Utility/Search";
                yield return "Editor Utility/Modes";
                yield return "Package Manager/PreviewPackagesInUse";
                yield return "Editor Utility/Undo";
                // Modules/Multiplayer/MultiplayerRoleDropdown.cs
                yield return "Multiplayer/MultiplayerRole";
            }
        }

        protected override VisualElement CreateRoot()
        {
            var visualTree = EditorToolbarUtility.LoadUxml("MainToolbar");

            VisualElement root = new VisualElement();
            root.style.flexGrow = 1;
            visualTree.CloneTree(root);

            var left = new EditorToolbar(leftToolbar);
            left.LoadToolbarElements(root.Q("ToolbarZoneLeftAlign"));

            var middle = new EditorToolbar(middleToolbar);
            middle.LoadToolbarElements(root.Q("ToolbarZonePlayMode"));

            var right = new EditorToolbar(rightToolbar);
            right.LoadToolbarElements(root.Q("ToolbarZoneRightAlign"));

            EditorToolbarUtility.LoadStyleSheets("MainToolbar", root);
            return root;
        }
    }
}
