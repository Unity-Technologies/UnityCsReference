// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

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
    }

    internal class TerrainLayersContextMenus
    {
        [MenuItem("CONTEXT/TerrainLayers/Add Layer...")]
        static internal void AddLayer(MenuCommand item)
        {
            TerrainLayerSelectionWindow.ShowTerrainLayerListEditor("Add Terrain Layer", (Terrain)item.context, -1);
        }

        [MenuItem("CONTEXT/TerrainLayers/Replace Layer...")]
        static internal void ReplaceLayer(MenuCommand item)
        {
            TerrainLayerSelectionWindow.ShowTerrainLayerListEditor("Replace Terrain Layer", (Terrain)item.context, item.userData);
        }

        [MenuItem("CONTEXT/TerrainLayers/Remove layer")]
        static internal void RemoveSplat(MenuCommand item)
        {
            Terrain terrain = (Terrain)item.context;
            RemoveTerrainLayer(terrain.terrainData, item.userData);
        }

        [MenuItem("CONTEXT/TerrainLayers/Remove layer", true)]
        static internal bool RemoveSplatCheck(MenuCommand item)
        {
            Terrain terrain = (Terrain)item.context;
            return item.userData >= 0 && item.userData < terrain.terrainData.terrainLayers.Length;
        }

        //

        internal static void RemoveTerrainLayer(TerrainData terrainData, int index)
        {
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
            TerrainLayer[] layers = terrainData.terrainLayers;
            TerrainLayer[] newSplats = new TerrainLayer[layers.Length - 1];
            for (int a = 0; a < index; ++a)
                newSplats[a] = layers[a];
            for (int a = index + 1; a < alphaCount; ++a)
                newSplats[a - 1] = layers[a];
            terrainData.terrainLayers = newSplats;

            // set new alphamaps
            terrainData.SetAlphamaps(0, 0, newalphamap);
        }
    }
}
