// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace UnityEditor
{
    internal class PrefabInspector
    {
        public static void OnOverridenPrefabsInspector(GameObject gameObject)
        {
            GUI.enabled = true;

            var prefab = PrefabUtility.GetPrefabObject(gameObject);
            if (prefab == null)
                return;

            EditorGUIUtility.labelWidth = 200;

            if (PrefabUtility.GetPrefabType(gameObject) == PrefabType.PrefabInstance)
            {
                PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(gameObject);
                if (modifications != null && modifications.Length != 0)
                {
                    GUI.changed = false;
                    for (int i = 0; i < modifications.Length; i++)
                    {
                        modifications[i].value = EditorGUILayout.TextField(modifications[i].propertyPath, modifications[i].value);
                    }

                    if (GUI.changed)
                    {
                        PrefabUtility.SetPropertyModifications(gameObject, modifications);
                    }
                }
            }

            AddComponentGUI(prefab);
        }

        static void AddComponentGUI(Object prefab)
        {
            var serialized = new SerializedObject(prefab);

            var i = serialized.FindProperty("m_Modification");
            var end = i.GetEndProperty();
            while (true)
            {
                var expanded = EditorGUILayout.PropertyField(i);

                if (!i.NextVisible(expanded))
                    break;
                if (SerializedProperty.EqualContents(i, end))
                    break;
            }

            serialized.ApplyModifiedProperties();
        }
    }
}
