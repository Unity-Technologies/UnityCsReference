// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEditor.Snap;

namespace UnityEditor
{
    [System.Serializable]
    internal class SceneViewGrid
    {
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
        bool m_ShowGrid = true;

        [SerializeField]
        GridRenderAxis m_GridAxis = GridRenderAxis.Y;

        [SerializeField]
        float m_gridOpacity = 1.0f;

        internal bool showGrid
        {
            get { return m_ShowGrid; }
            set { m_ShowGrid = value; }
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

        internal void OnEnable()
        {
            xGrid.color = yGrid.color = zGrid.color = kViewGridColor;
        }

        internal void Register(SceneView source)
        {
            // hook up the anims, so repainting can work correctly
            xGrid.fade.valueChanged.AddListener(source.Repaint);
            yGrid.fade.valueChanged.AddListener(source.Repaint);
            zGrid.fade.valueChanged.AddListener(source.Repaint);
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

        internal void UpdateGridsVisibility(Quaternion rotation, bool orthoMode)
        {
            bool _xGrid = false, _yGrid = false, _zGrid = false;

            if (showGrid)
            {
                if (orthoMode)
                {
                    Vector3 fwd = rotation * Vector3.forward;
                    // Show horizontal grid as long as angle is not too small
                    if (Mathf.Abs(fwd.y) > 0.2f)
                        _yGrid = true;
                    // Show xy and zy planes only when straight on
                    else if (fwd == Vector3.left || fwd == Vector3.right)
                        _xGrid = true;
                    else if (fwd == Vector3.forward || fwd == Vector3.back)
                        _zGrid = true;
                }
                else
                {
                    _xGrid = (gridAxis == GridRenderAxis.X || gridAxis == GridRenderAxis.All);
                    _yGrid = (gridAxis == GridRenderAxis.Y || gridAxis == GridRenderAxis.All);
                    _zGrid = (gridAxis == GridRenderAxis.Z || gridAxis == GridRenderAxis.All);
                }
            }

            xGrid.fade.target = _xGrid;
            yGrid.fade.target = _yGrid;
            zGrid.fade.target = _zGrid;
        }

        internal void ApplySnapConstraintsInPerspectiveMode()
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

        internal void ApplySnapConstraintsInOrthogonalMode()
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
            xGrid.size = new Vector2(EditorSnapSettings.move.y, EditorSnapSettings.move.z);
        }

        void ApplySnapContraintsOnYAxis()
        {
            yGrid.size = new Vector2(EditorSnapSettings.move.z, EditorSnapSettings.move.x);
        }

        void ApplySnapContraintsOnZAxis()
        {
            zGrid.size = new Vector2(EditorSnapSettings.move.x, EditorSnapSettings.move.y);
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

        internal DrawGridParameters PrepareGridRenderPerspectiveMode(Camera camera, Vector3 pivot, Quaternion rotation,
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

        internal DrawGridParameters PrepareGridRenderOrthogonalMode(Camera camera, Vector3 pivot, Quaternion rotation,
            float size)
        {
            Vector3 direction = camera.transform.TransformDirection(new Vector3(0, 0, 1));

            DrawGridParameters parameters = default(DrawGridParameters);

            // Don't show orthographic grid at very shallow angles because it looks bad.
            // It's normally already faded out by the managed animated fading values at this angle,
            // but if it's orbited rapidly, it can end up at this angle faster than the fading has kicked in.
            // For these cases hiding it abruptly looks better.
            // The popping isn't noticable because the user is orbiting rapidly to begin with.
            if (Mathf.Abs(direction.x) >= k_AngleThresholdForOrthographicGrid)
                parameters = xGrid.PrepareGridRender(0, gridOpacity);
            else if (Mathf.Abs(direction.y) >= k_AngleThresholdForOrthographicGrid)
                parameters = yGrid.PrepareGridRender(1, gridOpacity);
            else if (Mathf.Abs(direction.z) >= k_AngleThresholdForOrthographicGrid)
                parameters = zGrid.PrepareGridRender(2, gridOpacity);

            return parameters;
        }
    }
} // namespace
