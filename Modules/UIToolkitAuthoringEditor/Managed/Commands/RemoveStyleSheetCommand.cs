// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct RemoveStyleSheetCommand
{
    const string CommandUndoName = "Remove style sheet";

    readonly VisualTreeAsset VisualTreeAsset;
    readonly StyleSheet StyleSheet;

    public RemoveStyleSheetCommand(VisualTreeAsset visualTreeAsset, StyleSheet styleSheet)
    {
        VisualTreeAsset = visualTreeAsset;
        StyleSheet = styleSheet;
    }

    public void Execute()
    {
        Assert.IsNotNull(VisualTreeAsset);
        Assert.IsNotNull(StyleSheet);

        var rootElement = VisualTreeAsset.visualTreeNoAlloc;

        if (rootElement == null || rootElement.stylesheets.Count == 0)
        {
            return;
        }

        Undo.RegisterCompleteObjectUndo(VisualTreeAsset, CommandUndoName);

        rootElement.stylesheets.Remove(StyleSheet);

        EditorUtility.SetDirty(VisualTreeAsset);
        UIElementsUtility.MarkVisualTreeAssetAsChanged(VisualTreeAsset);
    }
}
