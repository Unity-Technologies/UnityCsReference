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
            get
            {
                return (EditorTools.ToolManager.activeToolType != null)
                    && EditorToolManager.activeTool.gridSnapEnabled;
            }
        }

        public static bool gridSnapActive
        {
            get { return !incrementalSnapActive && activeToolGridSnapEnabled && gridSnapEnabled; }
        }

        public static event Action gridSnapEnabledChanged;

        internal static bool vertexSnapActive
        {
            get { return Tools.vertexDragging; }
        }

        public static bool gridSnapEnabled
        {
            get { return instance.snapEnabled; }
            set
            {
                if (gridSnapEnabled != value)
                {
                    instance.snapEnabled = value;
                    gridSnapEnabledChanged?.Invoke();
                }
            }
        }

        public static Vector3 gridSize
        {
            get => GridSettings.size;
            set => GridSettings.size = value;
        }

        internal static bool hotkeyActive
        {
            get { return EditorGUI.actionKey; }
        }

        public static bool incrementalSnapActive
        {
            get { return Event.current != null && EditorGUI.actionKey; }
        }

        internal static Action<Vector3> moveChanged;
        internal static Action<float> rotateChanged;
        internal static Action<float> scaleChanged;

        public static Vector3 move
        {
            get { return instance.snapSettings.snapValue; }
            set
            {
                if (instance.snapSettings.snapValue != value)
                {
                    instance.snapSettings.snapValue = value;
                    moveChanged?.Invoke(value);
                }
            }
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

        internal static Vector3Int snapMultiplier
        {
            get { return instance.snapSettings.snapMultiplier; }
            set { instance.snapSettings.snapMultiplier = value; }
        }

        internal static void ResetMultiplier()
        {
            instance.snapSettings.ResetMultiplier();
        }

        internal static void Save()
        {
            instance.Save();
        }
    }
}
