// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.UI;
using UnityEditor.ShortcutManagement;
using UnityEditor.StyleSheets;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
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

        public Type GetDefaultToolbarType() => typeof(DefaultMainToolbar);
        public void AddSubToolbar(SubToolbar subToolbar) => MainToolbarImguiContainer.AddDeprecatedSubToolbar(subToolbar);

        public IEditorElement CreateEditorElement(int editorIndex, IPropertyView iw, string title) => new EditorElement(editorIndex, iw) {name = title};

        public IEditorElement CreateCulledEditorElement(int editorIndex, IPropertyView iw, string title) => new EditorElement(editorIndex, iw, true) {name = title};

        public void PackageManagerOpen() => PackageManagerWindow.OpenPackageManager(null);

        public IShortcutManagerWindowView CreateShortcutManagerWindowView(IShortcutManagerWindowViewController viewController, IKeyBindingStateProvider bindingStateProvider) =>
            new ShortcutManagerWindowView(viewController, bindingStateProvider);

        public void ProgressWindowShowDetails(bool shouldReposition) => ProgressWindow.ShowDetails(shouldReposition);
        public void ProgressWindowHideDetails() => ProgressWindow.HideDetails();
        public bool ProgressWindowCanHideDetails() => ProgressWindow.canHideDetails;

        public void AddDefaultEditorStyleSheets(VisualElement ve) => UIElementsEditorUtility.AddDefaultEditorStyleSheets(ve);
        public string GetUIToolkitDefaultCommonDarkStyleSheetPath() => UIElementsEditorUtility.s_DefaultCommonDarkStyleSheetPath;
        public string GetUIToolkitDefaultCommonLightStyleSheetPath() => UIElementsEditorUtility.s_DefaultCommonLightStyleSheetPath;
        public StyleSheet GetUIToolkitDefaultCommonDarkStyleSheet() => UIElementsEditorUtility.GetCommonDarkStyleSheet();
        public StyleSheet GetUIToolkitDefaultCommonLightStyleSheet() => UIElementsEditorUtility.GetCommonLightStyleSheet();

        public StyleSheet CompileStyleSheetContent(string styleSheetContent, bool disableValidation, bool reportErrors)
        {
            var importer = new StyleSheetImporterImpl();
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            importer.disableValidation = disableValidation;
            importer.Import(styleSheet, styleSheetContent);
            if (reportErrors)
            {
                foreach (var err in importer.importErrors)
                    Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, styleSheet, err.ToString());
            }
            return styleSheet;
        }
    }
}
