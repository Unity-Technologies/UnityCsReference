// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace UnityEditorInternal
{
    internal class TileUtility
    {
        [MenuItem("Assets/Create/Tile", priority = 357)]
        public static void CreateNewTile()
        {
            string message = string.Format("Save tile'{0}':", "tile");
            string newAssetPath = EditorUtility.SaveFilePanelInProject("Save tile", "New Tile", "asset", message, ProjectWindowUtil.GetActiveFolderPath());

            // If user canceled or save path is invalid, we can't create the tile
            if (string.IsNullOrEmpty(newAssetPath))
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<Tile>(), newAssetPath);
        }
    }
}
