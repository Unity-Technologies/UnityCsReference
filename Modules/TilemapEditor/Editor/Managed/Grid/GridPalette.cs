// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public class GridPalette : ScriptableObject
    {
        public enum CellSizing { Automatic = 0, Manual = 100 }
        [SerializeField]
        public CellSizing cellSizing;
    }
}
