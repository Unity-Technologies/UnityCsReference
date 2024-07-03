// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor.TerrainTools;

namespace UnityEditor
{
    internal class TerrainMenus
    {
        [MenuItem("GameObject/3D Object/Terrain", false, 3000)]
        static void CreateTerrain(MenuCommand menuCommand)
        {
            // Create the storage for the terrain in the project
            // (So we can reuse it in multiple scenes)
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(1000, 600, 1000);
            terrainData.baseMapResolution = 1024;
            terrainData.SetDetailResolution(1024, terrainData.detailResolutionPerPatch);

            AssetDatabase.CreateAsset(terrainData, AssetDatabase.GenerateUniqueAssetPath("Assets/New Terrain.asset"));
            var parent = menuCommand.context as GameObject;
            GameObject terrain = Terrain.CreateTerrainGameObject(terrainData);
            terrain.name = "Terrain";

            GameObjectUtility.SetDefaultParentForNewObject(terrain, parent?.transform, true);
            StageUtility.PlaceGameObjectInCurrentStage(terrain);
            GameObjectUtility.EnsureUniqueNameForSibling(terrain);
            Selection.activeObject = terrain;
            Undo.RegisterCreatedObjectUndo(terrain, "Create terrain");
        }

        internal static void ImportRaw()
        {
            string saveLocation = EditorUtility.OpenFilePanel("Import Raw Heightmap", "", "raw");
            if (saveLocation != "")
            {
                ImportRawHeightmap wizard = TerrainWizard.DisplayTerrainWizard<ImportRawHeightmap>("Import Heightmap", "Import");
                wizard.InitializeImportRaw(GetActiveTerrain(), saveLocation);
            }
        }

        internal static void ExportHeightmapRaw()
        {
            ExportRawHeightmap wizard = TerrainWizard.DisplayTerrainWizard<ExportRawHeightmap>("Export Heightmap", "Export");
            wizard.ResetDefaults(GetActiveTerrain());
        }

        internal static void MassPlaceTrees()
        {
            PlaceTreeWizard wizard = TerrainWizard.DisplayTerrainWizard<PlaceTreeWizard>("Place Trees", "Place");
            wizard.ResetDefaults(GetActiveTerrain());
        }

        internal static void Flatten()
        {
            FlattenHeightmap wizard = TerrainWizard.DisplayTerrainWizard<FlattenHeightmap>("Flatten Heightmap", "Flatten");
            wizard.ResetDefaults(GetActiveTerrain());
        }

        internal static void RefreshPrototypes()
        {
            GetActiveTerrainData().RefreshPrototypes();
            GetActiveTerrain().Flush();
            EditorApplication.SetSceneRepaintDirty();
        }

        static Terrain GetActiveTerrain()
        {
            Object[] selection = Selection.GetFiltered(typeof(Terrain), SelectionMode.Editable);

            if (selection.Length != 0)
                return selection[0] as Terrain;
            else
                return Terrain.activeTerrain;
        }

        static TerrainData GetActiveTerrainData()
        {
            if (GetActiveTerrain())
                return GetActiveTerrain().terrainData;
            else
                return null;
        }
    }

    class TerrainDetailContextMenus
    {
        [MenuItem("CONTEXT/TerrainEngineDetails/Add Detail Mesh", secondaryPriority = 20)]
        static internal void AddDetailMesh(MenuCommand item)
        {
            TerrainWizard.DisplayTerrainWizard<TerrainDetailMeshWizard>("Add Detail Mesh", "Add").ResetDefaults((Terrain)item.context, -1);
        }

        [MenuItem("CONTEXT/TerrainEngineDetails/Add Grass Texture", secondaryPriority = 21)]
        static internal void AddDetailTexture(MenuCommand item)
        {
            TerrainWizard.DisplayTerrainWizard<TerrainDetailTextureWizard>("Add Grass Texture", "Add").ResetDefaults((Terrain)item.context, -1);
        }

        [MenuItem("CONTEXT/TerrainEngineDetails/Add Grass Texture", validate = true)]
        static internal bool AddDetailTextureValidate(MenuCommand item)
        {
            return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null
                || UnityEngine.Rendering.GraphicsSettings.GetDefaultShader(UnityEngine.Rendering.DefaultShaderType.TerrainDetailGrassBillboard) != null
                || UnityEngine.Rendering.GraphicsSettings.GetDefaultShader(UnityEngine.Rendering.DefaultShaderType.TerrainDetailGrass) != null;
        }

        [MenuItem("CONTEXT/TerrainEngineDetails/Edit", secondaryPriority = 22)]
        static internal void EditDetail(MenuCommand item)
        {
            Terrain terrain = (Terrain)item.context;
            DetailPrototype prototype = terrain.terrainData.detailPrototypes[item.userData];

            if (prototype.usePrototypeMesh)
            {
                TerrainWizard.DisplayTerrainWizard<TerrainDetailMeshWizard>("Edit Detail Mesh", "Apply").ResetDefaults((Terrain)item.context, item.userData);
            }
            else
            {
                TerrainWizard.DisplayTerrainWizard<TerrainDetailTextureWizard>("Edit Grass Texture", "Apply").ResetDefaults((Terrain)item.context, item.userData);
            }
        }

        [MenuItem("CONTEXT/TerrainEngineDetails/Edit", validate = true)]
        static internal bool EditDetailCheck(MenuCommand item)
        {
            Terrain terrain = (Terrain)item.context;
            return item.userData >= 0 && item.userData < terrain.terrainData.detailPrototypes.Length;
        }

        [MenuItem("CONTEXT/TerrainEngineDetails/Remove", secondaryPriority = 23)]
        static internal void RemoveDetail(MenuCommand item)
        {
            Terrain terrain = (Terrain)item.context;
            TerrainEditorUtility.RemoveDetail(terrain, item.userData);
        }

        [MenuItem("CONTEXT/TerrainEngineDetails/Remove", validate = true)]
        static internal bool RemoveDetailCheck(MenuCommand item)
        {
            Terrain terrain = (Terrain)item.context;
            return item.userData >= 0 && item.userData < terrain.terrainData.detailPrototypes.Length;
        }
    }
    class TerrainTreeContextMenus
    {
        [MenuItem("CONTEXT/TerrainEngineTrees/Add Tree", secondaryPriority = 24)]
        static internal void AddTree(MenuCommand item)
        {
            TreeWizard wizard = TerrainWizard.DisplayTerrainWizard<TreeWizard>("Add Tree", "Add");
            wizard.InitializeDefaults((Terrain)item.context, -1);
        }

        [MenuItem("CONTEXT/TerrainEngineTrees/Edit Tree", secondaryPriority = 25)]
        static internal void EditTree(MenuCommand item)
        {
            TreeWizard wizard = TerrainWizard.DisplayTerrainWizard<TreeWizard>("Edit Tree", "Apply");
            wizard.InitializeDefaults((Terrain)item.context, item.userData);
        }

        [MenuItem("CONTEXT/TerrainEngineTrees/Edit Tree", validate = true)]
        static internal bool EditTreeCheck(MenuCommand item)
        {
            var paintTrees = EditorTools.EditorToolManager.GetActiveTool() as PaintTreesTool;
            Debug.Assert(paintTrees != null, "Attempting to render PaintTreesTools context menu content but the active tool is not the PaintTreesTool");
            return paintTrees.selectedTree >= 0;
        }

        [MenuItem("CONTEXT/TerrainEngineTrees/Remove Tree", secondaryPriority = 26)]
        static internal void RemoveTree(MenuCommand item)
        {
            Terrain terrain = (Terrain)item.context;
            TerrainEditorUtility.RemoveTree(terrain, item.userData);
        }

        [MenuItem("CONTEXT/TerrainEngineTrees/Remove Tree", validate = true)]
        static internal bool RemoveTreeCheck(MenuCommand item)
        {
            var paintTrees = EditorTools.EditorToolManager.GetActiveTool() as PaintTreesTool;
            Debug.Assert(paintTrees != null, "Attempting to render PaintTreesTools context menu content but the active tool is not the PaintTreesTool");
            return paintTrees.selectedTree >= 0;
        }
    }

    /*
        [MenuItem ("Terrain/Import Heightmap - Texture...")]
        static void ImportHeightmap () {
            ImportTextureHeightmap wizard = ScriptableWizard.DisplayWizard<ImportTextureHeightmap>("Import Heightmap", "Import");
            wizard.InitializeDefaults(GetActiveTerrain());
        }
    */
} //namespace
