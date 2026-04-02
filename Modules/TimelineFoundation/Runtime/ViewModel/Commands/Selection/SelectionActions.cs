// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.CSO;

namespace Unity.Timeline.Foundation.Commands.Selection
{
    /// <summary>
    /// Removes all items from selection.
    /// </summary>
    readonly struct ClearSelection : ICommand { }

    /// <summary>
    /// Toggles the selection state from an array of items.
    /// </summary>
    readonly struct ToggleSelection : ICommand
    {
        public readonly UniqueID[] Ids;

        public ToggleSelection(params UniqueID[] ids) => Ids = ids;
    }

    /// <summary>
    /// Sets the selection state from an array of items to Selected.
    /// </summary>
    readonly struct Select : ICommand
    {
        public readonly UniqueID[] Ids;

        public Select(params UniqueID[] ids) => Ids = ids;
    }

    /// <summary>
    /// Sets the selection state from an array of items to Deselected.
    /// </summary>
    readonly struct Deselect : ICommand
    {
        public readonly UniqueID[] Ids;

        public Deselect(params UniqueID[] ids) => Ids = ids;
    }
}
