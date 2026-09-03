// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class CopyElementsCommand : Command<CopyElementsCommand>
{
    const string CommandUndoName = "Copy elements";

    public VisualElementAsset[] ElementsToCopy { get; private set; }

    public override string UndoName => CommandUndoName;

    public static CopyElementsCommand GetPooled(object source, VisualElementAsset[] elements)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ElementsToCopy = elements;
        return cmd;
    }

    public static bool Validate(object source, VisualElementAsset[] elements)
    {
        using var cmd = GetPooled(source, elements);
        return cmd.Validate();
    }

    public static bool Validate(ReadOnlySpan<VisualElementAsset> elements)
    {
        if (elements.IsEmpty)
            return false;
        foreach (var element in elements)
        {
            if (element?.visualTreeAsset == null)
                return false;
        }
        return true;
    }

    public static CommandExecutionStatus Execute(object source, VisualElementAsset[] elements)
    {
        using var cmd = GetPooled(source, elements);
        return UICommandQueue.Execute(cmd).Status;
    }

    protected override void Init()
    {
        base.Init();
        ElementsToCopy = null;
    }

    public override bool Validate()
        => Validate(ElementsToCopy.AsSpan());

    public override CommandExecutionStatus Execute()
    {
        using var toCopyHandle = ListPool<UxmlAsset>.Get(out var toCopy);
        foreach (var element in ElementsToCopy)
            toCopy.Add(element);

        var content = VisualTreeAssetExporterUtility.ToUxmlString(toCopy);
        if (string.IsNullOrEmpty(content))
            return CommandExecutionStatus.ExecutionFailed;

        Clipboard.SystemCopyBuffer = content;
        Clipboard.GetClipboardForStage()?.ClearCutElements();
        return CommandExecutionStatus.Success;
    }
}
