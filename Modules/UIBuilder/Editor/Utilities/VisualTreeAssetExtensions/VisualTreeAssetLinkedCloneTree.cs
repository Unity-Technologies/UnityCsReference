// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Assertions;
using System;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal static class VisualTreeAssetLinkedCloneTree
    {
        static readonly StylePropertyReader s_StylePropertyReader = new StylePropertyReader();
        static readonly Dictionary<string, VisualElement> s_TemporarySlotInsertionPoints = new Dictionary<string, VisualElement>();

        static VisualElement CloneSetupRecursively(VisualTreeAsset vta, VisualElementAsset root,
            Dictionary<int, List<VisualElementAsset>> idToChildren, CreationContext context)
        {
            var ve = VisualTreeAsset.Create(root, context);

            // Linking the new element with its VisualElementAsset.
            // All this copied code for this one line!
            ve.SetVisualElementAsset(root);
            ve.SetProperty(BuilderConstants.ElementLinkedBelongingVisualTreeAssetVEPropertyName, vta);

            // context.target is the created templateContainer
            if (root.id == context.visualTreeAsset.contentContainerId)
            {
                if (context.target is TemplateContainer)
                    ((TemplateContainer)context.target).SetContentContainer(ve);
                else
                    Debug.LogError(
                        "Trying to clone a VisualTreeAsset with a custom content container into a element which is not a template container");
            }

            // if the current element had a slot-name attribute, put it in the resulting slot mapping
            string slotName;
            if (context.slotInsertionPoints != null && vta.TryGetSlotInsertionPoint(root.id, out slotName))
            {
                context.slotInsertionPoints.Add(slotName, ve);
            }

            if (root.classes != null)
            {
                for (int i = 0; i < root.classes.Length; i++)
                {
                    ve.AddToClassList(root.classes[i]);
                }
            }

            if (root.ruleIndex != -1)
            {
                if (vta.inlineSheet == null)
                    Debug.LogWarning("VisualElementAsset has a RuleIndex but no inlineStyleSheet");
                else
                {
                    var rule = vta.inlineSheet.rules[root.ruleIndex];
                    ve.UpdateInlineRule(vta.inlineSheet, rule);
                }
            }

            var templateAsset = root as TemplateAsset;
            if (templateAsset != null)
            {
                var templatePath = vta.GetPathFromTemplateName(templateAsset.templateAlias);
                ve.SetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName, templatePath);
                var instancedTemplateVTA = vta.ResolveTemplate(templateAsset.templateAlias);
                if (instancedTemplateVTA != null)
                    ve.SetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName, instancedTemplateVTA);
            }

            List<VisualElementAsset> children;
            if (idToChildren.TryGetValue(root.id, out children))
            {
                children.Sort(VisualTreeAssetUtilities.CompareForOrder);

                foreach (VisualElementAsset childVea in children)
                {
                    // this will fill the slotInsertionPoints mapping
                    var childVe = CloneSetupRecursively(vta, childVea, idToChildren, context);
                    if (childVe == null)
                        continue;

                    // if the parent is not a template asset, just add the child to whatever hierarchy we currently have
                    // if ve is a scrollView (with contentViewport as contentContainer), this will go to the right place
                    if (templateAsset == null)
                    {
                        ve.Add(childVe);
                        continue;
                    }

                    int index = templateAsset.slotUsages == null
                        ? -1
                        : templateAsset.slotUsages.FindIndex(u => u.assetId == childVea.id);
                    if (index != -1)
                    {
                        VisualElement parentSlot;
                        string key = templateAsset.slotUsages[index].slotName;
                        Assert.IsFalse(String.IsNullOrEmpty(key),
                            "a lost name should not be null or empty, this probably points to an importer or serialization bug");
                        if (context.slotInsertionPoints == null ||
                            !context.slotInsertionPoints.TryGetValue(key, out parentSlot))
                        {
                            Debug.LogErrorFormat("Slot '{0}' was not found. Existing slots: {1}", key,
                                context.slotInsertionPoints == null
                                ? String.Empty
                                : String.Join(", ",
                                    System.Linq.Enumerable.ToArray(context.slotInsertionPoints.Keys)));
                            ve.Add(childVe);
                        }
                        else
                            parentSlot.Add(childVe);
                    }
                    else
                        ve.Add(childVe);
                }
            }

            if (templateAsset != null && context.slotInsertionPoints != null)
                context.slotInsertionPoints.Clear();

            return ve;
        }

        public static void CloneTree(
            VisualTreeAsset vta, VisualElement target,
            Dictionary<string, VisualElement> slotInsertionPoints,
            List<CreationContext.AttributeOverrideRange> attributeOverridesRanges)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if ((vta.visualElementAssets == null || vta.visualElementAssets.Count <= 0) &&
                (vta.templateAssets == null || vta.templateAssets.Count <= 0))
                return;

            var idToChildren = VisualTreeAssetUtilities.GenerateIdToChildren(vta);

            List<VisualElementAsset> rootAssets;

            // Tree root has parentId == 0
            idToChildren.TryGetValue(0, out rootAssets);
            if (rootAssets == null || rootAssets.Count == 0)
                return;

            var uxmlRootAsset = rootAssets[0];

            vta.AssignClassListFromAssetToElement(uxmlRootAsset, target);
            vta.AssignStyleSheetFromAssetToElement(uxmlRootAsset, target);

            // Get the first-level elements. These will be instantiated and added to target.
            idToChildren.TryGetValue(uxmlRootAsset.id, out rootAssets);
            if (rootAssets == null || rootAssets.Count == 0)
                return;

            rootAssets.Sort(VisualTreeAssetUtilities.CompareForOrder);
            foreach (VisualElementAsset rootElement in rootAssets)
            {
                Assert.IsNotNull(rootElement);

                // Don't try to instatiate the special selection tracking element.
                if (rootElement.fullTypeName == BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName)
                    continue;

                var rootVe = CloneSetupRecursively(vta, rootElement, idToChildren,
                    new CreationContext(slotInsertionPoints, attributeOverridesRanges, vta, target));

                // if contentContainer == this, the shadow and the logical hierarchy are identical
                // otherwise, if there is a CC, we want to insert in the shadow
                target.hierarchy.Add(rootVe);
            }
        }

        public static void CloneTree(VisualTreeAsset vta, VisualElement target)
        {
            try
            {
                CloneTree(vta, target, s_TemporarySlotInsertionPoints, null);
            }
            finally
            {
                s_TemporarySlotInsertionPoints.Clear();
            }
        }
    }
}
