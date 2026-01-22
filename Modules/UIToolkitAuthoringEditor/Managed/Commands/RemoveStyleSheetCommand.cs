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
    readonly int Index;

    public RemoveStyleSheetCommand(VisualTreeAsset visualTreeAsset, int index)
    {
        VisualTreeAsset = visualTreeAsset;
        Index = index;
    }

    public bool Execute()
    {
        Assert.IsNotNull(VisualTreeAsset);

        var rootElement = VisualTreeAsset.visualTreeNoAlloc;
        var actualIndex = Index == -1 ? rootElement.stylesheets.Count - 1 : Index;
        if (rootElement?.stylesheets == null || actualIndex < 0 || actualIndex >= rootElement.stylesheets.Count)
        {
            return false;
        }

        Undo.RegisterCompleteObjectUndo(VisualTreeAsset, CommandUndoName);

        rootElement.stylesheets.RemoveAt(actualIndex);
        EditorUtility.SetDirty(VisualTreeAsset);

        return true;
    }
}
