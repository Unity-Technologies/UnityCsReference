// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderVisualTreeStyleUpdaterTraversal : VisualTreeStyleUpdaterTraversal
    {
        struct SavedContext
        {
            public static SavedContext none = new SavedContext();
            public List<StyleSheet> styleSheets;
            public StyleVariableContext variableContext;
        }

        SavedContext m_SavedContext = SavedContext.none;
        WeakReference<VisualElement> m_DocumentElement;
        WeakReference<VisualElement> m_PreviewDocumentElement;

        public VisualElement previewDocument
        {
            get => m_PreviewDocumentElement.TryGetTarget(out var previewDocument) ? previewDocument : null;
            set => m_PreviewDocumentElement = new WeakReference<VisualElement>(value);
        }

        public BuilderVisualTreeStyleUpdaterTraversal(VisualElement document)
        {
            m_DocumentElement = new WeakReference<VisualElement>(document);
        }

        void SaveAndClearContext()
        {
            var originalStyleSheets = new List<StyleSheet>();
            var originalVariableContext = styleMatchingContext.variableContext;

            for (var index = 0; index < styleMatchingContext.styleSheetCount; index++)
            {
                originalStyleSheets.Add(styleMatchingContext.GetStyleSheetAt(index));
            }

            styleMatchingContext.RemoveStyleSheetRange(0, styleMatchingContext.styleSheetCount);
            styleMatchingContext.variableContext = StyleVariableContext.none;

            m_SavedContext = new SavedContext() { styleSheets = originalStyleSheets, variableContext = originalVariableContext };
        }

        void RestoreSavedContext()
        {
            styleMatchingContext.RemoveStyleSheetRange(0, styleMatchingContext.styleSheetCount);
            foreach (var sheet in m_SavedContext.styleSheets)
            {
                styleMatchingContext.AddStyleSheet(sheet);
            }
            styleMatchingContext.variableContext = m_SavedContext.variableContext;
            m_SavedContext = SavedContext.none;
        }

        bool ShouldClearStyleContext(WeakReference<VisualElement> documentRef, VisualElement element)
        {
            return documentRef != null && documentRef.TryGetTarget(out var document) && document != null && element == document && document.styleSheets.count != 0;
        }

        public override void TraverseRecursive(VisualElement element, int depth)
        {
            if (ShouldSkipElement(element))
            {
                return;
            }

            // In order to ensure that only the selected preview theme is applied to the document content in the viewport, 
            // we clear the current style context to prevent the document element from inheriting from the actual Unity Editor theme.
            bool shouldClearStyleContext = ShouldClearStyleContext(m_DocumentElement, element) || ShouldClearStyleContext(m_PreviewDocumentElement, element);

            if (shouldClearStyleContext)
            {
                SaveAndClearContext();
            }
            try
            {
                base.TraverseRecursive(element, depth);
            }
            finally
            {
                if (shouldClearStyleContext)
                {
                    RestoreSavedContext();
                }
            }
        }
    }
}
