// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class SerializedPropertyFilters
    {
        internal interface IFilter
        {
            bool Active();                          // returns true if filter is active, false otherwise
            bool Filter(SerializedProperty prop);   // returns true if filtering passes
            void OnGUI(Rect r);                     // draws the filter control
            string SerializeState();                // returns null if there's nothing to serialize
            void DeserializeState(string state);    // state must not be null
        }

        internal abstract class SerializableFilter : IFilter
        {
            public abstract bool Active();                          // returns true if filter is active, false otherwise
            public abstract bool Filter(SerializedProperty prop);   // returns true if filtering passes
            public abstract void OnGUI(Rect r);                     // draws the filter control
            public string SerializeState() { return JsonUtility.ToJson(this); }
            public void DeserializeState(string state) { JsonUtility.FromJsonOverwrite(state, this); }
        };


        internal class String : SerializableFilter
        {
            static class Styles
            {
                public static readonly GUIStyle searchField = "SearchTextField";
                public static readonly GUIStyle searchFieldCancelButton = "SearchCancelButton";
                public static readonly GUIStyle searchFieldCancelButtonEmpty = "SearchCancelButtonEmpty";
            }

            [SerializeField] protected string m_Text = "";
            public override bool Active() { return !string.IsNullOrEmpty(m_Text); }
            public override bool Filter(SerializedProperty prop) { return prop.stringValue.IndexOf(m_Text, 0, System.StringComparison.OrdinalIgnoreCase) >= 0; }
            public override void OnGUI(Rect r)
            {
                r.width -= 15;
                m_Text = EditorGUI.TextField(r, GUIContent.none, m_Text, Styles.searchField);

                // draw the cancel button
                r.x += r.width;
                r.width = 15;
                bool notEmpty = m_Text != "";
                if (GUI.Button(r, GUIContent.none, notEmpty ? Styles.searchFieldCancelButton : Styles.searchFieldCancelButtonEmpty) && notEmpty)
                {
                    m_Text = "";
                    GUIUtility.keyboardControl = 0;
                }
            }
        }

        internal sealed class Name : String
        {
            public bool Filter(string str) { return str.IndexOf(m_Text, 0, System.StringComparison.OrdinalIgnoreCase) >= 0; }
        }

        internal sealed class None : IFilter
        {
            public bool Active() { return false; }
            public bool Filter(SerializedProperty prop) { return true; }
            public void OnGUI(Rect r) {}
            public string SerializeState() { return null; }
            public void DeserializeState(string state) {}
        }
        internal static readonly None s_FilterNone = new None();
    }
}
