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

    public static class EditorSnapSettings
    {
        static EditorSnapSettingsData instance
        {
            get { return EditorSnapSettingsData.instance; }
        }

        // Is snapping toggled as `on` in the grid toolbar
        public static bool enabled
        {
            get { return instance.snapEnabled; }
            set { instance.snapEnabled = value; }
        }

        // Is snapping active (either through shortcut key or enabled)
        public static bool active
        {
            get
            {
                return Event.current == null
                    ? instance.snapEnabled
                    : EditorGUI.actionKey ? !instance.snapEnabled : instance.snapEnabled;
            }
        }

        public static bool preferGrid
        {
            get { return instance.snapSettings.preferGrid; }
            set { instance.snapSettings.preferGrid = value; }
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
