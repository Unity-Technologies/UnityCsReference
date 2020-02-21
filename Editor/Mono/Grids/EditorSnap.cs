// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

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

        internal static bool activeToolSupportsGridSnap
        {
            get
            {
                return (EditorTools.EditorTools.activeToolType == typeof(MoveTool)
                    || EditorTools.EditorTools.activeToolType == typeof(TransformTool))
                    && Tools.pivotRotation == PivotRotation.Global;
            }
        }

        internal static bool gridSnapActive
        {
            get { return !incrementalSnapActive && activeToolSupportsGridSnap && instance.snapEnabled; }
        }

        internal static bool vertexSnapActive
        {
            get { return Tools.vertexDragging; }
        }

        public static bool gridSnapEnabled
        {
            get { return instance.snapEnabled; }
            set { instance.snapEnabled = value; }
        }

        internal static bool hotkeyActive
        {
            get { return EditorGUI.actionKey; }
        }

        internal static bool incrementalSnapActive
        {
            get { return Event.current != null && EditorGUI.actionKey; }
        }

        public static Vector3 move
        {
            get { return instance.snapSettings.snapValue; }
            set { instance.snapSettings.snapValue = value; }
        }

        public static float rotate
        {
            get { return instance.snapSettings.rotation; }
            set { instance.snapSettings.rotation = value; }
        }

        public static float scale
        {
            get { return instance.snapSettings.scale; }
            set { instance.snapSettings.scale = value; }
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
