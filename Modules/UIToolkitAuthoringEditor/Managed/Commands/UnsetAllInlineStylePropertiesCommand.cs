// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct UnsetAllInlineStylePropertiesCommand
{
    const string CommandUndoName = "Unset all inline style properties";

    readonly VisualElement Element;

    public UnsetAllInlineStylePropertiesCommand(VisualElement element)
    {
        Element = element;
    }

    public void Execute()
    {
        Assert.IsNotNull(Element);
        Assert.IsNotNull(Element.visualElementAsset);
        Assert.IsNotNull(Element.visualTreeAssetSource);

        var visualTreeAsset = Element.visualTreeAssetSource;
        Undo.RegisterCompleteObjectUndo(visualTreeAsset, CommandUndoName);

        var inlineStyleSheet = visualTreeAsset.inlineSheet;
        if (inlineStyleSheet == null)
            return;

        Undo.RegisterCompleteObjectUndo(inlineStyleSheet, CommandUndoName);

        var vea = Element.visualElementAsset;
        if (vea.ruleIndex < 0)
            return;

        var rule = inlineStyleSheet.rules[vea.ruleIndex];
        rule.ClearProperties();

        var clearAllStylePropertyBindingsCommand = new ClearAllStylePropertyBindingsCommand(Element);
        clearAllStylePropertyBindingsCommand.Execute();

        Element.UpdateInlineRule(inlineStyleSheet, rule);
        Element.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);

        EditorUtility.SetDirty(visualTreeAsset);
        EditorUtility.SetDirty(inlineStyleSheet);
    }
}
