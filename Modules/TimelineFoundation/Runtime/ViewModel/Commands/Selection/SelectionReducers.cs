// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.Commands.Selection
{
    static class Reducers
    {
        public static void RegisterAll(ViewModelBase viewModel)
        {
            viewModel.RegisterCommandHandler<SelectionComponent, ClearSelection>(ClearSelectionReducer);
            viewModel.RegisterCommandHandler<SelectionComponent, ToggleSelection>(ToggleReducer);
            viewModel.RegisterCommandHandler<SelectionComponent, Select>(SelectReducer);
            viewModel.RegisterCommandHandler<SelectionComponent, Deselect>(DeselectReducer);
        }

        static void ClearSelectionReducer(SelectionComponent selectionComponent, ClearSelection action)
        {
            using (selectionComponent.UpdateScope())
            {
                selectionComponent.ChangeSelection(new List<UniqueID>());
            }
        }

        static void ToggleReducer(SelectionComponent selectionComponent, ToggleSelection action)
        {
            using (selectionComponent.UpdateScope())
            {
                selectionComponent.ToggleSelection(action.Ids);
            }
        }

        static void SelectReducer(SelectionComponent selectionComponent, Select action)
        {
            using (selectionComponent.UpdateScope())
            {
                selectionComponent.Select(action.Ids);
            }
        }

        static void DeselectReducer(SelectionComponent selectionComponent, Deselect action)
        {
            using (selectionComponent.UpdateScope())
            {
                selectionComponent.Deselect(action.Ids);
            }
        }
    }
}
