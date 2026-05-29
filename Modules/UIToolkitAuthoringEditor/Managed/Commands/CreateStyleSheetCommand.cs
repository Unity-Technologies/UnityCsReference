// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class CreateStyleSheetCommand : Command<CreateStyleSheetCommand>
{
    const string CommandUndoName = "Create style sheet";

    public static CreateStyleSheetCommand GetPooled(object source, VisualTreeAsset vta, string ussPath, int index = -1)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.VisualTreeAsset = vta;
        cmd.UssPath = ussPath;
        cmd.Index = index;
        return cmd;
    }

    public VisualTreeAsset VisualTreeAsset { get; private set; }
    public string UssPath { get; private set; }
    public int Index { get; private set; }

    public override string UndoName => CommandUndoName;

    protected override void Init()
    {
        VisualTreeAsset = null;
        UssPath = null;
        Index = int.MinValue;
        base.Init();
    }

    public override bool Validate() => true;

    public override CommandExecutionStatus Execute()
    {
        if (!StyleSheetAssetUtilities.CreateNewUSSFile(UssPath))
            return CommandExecutionStatus.ExecutionFailed;

        var addCommand = new AddStyleSheetCommand(VisualTreeAsset, UssPath, Index);
        addCommand.Execute();
        return CommandExecutionStatus.Success;
    }
}
