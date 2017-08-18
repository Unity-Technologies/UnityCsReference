// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeType(Header = "Modules/Grid/Public/Grid.h")]
    public sealed partial class Grid : GridLayout
    {
        public new Vector3 cellSize
        {
            get { return GetCellSize(this); }
            set { SetCellSize(this, value); }
        }

        [FreeFunction("GridBindings::GetCellSize")]
        private extern static Vector3 GetCellSize(Grid self);

        [FreeFunction("GridBindings::SetCellSize")]
        private extern static void SetCellSize(Grid self, Vector3 value);

        public new Vector3 cellGap
        {
            get { return GetCellGap(this); }
            set { SetCellGap(this, value); }
        }

        [FreeFunction("GridBindings::GetCellGap")]
        private extern static Vector3 GetCellGap(Grid self);

        [FreeFunction("GridBindings::SetCellGap")]
        private extern static void SetCellGap(Grid self, Vector3 value);

        public new extern GridLayout.CellLayout cellLayout
        {
            get;
            set;
        }

        public new extern GridLayout.CellSwizzle cellSwizzle
        {
            get;
            set;
        }

        [FreeFunction("GridBindings::CellSwizzle")]
        public extern static Vector3 Swizzle(GridLayout.CellSwizzle swizzle, Vector3 position);

        [FreeFunction("GridBindings::InverseCellSwizzle")]
        public extern static Vector3 InverseSwizzle(GridLayout.CellSwizzle swizzle, Vector3 position);
    }
}
