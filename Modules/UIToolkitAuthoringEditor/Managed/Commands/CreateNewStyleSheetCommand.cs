// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct CreateNewStyleSheetCommand
{
    const string CommandUndoName = "Create new style sheet";

    readonly VisualTreeAsset VisualTreeAsset;
    readonly string UssPath;
    readonly int Index;

    public CreateNewStyleSheetCommand( VisualTreeAsset visualTreeAsset, string ussPath, int index = -1)
    {
        VisualTreeAsset = visualTreeAsset;
        UssPath = ussPath;
        Index = index;
    }

    public bool Execute()
    {
        if (!StyleSheetAssetUtilities.CreateNewUSSFile(UssPath))
            return false;

        var addCommand = new AddStyleSheetCommand(VisualTreeAsset, UssPath, Index);
        return addCommand.Execute();
    }
}
