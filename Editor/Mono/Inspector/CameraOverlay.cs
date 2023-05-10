// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.CameraPreviewUtils;

namespace UnityEditor
{
    [Overlay(id = k_OverlayID, displayName = k_DisplayName, defaultDisplay = true)]
    [Icon("Icons/Overlays/CameraPreview.png")]
    class SceneViewCameraOverlay : IMGUIOverlay
    {
        internal static bool forceDisable = false;

        // should match color in GizmosDrawers.cpp
        const float kPreviewNormalizedSize = 0.2f;
        const string k_OverlayID = "Scene View/Camera";
        const string k_DisplayName = "Camera Preview";

        Camera m_SelectedCamera;
        Camera selectedCamera => m_SelectedCamera;

        static Dictionary<Camera, (SceneViewCameraOverlay overlay, int count)> s_CameraOverlays = new Dictionary<Camera, (SceneViewCameraOverlay, int)>();

        SceneViewCameraOverlay(Camera camera)
        {
            minSize = new Vector2(40, 40);
            maxSize = new Vector2(4000, 4000);
            defaultSize = new Vector2(240, 135);
            m_SelectedCamera = camera;
            displayName = selectedCamera == null || string.IsNullOrEmpty(selectedCamera.name)
                ? "Camera Preview"
                : selectedCamera.name;

            s_CameraOverlays.Add(camera, (this ,1));
        }

        public static SceneViewCameraOverlay GetOrCreateCameraOverlay(Camera camera)
        {
            if (s_CameraOverlays.ContainsKey(camera))
            {
                var value = s_CameraOverlays[camera];
                value.count += 1;
                s_CameraOverlays[camera] = value;
                return value.overlay;
            }

            var overlay = new SceneViewCameraOverlay(camera);
            SceneView.AddOverlayToActiveView(overlay);
            return overlay;
        }

        public static void DisableCameraOverlay(Camera cam)
        {
            if (s_CameraOverlays.ContainsKey(cam))
            {
                var value = s_CameraOverlays[cam];
                value.count -= 1;
                if (value.count == 0)
                {
                    s_CameraOverlays.Remove(cam);
                    SceneView.RemoveOverlayFromActiveView(value.overlay);
                }
                else
                    s_CameraOverlays[cam] = value;
            }
        }

        void UpdateSize()
        {
            if (!sizeOverridden)
                size = new Vector2(240, 135);
        }

        public override void OnGUI()
        {
            if (selectedCamera == null)
            {
                GUILayout.Label("No camera selected", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (!CameraEditorUtils.IsViewportRectValidToRender(selectedCamera.rect))
                return;

            imguiContainer.style.flexGrow = 1;
            var sceneView = SceneView.lastActiveSceneView;

            // Do not render the Camera Preview overlay if the target camera GameObject is not part of the objects the
            // SceneView is rendering
            if (!sceneView.IsGameObjectInThisSceneView(selectedCamera.gameObject))
                return;

            var cameraRect = imguiContainer.rect;
            cameraRect.width = Mathf.Floor(cameraRect.width);

            if (cameraRect.width < 1 || cameraRect.height < 1 || float.IsNaN(cameraRect.width) || float.IsNaN(cameraRect.height))
                return;

            if (Event.current.type == EventType.Repaint)
            {
                Graphics.DrawTexture(cameraRect, Texture2D.whiteTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, Color.black);

                Vector2 previewSize = selectedCamera.targetTexture
                    ? new Vector2(selectedCamera.targetTexture.width, selectedCamera.targetTexture.height)
                    : PlayModeView.GetMainPlayModeViewTargetSize();

                if (previewSize.x < 0f)
                {
                    // Fallback to Scene View of not a valid game view size
                    previewSize.x = sceneView.position.width;
                    previewSize.y = sceneView.position.height;
                }

                float rectAspect = cameraRect.width / cameraRect.height;
                float previewAspect = previewSize.x / previewSize.y;
                Rect previewRect = cameraRect;
                if (rectAspect > previewAspect)
                {
                    float stretch = previewAspect / rectAspect;
                    previewRect = new Rect(cameraRect.xMin + cameraRect.width * (1.0f - stretch) * .5f, cameraRect.yMin, stretch * cameraRect.width, cameraRect.height);
                }
                else
                {
                    float stretch = rectAspect / previewAspect;
                    previewRect = new Rect(cameraRect.xMin, cameraRect.yMin + cameraRect.height * (1.0f - stretch) * .5f, cameraRect.width, stretch * cameraRect.height);
                }

                var settings = new PreviewSettings(new Vector2((int)previewRect.width, (int)previewRect.height));
                settings.overrideSceneCullingMask = sceneView.overrideSceneCullingMask;
                settings.scene = sceneView.customScene;

                var previewTexture = CameraPreviewUtils.GetPreview(selectedCamera, settings);

                Graphics.DrawTexture(previewRect, previewTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, EditorGUIUtility.GUITextureBlit2SRGBMaterial);
            }
        }
    }
}
