// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UnityEngine.Experimental.UIElements
{
    [Serializable]
    internal class VisualElementAsset : IUxmlAttributes
    {
        [SerializeField]
        private string m_Name;

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        [SerializeField]
        private int m_Id;

        public int id
        {
            get { return m_Id; }
            set { m_Id = value; }
        }

        [SerializeField]
        private int m_ParentId;

        public int parentId
        {
            get { return m_ParentId; }
            set { m_ParentId = value; }
        }

        [SerializeField]
        private int m_RuleIndex;

        public int ruleIndex
        {
            get { return m_RuleIndex; }
            set { m_RuleIndex = value; }
        }

        [SerializeField]
        private string m_Text;

        public string text
        {
            get { return m_Text; }
            set { m_Text = value; }
        }


        [SerializeField]
        private PickingMode m_PickingMode;

        public PickingMode pickingMode
        {
            get { return m_PickingMode; }
            set { m_PickingMode = value; }
        }

        [SerializeField]
        private string m_FullTypeName;

        public string fullTypeName
        {
            get { return m_FullTypeName; }
            set { m_FullTypeName = value; }
        }

        [SerializeField]
        private string[] m_Classes;

        public string[] classes
        {
            get { return m_Classes; }
            set { m_Classes = value; }
        }

        [SerializeField]
        private List<string> m_Stylesheets;

        public List<string> stylesheets
        {
            get { return m_Stylesheets == null ? (m_Stylesheets = new List<string>()) : m_Stylesheets; }
            set { m_Stylesheets = value; }
        }

        [SerializeField]
        private List<string> m_Properties;

        public VisualElementAsset(string fullTypeName)
        {
            m_FullTypeName = fullTypeName;
        }

        public VisualElement Create(CreationContext ctx)
        {
            Func<IUxmlAttributes, CreationContext, VisualElement> factory;
            if (!Factories.TryGetValue(fullTypeName, out factory))
            {
                Debug.LogErrorFormat("Visual Element Type '{0}' has no factory method.", fullTypeName);
                return new Label(string.Format("Unknown type: '{0}'", fullTypeName));
            }

            if (factory == null)
            {
                Debug.LogErrorFormat("Visual Element Type '{0}' has a null factory method.", fullTypeName);
                return new Label(string.Format("Type with no factory method: '{0}'", fullTypeName));
            }

            VisualElement res = factory(this, ctx);
            if (res == null)
            {
                Debug.LogErrorFormat("The factory of Visual Element Type '{0}' has returned a null object", fullTypeName);
                return new Label(string.Format("The factory of Visual Element Type '{0}' has returned a null object", fullTypeName));
            }
            res.name = name;

            if (classes != null)
            {
                for (int i = 0; i < classes.Length; i++)
                    res.AddToClassList(classes[i]);
            }

            if (stylesheets != null)
            {
                for (int i = 0; i < stylesheets.Count; i++)
                    res.AddStyleSheetPath(stylesheets[i]);
            }

            if (!string.IsNullOrEmpty(text))
                res.text = text;

            res.pickingMode = pickingMode;

            return res;
        }

        public void AddProperty(string propertyName, string propertyValue)
        {
            if (m_Properties == null)
                m_Properties = new List<string>();

            m_Properties.Add(propertyName);
            m_Properties.Add(propertyValue);
        }

        public string GetPropertyString(string propertyName)
        {
            if (m_Properties == null)
                return null;

            for (int i = 0; i < m_Properties.Count - 1; i += 2)
            {
                if (m_Properties[i] == propertyName)
                    return m_Properties[i + 1];
            }

            return null;
        }

        public int GetPropertyInt(string propertyName, int defaultValue)
        {
            var v = GetPropertyString(propertyName);
            int l;
            if (v == null || !int.TryParse(v, out l))
                return defaultValue;

            return l;
        }

        public bool GetPropertyBool(string propertyName, bool defaultValue)
        {
            var v = GetPropertyString(propertyName);
            bool l;
            if (v == null || !bool.TryParse(v, out l))
                return defaultValue;

            return l;
        }

        public Color GetPropertyColor(string propertyName, Color defaultValue)
        {
            var v = GetPropertyString(propertyName);
            Color32 l;
            if (v == null || !ColorUtility.DoTryParseHtmlColor(v, out l))
                return defaultValue;
            return l;
        }

        public long GetPropertyLong(string propertyName, long defaultValue)
        {
            var v = GetPropertyString(propertyName);
            long l;
            if (v == null || !long.TryParse(v, out l))
                return defaultValue;
            return l;
        }

        public float GetPropertyFloat(string propertyName, float def)
        {
            var v = GetPropertyString(propertyName);
            float l;
            if (v == null || !float.TryParse(v, out l))
                return def;
            return l;
        }

        public T GetPropertyEnum<T>(string propertyName, T def)
        {
            var v = GetPropertyString(propertyName);
            if (v == null || !Enum.IsDefined(typeof(T), v))
                return def;

            var l = (T)Enum.Parse(typeof(T), v);
            return l;
        }
    }
}
