// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.Commands.Selection;
using Unity.Timeline.Foundation.Common;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal class SelectionBehaviour : ISelectionBehaviour
    {
        public void ClearSelection(ISequenceViewModel vm)
        {
            vm.Dispatch(new ClearSelection());
        }

        public void ToggleSelection(ISequenceViewModel vm, IEnumerable<UniqueID> ids)
        {
            vm.Dispatch(new ToggleSelection([.. ids]));
        }

        public void Select(ISequenceViewModel vm, IEnumerable<UniqueID> ids)
        {
            vm.Dispatch(new Select([.. ids]));
        }

        public void Select(ISequenceViewModel vm, UniqueID id, ISelectionBehaviour.Location location = ISelectionBehaviour.Location.None)
        {
            var itemData = vm.GetData<SequenceData>().GetItemFromId(id);
            vm.Dispatch(itemData.type != Item.Type.Invalid ? PrepareSelectCommand(itemData, location) : new Select(id));
        }

        public Select PrepareSelectCommand(Item item, ISelectionBehaviour.Location location)
        {
            if (item.type == Item.Type.Clip)
            {
                switch (location)
                {
                    case ISelectionBehaviour.Location.Start:
                        var previous = item.Previous();
                        if (previous.type == Item.Type.Transition)
                        {
                            return new Select(item.PreviousClip().ID);
                        }

                        break;
                    case ISelectionBehaviour.Location.End:
                        var next = item.Next();
                        if (next.type == Item.Type.Transition)
                        {
                            return new Select(item.NextClip().ID);
                        }

                        break;
                    case ISelectionBehaviour.Location.None:
                    default:
                        break;
                }
            }

            return new Select(item.ID);
        }

        public void Deselect(ISequenceViewModel vm, IEnumerable<UniqueID> ids)
        {
            vm.Dispatch(new Deselect([.. ids]));
        }
    }
}
