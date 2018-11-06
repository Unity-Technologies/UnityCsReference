// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.UIElements;
using VisualElementAsset = UnityEngine.UIElements.VisualElementAsset;

namespace UnityEngine.Experimental.UIElements
{
    [Serializable]
    public class VisualTreeAsset : UnityEngine.UIElements.VisualTreeAsset
    {
        public VisualElement CloneTree(Dictionary<string, VisualElement> slotInsertionPoints)
        {
            var tc = new TemplateContainer(name);
            CloneTree(tc, slotInsertionPoints ?? new Dictionary<string, VisualElement>());
            return tc;
        }

        public VisualElement CloneTree(Dictionary<string, VisualElement> slotInsertionPoints, string bindingPath)
        {
            var tc = CloneTree(slotInsertionPoints) as TemplateContainer;
            tc.bindingPath = bindingPath;
            return tc;
        }

        public void CloneTree(VisualElement target, Dictionary<string, VisualElement> slotInsertionPoints)
        {
            if (target == null)
                throw new ArgumentNullException("target", "Cannot clone a Visual Tree in a null target");

            if ((visualElementAssets == null || visualElementAssets.Count <= 0) &&
                (templateAssets == null || templateAssets.Count <= 0))
                return;

            Dictionary<int, List<VisualElementAsset>> idToChildren = new Dictionary<int, List<VisualElementAsset>>();
            int eltcount = visualElementAssets == null ? 0 : visualElementAssets.Count;
            int tplcount = templateAssets == null ? 0 : templateAssets.Count;
            for (int i = 0; i < eltcount + tplcount; i++)
            {
                VisualElementAsset asset = i < eltcount ? visualElementAssets[i] : templateAssets[i - eltcount];
                List<VisualElementAsset> children;
                if (!idToChildren.TryGetValue(asset.parentId, out children))
                {
                    children = new List<VisualElementAsset>();
                    idToChildren.Add(asset.parentId, children);
                }

                children.Add(asset);
            }

            // all nodes under the tree root have a parentId == 0
            List<VisualElementAsset> rootAssets;
            if (idToChildren.TryGetValue(0, out rootAssets) && rootAssets != null)
            {
                foreach (VisualElementAsset rootElement in rootAssets)
                {
                    Assert.IsNotNull(rootElement);
                    VisualElement rootVe = CloneSetupRecursively(rootElement, idToChildren,
                        new CreationContext(slotInsertionPoints, this, target));

                    // if contentContainer == this, the shadow and the logical hierarchy are identical
                    // otherwise, if there is a CC, we want to insert in the shadow
                    target.shadow.Add(rootVe);
                }
            }
        }

        private VisualElement CloneSetupRecursively(VisualElementAsset root,
            Dictionary<int, List<VisualElementAsset>> idToChildren, CreationContext context)
        {
            VisualElement ve = Create(root, context);

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
            if (context.slotInsertionPoints != null && TryGetSlotInsertionPoint(root.id, out slotName))
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
                if (inlineSheet == null)
                    Debug.LogWarning("VisualElementAsset has a RuleIndex but no inlineStyleSheet");
                else
                {
                    StyleRule r = inlineSheet.rules[root.ruleIndex];
                    var stylesData = new VisualElementStylesData(false);
                    ve.SetInlineStyles(stylesData);
                    stylesData.ApplyRule(inlineSheet, Int32.MaxValue, r,
                        StyleSheetCache.GetPropertyIDs(inlineSheet, root.ruleIndex));
                }
            }

            var templateAsset = root as TemplateAsset;
            List<VisualElementAsset> children;
            if (idToChildren.TryGetValue(root.id, out children))
            {
                foreach (VisualElementAsset childVea in children)
                {
                    // this will fill the slotInsertionPoints mapping
                    VisualElement childVe = CloneSetupRecursively(childVea, idToChildren, context);
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

        internal static VisualElement Create(VisualElementAsset asset, CreationContext ctx)
        {
            List<IUxmlFactory> factoryList;
            if (!VisualElementFactoryRegistry.TryGetValue(asset.fullTypeName, out factoryList))
            {
                if (asset.fullTypeName.StartsWith("UnityEngine.UIElements.") || asset.fullTypeName.StartsWith("UnityEditor.UIElements."))
                {
                    string experimentalTypeName = asset.fullTypeName.Replace(".UIElements", ".Experimental.UIElements");
                    if (!VisualElementFactoryRegistry.TryGetValue(experimentalTypeName, out factoryList))
                    {
                        Debug.LogErrorFormat("Element '{0}' has no registered factory method.", asset.fullTypeName);
                        return new Label(string.Format("Unknown type: '{0}'", asset.fullTypeName));
                    }
                }
                else
                {
                    Debug.LogErrorFormat("Element '{0}' has no registered factory method.", asset.fullTypeName);
                    return new Label(string.Format("Unknown type: '{0}'", asset.fullTypeName));
                }
            }

            IUxmlFactory factory = null;
            foreach (IUxmlFactory f in factoryList)
            {
                if (f.AcceptsAttributeBag(asset, ctx))
                {
                    factory = f;
                    break;
                }
            }

            if (factory == null)
            {
                Debug.LogErrorFormat("Element '{0}' has a no factory that accept the set of XML attributes specified.", asset.fullTypeName);
                return new Label(string.Format("Type with no factory: '{0}'", asset.fullTypeName));
            }

            if (factory is UxmlRootElementFactory)
            {
                return null;
            }

            VisualElement res = factory.Create(asset, ctx);
            if (res == null)
            {
                Debug.LogErrorFormat("The factory of Visual Element Type '{0}' has returned a null object", asset.fullTypeName);
                return new Label(string.Format("The factory of Visual Element Type '{0}' has returned a null object", asset.fullTypeName));
            }

            if (asset.classes != null)
            {
                for (int i = 0; i < asset.classes.Length; i++)
                    res.AddToClassList(asset.classes[i]);
            }

            if (asset.stylesheets != null)
            {
                for (int i = 0; i < asset.stylesheets.Count; i++)
                    res.AddStyleSheetPath(asset.stylesheets[i]);
            }

            return res;
        }
    }

    public struct CreationContext
    {
        public static readonly CreationContext Default = new CreationContext();
        public VisualElement target { get; private set; }
        public VisualTreeAsset visualTreeAsset { get; private set; }
        public Dictionary<string, VisualElement> slotInsertionPoints { get; private set; }

        internal CreationContext(Dictionary<string, VisualElement> slotInsertionPoints, VisualTreeAsset vta, VisualElement target)
        {
            this.target = target;
            this.slotInsertionPoints = slotInsertionPoints;
            visualTreeAsset = vta;
        }
    }
}
