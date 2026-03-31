// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    interface IGridMode
    {
        Vector3 position { get; set; }
        Quaternion rotation { get; set; }

        event Action<IGridMode> settingsChanged;
    }

    [Serializable]
    class WorldGridMode : IGridMode
    {
        public Vector3 position
        {
            get => Vector3.zero;
            set { }
        }

        public Quaternion rotation
        {
            get => Quaternion.identity;
            set { }
        }

#pragma warning disable CS0067
        public event Action<IGridMode> settingsChanged;
#pragma warning restore CS0067
    }

    [Serializable]
    class CustomGridMode : IGridMode
    {
        public Vector3 position
        {
            get => m_Position;
            set
            {
                if (m_Position == value)
                    return;

                m_Position = value;
                settingsChanged?.Invoke(this);
            }
        }

        public Quaternion rotation
        {
            get { return m_Rotation; }
            set
            {
                if (m_Rotation == value)
                    return;

                // Do not allow setting invalid quaternion
                if (value.x == 0 && value.y == 0 &&
                    value.z == 0 && value.w == 0)
                    return;

                m_Rotation = value;
                settingsChanged?.Invoke(this);
            }
        }

        [SerializeField] 
        Vector3 m_Position;
        [SerializeField] 
        Quaternion m_Rotation = Quaternion.identity;

        public event Action<IGridMode> settingsChanged;
    }

    enum GridMode
    {
        World,
        Custom
    }

    [FilePath("Library/EditorGridSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    class GridSettings : ScriptableSingleton<GridSettings>
    {
        const GridMode k_DefaultGridMode = GridMode.World;

        const float k_GridSizeMin = 0f;
        const float k_GridSizeMax = 1024f;
        const int k_DefaultMultiplier = 0;

        public const float defaultGridSize = 1.0f;
        static readonly Vector3 k_DefaultGridSizeVec = new(defaultGridSize, defaultGridSize, defaultGridSize);

        [SerializeField]
        GridMode m_ActiveModeIndex = k_DefaultGridMode;

        [SerializeReference]
        List<IGridMode> m_GridModes = new List<IGridMode> { new WorldGridMode(), new CustomGridMode() };

        [SerializeField]
        Vector3 m_GridSize = k_DefaultGridSizeVec;

        [SerializeField]
        int m_GridMultiplier = k_DefaultMultiplier;

        void OnEnable()
        {
            for (int i = 0; i < m_GridModes.Count; ++i)
                m_GridModes[i].settingsChanged += OnModeSettingsChanged;
        }

        void OnDisable()
        {
            for (int i = 0; i < m_GridModes.Count; ++i)
                m_GridModes[i].settingsChanged -= OnModeSettingsChanged;

            Save();
        }

        internal void Save()
        {
            Save(true);
        }

        internal GridMode activeModeIndex => m_ActiveModeIndex;

        internal IGridMode activeMode => m_GridModes[(int)m_ActiveModeIndex];

        // Used by ProBuilder package.
        internal static Vector3 size => instance.gridSize;

        public Vector3 gridSize
        {
            get => ApplyMultiplier(m_GridSize, sizeMultiplier);

            set
            {
                var currentSize = sizeMultiplier == 0 ? m_GridSize : ApplyMultiplier(m_GridSize, sizeMultiplier);
                if (currentSize == value)
                    return;

                ResetSizeMultiplier();
                m_GridSize = ClampToGrid(value);

                sizeChanged?.Invoke(this.gridSize);
            }
        }

        internal int sizeMultiplier
        {
            get => m_GridMultiplier;

            set
            {
                if (sizeMultiplier != value)
                {
                    m_GridMultiplier = value;
                    sizeChanged?.Invoke(gridSize);
                }
            }
        }

        public Vector3 position
        {
            get => activeMode.position;

            set
            {
                var newValue = value;
                if (sceneViewIn2DModeExists)
                    newValue.z = 0;
                
                if (activeMode.position == newValue)
                    return;
                
                activeMode.position = newValue;
            }
        }
        
        internal static Transform lastRotationSampleTransform { get; set; }

        public Quaternion rotation
        {
            get => activeMode.rotation;

            set
            {
                var newValue = value;
                if (sceneViewIn2DModeExists)
                {
                    var euler = newValue.eulerAngles;
                    newValue = Quaternion.Euler(0f, 0f, euler.z);
                }

                if (activeMode.rotation == newValue)
                    return;

                activeMode.rotation = newValue;
                lastRotationSampleTransform = null;
            }
        }

        bool sceneViewIn2DModeExists
        {
            get
            {
                foreach (SceneView sceneView in SceneView.sceneViews)
                {
                    if (sceneView.in2DMode)
                        return true;
                }

                return false;
            }
        }

        internal event Action<Vector3> sizeChanged = delegate { };
        internal event Action modeChanged = delegate { };
        internal event Action modeSettingsChanged = delegate { };

        internal bool linked => Mathf.Approximately(gridSize.x, gridSize.y) && Mathf.Approximately(gridSize.x, gridSize.z);

        internal bool currentGridIsWorld => position == Vector3.zero && rotation == Quaternion.identity;

        internal bool customGridIsWorld
        {
            get
            {
                var customMode = GetMode(GridMode.Custom);
                return customMode.position == Vector3.zero && customMode.rotation == Quaternion.identity;
            }
        }

        internal void ActivateMode(GridMode mode)
        {
            m_ActiveModeIndex = mode;
            modeChanged?.Invoke();
        }

        internal IGridMode GetMode(GridMode mode)
        {
            return m_GridModes[(int)mode];
        }

        internal void ResetSizeMultiplier()
        {
            sizeMultiplier = k_DefaultMultiplier;
        }

        Vector3 ApplyMultiplier(Vector3 value, int mul)
        {
            if (mul != 0)
            {
                float multiplier = Mathf.Pow(2f, mul);
                value *= multiplier;
            }

            value = ClampToGrid(value);

            return value;
        }

        internal void ApplyCustomPosition(Vector3 newPosition)
        {
            if(!IsValid(newPosition))
                return;

            if (activeModeIndex != GridMode.Custom)
            {
                var retainedRotation = rotation;
                ActivateMode(GridMode.Custom);
                rotation = retainedRotation;
            }

            EditorSnapSettings.gridPosition = newPosition;
            SceneView.RepaintAll();
        }

        internal void ApplyCustomRotation(Quaternion newRotation)
        {
            if(!IsValid(newRotation))
                return;

            if (activeModeIndex != GridMode.Custom)
            {
                var retainedPosition = position;
                ActivateMode(GridMode.Custom);
                position = retainedPosition;
            }

            EditorSnapSettings.gridRotation = newRotation;
            SceneView.RepaintAll();
        }
        
        internal void SampleTransformRotation(Transform transform)
        {
            ApplyCustomRotation(transform.rotation);
            lastRotationSampleTransform = transform;
        }

        internal void ResetGridSettings()
        {
            gridSize = k_DefaultGridSizeVec;
            rotation = Quaternion.identity;
            m_GridMultiplier = k_DefaultMultiplier;
            ActivateMode(k_DefaultGridMode);
        }

        void OnModeSettingsChanged(IGridMode mode)
        {
            modeSettingsChanged?.Invoke();
        }

        Vector3 ClampToGrid(Vector3 value)
        {
            value.x = Mathf.Clamp(value.x, k_GridSizeMin, k_GridSizeMax);
            value.y = Mathf.Clamp(value.y, k_GridSizeMin, k_GridSizeMax);
            value.z = Mathf.Clamp(value.z, k_GridSizeMin, k_GridSizeMax);

            return value;
        }

        internal static bool IsValid(Vector3 value)
        {
            return !(float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z) ||
                     float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z));
        }

        bool IsValid(Quaternion value)
        {
            return !(float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z) || float.IsNaN(value.w) ||
                     float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z) || float.IsInfinity(value.w));
        }
    }
}
