// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [HelpURL("tilemaps/tile-palettes/new-tile-palette-reference")]
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

        [SerializeField]
        private TransparencySortMode m_TransparencySortMode = TransparencySortMode.Default;

        [SerializeField]
        private Vector3 m_TransparencySortAxis = new Vector3(0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Determines the transparency sorting mode of renderers in the Palette
        /// </summary>
        public TransparencySortMode transparencySortMode
        {
            get => m_TransparencySortMode;
            set => m_TransparencySortMode = value;
        }

        /// <summary>
        /// Determines the sorting axis if the transparency sort mode is set to Custom Axis Sort
        /// </summary>
        public Vector3 transparencySortAxis
        {
            get => m_TransparencySortAxis;
            set =>  m_TransparencySortAxis = value;
        }
    }
}
