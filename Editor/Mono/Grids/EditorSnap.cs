// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using UnityEditor.EditorTools;

namespace UnityEditor
{
    [FilePath("Library/EditorSnapSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    class EditorSnapSettingsData : ScriptableSingleton<EditorSnapSettingsData>
    {
        [SerializeField]
        bool m_SnapEnabled;
        
        [SerializeField]
        bool m_AngleSnapEnabled;
        
        [SerializeField]
        bool m_ScaleSnapEnabled;
        
        [SerializeField]
        SnapSettings m_SnapSettings = new SnapSettings();

        internal bool snapEnabled
        {
            get { return m_SnapEnabled; }
            set { m_SnapEnabled = value; }
        }
        
        internal bool angleSnapEnabled
        {
            get { return m_AngleSnapEnabled; }
            set { m_AngleSnapEnabled = value; }
        }

        internal bool scaleSnapEnabled
        {
            get { return m_ScaleSnapEnabled; }
            set { m_ScaleSnapEnabled = value; }
        }

        internal SnapSettings snapSettings
        {
            get { return m_SnapSettings; }
            set { m_SnapSettings = value; }
        }

        void OnDisable()
        {
            Save();
        }

        internal void Save()
        {
            Save(true);
        }
    }

    // Most of EditorSnapSettings is private for now because we are just hard-coding grid snap and incremental snap.
    // in the near future, we will be extending support for multiple snapping modes and this API will change.
    // ex, snap modes will likely be implemented as objects that can be registered to this class, making user-implemented
    // snapping behaviours possible.
    public static class EditorSnapSettings
    {
        static EditorSnapSettingsData instance
        {
            get { return EditorSnapSettingsData.instance; }
        }

        internal static bool activeToolGridSnapEnabled
        {
            get => ToolManager.activeToolType != null && EditorToolManager.activeTool.gridSnapEnabled;
        }

        // gridSnapEnabled controls whether objects are rounded to absolute positions on the grid, as opposed to
        // incremental rounding from translation origin. this option is only applicable when the handle rotation
        // is set to "Global" or "Grid".
        public static bool gridSnapEnabled
        {
            get
            {
                var snapSettings = instance.snapSettings;
                if (snapEnabled && snapSettings.snapToGrid && EditorGUI.actionKey)
                    return false;
                
                return snapSettings.snapToGrid;
            }
            set
            {
                var snapSettings = instance.snapSettings;
                if (snapSettings.snapToGrid != value)
                {
                    snapSettings.snapToGrid = value;
                    gridSnapEnabledChanged?.Invoke();
                }
            }
        }

        // snapEnabled is the general "is snap on" toggle.
        public static bool snapEnabled
        {
            get { return EditorGUI.actionKey ? !instance.snapEnabled : instance.snapEnabled; }
            set
            {
                if (snapEnabled != value)
                {
                    instance.snapEnabled = value;
                    snapEnabledChanged?.Invoke();
                }
            }
        }
        
        public static bool angleSnapEnabled
        {
            get { return EditorGUI.actionKey ? !instance.angleSnapEnabled : instance.angleSnapEnabled; }
            set
            {
                if (angleSnapEnabled != value)
                {
                    instance.angleSnapEnabled = value;
                    angleSnapEnabledChanged?.Invoke();
                }
            }
        }
        
        public static bool scaleSnapEnabled
        {
            get { return EditorGUI.actionKey ? !instance.scaleSnapEnabled : instance.scaleSnapEnabled; }
            set
            {
                if (scaleSnapEnabled != value)
                {
                    instance.scaleSnapEnabled = value;
                    scaleSnapEnabledChanged?.Invoke();
                }
            }
        }

        // where the 'snapEnabled' properties are preference toggles, the 'snapActive' properties are what tells tools
        // when and which snapping modes should be be applied.
        public static bool gridSnapActive => activeToolGridSnapEnabled && gridSnapEnabled && snapEnabled;

        // callback invoked when grid snapping is enabled or disabled
        public static event Action gridSnapEnabledChanged;

        public static event Action snapEnabledChanged;
        
        public static event Action angleSnapEnabledChanged;
        
        public static event Action scaleSnapEnabledChanged;

        internal static bool vertexSnapActive => HandleUtility.vertexDragging;

        // Used by 2D package
        internal static bool hotkeyActive => EditorGUI.actionKey;

        public static bool incrementalSnapActive => !gridSnapActive && snapEnabled;

        public static Vector3 gridSize
        {
            get => GridSettings.instance.gridSize;
            set
            {
                if (GridSettings.instance.gridSize != value)
                    GridSettings.instance.gridSize = value;
            }
        }

        public static Vector3 gridPosition
        {
            get => GridSettings.instance.position;
            set
            {
                var gridSettings = GridSettings.instance;
                if (gridSettings.position != value)
                {
                    // This is subject to change if we make modes public or remove them altogether
                    if (value == Vector3.zero && gridSettings.rotation == Quaternion.identity && gridSettings.activeModeIndex == GridMode.Custom)
                        gridSettings.ActivateMode(GridMode.World);
                    else  if (value != Vector3.zero && gridSettings.activeModeIndex == GridMode.World)
                        gridSettings.ActivateMode(GridMode.Custom);
                    
                    gridSettings.position = value;
                }
            }
        }
        
        public static Quaternion gridRotation
        {
            get => GridSettings.instance.rotation;
            set
            {
                var gridSettings = GridSettings.instance;
                if (gridSettings.rotation != value)
                {
                    // This is subject to change if we expose modes public or remove them altogether
                    if (value == Quaternion.identity && gridSettings.position == Vector3.zero && gridSettings.activeModeIndex == GridMode.Custom)
                        gridSettings.ActivateMode(GridMode.World);
                    else  if (value != Quaternion.identity && gridSettings.activeModeIndex == GridMode.World)
                        gridSettings.ActivateMode(GridMode.Custom);
                    
                    gridSettings.rotation = value;
                }
            }
        }

        internal static Action<Vector3> moveChanged;
        internal static Action<float> rotateChanged;
        internal static Action<float> scaleChanged;

        public static Vector3 move
        {
            get { return instance.snapSettings.incrementalSnapSize; }
            set
            {
                var snapSettings = instance.snapSettings;
                if (snapSettings.incrementalSnapSize != value)
                {
                    snapSettings.incrementalSnapSize = value;
                    moveChanged?.Invoke(value);
                }
            }
        }
        
        internal static bool moveLinked => Mathf.Approximately(move.x, move.y) && Mathf.Approximately(move.x, move.z);
        
        public static float rotate
        {
            get { return instance.snapSettings.rotation; }
            set
            {
                if (instance.snapSettings.rotation != value)
                {
                    instance.snapSettings.rotation = value;
                    rotateChanged?.Invoke(value);
                }
            }
        }

        public static float scale
        {
            get { return instance.snapSettings.scale; }
            set
            {
                if (instance.snapSettings.scale != value)
                {
                    instance.snapSettings.scale = value;
                    scaleChanged?.Invoke(value);
                }
            }
        }

        public static void ResetSnapSettings()
        {
            instance.snapSettings = new SnapSettings();
        }

        internal static void Save()
        {
            instance.Save();
        }
    }
}
