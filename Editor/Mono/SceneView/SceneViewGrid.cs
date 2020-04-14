// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor
{
    static class GridSettings
    {
        const float k_GridSizeMin = .0001f;
        const float k_GridSizeMax = 1024f;
        const float k_DefaultGridSize = 1.0f;
        const int k_DefaultMultiplier = 0;

        static SavedFloat s_GridSizeX = new SavedFloat("GridSizeX", k_DefaultGridSize);
        static SavedFloat s_GridSizeY = new SavedFloat("GridSizeY", k_DefaultGridSize);
        static SavedFloat s_GridSizeZ = new SavedFloat("GridSizeZ", k_DefaultGridSize);
        static SavedInt s_GridMultiplier = new SavedInt("GridMultiplier", k_DefaultMultiplier);

        static Vector3 rawSize
        {
            get { return new Vector3(s_GridSizeX, s_GridSizeY, s_GridSizeZ); }
        }

        internal static event Action<Vector3> sizeChanged = delegate {};

        public static Vector3 size
        {
            get { return ApplyMultiplier(rawSize, s_GridMultiplier); }
            set
            {
                if (size == value)
                    return;
                ResetSizeMultiplier();
                s_GridSizeX.value = Mathf.Min(k_GridSizeMax, Mathf.Max(k_GridSizeMin, value.x));
                s_GridSizeY.value = Mathf.Min(k_GridSizeMax, Mathf.Max(k_GridSizeMin, value.y));
                s_GridSizeZ.value = Mathf.Min(k_GridSizeMax, Mathf.Max(k_GridSizeMin, value.z));
                sizeChanged(size);
            }
        }

        internal static int sizeMultiplier
        {
            get { return s_GridMultiplier.value; }
            set { s_GridMultiplier.value = value; }
        }

        internal static void ResetSizeMultiplier()
        {
            s_GridMultiplier.value = k_DefaultMultiplier;
        }

        static Vector3 ApplyMultiplier(Vector3 value, int mul)
        {
            if (mul > 0)
            {
                for (int i = 0; i < mul; i++)
                    value *= 2f;
            }
            else if (mul < 0)
            {
                for (int i = 0; i > mul; i--)
                    value /= 2f;
            }
            return value;
        }

        public static void ResetGridSettings()
        {
            size = new Vector3(k_DefaultGridSize, k_DefaultGridSize, k_DefaultGridSize);
        }
    }

    [System.Serializable]
    class SceneViewGrid
    {
        const float k_DefaultGridOpacity = .5f;
        const GridRenderAxis k_DefaultRenderAxis = GridRenderAxis.Y;
        const bool k_DefaultShowGrid = true;

        internal event Action<bool> gridVisibilityChanged = delegate(bool b) {};

        internal enum GridRenderAxis
        {
            X,
            Y,
            Z,
            All
        }

        [System.Serializable]
        internal class Grid
        {
            [SerializeField]
            AnimBool m_Fade = new AnimBool();

            [SerializeField]
            Color m_Color;

            [SerializeField]
            Vector3 m_Pivot;

            [SerializeField]
            Vector2 m_Size;

            internal AnimBool fade
            {
                get { return m_Fade; }
                set { m_Fade = value; }
            }

            internal Color color
            {
                get { return m_Color; }
                set { m_Color = value; }
            }

            internal Vector3 pivot
            {
                get { return m_Pivot; }
                set { m_Pivot = value; }
            }

            internal Vector2 size
            {
                get { return m_Size; }
                set { m_Size = value; }
            }

            internal DrawGridParameters PrepareGridRender(int gridID, float opacity)
            {
                DrawGridParameters parameters = default(DrawGridParameters);
                parameters.gridID = gridID;
                parameters.pivot = pivot;
                parameters.color = color;
                parameters.color.a = fade.faded * opacity;
                parameters.size = size;

                return parameters;
            }
        }

        internal static PrefColor kViewGridColor = new PrefColor("Scene/Grid", .5f, .5f, .5f, .4f);
        static float k_AngleThresholdForOrthographicGrid = 0.15f;

        [SerializeField]
        Grid xGrid = new Grid();

        [SerializeField]
        Grid yGrid = new Grid();

        [SerializeField]
        Grid zGrid = new Grid();

        [SerializeField]
        bool m_ShowGrid = k_DefaultShowGrid;

        [SerializeField]
        GridRenderAxis m_GridAxis = k_DefaultRenderAxis;

        [SerializeField]
        float m_gridOpacity = k_DefaultGridOpacity;

        internal bool showGrid
        {
            get { return m_ShowGrid; }
            set
            {
                if (value == m_ShowGrid)
                    return;
                m_ShowGrid = value;
                gridVisibilityChanged(m_ShowGrid);
            }
        }

        internal float gridOpacity
        {
            get { return m_gridOpacity; }
            set { m_gridOpacity = Mathf.Clamp01(value); }
        }

        internal GridRenderAxis gridAxis
        {
            get { return m_GridAxis; }
            set { m_GridAxis = value; }
        }

        internal Grid activeGrid
        {
            get
            {
                if (gridAxis == GridRenderAxis.X)
                    return xGrid;
                else if (gridAxis == GridRenderAxis.Y)
                    return yGrid;
                else if (gridAxis == GridRenderAxis.Z)
                    return zGrid;
                return yGrid;
            }
        }

        internal void UpdateGridColor()
        {
            xGrid.color = yGrid.color = zGrid.color = kViewGridColor;
        }

        internal void OnEnable(SceneView view)
        {
            UpdateGridColor();

            GridSettings.sizeChanged += GridSizeChanged;

            // hook up the anims, so repainting can work correctly
            xGrid.fade.valueChanged.AddListener(view.Repaint);
            yGrid.fade.valueChanged.AddListener(view.Repaint);
            zGrid.fade.valueChanged.AddListener(view.Repaint);
        }

        internal void OnDisable(SceneView view)
        {
            GridSettings.sizeChanged -= GridSizeChanged;

            xGrid.fade.valueChanged.RemoveListener(view.Repaint);
            yGrid.fade.valueChanged.RemoveListener(view.Repaint);
            zGrid.fade.valueChanged.RemoveListener(view.Repaint);
        }

        void GridSizeChanged(Vector3 size)
        {
            SetPivot(GridRenderAxis.X, Snapping.Snap(GetPivot(GridRenderAxis.X), GridSettings.size));
            SetPivot(GridRenderAxis.Y, Snapping.Snap(GetPivot(GridRenderAxis.Y), GridSettings.size));
            SetPivot(GridRenderAxis.Z, Snapping.Snap(GetPivot(GridRenderAxis.Z), GridSettings.size));
        }

        internal void SetAllGridsPivot(Vector3 pivot)
        {
            xGrid.pivot = pivot;
            yGrid.pivot = pivot;
            zGrid.pivot = pivot;
        }

        internal void SetPivot(GridRenderAxis axis, Vector3 pivot)
        {
            if (axis == GridRenderAxis.X)
                xGrid.pivot = pivot;
            else if (axis == GridRenderAxis.Y)
                yGrid.pivot = pivot;
            else if (axis == GridRenderAxis.Z)
                zGrid.pivot = pivot;
        }

        internal Vector3 GetPivot(GridRenderAxis axis)
        {
            if (axis == GridRenderAxis.X)
                return xGrid.pivot;
            else if (axis == GridRenderAxis.Y)
                return yGrid.pivot;
            else if (axis == GridRenderAxis.Z)
                return zGrid.pivot;
            return Vector3.zero;
        }

        internal void ResetPivot(GridRenderAxis axis)
        {
            if (axis == GridRenderAxis.X)
                xGrid.pivot = Vector3.zero;
            else if (axis == GridRenderAxis.Y)
                yGrid.pivot = Vector3.zero;
            else if (axis == GridRenderAxis.Z)
                zGrid.pivot = Vector3.zero;
            else if (axis == GridRenderAxis.All)
                xGrid.pivot = yGrid.pivot = zGrid.pivot = Vector3.zero;
        }

        internal void UpdateGridsVisibility(Quaternion rotation, bool orthoMode)
        {
            bool showX = false, showY = false, showZ = false;

            if (showGrid)
            {
                if (orthoMode)
                {
                    Vector3 fwd = rotation * Vector3.forward;

                    // Show xy, zy and xz planes only when straight on
                    if (fwd == Vector3.up || fwd == Vector3.down)
                        showY = true;
                    else if (fwd == Vector3.left || fwd == Vector3.right)
                        showX = true;
                    else if (fwd == Vector3.forward || fwd == Vector3.back)
                        showZ = true;
                }

                // Main path for perspective mode.
                // In ortho, fallback on this path if camera is not aligned with X, Y or Z axis.
                if (!showX && !showY && !showZ)
                {
                    showX = (gridAxis == GridRenderAxis.X || gridAxis == GridRenderAxis.All);
                    showY = (gridAxis == GridRenderAxis.Y || gridAxis == GridRenderAxis.All);
                    showZ = (gridAxis == GridRenderAxis.Z || gridAxis == GridRenderAxis.All);
                }
            }

            xGrid.fade.target = showX;
            yGrid.fade.target = showY;
            zGrid.fade.target = showZ;
        }

        internal void SkipFading()
        {
            xGrid.fade.SkipFading();
            yGrid.fade.SkipFading();
            zGrid.fade.SkipFading();
        }

        void ApplySnapConstraintsInPerspectiveMode()
        {
            switch (gridAxis)
            {
                case GridRenderAxis.X:
                    ApplySnapContraintsOnXAxis();
                    break;
                case GridRenderAxis.Y:
                    ApplySnapContraintsOnYAxis();
                    break;
                case GridRenderAxis.Z:
                    ApplySnapContraintsOnZAxis();
                    break;
            }
        }

        void ApplySnapConstraintsInOrthogonalMode()
        {
            if (xGrid.fade.target)
                ApplySnapContraintsOnXAxis();
            if (yGrid.fade.target)
                ApplySnapContraintsOnYAxis();
            if (zGrid.fade.target)
                ApplySnapContraintsOnZAxis();
        }

        void ApplySnapContraintsOnXAxis()
        {
            Vector3 grid = GridSettings.size;
            xGrid.size = new Vector2(grid.y, grid.z);
        }

        void ApplySnapContraintsOnYAxis()
        {
            Vector3 grid = GridSettings.size;
            yGrid.size = new Vector2(grid.z, grid.x);
        }

        void ApplySnapContraintsOnZAxis()
        {
            Vector3 grid = GridSettings.size;
            zGrid.size = new Vector2(grid.x, grid.y);
        }

        internal DrawGridParameters PrepareGridRender(Camera camera, Vector3 pivot, Quaternion rotation,
            float size, bool orthoMode)
        {
            UpdateGridsVisibility(rotation, orthoMode);

            if (orthoMode)
            {
                ApplySnapConstraintsInOrthogonalMode();
                return PrepareGridRenderOrthogonalMode(camera, pivot, rotation, size);
            }

            ApplySnapConstraintsInPerspectiveMode();
            return PrepareGridRenderPerspectiveMode(camera, pivot, rotation, size);
        }

        DrawGridParameters PrepareGridRenderPerspectiveMode(Camera camera, Vector3 pivot, Quaternion rotation,
            float size)
        {
            DrawGridParameters parameters = default(DrawGridParameters);

            switch (gridAxis)
            {
                case GridRenderAxis.X:
                    parameters = xGrid.PrepareGridRender(0, gridOpacity);
                    break;
                case GridRenderAxis.Y:
                    parameters = yGrid.PrepareGridRender(1, gridOpacity);
                    break;
                case GridRenderAxis.Z:
                    parameters = zGrid.PrepareGridRender(2, gridOpacity);
                    break;
            }

            return parameters;
        }

        DrawGridParameters PrepareGridRenderOrthogonalMode(Camera camera, Vector3 pivot, Quaternion rotation,
            float size)
        {
            Vector3 direction = camera.transform.TransformDirection(new Vector3(0, 0, 1));

            DrawGridParameters parameters = default(DrawGridParameters);

            // Don't show orthographic grid at very shallow angles because it looks bad.
            // It's normally already faded out by the managed animated fading values at this angle,
            // but if it's orbited rapidly, it can end up at this angle faster than the fading has kicked in.
            // For these cases hiding it abruptly looks better.
            // The popping isn't noticable because the user is orbiting rapidly to begin with.
            if (xGrid.fade.target && Mathf.Abs(direction.x) >= k_AngleThresholdForOrthographicGrid)
                parameters = xGrid.PrepareGridRender(0, gridOpacity);
            else if (yGrid.fade.target && Mathf.Abs(direction.y) >= k_AngleThresholdForOrthographicGrid)
                parameters = yGrid.PrepareGridRender(1, gridOpacity);
            else if (zGrid.fade.target && Mathf.Abs(direction.z) >= k_AngleThresholdForOrthographicGrid)
                parameters = zGrid.PrepareGridRender(2, gridOpacity);

            return parameters;
        }

        internal void Reset()
        {
            gridOpacity = k_DefaultGridOpacity;
            showGrid = k_DefaultShowGrid;
            gridAxis = k_DefaultRenderAxis;
        }
    }
} // namespace
