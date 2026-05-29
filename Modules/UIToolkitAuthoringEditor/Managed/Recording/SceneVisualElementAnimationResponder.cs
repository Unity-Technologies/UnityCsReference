// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.AnimationWindowBuiltin;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{

    internal sealed class SceneVisualElementAnimationResponder : IAnimationWindowResponder
    {
        public bool OnSelectionChange(AnimationWindow window, UnityObject selectedObject, out IAnimationWindowSelectionItem newSelection)
        {
            newSelection = null;

            if (!UIToolkitProjectSettings.s_EnablePanelRendererAnimationAtBoot)
                return false;

            bool inStage = StageUtility.GetCurrentStage() is VisualElementEditingStage;

            if (selectedObject is VisualElementSelection visualElementSelection)
                return TryEditVisualElement(window, visualElementSelection.Element, inStage, out newSelection);

            // Stage has no scene Component for the panel-wide / Animator route to target.
            if (inStage)
                return false;

            if (selectedObject is VisualTreeAssetSelection visualTreeAssetSelection)
                return TryEditPanelRoot(window, visualTreeAssetSelection.panelComponent, out newSelection);

            return false;
        }

        static bool TryEditVisualElement(AnimationWindow window, VisualElement element, bool inStage, out IAnimationWindowSelectionItem newSelection)
        {
            newSelection = null;

            if (element == null)
                return false;

            // Per-element UIAnimationClip takes priority: if the element (or one of its
            // ancestors) has unityAnimationClip set, the Animation Window edits that clip
            // through the VE-native pipeline rather than walking up to the panel's Animator.
            var clipOwner = VisualElementAnimationClipUtility.FindClipOwner(element);
            if (clipOwner != null)
            {
                // In stage there is no PanelRenderer; the per-element pipeline is GameObject-free.
                var panelRenderer = inStage ? null : VisualElementAnimationClipUtility.FindPanelRenderer(clipOwner);
                if (inStage || panelRenderer != null)
                    return TryEditPerElementClip(window, panelRenderer, clipOwner, clipOwner.resolvedStyle.unityAnimationClip, out newSelection);
            }

            if (inStage)
                return false;

            var rootElement = element.GetFirstAncestorOfType<IPanelComponentRootElement>();
            if (rootElement == null)
                return false;

            // When an Animator is in the parent chain, fall back to the standard
            // Animator-based workflow (current behavior).
            if (TryEditPanelRoot(window, rootElement.panelComponent, out newSelection))
                return true;

            // No clip and no Animator: offer per-element clip onboarding (CreateNewClip
            // will save a new UIAnimationClip and assign it to the element's style).
            if (rootElement.panelComponent is PanelRenderer pr)
            {
                newSelection = VisualElementAnimationSelectionItem.Create(window, pr, element, null);
                return true;
            }

            return false;
        }

        static bool TryEditPerElementClip(
            AnimationWindow window,
            PanelRenderer panelRenderer,
            VisualElement clipOwner,
            UIAnimationClip uiClip,
            out IAnimationWindowSelectionItem newSelection)
        {
            // If the existing selection already targets the same clip owner, reuse it so
            // the controller's preview state is preserved. Synchronize() will pick up the
            // current resolved clip if it changed under us.
            if (window.selection is VisualElementAnimationSelectionItem existing && existing.clipOwner == clipOwner)
            {
                existing.Synchronize();
                newSelection = existing;
                return true;
            }

            newSelection = VisualElementAnimationSelectionItem.Create(window, panelRenderer, clipOwner, uiClip);
            return true;
        }

        static bool TryEditPanelRoot(AnimationWindow window, IPanelComponent panelComponent, out IAnimationWindowSelectionItem newSelection)
        {
            newSelection = null;

            if (panelComponent == null)
                return false;

            var gameObject = panelComponent.gameObject;
            if (gameObject == null)
                return false;

            var animationPlayer = AnimationWindowSelectionItem.GetClosestAnimationPlayerComponentInParents(gameObject.transform);
            if (animationPlayer == null)
                return false;

            if (ShouldUpdateSelection(window.selection, animationPlayer))
            {
                newSelection = GameObjectSelectionItem.Create(window, gameObject);
                return true;
            }

            newSelection = window.selection;
            return true;
        }

        static bool ShouldUpdateSelection(IAnimationWindowSelectionItem selection, Component animationPlayer)
        {
            if (selection is not GameObjectSelectionItem)
                return true;

            if (animationPlayer != selection.animationPlayer)
                return true;

            if (selection.clip == null)
                return true;

            if (selection.rootGameObject != null)
            {
                var allClips = selection.GetClips();
                foreach (var x in allClips)
                {
                    if (x != null && x == selection.clip)
                        return true;
                }
            }

            return false;
        }
    }
}
