// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Internal;

namespace UnityEditor
{
    [ExcludeFromDocs, Flags]
    public enum SettingsScopes : uint
    {
        None = 0,

        Any = 0xFFFFFFFF,

        User = 1U << 1,
        Project = 1U << 2,

        BuiltIn = 1U << 10,
        Package = 1U << 11,

        // User defined
        Custom1 = 1U << 21,
        Custom2 = 1U << 22,
        Custom3 = 1U << 23,
        Custom4 = 1U << 24,
        Custom5 = 1U << 25,
        Custom6 = 1U << 26,
        Custom7 = 1U << 27,
        Custom8 = 1U << 28,
        Custom9 = 1U << 29,

        Reserved = 1U << 30,
        Reserved1 = 1U << 31
    }

    [ExcludeFromDocs]
    public class SettingsProvider
    {
        private string m_Label;

        internal SettingsWindow settingsWindow { get; set; }

        public string name { get; set; }

        public string label
        {
            get
            {
                if (String.IsNullOrEmpty(m_Label))
                    return name;
                return m_Label;
            }
            set { m_Label = value; }
        }

        public string settingsPath { get; set; }
        public string[] pathTokens { get; }
        public Texture2D icon { get; set; }
        public HashSet<string> keywords { get; set; }
        public SettingsScopes scopes { get; set; }

        public Action<string> guiHandler { get; set; }
        public Action titleBarGuiHandler { get; set; }
        public Action<string, VisualElement> activateHandler { get; set; }
        public Action deactivateHandler { get; set; }
        public Func<string, bool> hasSearchInterestHandler { get; set; }

        public SettingsProvider(string path, SettingsScopes scopes = SettingsScopes.Any)
        {
            settingsPath = path;
            name = Path.GetFileName(settingsPath);
            pathTokens = settingsPath.Split('/');
            this.scopes = scopes;
            keywords = new HashSet<string>();
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
                if (SearchUtils.FuzzySearch(searchContext, searchKeyword))
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

        public void PopulateSearchKeywordsFromGUIContentProperties<T>()
        {
            GetSearchKeywordsFromGUIContentProperties<T>(keywords);
        }

        #region Helper
        public static void GetSearchKeywordsFromGUIContentProperties<T>(ICollection<string> searchKeywords)
        {
            var keywords = typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(field => typeof(GUIContent).IsAssignableFrom(field.FieldType))
                .Select(field => ((GUIContent)field.GetValue(null)).text)
                .Concat(typeof(T).GetProperties(BindingFlags.Static | BindingFlags.Public)
                    .Where(prop => typeof(GUIContent).IsAssignableFrom(prop.PropertyType))
                    .Select(prop => ((GUIContent)prop.GetValue(null, null)).text))
                .Where(content => content != null)
                .Select(content => content.ToLowerInvariant());

            foreach (var keyword in keywords)
            {
                searchKeywords.Add(keyword);
            }
        }

        public static void GetSearchKeywordsFromSerializedObject(SerializedObject serializedObject, ICollection<string> searchKeywords)
        {
            var property = serializedObject.GetIterator();
            while (property.NextVisible(true))
            {
                searchKeywords.Add(property.displayName);
            }
        }

        #endregion
    }
}
