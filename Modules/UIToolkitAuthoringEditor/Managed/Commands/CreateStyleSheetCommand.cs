// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct CreateStyleSheetCommand
{
    const string CommandUndoName = "Create style sheet";

    readonly VisualTreeAsset VisualTreeAsset;
    readonly string UssPath;
    readonly int Index;

    public CreateStyleSheetCommand(VisualTreeAsset visualTreeAsset, string ussPath, int index = -1)
    {
        VisualTreeAsset = visualTreeAsset;
        UssPath = ussPath;
        Index = index;
    }

    public void Execute()
    {
        if (!StyleSheetAssetUtilities.CreateNewUSSFile(UssPath))
            return;

        var addCommand = new AddStyleSheetCommand(VisualTreeAsset, UssPath, Index);
        addCommand.Execute();
    }
}
