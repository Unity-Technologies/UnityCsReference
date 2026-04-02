// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct AddStyleSheetCommand
{
    const string CommandUndoName = "Add style sheet";

    readonly VisualTreeAsset VisualTreeAsset;
    readonly string UssPath;
    readonly int Index;

    public AddStyleSheetCommand(VisualTreeAsset visualTreeAsset, string ussPath, int index = -1)
    {
        VisualTreeAsset = visualTreeAsset;
        UssPath = ussPath;
        Index = index;
    }

    public void Execute()
    {
        Assert.IsFalse(string.IsNullOrEmpty(UssPath));
        Assert.IsNotNull(VisualTreeAsset);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
        if (styleSheet.importedWithErrors)
        {
            var errorMessage = $"The USS file at path {UssPath} has import errors and cannot be used.";
            EditorUtility.DisplayDialog("Invalid USS File", errorMessage, "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(VisualTreeAsset, CommandUndoName);

        var rootElement = VisualTreeAsset.visualTreeNoAlloc;
        if (rootElement != null)
        {
            rootElement.stylesheets ??= [];
            var actualIndex = Index == -1 ? rootElement.stylesheets.Count : Index;

            if (actualIndex < 0 || actualIndex > rootElement.stylesheets.Count)
            {
                Debug.LogWarning($"Invalid index {Index}. Must be -1 (append) or between 0 and {rootElement.stylesheets.Count}.");
                return;
            }

            rootElement.stylesheets.Insert(actualIndex, styleSheet);
        }

        EditorUtility.SetDirty(VisualTreeAsset);
        UIElementsUtility.MarkVisualTreeAssetAsChanged(VisualTreeAsset);
    }
}
