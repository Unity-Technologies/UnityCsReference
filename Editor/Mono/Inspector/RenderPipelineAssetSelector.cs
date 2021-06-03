// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    /// <summary>
    /// Provides a display for a <see cref="EditorGUI.ObjectField"/> and prompts the user to confirm the change.
    /// </summary>
    internal static class RenderPipelineAssetSelector
    {
        static class Styles
        {
            public static readonly GUIContent renderPipeLabel = EditorGUIUtility.TrTextContent("Scriptable Render Pipeline");

            public static string renderPipeChangedWarning => LocalizationDatabase.GetLocalizedString("Changing this render pipeline asset may take a significant amount of time.");
            public static string renderPipeChangedTitleBox => LocalizationDatabase.GetLocalizedString("Changing Render Pipeline");
            public static string renderPipeChangedConfirmation => LocalizationDatabase.GetLocalizedString("Continue");
            public static string cancelLabel => LocalizationDatabase.GetLocalizedString("Cancel");
        }

        static void AskRenderPipelineChangeConfirmation(SerializedObject serializedObject, SerializedProperty serializedProperty, Object selectedRenderPipelineAsset)
        {
            if (selectedRenderPipelineAsset == serializedProperty.objectReferenceValue)
                return;

            if (EditorUtility.DisplayDialog(Styles.renderPipeChangedTitleBox, Styles.renderPipeChangedWarning, Styles.renderPipeChangedConfirmation, Styles.cancelLabel))
            {
                serializedProperty.objectReferenceValue = selectedRenderPipelineAsset;
                serializedObject.ApplyModifiedProperties();
            }

            GUIUtility.ExitGUI();
        }

        static void ApplyChangeIfNeeded(SerializedObject serializedObject, SerializedProperty serializedProperty, Object selectedRenderPipelineAsset)
        {
            if (!ObjectSelector.isVisible)
            {
                AskRenderPipelineChangeConfirmation(serializedObject, serializedProperty, selectedRenderPipelineAsset);
                return;
            }

            if (Event.current.type == EventType.ExecuteCommand &&
                Event.current.commandName == ObjectSelector.ObjectSelectorClosedCommand)
            {
                AskRenderPipelineChangeConfirmation(serializedObject, serializedProperty, ObjectSelector.GetCurrentObject());
                Event.current.Use();
                GUIUtility.ExitGUI();
            }
        }

        /// <summary>
        /// Draws the object field and, if the user attempts to change the value, asks the user for confirmation.
        /// </summary>
        /// <param name="content">The label.</param>
        /// <param name="serializedObject">The <see cref="SerializedObject"/> that holds the <see cref="SerializedProperty"/> with the new render pipeline asset.</param>
        /// <param name="serializedProperty">The <see cref="SerializedProperty"/> to modify with the new render pipeline asset.</param>
        public static void Draw(GUIContent content, SerializedObject serializedObject, SerializedProperty serializedProperty)
        {
            var selectedRenderPipelineAsset = EditorGUILayout.ObjectField(content, serializedProperty.objectReferenceValue, typeof(RenderPipelineAsset), false);
            ApplyChangeIfNeeded(serializedObject, serializedProperty, selectedRenderPipelineAsset);
        }

        /// <summary>
        /// Draws the object field and, if the user attempts to change the value, asks the user for confirmation.
        /// </summary>
        /// <param name="serializedObject">TThe <see cref="SerializedObject"/> that holds the <see cref="SerializedProperty"/> with the new render pipeline asset.</param>
        /// <param name="serializedProperty">The <see cref="SerializedProperty"/> to modify with the new render pipeline asset.</param>
        public static void Draw(SerializedObject serializedObject, SerializedProperty serializedProperty)
        {
            Rect renderLoopRect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(serializedProperty));

            EditorGUI.BeginProperty(renderLoopRect, Styles.renderPipeLabel, serializedProperty);

            var selectedRenderPipelineAsset = EditorGUI.ObjectField(renderLoopRect, serializedProperty.objectReferenceValue, typeof(RenderPipelineAsset), false);
            ApplyChangeIfNeeded(serializedObject, serializedProperty, selectedRenderPipelineAsset);

            EditorGUI.EndProperty();
        }
    }
}
