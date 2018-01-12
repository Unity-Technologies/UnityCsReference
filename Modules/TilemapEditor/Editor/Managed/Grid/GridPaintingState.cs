// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class GridPaintingState : ScriptableSingleton<GridPaintingState>, IToolModeOwner
    {
        [SerializeField] private GameObject m_ScenePaintTarget; // Which GameObject in scene is considered as painting target
        [SerializeField] private GridBrushBase m_Brush; // Which brush will handle painting callbacks
        [SerializeField] private PaintableGrid m_ActiveGrid; // Grid that has painting focus (can be palette, too)
        [SerializeField] private HashSet<Object> m_InterestedPainters = new HashSet<Object>(); // A list of objects that can paint using the GridPaintingState

        private GameObject[] m_CachedPaintTargets = null;
        private bool m_FlushPaintTargetCache;
        private Editor m_CachedEditor;
        private bool m_SavingPalette;

        public static event Action<GameObject> scenePaintTargetChanged;
        public static event Action<GridBrushBase> brushChanged;

        void OnEnable()
        {
            EditorApplication.hierarchyWindowChanged += HierarchyChanged;
            Selection.selectionChanged += OnSelectionChange;
            m_FlushPaintTargetCache = true;
        }

        void OnDisable()
        {
            m_InterestedPainters.Clear();
            EditorApplication.hierarchyWindowChanged -= HierarchyChanged;
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
            set { instance.m_ActiveGrid = value; }
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
            get
            {
                if (instance.m_FlushPaintTargetCache)
                {
                    instance.m_CachedPaintTargets = null;
                    if (activeBrushEditor != null)
                        instance.m_CachedPaintTargets = activeBrushEditor.validTargets;
                    instance.m_FlushPaintTargetCache = false;
                }
                return instance.m_CachedPaintTargets;
            }
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
}
