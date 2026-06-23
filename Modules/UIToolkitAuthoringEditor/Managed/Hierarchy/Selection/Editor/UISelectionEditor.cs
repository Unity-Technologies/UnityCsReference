// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;

namespace Unity.UIToolkit.Editor
{
    // Shared lifecycle for the UI selection editors (VisualElement / StyleRule): keeps the inspector's
    // animation-recording state in sync with AnimationMode. Subclasses supply the inspector and the
    // recording context for their target type.
    abstract class UISelectionEditor : UnityEditor.Editor
    {
        // The recording start/stop events don't fire when AnimationMode enters plain preview, so we
        // also watch this each tick to refresh the inspector on those transitions.
        bool m_LastInAnimationMode;

        // The inspector this editor drives (search reset, dispose, recording state).
        protected abstract UIInspector Inspector { get; }

        // Recording context for this editor's target (element vs rule), or null when not applicable.
        protected abstract StyleInspectorAnimationRecordingContext CreateRecordingContext();

        protected virtual void OnEnable()
        {
            AnimationMode.onAnimationRecordingStart += OnRecordingStateChanged;
            AnimationMode.onAnimationRecordingStop += OnRecordingStateChanged;
            m_LastInAnimationMode = AnimationMode.InAnimationMode();
            EditorApplication.update += OnEditorUpdate;
            StageNavigationManager.instance.afterSuccessfullySwitchedToStage += OnStageChanged;
        }

        protected virtual void OnDisable()
        {
            AnimationMode.onAnimationRecordingStart -= OnRecordingStateChanged;
            AnimationMode.onAnimationRecordingStop -= OnRecordingStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= OnStageChanged;
        }

        protected virtual void OnDestroy() => Inspector?.Dispose();

        void OnStageChanged(Stage _) => Inspector?.ResetSearch();

        void OnRecordingStateChanged() => ApplyState();

        void OnEditorUpdate()
        {
            var inMode = AnimationMode.InAnimationMode();
            if (inMode == m_LastInAnimationMode)
                return;
            m_LastInAnimationMode = inMode;
            ApplyState();
        }

        // Recording is only armed when the panel-renderer animation feature is enabled.
        protected StyleInspectorAnimationRecordingContext CreateRecordingContextIfEnabled()
            => UIToolkitProjectSettings.s_EnablePanelRendererAnimationAtBoot ? CreateRecordingContext() : null;

        // Pushes the current recording context onto the inspector. Subclasses override to also apply
        // target-specific state (edit flags, header visibility, ...).
        protected virtual void ApplyState()
        {
            if (Inspector == null)
                return;
            Inspector.RefreshRecordingState(CreateRecordingContextIfEnabled());
        }

        public override bool UseDefaultMargins() => false; // no artificial padding
        internal override bool isHeaderSticky => true;
    }
}
