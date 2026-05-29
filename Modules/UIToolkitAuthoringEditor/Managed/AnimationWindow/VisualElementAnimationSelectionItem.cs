// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.AnimationWindowBuiltin;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Animation Window selection item for per-element <see cref="UIAnimationClip"/>s.
    /// There is no scene proxy: the "Add Property" menu is populated directly from the
    /// per-element <see cref="UIAnimationBinder"/>'s channel set.
    /// </summary>
    internal sealed class VisualElementAnimationSelectionItem : IAnimationWindowSelectionItem
    {
        readonly AnimationWindow m_Window;
        readonly PanelRenderer m_PanelRenderer;
        readonly VisualElement m_ClipOwner;
        UIAnimationClip m_UIClip;
        AnimationWindowClip m_Clip;
        IAnimationWindowController m_Controller;

        VisualElementAnimationSelectionItem(
            AnimationWindow window,
            PanelRenderer panelRenderer,
            VisualElement clipOwner,
            UIAnimationClip uiClip)
        {
            m_Window = window;
            m_PanelRenderer = panelRenderer;
            m_ClipOwner = clipOwner;
            SetUIClip(uiClip);
        }

        internal static VisualElementAnimationSelectionItem Create(
            AnimationWindow window,
            PanelRenderer panelRenderer,
            VisualElement clipOwner,
            UIAnimationClip uiClip)
        {
            var item = new VisualElementAnimationSelectionItem(window, panelRenderer, clipOwner, uiClip);
            item.m_Controller = new VisualElementAnimationWindowController(item);
            return item;
        }

        void SetUIClip(UIAnimationClip uiClip)
        {
            if (uiClip != null && uiClip.animationClip == null)
                EnsureInnerAnimationClip(uiClip);

            m_UIClip = uiClip;
            var inner = uiClip != null ? uiClip.animationClip : null;
            m_Clip = inner != null ? new VisualElementAnimationWindowClip(inner) : null;
        }

        // Without an inner AnimationClip, IAnimationWindowSelectionItem.disabled reports
        // true and the Animation Window paints "No animatable object selected".
        static void EnsureInnerAnimationClip(UIAnimationClip uiClip)
        {
            var innerClip = new AnimationClip();
            var settings = AnimationUtility.GetAnimationClipSettings(innerClip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(innerClip, settings);

            uiClip.animationClip = innerClip;

            if (AssetDatabase.IsMainAsset(uiClip))
            {
                innerClip.name = uiClip.name;
                AssetDatabase.AddObjectToAsset(innerClip, uiClip);
                AssetDatabase.SaveAssetIfDirty(uiClip);
            }
        }

        internal VisualElement clipOwner => m_ClipOwner;
        internal PanelRenderer panelRenderer => m_PanelRenderer;
        internal AnimationWindow animationWindow => m_Window;
        internal UIAnimationClip uiAnimationClip => m_UIClip;

        internal UIAnimationBinder GetOrCreateElementBinder()
        {
            var concretePanel = m_ClipOwner?.panel as Panel;
            return concretePanel?.GetOrCreateElementBinder(m_ClipOwner);
        }

        // The VE path is GameObject-less - returning null engages the Animation Window's
        // curve-display fallbacks (evaluate curves directly, no AnimationUtility lookup).
        public GameObject gameObject => null;
        public GameObject rootGameObject => null;
        public Component animationPlayer => null;

        public IAnimationWindowClip clip
        {
            get => m_Clip;
            set => m_Clip = value as AnimationWindowClip;
        }

        public IAnimationWindowController controller => m_Controller;

        public bool disabled => m_Clip == null || !m_Clip.isValid;
        public bool isReadOnly => m_Clip != null && m_Clip.isReadOnly;
        public bool canChangeClip => true;
        // Gating on `disabled` keeps the toolbar "+" popup and the inline tree-row "Add
        // Property" button in sync; otherwise the popup lists properties that
        // CreateDefaultCurves would silently drop because selection.clip is null.
        public bool canAddCurves => !disabled && !isReadOnly;
        public bool canCreateClips => m_ClipOwner != null;
        public bool canSyncSceneSelection => true;

        public IAnimationWindowClip[] GetClips()
        {
            if (m_Clip == null || !m_Clip.isValid)
                return Array.Empty<IAnimationWindowClip>();
            return new IAnimationWindowClip[] { m_Clip };
        }

        public IAnimationWindowClip CreateNewClip()
        {
            if (m_ClipOwner == null)
                return null;

            string elementName = string.IsNullOrEmpty(m_ClipOwner.name)
                ? (m_PanelRenderer != null ? m_PanelRenderer.gameObject.name : "VisualElement")
                : m_ClipOwner.name;

            string message = $"Create a new animation for element '{elementName}':";
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Animation", "New Animation", "asset", message, "Assets");

            if (string.IsNullOrEmpty(path))
                return null;

            var innerClip = new AnimationClip();
            var settings = AnimationUtility.GetAnimationClipSettings(innerClip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(innerClip, settings);

            var newUIClip = new UIAnimationClip { animationClip = innerClip };
            AssetDatabase.CreateAsset(newUIClip, path);
            AssetDatabase.AddObjectToAsset(innerClip, newUIClip);
            AssetDatabase.SaveAssets();

            var loaded = AssetDatabase.LoadAssetAtPath<UIAnimationClip>(path);
            if (loaded != null)
                newUIClip = loaded;

            m_ClipOwner.style.unityAnimationClip = new StyleUIAnimationClip(newUIClip);
            SetUIClip(newUIClip);
            return m_Clip;
        }

        public bool InitializeSelection() => CreateNewClip() != null;

        public int GetRefreshHash()
        {
            var animClip = m_Clip?.animationClip;
            uint dirtyCount = animClip != null ? (uint)EditorUtility.GetDirtyCount(animClip) : 0;
            return new Hash128(
                (uint)(animClip != null ? animClip.GetHashCode() : 0),
                (uint)(m_ClipOwner != null ? m_ClipOwner.GetHashCode() : 0),
                (uint)(m_PanelRenderer != null ? m_PanelRenderer.GetHashCode() : 0),
                dirtyCount).GetHashCode();
        }

        public void Synchronize()
        {
            // Treat a released or detached owner as "no clip" - reading resolvedStyle on a
            // recycled layout node throws from LayoutDataAccess. The responder will pick
            // up a fresh selection on the next selection-change event (Animator-deletion
            // behavior).
            if (m_ClipOwner == null || m_ClipOwner.resourcesReleased)
            {
                ClearClip();
                return;
            }

            var resolvedClip = m_ClipOwner.resolvedStyle.unityAnimationClip;
            if (resolvedClip == null)
            {
                ClearClip();
                return;
            }

            if (resolvedClip != m_UIClip)
                SetUIClip(resolvedClip);
        }

        void ClearClip()
        {
            if (m_Clip == null)
                return;

            (m_Controller as VisualElementAnimationWindowController)?.StopImmediately();
            SetUIClip(null);
        }

        public bool IsCompatibleWith(UnityEngine.Object selectedObject)
        {
            if (selectedObject is VisualElementSelection veSelection && veSelection.Element != null)
            {
                var owner = VisualElementAnimationClipUtility.FindClipOwner(veSelection.Element);
                return owner != null && owner == m_ClipOwner;
            }
            return false;
        }

        public EditorCurveBinding[] GetAnimatableBindings(GameObject go) => GetAnimatableBindings();

        public EditorCurveBinding[] GetAnimatableBindings()
        {
            if (m_ClipOwner == null)
                return Array.Empty<EditorCurveBinding>();

            var binder = GetOrCreateElementBinder();
            if (binder == null)
                return Array.Empty<EditorCurveBinding>();

            binder.UpdateElementNamesIfNeeded();
            var bindings = UIAnimationBinderEditorBindings.GetAllAnimatableProperties(binder, typeof(UIAnimationClip));

            // PPtr rows need a Component-derived discriminator; see PerElementPPtrDiscriminatorType.
            for (int i = 0; i < bindings.Length; i++)
            {
                if (!bindings[i].isPPtrCurve)
                    continue;
                if (typeof(Component).IsAssignableFrom(bindings[i].type))
                    continue;
                bindings[i] = EditorCurveBinding.PPtrCurve(
                    bindings[i].path,
                    VisualElementAnimationClipUtility.PerElementPPtrDiscriminatorType,
                    bindings[i].propertyName);
            }
            return bindings;
        }

        public Type GetValueType(EditorCurveBinding binding)
        {
            // Per-element clips have no GameObject root; rely on the binding's
            // self-description (set by the native channel walk) so style-enum
            // rows render as discrete int curves instead of continuous floats.
            if (binding.isPPtrCurve)
                return null;
            if (binding.isDiscreteCurve)
                return typeof(int);
            return typeof(float);
        }

        public bool isImported => false;
        public bool hasUnsavedChanges => false;
        public void SaveChanges() { }
        public void DiscardChanges() { }

        public void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            m_Controller?.OnPlayModeStateChanged(state);
        }

        public void Dispose()
        {
            m_Controller?.Dispose();
            m_Controller = null;
        }
    }
}
