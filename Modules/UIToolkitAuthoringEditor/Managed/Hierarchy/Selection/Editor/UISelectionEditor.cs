// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    // Shared lifecycle for the UI selection editors (VisualElement / StyleRule): shows the content of the target,
    // which keeps the inspector's animation-recording state in sync with AnimationMode. Subclasses supply the
    // content for their target type.
    abstract class UISelectionEditor : UnityEditor.Editor
    {
        UISelectionInspectorContent m_Content;

        // The context of the target: where the inspector reads its document and sends its messages.
        protected AuthoringContext Context => AuthoringContext.For(target as UISelectionObject);

        protected abstract UISelectionInspectorContent CreateContent();

        protected virtual void OnEnable()
        {
            if (m_Content != null)
                m_Content.Target = target as UISelectionObject;
        }

        protected virtual void OnDisable()
        {
            if (m_Content != null)
                m_Content.Target = null;
        }

        protected virtual void OnDestroy()
        {
            m_Content?.Dispose();
            m_Content = null;
        }

        // The header comes first: the inspector GUI is built right after it, from the same content.
        internal override VisualElement CreateInspectorHeaderGUI()
        {
            m_Content?.Dispose();
            m_Content = CreateContent();
            m_Content.Target = target as UISelectionObject;
            return m_Content.Header;
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (m_Content == null)
                CreateInspectorHeaderGUI();

            return m_Content.Inspector;
        }

        public override bool UseDefaultMargins() => false; // no artificial padding
        internal override bool isHeaderSticky => true;
    }
}
