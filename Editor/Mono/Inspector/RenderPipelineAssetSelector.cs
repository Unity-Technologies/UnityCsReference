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
    public sealed partial class EditorGUI
    {
        static class RenderPipelineAssetSelectorStyles
        {
            public static readonly GUIContent renderPipeLabel = EditorGUIUtility.TrTextContent("Scriptable Render Pipeline");

            public static string renderPipeChangedWarning => LocalizationDatabase.GetLocalizedString("Changing this render pipeline asset may take a significant amount of time.");
            public static string renderPipeChangedTitleBox => LocalizationDatabase.GetLocalizedString("Changing Render Pipeline");
            public static string renderPipeChangedConfirmation => LocalizationDatabase.GetLocalizedString("Continue");
            public static string cancelLabel => LocalizationDatabase.GetLocalizedString("Cancel");
        }

        static void PromptConfirmation(SerializedObject serializedObject, SerializedProperty serializedProperty, Object selectedRenderPipelineAsset)
        {
            if (selectedRenderPipelineAsset == serializedProperty.objectReferenceValue)
                return;

            if (EditorUtility.DisplayDialog(RenderPipelineAssetSelectorStyles.renderPipeChangedTitleBox, RenderPipelineAssetSelectorStyles.renderPipeChangedWarning, RenderPipelineAssetSelectorStyles.renderPipeChangedConfirmation, RenderPipelineAssetSelectorStyles.cancelLabel))
            {
                serializedProperty.objectReferenceValue = selectedRenderPipelineAsset;
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Draws the object field and, if the user attempts to change the value, asks the user for confirmation.
        /// </summary>
        /// <param name="content">The label.</param>
        /// <param name="serializedObject">The <see cref="SerializedObject"/> that holds the <see cref="SerializedProperty"/> with the new render pipeline asset.</param>
        /// <param name="serializedProperty">The <see cref="SerializedProperty"/> to modify with the new render pipeline asset.</param>
        internal static void RenderPipelineAssetField(GUIContent content, SerializedObject serializedObject, SerializedProperty serializedProperty)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(content);
                RenderPipelineAssetField(serializedObject, serializedProperty);
            }
        }

        /// <summary>
        /// Draws the object field and, if the user attempts to change the value, asks the user for confirmation.
        /// </summary>
        /// <param name="serializedObject">The <see cref="SerializedObject"/> that holds the <see cref="SerializedProperty"/> with the new render pipeline asset.</param>
        /// <param name="serializedProperty">The <see cref="SerializedProperty"/> to modify with the new render pipeline asset.</param>
        internal static void RenderPipelineAssetField(SerializedObject serializedObject, SerializedProperty serializedProperty)
        {
            Rect renderLoopRect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(serializedProperty));

            EditorGUI.BeginProperty(renderLoopRect, RenderPipelineAssetSelectorStyles.renderPipeLabel, serializedProperty);

            int id = GUIUtility.GetControlID(s_ObjectFieldHash, FocusType.Keyboard, renderLoopRect);

            var selectedRenderPipelineAsset = DoObjectField(
                position: IndentedRect(renderLoopRect),
                dropRect: IndentedRect(renderLoopRect),
                id: id,
                obj: serializedProperty.objectReferenceValue,
                objBeingEdited: null,
                objType: typeof(RenderPipelineAsset),
                additionalType: null,
                property: null,
                validator: null,
                allowSceneObjects: false,
                style: EditorStyles.objectField,
                onObjectSelectorClosed: obj =>
                {
                    if (!ObjectSelector.SelectionCanceled())
                        PromptConfirmation(serializedObject, serializedProperty, obj);
                });

            if (!ObjectSelector.isVisible)
            {
                PromptConfirmation(serializedObject, serializedProperty, selectedRenderPipelineAsset);
            }

            EditorGUI.EndProperty();
        }
    }
}
