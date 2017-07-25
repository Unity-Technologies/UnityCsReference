// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    internal class GridPalettesDropdown : FlexibleMenu
    {
        public GridPalettesDropdown(IFlexibleMenuItemProvider itemProvider, int selectionIndex, FlexibleMenuModifyItemUI modifyItemUi, Action<int, object> itemClickedCallback, float minWidth)
            : base(itemProvider, selectionIndex, modifyItemUi, itemClickedCallback)
        {
            minTextWidth = minWidth;
        }

        internal class MenuItemProvider : IFlexibleMenuItemProvider
        {
            public int Count()
            {
                return GridPalettes.palettes.Count + 1;
            }

            public object GetItem(int index)
            {
                if (index < GridPalettes.palettes.Count)
                    return GridPalettes.palettes[index];

                return null;
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
                if (index < GridPalettes.palettes.Count)
                    return GridPalettes.palettes[index].name;
                else if (index == GridPalettes.palettes.Count)
                    return "Create New Palette";
                else
                    return "";
            }

            public bool IsModificationAllowed(int index)
            {
                return false;
            }

            public int[] GetSeperatorIndices()
            {
                return new int[] { GridPalettes.palettes.Count - 1 };
            }
        }
    }
}
