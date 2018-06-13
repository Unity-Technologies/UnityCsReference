// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Internal;

namespace UnityEditor
{
    [ExcludeFromDocs]
    public class AssetSettingsProvider : SettingsProvider
    {
        UnityEngine.Object m_Settings;
        Editor m_SettingsEditor;
        public List<string> SearchKeywords = new List<string>();

        public string settingsAssetPath { get; set; }

        public AssetSettingsProvider(string preferencePath, string assetPath)
            : base(preferencePath)
        {
            settingsAssetPath = assetPath;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_Settings = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(settingsAssetPath);
            if (m_Settings != null)
                m_SettingsEditor = Editor.CreateEditor(m_Settings);
        }

        public override void OnDeactivate()
        {
            m_Settings = null;
        }

        public override void OnGUI(string searchContext)
        {
            if (m_SettingsEditor != null)
                m_SettingsEditor.OnInspectorGUI();

            base.OnGUI(searchContext);
        }

        public override bool HasSearchInterest(string searchContext)
        {
            foreach (var searchKeyword in SearchKeywords)
            {
                if (MatchSearch(searchKeyword, searchContext))
                    return true;
            }

            return false;
        }
    }
}
