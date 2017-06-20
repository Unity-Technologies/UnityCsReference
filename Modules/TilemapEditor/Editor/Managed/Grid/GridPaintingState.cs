// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    internal class GridPaintingState : ScriptableSingleton<GridPaintingState>, IToolModeOwner
    {
        [SerializeField] private GameObject m_ScenePaintTarget; // Which GameObject in scene is considered as painting target
        [SerializeField] private GridBrushBase m_Brush; // Which brush will handle painting callbacks
        [SerializeField] private PaintableGrid m_ActiveGrid; // Grid that has painting focus (can be palette, too)

        private Editor m_CachedEditor;
        private bool m_SavingPalette;

        public static event Action<GameObject> scenePaintTargetChanged;
        public static event Action<GridBrushBase> brushChanged;

        void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChange;
        }

        void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChange;
        }

        private void OnSelectionChange()
        {
            if (ValidatePaintTarget(Selection.activeGameObject))
                scenePaintTarget = Selection.activeGameObject;
        }

        static public GameObject scenePaintTarget
        {
            get { return instance.m_ScenePaintTarget; }
            set
            {
                instance.m_ScenePaintTarget = value;
                if (scenePaintTargetChanged != null)
                    scenePaintTargetChanged(instance.m_ScenePaintTarget);
            }
        }

        public static GridBrushBase gridBrush
        {
            get { return instance.m_Brush; }
            set
            {
                if (instance.m_Brush != value)
                {
                    instance.m_Brush = value;

                    scenePaintTarget = ValidatePaintTarget(Selection.activeGameObject) ? Selection.activeGameObject : null;

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
            if (candidate == null || candidate.GetComponentInParent<Grid>() == null || activeBrushEditor == null)
                return false;

            GameObject[] validTargets = activeBrushEditor.validTargets;
            if (validTargets != null && !validTargets.Contains(candidate))
                return false;

            return true;
        }

        public static void FlushCache()
        {
            if (instance.m_CachedEditor != null)
                DestroyImmediate(instance.m_CachedEditor);
        }

        public static bool savingPalette
        {
            get { return instance.m_SavingPalette; }
            set { instance.m_SavingPalette = value; }
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
