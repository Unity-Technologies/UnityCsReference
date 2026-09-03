// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class CutElementsCommand : Command<CutElementsCommand>
{
    public VisualElementAsset[] ElementsToCut { get; private set; }

    public static CutElementsCommand GetPooled(object source, VisualElementAsset[] elements)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ElementsToCut = elements;
        return cmd;
    }

    public static bool Validate(object source, VisualElementAsset[] elements)
    {
        using var cmd = GetPooled(source, elements);
        return cmd.Validate();
    }

    public static bool Validate(object source, ReadOnlySpan<VisualElementAsset> elements)
    {
        if (Clipboard.GetClipboardForStage() == null)
            return false;
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
        ElementsToCut = null;
    }

    public override bool Validate()
        => Validate(Source, ElementsToCut.AsSpan());

    public override CommandExecutionStatus Execute()
    {
        using var toCopyHandle = ListPool<UxmlAsset>.Get(out var toCopy);
        foreach (var element in ElementsToCut)
            toCopy.Add(element);

        var content = VisualTreeAssetExporterUtility.ToUxmlString(toCopy);
        if (string.IsNullOrEmpty(content))
            return CommandExecutionStatus.ExecutionFailed;

        Clipboard.SystemCopyBuffer = content;
        Clipboard.GetClipboardForStage().SetCutElements(ElementsToCut);
        return CommandExecutionStatus.Success;
    }
}
