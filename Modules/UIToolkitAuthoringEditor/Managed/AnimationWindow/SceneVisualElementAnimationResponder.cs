// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.AnimationWindowBuiltin;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Animation Window responder for visual elements under a PanelRenderer in the hierarchy.
    /// When a VisualElementSelection or VisualTreeAssetSelection is selected (main scene or Prefab Stage),
    /// resolves to the panel's GameObject and sets the Animation Window context so the correct clip is shown.
    /// Returns false when in VisualElementEditingStage (staging mode) so the scene context is not applied.
    /// </summary>
    internal sealed class SceneVisualElementAnimationResponder : IAnimationWindowResponder
    {
        public bool OnSelectionChange(AnimationWindow window, UnityObject selectedObject, out IAnimationWindowSelectionItem newSelection)
        {
            newSelection = null;

            if (StageUtility.GetCurrentStage() is VisualElementEditingStage)
                return false;

            if (selectedObject is VisualElementSelection visualElementSelection)
                return TryEditVisualElement(window, visualElementSelection.Element, out newSelection);

            if (selectedObject is VisualTreeAssetSelection visualTreeAssetSelection)
                return TryEditPanelRoot(window, visualTreeAssetSelection.panelComponent, out newSelection);

            return false;
        }

        static bool TryEditVisualElement(AnimationWindow window, VisualElement element, out IAnimationWindowSelectionItem newSelection)
        {
            newSelection = null;

            if (element == null)
                return false;

            var rootElement = element.GetFirstAncestorOfType<IPanelComponentRootElement>();
            if (rootElement == null)
                return false;

            return TryEditPanelRoot(window, rootElement.panelComponent, out newSelection);
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
