// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Connect;
using UnityEditor.PackageManager.UI;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class EditorUIServiceImpl : IEditorUIService
    {
        // This is called on beforeProcessingInitializeOnLoad callback to ensure
        // the instance is set before InitializeOnLoad attributes are processed
        [RequiredByNativeCode]
        static void InitializeInstance()
        {
            if (EditorUIService.instance == null)
                EditorUIService.instance = new EditorUIServiceImpl();
        }

        private EditorUIServiceImpl()
        {}

        public IWindowBackend GetDefaultWindowBackend(IWindowModel model) => model is IEditorWindowModel ? new DefaultEditorWindowBackend() : new DefaultWindowBackend();

        public Type GetDefaultToolbarType() => typeof(UnityMainToolbar);
        public void AddSubToolbar(SubToolbar subToolbar) => UnityMainToolbar.AddSubToolbar(subToolbar);

        public IEditorElement CreateEditorElement(int editorIndex, IPropertyView iw, string title) => new EditorElement(editorIndex, iw) {name = title};

        public void PackageManagerOpen() => PackageManagerWindow.OpenPackageManager(null);

        public IShortcutManagerWindowView CreateShortcutManagerWindowView(IShortcutManagerWindowViewController viewController, IKeyBindingStateProvider bindingStateProvider) =>
            new ShortcutManagerWindowView(viewController, bindingStateProvider);

        public void ProgressWindowShowDetails(bool shouldReposition) => ProgressWindow.ShowDetails(shouldReposition);
        public void ProgressWindowHideDetails() => ProgressWindow.HideDetails();
        public bool ProgressWindowCanHideDetails() => ProgressWindow.canHideDetails;

        public void AddDefaultEditorStyleSheets(VisualElement ve) => UIElementsEditorUtility.AddDefaultEditorStyleSheets(ve);
        public string GetUIToolkitDefaultCommonDarkStyleSheetPath() => UIElementsEditorUtility.s_DefaultCommonDarkStyleSheetPath;
        public string GetUIToolkitDefaultCommonLightStyleSheetPath() => UIElementsEditorUtility.s_DefaultCommonLightStyleSheetPath;
        public StyleSheet GetUIToolkitDefaultCommonDarkStyleSheet() => UIElementsEditorUtility.s_DefaultCommonDarkStyleSheet;
        public StyleSheet GetUIToolkitDefaultCommonLightStyleSheet() => UIElementsEditorUtility.s_DefaultCommonLightStyleSheet;
    }
}
