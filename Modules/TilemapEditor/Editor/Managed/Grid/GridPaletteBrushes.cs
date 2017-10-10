// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class GridPaletteBrushes : ScriptableSingleton<GridPaletteBrushes>
    {
        private static readonly string s_LibraryPath = "Library/GridBrush";
        private static readonly string s_GridBrushExtension = ".asset";

        private static bool s_RefreshCache;
        [SerializeField] private List<GridBrushBase> m_Brushes;

        public static List<GridBrushBase> brushes
        {
            get
            {
                if (instance.m_Brushes == null || instance.m_Brushes.Count == 0 || s_RefreshCache)
                {
                    instance.RefreshBrushesCache();
                    s_RefreshCache = false;
                }

                return instance.m_Brushes;
            }
        }
        private string[] m_BrushNames;

        public static string[] brushNames
        {
            get
            {
                return instance.m_BrushNames;
            }
        }

        public static System.Type GetDefaultBrushType()
        {
            System.Type defaultType = typeof(GridBrush);
            var editorAssemblies = EditorAssemblies.loadedAssemblies;
            int count = 0;
            for (int i = editorAssemblies.Length - 1; i >= 0; i--)
            {
                Type[] types = AssemblyHelper.GetTypesFromAssembly(editorAssemblies[i]);
                foreach (var type in types)
                {
                    CustomGridBrushAttribute[] attrs = type.GetCustomAttributes(typeof(CustomGridBrushAttribute), false) as CustomGridBrushAttribute[];
                    if (attrs != null && attrs.Length > 0)
                    {
                        if (attrs[0].defaultBrush)
                        {
                            defaultType = type;
                            count++;
                        }
                    }
                }
            }
            if (count > 1)
            {
                Debug.LogWarning("Multiple occurrences of defaultBrush == true found. It should only be declared once.");
            }
            return defaultType;
        }

        public static void ActiveGridBrushAssetChanged()
        {
            if (GridPaintingState.gridBrush == null)
                return;

            if (IsLibraryBrush(GridPaintingState.gridBrush))
            {
                instance.SaveLibraryGridBrushAsset(GridPaintingState.gridBrush);
            }
        }

        private void RefreshBrushesCache()
        {
            if (m_Brushes == null)
                m_Brushes = new List<GridBrushBase>();

            GridBrushBase defaultBrush = null;
            if (m_Brushes.Count == 0 || !(m_Brushes[0] is GridBrush))
            {
                System.Type defaultType = GetDefaultBrushType();
                defaultBrush = LoadOrCreateLibraryGridBrushAsset(defaultType);
                m_Brushes.Insert(0, defaultBrush);
                m_Brushes[0].name = GetBrushDropdownName(m_Brushes[0]);
            }

            var editorAssemblies = EditorAssemblies.loadedAssemblies;
            foreach (var editorAssembly in editorAssemblies)
            {
                try
                {
                    IEnumerable<Type> brushTypes = editorAssembly.GetTypes().Where(t => t != typeof(GridBrushBase) && t != typeof(GridBrush) && typeof(GridBrushBase).IsAssignableFrom(t));
                    foreach (var brushType in brushTypes)
                    {
                        if (IsDefaultInstanceVisibleGridBrushType(brushType))
                        {
                            var brush = LoadOrCreateLibraryGridBrushAsset(brushType);
                            if (brush != null)
                                m_Brushes.Add(brush);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log(string.Format("TilePalette failed to get types from {0}. Error: {1}", editorAssembly.FullName, ex.Message));
                }
            }

            string[] guids = AssetDatabase.FindAssets("t:GridBrushBase");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var brush = AssetDatabase.LoadAssetAtPath(path, typeof(GridBrushBase)) as GridBrushBase;
                if (brush != null && IsAssetVisibleGridBrushType(brush.GetType()))
                    m_Brushes.Add(brush);
            }

            m_BrushNames = new string[m_Brushes.Count];
            for (int i = 0; i < m_Brushes.Count; i++)
            {
                m_BrushNames[i] = m_Brushes[i].name;
            }
        }

        private bool IsDefaultInstanceVisibleGridBrushType(Type brushType)
        {
            CustomGridBrushAttribute[] customBrushes = brushType.GetCustomAttributes(typeof(CustomGridBrushAttribute), false) as CustomGridBrushAttribute[];
            if (customBrushes != null && customBrushes.Length > 0)
            {
                return !customBrushes[0].hideDefaultInstance;
            }
            return false;
        }

        private bool IsAssetVisibleGridBrushType(Type brushType)
        {
            CustomGridBrushAttribute[] customBrushes = brushType.GetCustomAttributes(typeof(CustomGridBrushAttribute), false) as CustomGridBrushAttribute[];
            if (customBrushes != null && customBrushes.Length > 0)
            {
                return !customBrushes[0].hideAssetInstances;
            }
            return false;
        }

        private void SaveLibraryGridBrushAsset(GridBrushBase brush)
        {
            var gridBrushPath = GenerateGridBrushInstanceLibraryPath(brush.GetType());
            string folderPath = Path.GetDirectoryName(gridBrushPath);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            InternalEditorUtility.SaveToSerializedFileAndForget(new[] { brush }, gridBrushPath, true);
        }

        private GridBrushBase LoadOrCreateLibraryGridBrushAsset(Type brushType)
        {
            var serializedObjects = InternalEditorUtility.LoadSerializedFileAndForget(GenerateGridBrushInstanceLibraryPath(brushType));
            if (serializedObjects != null && serializedObjects.Length > 0)
            {
                GridBrushBase brush = serializedObjects[0] as GridBrushBase;
                if (brush != null && brush.GetType() == brushType)
                    return brush;
            }
            return CreateLibraryGridBrushAsset(brushType);
        }

        private GridBrushBase CreateLibraryGridBrushAsset(Type brushType)
        {
            GridBrushBase brush = ScriptableObject.CreateInstance(brushType) as GridBrushBase;
            brush.hideFlags = HideFlags.DontSave;
            brush.name = GetBrushDropdownName(brush);
            SaveLibraryGridBrushAsset(brush);
            return brush;
        }

        private string GenerateGridBrushInstanceLibraryPath(Type brushType)
        {
            var path = FileUtil.CombinePaths(s_LibraryPath, brushType.ToString() + s_GridBrushExtension);
            path = FileUtil.NiceWinPath(path);
            return path;
        }

        private string GetBrushDropdownName(GridBrushBase brush)
        {
            // Asset Brushes use the asset name
            if (!IsLibraryBrush(brush))
                return brush.name;

            // Library Brushes
            CustomGridBrushAttribute[] customBrushes = brush.GetType().GetCustomAttributes(typeof(CustomGridBrushAttribute), false) as CustomGridBrushAttribute[];
            if (customBrushes != null && customBrushes.Length > 0 && customBrushes[0].defaultName.Length > 0)
                return customBrushes[0].defaultName;

            if (brush.GetType() == typeof(GridBrush))
                return "Default Brush";

            return brush.GetType().Name;
        }

        private static bool IsLibraryBrush(GridBrushBase brush)
        {
            return !AssetDatabase.Contains(brush);
        }

        // TODO: Better way of clearing caches than AssetPostprocessor
        public class AssetProcessor : AssetPostprocessor
        {
            public override int GetPostprocessOrder()
            {
                return 1;
            }

            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
            {
                if (!GridPaintingState.savingPalette)
                    FlushCache();
            }
        }

        internal static void FlushCache()
        {
            s_RefreshCache = true;
            if (instance.m_Brushes != null)
            {
                instance.m_Brushes.Clear();
                GridPaintingState.FlushCache();
            }
        }
    }
}
