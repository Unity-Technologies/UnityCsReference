// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    public enum SettingsScope
    {
        User,
        Project
    }

    public class SettingsProvider
    {
        private string m_Label;
        private string m_Name;
        private HashSet<string> m_Keywords;

        internal SettingsWindow settingsWindow { get; set; }
        internal string[] pathTokens { get; }
        internal Texture2D icon { get; set; }


        public string label
        {
            get
            {
                if (String.IsNullOrEmpty(m_Label))
                    return m_Name;
                return m_Label;
            }
            set { m_Label = L10n.Tr(value); }
        }

        public string settingsPath { get; }
        public SettingsScope scope { get; }
        public IEnumerable<string> keywords
        {
            get { return m_Keywords; }
            set
            {
                m_Keywords = new HashSet<string>(value);
            }
        }

        public Action<string> guiHandler { get; set; }
        public Action titleBarGuiHandler { get; set; }
        public Action footerBarGuiHandler { get; set; }
        public Action<string, VisualElement> activateHandler { get; set; }
        public Action deactivateHandler { get; set; }
        public Func<string, bool> hasSearchInterestHandler { get; set; }
        public Action inspectorUpdateHandler { get; set; }

        public SettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
        {
            settingsPath = path.Replace("\n", " ").Replace("\\", "/");
            var nameIndex = settingsPath.LastIndexOf("/");
            var name = settingsPath;
            if (nameIndex != -1)
            {
                name = settingsPath.Substring(nameIndex + 1);
            }
            m_Name = L10n.Tr(name);

            pathTokens = settingsPath.Split('/');
            this.scope = scopes;
            m_Keywords = keywords == null ? new HashSet<string>() : new HashSet<string>(keywords);
        }

        public virtual void OnActivate(string searchContext, VisualElement rootElement)
        {
            activateHandler?.Invoke(searchContext, rootElement);
        }

        public virtual void OnDeactivate()
        {
            deactivateHandler?.Invoke();
        }

        public virtual bool HasSearchInterest(string searchContext)
        {
            if (hasSearchInterestHandler != null)
            {
                return hasSearchInterestHandler.Invoke(searchContext);
            }

            foreach (var searchKeyword in keywords)
            {
                if (SearchUtils.MatchSearchGroups(searchContext, searchKeyword))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual void OnGUI(string searchContext)
        {
            guiHandler?.Invoke(searchContext);
        }

        public virtual void OnTitleBarGUI()
        {
            titleBarGuiHandler?.Invoke();
        }

        public virtual void OnFooterBarGUI()
        {
            footerBarGuiHandler?.Invoke();
        }

        public virtual void OnInspectorUpdate()
        {
            inspectorUpdateHandler?.Invoke();
        }

        public void Repaint()
        {
            settingsWindow?.Repaint();
        }

        public void PopulateSearchKeywordsFromGUIContentProperties<T>()
        {
            keywords = GetSearchKeywordsFromGUIContentProperties<T>();
        }

        #region Helper
        public static IEnumerable<string> GetSearchKeywordsFromGUIContentProperties<T>()
        {
            return typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(field => typeof(GUIContent).IsAssignableFrom(field.FieldType))
                .Select(field => ((GUIContent)field.GetValue(null)).text)
                .Concat(typeof(T).GetProperties(BindingFlags.Static | BindingFlags.Public)
                    .Where(prop => typeof(GUIContent).IsAssignableFrom(prop.PropertyType))
                    .Select(prop => ((GUIContent)prop.GetValue(null, null)).text))
                .Where(content => content != null)
                .Select(content => content.ToLowerInvariant())
                .Distinct();
        }

        public static IEnumerable<string> GetSearchKeywordsFromSerializedObject(SerializedObject serializedObject)
        {
            var keywords = new HashSet<string>();
            var property = serializedObject.GetIterator();
            while (property.NextVisible(true))
            {
                keywords.Add(property.displayName);
            }
            return keywords;
        }

        public static IEnumerable<string> GetSearchKeywordsFromPath(string path)
        {
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj != null)
            {
                return GetSearchKeywordsFromSerializedObject(new SerializedObject(obj));
            }

            return new string[] {};
        }

        #endregion
    }
}
