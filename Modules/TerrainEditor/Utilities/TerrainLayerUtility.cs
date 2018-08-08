// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public static class TerrainLayerUtility
    {
        static bool s_ShowLayerEditor = false;

        static class Styles
        {
            public static GUIContent terrainLayers = EditorGUIUtility.TrTextContent("Terrain Layers");
            public static GUIContent btnEditTerrainLayers = EditorGUIUtility.TrTextContentWithIcon("Edit Terrain Layers...", "Allows adding / replacing or removing terrain layers", EditorGUIUtility.IconContent("SettingsIcon").image);
            public static GUIContent errNoLayersFound = EditorGUIUtility.TrTextContent("No terrain layers founds. You can create a new terrain layer using the Asset/Create/Terrain Layer menu command.");
        }
        public static int ShowTerrainLayersSelectionHelper(Terrain terrain, int activeTerrainLayer)
        {
            GUILayout.Label(Styles.terrainLayers, EditorStyles.boldLabel);
            GUI.changed = false;
            bool doubleClick;
            int selectedTerrainLayer = activeTerrainLayer;

            if (terrain.terrainData.terrainLayers.Length > 0)
            {
                TerrainLayer[] layers = terrain.terrainData.terrainLayers;
                Texture2D[] layerIcons = new Texture2D[layers.Length];
                for (int i = 0; i < layerIcons.Length; ++i)
                {
                    layerIcons[i] = (layers[i] == null || layers[i].diffuseTexture == null) ? EditorGUIUtility.whiteTexture : AssetPreview.GetAssetPreview(layers[i].diffuseTexture) ?? layers[i].diffuseTexture;
                }
                selectedTerrainLayer = TerrainInspector.AspectSelectionGrid(activeTerrainLayer, layerIcons, 64, new GUIStyle("GridList"), Styles.errNoLayersFound, out doubleClick);
            }
            else
                selectedTerrainLayer = -1;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // menu button
            Rect r = GUILayoutUtility.GetRect(Styles.btnEditTerrainLayers, new GUIStyle("Button"));
            if (GUI.Button(r, Styles.btnEditTerrainLayers, new GUIStyle("Button")))
            {
                MenuCommand context = new MenuCommand(terrain, selectedTerrainLayer);
                EditorUtility.DisplayPopupMenu(new Rect(r.x, r.y, 0, 0), "CONTEXT /TerrainLayers", context);
            }
            GUILayout.EndHorizontal();

            if (selectedTerrainLayer != -1 && terrain)
            {
                TerrainLayer layer = terrain.terrainData.terrainLayers[selectedTerrainLayer];
                if (layer != null)
                {
                    Editor selectedTerrainLayerEditor = Editor.CreateEditor(layer);

                    Rect titleRect = Editor.DrawHeaderGUI(selectedTerrainLayerEditor, layer.name, 10f);
                    int id = GUIUtility.GetControlID(67890, FocusType.Passive);

                    Rect renderRect = EditorGUI.GetInspectorTitleBarObjectFoldoutRenderRect(titleRect);
                    renderRect.y = titleRect.yMax - 17f; // align with bottom
                    UnityEngine.Object[] targets = { layer };
                    bool newVisible = EditorGUI.DoObjectFoldout(s_ShowLayerEditor, titleRect, renderRect, targets, id);

                    // Toggle visibility
                    if (newVisible != s_ShowLayerEditor)
                    {
                        s_ShowLayerEditor = newVisible;
                        InternalEditorUtility.SetIsInspectorExpanded(layer, newVisible);
                    }
                    if (s_ShowLayerEditor)
                        selectedTerrainLayerEditor.OnInspectorGUI();
                }
            }

            return selectedTerrainLayer;
        }

        internal static int AddTerrainLayer(Terrain terrain, TerrainLayer inputLayer)
        {
            var terrainData = terrain.terrainData;
            var layers = terrainData.terrainLayers;
            for (var idx = 0; idx < layers.Length; ++idx)
            {
                if (layers[idx] == inputLayer)
                    return idx;
            }

            Undo.RegisterCompleteObjectUndo(terrainData, "Add terrain layer");

            int newIndex = layers.Length;
            var newarray = new TerrainLayer[newIndex + 1];
            Array.Copy(layers, 0, newarray, 0, newIndex);
            newarray[newIndex] = inputLayer;
            terrainData.terrainLayers = newarray;
            EditorUtility.SetDirty(terrain);
            return newIndex;
        }

        internal static int ReplaceTerrainLayer(Terrain terrain, int index, TerrainLayer inputLayer)
        {
            var layers = terrain.terrainData.terrainLayers;
            // Make sure the selection is legit
            if (index < 0 || index > layers.Length)
                return index;
            // See if they're already using this layer
            for (var idx = 0; idx < layers.Length; ++idx)
            {
                if (layers[idx] == inputLayer)
                    return idx;
            }

            Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Add terrain layer");

            layers[index] = inputLayer;
            EditorUtility.SetDirty(terrain);
            terrain.terrainData.terrainLayers = layers;
            return index;
        }

        internal static void RemoveTerrainLayer(Terrain terrain, int index)
        {
            var terrainData = terrain.terrainData;
            Undo.RegisterCompleteObjectUndo(terrainData, "Remove terrain layer");

            int width = terrainData.alphamapWidth;
            int height = terrainData.alphamapHeight;
            float[,,] alphamap = terrainData.GetAlphamaps(0, 0, width, height);
            int alphaCount = alphamap.GetLength(2);

            int newAlphaCount = alphaCount - 1;
            float[,,] newalphamap = new float[height, width, newAlphaCount];

            // move further alphamaps one index below
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    for (int a = 0; a < index; ++a)
                        newalphamap[y, x, a] = alphamap[y, x, a];
                    for (int a = index + 1; a < alphaCount; ++a)
                        newalphamap[y, x, a - 1] = alphamap[y, x, a];
                }
            }

            // normalize weights in new alpha map
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float sum = 0.0F;
                    for (int a = 0; a < newAlphaCount; ++a)
                        sum += newalphamap[y, x, a];
                    if (sum >= 0.01)
                    {
                        float multiplier = 1.0F / sum;
                        for (int a = 0; a < newAlphaCount; ++a)
                            newalphamap[y, x, a] *= multiplier;
                    }
                    else
                    {
                        // in case all weights sum to pretty much zero (e.g.
                        // removing splat that had 100% weight), assign
                        // everything to 1st splat texture (just like
                        // initial terrain).
                        for (int a = 0; a < newAlphaCount; ++a)
                            newalphamap[y, x, a] = (a == 0) ? 1.0f : 0.0f;
                    }
                }
            }

            // remove splat from terrain prototypes
            var layers = terrainData.terrainLayers;
            var newSplats = new TerrainLayer[layers.Length - 1];
            for (int a = 0; a < index; ++a)
                newSplats[a] = layers[a];
            for (int a = index + 1; a < alphaCount; ++a)
                newSplats[a - 1] = layers[a];
            terrainData.terrainLayers = newSplats;

            // set new alphamaps
            terrainData.SetAlphamaps(0, 0, newalphamap);
        }
    }

    internal class TerrainLayersContextMenus
    {
        [MenuItem("CONTEXT/TerrainLayers/Create Layer...")]
        internal static void CreateLayer(MenuCommand item)
        {
            ObjectSelector.get.Show(null, typeof(Texture2D), null, false, null,
                selection =>
                {
                    if (selection == null)
                        return;

                    var layerName = AssetDatabase.GenerateUniqueAssetPath(
                        Path.Combine(ProjectWindowUtil.GetActiveFolderPath(), "NewLayer.terrainlayer"));
                    var terrain = (Terrain)item.context;
                    var layer = new TerrainLayer();
                    AssetDatabase.CreateAsset(layer, layerName);
                    TerrainLayerUtility.AddTerrainLayer(terrain, layer);
                    layer.diffuseTexture = (Texture2D)selection;
                }, null);
        }

        [MenuItem("CONTEXT/TerrainLayers/Add Layer...")]
        internal static void AddLayer(MenuCommand item)
        {
            var terrain = (Terrain)item.context;
            ObjectSelector.get.Show(null, typeof(TerrainLayer), null, false, null,
                selection => { TerrainLayerUtility.AddTerrainLayer(terrain, (TerrainLayer)selection); }, null);
        }

        [MenuItem("CONTEXT/TerrainLayers/Replace Layer...")]
        internal static void ReplaceLayer(MenuCommand item)
        {
            var terrain = (Terrain)item.context;
            var layer = terrain.terrainData.terrainLayers[(int)item.userData];
            ObjectSelector.get.Show(layer, typeof(TerrainLayer), null, false, null, null,
                selection => { TerrainLayerUtility.ReplaceTerrainLayer(terrain, (int)item.userData, (TerrainLayer)selection); });
        }

        [MenuItem("CONTEXT/TerrainLayers/Replace Layer...", true)]
        internal static bool ReplaceLayerCheck(MenuCommand item)
        {
            var terrain = (Terrain)item.context;
            return item.userData >= 0 && item.userData < terrain.terrainData.terrainLayers.Length;
        }

        [MenuItem("CONTEXT/TerrainLayers/Remove layer")]
        internal static void RemoveSplat(MenuCommand item)
        {
            var terrain = (Terrain)item.context;
            TerrainLayerUtility.RemoveTerrainLayer(terrain, item.userData);
        }

        [MenuItem("CONTEXT/TerrainLayers/Remove layer", true)]
        internal static bool RemoveSplatCheck(MenuCommand item)
        {
            var terrain = (Terrain)item.context;
            return item.userData >= 0 && item.userData < terrain.terrainData.terrainLayers.Length;
        }
    }
}
