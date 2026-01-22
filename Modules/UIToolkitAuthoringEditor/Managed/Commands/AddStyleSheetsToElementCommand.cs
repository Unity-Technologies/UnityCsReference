// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct AddStyleSheetsToElementCommand
{
    const string CommandUndoName = "Add style sheets to element";

    readonly VisualElementAsset VisualElementAsset;
    readonly StyleSheet[] StyleSheets;

    public AddStyleSheetsToElementCommand(VisualElementAsset visualElementAsset, StyleSheet[] styleSheets)
    {
        VisualElementAsset = visualElementAsset;
        StyleSheets = styleSheets;
    }

    public void Execute()
    {
        Assert.IsNotNull(VisualElementAsset);
        Assert.IsNotNull(VisualElementAsset.visualTreeAsset);
        Assert.IsNotNull(StyleSheets);
        foreach (var styleSheet in StyleSheets)
        {
            Assert.IsNotNull(styleSheet);
            Assert.IsFalse(string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(styleSheet)));
        }

        var visualTreeAsset = VisualElementAsset.visualTreeAsset;
        Undo.RegisterCompleteObjectUndo(visualTreeAsset, CommandUndoName);

        // Remove existing style sheet so that they are re-added at the end.
        foreach (var styleSheet in StyleSheets)
            VisualElementAsset.stylesheets.Remove(styleSheet);

        VisualElementAsset.stylesheets.AddRange(StyleSheets);

        EditorUtility.SetDirty(visualTreeAsset);
    }
}
