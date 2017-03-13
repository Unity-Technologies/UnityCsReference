// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Object = UnityEngine.Object;
using EditorGraphicsSettings = UnityEditor.Rendering.EditorGraphicsSettings;
using TierSettingsEditor = UnityEditor.GraphicsSettingsWindow.TierSettingsEditor;

namespace UnityEditor
{
    internal partial class TierSettingsWindow : EditorWindow
    {
        static TierSettingsWindow s_Instance;
        static public void CreateWindow()
        {
            s_Instance = EditorWindow.GetWindow<TierSettingsWindow>();
            s_Instance.minSize = new Vector2(600, 300);
            s_Instance.titleContent = EditorGUIUtility.TextContent("Tier Settings");
        }

        internal static TierSettingsWindow GetInstance()
        {
            return s_Instance;
        }

        Editor m_TierSettingsEditor;

        void OnEnable()
        {
            s_Instance = this;
        }

        void OnDisable()
        {
            DestroyImmediate(m_TierSettingsEditor); m_TierSettingsEditor = null;
            if (s_Instance == this)
                s_Instance = null;
        }

        Object graphicsSettings
        {
            get { return UnityEngine.Rendering.GraphicsSettings.GetGraphicsSettings(); }
        }
        Editor tierSettingsEditor
        {
            get
            {
                Editor.CreateCachedEditor(graphicsSettings, typeof(TierSettingsEditor), ref m_TierSettingsEditor);
                ((TierSettingsEditor)m_TierSettingsEditor).verticalLayout = false;
                return m_TierSettingsEditor;
            }
        }

        void OnGUI()
        {
            tierSettingsEditor.OnInspectorGUI();
        }
    }
}
