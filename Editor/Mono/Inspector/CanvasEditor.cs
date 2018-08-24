// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    /// <summary>
    /// Editor class used to edit UI Canvases.
    /// </summary>

    [CanEditMultipleObjects]
    [CustomEditor(typeof(Canvas))]
    internal class CanvasEditor : Editor
    {
        SerializedProperty m_RenderMode;
        SerializedProperty m_Camera;
        SerializedProperty m_PixelPerfect;
        SerializedProperty m_PixelPerfectOverride;
        SerializedProperty m_PlaneDistance;
        SerializedProperty m_SortingLayerID;
        SerializedProperty m_SortingOrder;
        SerializedProperty m_TargetDisplay;
        SerializedProperty m_OverrideSorting;
        SerializedProperty m_ShaderChannels;

        AnimBool m_OverlayMode;
        AnimBool m_CameraMode;
        AnimBool m_WorldMode;

        AnimBool m_SortingOverride;

        private static class Styles
        {
            public static GUIContent eventCamera = new GUIContent("Event Camera", "The Camera which the events are triggered through. This is used to determine clicking and hover positions if the Canvas is in World Space render mode.");
            public static GUIContent renderCamera = new GUIContent("Render Camera", "The Camera which will render the canvas. This is also the camera used to send events.");
            public static GUIContent sortingOrder = new GUIContent("Sort Order", "The order in which Screen Space - Overlay canvas will render");
            public static string s_RootAndNestedMessage = "Cannot multi-edit root Canvas together with nested Canvas.";
            public static GUIContent m_SortingLayerStyle = EditorGUIUtility.TextContent("Sorting Layer|Name of the Renderer's sorting layer");
            public static GUIContent targetDisplay = new GUIContent("Target Display", "Display on which to render the canvas when in overlay mode");
            public static GUIContent m_SortingOrderStyle = EditorGUIUtility.TextContent("Order in Layer|Renderer's order within a sorting layer");
            public static GUIContent m_ShaderChannel = EditorGUIUtility.TextContent("Additional Shader Channels");
        }

        private bool m_AllNested = false;
        private bool m_AllRoot = false;

        private bool m_AllOverlay = false;
        private bool m_NoneOverlay = false;

        private string[] shaderChannelOptions = { "TexCoord1", "TexCoord2", "TexCoord3", "Normal", "Tangent" };


        enum PixelPerfect
        {
            Inherit,
            On,
            Off
        }

        private PixelPerfect pixelPerfect = PixelPerfect.Inherit;

        void OnEnable()
        {
            m_RenderMode = serializedObject.FindProperty("m_RenderMode");
            m_Camera = serializedObject.FindProperty("m_Camera");
            m_PixelPerfect = serializedObject.FindProperty("m_PixelPerfect");
            m_PlaneDistance = serializedObject.FindProperty("m_PlaneDistance");

            m_SortingLayerID = serializedObject.FindProperty("m_SortingLayerID");
            m_SortingOrder = serializedObject.FindProperty("m_SortingOrder");
            m_TargetDisplay = serializedObject.FindProperty("m_TargetDisplay");
            m_OverrideSorting = serializedObject.FindProperty("m_OverrideSorting");
            m_PixelPerfectOverride = serializedObject.FindProperty("m_OverridePixelPerfect");
            m_ShaderChannels = serializedObject.FindProperty("m_AdditionalShaderChannelsFlag");

            m_OverlayMode = new AnimBool(m_RenderMode.intValue == 0);
            m_OverlayMode.valueChanged.AddListener(Repaint);

            m_CameraMode = new AnimBool(m_RenderMode.intValue == 1);
            m_CameraMode.valueChanged.AddListener(Repaint);

            m_WorldMode = new AnimBool(m_RenderMode.intValue == 2);
            m_WorldMode.valueChanged.AddListener(Repaint);

            m_SortingOverride = new AnimBool(m_OverrideSorting.boolValue);
            m_SortingOverride.valueChanged.AddListener(Repaint);

            if (m_PixelPerfectOverride.boolValue)
                pixelPerfect = m_PixelPerfect.boolValue ? PixelPerfect.On : PixelPerfect.Off;
            else
                pixelPerfect = PixelPerfect.Inherit;

            m_AllNested = true;
            m_AllRoot = true;
            m_AllOverlay = true;
            m_NoneOverlay = true;

            for (int i = 0; i < targets.Length; i++)
            {
                Canvas canvas = targets[i] as Canvas;

                if (canvas.transform.parent == null || canvas.transform.parent.GetComponentInParent<Canvas>() == null)
                    m_AllNested = false;
                else
                    m_AllRoot = false;

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    m_NoneOverlay = false;
                else
                    m_AllOverlay = false;
            }
        }

        void OnDisable()
        {
            m_OverlayMode.valueChanged.RemoveListener(Repaint);
            m_CameraMode.valueChanged.RemoveListener(Repaint);
            m_WorldMode.valueChanged.RemoveListener(Repaint);
            m_SortingOverride.valueChanged.RemoveListener(Repaint);
        }

        private void AllRootCanvases()
        {
            if (PlayerSettings.virtualRealitySupported && (m_RenderMode.enumValueIndex == (int)RenderMode.ScreenSpaceOverlay))
            {
                EditorGUILayout.HelpBox("Using a render mode of ScreenSpaceOverlay while VR is enabled will cause the Canvas to continue to incur a rendering cost, even though the Canvas will not be visible in VR.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(m_RenderMode);

            m_OverlayMode.target = m_RenderMode.intValue == 0;
            m_CameraMode.target = m_RenderMode.intValue == 1;
            m_WorldMode.target = m_RenderMode.intValue == 2;

            EditorGUI.indentLevel++;
            if (EditorGUILayout.BeginFadeGroup(m_OverlayMode.faded))
            {
                EditorGUILayout.PropertyField(m_PixelPerfect);
                EditorGUILayout.PropertyField(m_SortingOrder, Styles.sortingOrder);
                GUIContent[] displayNames = DisplayUtility.GetDisplayNames();
                EditorGUILayout.IntPopup(m_TargetDisplay, displayNames, DisplayUtility.GetDisplayIndices(), Styles.targetDisplay);
            }
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_CameraMode.faded))
            {
                EditorGUILayout.PropertyField(m_PixelPerfect);
                EditorGUILayout.PropertyField(m_Camera, Styles.renderCamera);

                if (m_Camera.objectReferenceValue != null)
                    EditorGUILayout.PropertyField(m_PlaneDistance);

                EditorGUILayout.Space();

                if (m_Camera.objectReferenceValue != null)
                    EditorGUILayout.SortingLayerField(Styles.m_SortingLayerStyle, m_SortingLayerID, EditorStyles.popup, EditorStyles.label);
                EditorGUILayout.PropertyField(m_SortingOrder, Styles.m_SortingOrderStyle);

                if (m_Camera.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Screen Space - A canvas with no specified camera acts like a Overlay Canvas." +
                        " Please assign a camera to it in the 'Render Camera' field.", MessageType.Warning);
            }
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_WorldMode.faded))
            {
                EditorGUILayout.PropertyField(m_Camera, Styles.eventCamera);
                EditorGUILayout.Space();
                EditorGUILayout.SortingLayerField(Styles.m_SortingLayerStyle, m_SortingLayerID, EditorStyles.popup);
                EditorGUILayout.PropertyField(m_SortingOrder, Styles.m_SortingOrderStyle);
            }
            EditorGUILayout.EndFadeGroup();
            EditorGUI.indentLevel--;
        }

        private void AllNestedCanvases()
        {
            EditorGUI.BeginChangeCheck();
            pixelPerfect = (PixelPerfect)EditorGUILayout.EnumPopup("Pixel Perfect", pixelPerfect);

            if (EditorGUI.EndChangeCheck())
            {
                if (pixelPerfect == PixelPerfect.Inherit)
                {
                    m_PixelPerfectOverride.boolValue = false;
                }
                else if (pixelPerfect == PixelPerfect.Off)
                {
                    m_PixelPerfectOverride.boolValue = true;
                    m_PixelPerfect.boolValue = false;
                }
                else
                {
                    m_PixelPerfectOverride.boolValue = true;
                    m_PixelPerfect.boolValue = true;
                }
            }

            // we need to pass through the property setter to trigger a canvasHierarchyChanged event and so on
            // see case 787195
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_OverrideSorting);
            if (EditorGUI.EndChangeCheck())
            {
                ((Canvas)serializedObject.targetObject).overrideSorting = m_OverrideSorting.boolValue;
                m_SortingOverride.target = m_OverrideSorting.boolValue;
            }

            if (EditorGUILayout.BeginFadeGroup(m_SortingOverride.faded))
            {
                GUIContent sortingOrderStyle = null;
                if (m_AllOverlay)
                {
                    sortingOrderStyle = Styles.sortingOrder;
                }
                else if (m_NoneOverlay)
                {
                    sortingOrderStyle = Styles.m_SortingOrderStyle;
                    EditorGUILayout.SortingLayerField(Styles.m_SortingLayerStyle, m_SortingLayerID, EditorStyles.popup);
                }
                if (sortingOrderStyle != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_SortingOrder, sortingOrderStyle);
                    if (EditorGUI.EndChangeCheck())
                        ((Canvas)serializedObject.targetObject).sortingOrder = m_SortingOrder.intValue;
                }
            }
            EditorGUILayout.EndFadeGroup();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (m_AllRoot || m_AllNested)
            {
                if (m_AllRoot)
                {
                    AllRootCanvases();
                }
                else if (m_AllNested)
                {
                    AllNestedCanvases();
                }

                int newShaderChannelValue = 0;
                EditorGUI.BeginChangeCheck();
                newShaderChannelValue = EditorGUILayout.MaskField(Styles.m_ShaderChannel, m_ShaderChannels.intValue, shaderChannelOptions);


                if (EditorGUI.EndChangeCheck())
                    m_ShaderChannels.intValue = newShaderChannelValue;

                if (m_RenderMode.intValue == 0) // Overlay canvas
                {
                    if (((newShaderChannelValue & (int)AdditionalCanvasShaderChannels.Normal) | (newShaderChannelValue & (int)AdditionalCanvasShaderChannels.Tangent)) != 0)
                        EditorGUILayout.HelpBox("Shader channels Normal and Tangent are most often used with lighting, which an Overlay canvas does not support. Its likely these channels are not needed.", MessageType.Warning);
                }
            }
            else
            {
                GUILayout.Label(Styles.s_RootAndNestedMessage, EditorStyles.helpBox);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
