// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Animation Window controller for per-element <see cref="UIAnimationClip"/>s.
    /// Samples through the per-element <see cref="UIAnimationBinder"/> rather than the
    /// panel-wide binder driven by <c>AnimationMode.SampleAnimationClip</c>.
    /// </summary>
    [Serializable]
    internal sealed partial class VisualElementAnimationWindowController : IAnimationWindowController, IAnimationContextualResponder
    {
        struct SnapshotEntry
        {
            public string propertyName;
            public UIAnimationBinder.AnimationChannelKind kind;
            public float floatValue;
            public int intValue;
            public EntityId objectValue;
        }

        [NonSerialized] VisualElementAnimationSelectionItem m_Selection;

        [SerializeField] float m_Time;
        [SerializeField] int m_Frame;
        [SerializeField] AnimationModeDriver m_Driver;
        [SerializeField] AnimationModeDriver m_CandidateDriver;
        [SerializeField] AnimationClip m_CandidateClip;

        [NonSerialized] float m_PreviousUpdateTime;
        [NonSerialized] List<SnapshotEntry> m_PrePreviewSnapshot;
        [NonSerialized] bool m_PostprocessSubscribed;
        [NonSerialized] bool m_BinderHookSubscribed;

        internal VisualElementAnimationWindowController(VisualElementAnimationSelectionItem selection)
        {
            m_Selection = selection;
        }

        AnimationClip animationClip
        {
            get
            {
                var clip = m_Selection?.clip as UnityEditor.AnimationWindowBuiltin.AnimationWindowClip;
                return clip?.animationClip;
            }
        }

        float frameRate => animationClip != null ? animationClip.frameRate : 60f;

        public void OnSelectionChanged()
        {
            m_Time = 0f;
            m_Frame = 0;
            StopPreview();
        }

        public void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.ExitingEditMode)
            {
                StopPreview();
            }
        }

        public float time
        {
            get => m_Time;
            set => SetCurrentTime(value);
        }

        public int frame
        {
            get => m_Frame;
            set => SetCurrentFrame(value);
        }

        void SetCurrentTime(float value)
        {
            value = Mathf.Max(0f, value);
            if (!Mathf.Approximately(value, m_Time))
            {
                m_Time = value;
                m_Frame = Mathf.RoundToInt(value * frameRate);
                StartPreview();
                ResampleAnimation();
            }
        }

        void SetCurrentFrame(int value)
        {
            value = Mathf.Max(0, value);
            if (value != m_Frame)
            {
                m_Frame = value;
                m_Time = value / frameRate;
                StartPreview();
                ResampleAnimation();
            }
        }

        public bool canPlay => canPreview;

        public bool playing
        {
            get => AnimationMode.InAnimationPlaybackMode() && previewing;
            set
            {
                if (value)
                    StartPlayback();
                else
                    StopPlayback();
            }
        }

        void StartPlayback()
        {
            if (!canPlay || playing)
                return;
            AnimationMode.StartAnimationPlaybackMode();
            m_PreviousUpdateTime = Time.realtimeSinceStartup;
        }

        void StopPlayback()
        {
            if (AnimationMode.InAnimationPlaybackMode())
            {
                AnimationMode.StopAnimationPlaybackMode();
                m_Time = m_Frame / frameRate;
            }
        }

        public bool PlaybackUpdate()
        {
            var now = Time.realtimeSinceStartup;
            var dt = now - m_PreviousUpdateTime;
            m_PreviousUpdateTime = now;

            float newTime = m_Time + dt;
            float clipLen = animationClip != null ? animationClip.length : 0f;
            if (clipLen > 0f && newTime > clipLen)
                newTime = 0f;

            m_Time = Mathf.Max(0f, newTime);
            m_Frame = Mathf.RoundToInt(m_Time * frameRate);
            ResampleAnimation();
            return true;
        }

        public bool canPreview
        {
            get
            {
                if (m_Selection == null || m_Selection.disabled)
                    return false;

                var driver = m_Driver;
                return !AnimationMode.InAnimationMode()
                       || (driver != null && AnimationMode.InAnimationMode(driver));
            }
        }

        public bool previewing
        {
            get
            {
                var driver = m_Driver;
                return driver != null && AnimationMode.InAnimationMode(driver);
            }
            set
            {
                if (value)
                    StartPreview();
                else
                    StopPreview();
            }
        }

        void StartPreview()
        {
            if (previewing || !canPreview)
                return;

            var uiClip = m_Selection?.uiAnimationClip;
            var clipOwner = m_Selection?.clipOwner;
            var binder = m_Selection?.GetOrCreateElementBinder();
            var driver = GetOrCreateDriver();

            // Capture pre-preview values before entering animation mode so StopPreview can
            // restore them; native RevertAnimatedProperties only walks Component targets.
            CapturePrePreviewSnapshot(uiClip, binder);

            AnimationMode.StartAnimationMode(driver);

            if (uiClip != null && clipOwner != null && binder != null)
                PerElementAnimationContext.SetActive(uiClip, clipOwner, binder, driver);

            clipOwner?.SetUIAnimationClipPreviewing(true);

            SubscribeBinderHook();
            SubscribeUndoPostprocess();

            CreateCandidateClip();

            // Owns the inspector style-property context menu for the per-element clip
            // path; mirrors AnimationWindowControl's panel-wide registration so only
            // one responder is active for the current Animation Window selection.
            AnimationPropertyContextualMenu.Instance.SetResponder(this);

            ResampleAnimation();
        }

        void StopPreview()
        {
            if (!previewing)
            {
                ClearTransientPreviewState();
                return;
            }

            UnsubscribeUndoPostprocess();
            UnsubscribeBinderHook();
            StopCandidateRecording();
            DestroyCandidateClip();

            // Release the inspector context menu hand-off so a follow-up panel-wide
            // selection's AnimationWindowControl can claim it cleanly.
            if (AnimationPropertyContextualMenu.Instance.IsResponder(this))
                AnimationPropertyContextualMenu.Instance.SetResponder(null);

            m_Selection?.clipOwner?.SetUIAnimationClipPreviewing(false);

            // Restore snapshot before StopAnimationMode / UnregisterProperties so the inspector
            // tint sees the original values while registrations are still active.
            RestoreSnapshot();

            // UIAnimationClip targets aren't Components; clear them explicitly since the
            // native StopAnimationMode revert walk only handles Component targets.
            if (m_Driver != null)
                DrivenPropertyManager.UnregisterProperties(m_Driver);

            // Covers curves added/removed mid-preview that the snapshot misses.
            var stopBinder = m_Selection?.GetOrCreateElementBinder();
            if (stopBinder != null)
                stopBinder.IncrementBoundElementsStyleVersion();

            PerElementAnimationContext.ClearActive(m_Selection?.uiAnimationClip);

            AnimationMode.StopAnimationMode(GetOrCreateDriver());
        }

        void ClearTransientPreviewState()
        {
            UnsubscribeUndoPostprocess();
            UnsubscribeBinderHook();
            StopCandidateRecording();
            DestroyCandidateClip();
            m_PrePreviewSnapshot = null;
            // Defensive: in case of race conditions
            m_Selection?.clipOwner?.SetUIAnimationClipPreviewing(false);
            if (AnimationPropertyContextualMenu.Instance.IsResponder(this))
                AnimationPropertyContextualMenu.Instance.SetResponder(null);
            PerElementAnimationContext.ClearActive(m_Selection?.uiAnimationClip);
        }

        // Capability, not state - mirrors AnimationWindowControl.canRecord. Must NOT include
        // `previewing` or AnimationWindowState's auto-preview-on-record branch can't fire.
        public bool canRecord => canPreview && m_Selection != null && !m_Selection.isReadOnly;

        public bool recording
        {
            get => previewing && AnimationMode.InAnimationRecording();
            set
            {
                if (value)
                    StartRecording();
                else
                    StopRecording();
            }
        }

        void StartRecording()
        {
            if (!canRecord || recording)
                return;
            AnimationMode.StartAnimationRecording();
            ClearCandidates();
        }

        void StopRecording()
        {
            if (!recording)
                return;
            AnimationMode.StopAnimationRecording();
        }

        public void ResampleAnimation()
        {
            if (m_Selection == null || m_Selection.disabled)
                return;
            if (!canPreview || !previewing)
                return;

            var uiClip = m_Selection.uiAnimationClip;
            if (uiClip == null)
                return;

            var binder = m_Selection.GetOrCreateElementBinder();
            if (binder == null)
                return;

            AnimationMode.BeginSampling();
            Undo.FlushUndoRecordObjects();

            // SampleClipForEditor fires editorPostSample, where we re-assert
            // DrivenPropertyManager registrations (see OnBinderEditorPostSample).
            binder.SampleClipForEditor(uiClip, m_Time);

            // Re-apply candidate keys on top of the previewed pose, mirroring
            // AnimationWindowControl.ResampleAnimation's m_CandidateClip handling.
            if (m_CandidateClip != null && AnimationUtility.GetCurveBindings(m_CandidateClip).Length > 0)
                binder.SampleClipForEditor(WrapAsUIAnimationClip(m_CandidateClip), 0f);

            AnimationMode.EndSampling();

            SceneView.RepaintAll();
        }

        // Lazy wrapper so the candidate AnimationClip can flow through the
        // UIAnimationClip-typed SampleClipForEditor entry point.
        [NonSerialized] UIAnimationClip m_CandidateUIClip;

        UIAnimationClip WrapAsUIAnimationClip(AnimationClip inner)
        {
            if (m_CandidateUIClip == null)
            {
                m_CandidateUIClip = new UIAnimationClip
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    name = "VisualElementCandidateUIClip",
                };
            }
            if (m_CandidateUIClip.animationClip != inner)
                m_CandidateUIClip.animationClip = inner;
            return m_CandidateUIClip;
        }

        public void ProcessCandidates()
        {
            if (m_CandidateClip == null)
                return;

            var floatBindings = AnimationUtility.GetCurveBindings(m_CandidateClip);
            var pptrBindings = AnimationUtility.GetObjectReferenceCurveBindings(m_CandidateClip);
            if (floatBindings.Length == 0 && pptrBindings.Length == 0)
            {
                ClearCandidates();
                return;
            }

            var animClip = animationClip;
            if (animClip == null)
            {
                ClearCandidates();
                return;
            }

            Undo.RegisterCompleteObjectUndo(animClip, "Promote Candidate Keys");

            foreach (var binding in floatBindings)
            {
                var candidateCurve = AnimationUtility.GetEditorCurve(m_CandidateClip, binding);
                if (candidateCurve == null || candidateCurve.length == 0)
                    continue;

                var existing = AnimationUtility.GetEditorCurve(animClip, binding) ?? new AnimationCurve();

                // The candidate clip stores values at t=0 (single key per binding);
                // promote them to a key at the playhead time on the active clip.
                float candidateValue = candidateCurve.keys[0].value;
                AnimationClipKeyEditing.AddOrReplaceFloatKey(existing, m_Time, candidateValue);
                AnimationUtility.SetEditorCurve(animClip, binding, existing);
            }

            foreach (var binding in pptrBindings)
            {
                var candidatePptrs = AnimationUtility.GetObjectReferenceCurve(m_CandidateClip, binding);
                if (candidatePptrs == null || candidatePptrs.Length == 0)
                    continue;

                var existing = AnimationUtility.GetObjectReferenceCurve(animClip, binding) ?? Array.Empty<ObjectReferenceKeyframe>();
                Object candidateRef = candidatePptrs[0].value;
                var merged = AnimationClipKeyEditing.ReplaceObjectReferenceKey(existing, m_Time, candidateRef);
                AnimationUtility.SetObjectReferenceCurve(animClip, binding, merged);
            }

            ClearCandidates();
        }

        public void ClearCandidates()
        {
            StopCandidateRecording();
            if (m_CandidateClip != null)
                m_CandidateClip.ClearCurves();
        }

        void CreateCandidateClip()
        {
            if (m_CandidateClip == null)
            {
                m_CandidateClip = new AnimationClip { name = "VECandidateClip", hideFlags = HideFlags.HideAndDontSave };
            }
        }

        void DestroyCandidateClip()
        {
            if (m_CandidateClip != null)
            {
                Object.DestroyImmediate(m_CandidateClip);
                m_CandidateClip = null;
            }
            if (m_CandidateUIClip != null)
            {
                Object.DestroyImmediate(m_CandidateUIClip);
                m_CandidateUIClip = null;
            }
        }

        void StartCandidateRecording()
        {
            if (m_CandidateDriver == null)
            {
                m_CandidateDriver = ScriptableObject.CreateInstance<AnimationModeDriver>();
                m_CandidateDriver.hideFlags = HideFlags.HideAndDontSave;
                m_CandidateDriver.name = "VisualElementCandidateDriver";
            }
            AnimationMode.StartCandidateRecording(m_CandidateDriver);
        }

        void StopCandidateRecording()
        {
            AnimationMode.StopCandidateRecording();
        }

        // GetFloatValue / GetIntValue / GetObjectReferenceValue seed the default keyframe
        // value for newly-added properties via CurveBindingUtility.GetCurrentValue. We
        // resolve through the binder so the default keyframe captures the element's
        // current resolved style instead of a placeholder zero.
        public float GetFloatValue(EditorCurveBinding binding)
        {
            return TryReadBoundValue(binding, out var kind, out var f, out var i, out _)
                && kind == UIAnimationBinder.AnimationChannelKind.Float
                ? f
                : kind == UIAnimationBinder.AnimationChannelKind.Int ? i : 0f;
        }

        public int GetIntValue(EditorCurveBinding binding)
        {
            return TryReadBoundValue(binding, out var kind, out _, out var i, out _) && kind == UIAnimationBinder.AnimationChannelKind.Int
                ? i
                : 0;
        }

        public Object GetObjectReferenceValue(EditorCurveBinding binding)
        {
            if (!TryReadBoundValue(binding, out var kind, out _, out _, out var entityId)
                || kind != UIAnimationBinder.AnimationChannelKind.PPtr)
                return null;
            return Resources.EntityIdToObject(entityId);
        }

        bool TryReadBoundValue(EditorCurveBinding binding,
            out UIAnimationBinder.AnimationChannelKind kind,
            out float floatValue,
            out int intValue,
            out EntityId objectValue)
        {
            kind = UIAnimationBinder.AnimationChannelKind.Float;
            floatValue = 0f;
            intValue = 0;
            objectValue = EntityId.None;

            if (m_Selection == null)
                return false;

            var binder = m_Selection.GetOrCreateElementBinder();
            if (binder == null)
                return false;
            return binder.TryReadCurrentBoundValue(binding.propertyName, out kind, out floatValue, out intValue, out objectValue);
        }

        AnimationModeDriver GetOrCreateDriver()
        {
            if (m_Driver == null)
            {
                m_Driver = ScriptableObject.CreateInstance<AnimationModeDriver>();
                m_Driver.hideFlags = HideFlags.HideAndDontSave;
                m_Driver.name = "VisualElementAnimationDriver";
            }
            return m_Driver;
        }

        // Re-asserts per-binding driven properties on each sample so inspector tint and
        // stop-preview revert flow uniformly through DrivenPropertyManager.
        // Routed via AnimationMode.AddPropertyModification (not DrivenPropertyManager
        // directly) so non-Component targets like UIAnimationClip bypass the native
        // ExtractPropertyModification check that requires serialized-field paths.
        void OnBinderEditorPostSample(UIAnimationBinder binder, UIAnimationClip clip, float time)
        {
            if (m_Selection == null)
                return;
            if (!ReferenceEquals(binder, m_Selection.GetOrCreateElementBinder()))
                return;
            // Skip ephemeral candidate-clip samples (WrapAsUIAnimationClip wrapper).
            if (!ReferenceEquals(clip, m_Selection.uiAnimationClip))
                return;

            var driver = m_Driver;
            if (driver == null)
                return;

            var animClip = clip != null ? clip.animationClip : null;
            if (animClip == null)
                return;

            // AddPropertyModification asserts InAnimationMode native-side.
            if (!AnimationMode.InAnimationMode())
                return;

            foreach (var binding in AnimationUtility.GetCurveBindings(animClip))
            {
                var modification = new PropertyModification
                {
                    target = clip,
                    propertyPath = binding.propertyName,
                    value = string.Empty,
                };
                AnimationMode.AddPropertyModification(binding, modification, keepPrefabOverride: false);
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(animClip))
            {
                var modification = new PropertyModification
                {
                    target = clip,
                    propertyPath = binding.propertyName,
                    value = string.Empty,
                };
                AnimationMode.AddPropertyModification(binding, modification, keepPrefabOverride: false);
            }
        }

        void SubscribeBinderHook()
        {
            if (m_BinderHookSubscribed)
                return;
            UIAnimationBinder.editorPostSample += OnBinderEditorPostSample;
            m_BinderHookSubscribed = true;
        }

        void UnsubscribeBinderHook()
        {
            if (!m_BinderHookSubscribed)
                return;
            UIAnimationBinder.editorPostSample -= OnBinderEditorPostSample;
            m_BinderHookSubscribed = false;
        }

        void SubscribeUndoPostprocess()
        {
            if (m_PostprocessSubscribed)
                return;
            Undo.postprocessModifications += PostprocessModifications;
            m_PostprocessSubscribed = true;
        }

        void UnsubscribeUndoPostprocess()
        {
            if (!m_PostprocessSubscribed)
                return;
            Undo.postprocessModifications -= PostprocessModifications;
            m_PostprocessSubscribed = false;
        }

        // Consumes synthetic UndoPropertyModifications targeting our active UIAnimationClip;
        // anything else is returned unchanged for the standard panel-wide path.
        UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (!previewing)
                return modifications;

            var uiClip = m_Selection?.uiAnimationClip;
            if (uiClip == null)
                return modifications;

            List<UndoPropertyModification> consumed = null;
            List<UndoPropertyModification> remaining = null;

            for (int i = 0; i < modifications.Length; i++)
            {
                var mod = modifications[i];
                var target = mod.previousValue?.target;
                if (!ReferenceEquals(target, uiClip))
                {
                    (remaining ??= new List<UndoPropertyModification>()).Add(mod);
                    continue;
                }
                (consumed ??= new List<UndoPropertyModification>()).Add(mod);
            }

            if (consumed == null)
                return modifications;

            ProcessConsumedModifications(uiClip, consumed);

            // Undo expects an empty array (not null) when fully consumed.
            return remaining?.ToArray() ?? Array.Empty<UndoPropertyModification>();
        }

        void ProcessConsumedModifications(UIAnimationClip uiClip, List<UndoPropertyModification> consumed)
        {
            var animClip = uiClip.animationClip;
            if (animClip == null)
                return;

            bool isRecording = AnimationMode.InAnimationRecording();
            CreateCandidateClip();
            if (!isRecording && m_CandidateDriver == null)
                StartCandidateRecording();

            if (isRecording)
                Undo.RegisterCompleteObjectUndo(animClip, "Record Visual Element Animation");

            foreach (var mod in consumed)
            {
                var prop = mod.currentValue ?? mod.previousValue;
                if (prop == null || string.IsNullOrEmpty(prop.propertyPath))
                    continue;

                // PPtr modifications carry the value on prop.objectReference and have an
                // empty `prop.value`, so they must be split out before the float-parse gate.
                bool hasChannelKind = UIAnimationBinderEditorExtensions.TryGetChannelKindForBinding(prop.propertyPath, out var channelKind);

                if (hasChannelKind && channelKind == UIAnimationBinder.AnimationChannelKind.PPtr)
                {
                    var binding = EditorCurveBinding.PPtrCurve(string.Empty, VisualElementAnimationClipUtility.PerElementPPtrDiscriminatorType, prop.propertyPath);
                    var objectValue = prop.objectReference;

                    if (isRecording)
                    {
                        AnimationClipKeyEditing.AddOrReplaceObjectReferenceKey(animClip, binding, m_Time, objectValue);
                        AnimationMode.AddPropertyModification(binding, prop, mod.keepPrefabOverride);
                    }
                    else
                    {
                        AnimationClipKeyEditing.AddOrReplaceObjectReferenceKey(m_CandidateClip, binding, 0f, objectValue);
                        AnimationMode.AddCandidate(binding, prop, mod.keepPrefabOverride);
                    }
                    continue;
                }

                if (!float.TryParse(prop.value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                    continue;

                // Int channels store the ordinal as a bit-encoded float (decoded by
                // UIAnimationBinder.SetFloatValue's Int/Enum branches via SingleToInt32Bits).
                EditorCurveBinding floatBinding;
                float storedValue = floatValue;
                if (hasChannelKind && channelKind == UIAnimationBinder.AnimationChannelKind.Int)
                {
                    floatBinding = EditorCurveBinding.DiscreteCurve(string.Empty, typeof(UIAnimationClip), prop.propertyPath);
                    storedValue = BitConverter.Int32BitsToSingle((int)floatValue);
                }
                else
                {
                    floatBinding = EditorCurveBinding.FloatCurve(string.Empty, typeof(UIAnimationClip), prop.propertyPath);
                }

                if (isRecording)
                {
                    AnimationClipKeyEditing.AddOrReplaceKey(animClip, floatBinding, m_Time, storedValue);
                    AnimationMode.AddPropertyModification(floatBinding, prop, mod.keepPrefabOverride);
                }
                else
                {
                    AnimationClipKeyEditing.AddOrReplaceKey(m_CandidateClip, floatBinding, 0f, storedValue);
                    AnimationMode.AddCandidate(floatBinding, prop, mod.keepPrefabOverride);
                }
            }

            if (isRecording)
                ResampleAnimation();
            else
                InspectorWindow.RepaintAllInspectors();
        }

        void CapturePrePreviewSnapshot(UIAnimationClip uiClip, UIAnimationBinder binder)
        {
            m_PrePreviewSnapshot = null;
            if (uiClip == null || binder == null)
                return;

            var animClip = uiClip.animationClip;
            if (animClip == null)
                return;

            binder.UpdateElementNamesIfNeeded();

            var snapshot = new List<SnapshotEntry>();

            foreach (var binding in AnimationUtility.GetCurveBindings(animClip))
                AppendSnapshotEntry(binder, binding.propertyName, snapshot);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(animClip))
                AppendSnapshotEntry(binder, binding.propertyName, snapshot);

            m_PrePreviewSnapshot = snapshot;
        }

        static void AppendSnapshotEntry(UIAnimationBinder binder, string propertyName, List<SnapshotEntry> snapshot)
        {
            if (string.IsNullOrEmpty(propertyName))
                return;
            // Skip duplicates - a binding's propertyName may appear on both float and PPtr channels.
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (string.Equals(snapshot[i].propertyName, propertyName, StringComparison.Ordinal))
                    return;
            }
            if (!binder.TryReadCurrentBoundValue(propertyName, out var kind, out var f, out var iv, out var ev))
                return;
            snapshot.Add(new SnapshotEntry
            {
                propertyName = propertyName,
                kind = kind,
                floatValue = f,
                intValue = iv,
                objectValue = ev,
            });
        }

        void RestoreSnapshot()
        {
            if (m_PrePreviewSnapshot == null || m_Selection == null)
            {
                m_PrePreviewSnapshot = null;
                return;
            }

            var binder = m_Selection.GetOrCreateElementBinder();
            if (binder == null)
            {
                m_PrePreviewSnapshot = null;
                return;
            }

            foreach (var s in m_PrePreviewSnapshot)
            {
                switch (s.kind)
                {
                    case UIAnimationBinder.AnimationChannelKind.Float:
                    case UIAnimationBinder.AnimationChannelKind.Int:
                        binder.TryApplyBoundFloatValue(s.propertyName, s.floatValue);
                        break;
                    case UIAnimationBinder.AnimationChannelKind.PPtr:
                        binder.TryApplyBoundObjectValue(s.propertyName, s.objectValue);
                        break;
                }
            }

            m_PrePreviewSnapshot = null;
        }

        /// <summary>
        /// Unconditionally stops playback and preview. Called when the clip is removed
        /// from the element so we mirror the Animator-deletion behavior of the standard
        /// controller.
        /// </summary>
        internal void StopImmediately()
        {
            StopPlayback();
            StopPreview();
        }

        public void Dispose()
        {
            // Drain the recording/playing setters before StopPreview so AnimationMode's
            // managed global flags (s_InAnimationRecordMode / s_InAnimationPlaybackMode)
            // get cleared on selection change. AnimationWindowControl has the same gap.
            recording = false;
            playing = false;
            StopPreview();
            UnsubscribeBinderHook();
            UnsubscribeUndoPostprocess();
            DestroyCandidateClip();
            if (m_Driver != null)
            {
                ScriptableObject.DestroyImmediate(m_Driver);
                m_Driver = null;
            }
            if (m_CandidateDriver != null)
            {
                ScriptableObject.DestroyImmediate(m_CandidateDriver);
                m_CandidateDriver = null;
            }
        }
    }
}
