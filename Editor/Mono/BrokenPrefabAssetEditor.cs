// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    [CustomEditor(typeof(BrokenPrefabAsset))]
    class BrokenPrefabAssetEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();

            var brokenPrefab = (BrokenPrefabAsset)target;

            container.Add(new HelpBox(
                brokenPrefab.message,
                brokenPrefab.isWarning ? HelpBoxMessageType.Error : HelpBoxMessageType.Info));

            if (brokenPrefab.isVariant)
            {
                var brokenPrefabParent = brokenPrefab.brokenPrefabParent;
                var field = new ObjectField("Variant Parent");
                field.SetEnabled(false);
                if(brokenPrefabParent != null)
                {
                    field.value = brokenPrefabParent;
                    field.objectType = typeof(BrokenPrefabAsset);
                }
                else
                {
                    //Hack to display "Missing" instead of "None"
                    var missingGameObject = EditorUtility.CreateGameObjectWithHideFlags("Missing GameObject for Object Field", HideFlags.HideAndDontSave);
                    DestroyImmediate(missingGameObject);
                    field.value = missingGameObject;
                    field.objectType = typeof(GameObject);
                }

                container.Add(field);
            }

            return container;
        }
    }
}
