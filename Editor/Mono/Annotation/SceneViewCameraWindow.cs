// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    public class SceneViewCameraWindow : PopupWindowContent
    {
        static class Styles
        {
            // Dynamic tooltips
            public static readonly string minTooltips = L10n.Tr($"The minimum speed of the camera in the Scene view. Valid values are between [{SceneView.CameraSettings.kAbsoluteSpeedMin}, {SceneView.CameraSettings.kAbsoluteSpeedMax - 1}].");
            public static readonly string maxTooltips = L10n.Tr($"The maximum speed of the camera in the Scene view. Valid values are between [{SceneView.CameraSettings.kAbsoluteSpeedMin + .0001f}, {SceneView.CameraSettings.kAbsoluteSpeedMax}].");

            // Menu labels
            public static readonly GUIContent copyPlacementLabel = EditorGUIUtility.TrTextContent("Copy Placement");
            public static readonly GUIContent pastePlacementLabel = EditorGUIUtility.TrTextContent("Paste Placement");
            public static readonly GUIContent copySettingsLabel = EditorGUIUtility.TrTextContent("Copy Settings");
            public static readonly GUIContent pasteSettingsLabel = EditorGUIUtility.TrTextContent("Paste Settings");
            public static readonly GUIContent resetSettingsLabel = EditorGUIUtility.TrTextContent("Reset Settings");

            // Layout
            public const int windowWidth = 290;
            public const int windowHeight = ((int)EditorGUI.kSingleLineHeight) * 16/*fieldCount*/ + 5/*contentPadding*/ * 2 + 2/*headerSpacing*/ * 2;
        }

        // Isolating the close on Escape pressed code in this Manipulator
        class CloseOnEscapeKeyPressed : Manipulator
        {
            EditorWindow m_Window;

            public CloseOnEscapeKeyPressed(EditorWindow window) => m_Window = window;

            protected override void RegisterCallbacksOnTarget()
            {
                // KeyDownEvent will be prevented until dropdown gets focus.
                // Force it to ensure it also works before any click on the dropdown.
                // Though the focus can only be set once the panel encompassing the target exists.
                target.focusable = true;
                if (target.panel == null)
                    target.RegisterCallbackOnce<AttachToPanelEvent>(evt => {
                        target.Focus();
                    });
                else
                    target.Focus();
                
                target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            }

            protected override void UnregisterCallbacksFromTarget()
                => target.UnregisterCallback<KeyDownEvent>(OnKeyDown);

            void OnKeyDown<T>(KeyboardEventBase<T> evt)
                where T : KeyboardEventBase<T>, new()
            {
                if (evt.keyCode == KeyCode.Escape)
                    m_Window.Close();
            }
        }

        const string k_UXMLResourcePath = "UXML/SceneView/SceneViewCameraEditor.uxml";
        
        // FOV values chosen to be the smallest and largest before extreme visual corruption
        const float k_MinFieldOfView = 4f;
        const float k_MaxFieldOfView = 120f;

        readonly SceneView m_SceneView;

        VisualElement m_Root;
        HelpBox m_ExtremeClipping;
        Slider m_FieldOfView;
        Toggle m_DynamicClipping;
        VisualElement m_WholeClippingPlanes;
        FloatField m_NearClip, m_FarClip;
        Toggle m_OcclusionCulling;
        Toggle m_Easing;
        Slider m_EasingDuration;
        Toggle m_Acceleration;
        Slider m_AccelerationSpeed;
        Slider m_Speed, m_SpeedModifier;
        FloatField m_MinSpeed, m_MaxSpeed;
        VisualElement m_AdditionalSettings;
        List<BaseFieldMouseDragger> m_CustomLabelDraggers = new();
        
        Vector2 m_WindowSize = new Vector2(Styles.windowWidth, Styles.windowHeight);

        [Obsolete($"{nameof(SceneViewCameraWindow)} has been converted to UITK. Please use {nameof(createAdditionalSettingsGUI)} and {nameof(bindAdditionalSettings)} instead. #from(6000.5)")]
        public static event Action<SceneView> additionalSettingsGui;

        public static Func<SceneView, VisualElement> createAdditionalSettingsGUI;
        public static Action<SceneView, VisualElement> bindAdditionalSettings;

        public override Vector2 GetWindowSize()
            => m_WindowSize;

        public SceneViewCameraWindow(SceneView sceneView)
            => m_SceneView = sceneView;
        
        public override VisualElement CreateGUI()
        {
            m_Root = new VisualElement();
            var visualTreeAsset = (VisualTreeAsset)EditorResources.Load<UnityEngine.Object>(k_UXMLResourcePath, isRequired: true);
            visualTreeAsset.CloneTree(m_Root);

            var menuButton = m_Root.Q<Button>(className: "menu");
            menuButton.clicked += ShowContextMenu;
            menuButton.style.backgroundImage = EditorGUIUtility.FindTexture("_Popup");

            m_Root.AddManipulator(new CloseOnEscapeKeyPressed(editorWindow));

            LinkElements(m_SceneView.cameraSettings);
            Bind(m_SceneView.cameraSettings);

            return m_Root;
        }

        void ShowExtremeClippingIfNeeded()
        {
            bool shouldShow = m_SceneView.cameraSettings.farClip / m_SceneView.cameraSettings.nearClip > 10000000
                || m_SceneView.cameraSettings.nearClip < 0.0001f;
            m_ExtremeClipping.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        private void CreateCustomLabelDragger<TValueType>(Label label, TextValueField<TValueType> field)
        {
            var dragger = new FieldMouseDragger<TValueType>(field);
            dragger.SetDragZone(label);
            label.EnableInClassList(BaseField<TValueType>.labelDraggerVariantUssClassName, true);
            m_CustomLabelDraggers.Add(dragger); // keep it alive
        }

        void LinkElements(SceneView.CameraSettings settings)
        {
            // Link C# constants and callbacks to VisualElement

            // Scene Camera
            //    Field of View
            m_FieldOfView = m_Root.MandatoryQ<Slider>("FieldOfView");
            m_FieldOfView.lowValue = k_MinFieldOfView;
            m_FieldOfView.highValue = k_MaxFieldOfView;
            m_FieldOfView.RegisterValueChangedCallback(evt =>
            {
                settings.fieldOfView = evt.newValue;
                m_SceneView.Repaint();
            });
            
            //    Clipping
            m_DynamicClipping = m_Root.MandatoryQ<Toggle>("DynamicClipping");
            m_WholeClippingPlanes = m_Root.MandatoryQ("ClippingPlanes");
            m_NearClip = m_Root.MandatoryQ<FloatField>("ClippingNear");
            m_FarClip = m_Root.MandatoryQ<FloatField>("ClippingFar");
            m_ExtremeClipping = m_Root.MandatoryQ<HelpBox>("ExtremeClippingHelpBox");

            m_DynamicClipping.RegisterValueChangedCallback(evt =>
            {
                settings.dynamicClip = evt.newValue;
                m_WholeClippingPlanes.SetEnabled(!settings.dynamicClip);
                m_SceneView.Repaint();
            });

            m_NearClip.RegisterValueChangedCallback(evt =>
            {
                settings.SetClipPlanes(evt.newValue, settings.farClip);
                m_NearClip.SetValueWithoutNotify(settings.nearClip);
                m_FarClip.SetValueWithoutNotify(settings.farClip);
                ShowExtremeClippingIfNeeded();
                m_SceneView.Repaint();
            });
            CreateCustomLabelDragger(m_Root.MandatoryQ<Label>("ClippingNearLabel"), m_NearClip);

            m_FarClip.RegisterValueChangedCallback(evt => 
            {
                settings.SetClipPlanes(settings.nearClip, evt.newValue);
                m_NearClip.SetValueWithoutNotify(settings.nearClip);
                m_FarClip.SetValueWithoutNotify(settings.farClip);
                ShowExtremeClippingIfNeeded();
                m_SceneView.Repaint();
            });
            CreateCustomLabelDragger(m_Root.MandatoryQ<Label>("ClippingFarLabel"), m_FarClip);

            //    Occlusion culling
            m_OcclusionCulling = m_Root.MandatoryQ<Toggle>("OcclusionCulling");
            m_OcclusionCulling.RegisterValueChangedCallback(evt =>
            {
                settings.occlusionCulling = evt.newValue;
                m_SceneView.Repaint();
            });

            // Camera Navigation
            //    Easing
            m_Easing = m_Root.MandatoryQ<Toggle>("Easing");
            m_EasingDuration = m_Root.MandatoryQ<Slider>("EasingDuration");

            m_Easing.RegisterValueChangedCallback(evt =>
            {
                settings.easingEnabled = evt.newValue;
                m_EasingDuration.SetEnabled(settings.easingEnabled);
            });

            m_EasingDuration.lowValue = SceneView.CameraSettings.kAbsoluteEasingDurationMin;
            m_EasingDuration.highValue = SceneView.CameraSettings.kAbsoluteEasingDurationMax;
            m_EasingDuration.RegisterValueChangedCallback(evt => settings.easingDuration = evt.newValue);

            //    Acceleration
            m_Acceleration = m_Root.MandatoryQ<Toggle>("Acceleration");
            m_AccelerationSpeed = m_Root.MandatoryQ<Slider>("AccelerationSpeed");

            m_Acceleration.RegisterValueChangedCallback(evt =>
            {
                settings.accelerationEnabled = evt.newValue;
                m_AccelerationSpeed.SetEnabled(settings.accelerationEnabled);
            });

            m_AccelerationSpeed.lowValue = SceneView.CameraSettings.kAbsoluteAccelerationSpeedMin;
            m_AccelerationSpeed.highValue = SceneView.CameraSettings.kAbsoluteAccelerationSpeedMax;
            m_AccelerationSpeed.RegisterValueChangedCallback(evt => settings.accelerationSpeed = evt.newValue);

            //    Speed
            m_Speed = m_Root.MandatoryQ<Slider>("Speed");
            m_SpeedModifier = m_Root.MandatoryQ<Slider>("SpeedModifier");
            m_MinSpeed = m_Root.MandatoryQ<FloatField>("SpeedMin");
            m_MaxSpeed = m_Root.MandatoryQ<FloatField>("SpeedMax");

            m_Speed.RegisterValueChangedCallback(evt => settings.speed = settings.RoundSpeedToNearestSignificantDecimal(evt.newValue));
            
            m_SpeedModifier.lowValue = SceneView.CameraSettings.kAbsoluteSpeedModifierMin;
            m_SpeedModifier.highValue = SceneView.CameraSettings.kAbsoluteSpeedModifierMax;
            m_SpeedModifier.RegisterValueChangedCallback(evt => settings.speedModifier = evt.newValue);

            m_MinSpeed.RegisterValueChangedCallback(evt =>
            {
                settings.SetSpeedMinMax(evt.newValue, settings.speedMax);
                m_Speed.lowValue = settings.speedMin;
                m_Speed.highValue = settings.speedMax;
                m_MinSpeed.SetValueWithoutNotify(settings.speedMin);
                m_MaxSpeed.SetValueWithoutNotify(settings.speedMax);
            });
            CreateCustomLabelDragger(m_Root.MandatoryQ<Label>("SpeedMinLabel"), m_MinSpeed);
            m_MinSpeed.tooltip = Styles.minTooltips;
            
            m_MaxSpeed.RegisterValueChangedCallback(evt =>
            {
                settings.SetSpeedMinMax(settings.speedMin, evt.newValue);
                m_Speed.lowValue = settings.speedMin;
                m_Speed.highValue = settings.speedMax;
                m_MinSpeed.SetValueWithoutNotify(settings.speedMin);
                m_MaxSpeed.SetValueWithoutNotify(settings.speedMax);
            });
            CreateCustomLabelDragger(m_Root.MandatoryQ<Label>("SpeedMaxLabel"), m_MaxSpeed);
            m_MaxSpeed.tooltip = Styles.maxTooltips;

            // Rendering (SRP extension, hidden in BiRP)
            if (createAdditionalSettingsGUI != null)
            {
                m_AdditionalSettings = createAdditionalSettingsGUI(m_SceneView);
                if (m_AdditionalSettings != null)
                {
                    var container = new VisualElement() { name = "AdditionalSettingsSection" };
                    container.AddToClassList("section");
                    container.Add(m_AdditionalSettings);
                    m_Root.Add(container);
                }
            }
            // Keep old extension system at the end as it was.
            else if (additionalSettingsGui != null)
            {
                var container = new IMGUIContainer(() =>
                {
                    var oldWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 140; //keep alignment with UITK UI, same value as .unity-label
                    additionalSettingsGui(m_SceneView);
                    EditorGUIUtility.labelWidth = oldWidth;
                });
                container.style.maxWidth = Styles.windowWidth;
                m_Root.Add(container);
            }
        }

        void Bind(SceneView.CameraSettings settings)
        {
            // Update the data. It need to be done manually as there is no underlying UnityObject for binding through serialization path
            // This is currently only binded again when performing a Reset or executing Paste
            
            // Scene Camera
            //    Field of View
            m_FieldOfView.SetValueWithoutNotify(settings.fieldOfView);
            m_FieldOfView.SetEnabled(!m_SceneView.camera.orthographic);

            //    Clipping
            m_WholeClippingPlanes.SetEnabled(!settings.dynamicClip);
            m_DynamicClipping.SetValueWithoutNotify(settings.dynamicClip);
            m_NearClip.SetValueWithoutNotify(settings.nearClip);
            m_FarClip.SetValueWithoutNotify(settings.farClip);
            ShowExtremeClippingIfNeeded();

            //    Occlusion culling
            m_OcclusionCulling.SetValueWithoutNotify(settings.occlusionCulling);

            // Camera Navigation
            //    Easing
            m_Easing.SetValueWithoutNotify(settings.easingEnabled);
            m_EasingDuration.SetEnabled(settings.easingEnabled);
            m_EasingDuration.SetValueWithoutNotify(settings.easingDuration);

            //    Acceleration
            m_Acceleration.SetValueWithoutNotify(settings.accelerationEnabled);
            m_AccelerationSpeed.SetEnabled(settings.accelerationEnabled);
            m_AccelerationSpeed.SetValueWithoutNotify(settings.accelerationSpeed);

            //    Speed
            m_Speed.lowValue = settings.speedMin;
            m_Speed.highValue = settings.speedMax;
            m_Speed.SetValueWithoutNotify(settings.RoundSpeedToNearestSignificantDecimal(settings.speed));
            m_SpeedModifier.SetValueWithoutNotify(settings.speedModifier);
            m_MinSpeed.SetValueWithoutNotify(settings.speedMin);
            m_MaxSpeed.SetValueWithoutNotify(settings.speedMax);

            // Rendering (SRP extension, hidden in BiRP)
            if (m_AdditionalSettings != null && bindAdditionalSettings != null)
                bindAdditionalSettings(m_SceneView, m_AdditionalSettings);
        }

        // ========= Menu interactions below =========

        void ShowContextMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(Styles.copyPlacementLabel, false, () => CopyPlacement(m_SceneView));
            if (CanPastePlacement())
                menu.AddItem(Styles.pastePlacementLabel, false, () => PastePlacement(m_SceneView));
            else
                menu.AddDisabledItem(Styles.pastePlacementLabel);
            menu.AddItem(Styles.copySettingsLabel, false, CopySettings);
            if (Clipboard.HasCustomValue<SceneView.CameraSettings>())
                menu.AddItem(Styles.pasteSettingsLabel, false, PasteSettings);
            else
                menu.AddDisabledItem(Styles.pasteSettingsLabel);
            menu.AddItem(Styles.resetSettingsLabel, false, ResetSettings);

            menu.ShowAsContext();
        }

        // ReSharper disable once UnusedMember.Local - called by a shortcut
        [Shortcut("Camera/Copy Placement")]
        static void CopyPlacementShortcut()
        {
            // if we are interacting with a game view, copy the main camera placement
            var playView = PlayModeView.GetLastFocusedPlayModeView();
            if (playView != null && (EditorWindow.focusedWindow == playView || EditorWindow.mouseOverWindow == playView))
            {
                var cam = Camera.main;
                if (cam != null)
                    Clipboard.SetCustomValue(new TransformWorldPlacement(cam.transform));
            }
            // otherwise copy the last active scene view placement
            else
            {
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                    CopyPlacement(sceneView);
            }
        }

        static void CopyPlacement(SceneView view)
            => Clipboard.SetCustomValue(new TransformWorldPlacement(view.camera.transform));

        // ReSharper disable once UnusedMember.Local - called by a shortcut
        [Shortcut("Camera/Paste Placement")]
        static void PastePlacementShortcut()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;
            if (CanPastePlacement())
                PastePlacement(sceneView);
        }

        static bool CanPastePlacement()
            => Clipboard.HasCustomValue<TransformWorldPlacement>();

        static void PastePlacement(SceneView view)
        {
            var tr = view.camera.transform;
            var placement = Clipboard.GetCustomValue<TransformWorldPlacement>();
            tr.position = placement.position;
            tr.rotation = placement.rotation;
            tr.localScale = placement.scale;

            // Similar to what AlignViewToObject does, except we need to do that instantly
            // in case the shortcut key was pressed while FPS camera controls (right click drag)
            // were active.
            view.size = 10;
            view.LookAt(tr.position + tr.forward * view.cameraDistance, tr.rotation, view.size, view.orthographic, true);

            view.Repaint();
        }

        void CopySettings()
        {
            // ClipboardParser.CustomPrefix uses the templated type, which may not match the actual runtime type.
            // We extracted its logic here using GetType() instead, so it doesn’t treat everything as "IAdditionalSettings".
            // Also, the parser requires the templated type to have a public constructor, which cannot be enforced via an interface.
            // To ensure our changes don’t affect other copy/paste operations (and since CameraSettings copy is already in progress),
            // AdditionalSettings are stored in a temporary buffer inside CameraSettings.
            var settings = m_SceneView.currentPipelineAdditionalSettings;
            m_SceneView.cameraSettings.m_LocalCopyBuffer = settings != null
                ? $"{settings.GetType().FullName}JSON:{EditorJsonUtility.ToJson(settings)}"
                : null;
            Clipboard.SetCustomValue(m_SceneView.cameraSettings);
            m_SceneView.cameraSettings.m_LocalCopyBuffer = null;
        }

        void PasteSettings()
        {
            // Beware all further result of Clipboard.GetCustomValue<T> give same object.
            // As we modify it (clean the local copy buffer), we want to be sure to use a new instance with same data instead.
            // This also fix case where SceneViewCamera become linked after a copy paste...
            m_SceneView.cameraSettings = Clipboard.GetCustomValue<SceneView.CameraSettings>().Clone();

            var settings = m_SceneView.currentPipelineAdditionalSettings;
            if (settings != null)
            {
                // This paste can be optimized using a cache for reconstructed object as in Clipboard.GetCustomValue
                // But this is adding a lot more complexity for an operation happening really sparsely...
                // Also we want to conserve linkedComponent unchanged to aply change on the right Camera...
                Component tmpLinkedComponent = settings.linkedComponent;
                string prefix = $"{settings.GetType().FullName}JSON:";
                if (m_SceneView.cameraSettings.m_LocalCopyBuffer?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ?? false)
                    EditorJsonUtility.FromJsonOverwrite(m_SceneView.cameraSettings.m_LocalCopyBuffer.Substring(prefix.Length), settings);
                settings.linkedComponent = tmpLinkedComponent;
                settings.Apply();
            }
            m_SceneView.cameraSettings.m_LocalCopyBuffer = null; // Buffer was copy-pasted too... It will not be used anymore until next Copy

            m_SceneView.Repaint();
            Bind(m_SceneView.cameraSettings);
        }

        void ResetSettings()
        {
            m_SceneView.ResetCameraSettings();
            var settings = m_SceneView.currentPipelineAdditionalSettings;
            if (settings != null)
            {
                settings.Reset();
                settings.Apply();
            }
            m_SceneView.Repaint();
            Bind(m_SceneView.cameraSettings);
        }
    }
}
