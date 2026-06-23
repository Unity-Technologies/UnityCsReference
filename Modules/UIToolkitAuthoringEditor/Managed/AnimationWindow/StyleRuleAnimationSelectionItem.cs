// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor
{
    // Animation Window selection item for a USS rule: drives preview/record (via the shared
    // VisualElementAnimationWindowController) against every staging-panel element the rule matches,
    // and the canonical match for single-target work. Element discovery re-walks the panel per call.
    internal sealed class StyleRuleAnimationSelectionItem : UIToolkitAnimationSelectionItemBase
    {
        static readonly string k_OnboardingLabelFallback = L10n.Tr("the selected style rule");

        // Renders the rule's selector the same way the inspector header does.
        static readonly StyleSheetNodeTypeHandler.StyleSheetEditorExporter s_SelectorExporter = new();

        readonly StyleSheet m_StyleSheet;
        readonly StyleRule m_Rule;

        // Lazily-allocated extractor reused across calls; MatchedRulesExtractor isn't thread-safe
        // but every consumer here is on the main editor thread.
        MatchedRulesExtractor m_MatchedRulesExtractor;

        // Reused DFS stack so the per-call panel walks don't allocate. Safe to share: traversal is
        // main-thread and non-reentrant (the fan-out action samples binders, it never re-walks).
        readonly Stack<VisualElement> m_TraversalStack = new();

        StyleRuleAnimationSelectionItem(AnimationWindow window, StyleSheet styleSheet, StyleRule rule)
            : base(window)
        {
            m_StyleSheet = styleSheet;
            m_Rule = rule;
        }

        internal static StyleRuleAnimationSelectionItem Create(AnimationWindow window, StyleSheet styleSheet, StyleRule rule)
        {
            var item = new StyleRuleAnimationSelectionItem(window, styleSheet, rule);
            item.m_Controller = new VisualElementAnimationWindowController(item);
            return item;
        }

        internal StyleSheet styleSheet => m_StyleSheet;
        internal StyleRule rule => m_Rule;

        // We only edit rules in the VisualElement editing stage (no scene Animator chain there).
        public override bool canCreateClips => m_Rule != null && m_StyleSheet != null && IsInVisualElementEditingStage();

        static bool IsInVisualElementEditingStage() => StageUtility.GetCurrentStage() is VisualElementEditingStage;

        // Subject is intentionally null so the save dialog uses its default wording, matching the
        // rule inspector's create-clip CTA (StyleRuleInspector wires ConnectButton with a null subject).
        protected override string DialogSubjectName => null;

        protected override void AssignClipToTarget(UIAnimationClip newClip)
        {
            SetStyleSheetPropertyCommand<StyleUIAnimationClip>.Execute(
                CommandSources.Inspector,
                m_StyleSheet,
                m_Rule,
                StylePropertyId.UnityAnimationClip,
                StylePropertyBinding.SetUIAnimationClip,
                new StyleUIAnimationClip(newClip));
        }

        // Stop any in-flight preview / record on the controller before dropping the clip so the
        // next selection-change event starts clean (mirrors the per-element path).
        protected override void OnClipCleared()
        {
            (m_Controller as VisualElementAnimationWindowController)?.StopImmediately();
        }

        public override string onboardingLabel
        {
            get
            {
                if (!IsInVisualElementEditingStage())
                    return null;
                var selector = TryGetSelectorText();
                var subject = string.IsNullOrEmpty(selector) ? k_OnboardingLabelFallback : $"'{selector}'";
                return string.Format(k_OnboardingLabelFormat, subject);
            }
        }

        string TryGetSelectorText()
        {
            if (m_Rule == null || m_StyleSheet == null || m_Rule.complexSelectors == null)
                return null;
            return s_SelectorExporter.ToUssString(m_StyleSheet, m_Rule.complexSelectors, StyleSheetNodeTypeHandler.s_ExportOptions);
        }

        public override int GetRefreshHash()
        {
            // Fold the clip's inner AnimationClip + dirty count in (mirrors the per-element item) so
            // creating/assigning a clip via onboarding flips the hash and the window refreshes; the
            // rule/stylesheet identity alone is constant across that transition.
            var animClip = m_Clip?.animationClip;
            uint dirtyCount = animClip != null ? (uint)EditorUtility.GetDirtyCount(animClip) : 0;
            return new Hash128(
                (uint)(m_Rule != null ? m_Rule.GetHashCode() : 0),
                (uint)(m_StyleSheet != null ? m_StyleSheet.GetHashCode() : 0),
                (uint)(animClip != null ? animClip.GetHashCode() : 0),
                dirtyCount).GetHashCode();
        }

        public override bool IsCompatibleWith(UnityEngine.Object selectedObject)
        {
            return selectedObject is StyleRuleSelection styleRuleSelection
                   && ReferenceEquals(styleRuleSelection.StyleRule, m_Rule);
        }

        // Sync the clip wrapper with the clip assigned to THIS rule. Reading the rule's own property
        // (not canonical.resolvedStyle) is deliberate: a matched element can also match a
        // higher-specificity rule whose clip would win the cascade, so resolvedStyle could surface a
        // different rule's clip and make us record onto the wrong one.
        public override void Synchronize()
        {
            var resolved = ReadClipFromRule();
            if (resolved == null)
            {
                ClearClip();
                return;
            }

            if (resolved != m_UIClip)
                SetUIClip(resolved);
        }

        UIAnimationClip ReadClipFromRule()
        {
            if (m_Rule == null || m_StyleSheet == null)
                return null;
            var property = m_Rule.FindLastProperty(StylePropertyId.UnityAnimationClip);
            if (property?.values == null || property.values.Length == 0)
                return null;
            // Skip keyword values (e.g. 'initial'/'none'); ReadAssetReference logs on a type mismatch.
            if (property.values[0].valueType != StyleValueType.AssetReference)
                return null;
            return m_StyleSheet.ReadAssetReference(property.values[0]) as UIAnimationClip;
        }

        // Preview / record surface ----------------------------------------------------------

        internal override void ForEachPreviewTarget(Action<VisualElement, UIAnimationBinder> action)
        {
            if (action == null || m_Rule == null)
                return;

            var root = TryGetStagingPanelRoot();
            if (root == null)
                return;

            var extractor = m_MatchedRulesExtractor ??= new MatchedRulesExtractor(AssetDatabase.GetAssetPath);
            EnumerateMatched(root, extractor, action);
        }

        internal override UIAnimationBinder GetCanonicalBinder()
        {
            var canonical = GetCanonicalElement();
            return (canonical?.panel as Panel)?.GetOrCreateElementBinder(canonical);
        }

        // First staging-panel element the rule matches; the rule item's representative for
        // GetCanonicalBinder, GetAnimatableBindings, and Synchronize.
        internal VisualElement GetCanonicalElement()
        {
            if (m_Rule == null)
                return null;
            var root = TryGetStagingPanelRoot();
            if (root == null)
                return null;
            var extractor = m_MatchedRulesExtractor ??= new MatchedRulesExtractor(AssetDatabase.GetAssetPath);
            return FindFirstMatch(root, extractor);
        }

        internal override void ActivateRecordingContext(UIAnimationClip clip, AnimationModeDriver driver)
        {
            if (clip == null || m_Rule == null)
                return;
            StyleRuleAnimationContext.SetActive(m_Rule, clip);
        }

        internal override void DeactivateRecordingContext(UIAnimationClip clip) => StyleRuleAnimationContext.ClearActive(clip);

        // Multi-element panels may have heterogeneous bindings; the canonical match is the authority.
        public override EditorCurveBinding[] GetAnimatableBindings(GameObject _) => GetAnimatableBindings();

        public override EditorCurveBinding[] GetAnimatableBindings()
            => GetAnimatableBindingsFromBinder(GetCanonicalBinder());

        // Helpers --------------------------------------------------------------------------

        static VisualElement TryGetStagingPanelRoot()
        {
            // Rule selections are only created in stage mode by SceneVisualElementAnimationResponder;
            // they can still outlive a stage exit, so handle the null gracefully.
            var stage = StageUtility.GetCurrentStage() as VisualElementEditingStage;
            return stage?.GetAuthoringPanel()?.visualTree;
        }

        void EnumerateMatched(VisualElement root, MatchedRulesExtractor extractor, Action<VisualElement, UIAnimationBinder> action)
        {
            var stack = m_TraversalStack;
            stack.Clear();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var element = stack.Pop();
                if (ElementMatchesRule(element, extractor))
                {
                    var binder = (element.panel as Panel)?.GetOrCreateElementBinder(element);
                    if (binder != null)
                        action(element, binder);
                }

                for (int i = 0; i < element.hierarchy.childCount; i++)
                    stack.Push(element.hierarchy[i]);
            }
        }

        VisualElement FindFirstMatch(VisualElement root, MatchedRulesExtractor extractor)
        {
            var stack = m_TraversalStack;
            stack.Clear();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var element = stack.Pop();
                if (ElementMatchesRule(element, extractor))
                {
                    stack.Clear();
                    return element;
                }

                for (int i = 0; i < element.hierarchy.childCount; i++)
                    stack.Push(element.hierarchy[i]);
            }
            return null;
        }

        bool ElementMatchesRule(VisualElement element, MatchedRulesExtractor extractor)
        {
            extractor.Clear();
            extractor.FindMatchingRules(element);
            var records = extractor.matchRecords;
            for (int i = 0; i < records.Count; i++)
            {
                if (ReferenceEquals(records[i].complexSelector?.rule, m_Rule))
                    return true;
            }
            return false;
        }
    }
}
