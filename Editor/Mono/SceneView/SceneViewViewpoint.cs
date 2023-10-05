// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using UnityEditor.ShortcutManagement;
using UObject = UnityEngine.Object;

namespace UnityEditor
{
    [Serializable]
    internal class SceneViewViewpoint
    {
        const float k_MouseWheelScaleSensitivityMultiplier = .01f;
        const string k_CameraPreviewShortcutIdPrefix = "Scene View/Camera View/";
        const int k_CameraFrameThickness = 2;

        [Serializable]
        internal class ViewpointSettings
        {
            internal const float defaultScale = 1.0f;
            internal const float minScale = 0.001f;
            internal const float maxScale = 10.0f;

            internal const int defaultOpacity = 50;
            internal const int minOpacity = 0;
            internal const int maxOpacity = 100;

            [SerializeField] int m_Opacity;
            [SerializeField] float m_Scale;

            internal int opacity
            {
                get => m_Opacity;
                set => m_Opacity = Mathf.Clamp(value, minOpacity, maxOpacity);
            }

            internal float scale
            {
                get => m_Scale;
                set
                {
                    m_Scale = Mathf.Clamp(value, minScale, maxScale);
                }
            }

            internal ViewpointSettings(int defaultOpacity = defaultOpacity, float defaultScale = defaultScale)
            {
                m_Opacity = defaultOpacity;
                m_Scale = defaultScale;
            }
        }

        internal event Action<bool> cameraLookThroughStateChanged;

        [SerializeField] SceneView m_SceneView;
        [SerializeField] IViewpoint m_ActiveViewpoint;
        [SerializeField] ViewpointSettings m_CameraOverscanSettings;

        Camera m_TemporaryCamera;

        Camera temporaryCamera
        {
            get
            {
                if (m_TemporaryCamera == null)
                {
                    m_TemporaryCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera",
                        HideFlags.HideAndDontSave,
                        typeof(Camera)).GetComponent<Camera>();
                    m_TemporaryCamera.enabled = false;
                    m_TemporaryCamera.cameraType = CameraType.Preview;
                }
                return m_TemporaryCamera;
            }
        }

        bool viewpointContextIsActive => SceneViewMotion.SceneViewViewport.IsActive
            && hasActiveViewpoint;

        bool shouldExitViewpoint => m_ActiveViewpoint != null && !m_ActiveViewpoint.TargetObject;

        internal IViewpoint activeViewpoint => m_ActiveViewpoint;

        internal bool hasActiveViewpoint => m_ActiveViewpoint != null && m_ActiveViewpoint.TargetObject;

        internal ViewpointSettings cameraOverscanSettings
        {
            get => m_CameraOverscanSettings;
            set => m_CameraOverscanSettings = value;
        }

        internal void AssignSceneView(SceneView sceneView)
        {
            m_SceneView = sceneView;
        }

        internal SceneViewViewpoint()
        {
            m_CameraOverscanSettings = new ViewpointSettings();
        }

        internal void SetViewpoint(IViewpoint view)
        {
            m_ActiveViewpoint = view;
            cameraLookThroughStateChanged?.Invoke(true);
        }

        internal void ClearViewpoint()
        {
            if (m_ActiveViewpoint == null)
                return;

            // When the Editor goes into PlayMode, it destroyes all the overlays.
            // At that specific moment, the SceneView's camera could be null.
            if (m_ActiveViewpoint.TargetObject && m_SceneView.camera)
            {
                m_SceneView.LookAt(
                    m_ActiveViewpoint.Position + (m_ActiveViewpoint.Rotation * Vector3.forward) * m_SceneView.CalcCameraDist(),
                    m_ActiveViewpoint.Rotation,
                    m_SceneView.size,
                    false,
                    true);
            }

            m_ActiveViewpoint = null;

            if (m_TemporaryCamera)
                UObject.DestroyImmediate(m_TemporaryCamera.gameObject);

            cameraLookThroughStateChanged?.Invoke(false);
        }

        // When in Camera view.
        // Transfer Camera's transform to the Viewpoint's transform when the SceneView's camera moves.
        // When the active Viewpoint moves (i.e from scripts or animation), transfer the data to the SceneView's Camera.
        internal void UpdateViewpointMotion(SceneView sceneView, bool sceneViewTransformIsAnimating)
        {
            // Exit the viewpoint if viewpoint is unlocked and
            // Scene View camera is moving.
            if (shouldExitViewpoint)
            {
                ClearViewpoint();
                return;
            }

            if (!hasActiveViewpoint)
                return;

            if (sceneView.sceneViewMotion.cameraSpeed.sqrMagnitude > Mathf.Epsilon
                    || sceneView.sceneViewMotion.isDragging
                    || sceneViewTransformIsAnimating)
            {
                // Camera is moving in the Scene View. Align SceneView's Camera to Viewpoint.
                TryMoveViewpoint();
            }
            else
            {
                // Align the Viewpoint's transform to the SceneView's Camera when no system is driving the Scene View.
                AlignSceneViewToViewpoint();
            }
        }

        // Applies the active viewpoint camera properties to the camera.
        internal void ApplyCameraLensFromViewpoint(bool sceneViewIsPerspective)
        {
            var lensData = activeViewpoint as ICameraLensData;

            if (lensData == null)
                return;

            m_SceneView.camera.nearClipPlane = lensData.NearClipPlane;
            m_SceneView.camera.farClipPlane = lensData.FarClipPlane;

            float scale = cameraOverscanSettings.scale;

            if (sceneViewIsPerspective)
            {
                // Perspective camera scope
                if (lensData.Orthographic == true)
                {
                    Undo.RecordObject(activeViewpoint.TargetObject, $"Camera {activeViewpoint.TargetObject.name} set to perspective.");
                    lensData.Orthographic = false;
                }

                m_SceneView.camera.orthographic = false;
                m_SceneView.camera.fieldOfView = m_SceneView.GetVerticalFOV(lensData.FieldOfView, scale);
            }
            else
            {
                // Orthographic camera scope
                if (lensData.Orthographic == false)
                {
                    Undo.RecordObject(activeViewpoint.TargetObject, $"Camera {activeViewpoint.TargetObject.name} set to orthographic.");
                    lensData.Orthographic = true;
                }

                m_SceneView.camera.orthographic = true;
                m_SceneView.camera.orthographicSize = lensData.OrthographicSize * scale;
            }
        }

        internal void OnGUIDrawCameraOverscan()
        {
            if (activeViewpoint?.TargetObject == null)
                return;

            var lensData = activeViewpoint as ICameraLensData;

            if (lensData == null)
                return;

            Camera camera = activeViewpoint.TargetObject as Camera;

            if (camera == null)
            {
                camera = temporaryCamera.GetComponent<Camera>();

                ViewpointUtility.ApplyTransformData(activeViewpoint, camera.transform);
                ViewpointUtility.ApplyCameraLensData(lensData, camera);
            }

            Vector3[] nearPlaneCorners = new Vector3[4];

            // Todo: to update with Karl to remove dependency on camera reference.
            CameraEditorUtils.TryGetFrustum(camera, nearPlaneCorners, null, out float _);

            Rect viewport = m_SceneView.cameraViewport;
            Color previous = GUI.color;

            Handles.BeginGUI();

            var pp0gui = HandleUtility.WorldToGUIPoint(nearPlaneCorners[0]);
            var pp1gui = HandleUtility.WorldToGUIPoint(nearPlaneCorners[1]);
            var pp2gui = HandleUtility.WorldToGUIPoint(nearPlaneCorners[2]);
            var pp3gui = HandleUtility.WorldToGUIPoint(nearPlaneCorners[3]);

            pp0gui = new Vector2(Mathf.RoundToInt(pp0gui.x), Mathf.RoundToInt(pp0gui.y));
            pp1gui = new Vector2(Mathf.RoundToInt(pp1gui.x), Mathf.RoundToInt(pp1gui.y));
            pp2gui = new Vector2(Mathf.RoundToInt(pp2gui.x), Mathf.RoundToInt(pp2gui.y));
            pp3gui = new Vector2(Mathf.RoundToInt(pp3gui.x), Mathf.RoundToInt(pp3gui.y));

            // Draw overscan
            GUI.color = new Color(1f, 1f, 1f, (float)cameraOverscanSettings.opacity / 100f);
            Rect top2 = Rect.MinMaxRect(0, 0, viewport.width, pp1gui.y);
            Rect bottom2 = Rect.MinMaxRect(0, pp3gui.y, viewport.width, viewport.height);
            Rect right2 = Rect.MinMaxRect(pp3gui.x, top2.yMax, viewport.width, bottom2.yMin);
            Rect left2 = Rect.MinMaxRect(0f, top2.yMax, pp1gui.x, bottom2.yMin);

            GUI.DrawTexture(top2, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(bottom2, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(left2, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(right2, Texture2D.whiteTexture, ScaleMode.StretchToFill);

            // Draw camera frame
            Rect topHardEdge = Rect.MinMaxRect(pp1gui.x - k_CameraFrameThickness, pp1gui.y - k_CameraFrameThickness, pp2gui.x + k_CameraFrameThickness, pp1gui.y);
            Rect bottomHardEdge = Rect.MinMaxRect(pp0gui.x - k_CameraFrameThickness, pp0gui.y, pp3gui.x + k_CameraFrameThickness, pp0gui.y + k_CameraFrameThickness);
            Rect leftHardEdge = Rect.MinMaxRect(pp1gui.x - k_CameraFrameThickness, pp1gui.y - k_CameraFrameThickness, pp1gui.x, pp0gui.y + k_CameraFrameThickness);
            Rect rightHardEdge = Rect.MinMaxRect(pp2gui.x, pp2gui.y - k_CameraFrameThickness, pp2gui.x + k_CameraFrameThickness, pp3gui.y + k_CameraFrameThickness);

            GUI.color = ViewpointIsSelected() ? Color.yellow : Color.white;
            GUI.DrawTexture(topHardEdge, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(bottomHardEdge, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(leftHardEdge, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(rightHardEdge, Texture2D.whiteTexture, ScaleMode.StretchToFill);

            Handles.EndGUI();

            GUI.color = previous;
        }

        [Shortcut(k_CameraPreviewShortcutIdPrefix + "Increase Field of View", typeof(SceneView), KeyCode.WheelUp, ShortcutModifiers.Alt)]
        static void IncreaseFieldOfView(ShortcutArguments args)
        {
            SceneView sv = SceneView.lastActiveSceneView;
            // Check active tool context
            if (!sv.viewpoint.viewpointContextIsActive || !(sv.viewpoint.activeViewpoint is ICameraLensData))
                return;

            DoChangeFieldOfView(sv.viewpoint.activeViewpoint, 1);

            // Repaint the Scene View as a Scrollwheel action doesn't.
            sv.Repaint();
        }

        [Shortcut(k_CameraPreviewShortcutIdPrefix + "Decrease Field of View", typeof(SceneView), KeyCode.WheelDown, ShortcutModifiers.Alt)]
        static void DecreaseFieldOfView(ShortcutArguments args)
        {
            SceneView sv = SceneView.lastActiveSceneView;
            // Check active tool context
            if (!sv.viewpoint.viewpointContextIsActive || !(sv.viewpoint.activeViewpoint is ICameraLensData))
                return;

            DoChangeFieldOfView(sv.viewpoint.activeViewpoint, -1);

            // Repaint the Scene View as a Scrollwheel action doesn't.
            sv.Repaint();
        }

        [Shortcut(k_CameraPreviewShortcutIdPrefix + "Decrease Size Of Overscan View Guides", typeof(SceneView), KeyCode.WheelUp)]
        static void IncreaseOverscan(ShortcutArguments args)
        {
            SceneView sv = SceneView.lastActiveSceneView as SceneView;
            // Check active tool context
            if (!sv.viewpoint.viewpointContextIsActive)
                return;

            float delta = Event.current.delta.y;
            sv.viewpoint.cameraOverscanSettings.scale += delta * k_MouseWheelScaleSensitivityMultiplier;

            // Repaint the Scene View as a Scrollwheel action doesn't.
            sv.Repaint();
        }

        [Shortcut(k_CameraPreviewShortcutIdPrefix + "Increase Size Of Overscan View Guides", typeof(SceneView), KeyCode.WheelDown)]
        static void DecreaseOverscan(ShortcutArguments args)
        {
            SceneView sv = SceneView.lastActiveSceneView;
            // Check active tool context
            if (!sv.viewpoint.viewpointContextIsActive)
                return;

            float delta = Event.current.delta.y;
            sv.viewpoint.cameraOverscanSettings.scale += delta * k_MouseWheelScaleSensitivityMultiplier;

            // Repaint the Scene View as a Scrollwheel action doesn't.
            sv.Repaint();
        }

        static void DoChangeFieldOfView(IViewpoint viewpoint, int sign)
        {
            var lensData = viewpoint as ICameraLensData;

            if (lensData.Orthographic == false)
            {
                if (lensData.UsePhysicalProperties)
                {
                    Undo.RecordObject(viewpoint.TargetObject, $"Modified Focal Length size in {viewpoint.TargetObject.name}");
                    lensData.FocalLength += sign * 1f;
                }
                else
                {
                    Undo.RecordObject(viewpoint.TargetObject, $"Modified Field of view in {viewpoint.TargetObject.name}");
                    lensData.FieldOfView += sign * 1f;
                }
            }
            else
            {
                Undo.RecordObject(viewpoint.TargetObject, $"Modified Orthographic size in {viewpoint.TargetObject.name}");
                lensData.OrthographicSize += sign * .025f;
            }
        }

        // Tells if the GameObject represented by the Viewpoint is currently selected in the Hierarchy.
        bool ViewpointIsSelected()
        {
            return Selection.Contains(activeViewpoint.TargetObject);
        }

        void TryMoveViewpoint()
        {
            activeViewpoint.Rotation = m_SceneView.GetTransformRotation();
            activeViewpoint.Position = m_SceneView.GetTransformPosition();

            if (activeViewpoint.Rotation != m_SceneView.GetTransformRotation()
                    || activeViewpoint.Position != m_SceneView.GetTransformPosition())
            {
                // Viewpoint is under some constraint. Apply back to SceneView so it gives the appropriate feedback to the user.
                AlignSceneViewToViewpoint();
            }
        }

        void AlignSceneViewToViewpoint()
        {
            m_SceneView.rotation = activeViewpoint.Rotation;
            m_SceneView.pivot = activeViewpoint.Position + activeViewpoint.Rotation * new Vector3(0, 0, m_SceneView.cameraDistance);
        }
    }
}
