// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Connect.Fallback
{
    static class VisualElementUtils
    {
        public static void AddUxmlToVisualElement(VisualElement visualElementContainer, string uxmlPath)
        {
            var visualTreeAsset = EditorGUIUtility.Load(uxmlPath) as VisualTreeAsset;
            if (visualTreeAsset != null)
            {
                visualTreeAsset.CloneTree(visualElementContainer);
            }
        }
    }
}
