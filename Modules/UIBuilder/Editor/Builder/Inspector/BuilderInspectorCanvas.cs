// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorCanvas : IBuilderInspectorSection
    {
        static readonly int s_CameraRefreshDelayMS = 100;
        internal const string ContainerName = "canvas-inspector";
        internal const string EditorExtensionsModeToggleName = "editor-extensions-mode-toggle";

        public VisualElement root => m_CanvasInspector;

        BuilderInspector m_Inspector;
        BuilderDocument m_Document;
        BuilderCanvas m_Canvas;
        VisualElement m_CanvasInspector;

        BuilderDocumentSettings settings => m_Document.settings;

        VisualElement customBackgroundElement => m_Canvas.customBackgroundElement;
        FoldoutWithCheckbox m_BackgroundOptionsFoldout;

        // Fields
        IntegerField m_CanvasWidth;
        IntegerField m_CanvasHeight;
        Toggle m_MatchGameViewToggle;
        HelpBox m_MatchGameViewHelpBox;
        PercentSlider m_ColorOpacityField;
        PercentSlider m_ImageOpacityField;
        PercentSlider m_CameraOpacityField;
        ToggleButtonGroup m_BackgroundMode;
        ColorField m_ColorField;
        ObjectField m_ImageField;
        ToggleButtonGroup m_ImageScaleModeField;
        Button m_FitCanvasToImageButton;
        ObjectField m_CameraField;

        bool m_CameraModeEnabled;
        RenderTexture m_InGamePreviewRenderTexture;
        Rect m_InGamePreviewRect;
        Texture2D m_InGamePreviewTexture2D;
        IVisualElementScheduledItem m_InGamePreviewScheduledItem;
        Toggle m_EditorExtensionsModeToggle;

        Camera backgroundCamera
        {
            get
            {
                var fieldValue = m_CameraField.value;
                var camera = fieldValue as Camera;
                return camera;
            }
        }

        // Internal for tests.
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        internal Texture2D inGamePreviewTexture2D => m_InGamePreviewTexture2D;

        // Background Control Containers
        VisualElement m_BackgroundColorModeControls;
        VisualElement m_BackgroundImageModeControls;
        VisualElement m_BackgroundCameraModeControls;

        public BuilderInspectorCanvas(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_Document = inspector.document;
            m_CanvasInspector = m_Inspector.Q(ContainerName);

            var builderWindow = inspector.paneWindow as Builder;
            if (builderWindow == null)
                return;

            m_Canvas = builderWindow.canvas;

            m_CameraModeEnabled = false;

            // Size Fields
            m_CanvasWidth = root.Q<IntegerField>("canvas-width");
            m_CanvasWidth.RegisterValueChangedCallback(OnWidthChange);
            m_CanvasHeight = root.Q<IntegerField>("canvas-height");
            m_CanvasHeight.RegisterValueChangedCallback(OnHeightChange);
            m_Canvas.RegisterCallback<GeometryChangedEvent>(OnCanvasSizeChange);

            // This allows user to temporarily type values below the minimum canvas size
            SetDelayOnSizeFieldsEnabled(true);

            // To update the canvas size as user mouse drags the width or height labels
            DisableDelayOnActiveLabelMouseDraggers();

            m_MatchGameViewToggle = root.Q<Toggle>("match-game-view");
            m_MatchGameViewToggle.RegisterValueChangedCallback(OnMatchGameViewModeChanged);
            m_MatchGameViewHelpBox = root.Q<HelpBox>("match-game-view-hint");

            // Background Opacity
            m_ColorOpacityField = root.Q<PercentSlider>("background-color-opacity-field");
            m_ColorOpacityField.RegisterValueChangedCallback(e =>
            {
                settings.ColorModeBackgroundOpacity = e.newValue;
                OnBackgroundOpacityChange(e.newValue);
            });

            m_ImageOpacityField = root.Q<PercentSlider>("background-image-opacity-field");
            m_ImageOpacityField.RegisterValueChangedCallback(e =>
            {
                settings.ImageModeCanvasBackgroundOpacity = e.newValue;
                OnBackgroundOpacityChange(e.newValue);
            });

            m_CameraOpacityField = root.Q<PercentSlider>("background-camera-opacity-field");
            m_CameraOpacityField.RegisterValueChangedCallback(e =>
            {
                settings.CameraModeCanvasBackgroundOpacity = e.newValue;
                OnBackgroundOpacityChange(e.newValue);
            });

            // Setup Background State
            m_BackgroundOptionsFoldout = root.Q<FoldoutWithCheckbox>("canvas-background-foldout");
            m_BackgroundOptionsFoldout.RegisterCheckboxValueChangedCallback(e =>
            {
                settings.EnableCanvasBackground = e.newValue;
                PostSettingsChange();
                ApplyBackgroundOptions();
            });

            // Setup Background Mode
            var backgroundModeType = typeof(BuilderCanvasBackgroundMode);
            m_BackgroundMode = root.Q<ToggleButtonGroup>("background-mode-field");
            m_BackgroundMode.userData = backgroundModeType;
            m_BackgroundMode.Add(new Button() { name="Color", iconImage = BuilderInspectorUtilities.LoadIcon("color_picker", "Canvas/"), tooltip = "color" });
            m_BackgroundMode.Add(new Button() { name="Image", iconImage = BuilderInspectorUtilities.LoadIcon("RawImage", "Canvas/"), tooltip = "image" });
            m_BackgroundMode.Add(new Button() { name="Camera", iconImage = EditorGUIUtility.FindTexture("d_SceneViewCamera"), tooltip = "camera" });
            m_BackgroundMode.RegisterValueChangedCallback(OnBackgroundModeChange);

            // Color field.
            m_ColorField = root.Q<ColorField>("background-color-field");
            m_ColorField.RegisterValueChangedCallback(OnBackgroundColorChange);

            // Set Image field.
            m_ImageField = root.Q<ObjectField>("background-image-field");
            m_ImageField.objectType = typeof(Texture2D);
            m_ImageField.RegisterValueChangedCallback(OnBackgroundImageChange);
            m_ImageScaleModeField = root.Q<ToggleButtonGroup>("background-image-scale-mode-field");
            m_ImageScaleModeField.userData = typeof(ScaleMode);
            var backgroundScaleModeValues = Enum.GetValues(typeof(ScaleMode))
                .OfType<ScaleMode>().Select((v) => StyleSheetUtility.ConvertCamelToDash(v.ToString())).ToList();
            foreach (var value in backgroundScaleModeValues)
            {
                m_ImageScaleModeField.Add(new Button() { iconImage = BuilderInspectorUtilities.LoadIcon(BuilderNameUtilities.ConvertDashToHuman(value), "Background/"), tooltip = value });
            }
            m_ImageScaleModeField.RegisterValueChangedCallback(OnBackgroundImageScaleModeChange);
            m_FitCanvasToImageButton = root.Q<Button>("background-image-fit-canvas-button");
            m_FitCanvasToImageButton.clickable.clicked += FitCanvasToImage;

            // Set Camera field.
            m_CameraField = root.Q<ObjectField>("background-camera-field");
            m_CameraField.objectType = typeof(Camera);
            m_CameraField.RegisterValueChangedCallback(OnBackgroundCameraChange);

            SetupEditorExtensionsModeToggle();

            // Control Containers
            m_BackgroundColorModeControls = root.Q("canvas-background-color-mode-controls");
            m_BackgroundImageModeControls = root.Q("canvas-background-image-mode-controls");
            m_BackgroundCameraModeControls = root.Q("canvas-background-camera-mode-controls");

            root.RegisterCallback<AttachToPanelEvent>(AttachToPanelCallback);
            root.RegisterCallback<DetachFromPanelEvent>(DetachFromPanelCallback);
        }

        void AttachToPanelCallback(AttachToPanelEvent e)
        {
            EditorApplication.playModeStateChanged += PlayModeStateChange;
        }

        void DetachFromPanelCallback(DetachFromPanelEvent e)
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChange;
        }

        // This is for temporarily disable delay on the width and height fields
        void DisableDelayOnActiveLabelMouseDraggers()
        {
            DisableDelayOnActiveLabelMouseDragger(m_CanvasWidth);
            DisableDelayOnActiveLabelMouseDragger(m_CanvasHeight);
        }

        void DisableDelayOnActiveLabelMouseDragger(IntegerField field)
        {
            // Use the Move event instead of the Down event because the Down event is intercepted (with Event.StopImmediatePropagation)
            // by the FieldMouseDragger manipulator attached to the label
            field.labelElement.RegisterCallback<PointerMoveEvent>(e => {
                if (e.pressedButtons != 0)
                    SetDelayOnSizeFieldsEnabled(false);
            });
            field.labelElement.RegisterCallback<PointerUpEvent>(e => SetDelayOnSizeFieldsEnabled(true));
        }

        void SetDelayOnSizeFieldsEnabled(bool enabled)
        {
            m_CanvasWidth.isDelayed = enabled;
            m_CanvasHeight.isDelayed = enabled;
        }

        void SetupEditorExtensionsModeToggle()
        {
            m_EditorExtensionsModeToggle = root.Q<Toggle>(EditorExtensionsModeToggleName);
            m_EditorExtensionsModeToggle.RegisterValueChangedCallback(e =>
            {
                m_Document.fileSettings.editorExtensionMode = e.newValue;
                m_Inspector.selection.NotifyOfHierarchyChange(m_Document, null, BuilderHierarchyChangeType.Attributes);
                if (e.newValue)
                {
                    Builder.ShowWarning(BuilderConstants.InspectorEditorExtensionAuthoringActivated);
                }
            });
        }

        void RemoveDocumentSettings()
        {
            root.Q("document-settings").RemoveFromHierarchy();
        }

        public void Disable()
        {
            throw new NotImplementedException();
        }

        public void Enable()
        {
            throw new NotImplementedException();
        }

        public void Refresh()
        {
            if (m_Document != m_Inspector.document)
                m_Document = m_Inspector.document;
            // HACK until fix goes in:
            SetDelayOnSizeFieldsEnabled(false);

            m_CanvasWidth.SetValueWithoutNotify(settings.CanvasWidth);
            m_CanvasHeight.SetValueWithoutNotify(settings.CanvasHeight);

            SetDelayOnSizeFieldsEnabled(true);

            m_BackgroundOptionsFoldout.SetCheckboxValueWithoutNotify(settings.EnableCanvasBackground);

            m_ColorOpacityField.SetValueWithoutNotify(settings.ColorModeBackgroundOpacity);
            m_ImageOpacityField.SetValueWithoutNotify(settings.ImageModeCanvasBackgroundOpacity);
            m_CameraOpacityField.SetValueWithoutNotify(settings.CameraModeCanvasBackgroundOpacity);

            var backgroundModeOptions = new ToggleButtonGroupState(0, Enum.GetNames(typeof(BuilderCanvasBackgroundMode)).Length);
            backgroundModeOptions[(int)settings.CanvasBackgroundMode] = true;
            m_BackgroundMode.SetValueWithoutNotify(backgroundModeOptions);
            m_ColorField.SetValueWithoutNotify(settings.CanvasBackgroundColor);

            var imageFieldOptions = new ToggleButtonGroupState(0, Enum.GetNames(typeof(ScaleMode)).Length);
            imageFieldOptions[(int)settings.CanvasBackgroundImageScaleMode] = true;
            m_ImageScaleModeField.SetValueWithoutNotify(imageFieldOptions);
            m_ImageField.SetValueWithoutNotify(settings.CanvasBackgroundImage);

            m_CameraField.SetValueWithoutNotify(FindCameraByName());
            m_EditorExtensionsModeToggle?.SetValueWithoutNotify(m_Document.fileSettings.editorExtensionMode);

            ApplyBackgroundOptions();
            RefreshMatchGameViewToggle();
            bool canvasDimensionsDifferentFromSettings = (int) m_Canvas.height != settings.CanvasHeight || (int) m_Canvas.width != settings.CanvasWidth;
            if (canvasDimensionsDifferentFromSettings)
            {
                m_Canvas.SetSizeFromDocumentSettings();
            }
        }

        void RefreshMatchGameViewToggle()
        {
            m_CanvasWidth.SetEnabled(!settings.MatchGameView);
            m_CanvasHeight.SetEnabled(!settings.MatchGameView);
            m_MatchGameViewToggle.SetValueWithoutNotify(settings.MatchGameView);
            m_MatchGameViewHelpBox.style.display = settings.MatchGameView
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        Camera FindCameraByName()
        {
            var cameraName = settings.CanvasBackgroundCameraName;
            if (string.IsNullOrEmpty(cameraName))
                return null;

            var camera = Camera.allCameras.FirstOrDefault((c) => c.name == cameraName);
            return camera;
        }

        void PostSettingsChange()
        {
            m_Document.SaveSettingsToDisk();
        }

        void ApplyBackgroundOptions()
        {
            DeactivateCameraMode();
            customBackgroundElement.style.backgroundColor = StyleKeyword.Initial;
            customBackgroundElement.style.backgroundImage = StyleKeyword.Initial;
            customBackgroundElement.style.backgroundPositionX = StyleKeyword.Initial;
            customBackgroundElement.style.backgroundPositionY = StyleKeyword.Initial;
            customBackgroundElement.style.backgroundRepeat = StyleKeyword.Initial;
            customBackgroundElement.style.backgroundSize = StyleKeyword.Initial;

            if (settings.EnableCanvasBackground)
            {
                switch (settings.CanvasBackgroundMode)
                {
                    case BuilderCanvasBackgroundMode.Color:
                        customBackgroundElement.style.backgroundColor = settings.CanvasBackgroundColor;
                        customBackgroundElement.style.opacity = settings.ColorModeBackgroundOpacity;
                        break;
                    case BuilderCanvasBackgroundMode.Image:
                        customBackgroundElement.style.backgroundImage = settings.CanvasBackgroundImage;
                        customBackgroundElement.style.backgroundPositionX = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(settings.CanvasBackgroundImageScaleMode);
                        customBackgroundElement.style.backgroundPositionY = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(settings.CanvasBackgroundImageScaleMode);
                        customBackgroundElement.style.backgroundRepeat = BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(settings.CanvasBackgroundImageScaleMode);
                        customBackgroundElement.style.backgroundSize = BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(settings.CanvasBackgroundImageScaleMode);
                        customBackgroundElement.style.opacity = settings.ImageModeCanvasBackgroundOpacity;
                        break;
                    case BuilderCanvasBackgroundMode.Camera:
                        ActivateCameraMode();
                        customBackgroundElement.style.opacity = settings.CameraModeCanvasBackgroundOpacity;
                        customBackgroundElement.style.backgroundPositionX = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop);
                        customBackgroundElement.style.backgroundPositionY = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop);
                        customBackgroundElement.style.backgroundRepeat = BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.ScaleAndCrop);
                        customBackgroundElement.style.backgroundSize = BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleAndCrop);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            UpdateBackgroundControlsView();
        }

        void UpdateBackgroundControlsView()
        {
            m_BackgroundColorModeControls.EnableInClassList(BuilderConstants.HiddenStyleClassName,
                settings.CanvasBackgroundMode != BuilderCanvasBackgroundMode.Color);

            m_BackgroundImageModeControls.EnableInClassList(BuilderConstants.HiddenStyleClassName,
                settings.CanvasBackgroundMode != BuilderCanvasBackgroundMode.Image);

            m_BackgroundCameraModeControls.EnableInClassList(BuilderConstants.HiddenStyleClassName,
                settings.CanvasBackgroundMode != BuilderCanvasBackgroundMode.Camera);
        }

        void ActivateCameraMode()
        {
            if (m_CameraModeEnabled || backgroundCamera == null)
                return;

            UpdateCameraRects();

            m_InGamePreviewScheduledItem = customBackgroundElement.schedule.Execute(UpdateInGameBackground);
            m_InGamePreviewScheduledItem.Every(s_CameraRefreshDelayMS);

            m_CameraModeEnabled = true;
        }

        void DeactivateCameraMode()
        {
            if (!m_CameraModeEnabled)
                return;

            m_InGamePreviewScheduledItem.Pause();
            m_InGamePreviewScheduledItem = null;

            m_InGamePreviewRenderTexture = null;
            m_InGamePreviewTexture2D = null;

            m_CameraModeEnabled = false;

            customBackgroundElement.style.backgroundImage = StyleKeyword.Initial;
        }

        void PlayModeStateChange(PlayModeStateChange state)
        {
            UpdateCameraRects();
        }

        void UpdateCameraRects()
        {
            if (settings.CanvasBackgroundMode != BuilderCanvasBackgroundMode.Camera)
                return;

            int width = 2 * settings.CanvasWidth;
            int height = 2 * settings.CanvasHeight;

            m_InGamePreviewRenderTexture = new RenderTexture(width, height, 1);
            m_InGamePreviewRect = new Rect(0, 0, width, height);
            m_InGamePreviewTexture2D = new Texture2D(width, height);
        }

        void UpdateInGameBackground()
        {
            if (backgroundCamera == null)
            {
                var refCamera = FindCameraByName();
                m_CameraField.value = null;
                m_CameraField.value = refCamera;
                return;
            }

            backgroundCamera.targetTexture = m_InGamePreviewRenderTexture;

            RenderTexture.active = m_InGamePreviewRenderTexture;
            backgroundCamera.Render();

            m_InGamePreviewTexture2D.ReadPixels(m_InGamePreviewRect, 0, 0);
            m_InGamePreviewTexture2D.Apply(false);

            RenderTexture.active = null;
            backgroundCamera.targetTexture = null;

            customBackgroundElement.style.backgroundImage = m_InGamePreviewTexture2D;
            customBackgroundElement.IncrementVersion(VersionChangeType.Repaint);
        }

        void OnCanvasSizeChange(GeometryChangedEvent evt)
        {
            // HACK until fix goes in:
            SetDelayOnSizeFieldsEnabled(false);

            m_CanvasWidth.SetValueWithoutNotify((int)m_Canvas.width);
            m_CanvasHeight.SetValueWithoutNotify((int)m_Canvas.height);

            SetDelayOnSizeFieldsEnabled(true);

            UpdateCameraRects();
        }

        void OnWidthChange(ChangeEvent<int> evt)
        {
            var newValue = evt.newValue;
            Undo.RegisterCompleteObjectUndo(m_Document, BuilderConstants.ChangeCanvasDimensionsOrMatchViewUndoMessage);
            if (newValue < (int)BuilderConstants.CanvasMinWidth)
            {
                newValue = (int)BuilderConstants.CanvasMinWidth;
                var field = evt.elementTarget as IntegerField;

                // HACK until fix goes in:
                field.isDelayed = false;
                field.SetValueWithoutNotify(newValue);
                field.isDelayed = true;
            }

            settings.CanvasWidth = newValue;
            m_Canvas.width = newValue;
            UpdateCameraRects();
            PostSettingsChange();
        }

        void OnHeightChange(ChangeEvent<int> evt)
        {
            var newValue = evt.newValue;
            Undo.RegisterCompleteObjectUndo(m_Document,BuilderConstants.ChangeCanvasDimensionsOrMatchViewUndoMessage);
            if (newValue < (int)BuilderConstants.CanvasMinHeight)
            {
                newValue = (int)BuilderConstants.CanvasMinHeight;

                var field = evt.elementTarget as IntegerField;

                // HACK until fix goes in:
                field.isDelayed = false;
                field.SetValueWithoutNotify(newValue);
                field.isDelayed = true;
            }

            settings.CanvasHeight = newValue;
            m_Canvas.height = newValue;
            UpdateCameraRects();
            PostSettingsChange();
        }

        void OnMatchGameViewModeChanged(ChangeEvent<bool> evt)
        {
            Undo.RegisterCompleteObjectUndo(m_Document,BuilderConstants.ChangeCanvasDimensionsOrMatchViewUndoMessage);
            settings.MatchGameView = evt.newValue;
            RefreshMatchGameViewToggle();
            m_Canvas.matchGameView = settings.MatchGameView;
            PostSettingsChange();
        }

        void OnBackgroundOpacityChange(float opacity)
        {
            customBackgroundElement.style.opacity = opacity;
            PostSettingsChange();
        }

        void OnBackgroundModeChange(ChangeEvent<ToggleButtonGroupState> evt)
        {
            var selected = evt.newValue.GetActiveOptions(stackalloc int[evt.newValue.length]);
            settings.CanvasBackgroundMode = (BuilderCanvasBackgroundMode)selected[0];
            PostSettingsChange();
            ApplyBackgroundOptions();
        }

        void OnBackgroundColorChange(ChangeEvent<Color> evt)
        {
            settings.CanvasBackgroundColor = evt.newValue;
            PostSettingsChange();
            ApplyBackgroundOptions();
        }

        void OnBackgroundImageChange(ChangeEvent<Object> evt)
        {
            settings.CanvasBackgroundImage = evt.newValue as Texture2D;
            PostSettingsChange();
            ApplyBackgroundOptions();
        }

        void OnBackgroundImageScaleModeChange(ChangeEvent<ToggleButtonGroupState> evt)
        {
            var selected = evt.newValue.GetActiveOptions(stackalloc int[evt.newValue.length]);
            settings.CanvasBackgroundImageScaleMode = (ScaleMode)selected[0];
            PostSettingsChange();
            ApplyBackgroundOptions();
        }

        void FitCanvasToImage()
        {
            if (settings.CanvasBackgroundImage == null)
                return;

            m_Canvas.width = settings.CanvasBackgroundImage.width;
            m_Canvas.height = settings.CanvasBackgroundImage.height;
        }

        void OnBackgroundCameraChange(ChangeEvent<Object> evt)
        {
            var previousCamera = evt.previousValue as Camera;
            if (ReferenceEquals(previousCamera, evt.newValue))
                return;

            var camera = evt.newValue as Camera;

            settings.CanvasBackgroundCameraName = camera == null ? null : camera.name;
            PostSettingsChange();
            ApplyBackgroundOptions();
        }
    }
}
