// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class AddStyleSheetCommand : Command<AddStyleSheetCommand>
{
    const string CommandUndoName = "Add style sheet";

    public static AddStyleSheetCommand GetPooled(object source, VisualTreeAsset visualTreeAsset, string ussPath, int index = -1)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.VisualTreeAsset = visualTreeAsset;
        cmd.UssPath = ussPath;
        cmd.Index = index;
        return cmd;
    }

    public static void Execute(object source, VisualTreeAsset visualTreeAsset, string ussPath, int index = -1)
    {
        using var command = GetPooled(source, visualTreeAsset, ussPath, index);
        UICommandQueue.Execute(command);
    }

    public VisualTreeAsset VisualTreeAsset { get; private set; }
    public string UssPath { get; private set; }
    public int Index { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext | CommandCategory.Hierarchy;

    protected override void Init()
    {
        base.Init();
        VisualTreeAsset = null;
        UssPath = null;
        Index = -1;
    }

    public override bool Validate() => !string.IsNullOrEmpty(UssPath) && VisualTreeAsset != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(VisualTreeAsset);
    }

    public override CommandExecutionStatus Execute()
    {
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
        if (styleSheet.importedWithErrors)
        {
            var errorMessage = $"The USS file at path {UssPath} has import errors and cannot be used.";
            EditorUtility.DisplayDialog("Invalid USS File", errorMessage, "OK");
            return CommandExecutionStatus.ExecutionFailed;
        }

        var rootElement = VisualTreeAsset.visualTreeNoAlloc;
        if (rootElement == null)
            return CommandExecutionStatus.Success;

        rootElement.stylesheets ??= [];
        var actualIndex = Index == -1 ? rootElement.stylesheets.Count : Index;

        if (actualIndex < 0 || actualIndex > rootElement.stylesheets.Count)
        {
            Debug.LogWarning($"Invalid index {Index}. Must be -1 (append) or between 0 and {rootElement.stylesheets.Count}.");
            return CommandExecutionStatus.ExecutionFailed;
        }

        rootElement.stylesheets.Insert(actualIndex, styleSheet);
        return CommandExecutionStatus.Success;
    }
}
