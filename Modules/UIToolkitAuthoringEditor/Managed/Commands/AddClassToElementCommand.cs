// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct AddClassToElementCommand
{
    const string CommandUndoName = "Add class to element";

    readonly VisualElementAsset ElementAsset;
    readonly string ClassName;

    public AddClassToElementCommand(VisualElementAsset elementAsset, string className)
    {
        ElementAsset = elementAsset;
        ClassName = className;
    }

    public void Execute()
    {
        Assert.IsNotNull(ElementAsset);
        Assert.IsFalse(string.IsNullOrEmpty(ClassName));

        var visualTreeAsset = ElementAsset.visualTreeAsset;

        Undo.RegisterCompleteObjectUndo(visualTreeAsset, CommandUndoName);

        ElementAsset.AddStyleClass(ClassName);
        EditorUtility.SetDirty(ElementAsset.visualTreeAsset);
    }
}
