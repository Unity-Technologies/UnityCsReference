// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    /// <summary>GridPalette stores settings for Palette assets when shown in the Palette window.</summary>
    public class GridPalette : ScriptableObject
    {
        /// <summary>Controls the sizing of cells for a Palette.</summary>
        public enum CellSizing
        {
            /// <summary>Automatically resizes the Palette cells by the size of Sprites in the Palette.</summary>
            Automatic = 0,
            /// <summary>Size of Palette cells will be changed manually by the user.</summary>
            Manual = 100
        }

        /// <summary>Determines the sizing of cells for a Palette.</summary>
        [SerializeField]
        public CellSizing cellSizing;
    }
}
