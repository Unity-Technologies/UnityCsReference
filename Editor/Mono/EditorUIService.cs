// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.ShortcutManagement;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal interface IEditorUIService
    {
        // Editor window
        IWindowBackend GetDefaultWindowBackend(IWindowModel model);

        // Toolbar
        Type GetDefaultToolbarType();
        void AddSubToolbar(SubToolbar subToolbar);

        // Inspector
        IEditorElement CreateEditorElement(int editorIndex, IPropertyView iw, string title);

        // PackageManagerUI
        void PackageManagerOpen();

        // ShortcutManager
        IShortcutManagerWindowView CreateShortcutManagerWindowView(IShortcutManagerWindowViewController viewController, IKeyBindingStateProvider bindingStateProvider);

        // Progress
        void ProgressWindowShowDetails(bool shouldReposition);
        void ProgressWindowHideDetails();
        bool ProgressWindowCanHideDetails();

        // UIToolkit
        void AddDefaultEditorStyleSheets(VisualElement ve);

        string GetUIToolkitDefaultCommonDarkStyleSheetPath();
        string GetUIToolkitDefaultCommonLightStyleSheetPath();

        StyleSheet GetUIToolkitDefaultCommonDarkStyleSheet();
        StyleSheet GetUIToolkitDefaultCommonLightStyleSheet();
    }

    internal static class EditorUIService
    {
        public static IEditorUIService instance { get; set; }
    }
}
