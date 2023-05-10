// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    static class ViewControllerUtility
    {
        // Loads the specified Uxml asset and returns its root VisualElement, discarding the Template container. If the Uxml specifies multiple roots, the first will be returned.
        public static VisualElement LoadVisualTreeFromUxmlAsset(string uxmlAssetGuid)
        {
            // Load Uxml template from disk.
            var uxmlAssetPath = AssetDatabase.GUIDToAssetPath(uxmlAssetGuid);
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlAssetPath);
            return LoadViewFromUxml(uxml);
        }

        // Loads the specified built-in Uxml and returns its root VisualElement, discarding the Template container. If the Uxml specifies multiple roots, the first will be returned.
        public static VisualElement LoadVisualTreeFromBuiltInUxml(string uxmlResourceName)
        {
            // Load Uxml template from disk.
            var uxml = EditorGUIUtility.Load(uxmlResourceName) as VisualTreeAsset;
            return LoadViewFromUxml(uxml);
        }

        static VisualElement LoadViewFromUxml(VisualTreeAsset uxml)
        {
            var template = uxml.Instantiate();

            // Retrieve first child from template container.
            VisualElement view = null;
            using (var enumerator = template.Children().GetEnumerator())
            {
                if (enumerator.MoveNext())
                    view = enumerator.Current;
            }

            return view;
        }
    }
}
