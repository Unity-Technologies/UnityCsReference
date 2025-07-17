// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{ 
    static class SubToolbarZone
    {
        const string k_Path = "Services/Collab";

        [MainToolbarElement(k_Path, true, defaultDockPosition = MainToolbarDockPosition.Left, defaultDockIndex = 11)]
        [UnityOnlyMainToolbarPreset]
        static IEnumerable<MainToolbarElement> CreateSubToolbarZone()
        {
            foreach (SubToolbar subToolbar in Toolbar.subToolbars)
            {
                yield return new MainToolbarCustom(() =>
                {
                    var container = new IMGUIContainer();
                    container.style.width = subToolbar.Width;
                    container.style.height = 20;
                    container.onGUIHandler += () => subToolbar.OnGUI(container.rect);
                    return container;
                });
            }
        }
    }

#pragma warning disable CS0618 // Type or member is obsolete
    [InitializeOnLoad]
    static class DeprecatedElementsToolbar
    {
        sealed class AccountDropdown : EditorToolbarDropdown { }
        sealed class StoreButton : EditorToolbarButton { }
        sealed class MainToolbarImguiContainer : IMGUIContainer { }
        sealed class PlayModeButtons : VisualElement { }
        sealed class LayoutDropdown : EditorToolbarDropdown { }
        sealed class SearchButton : EditorToolbarButton { }
        sealed class ModesDropdown : EditorToolbarDropdown { }
        sealed class PreviewPackagesInUseDropdown : EditorToolbarDropdown { }
        sealed class UndoButton : EditorToolbarButton { }
        sealed class MultiplayerRoleDropdown : EditorToolbarDropdown { }
        sealed class CloudButton : EditorToolbarButton { }

        static DeprecatedElementsToolbar()
        {
            Toolbar.populateFakeToolbar += PopulateFakeToolbar;
        }

        static void PopulateFakeToolbar(MainToolbarDockPosition zone, VisualElement root)
        {
            switch (zone)
            {
                case MainToolbarDockPosition.Left:
                    root.Add(CreateFakeElement<VisualElement>("ToolbarProductCaption", "unity-editor-toolbar-product-caption"));
                    root.Add(CreateFakeElement<AccountDropdown>("AccountDropdown"));
                    root.Add(CreateFakeElement<StoreButton>(""));
                    root.Add(CreateFakeElement<MainToolbarImguiContainer>(""));
                    break;

                case MainToolbarDockPosition.Middle:
                    var playButtons = CreateFakeElement<PlayModeButtons>("PlayMode");
                    root.Add(playButtons);
                    playButtons.Add(CreateFakeElement<EditorToolbarToggle>("Play"));
                    playButtons.Add(CreateFakeElement<EditorToolbarToggle>("Pause"));
                    playButtons.Add(CreateFakeElement<EditorToolbarButton>("Step"));
                    break;

                case MainToolbarDockPosition.Right:
                    root.Add(CreateFakeElement<LayoutDropdown>("LayoutDropdown"));
                    root.Add(CreateFakeElement<SearchButton>(""));
                    root.Add(CreateFakeElement<ModesDropdown>("ModesDropdown"));
                    root.Add(CreateFakeElement<PreviewPackagesInUseDropdown>("PreviewPackagesInUseDropdown", "unity-toolbar-button-preview-packages-in-use"));
                    root.Add(CreateFakeElement<UndoButton>("History"));
                    root.Add(CreateFakeElement<MultiplayerRoleDropdown>(""));
                    root.Add(CreateFakeElement<CloudButton>("Cloud"));
                    break;
            }
        }

        static T CreateFakeElement<T>(string name, params string[] classes) where T : VisualElement, new()
        {
            var element = new T();
            element.name = name;

            foreach (var @class in classes)
                element.AddToClassList(@class);

            return element;
        }

        [MainToolbarElement(Toolbar.deprecatedElementsId, false, defaultDockPosition = MainToolbarDockPosition.Left, defaultDockIndex = 11)]
        static MainToolbarElement CreateSubToolbarZone()
        {
            return new MainToolbarCustom(CreateToolbar);
        }

        static VisualElement CreateToolbar()
        {
            VisualElement root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            if (Toolbar.get != null)
                foreach (var element in Toolbar.get.deprecatedElements)
                    root.Add(element);

            return root;
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
