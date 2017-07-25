// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    internal class GridPaintTargetsDropdown : FlexibleMenu
    {
        public GridPaintTargetsDropdown(IFlexibleMenuItemProvider itemProvider, int selectionIndex, FlexibleMenuModifyItemUI modifyItemUi, Action<int, object> itemClickedCallback, float minWidth)
            : base(itemProvider, selectionIndex, modifyItemUi, itemClickedCallback)
        {
            minTextWidth = minWidth;
        }

        internal class MenuItemProvider : IFlexibleMenuItemProvider
        {
            public int Count()
            {
                return GridPaintingState.validTargets != null ? GridPaintingState.validTargets.Length : 0;
            }

            public object GetItem(int index)
            {
                return GridPaintingState.validTargets != null ? GridPaintingState.validTargets[index] : GridPaintingState.scenePaintTarget;
            }

            public int Add(object obj)
            {
                throw new NotImplementedException();
            }

            public void Replace(int index, object newPresetObject)
            {
                throw new NotImplementedException();
            }

            public void Remove(int index)
            {
                throw new NotImplementedException();
            }

            public object Create()
            {
                throw new NotImplementedException();
            }

            public void Move(int index, int destIndex, bool insertAfterDestIndex)
            {
                throw new NotImplementedException();
            }

            public string GetName(int index)
            {
                return GridPaintingState.validTargets != null ? GridPaintingState.validTargets[index].name : GridPaintingState.scenePaintTarget.name;
            }

            public bool IsModificationAllowed(int index)
            {
                return false;
            }

            public int[] GetSeperatorIndices()
            {
                return new int[0];
            }
        }
    }
}
