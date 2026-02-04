// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.Assertions;

namespace Unity.UI.Builder
{
    internal static class VisualTreeAssetLinkedCloneTree
    {
        static readonly Dictionary<string, VisualElement> s_TemporarySlotInsertionPoints = new Dictionary<string, VisualElement>();

        static VisualElement CloneSetupRecursively(VisualTreeAsset vta, VisualElementAsset root, CreationContext context)
        {
            if (root.skipClone)
                return null;

            if (root.serializedData == null && UxmlSerializedDataRegistry.GetDescription(root.fullTypeName) is UxmlSerializedDataDescription desc)
            {
                root.serializedData = desc.CreateSerializedData();
                if (root.properties != null)
                {
                    foreach (var p in root.properties)
                    {
                        UxmlSerializer.TryParseSerializedAttribute(p.name, p.value, root.serializedData, new CreationContext(vta));
                    }
                }
            }

            var ve = VisualTreeAsset.Create(root, context, null);

            if (ve == null)
                return null;

            // Save reference to the visualElementAsset so elements can be reinitialized when
            // we set their attributes in the editor
            ve.visualElementAsset = root;

            // Save reference to the VisualTreeAsset itself on the containing VisualElement so it can be
            // tracked for live reloading on changes, and also accessible for users that need to keep track
            // of their cloned VisualTreeAssets.
            ve.visualTreeAssetSource = vta;

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

            for (var i = 0; i < root.childCount; ++i)
            {
                var childVea = root[i] as VisualElementAsset;

                // It can be a UxmlObjectAsset. We only want to clone VisualElementAssets
                if (childVea == null)
                {
                    continue;
                }

                var isTemplate = false;
                if (childVea is TemplateAsset)
                {
                    context.veaIdsPath.Add(childVea.id);
                    isTemplate = true;
                }

                var childVe = CloneSetupRecursively(vta, childVea, context);

                if (isTemplate)
                {
                    context.veaIdsPath.Remove(childVea.id);
                }

                if (childVe == null)
                    continue;

                childVe.visualTreeAssetSource = vta;

                // Save reference to the visualElementAsset so elements can be reinitialized when
                // we set their attributes in the editor
                childVe.visualElementAsset = childVea;

                var index = templateAsset?.slotUsages?.FindIndex(u => u.assetId == childVea.id) ?? -1;
                if (index != -1)
                {
                    VisualElement parentSlot;
                    var key = templateAsset.slotUsages[index].slotName;
                    Assert.IsFalse(string.IsNullOrEmpty(key),
                        "a lost name should not be null or empty, this probably points to an importer or serialization bug");
                    if (context.slotInsertionPoints == null ||
                        !context.slotInsertionPoints.TryGetValue(key, out parentSlot))
                    {
                        Debug.LogErrorFormat("Slot '{0}' was not found. Existing slots: {1}", key,
                            context.slotInsertionPoints == null
                                ? String.Empty
                                : String.Join(", ",
                                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                                    System.Linq.Enumerable.ToArray(context.slotInsertionPoints.Keys)));
#pragma warning restore UA2001
                        ve.Add(childVe);
                    }
                    else
                        parentSlot.Add(childVe);
                }
                else
                    ve.Add(childVe);
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

            if (null == vta.visualTreeNoAlloc)
                return;

            var cc = new CreationContext(slotInsertionPoints, attributeOverridesRanges);

            var root = vta.visualTree;
            vta.AssignClassListFromAssetToElement(root, target);
            vta.AssignStyleSheetFromAssetToElement(root, target);

            for (var i = 0; i < root.childCount; ++i)
            {
                // Assumes the m_VisualTree only contain VisualElementAssets.
                var child = root[i] as VisualElementAsset;

                // Don't try to instantiate the special selection tracking element.
                if (child.fullTypeName == BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName)
                    continue;

                var veaIds = new List<int>();

                var childElement = CloneSetupRecursively(vta, child, new CreationContext(slotInsertionPoints, attributeOverridesRanges, null, vta, target, veaIds, null, null));

                if (childElement == null)
                    continue;

                // if contentContainer == this, the shadow and the logical hierarchy are identical
                // otherwise, if there is a CC, we want to insert in the shadow
                target.hierarchy.Add(childElement);
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
