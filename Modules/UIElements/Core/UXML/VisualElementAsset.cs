// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine.Bindings;
using CollectionExtensions = Unity.Collections.CollectionExtensions;

namespace UnityEngine.UIElements
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class VisualElementAsset : UxmlAsset
    {
        internal const string k_LostInlineStyles = $"{nameof(VisualElementAsset)} previously had inline styles that were lost.";

        [SerializeField]
        private int m_RuleIndex = -1;

        public int ruleIndex
        {
            get { return m_RuleIndex; }
            set { m_RuleIndex = value; }
        }

        [SerializeField]
        private string[] m_Classes = Array.Empty<string>();

        public IReadOnlyList<string> classes => m_Classes;

        internal void SetClasses(string[] newClasses)
        {
            // Copy the array reference as is. Behavior is undefined if the original is mutated afterwards.
            m_Classes = newClasses;
            m_ClassesUnique = null;
        }

        // A lazy evaluated, once per asset conversion to make AddToClassList faster during CloneTree.
        // Resets whenever the class list is modified.
        [NonSerialized]
        private UniqueStyleString[] m_ClassesUnique;
        public UniqueStyleString[] classesUnique
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (m_ClassesUnique == null && m_Classes != null)
                {
                    m_ClassesUnique = m_Classes.Length > 0 ?
                        Array.ConvertAll(m_Classes, s => new UniqueStyleString(s)) :
                        Array.Empty<UniqueStyleString>();
                }
                return m_ClassesUnique;
            }
        }

        [SerializeField]
        private List<string> m_StylesheetPaths;

        public List<string> stylesheetPaths
        {
            get { return m_StylesheetPaths ?? (m_StylesheetPaths = new List<string>()); }
            set { m_StylesheetPaths = value; }
        }

        public bool hasStylesheetPaths => m_StylesheetPaths != null;

        [SerializeField]
        private List<StyleSheet> m_Stylesheets;

        public List<StyleSheet> stylesheets
        {
            get => m_Stylesheets ??= new List<StyleSheet>();
            set => m_Stylesheets = value;
        }

        public bool hasStylesheets => m_Stylesheets != null;

        [SerializeReference]
        private UxmlSerializedData m_SerializedData;

        public UxmlSerializedData serializedData
        {
            get => m_SerializedData;
            set => m_SerializedData = value;
        }

        [SerializeField] private bool m_SkipClone;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool skipClone
        {
            get => m_SkipClone;
            set => m_SkipClone = value;
        }

        public VisualElementAsset(string fullTypeName, UxmlNamespaceDefinition xmlNamespace = default)
            : base(fullTypeName, xmlNamespace)
        {
        }

        private static bool IdsPathMatchesAttributeOverrideIdsPath(List<int> idsPath, List<int> attributeOverrideIdsPath, int templateId)
        {
            if (idsPath == null || attributeOverrideIdsPath == null
                                || idsPath.Count == 0 || attributeOverrideIdsPath.Count == 0)
            {
                return false;
            }

            var templateIdIndex = idsPath.IndexOf(templateId);

            if (idsPath.Count != attributeOverrideIdsPath.Count + templateIdIndex + 1)
            {
                return false;
            }

            for (var i = idsPath.Count - 1; i > templateIdIndex; --i)
            {
                if (idsPath[i] != attributeOverrideIdsPath[i - templateIdIndex - 1])
                {
                    return false;
                }
            }

            return true;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal virtual VisualElement Instantiate(CreationContext cc, VisualElementAssetReferenceTable.DocumentNode parentAuthoringNode = null)
        {
            var ve = (VisualElement) serializedData.CreateInstance();
            serializedData.Deserialize(ve);

            if (cc.templateAsset != null)
            {
                ve.templateAsset = cc.templateAsset;
            }

            if (cc.hasOverrides)
            {
                cc.veaIdsPath.Add(id);
                // Applying the overrides in reverse order. This means that the deepest overrides, the ones from nested VisualTreeAssets,
                // will be applied first and might be overridden by parent VisualTreeAssets.
                for (var i = cc.serializedDataOverrides.Count - 1; i >= 0; --i)
                {
                    foreach (var attributeOverride in cc.serializedDataOverrides[i].attributeOverrides)
                    {
                        if (attributeOverride.m_ElementId == id && IdsPathMatchesAttributeOverrideIdsPath(cc.veaIdsPath, attributeOverride.m_ElementIdsPath, cc.serializedDataOverrides[i].templateId))
                        {
                            attributeOverride.m_SerializedData.Deserialize(ve);
                        }
                    }
                }
                cc.veaIdsPath.Remove(id);
            }

            if (hasStylesheetPaths)
            {
                for (var i = 0; i < stylesheetPaths.Count; i++)
                    ve.AddStyleSheetPath(stylesheetPaths[i]);
            }

            if (hasStylesheets)
            {
                for (var i = 0; i < stylesheets.Count; ++i)
                {
                    if (stylesheets[i] != null)
                        ve.styleSheets.Add(stylesheets[i]);
                }
            }

            AssignClassListToElement(ve);

            if (hasAuthoringId && parentAuthoringNode != null)
                parentAuthoringNode.AddElement(id, ve);

            return ve;
        }

        internal override bool Accepts(UxmlAsset asset, out string errorMessage)
        {
            var result = !asset.isRoot;

            errorMessage = !result
                ? "[UI Toolkit] Cannot add a root UXML asset as a children of a UXML asset."
                : null;

            return result;
        }

        public override string ToString()
        {
            return TryGetAttributeValue("name", out var name)
                ? $"{name}({fullTypeName})({id})"
                : $"({fullTypeName})({id})";
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void AddStyleSheet(StyleSheet styleSheet)
        {
            if (styleSheet == null || stylesheets.Contains(styleSheet))
                return;

            stylesheets.Add(styleSheet);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        public void AddStyleSheets(IEnumerable<StyleSheet> styleSheets)
        {
            foreach (var styleSheet in styleSheets)
            {
                AddStyleSheet(styleSheet);
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void RemoveStyleSheet(StyleSheet styleSheet)
        {
            stylesheets.RemoveAll((s) => s == styleSheet);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void AddStyleClass(string className)
        {
            m_Classes ??= Array.Empty<string>();
            if (Array.IndexOf(m_Classes, className) == -1)
            {
                CollectionExtensions.AddToArray(ref m_Classes, className);
                m_ClassesUnique = null;
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void RemoveStyleClass(string className)
        {
            if (m_Classes == null)
                return;
            CollectionExtensions.RemoveFromArray(ref m_Classes, className);
            m_ClassesUnique = null;
        }

        public bool ContainsStyleClass(string className)
        {
            return m_Classes != null && m_Classes.Contains(className);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        public void ClearStyleSheets()
        {
            stylesheets.Clear();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void AssignClassListToElement(VisualElement ve)
        {
            if (m_Classes != null && m_Classes.Length > 0)
            {
                ve.AddToClassList(classesUnique);
            }
        }

        // For performance tests
        internal void ResetCachedClassList()
        {
            m_ClassesUnique = null;
        }

        private protected override void OnVisualTreeAssetChanged(VisualTreeAsset previousVta, VisualTreeAsset newVta)
        {
            base.OnVisualTreeAssetChanged(previousVta, newVta);
            // No inline styles, nothing to do.
            if (ruleIndex < 0)
                return;

            if (!previousVta)
            {
                ruleIndex = -1;
                Debug.LogWarning(k_LostInlineStyles);
                return;
            }

            // Transfer inline styles.
            if (newVta)
            {
                VisualTreeAsset.SwallowStyleRule(previousVta, newVta, this);
            }
        }
    }
}
