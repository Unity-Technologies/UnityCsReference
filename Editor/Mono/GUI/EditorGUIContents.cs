// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Reflection;

namespace UnityEditor
{
    public sealed partial class EditorGUI
    {
        // Common GUIContents used for EditorGUI controls.
        internal sealed class GUIContents
        {
            // The settings dropdown icon top right in a component
            [IconName("_Popup")]
            internal static GUIContent titleSettingsIcon { get; private set; }

            // The help icon in a component
            [IconName("_Help")]
            internal static GUIContent helpIcon  { get; private set; }

            // We use a static constructor to lazily initialize all static properties. This is useful because changed image files can then
            // be picked up on assembly reload.
            static GUIContents()
            {
                // Run through each static property and initialize it using the
                // filename provided in the IconName Attribute.
                PropertyInfo[] propertyInfos = typeof(GUIContents).GetProperties(System.Reflection.BindingFlags.Static | BindingFlags.NonPublic);
                foreach (PropertyInfo property in propertyInfos)
                {
                    IconName[] iconNames = (IconName[])property.GetCustomAttributes(typeof(IconName), false);
                    if (iconNames.Length > 0)
                    {
                        string name = iconNames[0].name;
                        GUIContent content = EditorGUIUtility.IconContent(name);
                        property.SetValue(null, content, null);
                    }
                }
            }

            private class IconName : System.Attribute
            {
                private string m_Name;

                public IconName(string name)
                {
                    this.m_Name = name;
                }

                public virtual string name
                {
                    get { return m_Name; }
                }
            }
        }
    }
}
