// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
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
        IEditorElement CreateCulledEditorElement(int editorIndex, IPropertyView iw, string title);

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

        BindableElement CreateFloatField(string name, Func<float, float> onValidateValue = null, bool isDelayed = false);
        BindableElement CreateDoubleField(string name, Func<double, double> onValidateValue = null, bool isDelayed = false);
        BindableElement CreateIntField(string name, Func<int, int> onValidateValue = null, bool isDelayed = false);
        BindableElement CreateLongField(string name, Func<long, long> onValidateValue = null, bool isDelayed = false);
        BindableElement CreateVector2Field(string name, Func<Vector2, Vector2> onValidateValue);
        BindableElement CreateVector2IntField(string name, Func<Vector2Int, Vector2Int> onValidateValue);
        BindableElement CreateVector3Field(string name, Func<Vector3, Vector3> onValidateValue);
        BindableElement CreateVector3IntField(string name, Func<Vector3Int, Vector3Int> onValidateValue);
        BindableElement CreateVector4Field(string name, Func<Vector4, Vector4> onValidateValue);
        BindableElement CreateTextField(string name = null, bool isMultiLine = false, bool isDelayed = false);
        BindableElement CreateColorField(string name, bool showAlpha, bool hdr);
        BindableElement CreateGradientField(string name, bool hdr, ColorSpace colorSpace);

        string GetUIToolkitDefaultCommonDarkStyleSheetPath();
        string GetUIToolkitDefaultCommonLightStyleSheetPath();

        StyleSheet GetUIToolkitDefaultCommonDarkStyleSheet();
        StyleSheet GetUIToolkitDefaultCommonLightStyleSheet();
        StyleSheet CompileStyleSheetContent(string styleSheetContent, bool disableValidation, bool reportErrors);
    }

    internal static class EditorUIService
    {
        public static IEditorUIService instance { get; set; }

        public static bool disableInspectorElementThrottling { get; set; } = false;
    }
}
