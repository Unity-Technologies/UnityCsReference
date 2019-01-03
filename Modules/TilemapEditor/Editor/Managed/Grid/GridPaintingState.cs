// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class GridPaintingState : ScriptableSingleton<GridPaintingState>, IToolModeOwner
    {
        internal class GridPaintTargetsSorting
        {
            public static readonly string targetSortingModeEditorPref = "TilePalette.ActiveTargetsSortingMode";
            public static readonly string targetSortingModeLookup = "Tile Palette Active Targets Sorting Mode";

            public static readonly string defaultSortingMode = L10n.Tr("None");
            public static readonly string noValidUserSortingComparer = L10n.Tr("There is no valid user comparer method for sorting Tile Palette Active Targets.");
            public static readonly GUIContent targetSortingModeLabel = EditorGUIUtility.TrTextContent(targetSortingModeLookup, "Controls the sorting of the Active Targets in the Tile Palette");

            private static string[] s_SortingNames;
            private static int s_SortingSelectionIndex;

            private static bool CompareSortingMethodName(string[] sortingMethodNames, MethodInfo sortingMethod)
            {
                return sortingMethodNames.Length == 2 && sortingMethodNames[0] == sortingMethod.ReflectedType.Name && sortingMethodNames[1] == sortingMethod.Name;
            }

            private static bool CompareSortingTypeName(string sortingTypeFullName, Type sortingType)
            {
                return sortingTypeFullName == sortingType.FullName;
            }

            internal static void PreferencesGUI()
            {
                using (new SettingsWindow.GUIScope())
                {
                    if (s_SortingNames == null)
                    {
                        var sortingTypeFullName = EditorPrefs.GetString(targetSortingModeEditorPref, defaultSortingMode);
                        var sortingMethodNames = sortingTypeFullName.Split('.');
                        s_SortingNames = new string[1 + GridPaintSortingAttribute.sortingMethods.Count + GridPaintSortingAttribute.sortingTypes.Count];
                        int count = 0;
                        s_SortingNames[count++] = defaultSortingMode;
                        foreach (var sortingMethod in GridPaintSortingAttribute.sortingMethods)
                        {
                            if (CompareSortingMethodName(sortingMethodNames, sortingMethod))
                                s_SortingSelectionIndex = count;
                            s_SortingNames[count++] = sortingMethod.Name;
                        }
                        foreach (var sortingType in GridPaintSortingAttribute.sortingTypes)
                        {
                            if (CompareSortingTypeName(sortingTypeFullName, sortingType))
                                s_SortingSelectionIndex = count;
                            s_SortingNames[count++] = sortingType.Name;
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    var val = EditorGUILayout.Popup(targetSortingModeLabel, s_SortingSelectionIndex, s_SortingNames);
                    if (EditorGUI.EndChangeCheck())
                    {
                        s_SortingSelectionIndex = val;
                        var sortingTypeFullName = defaultSortingMode;
                        if (s_SortingSelectionIndex > 0 && s_SortingSelectionIndex <= GridPaintSortingAttribute.sortingMethods.Count)
                        {
                            var sortingMethod = GridPaintSortingAttribute.sortingMethods[s_SortingSelectionIndex - 1];
                            sortingTypeFullName = String.Format("{0}.{1}", sortingMethod.ReflectedType.Name, sortingMethod.Name);
                        }
                        else
                        {
                            var idx = s_SortingSelectionIndex - GridPaintSortingAttribute.sortingMethods.Count - 1;
                            if (idx >= 0 && idx < GridPaintSortingAttribute.sortingTypes.Count)
                            {
                                var sortingType = GridPaintSortingAttribute.sortingTypes[idx];
                                sortingTypeFullName = sortingType.FullName;
                            }
                        }
                        EditorPrefs.SetString(targetSortingModeEditorPref, sortingTypeFullName);
                        GridPaintingState.FlushCache();
                    }
                }
            }

            public static IComparer<GameObject> GetTargetComparer()
            {
                var sortingTypeFullName = EditorPrefs.GetString(targetSortingModeEditorPref, defaultSortingMode);
                if (!sortingTypeFullName.Equals(defaultSortingMode))
                {
                    var sortingMethodNames = sortingTypeFullName.Split('.');
                    foreach (var sortingMethod in GridPaintSortingAttribute.sortingMethods)
                    {
                        if (CompareSortingMethodName(sortingMethodNames, sortingMethod))
                            return sortingMethod.Invoke(null, null) as IComparer<GameObject>;
                    }
                    foreach (var sortingType in GridPaintSortingAttribute.sortingTypes)
                    {
                        if (CompareSortingTypeName(sortingTypeFullName, sortingType))
                            return Activator.CreateInstance(sortingType) as IComparer<GameObject>;
                    }
                }
                return null;
            }
        }

        [SerializeField] private GameObject m_ScenePaintTarget; // Which GameObject in scene is considered as painting target
        [SerializeField] private GridBrushBase m_Brush; // Which brush will handle painting callbacks
        [SerializeField] private PaintableGrid m_ActiveGrid; // Grid that has painting focus (can be palette, too)
        [SerializeField] private PaintableGrid m_LastActiveGrid; // Grid that last had painting focus (can be palette, too)
        [SerializeField] private HashSet<Object> m_InterestedPainters = new HashSet<Object>(); // A list of objects that can paint using the GridPaintingState

        private GameObject[] m_CachedPaintTargets = null;
        private bool m_FlushPaintTargetCache;
        private Editor m_CachedEditor;
        private bool m_SavingPalette;

        public static event Action<GameObject> scenePaintTargetChanged;
        public static event Action<GridBrushBase> brushChanged;

        void OnEnable()
        {
            EditorApplication.hierarchyChanged += HierarchyChanged;
            Selection.selectionChanged += OnSelectionChange;
            m_FlushPaintTargetCache = true;
        }

        void OnDisable()
        {
            m_InterestedPainters.Clear();
            EditorApplication.hierarchyChanged -= HierarchyChanged;
            Selection.selectionChanged -= OnSelectionChange;
            FlushCache();
        }

        private void OnSelectionChange()
        {
            if (hasInterestedPainters && validTargets == null && ValidatePaintTarget(Selection.activeGameObject))
            {
                scenePaintTarget = Selection.activeGameObject;
            }
        }

        private void HierarchyChanged()
        {
            if (hasInterestedPainters)
            {
                m_FlushPaintTargetCache = true;
                if (validTargets == null || !validTargets.Contains(scenePaintTarget))
                    AutoSelectPaintTarget();
            }
        }

        private GameObject[] GetValidTargets()
        {
            if (m_FlushPaintTargetCache)
            {
                m_CachedPaintTargets = null;
                if (activeBrushEditor != null)
                    m_CachedPaintTargets = activeBrushEditor.validTargets;
                if (m_CachedPaintTargets == null || m_CachedPaintTargets.Length == 0)
                    scenePaintTarget = null;
                else
                {
                    var comparer = GridPaintTargetsSorting.GetTargetComparer();
                    if (comparer != null)
                        Array.Sort(m_CachedPaintTargets, comparer);
                }

                m_FlushPaintTargetCache = false;
            }
            return m_CachedPaintTargets;
        }

        public static void AutoSelectPaintTarget()
        {
            if (activeBrushEditor != null)
            {
                if (validTargets != null && validTargets.Length > 0)
                {
                    scenePaintTarget = validTargets[0];
                }
            }
        }

        public static GameObject scenePaintTarget
        {
            get { return instance.m_ScenePaintTarget; }
            set
            {
                if (value != instance.m_ScenePaintTarget)
                {
                    instance.m_ScenePaintTarget = value;
                    if (scenePaintTargetChanged != null)
                        scenePaintTargetChanged(instance.m_ScenePaintTarget);
                }
            }
        }

        public static GridBrushBase gridBrush
        {
            get
            {
                if (instance.m_Brush == null)
                    instance.m_Brush = GridPaletteBrushes.brushes[0];

                return instance.m_Brush;
            }
            set
            {
                if (instance.m_Brush != value)
                {
                    instance.m_Brush = value;
                    instance.m_FlushPaintTargetCache = true;

                    // Ensure that current scenePaintTarget is still a valid target after a brush change
                    if (scenePaintTarget != null && !ValidatePaintTarget(scenePaintTarget))
                        scenePaintTarget = null;

                    // Use Selection if previous scenePaintTarget was not valid
                    if (scenePaintTarget == null)
                        scenePaintTarget = ValidatePaintTarget(Selection.activeGameObject) ? Selection.activeGameObject : null;

                    // Auto select a valid target if there is still no scenePaintTarget
                    if (scenePaintTarget == null)
                        AutoSelectPaintTarget();

                    if (brushChanged != null)
                        brushChanged(instance.m_Brush);
                }
            }
        }

        public static GridBrush defaultBrush
        {
            get { return gridBrush as GridBrush; }
            set { gridBrush = value; }
        }

        public static GridBrushEditorBase activeBrushEditor
        {
            get
            {
                Editor.CreateCachedEditor(gridBrush, null, ref instance.m_CachedEditor);
                GridBrushEditorBase baseEditor = instance.m_CachedEditor as GridBrushEditorBase;
                return baseEditor;
            }
        }

        public static Editor fallbackEditor
        {
            get
            {
                Editor.CreateCachedEditor(gridBrush, null, ref instance.m_CachedEditor);
                return instance.m_CachedEditor;
            }
        }

        public static PaintableGrid activeGrid
        {
            get { return instance.m_ActiveGrid; }
            set
            {
                instance.m_ActiveGrid = value;
                if (instance.m_ActiveGrid != null)
                    instance.m_LastActiveGrid = value;
            }
        }

        public static PaintableGrid lastActiveGrid
        {
            get { return instance.m_LastActiveGrid; }
        }

        public static bool ValidatePaintTarget(GameObject candidate)
        {
            if (candidate == null || candidate.GetComponentInParent<Grid>() == null && candidate.GetComponent<Grid>() == null)
                return false;

            if (validTargets != null && !validTargets.Contains(candidate))
                return false;

            return true;
        }

        public static void FlushCache()
        {
            if (instance.m_CachedEditor != null)
            {
                DestroyImmediate(instance.m_CachedEditor);
                instance.m_CachedEditor = null;
            }
            instance.m_FlushPaintTargetCache = true;
        }

        public static bool savingPalette
        {
            get { return instance.m_SavingPalette; }
            set { instance.m_SavingPalette = value; }
        }

        public static GameObject[] validTargets
        {
            get { return instance.GetValidTargets(); }
        }

        public static void RegisterPainterInterest(Object painter)
        {
            instance.m_InterestedPainters.Add(painter);
        }

        public static void UnregisterPainterInterest(Object painter)
        {
            instance.m_InterestedPainters.Remove(painter);
        }

        public bool hasInterestedPainters
        {
            get { return m_InterestedPainters.Count > 0; }
        }

        public bool areToolModesAvailable { get { return true; } }

        public Bounds GetWorldBoundsOfTargets()
        {
            return new Bounds(Vector3.zero, Vector3.positiveInfinity);
        }

        public bool ModeSurvivesSelectionChange(int toolMode)
        {
            return true;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class GridPaintSortingAttribute : Attribute
    {
        private static List<MethodInfo> m_SortingMethods;
        private static List<Type> m_SortingTypes;

        internal static List<MethodInfo> sortingMethods
        {
            get
            {
                if (m_SortingMethods == null)
                    GetUserSortingComparers();
                return m_SortingMethods;
            }
        }

        internal static List<Type> sortingTypes
        {
            get
            {
                if (m_SortingTypes == null)
                    GetUserSortingComparers();
                return m_SortingTypes;
            }
        }

        private static void GetUserSortingComparers()
        {
            m_SortingMethods = new List<MethodInfo>();
            foreach (var sortingMethod in EditorAssemblies.GetAllMethodsWithAttribute<GridPaintSortingAttribute>())
            {
                if (!sortingMethod.ReturnType.IsAssignableFrom(typeof(IComparer<GameObject>)))
                    continue;
                if (sortingMethod.GetGenericArguments().Length > 0)
                    continue;
                m_SortingMethods.Add(sortingMethod);
            }

            m_SortingTypes = new List<Type>();
            foreach (var sortingType in EditorAssemblies.GetAllTypesWithAttribute<GridPaintSortingAttribute>())
            {
                if (sortingType.IsAbstract)
                    continue;
                m_SortingTypes.Add(sortingType);
            }
        }

        [GridPaintSorting]
        internal class Alphabetical : IComparer<GameObject>
        {
            public int Compare(GameObject go1, GameObject go2)
            {
                return String.Compare(go1.name, go2.name);
            }
        }

        [GridPaintSorting]
        internal class ReverseAlphabetical : IComparer<GameObject>
        {
            public int Compare(GameObject go1, GameObject go2)
            {
                return -String.Compare(go1.name, go2.name);
            }
        }
    }
}
