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
        SnapSettings m_SnapSettings = new SnapSettings();

        internal bool snapEnabled
        {
            get { return m_SnapEnabled; }
            set { m_SnapEnabled = value; }
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
        // is set to "Global."
        public static bool gridSnapEnabled
        {
            get => instance.snapSettings.snapToGrid;
            set
            {
                instance.snapSettings.snapToGrid = value;
                gridSnapEnabledChanged?.Invoke();
            }
        }

        // snapEnabled is the general "is snap on" toggle. it controls all snapping; grid, increment, rotation, scale.
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

        // where the 'snapEnabled' properties are preference toggles, the 'snapActive' properties are what tells tools
        // when and which snapping modes should be be applied.
        public static bool gridSnapActive => activeToolGridSnapEnabled && gridSnapEnabled && snapEnabled;

        // callback invoked when grid snapping is enabled or disabled
        public static event Action gridSnapEnabledChanged;

        public static event Action snapEnabledChanged;

        internal static bool vertexSnapActive => Tools.vertexDragging;

        // Used by 2D package
        internal static bool hotkeyActive => EditorGUI.actionKey;

        public static bool incrementalSnapActive => !gridSnapActive && snapEnabled;

        public static Vector3 gridSize
        {
            get => GridSettings.size;
            set
            {
                if (GridSettings.size != value)
                    GridSettings.size = value;
            }
        }

        internal static Action<float> rotateChanged;
        internal static Action<float> scaleChanged;

        public static Vector3 move
        {
            get => gridSize;
            set => gridSize = value;
        }

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
