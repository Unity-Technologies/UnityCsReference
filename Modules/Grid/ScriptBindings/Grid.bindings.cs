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
        public new extern Vector3 cellSize
        {
            [FreeFunction("GridBindings::GetCellSize", HasExplicitThis = true)]
            get;
            [FreeFunction("GridBindings::SetCellSize", HasExplicitThis = true)]
            set;
        }

        public new extern Vector3 cellGap
        {
            [FreeFunction("GridBindings::GetCellGap", HasExplicitThis = true)]
            get;
            [FreeFunction("GridBindings::SetCellGap", HasExplicitThis = true)]
            set;
        }

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
