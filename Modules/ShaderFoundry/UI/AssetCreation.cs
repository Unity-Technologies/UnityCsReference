// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Scripting;

namespace UnityEditor.ShaderFoundry
{
    // This class sets up asset creation workflows.
    // For example, it adds the "Assets > Create > Shader > Lit Block Shader" option to the editor toolbar.
    internal static class AssetCreation
    {
        private const string kParentMenu = "Assets/Create/Shader";
        private const string kParentContextMenu = "CONTEXT/Create/Shader";
        private const string kNewLitShaderName = "NewLitShader.blockshader";
        private const string kURPPackageName = "com.unity.render-pipelines.universal";

        private static bool AssetCreationEnabled => FeatureControl.IsFoundryEnabled();

        // A callback of sorts necessary for the create-name-finalize workflow used when creating new assets.
        private class CreateBlockShaderAction : ProjectWindowCallback.AssetCreationEndAction
        {
            public override void Action(EntityId entityId, string pathName, string resourceFile)
            {
                FinalizeShaderAsset(pathName, resourceFile);
            }
        }

        // Registers menu items at startup.
        // Note that the primary reason we create the menu items this way (rather than using attributes)
        // is to control which options are generated at runtime.
        [InitializeOnLoadMethod]
        public static void Init()
        {
            void UpdateMenuItemsAtStartup()
            {
                Menu.menuChanged -= UpdateMenuItemsAtStartup;
                UpdateMenuItems();
            }
            // On project open, this event should be invoked
            Menu.menuChanged += UpdateMenuItemsAtStartup;
        }

        // Callback to add/remove toolbar menu and context menu entries.
        // Required by native code since it will be invoked when Shader Foundry is enabled or disabled.
        [RequiredByNativeCode]
        private static void UpdateMenuItems()
        {
            bool menusEnabled = AssetCreationEnabled;

            // This priority is meant to place the item at the bottom of the list of shader types in the menu.
            // I.e. after "Ray Tracing Shader", "Compute Shader", etc.
            const int kMinimumPriority = 6;
            void UpdateMenuItem(string path, int priority, System.Action callback)
            {
                if (menusEnabled)
                    Menu.AddMenuItem(path, string.Empty, false, priority, callback, () => true);
                else
                    Menu.RemoveMenuItem(path);
            }
            // Lit > Empty
            UpdateMenuItem($"{kParentMenu}/Lit Block Shader/Empty", kMinimumPriority, CreateEmptyLit);
            UpdateMenuItem($"{kParentContextMenu}/Lit Block Shader/Empty", kMinimumPriority, CreateEmptyLit);

            // Lit > Standard
            UpdateMenuItem($"{kParentMenu}/Lit Block Shader/Standard", kMinimumPriority + 1, CreateStandardLit);
            UpdateMenuItem($"{kParentContextMenu}/Lit Block Shader/Standard", kMinimumPriority + 1, CreateStandardLit);
        }

        // Begins the process of creating a new block shader which implements the 'Lit' interface
        // and contains the minimum amount of code needed for the shader to compile.
        private static void CreateEmptyLit()
        {
            // TODO @ SHADERS: Once we publicly support user-defined templates, we'll need a procedural route for
            // generating the BSL source code, which should replace the template approach here.
            CreateShaderFromTemplate(kNewLitShaderName, "LitEmpty.blockshader.template");
        }

        // Begins the process of creating a new block shader which implements the 'Lit' interface
        // and contains some simple shader functionality.
        private static void CreateStandardLit()
        {
            CreateShaderFromTemplate(kNewLitShaderName, "LitStandard.blockshader.template");
        }

        // A helper method for creating a block shader from a template file.
        private static void CreateShaderFromTemplate(string newFileName, string templateFileName)
        {
            string filePath = $"{ProjectWindowUtil.GetActiveFolderPath()}/{newFileName}";
            Texture2D icon = EditorGUIUtility.FindTexture(typeof(BlockShaderContainer));

            // Kick off the asset creation process
            CreateBlockShaderAction endAction = ScriptableObject.CreateInstance<CreateBlockShaderAction>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(EntityId.None, endAction, filePath, icon,
                templateFileName);
        }

        // Once a name for the new aset has been determined (given by atPath), this function locates the relevant
        // block shader template file (note that "template" here doesn't refer to the BSL construct) and finalizes
        // the creation of the new asset.
        private static void FinalizeShaderAsset(string atPath, string templateFileName)
        {
            // Load the shader template file
            string shaderTemplatePath = FindSRPBlockShaderTemplate(templateFileName);
            if (shaderTemplatePath == null)
                return;

            // Create the asset
            string cleanAssetPath = AssetDatabase.GenerateUniqueAssetPath(atPath);
            string phsyicalTemplatePath = FileUtil.GetPhysicalPath(shaderTemplatePath);
            Object shaderAsset = ProjectWindowUtil.CreateScriptAssetFromTemplate(cleanAssetPath, phsyicalTemplatePath);
            ProjectWindowUtil.ShowCreatedAsset(shaderAsset);
        }

        // Gets the path to an asset in an SRP package whose file name (+ extension) are the same as the given one.
        private static string FindSRPBlockShaderTemplate(string templateFileName)
        {
            // Register URP as a package to search
            List<string> packagesToSearch = new List<string>();
            if (PackageManager.PackageInfo.FindForPackageName(kURPPackageName) is PackageManager.PackageInfo packageInfo)
                packagesToSearch.Add(packageInfo.assetPath);

            if (packagesToSearch.Count > 0)
            {
                string nameFilter = System.IO.Path.GetFileNameWithoutExtension(templateFileName);

                // Search for a file of the given name in the given packages
                SearchFilter filter = new SearchFilter();
                filter.searchArea = SearchFilter.SearchArea.SelectedFolders;
                filter.folders = packagesToSearch.ToArray();
                filter.nameFilter = nameFilter;
                foreach (HierarchyIterator assetProperty in AssetDatabase.FindAllAssets(filter))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(assetProperty.assetGUID);

                    // ADB's search strings don't take into account file extension, so force an exact filename match
                    string assetName = System.IO.Path.GetFileName(assetPath);
                    if (assetName == templateFileName)
                        return assetPath;
                }
            }
            return null;
        }
    }
}
